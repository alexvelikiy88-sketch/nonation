using Nonation.Server;

var builder = WebApplication.CreateBuilder(args);
// The backend is intentionally loopback-only until authentication and secure commands exist.
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("NONATION_LISTEN_URL") ?? "http://127.0.0.1:5180");
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.IncludeFields = true);
builder.Services.AddSingleton<WorldAuthority>();
builder.Services.AddHostedService<WorldSimulationWorker>();
var app = builder.Build();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "nonation", mode = "readonly-alpha" }));
app.MapGet("/api/v1/world", (WorldAuthority authority) => Results.Ok(authority.Snapshot()));
app.Run();
