using Tansekak.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddApi();

var app = builder.Build();

await app.UseApiPipelineAsync();

app.Run();

public partial class Program;
