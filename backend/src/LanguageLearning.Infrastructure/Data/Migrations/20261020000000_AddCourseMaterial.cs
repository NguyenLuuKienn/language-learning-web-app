using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261020000000_AddCourseMaterial")]
public partial class AddCourseMaterial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Domain",
            table: "courses",
            type: "varchar(50)",
            nullable: false,
            defaultValue: "general");

        migrationBuilder.AddColumn<string>(
            name: "MaterialJson",
            table: "courses",
            type: "longtext",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MaterialJson", table: "courses");
        migrationBuilder.DropColumn(name: "Domain", table: "courses");
    }
}
