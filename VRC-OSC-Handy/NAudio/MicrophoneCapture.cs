using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy.Osc;
using Whisper.net;
using Whisper.net.LibraryLoader;


namespace VRC_OSC_Handy.NAudio
{
    public class MicrophoneCapture
    {
        private WaveInEvent waveIn;
        private WhisperFactory factory;
        private WhisperProcessor processor;

        private int chunkDurationMs = 5000; // 5-second chunks
        private volatile bool isProcessingChunk = false;

        public void InitializeWhisper(string modelPath, string lang, bool translate)
        {
            DebugLogger.Log("Initializing whisper with " + modelPath);

            // Set CUDA backend preference
            RuntimeOptions.RuntimeLibraryOrder = new System.Collections.Generic.List<RuntimeLibrary>
            {
                RuntimeLibrary.Cuda,
                RuntimeLibrary.Cpu
            };

            factory = WhisperFactory.FromPath(modelPath);
            if (translate)
                processor = factory.CreateBuilder()
                .WithLanguage(lang).WithTranslate()
                .Build();
            else
                processor = factory.CreateBuilder()
                .WithLanguage(lang)
                .Build();

            DebugLogger.Log("Whisper initialized with CUDA backend.");
        }

        public void StartRecording()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16bit, mono
            waveIn.BufferMilliseconds = chunkDurationMs;

            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;

            waveIn.StartRecording();

            DebugLogger.Log("Recording started...");
        }

        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            await ProcessChunkAsync(e.Buffer, e.BytesRecorded);
        }

        private async Task ProcessChunkAsync(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded == 0)
                return;

            // WhisperProcessor isn't safe for concurrent ProcessAsync calls; if a chunk
            // is still processing when the next one arrives, drop it instead of racing.
            if (isProcessingChunk)
            {
                DebugLogger.LogWarning("Dropped audio chunk: previous chunk still processing.");
                return;
            }
            isProcessingChunk = true;

            MemoryStream wavStream = null;

            try
            {
                wavStream = ConvertRawPcmToWav(buffer, bytesRecorded, waveIn.WaveFormat);

                var resultEnumerator = processor.ProcessAsync(wavStream).GetAsyncEnumerator();

                while (await resultEnumerator.MoveNextAsync())
                {
                    var segment = resultEnumerator.Current;
                    var text = segment.Text;

                    if (!string.IsNullOrWhiteSpace(text) && !text.Contains("[BLANK_AUDIO]") && text != " .")
                    {
                        DebugLogger.Log("[Partial Transcript]: " + text);
                        VRCOSC.msgSst = text;
                    }
                }

                await resultEnumerator.DisposeAsync();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("Error processing chunk: " + ex.Message);
            }
            finally
            {
                if (wavStream != null)
                    wavStream.Dispose();
                isProcessingChunk = false;
            }
        }


        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
        }

        public void StopRecording()
        {
            Task.Run(() =>
            {
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                }

                Thread.Sleep(chunkDurationMs);

                if (processor != null)
                {
                    processor.DisposeAsync().AsTask().GetAwaiter().GetResult(); // wait for the real async dispose instead of firing it and forgetting
                    processor = null;
                }

                if (factory != null)
                {
                    factory.Dispose();
                    factory = null;
                }
            });
            DebugLogger.Log("Recording stopped.");
        }

        private MemoryStream ConvertRawPcmToWav(byte[] rawPcmData, int bytesRecorded, WaveFormat format)
        {
            MemoryStream wavStream = new MemoryStream();

            using (WaveFileWriter writer = new WaveFileWriter(new IgnoreDisposeStream(wavStream), format))
            {
                writer.Write(rawPcmData, 0, bytesRecorded);
                writer.Flush();
            }

            wavStream.Position = 0;
            return wavStream;
        }

        // Moved to WhisperLanguages.cs - this is pure static language data, not
        // audio-capture logic. Kept here as pass-through properties so every existing
        // call site (MainWindow, VRCOSC, and the test project) keeps working unchanged.
        public static Dictionary<string, string> LANGUAGES => WhisperLanguages.LANGUAGES;
        public static Dictionary<string, string> TO_LANGUAGE_CODE => WhisperLanguages.TO_LANGUAGE_CODE;
    }
}
