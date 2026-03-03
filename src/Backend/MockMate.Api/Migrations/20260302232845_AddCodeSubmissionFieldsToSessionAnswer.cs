using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeSubmissionFieldsToSessionAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "SessionAnswers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "SessionAnswers",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SessionAnswers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SessionAnswers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SessionAnswers");
        }
    }
}
