using LanguageLearning.Application.Interfaces;
using LanguageLearning.Domain.Entities;
using LanguageLearning.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, string? initialPassword)
    {
        await SeedCourseCategoriesAsync(context);
        const string adminEmail = "admin@languagelearning.app";
        if (await context.Users.AnyAsync(user => user.Email == adminEmail)) return;
        if (string.IsNullOrWhiteSpace(initialPassword))
            throw new InvalidOperationException("Bootstrap:AdminPassword must be configured to create the initial admin account.");

        await context.Users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = passwordHasher.HashPassword(initialPassword),
            DisplayName = "System Administrator",
            Role = UserRole.Admin,
            Timezone = "Asia/Ho_Chi_Minh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedCourseCategoriesAsync(AppDbContext context)
    {
        if (await context.CourseCategories.AnyAsync()) return;
        var defaults = new (string Kind, string Framework, string Code, string Vi, string En, string DescVi, string DescEn)[]
        {
            ("domain", "", "general", "Giao tiếp Đời sống", "General Communication", "Hội thoại thường ngày, xã hội, văn hóa và du lịch", "Everyday conversations, society, culture and travel"),
            ("domain", "", "it", "CNTT & Kỹ thuật Phần mềm", "IT & Software Engineering", "Báo cáo lỗi, quy trình agile, review mã nguồn & kiến trúc hệ thống", "Bug reports, agile sprints, code review & system architecture"),
            ("domain", "", "business", "Kinh doanh & Thương mại", "Business & Commerce", "Đàm phán hợp đồng, báo cáo tài chính & trao đổi đối tác", "Negotiations, financial reports & partner communications"),
            ("domain", "", "travel", "Du lịch & Khách sạn", "Travel & Hospitality", "Sân bay, đặt phòng khách sạn, ẩm thực & chỉ đường", "Airports, hotel reservations, dining & directions"),
            ("domain", "", "academic", "Học thuật & Nghiên cứu", "Academic & Research", "Viết luận văn, tài liệu khoa học & chuẩn bị du học", "Essay writing, scientific literature & study abroad preparation")
        };
        var categories = defaults.Select((item, index) => new CourseCategory
        {
            Kind = item.Kind, Framework = item.Framework, Code = item.Code, TitleVi = item.Vi, TitleEn = item.En,
            DescriptionVi = item.DescVi, DescriptionEn = item.DescEn, SortOrder = index
        }).ToList();
        static void AddLevels(List<CourseCategory> target, string framework, params (string Code, string Vi, string En)[] levels)
        {
            for (var index = 0; index < levels.Length; index++) target.Add(new CourseCategory
            {
                Kind = "level", Framework = framework, Code = levels[index].Code,
                TitleVi = levels[index].Vi, TitleEn = levels[index].En, SortOrder = index
            });
        }
        AddLevels(categories, "cefr", ("A1", "Mới bắt đầu", "Beginner"), ("A2", "Cơ bản", "Elementary"), ("B1", "Trung cấp", "Intermediate"), ("B2", "Nâng cao", "Upper Intermediate"), ("C1", "Thành thạo", "Advanced"), ("C2", "Bản xứ", "Mastery"));
        AddLevels(categories, "jlpt", ("N5", "Nhập môn / Căn bản", "Beginner / Foundation"), ("N4", "Sơ cấp", "Elementary"), ("N3", "Trung cấp", "Intermediate"), ("N2", "Trung cao cấp", "Upper Intermediate"), ("N1", "Thành thạo", "Advanced / Mastery"));
        AddLevels(categories, "hsk", ("HSK 1", "Sơ cấp 1", "Level 1 - Starter"), ("HSK 2", "Sơ cấp 2", "Level 2 - Elementary"), ("HSK 3", "Trung cấp 1", "Level 3 - Intermediate"), ("HSK 4", "Trung cấp 2", "Level 4 - Upper Intermediate"), ("HSK 5", "Cao cấp 1", "Level 5 - Advanced"), ("HSK 6", "Thành thạo", "Level 6 - Mastery"));
        AddLevels(categories, "topik", ("TOPIK 1", "Cấp 1 - Mới bắt đầu", "Level 1 - Starter"), ("TOPIK 2", "Cấp 2 - Sơ cấp", "Level 2 - Elementary"), ("TOPIK 3", "Cấp 3 - Trung cấp 1", "Level 3 - Intermediate"), ("TOPIK 4", "Cấp 4 - Trung cấp 2", "Level 4 - Upper Intermediate"), ("TOPIK 5", "Cấp 5 - Nâng cao", "Level 5 - Advanced"), ("TOPIK 6", "Cấp 6 - Thành thạo", "Level 6 - Mastery"));
        await context.CourseCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }
}
