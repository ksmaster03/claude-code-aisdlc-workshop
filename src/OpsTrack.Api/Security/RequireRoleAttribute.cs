using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OpsTrack.Api.Security;

/// <summary>
/// Allows the action only for the listed lab roles.
/// No user header -> 401, user with another role -> 403.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }

    public string[] Roles { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.GetLabUser();
        if (user == null)
        {
            context.Result = Problem(
                StatusCodes.Status401Unauthorized,
                "ต้องระบุผู้ใช้",
                "ส่ง header X-User และ X-User-Role (Operator, Technician หรือ Supervisor)");
            return;
        }

        if (!Roles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = Problem(
                StatusCodes.Status403Forbidden,
                "ไม่มีสิทธิ์ทำรายการนี้",
                $"บทบาท {user.Role} ทำรายการนี้ไม่ได้ ต้องเป็น {string.Join(" หรือ ", Roles)}");
        }
    }

    private static ObjectResult Problem(int status, string title, string detail) =>
        new(new ProblemDetails { Status = status, Title = title, Detail = detail })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
}
