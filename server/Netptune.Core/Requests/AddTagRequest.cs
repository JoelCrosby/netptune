using System;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddTagRequest
{
    [Required]
    [DenyPipes(ErrorMessage = "Characters are not allowed.")]
    public string Tag { get; set; } = null!;
}

public record AddTagToTaskRequest : AddTagRequest
{
    [Required]
    public string SystemId { get; set; } = null!;
}

[AttributeUsage(AttributeTargets.Property)]
public class DenyPipesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string stringValue)
        {
            return stringValue.Contains('|')
                ? new ValidationResult(ErrorMessage)
                : ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}
