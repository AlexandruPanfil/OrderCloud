using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderCloud.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToPlural2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_TenantDTO_TenantId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_TenantDTO_TenantId",
                table: "CatalogItems");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceDTO_TenantDTO_TenantId",
                table: "DeviceDTO");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalUserDTO_DeviceDTO_DeviceId",
                table: "LocalUserDTO");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalUserDTO_TenantDTO_TenantId",
                table: "LocalUserDTO");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_LocalUserDTO_LocalUserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TenantDTO_TenantId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantDTO_AspNetUsers_ApplicationUserId",
                table: "TenantDTO");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TenantDTO",
                table: "TenantDTO");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalUserDTO",
                table: "LocalUserDTO");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceDTO",
                table: "DeviceDTO");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerDTO",
                table: "CustomerDTO");

            migrationBuilder.RenameTable(
                name: "TenantDTO",
                newName: "Tenants");

            migrationBuilder.RenameTable(
                name: "LocalUserDTO",
                newName: "LocalUsers");

            migrationBuilder.RenameTable(
                name: "DeviceDTO",
                newName: "Devices");

            migrationBuilder.RenameTable(
                name: "CustomerDTO",
                newName: "Customers");

            migrationBuilder.RenameIndex(
                name: "IX_TenantDTO_ApplicationUserId",
                table: "Tenants",
                newName: "IX_Tenants_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalUserDTO_TenantId",
                table: "LocalUsers",
                newName: "IX_LocalUsers_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalUserDTO_DeviceId",
                table: "LocalUsers",
                newName: "IX_LocalUsers_DeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceDTO_TenantId",
                table: "Devices",
                newName: "IX_Devices_TenantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalUsers",
                table: "LocalUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Devices",
                table: "Devices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "Id");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Tenants_TenantId",
                table: "Bills",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_Tenants_TenantId",
                table: "CatalogItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Tenants_TenantId",
                table: "Devices",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalUsers_Devices_DeviceId",
                table: "LocalUsers",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalUsers_Tenants_TenantId",
                table: "LocalUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_LocalUsers_LocalUserId",
                table: "Orders",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Tenants_TenantId",
                table: "Orders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_Bills_Tenants_TenantId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_Tenants_TenantId",
                table: "CatalogItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Tenants_TenantId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalUsers_Devices_DeviceId",
                table: "LocalUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalUsers_Tenants_TenantId",
                table: "LocalUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_LocalUsers_LocalUserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Tenants_TenantId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_AspNetUsers_ApplicationUserId",
                table: "Tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalUsers",
                table: "LocalUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Devices",
                table: "Devices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "TenantDTO");

            migrationBuilder.RenameTable(
                name: "LocalUsers",
                newName: "LocalUserDTO");

            migrationBuilder.RenameTable(
                name: "Devices",
                newName: "DeviceDTO");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "CustomerDTO");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_ApplicationUserId",
                table: "TenantDTO",
                newName: "IX_TenantDTO_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalUsers_TenantId",
                table: "LocalUserDTO",
                newName: "IX_LocalUserDTO_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_LocalUsers_DeviceId",
                table: "LocalUserDTO",
                newName: "IX_LocalUserDTO_DeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_Devices_TenantId",
                table: "DeviceDTO",
                newName: "IX_DeviceDTO_TenantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TenantDTO",
                table: "TenantDTO",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalUserDTO",
                table: "LocalUserDTO",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceDTO",
                table: "DeviceDTO",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerDTO",
                table: "CustomerDTO",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_TenantDTO_TenantId",
                table: "Bills",
                column: "TenantId",
                principalTable: "TenantDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_TenantDTO_TenantId",
                table: "CatalogItems",
                column: "TenantId",
                principalTable: "TenantDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceDTO_TenantDTO_TenantId",
                table: "DeviceDTO",
                column: "TenantId",
                principalTable: "TenantDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalUserDTO_DeviceDTO_DeviceId",
                table: "LocalUserDTO",
                column: "DeviceId",
                principalTable: "DeviceDTO",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalUserDTO_TenantDTO_TenantId",
                table: "LocalUserDTO",
                column: "TenantId",
                principalTable: "TenantDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "CustomerDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_LocalUserDTO_LocalUserId",
                table: "Orders",
                column: "LocalUserId",
                principalTable: "LocalUserDTO",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TenantDTO_TenantId",
                table: "Orders",
                column: "TenantId",
                principalTable: "TenantDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantDTO_AspNetUsers_ApplicationUserId",
                table: "TenantDTO",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
