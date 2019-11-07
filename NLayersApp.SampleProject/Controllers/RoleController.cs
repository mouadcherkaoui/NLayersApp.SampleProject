using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLayersApp.Persistence.Abstractions;
using NLayersApp.SampleProject.Models;
using NLayersApp.SampleProject.Services;
using OpenIddict.Validation;

namespace NLayersApp.SampleProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = OpenIddictValidationDefaults.AuthenticationScheme)]
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
        public async Task<ActionResult> Create(RoleViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var bad = _mvcControllerDiscovery.GetControllers();
                return Ok(bad);
            }

            var user = new IdentityUser { UserName = viewModel.Name };
            var permissionDefinition = new UserPermissions() {
                UserId = user.Id
            };

            if (viewModel.SelectedControllers != null && viewModel.SelectedControllers.Any())
            {
                foreach (var controller in viewModel.SelectedControllers)
                    foreach (var action in controller.Actions)
                        action.ControllerId = controller.Id;

                var accessJson = JsonConvert.SerializeObject(viewModel.SelectedControllers);
                permissionDefinition.Permissions = accessJson;
            }

            var result = await _dbContext.Set<UserPermissions>().AddAsync(permissionDefinition);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            if (!(result.Entity.Id is 0))
                return Ok(nameof(Index));

            return BadRequest(viewModel);
        }
    }
}