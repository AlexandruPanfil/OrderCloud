using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderCloud.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class AddDbUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenantDTO_AspNetUsers_ApplicationUserId",
                table: "TenantDTO");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantDTO",
                table: "TenantDTO");

            migrationBuilder.RenameTable(
                name: "TenantDTO",
                newName: "Tenants");

            migrationBuilder.RenameIndex(
                name: "IX_TenantDTO_ApplicationUserId",
                table: "Tenants",
                newName: "IX_Tenants_ApplicationUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_AspNetUsers_ApplicationUserId",
                table: "Tenants",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_AspNetUsers_ApplicationUserId",
                table: "Tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "TenantDTO");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_ApplicationUserId",
                table: "TenantDTO",
                newName: "IX_TenantDTO_ApplicationUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantDTO",
                table: "TenantDTO",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TenantDTO_AspNetUsers_ApplicationUserId",
                table: "TenantDTO",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
