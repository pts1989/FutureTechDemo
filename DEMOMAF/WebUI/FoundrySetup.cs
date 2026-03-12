using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEMOMAF
{
    public static class FoundrySetup
    {

        public static Uri GetUri()
        {
            return new Uri(config.Web.Urls + "/v1");
        }

        private static Configuration config = new Configuration
        {
            AppName = "app-name",
            LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Debug,
            Web = new Configuration.WebService
            {
                Urls = "http://127.0.0.1:8576"
            }
        };

        static FoundryLocalManager mgr = null;
        static Model model = null;

        public static async Task StartChatFoundryService(string modelName)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });
            var logger = loggerFactory.CreateLogger<Program>();

            if(mgr == null)
            {
                await FoundryLocalManager.CreateAsync(config, logger);
            }
            
            mgr = FoundryLocalManager.Instance;

            // Get the model catalog
            var catalog = await mgr.GetCatalogAsync();


            // Get a model using an alias
            model = await catalog.GetModelAsync(modelName) ?? throw new Exception("Model not found");

            // is model cached
            Console.WriteLine($"Is model cached: {await model.IsCachedAsync()}");

            // print out cached models
            var cachedModels = await catalog.GetCachedModelsAsync();
            Console.WriteLine("Cached models:");
            foreach (var cachedModel in cachedModels)
            {
                Console.WriteLine($"- {cachedModel.Alias} ({cachedModel.Id})");
            }

            // Download the model (the method skips download if already cached)
            await model.DownloadAsync(progress =>
            {
                Console.Write($"\rDownloading model: {progress:F2}%");
                if (progress >= 100f)
                {
                    Console.WriteLine();
                }
            });

            // Initialize the singleton instance.

            // Load the model
            await model.LoadAsync();

            // Start the web service
            await mgr.StartWebServiceAsync();

        }

        public static IChatClient StartChatClient(string modelName = "phi-4-mini")
        {
           // await StartChatFoundryService(modelName);

            ApiKeyCredential key = new ApiKeyCredential("notneeded");
            OpenAIClient client = new OpenAIClient(key, new OpenAIClientOptions
            {
                Endpoint = new Uri(config.Web.Urls + "/v1"),
            });
            var chatClient = client.GetChatClient(modelName).AsIChatClient();
            return chatClient;
        }

        public static async Task StopFoundryAsync()
        {
            if (mgr != null)
            {
                await mgr.StopWebServiceAsync();
                await model.UnloadAsync();
            }
        }
    }
}
