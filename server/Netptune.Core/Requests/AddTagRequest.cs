using System;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests
{
    public class AddTagRequest
    {
        [Required]
        [DenyPipes(ErrorMessage = "Characters are not allowed.")]
        public string Tag { get; set; }

        [Required]
        public string SystemId { get; set; }

        [Required]
        public string WorkspaceSlug { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DenyPipesAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string stringValue)
            {
                return stringValue.Contains("|")
                    ? new ValidationResult(ErrorMessage)
                    : ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage);
        }
    }
}
