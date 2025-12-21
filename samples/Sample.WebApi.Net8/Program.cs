using Traceability.Extensions;
using Traceability.Logging;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog com CorrelationIdEnricher
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.With<CorrelationIdEnricher>()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Adicionar serviços de traceability
builder.Services.AddTraceability();

// Adicionar controllers
builder.Services.AddControllers();

// Adicionar HttpClient traceable
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
})
.AddHttpMessageHandler<Traceability.HttpClient.CorrelationIdHandler>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar o pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Adicionar middleware de correlation-id (deve ser adicionado antes dos controllers)
app.UseCorrelationId();

// Adicionar controllers
app.MapControllers();

// Exemplo de endpoint mínimo que usa correlation-id
app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    // O correlation-id está automaticamente disponível no contexto
    var correlationId = Traceability.CorrelationContext.Current;
    
    logger.LogInformation("Processando requisição de weather forecast com CorrelationId: {CorrelationId}", correlationId);
    
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
