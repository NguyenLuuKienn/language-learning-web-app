using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261022000000_AddPlacementReviews")]
public partial class AddPlacementReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "placement_attempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                CourseId = table.Column<Guid>(type: "char(36)", nullable: false),
                QuestionsJson = table.Column<string>(type: "longtext", nullable: false),
                AnswersJson = table.Column<string>(type: "longtext", nullable: false),
                AudioFilesJson = table.Column<string>(type: "longtext", nullable: false),
                ScoresJson = table.Column<string>(type: "longtext", nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Feedback = table.Column<string>(type: "longtext", nullable: false),
                ReviewedBy = table.Column<Guid>(type: "char(36)", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_placement_attempts", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_placement_attempts_UserId_CourseId_CreatedAt",
            table: "placement_attempts",
            columns: new[] { "UserId", "CourseId", "CreatedAt" });

        migrationBuilder.CreateTable(
            name: "placement_audios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                CourseId = table.Column<Guid>(type: "char(36)", nullable: false),
                QuestionId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                FileName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_placement_audios", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_placement_audios_UserId_CourseId_QuestionId_FileName",
            table: "placement_audios",
            columns: new[] { "UserId", "CourseId", "QuestionId", "FileName" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "placement_audios");
        migrationBuilder.DropTable(name: "placement_attempts");
    }
}
