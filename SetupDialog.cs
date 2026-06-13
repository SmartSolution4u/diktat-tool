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
    private ComboBox  _sttModel  = null!;
    private ComboBox  _hotkey    = null!;

    private ListBox   _modeList   = null!;
    private TextBox   _modeName   = null!;
    private TextBox   _modePrompt = null!;
    private List<DictationMode> _editModes = new();
    private int _currentModeIndex = -1;

    public SetupDialog()
    {
        Text            = "Diktat-Tool — Einrichtung";
        Size            = new Size(540, 640);
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
        tabs.TabPages.Add(BuildModesTab());
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

        PrefillFromConfig();
    }

    /// <summary>Befuellt die Felder mit den aktuell gespeicherten Werten
    /// (Keys aus dem Credential Manager), damit der Dialog auch zum Aendern taugt.</summary>
    private void PrefillFromConfig()
    {
        try
        {
            var cfg = Config.Load();
            _groqKey.Text = cfg.GroqApiKey;
            _orKey.Text   = cfg.OpenRouterKey;

            for (int i = 0; i < _model.Items.Count; i++)
            {
                if ((_model.Items[i]?.ToString() ?? "").TrimStart().StartsWith(cfg.Model))
                {
                    _model.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < _sttModel.Items.Count; i++)
            {
                if ((_sttModel.Items[i]?.ToString() ?? "").TrimStart().StartsWith(cfg.SttModel))
                {
                    _sttModel.SelectedIndex = i;
                    break;
                }
            }

            if (_hotkey.Items.Contains(cfg.Hotkey))
                _hotkey.SelectedItem = cfg.Hotkey;

            // Modi als bearbeitbare Kopie in den Editor laden
            _editModes = cfg.Modes
                .Select(m => new DictationMode { Name = m.Name, Prompt = m.Prompt, Enabled = m.Enabled })
                .ToList();
            RefreshModeList(0);
        }
        catch { }
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
            "anthropic/claude-haiku-4.5                 (Claude, schnell — empfohlen)",
            "anthropic/claude-sonnet-4.5                (Claude, beste Qualität)",
            "mistralai/mistral-small-3.1-24b-instruct   (günstig)",
            "openai/gpt-4o-mini                         (OpenAI, günstig)",
            "openai/gpt-4o                              (OpenAI, hohe Qualität)",
            "meta-llama/llama-3.3-70b-instruct          (Open Source)",
        });
        _model.SelectedIndex = 0;
        page.Controls.Add(_model);
        y += 35;

        Separator(page, y); y += 20;

        H1(page, "Kosten-Übersicht (ca.)", ref y);
        var cost = new Label
        {
            Text = "Claude Haiku:    ~1 $ Input / 5 $ Output je 1 Mio. Tokens\n" +
                   "                 ≈ deutlich unter 0,01 € pro Diktat\n" +
                   "Mistral Small:   ~0,10 € / 1 Mio. Tokens  (günstigste Option)\n" +
                   "GPT-4o mini:     ~0,15 € / 1 Mio. Tokens\n\n" +
                   "Bei 50 Diktaten/Tag mit Umformulierung: wenige Cent/Monat",
            Location  = new Point(20, y),
            Size      = new Size(460, 100),
            BackColor = Color.FromArgb(24, 24, 37),
            ForeColor = Color.FromArgb(166, 227, 161),
            Font      = new Font("Consolas", 8.5f),
            Padding   = new Padding(8)
        };
        page.Controls.Add(cost);
        y += 110;

        Separator(page, y); y += 20;

        H1(page, "Spracherkennung (Whisper bei Groq)", ref y);
        Hint(page, "Turbo = schnell & günstig (empfohlen). Large = minimal genauer, aber langsamer.", ref y);
        _sttModel = new ComboBox
        {
            Location      = new Point(20, y),
            Width         = 460,
            BackColor     = INPUT,
            ForeColor     = FG,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat
        };
        _sttModel.Items.AddRange(new[]
        {
            "whisper-large-v3-turbo   (schnell, günstig — empfohlen)",
            "whisper-large-v3         (minimal genauer, langsamer)",
        });
        _sttModel.SelectedIndex = 0;
        page.Controls.Add(_sttModel);

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
            Text      = "Den aktiven Modus wählst du jederzeit über das Tray-Icon\n" +
                        "(Rechtsklick auf das Symbol unten rechts).\n\n" +
                        "Die Modi und ihre Prompts kannst du im Tab 'Modi' anpassen -\n" +
                        "eigene Modi anlegen, umbenennen oder löschen.\n\n" +
                        "Tipp: Ein Modus mit leerem Prompt fügt den Text direkt ein\n" +
                        "(keine KI), genau wie der Modus 'Roh'.",
            Location  = new Point(20, y),
            Size      = new Size(460, 160),
            ForeColor = MUTED,
            Font      = new Font("Segoe UI", 9f),
            BackColor = BG
        };
        page.Controls.Add(hint2);

        return page;
    }

    private TabPage BuildModesTab()
    {
        var page = MakePage("Modi");
        int y = 15;

        H1(page, "Modi & Prompts bearbeiten", ref y);
        Hint(page, "Modus anklicken, dann Name/Prompt unten anpassen.", ref y);
        Hint(page, "Leerer Prompt = keine KI (Text wird direkt eingefügt, wie 'Roh').", ref y);

        _modeList = new ListBox
        {
            Location    = new Point(20, y),
            Size        = new Size(230, 120),
            BackColor   = INPUT,
            ForeColor   = FG,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Segoe UI", 9.5f)
        };
        _modeList.SelectedIndexChanged += OnModeSelected;
        page.Controls.Add(_modeList);

        var btnNew = MakeSmallButton("Neuer Modus", new Point(265, y));
        btnNew.Click += OnModeNew;
        page.Controls.Add(btnNew);

        var btnDel = MakeSmallButton("Löschen", new Point(265, y + 34));
        btnDel.Click += OnModeDelete;
        page.Controls.Add(btnDel);

        var btnReset = MakeSmallButton("Auf Standard", new Point(265, y + 68));
        btnReset.Click += OnModesReset;
        page.Controls.Add(btnReset);

        y += 132;

        page.Controls.Add(new Label
        {
            Text = "Name", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = FG,
            AutoSize = true, Location = new Point(20, y), BackColor = BG
        });
        y += 22;
        _modeName = new TextBox
        {
            Location = new Point(20, y), Width = 490, BackColor = INPUT, ForeColor = FG,
            Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle
        };
        page.Controls.Add(_modeName);
        y += 32;

        page.Controls.Add(new Label
        {
            Text = "Prompt (Anweisung an die KI)", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = FG, AutoSize = true, Location = new Point(20, y), BackColor = BG
        });
        y += 22;
        _modePrompt = new TextBox
        {
            Location = new Point(20, y), Size = new Size(490, 150), BackColor = INPUT, ForeColor = FG,
            Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle,
            Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true
        };
        page.Controls.Add(_modePrompt);

        return page;
    }

    private Button MakeSmallButton(string text, Point loc) => new Button
    {
        Text      = text,
        Location  = loc,
        Size      = new Size(120, 28),
        BackColor = INPUT,
        ForeColor = FG,
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9f),
        Cursor    = Cursors.Hand
    };

    private void OnModeSelected(object? sender, EventArgs e)
    {
        SaveCurrentModeFromFields();
        _currentModeIndex = _modeList.SelectedIndex;
        if (_currentModeIndex >= 0 && _currentModeIndex < _editModes.Count)
        {
            _modeName.Text   = _editModes[_currentModeIndex].Name;
            _modePrompt.Text = _editModes[_currentModeIndex].Prompt;
        }
    }

    private void SaveCurrentModeFromFields()
    {
        if (_currentModeIndex >= 0 && _currentModeIndex < _editModes.Count)
        {
            _editModes[_currentModeIndex].Name   = _modeName.Text.Trim();
            _editModes[_currentModeIndex].Prompt = _modePrompt.Text.Trim();
        }
    }

    private void RefreshModeList(int select)
    {
        _modeList.SelectedIndexChanged -= OnModeSelected;
        _modeList.Items.Clear();
        foreach (var m in _editModes) _modeList.Items.Add(m.Name);
        _currentModeIndex = -1;
        _modeList.SelectedIndexChanged += OnModeSelected;
        if (select >= 0 && select < _editModes.Count)
            _modeList.SelectedIndex = select;
    }

    private void OnModeNew(object? sender, EventArgs e)
    {
        SaveCurrentModeFromFields();
        _editModes.Add(new DictationMode { Name = "Neuer Modus", Prompt = "", Enabled = true });
        RefreshModeList(_editModes.Count - 1);
    }

    private void OnModeDelete(object? sender, EventArgs e)
    {
        SaveCurrentModeFromFields();
        int idx = _modeList.SelectedIndex;
        if (idx < 0) return;
        if (_editModes.Count <= 1)
        {
            MessageBox.Show("Mindestens ein Modus muss bestehen bleiben.", "Hinweis",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _editModes.RemoveAt(idx);
        RefreshModeList(Math.Min(idx, _editModes.Count - 1));
    }

    private void OnModesReset(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Alle Modi auf die Standard-Vorgaben zurücksetzen?",
            "Zurücksetzen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _editModes = Config.DefaultModes();
        RefreshModeList(0);
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

    private string SttModelId()
    {
        var s = _sttModel.SelectedItem?.ToString() ?? "";
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

        // Aktuelle Modus-Bearbeitung sichern und Modi uebernehmen
        SaveCurrentModeFromFields();
        var cleaned = _editModes.Where(m => !string.IsNullOrWhiteSpace(m.Name)).ToList();

        var cfg = Config.Load();
        cfg.GroqApiKey    = groq;
        cfg.OpenRouterKey = _orKey.Text.Trim();
        cfg.Model         = ModelId();
        cfg.SttModel      = SttModelId();
        cfg.Hotkey        = _hotkey.SelectedItem?.ToString() ?? "F9";
        if (cleaned.Count > 0)
        {
            cfg.Modes = cleaned;
            // Aktiven Modus auf gueltigen Wert halten, falls umbenannt/geloescht
            if (!cleaned.Any(m => m.Name == cfg.ActiveMode))
                cfg.ActiveMode = cleaned[0].Name;
        }
        cfg.Save();

        DialogResult = DialogResult.OK;
        Close();
    }
}
