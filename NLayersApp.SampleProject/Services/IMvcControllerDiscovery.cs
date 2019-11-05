using System.Collections.Generic;
using NLayersApp.SampleProject.Models;

namespace NLayersApp.SampleProject.Services
{
    public interface IMvcControllerDiscovery
    {
        IEnumerable<object> GetActions();
        IEnumerable<MvcControllerInfo> GetControllers();
    }
}