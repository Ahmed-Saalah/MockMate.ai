using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackAndSeniorityToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeniorityLevel",
                table: "InterviewSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrackName",
                table: "InterviewSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeniorityLevel",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "TrackName",
                table: "InterviewSessions");
        }
    }
}
