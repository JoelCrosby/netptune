using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Netptune.Core.Authentication;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;

namespace Netptune.Services.Authentication
{
    public static class AuthenticationServiceCollectionExtensions
    {
        public static IServiceCollection AddNeptuneAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<DbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })

                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Tokens:Issuer"],
                        ValidAudience = configuration["Tokens:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Tokens:SecurityKey"]))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (!context.Request.Headers.TryGetValue(NetptuneClaims.Workspace, out var workspace))
                            {
                                return Task.CompletedTask;
                            }

                            var claims = new [] { new Claim(NetptuneClaims.Workspace, workspace) };
                            var identity = new ClaimsIdentity(claims);

                            context.Principal?.AddIdentity(identity);

                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            var accessTokenNotEmpty = !string.IsNullOrEmpty(accessToken);
                            var isHubPath = path.StartsWithSegments("/hubs");

                            if (accessTokenNotEmpty && isHubPath)
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddTransient<INetptuneAuthService, NetptuneAuthService>();

            return services;
        }
    }
}
