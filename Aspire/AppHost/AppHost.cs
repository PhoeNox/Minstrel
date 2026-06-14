var builder = DistributedApplication.CreateBuilder(args);

var musicDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "Backend", "Music"));

// Fixed so the Backend can advertise it under the QR code: a phone reaches the
// Remote's Vite dev server directly by LAN IP, since Aspire's proxy and DCP only
// resolve a resource's target port within that resource — not into the Backend.
const int remotePort = 5174;

var backend = builder.AddProject<Projects.Backend>("backend")
	.WithHttpEndpoint(env: "PORT")
	.WithEnvironment("OpenPlayer", "false")
	.WithEnvironment("RemotePort", remotePort.ToString())
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
	.WithHttpEndpoint(targetPort: remotePort, env: "PORT")
	.WithExternalHttpEndpoints();

builder.Build().Run();
