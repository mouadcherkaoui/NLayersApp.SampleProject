using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NLayersApp.SampleProject.Controllers
{

    public class OpenIdActions: ControllerBase
    {
        [HttpPost("connect/authorize")]
        public async Task<IActionResult> Authorize(OpenIdConnectRequest request)
        {
            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            var claims = new List<Claim>();
            claims.Add(new Claim(OpenIdConnectConstants.Claims.Username, request.Username));
            var identity = new ClaimsIdentity(claims, "OpenIddict");
            var principal = new ClaimsPrincipal(identity);

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(principal,
                new AuthenticationProperties(),
                OpenIdConnectServerDefaults.AuthenticationScheme);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
        }

        [HttpGet("connect/userinfo"), Authorize(AuthenticationSchemes = OpenIddictServerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UserInfo()
        {
            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return new OkObjectResult(User);// (ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
        }
        [HttpPost("connect/token")]
        public async Task<IActionResult> Token(OpenIdConnectRequest request)
        {
            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            var claims = new List<Claim>();
            claims.Add(new Claim(OpenIdConnectConstants.Claims.Subject, "user-0001"));
            var identity = new ClaimsIdentity(claims, "OpenIddict");
            var principal = new ClaimsPrincipal(identity);

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(principal,
                new AuthenticationProperties(),
                OpenIdConnectServerDefaults.AuthenticationScheme);

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
        }
    }
}
