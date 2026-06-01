using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Interfaces.Auth;
using Surgenius.Application.Interfaces.Email;
using Surgenius.Application.Interfaces.Scans;
using Surgenius.Application.Interfaces.Storage;
using Surgenius.Infrastructure.Identity;
using Surgenius.Infrastructure.Services.Cases;
using Surgenius.Infrastructure.Services.Email;
using Surgenius.Infrastructure.Services.Scans;
using Surgenius.Infrastructure.Services.Storage;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;
using Surgenius.Application.Interfaces.Analysis;
using Surgenius.Infrastructure.Services.Analysis;
using Surgenius.Application.Interfaces.Cases;
using Surgenius.Application.Interfaces.Profile;
using Surgenius.Infrastructure.Services.Profile;
using System;

namespace Surgenius.Infrastructure.Configurations;

public static class IdentityConfiguration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddHttpClient("AiApiClient", client => 
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("RiskApiClient", client =>
        {
            var baseUrl = configuration["RiskApi:BaseUrl"] ?? "https://moutel258-ilpd.hf.space/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddScoped<IProfileService, ProfileService>();

        return services;
    }
}
