using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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

        // Auf 16 kHz Mono reduzieren (das Format, das Whisper nutzt). Die native
        // Aufnahme ist oft 48 kHz/Stereo/32-bit -> mehrere MB; nach dem Downsampling
        // nur ein Bruchteil davon -> deutlich schnellerer Upload zu Groq.
        return Downsample(_tempPath);
    }

    private static string? Downsample(string? srcPath)
    {
        if (srcPath == null || !File.Exists(srcPath)) return srcPath;
        try
        {
            var dstPath = System.IO.Path.ChangeExtension(srcPath, null) + "_16k.wav";
            using (var reader = new AudioFileReader(srcPath))
            {
                ISampleProvider source = reader;
                if (reader.WaveFormat.Channels > 1)
                    source = new StereoToMonoSampleProvider(reader)
                        { LeftVolume = 0.5f, RightVolume = 0.5f };

                var resampled = new WdlResamplingSampleProvider(source, 16000);
                WaveFileWriter.CreateWaveFile16(dstPath, resampled);
            }

            try { File.Delete(srcPath); } catch { }
            return dstPath;
        }
        catch
        {
            // Im Fehlerfall die Originaldatei verwenden — lieber langsam als gar nicht.
            return srcPath;
        }
    }

    public void Dispose()
    {
        // Gibt nur die Aufnahme-Ressourcen frei. Die temporaere WAV-Datei wird
        // bewusst NICHT hier geloescht — sie wird nach der Transkription vom
        // Aufrufer entfernt (MainApp finally). Sonst Use-after-delete:
        // die Datei waere weg, bevor sie transkribiert werden kann.
        Stop();
    }
}
