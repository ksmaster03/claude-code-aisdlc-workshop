namespace OpsTrack.Api.Security;

/// <summary>
/// LAB ONLY. The caller says who they are with two headers:
///   X-User: user name (ASCII, e.g. tech.anan)
///   X-User-Role: Operator | Technician | Supervisor
/// A real system would use Windows authentication or Entra ID instead of trusting headers.
/// </summary>
public sealed class LabUser
{
    public string Name { get; init; }

    public string Role { get; init; }
}

public static class LabRoles
{
    public const string Operator = "Operator";
    public const string Technician = "Technician";
    public const string Supervisor = "Supervisor";

    public static readonly string[] All = { Operator, Technician, Supervisor };

    /// <summary>Returns the canonical role name, or null when the role is unknown.</summary>
    public static string Normalize(string role) =>
        All.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class LabUserHttpContextExtensions
{
    public static LabUser GetLabUser(this HttpContext context) =>
        context.Items.TryGetValue(typeof(LabUser), out var value) ? value as LabUser : null;
}
