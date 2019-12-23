using NLayersApp.Persistence.Abstractions;
using System;
using System.ComponentModel.DataAnnotations;

namespace NLayersApp.Models
{
    public class TestModel: IAuditable, ISoftDelete
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
