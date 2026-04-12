namespace dsstats.pwa.Services;

public sealed class AppNotificationService
{
    private readonly List<AppNotification> notifications = [];
    private readonly object notificationsLock = new();

    public event Action? Changed;

    public IReadOnlyList<AppNotification> Notifications
    {
        get
        {
            lock (notificationsLock)
            {
                return notifications.ToList();
            }
        }
    }

    public AppNotification ShowSuccess(string message, int durationMs = 3500)
        => Show(message, AppNotificationLevel.Success, durationMs);

    public AppNotification ShowError(string message, int durationMs = 5000)
        => Show(message, AppNotificationLevel.Error, durationMs);

    public AppNotification ShowInfo(string message, int durationMs = 3500)
        => Show(message, AppNotificationLevel.Info, durationMs);

    public AppNotification ShowWarning(string message, int durationMs = 4000)
        => Show(message, AppNotificationLevel.Warning, durationMs);

    public void Dismiss(Guid id)
    {
        var changed = false;

        lock (notificationsLock)
        {
            changed = notifications.RemoveAll(notification => notification.Id == id) > 0;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    private AppNotification Show(string message, AppNotificationLevel level, int durationMs)
    {
        var notification = new AppNotification(Guid.NewGuid(), message, level);

        lock (notificationsLock)
        {
            notifications.Add(notification);
        }

        Changed?.Invoke();

        if (durationMs > 0)
        {
            _ = AutoDismissAsync(notification.Id, durationMs);
        }

        return notification;
    }

    private async Task AutoDismissAsync(Guid id, int durationMs)
    {
        await Task.Delay(durationMs);
        Dismiss(id);
    }
}

public sealed record AppNotification(Guid Id, string Message, AppNotificationLevel Level);

public enum AppNotificationLevel
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
