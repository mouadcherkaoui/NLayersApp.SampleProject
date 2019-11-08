using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using OpenIddict.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NLayersApp.SampleProject
{
    [Route("api/test")]
    public class TestController: Controller
    {
        UserManager<IdentityUser> userMgr;
        OpenIddictApplicationManager<OpenIddictApplication> appMgr;
        OpenIddictTokenManager<OpenIddictToken> tokenMgr;
        public TestController(UserManager<IdentityUser> userMgr, 
            OpenIddictApplicationManager<OpenIddictApplication> appMgr,
            OpenIddictTokenManager<OpenIddictToken> tokenMgr)
        {
            this.userMgr = userMgr;
            this.appMgr = appMgr;
            this.tokenMgr = tokenMgr;            
        }

        public IActionResult Get()
        {
            return Ok(Thread.CurrentPrincipal?.Identity);
        }

        [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            // var request = Request.HttpContext.GetOpenIdConnectRequest();
            
            var user = await userMgr.GetUserAsync(Request.HttpContext.User);
            ClaimsIdentity identity = (ClaimsIdentity)Request.HttpContext.User.Identity;
            
            var app = await appMgr.FindByClientIdAsync(identity.Name.ToLower());

            var resultToReturn = new { identity.Name, identity.AuthenticationType, app.Permissions };
            return Ok(resultToReturn);
        }
    }
}
