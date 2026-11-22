namespace LanguageLearning.Application.Common;

public static class ErrorCodes
{
    public const string Validation = "VALIDATION_ERROR";
    public const string AuthEmailInUse = "AUTH_EMAIL_IN_USE";
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthAccountDeactivated = "AUTH_ACCOUNT_DEACTIVATED";
    public const string AuthInvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
    public const string AuthIncorrectPassword = "AUTH_INCORRECT_PASSWORD";
    public const string CourseNotFound = "COURSE_NOT_FOUND";
    public const string CourseTitleInvalid = "COURSE_TITLE_INVALID";
}
