using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Utils;
using NAudio.Wave;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy.Osc;
using VRC_OSC_Handy.Wis;
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
            if(translate)
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
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    Task.Run(() => {
                        if (waveIn != null)
                        {
                            waveIn.StopRecording();
                        }

                        Thread.Sleep(chunkDurationMs);

                        if (processor != null)
                        {
                            processor.DisposeAsync();
                            processor = null;
                        }

                        if (factory != null)
                        {
                            factory.Dispose();
                            factory = null;
                        }
                    });
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

        public static Dictionary<string, string> LANGUAGES = new Dictionary<string, string>
        {
            { "auto", "auto" },
            { "en", "english" },
            { "zh", "chinese" },
            { "de", "german" },
            { "es", "spanish" },
            { "ru", "russian" },
            { "ko", "korean" },
            { "fr", "french" },
            { "ja", "japanese" },
            { "pt", "portuguese" },
            { "tr", "turkish" },
            { "pl", "polish" },
            { "ca", "catalan" },
            { "nl", "dutch" },
            { "ar", "arabic" },
            { "sv", "swedish" },
            { "it", "italian" },
            { "id", "indonesian" },
            { "hi", "hindi" },
            { "fi", "finnish" },
            { "vi", "vietnamese" },
            { "he", "hebrew" },
            { "uk", "ukrainian" },
            { "el", "greek" },
            { "ms", "malay" },
            { "cs", "czech" },
            { "ro", "romanian" },
            { "da", "danish" },
            { "hu", "hungarian" },
            { "ta", "tamil" },
            { "no", "norwegian" },
            { "th", "thai" },
            { "ur", "urdu" },
            { "hr", "croatian" },
            { "bg", "bulgarian" },
            { "lt", "lithuanian" },
            { "la", "latin" },
            { "mi", "maori" },
            { "ml", "malayalam" },
            { "cy", "welsh" },
            { "sk", "slovak" },
            { "te", "telugu" },
            { "fa", "persian" },
            { "lv", "latvian" },
            { "bn", "bengali" },
            { "sr", "serbian" },
            { "az", "azerbaijani" },
            { "sl", "slovenian" },
            { "kn", "kannada" },
            { "et", "estonian" },
            { "mk", "macedonian" },
            { "br", "breton" },
            { "eu", "basque" },
            { "is", "icelandic" },
            { "hy", "armenian" },
            { "ne", "nepali" },
            { "mn", "mongolian" },
            { "bs", "bosnian" },
            { "kk", "kazakh" },
            { "sq", "albanian" },
            { "sw", "swahili" },
            { "gl", "galician" },
            { "mr", "marathi" },
            { "pa", "punjabi" },
            { "si", "sinhala" },
            { "km", "khmer" },
            { "sn", "shona" },
            { "yo", "yoruba" },
            { "so", "somali" },
            { "af", "afrikaans" },
            { "oc", "occitan" },
            { "ka", "georgian" },
            { "be", "belarusian" },
            { "tg", "tajik" },
            { "sd", "sindhi" },
            { "gu", "gujarati" },
            { "am", "amharic" },
            { "yi", "yiddish" },
            { "lo", "lao" },
            { "uz", "uzbek" },
            { "fo", "faroese" },
            { "ht", "haitian creole" },
            { "ps", "pashto" },
            { "tk", "turkmen" },
            { "nn", "nynorsk" },
            { "mt", "maltese" },
            { "sa", "sanskrit" },
            { "lb", "luxembourgish" },
            { "my", "myanmar" },
            { "bo", "tibetan" },
            { "tl", "tagalog" },
            { "mg", "malagasy" },
            { "as", "assamese" },
            { "tt", "tatar" },
            { "haw", "hawaiian" },
            { "ln", "lingala" },
            { "ha", "hausa" },
            { "ba", "bashkir" },
            { "jw", "javanese" },
            { "su", "sundanese" },
            { "yue", "cantonese" }
        };

        public static Dictionary<string, string> TO_LANGUAGE_CODE = new Dictionary<string, string>
        {
            { "english", "en" },
            { "chinese", "zh" },
            { "german", "de" },
            { "spanish", "es" },
            { "russian", "ru" },
            { "korean", "ko" },
            { "french", "fr" },
            { "japanese", "ja" },
            { "portuguese", "pt" },
            { "turkish", "tr" },
            { "polish", "pl" },
            { "catalan", "ca" },
            { "dutch", "nl" },
            { "arabic", "ar" },
            { "swedish", "sv" },
            { "italian", "it" },
            { "indonesian", "id" },
            { "hindi", "hi" },
            { "finnish", "fi" },
            { "vietnamese", "vi" },
            { "hebrew", "he" },
            { "ukrainian", "uk" },
            { "greek", "el" },
            { "malay", "ms" },
            { "czech", "cs" },
            { "romanian", "ro" },
            { "danish", "da" },
            { "hungarian", "hu" },
            { "tamil", "ta" },
            { "norwegian", "no" },
            { "thai", "th" },
            { "urdu", "ur" },
            { "croatian", "hr" },
            { "bulgarian", "bg" },
            { "lithuanian", "lt" },
            { "latin", "la" },
            { "maori", "mi" },
            { "malayalam", "ml" },
            { "welsh", "cy" },
            { "slovak", "sk" },
            { "telugu", "te" },
            { "persian", "fa" },
            { "latvian", "lv" },
            { "bengali", "bn" },
            { "serbian", "sr" },
            { "azerbaijani", "az" },
            { "slovenian", "sl" },
            { "kannada", "kn" },
            { "estonian", "et" },
            { "macedonian", "mk" },
            { "breton", "br" },
            { "basque", "eu" },
            { "icelandic", "is" },
            { "armenian", "hy" },
            { "nepali", "ne" },
            { "mongolian", "mn" },
            { "bosnian", "bs" },
            { "kazakh", "kk" },
            { "albanian", "sq" },
            { "swahili", "sw" },
            { "galician", "gl" },
            { "marathi", "mr" },
            { "punjabi", "pa" },
            { "sinhala", "si" },
            { "khmer", "km" },
            { "shona", "sn" },
            { "yoruba", "yo" },
            { "somali", "so" },
            { "afrikaans", "af" },
            { "occitan", "oc" },
            { "georgian", "ka" },
            { "belarusian", "be" },
            { "tajik", "tg" },
            { "sindhi", "sd" },
            { "gujarati", "gu" },
            { "amharic", "am" },
            { "yiddish", "yi" },
            { "lao", "lo" },
            { "uzbek", "uz" },
            { "faroese", "fo" },
            { "haitian creole", "ht" },
            { "pashto", "ps" },
            { "turkmen", "tk" },
            { "nynorsk", "nn" },
            { "maltese", "mt" },
            { "sanskrit", "sa" },
            { "luxembourgish", "lb" },
            { "myanmar", "my" },
            { "tibetan", "bo" },
            { "tagalog", "tl" },
            { "malagasy", "mg" },
            { "assamese", "as" },
            { "tatar", "tt" },
            { "hawaiian", "haw" },
            { "lingala", "ln" },
            { "hausa", "ha" },
            { "bashkir", "ba" },
            { "javanese", "jw" },
            { "sundanese", "su" },
            { "cantonese", "yue" },

            // Synonyms / Alternate names
            { "burmese", "my" },
            { "valencian", "ca" },
            { "flemish", "nl" },
            { "haitian", "ht" },
            { "letzeburgesch", "lb" },
            { "pushto", "ps" },
            { "panjabi", "pa" },
            { "moldavian", "ro" },
            { "moldovan", "ro" },
            { "sinhalese", "si" },
            { "castilian", "es" },
            { "mandarin", "zh" }
        };
    }
}
