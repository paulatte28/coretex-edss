using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coretex_finalproj.Migrations
{
    /// <inheritdoc />
    public partial class AddItemizedSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cogs",
                table: "BranchSubmissions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rent",
                table: "BranchSubmissions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Salaries",
                table: "BranchSubmissions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Utilities",
                table: "BranchSubmissions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cogs",
                table: "BranchSubmissions");

            migrationBuilder.DropColumn(
                name: "Rent",
                table: "BranchSubmissions");

            migrationBuilder.DropColumn(
                name: "Salaries",
                table: "BranchSubmissions");

            migrationBuilder.DropColumn(
                name: "Utilities",
                table: "BranchSubmissions");
        }
    }
}
