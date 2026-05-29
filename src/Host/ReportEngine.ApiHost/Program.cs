using Designer.Api;
using Labeling.Application;
using Labeling.Infrastructure;
using Reporting.Api;
using Templates.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Controllers — load from the host assembly AND all module assemblies
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(Reporting.Api.Controllers.ReportingController).Assembly)
    .AddApplicationPart(typeof(Templates.Api.Controllers.TemplatesController).Assembly)
    .AddApplicationPart(typeof(Designer.Api.Controllers.DesignerController).Assembly);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// ── Labeling module ───────────────────────────────────────────────────────────
builder.Services.AddLabelingInfrastructure(builder.Configuration);
builder.Services.AddLabelingApplication();

// ── Reporting module ──────────────────────────────────────────────────────────
builder.Services.AddReportingApi(builder.Configuration);

// ── Templates module ──────────────────────────────────────────────────────────
builder.Services.AddTemplatesApi(builder.Configuration);

// ── Designer module ──────────────────────────────────────────────────────────
builder.Services.AddDesignerApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static assets consumed by Playwright during HTML → PDF rendering.
// /fonts/  → wwwroot/fonts   (Thai fonts: THSarabun.ttf, etc.)
// /uploads/ → wwwroot/uploads (user-uploaded background images, logos, etc.)
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
