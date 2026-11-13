namespace OpsTrack.Api.Security;

/// <summary>Reads X-User / X-User-Role and stores a <see cref="LabUser"/> in HttpContext.Items.</summary>
public sealed class LabUserMiddleware
{
    public const string UserHeader = "X-User";
    public const string RoleHeader = "X-User-Role";

    private readonly RequestDelegate _next;

    public LabUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var name = context.Request.Headers[UserHeader].ToString().Trim();
        var role = LabRoles.Normalize(context.Request.Headers[RoleHeader].ToString());

        if (name.Length > 0 && role != null)
        {
            context.Items[typeof(LabUser)] = new LabUser { Name = name, Role = role };
        }

        await _next(context);
    }
}
