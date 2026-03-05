var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Temasek_WebApi>("temasek-webapi");

builder.Build().Run();
