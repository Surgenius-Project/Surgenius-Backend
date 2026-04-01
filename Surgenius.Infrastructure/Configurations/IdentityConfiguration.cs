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
using Surgenius.Application.Interfaces.Cases;

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

        return services;
    }
}
