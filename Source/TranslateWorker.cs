using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ValheimAutoTranslator
{
    public static class TranslateWorker
    {
        private sealed class Job
        {
            public string Context;
            public string Source;
            public string Key;
            public int Attempts;
            public string LastFailure;
        }

        private static readonly ConcurrentQueue<Job> Queue = new ConcurrentQueue<Job>();
        private static readonly ConcurrentDictionary<string, byte> Pending =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, long> Rejected =
            new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private static readonly AutoResetEvent Wake = new AutoResetEvent(false);
        private static Thread[] threads;
        private static volatile bool running;
        private static GATSettings settings;
        private static long nextNetworkTryUtcTicks;
        private static int networkFailureStreak;
        private const int MaxPending = 5000;
        private const int MaxRejected = 5000;

        public static void Start(GATSettings value)
        {
            if (running) return;
            settings = value;
            running = true;
            int n = Math.Max(1, Math.Min(4, settings.maxConcurrent));
            threads = new Thread[n];
            for (int i = 0; i < n; i++)
            {
                threads[i] = new Thread(Work);
                threads[i].IsBackground = true;
                threads[i].Name = "ValheimAutoTranslator-" + i;
                threads[i].Start();
            }
        }

        public static void Stop()
        {
            running = false;
            Wake.Set();
            var current = threads;
            if (current == null) return;
            foreach (Thread thread in current)
                if (thread != null && thread.IsAlive) thread.Join(3000);
        }

        public static void Request(string context, string source)
        {
            if (!running || TextSafety.IsDemoChangelog(source) ||
                TranslationCache.IsKnownTranslation(source) ||
                TranslationCache.IsPermanentFailed(context, source) ||
                !PlaceholderGuard.NeedsTranslation(source)) return;
            string cached;
            if (TranslationCache.TryGet(context, source, out cached)) return;
            string key = context + "\t" + source;
            long rejectedAt;
            if (Rejected.TryGetValue(key, out rejectedAt))
            {
                if (DateTime.UtcNow.Ticks - rejectedAt < TimeSpan.FromMinutes(10).Ticks) return;
                long ignored;
                Rejected.TryRemove(key, out ignored);
            }
            if (Pending.Count >= MaxPending || !Pending.TryAdd(key, 0)) return;
            Queue.Enqueue(new Job { Context = context, Source = source, Key = key });
            Wake.Set();
        }

        private static void Work()
        {
            while (running)
            {
                long waitTicks = Interlocked.Read(ref nextNetworkTryUtcTicks) - DateTime.UtcNow.Ticks;
                if (waitTicks > 0)
                {
                    Wake.WaitOne((int)Math.Min(1000, Math.Max(1, waitTicks / TimeSpan.TicksPerMillisecond)));
                    continue;
                }
                Job first;
                if (!Queue.TryDequeue(out first)) { Wake.WaitOne(500); continue; }
                var jobs = new List<Job> { first };
                int max = Math.Max(1, Math.Min(80, settings.batchSize));
                while (jobs.Count < max)
                {
                    Job next;
                    if (!Queue.TryPeek(out next) || next.Context != first.Context) break;
                    if (!Queue.TryDequeue(out next)) break;
                    jobs.Add(next);
                }
                try { Process(jobs); }
                catch (Exception ex)
                {
                    GATLog.Warn("Worker error: " + ex);
                    foreach (Job job in jobs) Retry(job);
                }
            }
        }

        private static void Process(List<Job> jobs)
        {
            var items = new Dictionary<string, string>();
            for (int i = 0; i < jobs.Count; i++) items[i.ToString()] = jobs[i].Source;
            bool isRetry = false;
            var hints = new Dictionary<string, string>();
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].Attempts == 0) continue;
                isRetry = true;
                Dictionary<int, string> markers;
                PlaceholderGuard.MaskPlaceholders(jobs[i].Source, out markers);
                string hint = PlaceholderGuard.BuildRetryHint(markers);
                if (jobs[i].LastFailure == "модель вернула исходную строку без перевода")
                    hint += " Предыдущий ответ повторил исходную строку. Переведи английские слова на русский; не возвращай оригинал.";
                hints[i.ToString()] = hint;
            }
            var result = LlmClient.TranslateBatch(settings, jobs[0].Context, items, isRetry, hints);
            if (result == null)
            {
                int failures = Interlocked.Increment(ref networkFailureStreak);
                int seconds = Math.Min(300, 15 * (1 << Math.Min(4, failures - 1)));
                Interlocked.Exchange(ref nextNetworkTryUtcTicks, DateTime.UtcNow.AddSeconds(seconds).Ticks);
                foreach (Job job in jobs) Queue.Enqueue(job);
                GATLog.Warn("Translation endpoint unavailable. Retrying in " + seconds + " seconds.");
                return;
            }
            Interlocked.Exchange(ref networkFailureStreak, 0);
            Interlocked.Exchange(ref nextNetworkTryUtcTicks, 0);
            int accepted = 0;
            for (int i = 0; i < jobs.Count; i++)
            {
                Job job = jobs[i];
                string translated;
                string reason = null;
                bool present = result.TryGetValue(i.ToString(), out translated);
                if (present && PlaceholderGuard.Validate(job.Source, translated, out reason))
                {
                    if (!TranslationCache.Put(job.Context, job.Source, translated))
                    {
                        Retry(job, "cache did not accept validated translation");
                        continue;
                    }
                    byte ignored;
                    Pending.TryRemove(job.Key, out ignored);
                    ValheimPlugin.ScheduleRefresh();
                    accepted++;
                }
                else Retry(job, present ? reason : "missing response id");
            }
            if (settings.verboseLogging) GATLog.Msg("Batch " + jobs[0].Context + ": " + accepted + "/" + jobs.Count);
        }

        private static void Retry(Job job, string reason = "worker error")
        {
            job.LastFailure = reason;
            job.Attempts++;
            if (!running)
            {
                byte stopped;
                Pending.TryRemove(job.Key, out stopped);
                return;
            }
            if (job.Attempts >= 3)
            {
                byte ignored;
                Pending.TryRemove(job.Key, out ignored);
                if (Rejected.Count >= MaxRejected) Rejected.Clear();
                Rejected[job.Key] = DateTime.UtcNow.Ticks;
                if (PlaceholderGuard.IsStructuralFailure(reason))
                    TranslationCache.AddPermanentFailed(job.Context, job.Source, reason);
                if (settings != null && settings.verboseLogging)
                    GATLog.Warn("Rejected " + job.Context + " (length " + job.Source.Length + "): " + reason);
                return;
            }
            if (settings != null && settings.verboseLogging)
                GATLog.Warn("Retry " + job.Context + " (length " + job.Source.Length + ", " + job.Attempts + "/3): " + reason);
            Queue.Enqueue(job);
            Wake.Set();
        }
    }
}
