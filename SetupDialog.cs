using System.Drawing;
using System.Windows.Forms;

namespace DiktatTool;

public class SetupDialog : Form
{
    private static readonly Color BG     = Color.FromArgb(30, 30, 46);
    private static readonly Color FG     = Color.FromArgb(205, 214, 244);
    private static readonly Color ACCENT = Color.FromArgb(137, 180, 250);
    private static readonly Color MUTED  = Color.FromArgb(108, 112, 134);
    private static readonly Color INPUT  = Color.FromArgb(49, 50, 68);
    private static readonly Color SEP    = Color.FromArgb(69, 71, 90);

    private TextBox   _groqKey   = null!;
    private TextBox   _orKey     = null!;
    private ComboBox  _model     = null!;
    private ComboBox  _hotkey    = null!;

    public SetupDialog()
    {
        Text            = "Diktat-Tool — Einrichtung";
        Size            = new Size(520, 560);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = BG;
        ForeColor       = FG;
        Font            = new Font("Segoe UI", 10f);

        var tabs = new TabControl
        {
            Dock      = DockStyle.Fill,
            Padding   = new Point(12, 6),
            BackColor = BG
        };
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.DrawItem += DrawTab;
        Controls.Add(tabs);

        tabs.TabPages.Add(BuildApiTab());
        tabs.TabPages.Add(BuildModelTab());
        tabs.TabPages.Add(BuildHotkeyTab());

        // Save button outside tabs
        var btnPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 60,
            BackColor = BG,
            Padding   = new Padding(20, 10, 20, 10)
        };
        var btn = new Button
        {
            Text      = "  Speichern & Starten  ",
            AutoSize  = true,
            BackColor = ACCENT,
            ForeColor = BG,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            Dock      = DockStyle.Left
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += OnSave;
        btnPanel.Controls.Add(btn);
        Controls.Add(btnPanel);
    }

    private static void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tc) return;
        var page   = tc.TabPages[e.Index];
        var bounds = e.Bounds;
        bool sel   = e.Index == tc.SelectedIndex;

        using var bg   = new SolidBrush(sel ? Color.FromArgb(49, 50, 68) : BG);
        using var fg   = new SolidBrush(sel ? ACCENT : MUTED);
        using var font = new Font("Segoe UI", 9f, sel ? FontStyle.Bold : FontStyle.Regular);

        e.Graphics.FillRectangle(bg, bounds);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(page.Text, font, fg, bounds, sf);
    }

    private TabPage BuildApiTab()
    {
        var page = MakePage("API Keys");
        int y = 20;

        H1(page, "Groq API Key", ref y);
        Hint(page, "Kostenlos: console.groq.com → API Keys → Create API Key", ref y);
        _groqKey = Input(page, ref y, password: true);
        Link(page, "→ console.groq.com/keys öffnen", "https://console.groq.com/keys", ref y);
        ShowToggle(page, _groqKey, ref y);

        Separator(page, y); y += 20;

        H1(page, "OpenRouter API Key  (für KI-Umformulierung)", ref y);
        Hint(page, "Optional — für Förmlich/Locker/Kürzer etc. Kostenlos: openrouter.ai", ref y);
        _orKey = Input(page, ref y, password: true);
        Link(page, "→ openrouter.ai/keys öffnen", "https://openrouter.ai/keys", ref y);
        ShowToggle(page, _orKey, ref y);

        return page;
    }

    private TabPage BuildModelTab()
    {
        var page = MakePage("Modell");
        int y = 20;

        H1(page, "KI-Modell für Umformulierung", ref y);
        Hint(page, "Wird nur bei Modi wie Förmlich/Locker/Kürzer verwendet", ref y);

        _model = new ComboBox
        {
            Location      = new Point(20, y),
            Width         = 460,
            BackColor     = INPUT,
            ForeColor     = FG,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat
        };
        _model.Items.AddRange(new[]
        {
            "mistralai/mistral-small-3.1-24b-instruct  (schnell, günstig)",
            "mistralai/mistral-large-latest             (hohe Qualität)",
            "openai/gpt-4o-mini                         (OpenAI, günstig)",
            "openai/gpt-4o                              (OpenAI, beste Qualität)",
            "anthropic/claude-haiku-4-5-20251001        (Claude, sehr schnell)",
            "anthropic/claude-sonnet-4-5                (Claude, sehr gut)",
            "meta-llama/llama-3.3-70b-instruct          (Open Source, kostenlos)",
        });
        _model.SelectedIndex = 0;
        page.Controls.Add(_model);
        y += 35;

        Separator(page, y); y += 20;

        H1(page, "Kosten-Übersicht (ca.)", ref y);
        var cost = new Label
        {
            Text = "Mistral Small:   ~0,10 € / 1 Mio. Tokens  ≈ 0,0001 € pro Diktat\n" +
                   "GPT-4o mini:     ~0,15 € / 1 Mio. Tokens  ≈ 0,0001 € pro Diktat\n" +
                   "Claude Haiku:    ~0,25 € / 1 Mio. Tokens  ≈ 0,0002 € pro Diktat\n\n" +
                   "Bei 50 Diktaten/Tag mit Umformulierung: ~0,15 €/Monat",
            Location  = new Point(20, y),
            Size      = new Size(460, 100),
            BackColor = Color.FromArgb(24, 24, 37),
            ForeColor = Color.FromArgb(166, 227, 161),
            Font      = new Font("Consolas", 8.5f),
            Padding   = new Padding(8)
        };
        page.Controls.Add(cost);

        return page;
    }

    private TabPage BuildHotkeyTab()
    {
        var page = MakePage("Hotkey");
        int y = 20;

        H1(page, "Aufnahme-Hotkey", ref y);
        Hint(page, "Einmal drücken = Start, nochmal = Stop + Text einfügen", ref y);

        _hotkey = new ComboBox
        {
            Location      = new Point(20, y),
            Width         = 120,
            BackColor     = INPUT,
            ForeColor     = FG,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat
        };
        _hotkey.Items.AddRange(new[] { "F8", "F9", "F10", "F11", "F12" });
        _hotkey.SelectedItem = "F9";
        page.Controls.Add(_hotkey);
        y += 40;

        Separator(page, y); y += 20;

        H1(page, "Modi wechseln", ref y);
        var hint2 = new Label
        {
            Text      = "Den aktiven Modus wählst du über das Tray-Icon (Rechtsklick).\n\n" +
                        "Verfügbare Modi:\n" +
                        "  • Roh         — Text direkt einfügen\n" +
                        "  • Förmlich    — Professionelle Geschäftssprache\n" +
                        "  • Locker      — Freundlich, wie an Kollegen\n" +
                        "  • Korrigieren — Nur Grammatik/Rechtschreibung\n" +
                        "  • Kürzer      — Auf das Wesentliche kürzen\n" +
                        "  • Aufzählung  — Als Stichpunktliste",
            Location  = new Point(20, y),
            Size      = new Size(460, 160),
            ForeColor = MUTED,
            Font      = new Font("Segoe UI", 9f),
            BackColor = BG
        };
        page.Controls.Add(hint2);

        return page;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static TabPage MakePage(string title)
    {
        var p = new TabPage(title) { BackColor = BG, ForeColor = FG, Padding = new Padding(0) };
        return p;
    }

    private static void H1(TabPage p, string text, ref int y)
    {
        p.Controls.Add(new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = FG,
            AutoSize  = true,
            Location  = new Point(20, y),
            BackColor = BG
        });
        y += 24;
    }

    private static void Hint(TabPage p, string text, ref int y)
    {
        p.Controls.Add(new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = MUTED,
            AutoSize  = true,
            Location  = new Point(20, y),
            BackColor = BG
        });
        y += 20;
    }

    private static TextBox Input(TabPage p, ref int y, bool password = false)
    {
        var tb = new TextBox
        {
            Location      = new Point(20, y),
            Width         = 460,
            BackColor     = INPUT,
            ForeColor     = FG,
            Font          = new Font("Consolas", 9.5f),
            BorderStyle   = BorderStyle.FixedSingle,
            PasswordChar  = password ? '•' : '\0'
        };
        p.Controls.Add(tb);
        y += 32;
        return tb;
    }

    private static void Link(TabPage p, string text, string url, ref int y)
    {
        var lnk = new LinkLabel
        {
            Text      = text,
            Location  = new Point(18, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 8.5f),
            BackColor = BG
        };
        lnk.LinkColor = ACCENT;
        lnk.LinkClicked += (s, e) =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = url, UseShellExecute = true });
        p.Controls.Add(lnk);
        y += 22;
    }

    private static void ShowToggle(TabPage p, TextBox tb, ref int y)
    {
        var cb = new CheckBox
        {
            Text      = "Anzeigen",
            Location  = new Point(20, y),
            AutoSize  = true,
            ForeColor = MUTED,
            BackColor = BG
        };
        cb.CheckedChanged += (s, e) => tb.PasswordChar = cb.Checked ? '\0' : '•';
        p.Controls.Add(cb);
        y += 28;
    }

    private static void Separator(TabPage p, int y) =>
        p.Controls.Add(new Panel
        {
            Height    = 1,
            Width     = 460,
            BackColor = SEP,
            Location  = new Point(20, y)
        });

    private string ModelId()
    {
        var s = _model.SelectedItem?.ToString() ?? "";
        return s.Split(' ')[0].Trim();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        var groq = _groqKey.Text.Trim();
        if (!groq.StartsWith("gsk_"))
        {
            MessageBox.Show("Bitte einen gültigen Groq API Key eingeben.\n(beginnt mit gsk_)",
                "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cfg = Config.Load();
        cfg.GroqApiKey    = groq;
        cfg.OpenRouterKey = _orKey.Text.Trim();
        cfg.Model         = ModelId();
        cfg.Hotkey        = _hotkey.SelectedItem?.ToString() ?? "F9";
        cfg.Save();

        DialogResult = DialogResult.OK;
        Close();
    }
}
