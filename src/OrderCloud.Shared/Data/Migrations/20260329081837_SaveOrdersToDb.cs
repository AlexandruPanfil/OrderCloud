using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderCloud.Shared.Migrations
{
    /// <inheritdoc />
    public partial class SaveOrdersToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDTO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDNO = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDTO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceDTO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveTill = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceDTO_TenantDTO_TenantId",
                        column: x => x.TenantId,
                        principalTable: "TenantDTO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalUserDTO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PinCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalUserDTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalUserDTO_DeviceDTO_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "DeviceDTO",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LocalUserDTO_TenantDTO_TenantId",
                        column: x => x.TenantId,
                        principalTable: "TenantDTO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_CustomerDTO_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerDTO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_LocalUserDTO_LocalUserId",
                        column: x => x.LocalUserId,
                        principalTable: "LocalUserDTO",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_TenantDTO_TenantId",
                        column: x => x.TenantId,
                        principalTable: "TenantDTO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemDTO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TVA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemDTO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemDTO_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceDTO_TenantId",
                table: "DeviceDTO",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemDTO_OrderId",
                table: "ItemDTO",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalUserDTO_DeviceId",
                table: "LocalUserDTO",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalUserDTO_TenantId",
                table: "LocalUserDTO",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LocalUserId",
                table: "Orders",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId",
                table: "Orders",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemDTO");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "CustomerDTO");

            migrationBuilder.DropTable(
                name: "LocalUserDTO");

            migrationBuilder.DropTable(
                name: "DeviceDTO");
        }
    }
}

