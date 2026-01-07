using HraBot.Core;
using HraBot.MigrationService;
using HraBot.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry().WithTracing(t => t.AddSource(Worker.ActivitySourceName));
builder.Services.AddDbContext<HraBotDbContext>(o =>
    o.UseNpgsql(
        Environment.GetEnvironmentVariable($"ConnectionStrings__{AppServices.hraBotDb}")
            ?? throw new InvalidOperationException($"Could not read hraBotDb connection string")
    )
);
var host = builder.Build();
host.Run();
