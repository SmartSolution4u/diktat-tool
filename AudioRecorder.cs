using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DiktatTool;

public class AudioRecorder : IDisposable
{
    private WasapiCapture? _capture;
    private WaveFileWriter? _writer;
    private string? _tempPath;

    public bool IsRecording { get; private set; }

    public void Start()
    {
        _tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"dictate_{Guid.NewGuid()}.wav");

        var device = new MMDeviceEnumerator()
            .GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);

        _capture = new WasapiCapture(device);
        _writer  = new WaveFileWriter(_tempPath, _capture.WaveFormat);

        _capture.DataAvailable += (s, e) =>
        {
            if (IsRecording)
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
        };

        _capture.StartRecording();
        IsRecording = true;
    }

    public string? Stop()
    {
        if (!IsRecording) return null;
        IsRecording = false;

        _capture?.StopRecording();

        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;

        _capture?.Dispose();
        _capture = null;

        return _tempPath;
    }

    public void Dispose()
    {
        Stop();
        if (_tempPath != null && File.Exists(_tempPath))
            try { File.Delete(_tempPath); } catch { }
    }
}
