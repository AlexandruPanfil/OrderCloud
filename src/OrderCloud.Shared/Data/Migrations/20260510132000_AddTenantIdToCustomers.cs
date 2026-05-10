using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderCloud.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TenantId column to Customers table as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: true);

            // Update existing customers to assign them to the first available tenant
            migrationBuilder.Sql(@"
                UPDATE Customers 
                SET TenantId = (SELECT TOP 1 Id FROM Tenants ORDER BY Id)
                WHERE TenantId IS NULL
            ");

            // Make TenantId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Create index on TenantId
            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            // Add foreign key constraint with Restrict delete behavior
            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId",
                table: "Customers");

            // Drop TenantId column
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");
        }
    }
}
