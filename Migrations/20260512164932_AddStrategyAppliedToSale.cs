using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coretex_finalproj.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyAppliedToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StrategyApplied",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StrategyApplied",
                table: "Sales");
        }
    }
}
