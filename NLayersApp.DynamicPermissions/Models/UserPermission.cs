using NLayersApp.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NLayersApp.DynamicPermissions.Models
{
    public class UserPermissions: IAuditable, ISoftDelete
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ApiEndpoint { get; set; }
        public string Permissions { get; set; }
    }

    public class PermissionDefinition
    {
        [Key]
        public string Id { get; set; }
        public string ResourceName { get; set; }
        public string Permission { get; set; }
    }
}
