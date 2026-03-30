using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class TransactionsSyncPropertiesAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsConsidered",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsConsidered",
                table: "transactions");
        }
    }
}
