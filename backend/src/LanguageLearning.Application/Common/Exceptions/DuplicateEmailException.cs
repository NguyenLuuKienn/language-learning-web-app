namespace LanguageLearning.Application.Common.Exceptions;

public class DuplicateEmailException : ArgumentException
{
    public DuplicateEmailException(string email)
        : base($"An account with the email '{email}' already exists.")
    {
    }

    public DuplicateEmailException(string email, Exception innerException)
        : base($"An account with the email '{email}' already exists.", innerException)
    {
    }
}
