using System.Collections.Generic;
using NLayersApp.DynamicPermissions.Models;

namespace NLayersApp.DynamicPermissions.Services
{
    public interface IMvcControllerDiscovery
    {
        IEnumerable<object> GetActions();
        IEnumerable<MvcControllerInfo> GetControllers();
    }
}