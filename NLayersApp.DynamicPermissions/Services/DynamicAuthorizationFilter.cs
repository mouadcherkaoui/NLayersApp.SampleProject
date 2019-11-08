using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLayersApp.Persistence;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.DynamicPermissions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NLayersApp.DynamicPermissions.Services
{
    public class DynamicAuthorizationFilter<TContext> : IAsyncAuthorizationFilter
        where TContext: IContext
    {
        private readonly IContext _dbContext;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;
        public DynamicAuthorizationFilter(IContext dbContext, UserManager<IdentityUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _dbContext = dbContext;
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!IsProtectedAction(context))
                return;

            if (!IsUserAuthenticated(context))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var actionId = GetActionId(context);
            var userName = context.HttpContext.User.Identity.Name;
            var user = await _dbContext.Set<IdentityUser>().FirstOrDefaultAsync(u => u.UserName == userName);
            var userId = user?.Id;

            // var userRoles = await _userMgr.GetRolesAsync(user);
            // var roles = _roleMgr.Roles.Where(r => userRoles.Contains(r.Name)).ToList();
            var userPermissions = await (
                from userPermission in _dbContext.Set<UserPermissions>() 
                where userPermission.UserId == userId
                select userPermission
            ).ToListAsync();

            var action = actionId.Split(':')[2].ToLower();
            var controller = actionId.Split(':')[1].ToLower();
            var permissions = userPermissions.First(c => c.ApiEndpoint.ToLower() == controller).Permissions;

            if(permissions != null)
            {
                //var deserialized_permissions = JsonConvert.DeserializeObject<MvcControllerInfo[]>(permissions);
                if (permissions.Split(',').Any(p => p.ToLower() == action.ToLower())) return;
            }

            context.Result = new ForbidResult();
        }

        private bool IsProtectedAction(AuthorizationFilterContext context)
        {
            if (context.Filters.Any(item => item is IAllowAnonymousFilter))
                return false;

            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var controllerTypeInfo = controllerActionDescriptor.ControllerTypeInfo;
            var actionMethodInfo = controllerActionDescriptor.MethodInfo;

            var authorizeAttribute = controllerTypeInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            authorizeAttribute = actionMethodInfo.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute != null)
                return true;

            return false;
        }

        private bool IsUserAuthenticated(AuthorizationFilterContext context)
        {
            return context.HttpContext.User.Identity.IsAuthenticated;
        }

        private string GetActionId(AuthorizationFilterContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var area = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue;
            var controller = controllerActionDescriptor.ControllerName;
            var action = controllerActionDescriptor.ActionName;

            return $"{area}:{controller}:{action}";
        }
    }
}
