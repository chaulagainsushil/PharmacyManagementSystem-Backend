using Microsoft.EntityFrameworkCore;
using PMS.DependencyHandler;
using PMS.Repository.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity + JWT + ITenantProvider + Auth + Subscription Services ───────────
builder.Services.AddApplicationServices(builder.Configuration);

// ── CRUD Services ─────────────────────────────────────────────────────────────
builder.Services.AddCrudServices();

// ── IP-based Rate Limiting ────────────────────────────────────────────────────
builder.Services.AddConfiguredRateLimiting(builder.Configuration);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    var allowedOriginsCommaSeparated = builder.Configuration.GetSection("AllowedOrigins").Get<string>() ?? string.Empty;
    var allowedOrigins = allowedOriginsCommaSeparated.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    options.AddPolicy("MyAppPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ── Controllers + Subscription Gate Filter + OpenAPI ─────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()))
    .AddSubscriptionGateFilter();   // registers SubscriptionGateFilter globally

builder.Services.AddOpenApi();

var app = builder.Build();

// ── Seed roles ────────────────────────────────────────────────────────────────
await app.SeedRolesAsync();

// ── Seed subscription plan definitions ───────────────────────────────────────
await app.SeedSubscriptionPlansAsync();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("MyAppPolicy");

// Rate limiting must come before auth so throttled requests are rejected early
app.UseRateLimiter();

// Serves files from wwwroot — includes:
//   /prescriptions/*        (prescription images)
//   /pharmacy/logos/*       (pharmacy logo images)
//   /pharmacy/signatures/*  (authorised signature images)
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
