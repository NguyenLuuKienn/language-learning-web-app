using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is Guid id && id != Guid.Empty;
}
