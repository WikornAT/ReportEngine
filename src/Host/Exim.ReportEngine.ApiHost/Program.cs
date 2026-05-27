using Labeling.Application;
using Labeling.Infrastructure;
using Reporting.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Controllers — load from the host assembly AND all module assemblies
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(Reporting.Api.Controllers.ReportingController).Assembly);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// ── Labeling module ───────────────────────────────────────────────────────────
builder.Services.AddLabelingInfrastructure(builder.Configuration);
builder.Services.AddLabelingApplication();

// ── Reporting module ──────────────────────────────────────────────────────────
builder.Services.AddReportingApi(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
