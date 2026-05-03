using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class LiveInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "InterviewSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "standard"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "InterviewType", table: "InterviewSessions");
        }
    }
}
