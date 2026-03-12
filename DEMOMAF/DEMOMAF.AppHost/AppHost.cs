var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WebUI>("webui");

builder.AddProject<Projects.ChatFrontend>("chatfrontend");

builder.Build().Run();
