using System.Text.Json;
using VectronTablet.Models;

namespace VectronTablet.Services;

public class QueuedOrder
{
    public string PendingId { get; set; } = Guid.NewGuid().ToString("N");
    public OrderRequest Req { get; set; } = new();
    public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
}

public static class OfflineQueue
{
    private static readonly object Gate = new();
    private static List<QueuedOrder>? _items;

    private static string FilePath => Path.Combine(FileSystem.Current.AppDataDirectory, "offline-orders.json");

    private static List<QueuedOrder> Items
    {
        get
        {
            if (_items != null) return _items;
            try
            {
                _items = File.Exists(FilePath)
                    ? JsonSerializer.Deserialize<List<QueuedOrder>>(File.ReadAllText(FilePath), PosApi.Json) ?? []
                    : [];
            }
            catch { _items = []; }
            return _items;
        }
    }

    public static int Count { get { lock (Gate) return Items.Count; } }

    public static void Enqueue(OrderRequest req, string pendingId)
    {
        lock (Gate)
        {
            req.PendingId = pendingId;
            if (Items.All(q => q.PendingId != pendingId))
            {
                Items.Add(new QueuedOrder { PendingId = pendingId, Req = req });
                SaveLocked();
            }
        }
    }

    public static List<QueuedOrder> Snapshot()
    {
        lock (Gate) return Items.ToList();
    }

    public static void Remove(string pendingId)
    {
        lock (Gate)
        {
            if (Items.RemoveAll(q => q.PendingId == pendingId) > 0) SaveLocked();
        }
    }

    private static void SaveLocked()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(Items, PosApi.Json)); }
        catch { }
    }
}
