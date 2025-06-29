using APIContagem;
using APIContagem.Data;
using APIContagem.Tracing;
using Grafana.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContagemPostgresContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagemPostgres"),
        o => o.UseNodaTime());
});

builder.Services.AddDbContext<ContagemMySqlContext>(options =>
{
    options.UseMySQL(
        builder.Configuration.GetConnectionString("BaseContagemMySql")!);
});

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);

builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddNpgsql() // PostgreSQL
            .AddConnectorNet() // MySQL
            .AddEntityFrameworkCoreInstrumentation(cfg =>
            {
                cfg.SetDbStatementForText = true;
            })
            .AddConsoleExporter()
            .UseGrafana();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AttachLogsToActivityEvent();
    options.AddConsoleExporter();
    options.UseGrafana();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ContagemRepository>();
builder.Services.AddScoped<ContagemRegressivaRepository>();
builder.Services.AddSingleton<Contador>();

builder.Services.AddCors();

var app = builder.Build();

app.MapOpenApi();

app.UseAuthorization();

app.MapControllers();

app.Run();