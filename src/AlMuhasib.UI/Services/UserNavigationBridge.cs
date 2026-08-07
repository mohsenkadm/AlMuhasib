namespace AlMuhasib.UI.Services;

/// <summary>Deep-link from Users list into profile / permissions screens.</summary>
public static class UserNavigationBridge
{
    /// <summary>Open UserActivityProfile for this user id (null = current session user).</summary>
    public static int? PendingActivityUserId { get; set; }

    /// <summary>Select this user on Permissions screen after load.</summary>
    public static int? PendingPermissionsUserId { get; set; }
}
