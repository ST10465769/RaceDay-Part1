namespace RaceDay.Api.Services;

public static class SessionUserExtensions
{
    // Gets the logged-in user's id from the session.
    // The [SessionAuthorize] filter already checked the user is logged in,
    // so the id is always there by the time a controller calls this.
    public static int GetUserId(this ISession session)
    {
        return session.GetInt32(SessionKeys.UserId) ?? 0;
    }
}