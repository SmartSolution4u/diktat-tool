using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DiktatTool;

public class MainApp : ApplicationContext
{
    [DllImport("kernel32.dll")] static extern bool Beep(uint freq, uint ms);
    [DllImport("user32.dll")]   static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    private const byte VK_CONTROL       = 0x11;
    private const byte VK_V             = 0x56;
    private const uint KEYEVENTF_KEYUP  = 0x0002;

    private readonly Config       _config;
    private readonly NotifyIcon   _tray;
    private readonly GlobalHotkey _hotkey;
    private readonly GroqClient   _groq;
    private AudioRecorder?        _recorder;
    private bool                  _processing;

    private readonly Icon _iconIdle;
    private readonly Icon _iconRecording;
    private readonly Icon _iconProcessing;

    public MainApp(Config config)
    {
        _config = config;
        _groq   = new GroqClient(config.GroqApiKey);

        _iconIdle       = MakeIcon(Color.FromArgb(100, 100, 100));
        _iconRecording  = MakeIcon(Color.FromArgb(220, 50, 50));
        _iconProcessing = MakeIcon(Color.FromArgb(50, 150, 220));

        _tray = new NotifyIcon
        {
            Icon    = _iconIdle,
            Text    = $"Diktat-Tool  ({config.Hotkey} = Start/Stop)",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _hotkey = new GlobalHotkey(config.Hotkey);
        _hotkey.Pressed += OnHotkey;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add($"{_config.Hotkey} = Start / Stop").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (s, e) =>
        {
            _tray.Visible = false;
            Application.Exit();
        });
        return menu;
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
            SetTray(_iconRecording, "Diktat-Tool — Aufnahme läuft...");
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

        SetTray(_iconProcessing, "Diktat-Tool — Transkribiert...");
        Task.Run(() => Beep(660, 120));

        if (path == null) return;

        _processing = true;
        Task.Run(async () =>
        {
            try
            {
                var text = await _groq.TranscribeAsync(path);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    CopyToClipboard(text);
                    if (_config.AutoPaste)
                    {
                        await Task.Delay(150);
                        Paste();
                    }
                    Beep(1000, 80);
                    await Task.Delay(60);
                    Beep(1200, 80);
                }
            }
            catch (Exception ex)
            {
                SetTray(_iconIdle, $"Fehler: {ex.Message[..Math.Min(50, ex.Message.Length)]}");
                Beep(300, 300);
                await Task.Delay(3000);
            }
            finally
            {
                try { File.Delete(path); } catch { }
                _processing = false;
                SetTray(_iconIdle, $"Diktat-Tool  ({_config.Hotkey} = Start/Stop)");
            }
        });
    }

    private void SetTray(Icon icon, string text)
    {
        // Must run on UI thread
        if (_tray.ContextMenuStrip?.InvokeRequired == true)
            _tray.ContextMenuStrip.Invoke(() => { _tray.Icon = icon; _tray.Text = text[..Math.Min(63, text.Length)]; });
        else
        {
            _tray.Icon = icon;
            _tray.Text = text[..Math.Min(63, text.Length)];
        }
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
