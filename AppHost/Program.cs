var builder = DistributedApplication.CreateBuilder(args);

var game = builder.AddProject<Projects.Game>("game");
builder.AddProject<Projects.Controller>("controller")
	.WithReference(game);

await builder.Build().RunAsync();
