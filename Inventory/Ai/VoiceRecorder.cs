using System.Runtime.InteropServices;
using System.Text;

namespace Inventory.Ai
{
    // Records from the default microphone to a WAV file with Windows' own sound recorder (winmm, no extra package).
    // 16 kHz mono 16-bit: clear for speech and small (about 2 MB per minute).
    public sealed class VoiceRecorder : IDisposable
    {
        private const string Alias = "invpurchasevoice";
        private bool _open;

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr callback);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern bool mciGetErrorString(int error, StringBuilder text, int length);

        public bool IsRecording { get; private set; }

        public void Start()
        {
            if (IsRecording) return;
            Close();
            Send($"open new type waveaudio alias {Alias}");
            _open = true;
            Send($"set {Alias} bitspersample 16 samplespersec 16000 channels 1 bytespersec 32000 alignment 2");
            Send($"record {Alias}");
            IsRecording = true;
        }

        /// <summary>Stops and saves the recording; returns the WAV file (in the temp folder).</summary>
        public string Stop()
        {
            if (!IsRecording) throw new InvalidOperationException("Nothing is being recorded.");
            var path = Path.Combine(Path.GetTempPath(), $"purchase-voice-{DateTime.Now:yyyyMMdd-HHmmss}.wav");
            try
            {
                Send($"stop {Alias}");
                Send($"save {Alias} \"{path}\"");
            }
            finally
            {
                IsRecording = false;
                Close();
            }
            return path;
        }

        /// <summary>Stops without saving.</summary>
        public void Cancel()
        {
            IsRecording = false;
            Close();
        }

        private void Close()
        {
            if (!_open) return;
            mciSendString($"close {Alias}", null, 0, IntPtr.Zero);
            _open = false;
        }

        private static void Send(string command)
        {
            var error = mciSendString(command, null, 0, IntPtr.Zero);
            if (error == 0) return;
            var text = new StringBuilder(256);
            mciGetErrorString(error, text, text.Capacity);
            throw new InvalidOperationException("The microphone could not be used: " + text +
                                                "\nCheck that a microphone is connected and allowed (Windows Settings > Privacy > Microphone).");
        }

        public void Dispose() => Cancel();
    }
}
