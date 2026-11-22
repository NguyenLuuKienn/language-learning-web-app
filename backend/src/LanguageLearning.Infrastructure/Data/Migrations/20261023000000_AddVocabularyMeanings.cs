using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261023000000_AddVocabularyMeanings")]
public partial class AddVocabularyMeanings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MeaningsJson",
            table: "vocabularies",
            type: "longtext",
            nullable: true);
        migrationBuilder.Sql("UPDATE vocabularies SET MeaningsJson = '[]' WHERE MeaningsJson IS NULL");
        migrationBuilder.AlterColumn<string>(
            name: "MeaningsJson",
            table: "vocabularies",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true);
        migrationBuilder.AddColumn<string>(
            name: "PartOfSpeech",
            table: "vocabularies",
            type: "varchar(60)",
            maxLength: 60,
            nullable: false,
            defaultValue: "");
        foreach (var column in new[] { "Phonetic", "Example", "MeaningsJson" })
        {
            migrationBuilder.AddColumn<string>(name: column, table: "cards", type: "longtext", nullable: true);
            migrationBuilder.Sql($"UPDATE cards SET {column} = '' WHERE {column} IS NULL");
            migrationBuilder.AlterColumn<string>(name: column, table: "cards", type: "longtext", nullable: false, oldClrType: typeof(string), oldType: "longtext", oldNullable: true);
        }
        migrationBuilder.AddColumn<string>(name: "PartOfSpeech", table: "cards", type: "varchar(60)", maxLength: 60, nullable: false, defaultValue: "");
        migrationBuilder.Sql("UPDATE cards SET MeaningsJson = '[]' WHERE MeaningsJson = ''");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MeaningsJson", table: "vocabularies");
        migrationBuilder.DropColumn(name: "PartOfSpeech", table: "vocabularies");
        foreach (var column in new[] { "Phonetic", "Example", "MeaningsJson", "PartOfSpeech" })
            migrationBuilder.DropColumn(name: column, table: "cards");
    }
}
