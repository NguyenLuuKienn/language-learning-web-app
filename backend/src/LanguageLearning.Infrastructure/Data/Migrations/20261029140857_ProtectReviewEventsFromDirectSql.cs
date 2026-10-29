using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearning.Infrastructure.Data.Migrations
{
    public partial class ProtectReviewEventsFromDirectSql : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_review_events_prevent_update
                BEFORE UPDATE ON review_events
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Review events are append-only and cannot be updated.';
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_review_events_prevent_delete
                BEFORE DELETE ON review_events
                FOR EACH ROW
                SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Review events are append-only and cannot be deleted.';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_review_events_prevent_update;
            ");

            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_review_events_prevent_delete;
            ");
        }

    }
}
