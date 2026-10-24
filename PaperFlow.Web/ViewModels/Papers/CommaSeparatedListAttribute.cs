

namespace PaperFlow.Web.ViewModels.Papers
{

    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CommaSeparatedListAttribute : ValidationAttribute, IClientModelValidator
    {
        public int MaxItemLength { get; }

        public CommaSeparatedListAttribute(int maxItemLength)
        {
            MaxItemLength = maxItemLength;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
                return ValidationResult.Success;

            string[] parts = text.Split(',');
            if (parts.Any(part => string.IsNullOrWhiteSpace(part)))
                return Error(context, "Separate entries with a single comma. Empty entries are not allowed.");

            string[] names = parts.Select(part => part.Trim()).ToArray();
            if (names.Any(name => name.Length > MaxItemLength))
                return Error(context, $"Each entry must be {MaxItemLength} characters or fewer.");

            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
                return Error(context, "Remove repeated entries.");

            return ValidationResult.Success;
        }

        private static ValidationResult Error(ValidationContext context, string message) =>
            new(message, new[] { context.MemberName ?? string.Empty });

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-commalist", "Use comma-separated entries without empty or repeated values.");
            context.Attributes.TryAdd("data-val-commalist-maxlength", MaxItemLength.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

}

