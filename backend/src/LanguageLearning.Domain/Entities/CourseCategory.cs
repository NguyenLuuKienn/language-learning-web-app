using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.Entities;

public class CourseCategory : BaseEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionVi { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
