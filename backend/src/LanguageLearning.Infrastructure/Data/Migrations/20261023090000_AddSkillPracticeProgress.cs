using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261023090000_AddSkillPracticeProgress")]
public partial class AddSkillPracticeProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "skill_practice_progresses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                CourseId = table.Column<Guid>(type: "char(36)", nullable: false),
                Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                ActivityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Attempts = table.Column<int>(type: "int", nullable: false),
                CorrectAttempts = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_skill_practice_progresses", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_skill_practice_progresses_UserId_CourseId_Kind_ActivityId",
            table: "skill_practice_progresses",
            columns: new[] { "UserId", "CourseId", "Kind", "ActivityId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "skill_practice_progresses");
}
