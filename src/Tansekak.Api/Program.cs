using Tansekak.Api;

var builder = WebApplication.CreateBuilder(args);

// Render sets PORT in production. When unset, launchSettings (local :5080) applies.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.AddApi();

var app = builder.Build();

await app.UseApiPipelineAsync();

app.Run();

public partial class Program;
