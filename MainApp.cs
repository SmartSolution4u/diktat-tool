using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DiktatTool;

public class MainApp : ApplicationContext
{
    [DllImport("kernel32.dll")] static extern bool Beep(uint freq, uint ms);
    [DllImport("user32.dll")]   static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    private const byte VK_CONTROL      = 0x11;
    private const byte VK_V            = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private readonly Config           _config;
    private readonly NotifyIcon       _tray;
    private readonly GlobalHotkey     _hotkey;
    private readonly GroqClient       _groq;
    private readonly OpenRouterClient? _openRouter;
    private AudioRecorder?            _recorder;
    private bool                      _processing;

    private readonly Icon _iconIdle;
    private readonly Icon _iconRecording;
    private readonly Icon _iconProcessing;

    public MainApp(Config config)
    {
        _config     = config;
        _groq       = new GroqClient(config.GroqApiKey);
        _openRouter = string.IsNullOrEmpty(config.OpenRouterKey)
            ? null
            : new OpenRouterClient(config.OpenRouterKey);

        _iconIdle       = MakeIcon(Color.FromArgb(100, 100, 100));
        _iconRecording  = MakeIcon(Color.FromArgb(220, 50, 50));
        _iconProcessing = MakeIcon(Color.FromArgb(50, 150, 220));

        _tray = new NotifyIcon
        {
            Icon    = _iconIdle,
            Visible = true
        };
        UpdateTrayText();
        _tray.ContextMenuStrip = BuildMenu();

        _hotkey = new GlobalHotkey(config.Hotkey);
        _hotkey.Pressed += OnHotkey;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 46),
            ForeColor = Color.FromArgb(205, 214, 244),
            Font      = new Font("Segoe UI", 9.5f)
        };

        // Aktueller Modus Header
        var header = new ToolStripMenuItem("MODUS WÄHLEN") { Enabled = false };
        header.ForeColor = Color.FromArgb(108, 112, 134);
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        // Modi
        foreach (var mode in _config.Modes.Where(m => m.Enabled))
        {
            var m = mode;
            var item = new ToolStripMenuItem(m.Name)
            {
                Checked    = m.Name == _config.ActiveMode,
                CheckOnClick = false,
                BackColor  = Color.FromArgb(30, 30, 46),
                ForeColor  = m.Name == _config.ActiveMode
                    ? Color.FromArgb(137, 180, 250)
                    : Color.FromArgb(205, 214, 244)
            };
            item.Click += (s, e) =>
            {
                _config.ActiveMode = m.Name;
                _config.Save();
                RefreshMenu();
                UpdateTrayText();
            };
            menu.Items.Add(item);
        }

        menu.Items.Add(new ToolStripSeparator());

        // Info
        var info = new ToolStripMenuItem($"{_config.Hotkey} = Start / Stop") { Enabled = false };
        info.ForeColor = Color.FromArgb(108, 112, 134);
        menu.Items.Add(info);

        menu.Items.Add(new ToolStripSeparator());

        // Beenden
        var quit = new ToolStripMenuItem("Beenden");
        quit.BackColor = Color.FromArgb(30, 30, 46);
        quit.ForeColor = Color.FromArgb(205, 214, 244);
        quit.Click += (s, e) => { _tray.Visible = false; Application.Exit(); };
        menu.Items.Add(quit);

        return menu;
    }

    private void RefreshMenu()
    {
        _tray.ContextMenuStrip?.Dispose();
        _tray.ContextMenuStrip = BuildMenu();
    }

    private void UpdateTrayText()
    {
        var mode = _config.ActiveMode;
        var txt  = $"Diktat-Tool  [{mode}]  {_config.Hotkey}=Start/Stop";
        _tray.Text = txt[..Math.Min(63, txt.Length)];
    }

    private void OnHotkey()
    {
        if (_recorder?.IsRecording == true)
            StopAndProcess();
        else if (!_processing)
            StartRecording();
    }

    private void StartRecording()
    {
        try
        {
            _recorder = new AudioRecorder();
            _recorder.Start();

            var mode = _config.ActiveMode;
            SetIcon(_iconRecording);
            _tray.Text = $"Diktat-Tool — Aufnahme läuft [{mode}]...";

            ShowBalloon($"Aufnahme läuft [{mode}]", $"{_config.Hotkey} nochmal zum Stoppen", 1500);
            Task.Run(() => Beep(880, 120));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Mikrofon-Fehler:\n{ex.Message}", "Diktat-Tool",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Task.Run(() => Beep(300, 300));
        }
    }

    private void StopAndProcess()
    {
        var path = _recorder?.Stop();
        _recorder?.Dispose();
        _recorder = null;

        SetIcon(_iconProcessing);
        _tray.Text = "Diktat-Tool — Transkribiert...";
        Task.Run(() => Beep(660, 120));

        if (path == null) return;

        var activeMode = _config.GetActiveMode();
        _processing = true;

        Task.Run(async () =>
        {
            try
            {
                var text = await _groq.TranscribeAsync(path);
                if (string.IsNullOrWhiteSpace(text)) return;

                // Reformulieren wenn Modus einen Prompt hat + OpenRouter verfügbar
                if (!string.IsNullOrEmpty(activeMode?.Prompt) && _openRouter != null)
                {
                    _tray.Text = $"Diktat-Tool — Formuliere [{activeMode.Name}]...";
                    text = await _openRouter.ReformulateAsync(text, activeMode.Prompt, _config.Model);
                }

                CopyToClipboard(text);

                if (_config.AutoPaste)
                {
                    await Task.Delay(150);
                    Paste();
                }

                ShowBalloon("Text eingefügt", text[..Math.Min(80, text.Length)] + (text.Length > 80 ? "..." : ""), 2000);
                Beep(1000, 80);
                await Task.Delay(60);
                Beep(1200, 80);
            }
            catch (Exception ex)
            {
                ShowBalloon("Fehler", ex.Message[..Math.Min(100, ex.Message.Length)], 3000);
                Task.Run(() => Beep(300, 300));
            }
            finally
            {
                try { File.Delete(path); } catch { }
                _processing = false;
                SetIcon(_iconIdle);
                UpdateTrayText();
            }
        });
    }

    private void SetIcon(Icon icon)
    {
        if (_tray.ContextMenuStrip?.InvokeRequired == true)
            _tray.ContextMenuStrip.Invoke(() => _tray.Icon = icon);
        else
            _tray.Icon = icon;
    }

    private void ShowBalloon(string title, string text, int ms)
    {
        try
        {
            _tray.BalloonTipTitle = title;
            _tray.BalloonTipText  = text;
            _tray.ShowBalloonTip(ms);
        }
        catch { }
    }

    private static void CopyToClipboard(string text)
    {
        var t = new Thread(() => Clipboard.SetText(text));
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
    }

    private static void Paste()
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V,       0, 0, UIntPtr.Zero);
        keybd_event(VK_V,       0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static Icon MakeIcon(Color dot)
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var b = new SolidBrush(dot);
        g.FillEllipse(b, 4, 4, 24, 24);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkey.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _recorder?.Dispose();
            _iconIdle.Dispose();
            _iconRecording.Dispose();
            _iconProcessing.Dispose();
        }
        base.Dispose(disposing);
    }
}
