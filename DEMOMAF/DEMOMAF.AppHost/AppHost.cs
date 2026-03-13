var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WebUI>("webui");

builder.AddProject<Projects.ChatFrontend>("chatfrontend");

builder.AddProject<Projects.DEMOMAF_DnDPortal>("dndportal");

builder.Build().Run();
