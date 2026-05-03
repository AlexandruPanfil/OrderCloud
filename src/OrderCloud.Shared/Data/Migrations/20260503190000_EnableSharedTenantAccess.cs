using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderCloud.Shared.Data;

#nullable disable

namespace OrderCloud.Shared.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260503190000_EnableSharedTenantAccess")]
    public partial class EnableSharedTenantAccess : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantApplicationUsers",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantApplicationUsers", x => new { x.TenantId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_TenantApplicationUsers_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantApplicationUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantApplicationUsers_ApplicationUserId",
                table: "TenantApplicationUsers",
                column: "ApplicationUserId");

            migrationBuilder.Sql(
                """
                INSERT INTO [TenantApplicationUsers] ([TenantId], [ApplicationUserId])
                SELECT [Id], [ApplicationUserId]
                FROM [Tenants]
                WHERE [ApplicationUserId] IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantApplicationUsers");
        }
    }
}
