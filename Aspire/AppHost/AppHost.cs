var builder = DistributedApplication.CreateBuilder(args);

var backend = builder.AddProject<Projects.Backend>("backend")
	.WithEnvironment("OpenPlayer", "false");

builder.AddJavaScriptApp("player", "../../Player")
	.WithRunScript("dev")
	.WithReference(backend)
	.WaitFor(backend)
	.WithHttpEndpoint(env: "PORT")
	.WithExternalHttpEndpoints();

builder.AddJavaScriptApp("remote", "../../Remote")
	.WithRunScript("dev")
	.WithReference(backend)
	.WaitFor(backend)
	.WithHttpEndpoint(env: "PORT")
	.WithExternalHttpEndpoints();

builder.Build().Run();
