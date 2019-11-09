using Microsoft.EntityFrameworkCore.Migrations;

namespace NLayersApp.SampleProject.Migrations
{
    public partial class simplifyingpermissiontablesrelationship : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PermissionDefinition_UserPermissions_UserPermissionsId",
                table: "PermissionDefinition");

            migrationBuilder.DropIndex(
                name: "IX_PermissionDefinition_UserPermissionsId",
                table: "PermissionDefinition");

            migrationBuilder.DropColumn(
                name: "UserPermissionsId",
                table: "PermissionDefinition");

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "UserPermissions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "UserPermissions");

            migrationBuilder.AddColumn<int>(
                name: "UserPermissionsId",
                table: "PermissionDefinition",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDefinition_UserPermissionsId",
                table: "PermissionDefinition",
                column: "UserPermissionsId");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionDefinition_UserPermissions_UserPermissionsId",
                table: "PermissionDefinition",
                column: "UserPermissionsId",
                principalTable: "UserPermissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
