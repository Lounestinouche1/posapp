namespace LicenseGen;

public class MainForm : Form
{
    // ── Controls ──
    private readonly TextBox _customerBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _requestBox = new();
    private readonly TextBox _pcNameBox = new();
    private readonly TextBox _notesBox = new() { Width = 300 };
    private readonly NumericUpDown _yearsBox = new() { Minimum = 0, Maximum = 20, Value = 1, Width = 80 };
    private readonly NumericUpDown _monthsBox = new() { Minimum = 0, Maximum = 11, Value = 0, Width = 80 };
    private readonly NumericUpDown _daysBox = new() { Minimum = 0, Maximum = 999, Value = 0, Width = 80 };
    private readonly Label _expiryLabel = new() { AutoSize = true, ForeColor = Color.FromArgb(0, 180, 120), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly TextBox _keyBox = new() { Multiline = true, ReadOnly = true, Height = 70, Font = new Font("Consolas", 10), BackColor = Color.FromArgb(30, 38, 52), ForeColor = Color.FromArgb(0, 220, 150) };
    private readonly Button _generateBtn = new() { Text = "Generate License Key", BackColor = Color.FromArgb(0, 180, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 40, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly Button _copyBtn = new() { Text = "Copy Key", Enabled = false, Height = 32, Width = 100 };
    private readonly Button _copyReqBtn = new() { Text = "Copy", Height = 28, Width = 70 };
    private readonly ListView _historyList = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly Button _exportPubBtn = new() { Text = "Export Public Key", Height = 28, Width = 150 };
    private readonly Button _importCsvBtn = new() { Text = "Import Client CSV", Height = 28, Width = 150 };
    private readonly Button _openDbBtn = new() { Text = "Open Database Folder", Height = 28, Width = 170 };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Gray, Dock = DockStyle.Bottom, Padding = new Padding(8) };

    private DateTime _expiryUtc;

    public MainForm()
    {
        Text = "Vectron POS — License Generator";
        Size = new Size(920, 780);
        MinimumSize = new Size(740, 560);
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
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 296));  // input area
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // key output
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // history
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));   // status

        // ── Input area ──
        var inputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0) };
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputPanel.RowCount = 8;
        for (int i = 0; i < 8; i++) inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        void AddRow(int row, string label, Control control)
        {
            var lbl = new Label { Text = label, ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
            inputPanel.Controls.Add(lbl, 0, row);
            control.Dock = DockStyle.Fill;
            inputPanel.Controls.Add(control, 1, row);
        }

        AddRow(0, "Customer name:", _customerBox);
        AddRow(1, "Phone:", _phoneBox);
        AddRow(2, "PC name:", _pcNameBox);
        AddRow(3, "Request code:", _requestBox);

        // Duration row
        var durPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        durPanel.Controls.Add(_yearsBox);
        durPanel.Controls.Add(new Label { Text = "year(s)", ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Padding = new Padding(4, 6, 12, 0) });
        durPanel.Controls.Add(_monthsBox);
        durPanel.Controls.Add(new Label { Text = "month(s)", ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Padding = new Padding(4, 6, 12, 0) });
        durPanel.Controls.Add(_daysBox);
        durPanel.Controls.Add(new Label { Text = "day(s) →", ForeColor = Color.FromArgb(143, 154, 171), AutoSize = true, Padding = new Padding(4, 6, 8, 0) });
        durPanel.Controls.Add(_expiryLabel);
        inputPanel.Controls.Add(new Label { Text = "Duration:", ForeColor = Color.FromArgb(143, 154, 171), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 0, 4);
        inputPanel.Controls.Add(durPanel, 1, 4);

        // Notes row
        AddRow(5, "Notes:", _notesBox);

        // Generate button row
        var genPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _generateBtn.Width = 220;
        _generateBtn.Click += OnGenerate;
        genPanel.Controls.Add(_generateBtn);
        _copyReqBtn.Click += (_, _) => { Clipboard.SetText(_requestBox.Text); _statusLabel.Text = "Request code copied."; };
        genPanel.Controls.Add(_copyReqBtn);
        inputPanel.Controls.Add(new Label { Text = "", ForeColor = Color.Gray }, 0, 6);
        inputPanel.Controls.Add(genPanel, 1, 6);

        // Tools row: Import CSV + Open DB folder + Export public key
        var toolsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _importCsvBtn.Click += OnImportCsv;
        _openDbBtn.Click += (_, _) =>
        {
            System.IO.Directory.CreateDirectory(LicenseCore.ClientsDir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = LicenseCore.ClientsDir, UseShellExecute = true });
        };
        _exportPubBtn.Click += (_, _) =>
        {
            Clipboard.SetText(LicenseCore.PublicKeyB64);
            MessageBox.Show("Public key copied to clipboard.\n\nEmbed this in VectronPOS's License.cs (PublicKeyB64 constant).",
                "Public Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        toolsPanel.Controls.Add(_importCsvBtn);
        toolsPanel.Controls.Add(_openDbBtn);
        toolsPanel.Controls.Add(_exportPubBtn);
        inputPanel.Controls.Add(new Label { Text = "", ForeColor = Color.Gray }, 0, 7);
        inputPanel.Controls.Add(toolsPanel, 1, 7);

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
        _historyList.Columns.Add("Client", 150);
        _historyList.Columns.Add("Phone", 120);
        _historyList.Columns.Add("PC name", 95);
        _historyList.Columns.Add("Request code", 165);
        _historyList.Columns.Add("Expires", 95);
        _historyList.Columns.Add("Generated", 110);
        _historyList.Columns.Add("Key (truncated)", 160);
        _historyList.BackColor = Color.FromArgb(30, 38, 52);
        _historyList.ForeColor = Color.White;
        _historyList.FullRowSelect = true;
        historyPanel.Controls.Add(_historyList);

        mainPanel.Controls.Add(historyPanel, 0, 2);
        mainPanel.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(mainPanel);

        _yearsBox.ValueChanged += (_, _) => RefreshExpiry();
        _monthsBox.ValueChanged += (_, _) => RefreshExpiry();
        _daysBox.ValueChanged += (_, _) => RefreshExpiry();
    }

    private void RefreshExpiry()
    {
        _expiryUtc = DateTime.UtcNow.Date
            .AddYears((int)_yearsBox.Value)
            .AddMonths((int)_monthsBox.Value)
            .AddDays((int)_daysBox.Value);
        _expiryLabel.Text = $"expires {_expiryUtc:yyyy-MM-dd}";
    }

    private void OnGenerate(object? sender, EventArgs e)
    {
        try
        {
            var customer = _customerBox.Text.Trim();
            var phone = _phoneBox.Text.Trim();
            var requestCode = _requestBox.Text.Trim();
            var pcName = _pcNameBox.Text.Trim();
            var notes = _notesBox.Text.Trim();

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

            // Save to history (JSON)
            var info = new LicenseCore.LicenseInfo(customer, requestCode, _expiryUtc, DateTime.UtcNow, key);
            LicenseCore.AddToHistory(info);

            // Save to CSV database (per-client + master)
            var record = new LicenseCore.ClientRecord(
                customer, phone, requestCode, pcName, _expiryUtc, DateTime.UtcNow, key, notes);
            LicenseCore.SaveToCsv(record);

            LoadHistoryIntoList();

            _statusLabel.Text = $"License generated for \"{customer}\" — expires {_expiryUtc:yyyy-MM-dd}. Saved to CSV database ({LicenseCore.ClientsDir}).";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Generation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportCsv(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Import client CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            InitialDirectory = LicenseCore.ClientsDir,
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        var records = LicenseCore.LoadCsvFile(ofd.FileName);
        if (records.Count == 0)
        {
            MessageBox.Show("No valid records found in this file.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Show picker if multiple records
        var rec = records[records.Count - 1]; // default: most recent (last row)
        if (records.Count > 1)
        {
            using var picker = new Form
            {
                Text = $"Select record ({records.Count} found)",
                Size = new Size(700, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(18, 24, 36),
            };
            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Color.FromArgb(30, 38, 52), ForeColor = Color.White };
            lv.Columns.Add("Client Name", 150);
            lv.Columns.Add("Phone", 130);
            lv.Columns.Add("PC name", 100);
            lv.Columns.Add("Request code", 160);
            lv.Columns.Add("Expires", 100);
            foreach (var r in records)
            {
                var item = new ListViewItem(new[] { r.CustomerName, r.Phone, r.PcName, r.RequestCode, r.ExpiryUtc.ToString("yyyy-MM-dd") });
                item.Tag = r;
                lv.Items.Add(item);
            }
            var okBtn = new Button { Text = "Select", Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(0, 180, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            okBtn.Click += (_, _) => { picker.DialogResult = DialogResult.OK; };
            lv.DoubleClick += (_, _) => { picker.DialogResult = DialogResult.OK; };
            picker.Controls.Add(lv);
            picker.Controls.Add(okBtn);
            if (picker.ShowDialog() != DialogResult.OK) return;
            if (lv.SelectedItems.Count == 0) return;
            rec = (LicenseCore.ClientRecord)lv.SelectedItems[0].Tag!;
        }

        // Fill form
        _customerBox.Text = rec.CustomerName;
        _phoneBox.Text = rec.Phone;
        _requestBox.Text = rec.RequestCode;
        _pcNameBox.Text = rec.PcName;
        _notesBox.Text = rec.Notes;

        // Prefill duration with the time actually remaining on the loaded license,
        // so a lost license can be re-issued for the exact same end date.
        // Fall back to 1 year if it already expired.
        var nowUtc = DateTime.UtcNow.Date;
        var targetUtc = rec.ExpiryUtc.Date;
        int y = 0, m = 0, d = 0;
        if (targetUtc > nowUtc)
        {
            var cursor = nowUtc;
            while (y < _yearsBox.Maximum && cursor.AddYears(1) <= targetUtc) { y++; cursor = cursor.AddYears(1); }
            while (m < _monthsBox.Maximum && cursor.AddMonths(1) <= targetUtc) { m++; cursor = cursor.AddMonths(1); }
            d = (targetUtc - cursor).Days;
            if (d > _daysBox.Maximum) d = (int)_daysBox.Maximum;
        }
        else
        {
            y = 1;
        }
        _yearsBox.Value = y;
        _monthsBox.Value = m;
        _daysBox.Value = d;
        RefreshExpiry();

        var remainTxt = targetUtc > nowUtc ? $"{y}y {m}m {d}d remaining" : "license expired";
        _statusLabel.Text = $"Loaded \"{rec.CustomerName}\" ({rec.PcName}) — {remainTxt}. Adjust if needed, then Generate to re-issue.";
    }

    private void LoadHistoryIntoList()
    {
        _historyList.Items.Clear();
        var csvCache = new Dictionary<string, List<LicenseCore.ClientRecord>>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in LicenseCore.LoadHistory())
        {
            if (!csvCache.TryGetValue(h.CustomerName, out var recs))
            {
                recs = LicenseCore.LoadClientCsv(h.CustomerName);
                csvCache[h.CustomerName] = recs;
            }
            var match = recs.LastOrDefault(r => r.RequestCode == h.RequestCode);
            var truncatedKey = h.Key.Length > 30 ? h.Key.Substring(0, 30) + "…" : h.Key;
            var item = new ListViewItem(new[]
            {
                h.CustomerName,
                match?.Phone ?? "",
                match?.PcName ?? "",
                h.RequestCode,
                h.ExpiryUtc.ToString("yyyy-MM-dd"),
                h.GeneratedAt.ToString("yyyy-MM-dd HH:mm"),
                truncatedKey
            });
            item.Tag = h.Key;
            _historyList.Items.Add(item);
        }

        _historyList.DoubleClick -= HistoryList_DoubleClick;
        _historyList.DoubleClick += HistoryList_DoubleClick;
    }

    private void HistoryList_DoubleClick(object? sender, EventArgs e)
    {
        if (_historyList.SelectedItems.Count > 0 && _historyList.SelectedItems[0].Tag is string fullKey)
        {
            Clipboard.SetText(fullKey);
            _statusLabel.Text = "Full key from history copied.";
        }
    }
}
