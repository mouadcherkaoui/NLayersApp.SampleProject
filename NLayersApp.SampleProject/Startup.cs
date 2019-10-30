using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLayersApp.Controllers.DependencyInjection;
using NLayersApp.Persistence;
using NLayersApp.CQRS.DependencyInjection;
using NLayersApp.Persistence.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using IdentityServer4.Services;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.EntityFramework.Entities;
using System.Threading.Tasks;
using System.Threading;
using NLayersApp.Extensions;
using NLayersApp.Authorisation.Models;
using NLayersApp.Authorisation;
using NLayersApp.Authorization.Extensions;
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
            // services.AddScoped<TDbContext>();

            services.AddDbContext<IContext, TDbContextConfigStore>(optionsAction: (s, o) =>
            {
                o.UseSqlServer("Server=.\\;Initial Catalog=nlayersapp-tests; Integrated Security=True;");
            }, ServiceLifetime.Scoped);

            services.AddAuthorizationConfig<TDbContextConfigStore, AppUser, IdentityRole, string>();
            // ConfigureAuthenticationAndAuthorisation(services);

            services.AddMediatRHandlers(resolver);

            services.AddControllers()
                .UseDynamicControllers(resolver)
                .AddControllersAsServices()
                .UseNLayersAppAccountController();
        }

        private void ConfigureAuthenticationAndAuthorisation(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "app";
                options.DefaultScheme = "app";
            })
            .AddCookie();


            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<TDbContextConfigStore>()
                .AddUserManager<UserManager<AppUser>>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddDefaultTokenProviders();
            //.AddClaimsPrincipalFactory();

            var builder = services.AddIdentityServer()
                .AddConfigurationStore<TDbContextConfigStore>()
                .AddAspNetIdentity<AppUser>()
                .AddInMemoryApiResources(AuthConfig.Apis)
                .AddInMemoryClients(AuthConfig.Clients)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder => builder.UseSqlServer("Server=.\\;Initial Catalog=nlayersapp-tests; Integrated Security=True;");
                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30; // interval in seconds
                })
                //.AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(AuthConfig.GetIdentityResources())
                .AddInMemoryApiResources(AuthConfig.GetApiResources())
                .AddInMemoryClients(AuthConfig.GetClients());

            /* We'll play with this down the road... 
                services.AddAuthentication()
                .AddGoogle("Google", options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.ClientId = "<insert here>";
                    options.ClientSecret = "<insert here>";
                });*/

            services.AddTransient<IProfileService, IdentityClaimsProfileService>();

            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader())); ;

            builder.AddDeveloperSigningCredential();
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
                        context.Response.AddApplicationError(error.Error.Message);
                        await context.Response.WriteAsync(error.Error.Message).ConfigureAwait(false);
                    }
                });
            });

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseIdentityServer().UseAuthentication();
            
            //app.UseAuthentication();

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