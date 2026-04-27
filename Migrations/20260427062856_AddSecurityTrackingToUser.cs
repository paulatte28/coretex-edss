using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace coretex_finalproj.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityTrackingToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastLoginIP",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginLocation",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginIP",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginLocation",
                table: "AspNetUsers");
        }
    }
}
