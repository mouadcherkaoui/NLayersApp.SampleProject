using AspNet.Security.OAuth.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.SampleProject.Controllers;
using OpenIddict.Abstractions;
using System;

namespace NLayersApp.Authorization
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureAuthenticationAndAuthorisation<TUser, TRole, TKey, TContext>(this IServiceCollection services)
            where TUser: IdentityUser<TKey>
            where TRole: IdentityRole<TKey>
            where TKey: IEquatable<TKey>
            where TContext: DbContext, IContext
        {
            services.AddIdentity<TUser, TRole>()
                .AddEntityFrameworkStores<TContext>()
                .AddUserManager<UserManager<TUser>>()
                .AddRoleManager<RoleManager<TRole>>()
                .AddDefaultTokenProviders();

            services.AddOpenIddict()
                .AddCore(options => {
                    options
                        .UseEntityFrameworkCore()
                        .UseDbContext<TContext>();
                })
                .AddServer(options => {
                    // Enable the authorization, logout, token and userinfo endpoints.
                    options
                        .EnableTokenEndpoint("/connect/token")
                        .EnableAuthorizationEndpoint("/connect/authorize")
                        .EnableLogoutEndpoint("/connect/logout")
                        .EnableUserinfoEndpoint("/connect/userinfo");

                    options
                        .AllowClientCredentialsFlow()
                        .AllowPasswordFlow()
                        .AllowRefreshTokenFlow()
                        .DisableHttpsRequirement();

                    // During development, you can disable the HTTPS requirement.

                    // Mark the "email", "profile" and "roles" scopes as supported scopes.
                    options.RegisterScopes(OpenIddictConstants.Scopes.Email,
                                OpenIddictConstants.Scopes.Profile,
                                OpenIddictConstants.Scopes.Roles);

                    // Register the signing and encryption credentials.
                    options.AddEphemeralSigningKey();

                    options.UseJsonWebTokens();
                });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OAuthValidationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = OAuthValidationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Bearer";
                options.DefaultScheme = "Bearer";
            })
            .AddOAuthValidation()
            .AddCookie();
            services.AddScoped<AccountController>();
            services.AddScoped<AuthorizationController>();

            services.AddControllers().AddControllersAsServices();
            
            return services;
        }
    }
}
