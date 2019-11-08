using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.DynamicPermissions.Models;
using NLayersApp.DynamicPermissions.Services;

namespace NLayersApp.SampleProject.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]
    public class RoleController : Controller
    {
        private readonly IMvcControllerDiscovery _mvcControllerDiscovery;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IContext _dbContext;
        public RoleController(IMvcControllerDiscovery mvcControllerDiscovery, 
            UserManager<IdentityUser> userManager, 
            IContext dbContext)
        {
            _mvcControllerDiscovery = mvcControllerDiscovery;
            _userManager = userManager;
            _dbContext = dbContext;
        }
        
        [HttpGet]
        public ActionResult Create()
        {
            var resultToReturn = _mvcControllerDiscovery.GetControllers();

            return Ok(resultToReturn);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult> Create(RoleViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var bad = _mvcControllerDiscovery.GetControllers();
                return Ok(bad);
            }

            var user = await _userManager.GetUserAsync(User);
            var permissionDefinition = new UserPermissions() {
                UserId = user.Id
            };

            if (viewModel.SelectedControllers != null && viewModel.SelectedControllers.Any())
            {
                foreach (var controller in viewModel.SelectedControllers)
                    foreach (var action in controller.Actions)
                        action.ControllerId = controller.Id;

                var resultToSerialize = viewModel.SelectedControllers.ToList()
                    .Select(c => new UserPermissions()
                    {
                        UserId = user.Id,
                        ApiEndpoint = c.Name,
                        Permissions = $"{c.Actions.Aggregate("", (ac, a) => $"{a.Name},{ac}")}"
                    });
                var accessJson = JsonConvert.SerializeObject(viewModel.SelectedControllers);
                permissionDefinition.Permissions = resultToSerialize.Aggregate("", (ac, p) => $"{ac}{p.Permissions},").SkipLast(1).ToString();
                await _dbContext.Set<UserPermissions>().AddRangeAsync(resultToSerialize);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                
                return Ok();
            }

            return BadRequest(viewModel);
        }
    }
}