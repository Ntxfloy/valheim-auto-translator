using BepInEx.Configuration;

namespace ValheimAutoTranslator
{
    public sealed class GATSettings
    {
        public string endpoint = "http://127.0.0.1:8317/v1/chat/completions";
        public string model = "gemini-3.8-flash-high";
        public string apiKey = "";
        public int timeoutSeconds = 120;
        public int batchSize = 25;
        public int maxConcurrent = 2;
        public bool sendReasoningEffortNone = true;
        public bool requestJsonObject = true;
        public bool verboseLogging = true;

        public static GATSettings Load(ConfigFile config)
        {
            var s = new GATSettings();
            s.endpoint = config.Bind("Connection", "Endpoint", s.endpoint, "OpenAI-compatible chat completions URL").Value;
            s.model = config.Bind("Connection", "Model", s.model, "Model name").Value;
            s.apiKey = config.Bind("Connection", "ApiKey", s.apiKey, "Optional API key").Value;
            s.timeoutSeconds = config.Bind("Connection", "TimeoutSeconds", s.timeoutSeconds, "Request timeout").Value;
            s.batchSize = config.Bind("Worker", "BatchSize", s.batchSize, "Lines per batch").Value;
            s.maxConcurrent = config.Bind("Worker", "MaxConcurrent", s.maxConcurrent, "Worker count").Value;
            s.sendReasoningEffortNone = config.Bind("Connection", "SendReasoningEffortNone", s.sendReasoningEffortNone, "Send reasoning_effort=none").Value;
            s.requestJsonObject = config.Bind("Connection", "RequestJsonObject", s.requestJsonObject, "Request JSON response").Value;
            s.verboseLogging = config.Bind("Diagnostics", "VerboseLogging", s.verboseLogging, "Log batches and errors").Value;
            return s;
        }
    }
}
