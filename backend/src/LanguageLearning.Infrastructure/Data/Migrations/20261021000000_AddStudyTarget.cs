using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261021000000_AddStudyTarget")]
public partial class AddStudyTarget : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StudyTargetJson",
            table: "users",
            type: "longtext",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "StudyTargetJson", table: "users");
    }
}
