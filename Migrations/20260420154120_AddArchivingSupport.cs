using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coretex_finalproj.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Branches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Branches");

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Branches",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
