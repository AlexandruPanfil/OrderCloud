using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderCloud.Shared.Migrations
{
    public partial class MakeOrderCustomerOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "CustomerDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders");

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [CustomerDTO] WHERE [Id] = '00000000-0000-0000-0000-000000000000')
                BEGIN
                    INSERT INTO [CustomerDTO] ([Id], [Name], [IDNO])
                    VALUES ('00000000-0000-0000-0000-000000000000', 'Unknown customer', 0)
                END

                UPDATE [Orders]
                SET [CustomerId] = '00000000-0000-0000-0000-000000000000'
                WHERE [CustomerId] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerDTO_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "CustomerDTO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

