using System.Net;
using Nonation.Server;

var builder = WebApplication.CreateBuilder(args);

// Secure by default: only a loopback service unless explicitly deployed behind
// HTTPS reverse proxy with configured origins.
builder.WebHost.UseUrls(
    Environment.GetEnvironmentVariable("NONATION_LISTEN_URL") ?? "http://127.0.0.1:5180");
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.IncludeFields = true);
builder.Services.AddSingleton<WorldAuthority>();
builder.Services.AddHostedService<WorldSimulationWorker>();

string[] allowedOrigins = (Environment.GetEnvironmentVariable("NONATION_CORS_ORIGINS") ?? "")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(origin =>
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps ||
            (uri.Scheme == Uri.UriSchemeHttp && (uri.Host == "localhost" || uri.Host == "127.0.0.1"));
    })
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
    options.AddPolicy("SpectatorOnly", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).WithMethods("GET").AllowAnyHeader();
    }));

var app = builder.Build();
app.UseCors();

app.MapGet("/healthz",
    () => Results.Ok(new { status = "ok", service = "nonation", mode = "spectator-alpha" }));

// No authentication is needed for deliberately public, sanitized positions.
// Public endpoints never return financial or personal simulation state.
app.MapGet("/api/v1/spectator/scene",
    (HttpContext context, WorldAuthority authority) =>
    {
        context.Response.Headers.CacheControl = "no-store, max-age=0";
        return Results.Ok(authority.SpectatorSnapshot());
    })
    .RequireCors("SpectatorOnly");

// Internal diagnostic endpoint is DISABLED by default, including when served
// through a reverse proxy on the same machine. Never proxy this URL publicly.
if (Environment.GetEnvironmentVariable("NONATION_ENABLE_DEBUG_WORLD") == "1")
{
    app.MapGet("/api/v1/world", (HttpContext context, WorldAuthority authority) =>
    {
        IPAddress? ip = context.Connection.RemoteIpAddress;
        if (ip == null || !IPAddress.IsLoopback(ip)) return Results.NotFound();
        return Results.Ok(authority.Snapshot());
    });
}

app.Run();
