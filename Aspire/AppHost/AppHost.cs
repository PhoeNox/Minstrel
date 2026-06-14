var builder = DistributedApplication.CreateBuilder(args);

var musicDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "Backend", "Music"));

var backend = builder.AddProject<Projects.Backend>("backend")
	.WithHttpEndpoint(env: "PORT")
	.WithEnvironment("OpenPlayer", "false")
	.WithEnvironment("Music__Directory", musicDirectory);

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
