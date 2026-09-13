using Tansekak.Api;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.AddApi();

var app = builder.Build();

await app.UseApiPipelineAsync();

app.Run();

public partial class Program;
