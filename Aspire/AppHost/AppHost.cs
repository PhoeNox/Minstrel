var builder = DistributedApplication.CreateBuilder(args);

var backend = builder.AddProject<Projects.Backend>("backend")
	.WithEnvironment("OpenPlayer", "false");

builder.AddNpmApp("player", "../../Player", "dev")
	.WithReference(backend)
	.WaitFor(backend)
	.WithHttpEndpoint(env: "PORT")
	.WithExternalHttpEndpoints();

builder.Build().Run();
