namespace LicenseGen;

public class MainForm : Form
{
    // ── Controls ──
    private readonly TextBox _customerBox = new() { PlaceholderText = "e.g. Restaurant Le Olive" };
    private readonly TextBox _requestBox = new() { PlaceholderText = "REQ-XXXX-XXXX-XXXX-XXXX" };
    private readonly NumericUpDown _yearsBox = new() { Minimum = 1, Maximum = 20, Value = 1, Width = 80 };
    private readonly NumericUpDown _monthsBox = new() { Minimum = 0, Maximum = 11, Value = 0, Width = 80 };
    private readonly Label _expiryLabel = new() { AutoSize = true, ForeColor = Color.FromArgb(0, 180, 120), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly TextBox _keyBox = new() { Multiline = true, ReadOnly = true, Height = 70, Font = new Font("Consolas", 10), BackColor = Color.FromArgb(30, 38, 52), ForeColor = Color.FromArgb(0, 220, 150) };
    private readonly Button _generateBtn = new() { Text = "Generate License Key", BackColor = Color.FromArgb(0, 180, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 40, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly Button _copyBtn = new() { Text = "Copy Key", Enabled = false, Height = 32, Width = 100 };
    private readonly Button _copyReqBtn = new() { Text = "Copy", Height = 28, Width = 70 };
    private readonly ListView _historyList = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly Button _exportPubBtn = new() { Text = "Export Public Key", Height = 28, Width = 150 };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Gray, Dock = DockStyle.Bottom, Padding = new Padding(8) };

    private DateTime _expiryUtc;

    public MainForm()
    {
        Text = "Vectron POS — License Generator";
        Size = new Size(860, 680);
        MinimumSize = new Size(700, 500);
        BackColor = Color.FromArgb(18, 24, 36);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        BuildLayout();
        RefreshExpiry();
        LoadHistoryIntoList();
        _statusLabel.Text = "Ready. Enter customer details and a request code from their PC.";
    }

    private void BuildLayout()
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4,
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));  // input area
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // key output
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // history
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // status

        // ── Input area ──
        var inputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0) };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputPanel.RowCount = 5;
        for (int i = 0; i < 5; i++) inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        void AddRow(int row, string label, Control control)
        {
            var lbl = new Label { Text = label, ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(lbl, 0, row);
            control.Dock = DockStyle.Fill;
            inputPanel.Controls.Add(control, 1, row);
        }

        AddRow(0, "Customer name:", _customerBox);
        AddRow(1, "Request code:", _requestBox);

        // Duration row: years + months side by side
        var durPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        durPanel.Controls.Add(_yearsBox);
        durPanel.Controls.Add(new Label { Text = "year(s)", ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Padding = new Padding(4, 6, 12, 0) });
        durPanel.Controls.Add(_monthsBox);
        durPanel.Controls.Add(new Label { Text = "month(s) →", ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Padding = new Padding(4, 6, 8, 0) });
        durPanel.Controls.Add(_expiryLabel);
        inputPanel.Controls.Add(new Label { Text = "Duration:", ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        inputPanel.Controls.Add(durPanel, 1, 2);

        // Generate button row
        var genPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _generateBtn.Width = 220;
        _generateBtn.Click += OnGenerate;
        genPanel.Controls.Add(_generateBtn);
        _copyReqBtn.Click += (_, _) => { Clipboard.SetText(_requestBox.Text); _statusLabel.Text = "Request code copied."; };
        genPanel.Controls.Add(_copyReqBtn);
        inputPanel.Controls.Add(new Label { Text = "", ForeColor = Color.Gray }, 0, 3);
        inputPanel.Controls.Add(genPanel, 1, 3);

        // Export public key row
        var exportPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _exportPubBtn.Click += (_, _) =>
        {
            Clipboard.SetText(LicenseCore.PublicKeyB64);
            MessageBox.Show("Public key copied to clipboard.\n\nEmbed this in VectronPOS's License.cs (PublicKeyB64 constant).",
                "Public Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        exportPanel.Controls.Add(_exportPubBtn);
        var pubInfo = new Label { Text = "  (for embedding in the POS app)", ForeColor = Color.Gray, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        exportPanel.Controls.Add(pubInfo);
        inputPanel.Controls.Add(new Label { Text = "", ForeColor = Color.Gray }, 0, 4);
        inputPanel.Controls.Add(exportPanel, 1, 4);

        mainPanel.Controls.Add(inputPanel, 0, 0);

        // ── Key output ──
        var keyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        keyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        keyPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        keyPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var keyTitle = new Label { Text = "Generated license key (send this to your customer):", ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Fill };
        keyPanel.Controls.Add(keyTitle, 0, 0);
        _keyBox.Dock = DockStyle.Fill;
        keyPanel.Controls.Add(_keyBox, 0, 1);

        var keyBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _copyBtn.Click += (_, _) => { Clipboard.SetText(_keyBox.Text); _statusLabel.Text = "License key copied to clipboard."; };
        keyBtnPanel.Controls.Add(_copyBtn);
        keyPanel.Controls.Add(keyBtnPanel, 0, 2);

        mainPanel.Controls.Add(keyPanel, 0, 1);

        // ── History ──
        var historyPanel = new Panel { Dock = DockStyle.Fill };
        var histTitle = new Label { Text = "License history (issued keys):", ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Top, Height = 24 };
        historyPanel.Controls.Add(histTitle);

        _historyList.Dock = DockStyle.Fill;
        _historyList.Columns.Add("Customer", 200);
        _historyList.Columns.Add("Request code", 200);
        _historyList.Columns.Add("Expires", 130);
        _historyList.Columns.Add("Generated", 130);
        _historyList.Columns.Add("Key (truncated)", 180);
        _historyList.BackColor = Color.FromArgb(30, 38, 52);
        _historyList.ForeColor = Color.White;
        _historyList.FullRowSelect = true;
        historyPanel.Controls.Add(_historyList);

        mainPanel.Controls.Add(historyPanel, 0, 2);
        mainPanel.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(mainPanel);

        // Duration change → update expiry label
        _yearsBox.ValueChanged += (_, _) => RefreshExpiry();
        _monthsBox.ValueChanged += (_, _) => RefreshExpiry();
    }

    private void RefreshExpiry()
    {
        _expiryUtc = DateTime.UtcNow.Date
            .AddYears((int)_yearsBox.Value)
            .AddMonths((int)_monthsBox.Value);
        _expiryLabel.Text = $"expires {_expiryUtc:yyyy-MM-dd}";
    }

    private void OnGenerate(object? sender, EventArgs e)
    {
        try
        {
            var customer = _customerBox.Text.Trim();
            var requestCode = _requestBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(customer))
            { MessageBox.Show("Enter the customer name.", "Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(requestCode))
            { MessageBox.Show("Enter the request code from the customer's PC.", "Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            byte[] fingerprint;
            try { fingerprint = LicenseCore.ParseRequestCode(requestCode); }
            catch (FormatException ex)
            { MessageBox.Show(ex.Message, "Invalid request code", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            var key = LicenseCore.GenerateKey(customer, fingerprint, _expiryUtc);

            _keyBox.Text = key;
            _copyBtn.Enabled = true;

            // Save to history
            var info = new LicenseCore.LicenseInfo(customer, requestCode, _expiryUtc, DateTime.UtcNow, key);
            LicenseCore.AddToHistory(info);
            LoadHistoryIntoList();

            _statusLabel.Text = $"License generated for \"{customer}\" — expires {_expiryUtc:yyyy-MM-dd}. Send the key to your customer.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Generation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadHistoryIntoList()
    {
        _historyList.Items.Clear();
        foreach (var h in LicenseCore.LoadHistory())
        {
            var truncatedKey = h.Key.Length > 30 ? h.Key[..30] + "…" : h.Key;
            var item = new ListViewItem(new[]
            {
                h.CustomerName, h.RequestCode,
                h.ExpiryUtc.ToString("yyyy-MM-dd"),
                h.GeneratedAt.ToString("yyyy-MM-dd HH:mm"),
                truncatedKey
            });
            item.Tag = h.Key; // full key on double-click
            _historyList.Items.Add(item);
        }

        // Double-click a history row → copy full key
        _historyList.DoubleClick += (_, _) =>
        {
            if (_historyList.SelectedItems.Count > 0 && _historyList.SelectedItems[0].Tag is string fullKey)
            {
                Clipboard.SetText(fullKey);
                _statusLabel.Text = "Full key from history copied.";
            }
        };
    }
}
