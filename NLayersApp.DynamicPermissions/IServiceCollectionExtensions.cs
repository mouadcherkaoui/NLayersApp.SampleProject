using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NLayersApp.DynamicPermissions.Services;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.Persistence;
using NLayersApp.SampleProject.Controllers;
using System;

namespace NLayersApp.DynamicPermissions
{
    public static class IServiceCollectionExtensions
    {
        public static void AddDynamicRolesAuthorizationServices<TContext>(this IServiceCollection services)
            where TContext: IContext
        {
            services.AddScoped<IMvcControllerDiscovery, MvcControllerDiscovery>();
            services.AddScoped<RoleController>();
            services.AddScoped<DynamicAuthorizationFilter<TContext>>();
        }

        public static void AddDynamicRolesAuthorizationFilter<TContext>(this MvcOptions options)
    where TContext : IContext
        {
            options.Filters.Add<DynamicAuthorizationFilter<TContext>>();
        }
    }
}
