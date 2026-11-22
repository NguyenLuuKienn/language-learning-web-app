using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261024090000_AddSpecialPracticeAttempts")]
public partial class AddSpecialPracticeAttempts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "special_practice_attempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                CourseId = table.Column<Guid>(type: "char(36)", nullable: false),
                ContentId = table.Column<Guid>(type: "char(36)", nullable: false),
                Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                ContentSnapshotJson = table.Column<string>(type: "longtext", nullable: false),
                AnswersJson = table.Column<string>(type: "longtext", nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                ObjectiveScore = table.Column<int>(type: "int", nullable: false),
                ObjectiveMaxScore = table.Column<int>(type: "int", nullable: false),
                SubjectiveScore = table.Column<int>(type: "int", nullable: false),
                SubjectiveMaxScore = table.Column<int>(type: "int", nullable: false),
                EvaluationJson = table.Column<string>(type: "longtext", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_special_practice_attempts", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_special_practice_attempts_UserId_CourseId_ContentId_Status",
            table: "special_practice_attempts",
            columns: new[] { "UserId", "CourseId", "ContentId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "special_practice_attempts");
}
