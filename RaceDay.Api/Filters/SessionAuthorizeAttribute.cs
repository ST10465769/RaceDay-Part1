using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Services;

namespace RaceDay.Api.Filters;

// I put this attribute on a controller or an action to protect it.
// [SessionAuthorize] = any logged-in user
// [SessionAuthorize(UserRoles.Organiser)] = Organisers only
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    // This runs before the action. If I set context.Result, the action never runs.
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        int? userId = session.GetInt32(SessionKeys.UserId);
        string? role = session.GetString(SessionKeys.Role);

        // Nothing in the session means the user is not logged in: 401 Unauthorized
        if (userId == null || role == null)
        {
            context.Result = new JsonResult(new { message = "You must be logged in." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // Logged in, but not allowed to use this endpoint: 403 Forbidden
        if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role))
        {
            context.Result = new JsonResult(new { message = "Your role is not allowed to use this endpoint." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}