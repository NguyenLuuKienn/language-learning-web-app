using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearning.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizAttemptIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quiz_attempts_UserId",
                table: "quiz_attempts");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientAttemptId",
                table: "quiz_attempts",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_attempts_UserId_ClientAttemptId",
                table: "quiz_attempts",
                columns: new[] { "UserId", "ClientAttemptId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quiz_attempts_UserId_ClientAttemptId",
                table: "quiz_attempts");

            migrationBuilder.DropColumn(
                name: "ClientAttemptId",
                table: "quiz_attempts");

            migrationBuilder.CreateIndex(
                name: "IX_quiz_attempts_UserId",
                table: "quiz_attempts",
                column: "UserId");
        }
    }
}
