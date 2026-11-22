using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearning.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewObservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PreviousIntervalDays",
                table: "review_events",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasDue",
                table: "review_events",
                type: "tinyint(1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousIntervalDays",
                table: "review_events");

            migrationBuilder.DropColumn(
                name: "WasDue",
                table: "review_events");
        }
    }
}
