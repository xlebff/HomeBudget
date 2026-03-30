using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_currencies_Id",
                table: "currencies");

            migrationBuilder.CreateIndex(
                name: "IX_currencies_Id",
                table: "currencies",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_currencies_Id",
                table: "currencies");

            migrationBuilder.CreateIndex(
                name: "IX_currencies_Id",
                table: "currencies",
                column: "Id")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }
    }
}
