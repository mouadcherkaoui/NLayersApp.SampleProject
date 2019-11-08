using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using OpenIddict.Server;
using OpenIddict.Validation;

namespace NLayersApp.SampleProject.Controllers
{
    // [Authorize(AuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]    
    [Route("test")]
    public class TestController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public TestController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
        public IActionResult GetActions()
        {
            return Ok(_actionDescriptorCollectionProvider
                .ActionDescriptors
                .Items
                .OfType<ControllerActionDescriptor>()
                .Select(a => new
                {
                    a.DisplayName,
                    a.ControllerName,
                    a.ActionName,
                    AttributeRouteTemplate = a.AttributeRouteInfo?.Template,
                    HttpMethods = string.Join(", ", a.ActionConstraints?.OfType<HttpMethodActionConstraint>().SingleOrDefault()?.HttpMethods ?? new string[] { "any" }),
                    Parameters = a.Parameters?.Select(p => new
                    {
                        Type = p.ParameterType.Name,
                        p.Name
                    }),
                    ControllerClassName = a.ControllerTypeInfo.FullName,
                    ActionMethodName = a.MethodInfo.Name,
                    Filters = a.FilterDescriptors?.Select(f => new
                    {
                        ClassName = f.Filter.GetType().FullName,
                        f.Scope //10 = Global, 20 = Controller, 30 = Action
                }),
                    Constraints = a.ActionConstraints?.Select(c => new
                    {
                        Type = c.GetType().Name
                    }),
                    RouteValues = a.RouteValues.Select(r => new
                    {
                        r.Key,
                        r.Value
                    }),
                }));
        }
    }
}