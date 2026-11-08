using VectronTablet.Models;

namespace VectronTablet.Services;

public static class AppState
{
    public static PosApi Api { get; } = new();
    public static BootstrapInfo? Features;
    public static string Operator { get; private set; } = Preferences.Default.Get("operator", "");
    public static List<string> Permissions { get; private set; } = [];

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Operator) && !string.IsNullOrEmpty(Api.BaseUrl) && !string.IsNullOrEmpty(Api.ApiKey);

    // --- notification tap → open the alerts list (set by MainActivity, consumed by TablesPage) ---
    public static bool PendingAlertsNav;
    public static Action? AlertsNavRequested;

    /// <summary>Starts the background alert-polling service (Android system notifications).</summary>
    public static void StartAlertPolling()
    {
        if (!IsLoggedIn) return;
        try { AlertPollingService.Start(Android.App.Application.Context); }
        catch { }
    }

    public static void StopAlertPolling()
    {
        try { AlertPollingService.Stop(Android.App.Application.Context); }
        catch { }
    }

    public static void SetOperator(string name, List<string> permissions)
    {
        Operator = name;
        Permissions = permissions;
        Preferences.Default.Set("operator", name);
    }

    public static void Logout()
    {
        Operator = "";
        Permissions = [];
        Features = null;
        Preferences.Default.Remove("operator");
    }

    public static bool HasPermission(string permission) =>
        Permissions.Contains("All permissions") || Permissions.Contains(permission);

    private static bool _flushing;

    public static async Task<(int Sent, string? Error)> FlushQueue()
    {
        if (OfflineQueue.Count == 0 || _flushing) return (0, null);
        _flushing = true;
        var sent = 0;
        string? error = null;
        try
        {
            foreach (var q in OfflineQueue.Snapshot())
            {
                var res = await Api.SendOrder(q.Req);
                if (res.Ok)
                {
                    OfflineQueue.Remove(q.PendingId);
                    sent++;
                    continue;
                }
                if (res.Network) { error = res.Error; break; }
                OfflineQueue.Remove(q.PendingId);
                error = res.Error;
            }
        }
        finally { _flushing = false; }
        return (sent, error);
    }
}
