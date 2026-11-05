using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VectronTablet.Models;

namespace VectronTablet.Services;

public class PosApi
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public string BaseUrl { get; set; } = Preferences.Default.Get("server_url", "");
    public string ApiKey { get; set; } = Preferences.Default.Get("api_key", "");

    public void SaveConnection()
    {
        Preferences.Default.Set("server_url", BaseUrl.Trim());
        Preferences.Default.Set("api_key", ApiKey.Trim());
    }

    public void ClearConnection()
    {
        Preferences.Default.Remove("server_url");
        Preferences.Default.Remove("api_key");
        BaseUrl = "";
        ApiKey = "";
    }

    private string Url(string path)
    {
        var b = BaseUrl.TrimEnd('/');
        var sep = path.Contains('?') ? '&' : '?';
        return $"{b}{path}{sep}key={Uri.EscapeDataString(ApiKey)}";
    }

    private static T NetError<T>(string message) where T : class, new()
    {
        var r = new T();
        switch (r)
        {
            case BootstrapInfo b: b.Ok = false; b.Error = message; break;
            case MenuInfo m: m.Ok = false; m.Error = message; break;
            case MapInfo mp: mp.Ok = false; mp.Error = message; break;
            case AlertsResult a: a.Ok = false; a.Error = message; break;
            case LoginResult l: l.Ok = false; l.Error = message; break;
            case OrderPostResult o: o.Ok = false; o.Error = message; break;
            case SimpleResult s: s.Ok = false; s.Error = message; break;
            case OrderEnvelope e: e.Ok = false; e.Error = message; break;
            case LineEditResult le: le.Ok = false; le.Error = message; break;
        }
        return r;
    }

    private async Task<T> Get<T>(string path) where T : class, new()
    {
        try
        {
            var res = await _http.GetAsync(Url(path));
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<T>(Json);
            return data ?? new T();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("VectronPOS", "GET " + path + " failed: " + ex);
            return NetError<T>(ex.Message);
        }
    }

    private async Task<T> Post<T>(string path, object body) where T : class, new()
    {
        try
        {
            var jsonBody = JsonSerializer.Serialize(body, Json);
            Android.Util.Log.Error("VectronPOS", "POST " + Url(path) + " body=" + jsonBody);
            var req = new HttpRequestMessage(HttpMethod.Post, Url(path))
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            var res = await _http.SendAsync(req);
            Android.Util.Log.Error("VectronPOS", "POST status=" + (int)res.StatusCode
                + " reqCL=" + (res.RequestMessage.Content?.Headers.ContentLength?.ToString() ?? "null")
                + " TE=" + (res.RequestMessage.Headers.TransferEncoding.ToString() == "" ? "none" : res.RequestMessage.Headers.TransferEncoding.ToString())
                + " resp=" + await res.Content.ReadAsStringAsync());
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<T>(Json);
            return data ?? new T();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("VectronPOS", "POST " + path + " failed: " + ex);
            var r = NetError<T>(ex.Message);
            if (r is OrderPostResult opr && ex is HttpRequestException or TaskCanceledException)
                opr.Network = true;
            return r;
        }
    }

    public Task<BootstrapInfo> Bootstrap() => Get<BootstrapInfo>("/api/tablet/bootstrap");
    public Task<MenuInfo> Menu() => Get<MenuInfo>("/api/tablet/menu");
    public Task<MapInfo> Map() => Get<MapInfo>("/api/tablet/map");

    /// <summary>Downloads a catalog image (server sends a 480px JPEG as base64). Null if missing/failed.</summary>
    public async Task<byte[]?> ImageBytes(string name)
    {
        try
        {
            var res = await _http.GetAsync(Url($"/api/tablet/image?name={Uri.EscapeDataString(name)}"));
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<ImageResult>(Json);
            if (data is not { Ok: true } || string.IsNullOrEmpty(data.Data)) return null;
            return Convert.FromBase64String(data.Data);
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("VectronPOS", "image " + name + " failed: " + ex);
            return null;
        }
    }
    public Task<AlertsResult> Alerts(string? since = null) =>
        Get<AlertsResult>(since == null ? "/api/tablet/alerts" : $"/api/tablet/alerts?since={Uri.EscapeDataString(since)}");

    public Task<LoginResult> Login(string name, string pin) =>
        Post<LoginResult>("/api/tablet/login", new LoginRequest { Name = name, Pin = pin });

    public Task<OrderDetailInfo?> GetOrder(string room, string table) =>
        GetOrderInner(room, table);

    private async Task<OrderDetailInfo?> GetOrderInner(string room, string table)
    {
        var res = await Get<OrderEnvelope>($"/api/tablet/order?room={Uri.EscapeDataString(room)}&table={Uri.EscapeDataString(table)}");
        if (!res.Ok) throw new PosApiException(res.Error ?? "Failed to load order.");
        return res.Order;
    }

    public Task<OrderPostResult> SendOrder(OrderRequest req) =>
        Post<OrderPostResult>("/api/tablet/order", req);

    public Task<SimpleResult> Fire(string room, string table, string op) =>
        Post<SimpleResult>("/api/tablet/fire", new TableActionRequest { Room = room, Table = table, Operator = op });

    public Task<SimpleResult> Bill(string room, string table, string op) =>
        Post<SimpleResult>("/api/tablet/bill", new TableActionRequest { Room = room, Table = table, Operator = op });

    public Task<LineEditResult> LineEdit(LineEditRequest req) =>
        Post<LineEditResult>("/api/tablet/line-edit", req);

    public Task<SimpleResult> Dismiss(DismissRequest req) =>
        Post<SimpleResult>("/api/tablet/alerts/dismiss", req);
}

public class PosApiException : Exception
{
    public PosApiException(string message) : base(message) { }
}

public class OrderEnvelope
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public OrderDetailInfo? Order { get; set; }
}

public class ImageResult
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Name { get; set; }
    public string? Mime { get; set; }
    public string? Data { get; set; }
}
