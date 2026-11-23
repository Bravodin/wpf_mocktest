using ErpMock.Api;
using ErpMock.Contracts;
using ErpMock.Core;

var builder = WebApplication.CreateBuilder(args);

// Banco: SQLite por padrão (nada para instalar); o docker-compose troca para PostgreSQL por variável de ambiente.
var provider = builder.Configuration.GetValue("Database:Provider", DatabaseProvider.Sqlite);
var connectionString = builder.Configuration.GetConnectionString("Erp")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Erp.");
builder.Services.AddErpCore(connectionString, provider);

builder.Services.ConfigureHttpJsonOptions(o => ErpJson.Configure(o.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();                 // /openapi/v1.json
app.MapHealthChecks("/health");
app.MapErpEndpoints();

await DatabaseSetup.InitializeAsync(app.Services, app.Logger);

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
