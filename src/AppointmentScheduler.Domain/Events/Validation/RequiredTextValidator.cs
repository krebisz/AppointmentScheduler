namespace AppointmentScheduler.Domain.Events.Validation;

internal static class RequiredTextValidator
{
    public static string ValidateAndNormalize(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
