/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NLayersApp.SampleProject.Helpers;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;

namespace NLayersApp.SampleProject.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly OpenIddictApplicationManager<OpenIddictApplication> _applicationMgr;
        private readonly IOpenIddictTokenManager _tokenMgr;

        public AuthorizationController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            OpenIddictApplicationManager<OpenIddictApplication> openDictMgr,
            IOpenIddictTokenManager tokenMgr)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _applicationMgr = openDictMgr;
            _tokenMgr = tokenMgr;
        }

        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIdConnectRequest();
            if (request.IsPasswordGrantType())
            {
                return await Execute_PasswordFlow(request);
            }
            else if (request.IsAuthorizationCodeGrantType())
            {
                return await Execute_ClientCredentialsGrantFlow(request);
            }
            else if (request.IsClientCredentialsGrantType())
            {
                return await Execute_ClientCredentialsGrantFlow(request);
            }
            else if (request.IsImplicitFlow())
            {
                return await Execute_ClientCredentialsGrantFlow(request);
            }
            throw new NotImplementedException("the flow terminated unexpectedly!");


            throw new NotImplementedException("The specified grant type is not implemented.");
        }

        #region Authentication flow types handling

        private async Task<IActionResult> Execute_PasswordFlow(OpenIdConnectRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIdConnectConstants.Properties.Error] = OpenIdConnectConstants.Errors.InvalidGrant,
                    [OpenIdConnectConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                });

                return Forbid(properties, OpenIddictServerDefaults.AuthenticationScheme);
            }

            // Validate the username/password parameters and ensure the account is not locked out.
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIdConnectConstants.Properties.Error] = OpenIdConnectConstants.Errors.InvalidGrant,
                    [OpenIdConnectConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                });

                return Forbid(properties, OpenIddictServerDefaults.AuthenticationScheme);
            }

            // Create a new ClaimsPrincipal containing the claims that
            // will be used to create an id_token, a token or a code.
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(principal,
                new AuthenticationProperties(),
                OpenIddictServerDefaults.AuthenticationScheme);

            // Set the list of scopes granted to the client application.
            ticket.SetScopes(new[]
            {
                    OpenIdConnectConstants.Scopes.OpenId,
                    OpenIdConnectConstants.Scopes.Email,
                    OpenIdConnectConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles
                }.Intersect(request.GetScopes()));

            ticket.SetResources("resource-server");

            foreach (var claim in ticket.Principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, ticket));
            }

            return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
        }

        private async Task<IActionResult> Execute_ClientCredentialsGrantFlow(OpenIdConnectRequest request)
        {
            var application = await _applicationMgr.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
            if (application == null)
            {
                throw new InvalidOperationException("The application details cannot be found in the database.");
            }

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(
                OpenIddictServerDefaults.AuthenticationScheme,
                OpenIdConnectConstants.Claims.Name,
                OpenIdConnectConstants.Claims.Role);

            // Use the client_id as the subject identifier.
            identity.AddClaim(OpenIdConnectConstants.Claims.Subject, application.ClientId,
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken);

            identity.AddClaim(OpenIdConnectConstants.Claims.Name, application.DisplayName,
                OpenIdConnectConstants.Destinations.AccessToken,
                OpenIdConnectConstants.Destinations.IdentityToken);

            // Create a new authentication ticket holding the user identity.
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                new AuthenticationProperties(),
                OpenIddictServerDefaults.AuthenticationScheme);

            ticket.SetResources("resource_server");

            return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
        }

        #endregion

        [HttpGet("~/connect/logout")]
        public IActionResult Logout()
        {
            var request = HttpContext.GetOpenIdConnectRequest();

            // Flow the request_id to allow OpenIddict to restore
            // the original logout request from the distributed cache.
            return Ok(new{
                RequestId = request.RequestId,
            });
        }

        private IEnumerable<string> GetDestinations(Claim claim, AuthenticationTicket ticket)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
            // whether they should be included in access tokens, in identity tokens or in both.

            switch (claim.Type)
            {
                case OpenIdConnectConstants.Claims.Name:
                    yield return OpenIdConnectConstants.Destinations.AccessToken;

                    if (ticket.HasScope(OpenIdConnectConstants.Scopes.Profile))
                        yield return OpenIdConnectConstants.Destinations.IdentityToken;

                    yield break;

                case OpenIdConnectConstants.Claims.Email:
                    yield return OpenIdConnectConstants.Destinations.AccessToken;

                    if (ticket.HasScope(OpenIdConnectConstants.Scopes.Email))
                        yield return OpenIdConnectConstants.Destinations.IdentityToken;

                    yield break;

                case OpenIdConnectConstants.Claims.Role:
                    yield return OpenIdConnectConstants.Destinations.AccessToken;

                    if (ticket.HasScope(OpenIddictConstants.Scopes.Roles))
                        yield return OpenIdConnectConstants.Destinations.IdentityToken;

                    yield break;

                case OpenIdConnectConstants.Claims.Subject:
                    yield return OpenIdConnectConstants.Destinations.IdentityToken;
                    yield break;
                // Never include the security stamp in the access and identity tokens, as it's a secret value.
                case "AspNet.Identity.SecurityStamp": yield break;

                default:
                    yield return OpenIdConnectConstants.Destinations.AccessToken;
                    yield break;
            }
        }
    }
}