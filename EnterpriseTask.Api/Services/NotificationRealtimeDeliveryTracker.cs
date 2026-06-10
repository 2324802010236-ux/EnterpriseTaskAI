using System.Collections.Concurrent;

namespace EnterpriseTask.Api.Services;

public class NotificationRealtimeDeliveryTracker
{
    private readonly ConcurrentDictionary<int, DateTime> _delivered = new();

    public bool TryMarkDelivered(int notificationId)
    {
        Cleanup();
        return _delivered.TryAdd(notificationId, DateTime.UtcNow);
    }

    private void Cleanup()
    {
        if (_delivered.Count < 1000)
        {
            return;
        }

        var threshold = DateTime.UtcNow.AddMinutes(-10);
        foreach (var item in _delivered.Where(item => item.Value < threshold))
        {
            _delivered.TryRemove(item.Key, out _);
        }
    }
}
