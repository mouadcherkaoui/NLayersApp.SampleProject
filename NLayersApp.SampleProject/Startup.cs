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

namespace NLayersApp.SampleProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var resolver = new TypesResolver(() => new Type[] { typeof(TestModel) });

            services.AddScoped<ITypesResolver>(s => resolver);
            services.AddScoped<TDbContext>();

            services.AddDbContext<IContext, TDbContext>(
                optionsAction: (s, o) =>
                {
                    o.UseSqlServer(
                        connectionString: "Server=.\\;Initial Catalog=nlayersapp-tests; Integrated Security=True;", 
                        sqlServerOptionsAction: b => b.MigrationsAssembly("NLayersApp.SampleProject")
                    );
                    o.UseOpenIddict();
                }, 
                contextLifetime: ServiceLifetime.Scoped
            );

            ConfigureAuthenticationAndAuthorisation(services);

            services.AddOpenIddict()
                .AddCore(options => {
                    options
                        .UseEntityFrameworkCore()
                        .UseDbContext<TDbContext>();
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
                        .AllowAuthorizationCodeFlow()
                        .AllowPasswordFlow()
                        .AllowRefreshTokenFlow()
                        .DisableHttpsRequirement() // development 
                        .AllowImplicitFlow();

                    // During development, you can disable the HTTPS requirement.

                    // Mark the "email", "profile" and "roles" scopes as supported scopes.
                    options.RegisterScopes(OpenIddictConstants.Scopes.Email,
                                            OpenIddictConstants.Scopes.Profile,
                                            OpenIddictConstants.Scopes.Roles);

                    // Register the signing and encryption credentials.
                    options.AddEphemeralSigningKey();

                    options.UseJsonWebTokens();
                    options.AcceptAnonymousClients();
                });

            services.AddMediatRHandlers(resolver);

            services.AddControllers()
                .UseDynamicControllers(resolver)
                .AddControllersAsServices();
        }

        private void ConfigureAuthenticationAndAuthorisation(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Bearer";
                options.DefaultScheme = "Bearer";
            })
            .AddCookie();


            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<TDbContext>()
                .AddUserManager<UserManager<IdentityUser>>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddDefaultTokenProviders();

            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader())); ;
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
            app.UseHttpsRedirection();
            
            app.UseAuthentication();

            // app.UseClientSideBlazorFiles<Client.Startup>();
            
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}