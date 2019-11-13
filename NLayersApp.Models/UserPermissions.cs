using NLayersApp.Persistence.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NLayersApp.Models
{
    public class UserPermissions : IAuditable, ISoftDelete
    {

        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ApiEndpoint { get; set; }
        public string Permissions { get; set; }
    }
}
