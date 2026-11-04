namespace ExpenseLens.Domain;
public sealed class ValidationException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public static class Guard
{
    public static string Text(string? value, string field, int max = 120)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ValidationException($"{field} must contain 1 to {max} characters.");
        return value.Trim();
    }
    public static decimal Money(decimal value, string field)
    {
        if (value <= 0 || value > 1_000_000_000 || decimal.Round(value, 2) != value)
            throw new ValidationException($"{field} must be positive, at most 1000000000, with at most two decimal places.");
        return value;
    }
}
