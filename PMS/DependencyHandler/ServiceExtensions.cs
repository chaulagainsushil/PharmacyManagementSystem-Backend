using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Application.Interface.Repository;
using PMS.Application.Application.Interface.Service;
using PMS.Application.Application.Repository;
using PMS.Application.Application.Service;
using PMS.Application.Application.Validators;
using PMS.BackgroundJobs;
using PMS.Common;
using PMS.Filters;
using PMS.Models.Enums;
using PMS.Models.Interfaces;
using PMS.Models.Models;
using PMS.Repository.Data;
using PMS.TenantProvider;

namespace PMS.DependencyHandler;

public static class ServiceExtensions
{
    /// <summary>
    /// Registers ASP.NET Identity, JWT Authentication, ITenantProvider, and all application services.
    /// Password rules are read from the <c>PasswordPolicy</c> section in appsettings.json.
    /// Call this from Program.cs: builder.Services.AddApplicationServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Bind PasswordPolicy from configuration ────────────────────────────
        var passwordPolicy = configuration
            .GetSection(PasswordPolicy.Section)
            .Get<PasswordPolicy>() ?? new PasswordPolicy();

        // ── ASP.NET Identity ─────────────────────────────────────────────────
        services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength         = passwordPolicy.RequiredLength;
                options.Password.RequireUppercase       = passwordPolicy.RequireUppercase;
                options.Password.RequireLowercase       = passwordPolicy.RequireLowercase;
                options.Password.RequireDigit           = passwordPolicy.RequireDigit;
                options.Password.RequireNonAlphanumeric = passwordPolicy.RequireNonAlphanumeric;
                options.Password.RequiredUniqueChars    = passwordPolicy.RequiredUniqueChars;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<WeakPasswordValidator>();

        // ── JWT Authentication ────────────────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey   = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings["Issuer"],
                    ValidAudience            = jwtSettings["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(secretKey))
                };
            });

        services.AddAuthorization();

        // ── Tenant context provider ───────────────────────────────────────────
        // IHttpContextAccessor is required by HttpContextTenantProvider
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

        // ── Subscription services ─────────────────────────────────────────────
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // ── Repository + Service DI ───────────────────────────────────────────
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // ── Subscription gate filter (registered globally via AddControllers) ─
        services.AddScoped<SubscriptionGateFilter>();

        // ── Background expiry job ─────────────────────────────────────────────
        services.AddHostedService<SubscriptionExpiryJob>();

        return services;
    }

    /// <summary>
    /// Seeds all application roles into the database if they do not already exist.
    /// Call this after <c>app.Build()</c>:
    /// <code>await app.SeedRolesAsync();</code>
    /// </summary>
    public static async Task SeedRolesAsync(this IApplicationBuilder app)
    {
        using var scope   = app.ApplicationServices.CreateScope();
        var roleManager   = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    /// <summary>
    /// Ensures SubscriptionPlanDefinition rows exist for Silver, Gold, and Diamond.
    /// Safe to run on every startup — uses INSERT … ON CONFLICT via a check-before-insert approach.
    /// </summary>
    public static async Task SeedSubscriptionPlansAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var repo        = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        var existingTypes = (await repo.GetAllPlanDefinitionsAsync())
            .Select(p => p.PlanType)
            .ToHashSet();

        var plans = new[]
        {
            new SubscriptionPlanDefinition
            {
                PlanType       = PlanType.Silver,
                DurationInDays = 180,
                Price          = 999.00m,
                Description    = "Silver — 6-month access"
            },
            new SubscriptionPlanDefinition
            {
                PlanType       = PlanType.Gold,
                DurationInDays = 365,
                Price          = 1799.00m,
                Description    = "Gold — 1-year access"
            },
            new SubscriptionPlanDefinition
            {
                PlanType       = PlanType.Diamond,
                DurationInDays = null,   // lifetime — EndDate will be null
                Price          = 4999.00m,
                Description    = "Diamond — Lifetime access, never expires"
            }
        };

        foreach (var plan in plans.Where(p => !existingTypes.Contains(p.PlanType)))
            await repo.CreatePlanDefinitionAsync(plan);
    }
}
