using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LanguageLearning.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261025090000_AddCourseCategories")]
public partial class AddCourseCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "course_categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false),
                Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Framework = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                TitleVi = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                TitleEn = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                DescriptionVi = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                DescriptionEn = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_course_categories", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_course_categories_Kind_Framework_Code",
            table: "course_categories",
            columns: new[] { "Kind", "Framework", "Code" },
            unique: true);

        var entries = new (string Kind, string Framework, string Code, string Vi, string En, string DescVi, string DescEn)[]
        {
            ("domain", "", "general", "Giao tiếp Đời sống", "General Communication", "Hội thoại thường ngày, xã hội, văn hóa và du lịch", "Everyday conversations, society, culture and travel"),
            ("domain", "", "it", "CNTT & Kỹ thuật Phần mềm", "IT & Software Engineering", "Báo cáo lỗi, quy trình agile, review mã nguồn & kiến trúc hệ thống", "Bug reports, agile sprints, code review & system architecture"),
            ("domain", "", "business", "Kinh doanh & Thương mại", "Business & Commerce", "Đàm phán hợp đồng, báo cáo tài chính & trao đổi đối tác", "Negotiations, financial reports & partner communications"),
            ("domain", "", "travel", "Du lịch & Khách sạn", "Travel & Hospitality", "Sân bay, đặt phòng khách sạn, ẩm thực & chỉ đường", "Airports, hotel reservations, dining & directions"),
            ("domain", "", "academic", "Học thuật & Nghiên cứu", "Academic & Research", "Viết luận văn, tài liệu khoa học & chuẩn bị du học", "Essay writing, scientific literature & study abroad preparation"),
            ("level", "cefr", "A1", "Mới bắt đầu", "Beginner", "", ""),
            ("level", "cefr", "A2", "Cơ bản", "Elementary", "", ""),
            ("level", "cefr", "B1", "Trung cấp", "Intermediate", "", ""),
            ("level", "cefr", "B2", "Nâng cao", "Upper Intermediate", "", ""),
            ("level", "cefr", "C1", "Thành thạo", "Advanced", "", ""),
            ("level", "cefr", "C2", "Bản xứ", "Mastery", "", ""),
            ("level", "jlpt", "N5", "Nhập môn / Căn bản", "Beginner / Foundation", "", ""),
            ("level", "jlpt", "N4", "Sơ cấp", "Elementary", "", ""),
            ("level", "jlpt", "N3", "Trung cấp", "Intermediate", "", ""),
            ("level", "jlpt", "N2", "Trung cao cấp", "Upper Intermediate", "", ""),
            ("level", "jlpt", "N1", "Thành thạo", "Advanced / Mastery", "", ""),
            ("level", "hsk", "HSK 1", "Sơ cấp 1", "Level 1 - Starter", "", ""),
            ("level", "hsk", "HSK 2", "Sơ cấp 2", "Level 2 - Elementary", "", ""),
            ("level", "hsk", "HSK 3", "Trung cấp 1", "Level 3 - Intermediate", "", ""),
            ("level", "hsk", "HSK 4", "Trung cấp 2", "Level 4 - Upper Intermediate", "", ""),
            ("level", "hsk", "HSK 5", "Cao cấp 1", "Level 5 - Advanced", "", ""),
            ("level", "hsk", "HSK 6", "Thành thạo", "Level 6 - Mastery", "", ""),
            ("level", "topik", "TOPIK 1", "Cấp 1 - Mới bắt đầu", "Level 1 - Starter", "", ""),
            ("level", "topik", "TOPIK 2", "Cấp 2 - Sơ cấp", "Level 2 - Elementary", "", ""),
            ("level", "topik", "TOPIK 3", "Cấp 3 - Trung cấp 1", "Level 3 - Intermediate", "", ""),
            ("level", "topik", "TOPIK 4", "Cấp 4 - Trung cấp 2", "Level 4 - Upper Intermediate", "", ""),
            ("level", "topik", "TOPIK 5", "Cấp 5 - Nâng cao", "Level 5 - Advanced", "", ""),
            ("level", "topik", "TOPIK 6", "Cấp 6 - Thành thạo", "Level 6 - Mastery", "", "")
        };
        for (var index = 0; index < entries.Length; index++)
        {
            var item = entries[index];
            migrationBuilder.InsertData("course_categories",
                new[] { "Id", "Kind", "Framework", "Code", "TitleVi", "TitleEn", "DescriptionVi", "DescriptionEn", "SortOrder", "CreatedAt" },
                new[] { "char(36)", "varchar(20)", "varchar(20)", "varchar(50)", "varchar(150)", "varchar(150)", "varchar(500)", "varchar(500)", "int", "datetime(6)" },
                new object[] { Guid.NewGuid(), item.Kind, item.Framework, item.Code, item.Vi, item.En, item.DescVi, item.DescEn, index, new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc) });
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("course_categories");
}
