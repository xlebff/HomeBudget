using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBudgetShared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_currencies_Id",
                table: "currencies",
                column: "Id")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_currencies_Id",
                table: "currencies");
        }
    }
}
