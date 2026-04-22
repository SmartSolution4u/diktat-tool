using System.Drawing;
using System.Windows.Forms;

namespace DiktatTool;

public class SetupDialog : Form
{
    private TextBox _apiKeyBox = null!;
    private ComboBox _hotkeyBox = null!;

    private static readonly Color BG     = Color.FromArgb(30, 30, 46);
    private static readonly Color FG     = Color.FromArgb(205, 214, 244);
    private static readonly Color ACCENT = Color.FromArgb(137, 180, 250);
    private static readonly Color MUTED  = Color.FromArgb(108, 112, 134);
    private static readonly Color INPUT  = Color.FromArgb(49, 50, 68);

    public SetupDialog()
    {
        Text = "Diktat-Tool — Einrichtung";
        Size = new Size(490, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BG;
        ForeColor = FG;
        Font = new Font("Segoe UI", 10f);

        var panel = new Panel { Dock = DockStyle.Fill, BackColor = BG, Padding = new Padding(30) };
        Controls.Add(panel);

        int y = 20;

        Add(panel, new Label
        {
            Text = "Diktat-Tool",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ACCENT,
            AutoSize = true,
            Location = new Point(30, y)
        });
        y += 40;

        Add(panel, new Label
        {
            Text = "Sprechen statt tippen — powered by Groq Whisper",
            ForeColor = MUTED,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(30, y)
        });
        y += 28;

        Separator(panel, y); y += 18;

        Add(panel, new Label
        {
            Text = "Groq API Key",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(30, y)
        });
        y += 22;

        Add(panel, new Label
        {
            Text = "Kostenlos unter console.groq.com → API Keys → Create API Key",
            ForeColor = MUTED,
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Location = new Point(30, y)
        });
        y += 22;

        _apiKeyBox = new TextBox
        {
            Location = new Point(30, y),
            Width = 420,
            BackColor = INPUT,
            ForeColor = FG,
            Font = new Font("Consolas", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '•'
        };
        panel.Controls.Add(_apiKeyBox);
        y += 32;

        var showKey = new CheckBox
        {
            Text = "API Key anzeigen",
            Location = new Point(30, y),
            AutoSize = true,
            ForeColor = MUTED,
            BackColor = BG
        };
        showKey.CheckedChanged += (s, e) =>
            _apiKeyBox.PasswordChar = showKey.Checked ? '\0' : '•';
        panel.Controls.Add(showKey);
        y += 28;

        var link = new LinkLabel
        {
            Text = "→ Groq Console öffnen (kostenlos registrieren)",
            Location = new Point(28, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            BackColor = BG
        };
        link.LinkColor = ACCENT;
        link.ActiveLinkColor = Color.FromArgb(116, 199, 236);
        link.LinkClicked += (s, e) =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://console.groq.com/keys",
                UseShellExecute = true
            });
        panel.Controls.Add(link);
        y += 36;

        Separator(panel, y); y += 18;

        Add(panel, new Label
        {
            Text = "Hotkey:",
            ForeColor = MUTED,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(30, y)
        });

        _hotkeyBox = new ComboBox
        {
            Location = new Point(95, y - 3),
            Width = 80,
            BackColor = INPUT,
            ForeColor = FG,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat
        };
        _hotkeyBox.Items.AddRange(new[] { "F8", "F9", "F10", "F11", "F12" });
        _hotkeyBox.SelectedItem = "F9";
        panel.Controls.Add(_hotkeyBox);
        y += 42;

        var btn = new Button
        {
            Text = "  Speichern & Starten  ",
            Location = new Point(30, y),
            AutoSize = true,
            BackColor = ACCENT,
            ForeColor = BG,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += OnSave;
        panel.Controls.Add(btn);
    }

    private static void Add(Panel p, Control c) => p.Controls.Add(c);

    private static void Separator(Panel p, int y) =>
        p.Controls.Add(new Panel { Height = 1, Width = 420, BackColor = Color.FromArgb(69, 71, 90), Location = new Point(30, y) });

    private void OnSave(object? sender, EventArgs e)
    {
        var key = _apiKeyBox.Text.Trim();
        if (!key.StartsWith("gsk_"))
        {
            MessageBox.Show(
                "Bitte einen gültigen Groq API Key eingeben.\n(beginnt mit gsk_)",
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cfg = Config.Load();
        cfg.GroqApiKey = key;
        cfg.Hotkey = _hotkeyBox.SelectedItem?.ToString() ?? "F9";
        cfg.Save();

        DialogResult = DialogResult.OK;
        Close();
    }
}
