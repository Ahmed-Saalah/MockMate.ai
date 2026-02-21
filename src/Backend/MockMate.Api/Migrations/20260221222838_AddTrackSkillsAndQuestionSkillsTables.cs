using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MockMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackSkillsAndQuestionSkillsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Skills_SkillId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Tracks_TrackId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_TrackId_Name",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Questions_SkillId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Questions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QuestionSkills",
                columns: table => new
                {
                    QuestionsId = table.Column<int>(type: "int", nullable: false),
                    SkillsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionSkills", x => new { x.QuestionsId, x.SkillsId });
                    table.ForeignKey(
                        name: "FK_QuestionSkills_Questions_QuestionsId",
                        column: x => x.QuestionsId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionSkills_Skills_SkillsId",
                        column: x => x.SkillsId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackSkills",
                columns: table => new
                {
                    SkillsId = table.Column<int>(type: "int", nullable: false),
                    TracksId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackSkills", x => new { x.SkillsId, x.TracksId });
                    table.ForeignKey(
                        name: "FK_TrackSkills_Skills_SkillsId",
                        column: x => x.SkillsId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackSkills_Tracks_TracksId",
                        column: x => x.TracksId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionSkills_SkillsId",
                table: "QuestionSkills",
                column: "SkillsId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackSkills_TracksId",
                table: "TrackSkills",
                column: "TracksId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionSkills");

            migrationBuilder.DropTable(
                name: "TrackSkills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_Name",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "TrackId",
                table: "Skills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkillId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_TrackId_Name",
                table: "Skills",
                columns: new[] { "TrackId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SkillId",
                table: "Questions",
                column: "SkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Skills_SkillId",
                table: "Questions",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Tracks_TrackId",
                table: "Skills",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
