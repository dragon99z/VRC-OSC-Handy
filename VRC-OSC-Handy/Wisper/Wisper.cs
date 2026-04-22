using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRC_OSC_Handy.Config;
using VRC_OSC_Handy.Logger;
using VRC_OSC_Handy.NAudio;
using VRC_OSC_Handy.Osc;
using Whisper.net.Ggml;



namespace VRC_OSC_Handy.Wis
{
    public class Wisper
    {

        CancellationTokenSource WisperToken = new CancellationTokenSource();
        CancellationToken wisper_ct;
        MicrophoneCapture whisper = new MicrophoneCapture();

        public bool isRunning = false;

        public Wisper()
        {
            wisper_ct = WisperToken.Token;
        }

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public void start(string modelPath = @"models\Base.bin", string lang = "auto", bool translate = false)
        {
            Task.Run(() => run(modelPath, lang, translate), WisperToken.Token);
        }

        public void run(string modelPath, string lang, bool translate)
        {
            if (!File.Exists(modelPath))
            {
                string modelName = modelPath.Replace(".bin", "");
                modelName = modelName.Replace(@"models\", "");

                DownloadModel(modelPath, (GgmlType)Enum.Parse(typeof(GgmlType), modelName)).GetAwaiter().GetResult();
                DebugLogger.Log(modelName + " downloaded!");
                if (Application.Current != null)
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MainWindow main = Application.Current.MainWindow as MainWindow;
                        reloadModelList(main.STTModels, modelName);

                    });
            }
                
            Thread.Sleep(5000);
            whisper.InitializeWhisper(modelPath, lang, translate); // Path to your model
            whisper.StartRecording();
            isRunning = true;

            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    MainWindow main = Application.Current.MainWindow as MainWindow;
                    main.STTRunning.Text = "STT is active";
                    main.STTRunning.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));

                });
        }

        public void reloadModelList(ComboBox comboBox, string modelName)
        {

            if (!Directory.Exists(@"models\"))
                Directory.CreateDirectory(@"models\");

            bool isEmpty = !Directory.EnumerateFiles(@"models\").Any();

            if (!isEmpty)
            {
                var files = Directory
                .GetFiles(@"models\", "*", SearchOption.AllDirectories)
                .Select(f => System.IO.Path.GetFileName(f));

                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    ComboBoxItem item = comboBox.Items[i] as ComboBoxItem;
                    string model = item.Content as string;
                    DebugLogger.Log(model);
                    if (model == modelName + " (downloadable)")
                    {
                        item.Content = modelName;
                        break;
                    }
                }
            }
        }

        private static async Task DownloadModel(string fileName, GgmlType ggmlType)
        {
            DebugLogger.Log($"Downloading Model {fileName}");
            var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
            var fileWriter = File.OpenWrite(fileName);
            await modelStream.CopyToAsync(fileWriter);
        }

        public void stop()
        {
            whisper.StopRecording();
            isRunning = false;
        }
    }
}
