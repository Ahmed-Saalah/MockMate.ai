using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedSkillName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_Name",
                table: "Skills");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Skills",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_NormalizedName",
                table: "Skills",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Skills_NormalizedName",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Skills");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);
        }
    }
}
