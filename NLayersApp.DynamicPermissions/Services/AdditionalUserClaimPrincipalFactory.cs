using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.DynamicPermissions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

//# Links
//## ASP.NET Core Roles/Policies/Claims
//- https://www.red-gate.com/simple-talk/dotnet/c-programming/policy-based-authorization-in-asp-net-core-a-deep-dive/
//- https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-2.2
//- https://docs.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-2.2
//- https://gooroo.io/GoorooTHINK/Article/17333/Custom-user-roles-and-rolebased-authorization-in-ASPNET-core/32835
//- https://gist.github.com/SteveSandersonMS/175a08dcdccb384a52ba760122cd2eda

//- (Suppress redirect on API URLs in ASP.NET Core)[https://stackoverflow.com/a/56384729/54159]
//https://adrientorris.github.io/aspnet-core/identity/extend-user-model.html

namespace NLayersApp.DynamicPermissions.Services
{
    public class AdditionalUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<IdentityUser, IdentityRole>
    {
        private readonly IContext _context;
        public AdditionalUserClaimsPrincipalFactory(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IContext context,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
            _context = context;
        }

        public async override Task<ClaimsPrincipal> CreateAsync(IdentityUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity;
            var currentPermissions = _context.Set<UserPermissions>().Where(p => p.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                ((ClaimsIdentity)principal.Identity).AddClaims(new[] { new Claim(ClaimTypes.Email, user.Email) });
            }
            if (!String.IsNullOrEmpty(user?.UserName))
            {
                var claims = currentPermissions.Select(p => new Claim(ClaimTypes.Role, p.Permissions)).ToArray();
                ((ClaimsIdentity)principal.Identity).AddClaims(claims);
            }
            //Example of a trivial claim - https://www.c-sharpcorner.com/article/claim-based-and-policy-based-authorization-with-asp-net-core-2-1/
            if (user.Email == "readonly@blazorboilerplate.com")
            {
                identity.AddClaim(new Claim("ReadOnly", "true"));
            }
            return principal;
        }
    }
}
