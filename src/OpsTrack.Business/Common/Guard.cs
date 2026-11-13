namespace OpsTrack.Business.Common;

/// <summary>
/// Input checks shared by every service. Each method throws <see cref="ValidationFailedException"/>,
/// which the API turns into HTTP 400. Use these instead of writing new if/throw blocks.
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T value, string name, string message = null)
        where T : class
    {
        if (value == null)
        {
            throw new ValidationFailedException(name, message ?? $"ต้องระบุ {name}");
        }

        return value;
    }

    public static string NotEmpty(string value, string name, string message = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationFailedException(name, message ?? $"{name} ต้องไม่เป็นค่าว่าง");
        }

        return value.Trim();
    }

    public static int Positive(int value, string name, string message = null)
    {
        if (value <= 0)
        {
            throw new ValidationFailedException(name, message ?? $"{name} ต้องมากกว่า 0");
        }

        return value;
    }
}
