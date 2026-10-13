namespace VectronPOS;

/// <summary>
/// License activation screen — reachable from Settings.
/// Shows status, request code, and activation input.
/// </summary>
public class LicenseForm : Form
{
    private readonly Color _bg = Color.FromArgb(18, 24, 36);
    private readonly Color _panel = Color.FromArgb(30, 38, 52);
    private readonly Color _mint = Color.FromArgb(0, 220, 150);
    private readonly Color _text = Color.White;
    private readonly Color _muted = Color.FromArgb(143, 154, 171);

    private Label _statusTitle = null!;
    private Label _statusDetail = null!;
    private Label _requestCodeLabel = null!;
    private TextBox _keyInput = null!;
    private Button _activateBtn = null!;
    private Label _expiryLabel = null!;

    public LicenseForm()
    {
        Text = "License — Vectron POS";
        Size = new Size(640, 520);
        MinimumSize = new Size(560, 460);
        BackColor = _bg;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BuildUI();
        RefreshStatus();
    }

    private void BuildUI()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 6,
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // title
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // status card
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // request code
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // key input
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // activate button
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // footer info

        // Title
        var title = new Label { Text = "License", ForeColor = _mint, Font = new Font("Segoe UI Semibold", 16), AutoSize = true };
        main.Controls.Add(title);

        // Status card
        var statusPanel = new Panel { Dock = DockStyle.Fill, BackColor = _panel, Padding = new Padding(16) };
        _statusTitle = new Label { ForeColor = _text, Font = new Font("Segoe UI Semibold", 13), AutoSize = true, Location = new Point(16, 12) };
        _statusDetail = new Label { ForeColor = _muted, AutoSize = true, Location = new Point(16, 44), MaximumSize = new Size(480, 0) };
        _expiryLabel = new Label { ForeColor = _muted, AutoSize = true, Location = new Point(16, 70) };
        statusPanel.Controls.AddRange([_statusTitle, _statusDetail, _expiryLabel]);
        main.Controls.Add(statusPanel);

        // Request code row
        var reqPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var reqTitle = new Label { Text = "Request code (send to your supplier):", ForeColor = _muted, AutoSize = true, Padding = new Padding(0, 6, 8, 0) };
        _requestCodeLabel = new Label { Text = License.GetRequestCode(), ForeColor = _text, Font = new Font("Consolas", 11, FontStyle.Bold), AutoSize = true, BackColor = _panel, Padding = new Padding(8, 4, 8, 4) };
        var copyReqBtn = new Button { Text = "Copy", Height = 28, Width = 60, FlatStyle = FlatStyle.Flat };
        copyReqBtn.Click += (_, _) => { Clipboard.SetText(_requestCodeLabel.Text); };
        reqPanel.Controls.AddRange([reqTitle, _requestCodeLabel, copyReqBtn]);
        main.Controls.Add(reqPanel);

        // Key input
        var keyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        keyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        keyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var keyTitle = new Label { Text = "License key (paste from your supplier):", ForeColor = _muted, Dock = DockStyle.Fill };
        _keyInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 10), BackColor = _panel, ForeColor = _mint, ScrollBars = ScrollBars.Vertical };
        keyPanel.Controls.Add(keyTitle);
        keyPanel.Controls.Add(_keyInput);
        main.Controls.Add(keyPanel);

        // Activate button
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _activateBtn = new Button { Text = "Activate", BackColor = _mint, ForeColor = _bg, FlatStyle = FlatStyle.Flat, Width = 160, Height = 36, Font = new Font("Segoe UI Semibold", 10) };
        _activateBtn.Click += OnActivate;
        btnPanel.Controls.Add(_activateBtn);
        main.Controls.Add(btnPanel);

        // Footer
        var footer = new Label
        {
            Text = "Each PC (Main + each Order PC) requires its own license.\nContact your supplier to purchase or renew.",
            ForeColor = _muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft
        };
        main.Controls.Add(footer);

        Controls.Add(main);
    }

    private void RefreshStatus()
    {
        var info = License.GetStatus();
        switch (info.Status)
        {
            case License.Status.Valid:
                _statusTitle.Text = "✅ License active";
                _statusTitle.ForeColor = _mint;
                _statusDetail.Text = $"Licensed to: {ExtractCustomerFromKey(info.Key)}";
                if (info.TimeRemaining.HasValue)
                {
                    var d = info.TimeRemaining.Value.Days;
                    _expiryLabel.Text = $"Expires in {d} day(s) — {info.ExpiryUtc:yyyy-MM-dd}";
                    if (d <= 30) _expiryLabel.ForeColor = Color.FromArgb(255, 180, 0);
                }
                break;
            case License.Status.GracePeriod:
                _statusTitle.Text = "⚠️ License expired — grace period";
                _statusTitle.ForeColor = Color.FromArgb(255, 180, 0);
                _statusDetail.Text = $"Expired on {info.ExpiryUtc:yyyy-MM-dd}. Please contact your supplier to renew.\nApp will stop accepting sales in {info.TimeRemaining?.Days ?? 0} day(s).";
                _expiryLabel.Text = "";
                break;
            case License.Status.Expired:
                _statusTitle.Text = "🔴 License expired";
                _statusTitle.ForeColor = Color.FromArgb(220, 60, 60);
                _statusDetail.Text = $"Expired on {info.ExpiryUtc:yyyy-MM-dd}. Sales are blocked until you renew.\nEnter a new license key below.";
                _expiryLabel.Text = "";
                break;
            case License.Status.Invalid:
                _statusTitle.Text = "🔴 Invalid license";
                _statusTitle.ForeColor = Color.FromArgb(220, 60, 60);
                _statusDetail.Text = "The stored license doesn't match this computer or is corrupt.\nEnter a new license key below.";
                _expiryLabel.Text = "";
                break;
            case License.Status.NotActivated:
            default:
                _statusTitle.Text = "No license activated";
                _statusTitle.ForeColor = _muted;
                _statusDetail.Text = "Send your request code to your supplier, then paste the license key below.";
                _expiryLabel.Text = "";
                break;
        }

        // Prefill with stored key
        var (_, storedKey) = License.LoadLicense();
        if (!string.IsNullOrEmpty(storedKey) && string.IsNullOrEmpty(_keyInput.Text))
            _keyInput.Text = storedKey;
    }

    private string ExtractCustomerFromKey(string? key)
    {
        // Customer name isn't stored in the key directly (only a hash).
        // Try to read from license file's metadata or show generic.
        // For now, just show "Licensed" — the generator keeps the name in its history.
        return "Licensed";
    }

    private void OnActivate(object? sender, EventArgs e)
    {
        var key = _keyInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("Paste a license key first.", "Activate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (valid, info) = License.VerifyAndParse(key);
        if (!valid || info.Status == License.Status.Invalid)
        {
            MessageBox.Show(
                "This license key is not valid for this computer.\n\n" +
                "Possible reasons:\n" +
                "• Key was issued for a different PC\n" +
                "• Key was modified or corrupted\n" +
                "• Key format is incorrect",
                "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Save and confirm
        var requestCode = License.GetRequestCode();
        License.SaveLicense(key, requestCode);

        MessageBox.Show(
            $"License activated successfully!\n\nExpires: {info.ExpiryUtc:yyyy-MM-dd}\nStatus: {info.Status}",
            "Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);

        RefreshStatus();
    }
}
