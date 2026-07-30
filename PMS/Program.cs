using Microsoft.EntityFrameworkCore;
using PMS.DependencyHandler;
using PMS.Repository.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity + JWT + Auth Services ───────────────────────────────────────────
builder.Services.AddApplicationServices(builder.Configuration);

// ── CRUD Services ─────────────────────────────────────────────────────────────
builder.Services.AddCrudServices();

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

// ── Controllers + OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("MyAppPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
