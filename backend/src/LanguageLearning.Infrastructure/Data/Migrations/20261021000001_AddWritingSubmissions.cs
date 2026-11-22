using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261021000001_AddWritingSubmissions")]
public partial class AddWritingSubmissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "writing_submissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                PromptId = table.Column<string>(type: "varchar(200)", nullable: false),
                Text = table.Column<string>(type: "longtext", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_writing_submissions", x => x.Id);
                table.ForeignKey("FK_writing_submissions_users_UserId", x => x.UserId, "users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_writing_submissions_UserId_PromptId_CreatedAt",
            table: "writing_submissions",
            columns: new[] { "UserId", "PromptId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "writing_submissions");
    }
}
