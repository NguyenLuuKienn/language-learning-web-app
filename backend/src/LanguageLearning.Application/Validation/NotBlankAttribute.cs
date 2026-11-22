using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotBlankAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string text && !string.IsNullOrWhiteSpace(text);
}
