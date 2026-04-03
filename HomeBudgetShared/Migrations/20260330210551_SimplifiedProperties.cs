using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_currencies_CurrencyId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transaction_items_IsDeleted",
                table: "transaction_items");

            migrationBuilder.DropColumn(
                name: "LastSync",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "transaction_items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "transaction_items");

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_currencies_CurrencyId",
                table: "transactions",
                column: "CurrencyId",
                principalTable: "currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_currencies_CurrencyId",
                table: "transactions");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSync",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment",
                table: "transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "transactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "transaction_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "transaction_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_transaction_items_IsDeleted",
                table: "transaction_items",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_currencies_CurrencyId",
                table: "transactions",
                column: "CurrencyId",
                principalTable: "currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
