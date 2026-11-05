namespace VectronTablet.Services;

/// <summary>
/// In-memory cache of product catalog images downloaded from the POS server.
/// The menu is re-filtered on every category tap, so images are fetched once per
/// session and then served from RAM (each is a ~480px JPEG, a few dozen KB).
/// </summary>
public static class ProductImages
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, byte[]> _bytes = new();
    private static readonly HashSet<string> _inFlight = new();

    /// <summary>Cached image bytes, or null when not downloaded yet / not found.</summary>
    public static ImageSource? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        lock (_lock)
        {
            if (!_bytes.TryGetValue(name, out var b) || b.Length == 0) return null;
            var data = b;
            return ImageSource.FromStream(() => new MemoryStream(data));
        }
    }

    /// <summary>True while (or after) this image was requested and is not cached yet.</summary>
    public static bool TryStartFetch(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        lock (_lock)
        {
            if (_bytes.ContainsKey(name) || !_inFlight.Add(name)) return false;
        }
        return true;
    }

    /// <summary>Fetches an image in the background and returns it (null on failure).</summary>
    public static async Task<ImageSource?> FetchAsync(string name)
    {
        try
        {
            var bytes = await AppState.Api.ImageBytes(name);
            lock (_lock)
            {
                _inFlight.Remove(name);
                if (bytes is { Length: > 0 }) _bytes[name] = bytes;
            }
            return Get(name);
        }
        catch
        {
            lock (_lock) { _inFlight.Remove(name); }
            return null;
        }
    }
}
