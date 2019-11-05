using Microsoft.EntityFrameworkCore.Migrations;
using NLayersApp.SampleProject.Models;
using System;

namespace NLayersApp.SampleProject.Migrations
{
    public partial class updatingpermissiontablesrelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("UserPermissionId", "PermissionDefinition"); 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
