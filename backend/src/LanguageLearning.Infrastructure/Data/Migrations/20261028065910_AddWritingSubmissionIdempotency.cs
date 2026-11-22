using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearning.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWritingSubmissionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientSubmissionId",
                table: "writing_submissions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_writing_submissions_UserId_ClientSubmissionId",
                table: "writing_submissions",
                columns: new[] { "UserId", "ClientSubmissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_writing_submissions_UserId_ClientSubmissionId",
                table: "writing_submissions");

            migrationBuilder.DropColumn(
                name: "ClientSubmissionId",
                table: "writing_submissions");
        }
    }
}
