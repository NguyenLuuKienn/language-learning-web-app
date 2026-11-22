namespace LanguageLearning.Application.DTOs.Courses;

public class CourseDetailResponse : CourseResponse
{
    public List<UnitResponse> Units { get; set; } = new();
}
