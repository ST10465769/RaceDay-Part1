using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

// I put [SessionRole("Organiser")] on a controller to protect its pages.
// Not logged in -> redirected to Login. Wrong role -> redirected to Access Denied.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionRoleAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    public SessionRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        int? userId = session.GetInt32("UserId");
        string? role = session.GetString("Role");

        if (userId == null || role == null)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
        }
    }
}
