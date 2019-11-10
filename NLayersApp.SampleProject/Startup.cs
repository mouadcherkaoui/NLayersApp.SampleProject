using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLayersApp.Controllers.DependencyInjection;
using NLayersApp.Persistence;
using NLayersApp.CQRS.DependencyInjection;
using NLayersApp.Persistence.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using OpenIddict.Abstractions;
using System.Reflection;
using OpenIddict.Core;
using System.Threading.Tasks;
using OpenIddict.EntityFrameworkCore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NLayersApp.Controllers;
using NLayersApp.Authorization;
using NLayersApp.DynamicPermissions.Services;
using NLayersApp.DynamicPermissions.Models;
using NLayersApp.DynamicPermissions;

namespace NLayersApp.SampleProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var connectionString = Configuration["SQL_CONN_STR"];

            services.AddScoped<IMvcControllerDiscovery, MvcControllerDiscovery>();

            var resolver = new TypesResolver(() => new Type[] { typeof(TestModel), typeof(UserPermissions), typeof(PermissionDefinition) });

            services.AddScoped<ITypesResolver>(s => resolver);
            services.AddScoped<TDbContext>();

            services.AddDbContext<IContext, TDbContext>(
                optionsAction: (s, o) =>
                {
                    o.UseSqlServer(
                        connectionString: connectionString,
                        sqlServerOptionsAction: b =>
                        {
                            b.MigrationsAssembly("NLayersApp.SampleProject");
                            b.EnableRetryOnFailure(3);
                        }                        
                    );
                    o.UseOpenIddict();
                    // InitializeAsync(s).Wait();
                    // Task.Run(async () => await InitializeAsync(s).ConfigureAwait(true));
                }, 
                contextLifetime: ServiceLifetime.Scoped
            );


            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader())); ;

            services.AddMediatRHandlers(resolver);
            //config => {
            //config.Filters.Add(typeof(DynamicAuthorizationFilter<TDbContext>));

            services.AddDynamicRolesAuthorizationServices<TDbContext>();

            services.AddControllers(c => c.AddDynamicRolesAuthorizationFilter<TDbContext>())
                    .UseDynamicControllers(resolver)
                    .AddControllersAsServices();

            services.ConfigureAuthenticationAndAuthorisation<IdentityUser, IdentityRole, string, TDbContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseExceptionHandler(builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                    var error = context.Features.Get<IExceptionHandlerFeature>();
                    if (error != null)
                    {
                        // context.Response.AddApplicationError(error.Error.Message);
                        await context.Response.WriteAsync(error.Error.Message).ConfigureAwait(false);
                    }
                });
            });

            app.UseCors("AllowAll");
            
            app.UseRouting();
            
            app.UseHttpsRedirection();

            
            app.UseAuthentication().UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private async Task InitializeAsync(IServiceProvider services)
        {
            // Create a new service scope to ensure the database context is correctly disposed when this methods returns.
            using var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await context.Database.EnsureCreatedAsync();

            var manager = scope.ServiceProvider.GetRequiredService<OpenIddictApplicationManager<OpenIddictApplication>>();

            if (await manager.FindByClientIdAsync("mvc") == null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = "mvc",
                    ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
                    DisplayName = "MVC client application",
                    PostLogoutRedirectUris = { new Uri("http://localhost:53507/signout-callback-oidc") },
                    RedirectUris = { new Uri("http://localhost:53507/signin-oidc") },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.Endpoints.Logout,
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Roles
                    }
                };

                await manager.CreateAsync(descriptor);
            }

            // To test this sample with Postman, use the following settings:
            //
            // * Authorization URL: http://localhost:54540/connect/authorize
            // * Access token URL: http://localhost:54540/connect/token
            // * Client ID: postman
            // * Client secret: [blank] (not used with public clients)
            // * Scope: openid email profile roles
            // * Grant type: authorization code
            // * Request access token locally: yes
            if (await manager.FindByClientIdAsync("postman") == null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = "postman",
                    DisplayName = "Postman",
                    ClientSecret = "simple_secret",
                    RedirectUris = { new Uri("https://app.getpostman.com/oauth2/callback") },
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                        OpenIddictConstants.Permissions.GrantTypes.Password,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile,
                        OpenIddictConstants.Permissions.Scopes.Roles
                    }
                };

                await manager.CreateAsync(descriptor);
            }
        }

    }
}