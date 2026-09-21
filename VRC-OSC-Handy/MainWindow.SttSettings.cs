using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VRC_OSC_Handy.NAudio;
using Whisper.net.Ggml;

namespace VRC_OSC_Handy
{
    // The speech-to-text (Whisper) settings UI - model picker, language picker,
    // translate checkbox, enable button - plus the clock display. Split out of
    // MainWindow.xaml.cs since it's a self-contained settings area.
    public partial class MainWindow
    {
        private void Time_Loaded(object sender, RoutedEventArgs e)
        {

            string pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;

            bool uses24Hour = !pattern.Contains("tt");

            ct = updateToken.Token;
            TextBlock text = sender as TextBlock;
            Task.Run(() =>
            {
                while (!ct.IsCancellationRequested)
                {
                    var uiAccess = Time.Dispatcher.CheckAccess();

                    if (uiAccess)
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        if (uses24Hour)
                            Time.Text = DateTime.Now.ToString("HH:mm:ss");
                        else
                            Time.Text = DateTime.Now.ToString("HH:mm:ss tt");
                    }
                    else
                    {
                        if (ct.IsCancellationRequested)
                            break;
                        Time.Dispatcher.Invoke(() =>
                        {
                            if (uses24Hour)
                                Time.Text = DateTime.Now.ToString("HH:mm:ss");
                            else
                                Time.Text = DateTime.Now.ToString("HH:mm:ss tt");
                        });
                    }
                    Thread.Sleep(250);
                }
            }, updateToken.Token);
        }

        private void STTModels_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (!Directory.Exists(@"models\"))
                Directory.CreateDirectory(@"models\");

            bool isEmpty = !Directory.EnumerateFiles(@"models\").Any();

            if (!isEmpty)
            {
                var files = Directory
                .GetFiles(@"models\", "*", SearchOption.AllDirectories)
                .Select(f => System.IO.Path.GetFileName(f));

                foreach (string ggmlType in Enum.GetNames(typeof(GgmlType)))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    if (files.Contains(ggmlType + ".bin"))
                        item.Content = ggmlType;
                    else
                        item.Content = ggmlType + " (downloadable)";
                    item.Tag = ggmlType;
                    comboBox.Items.Add(item);
                }
            }
            else
            {
                foreach (string ggmlType in Enum.GetNames(typeof(GgmlType)))
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = ggmlType + " (downloadable)";
                    item.Tag = ggmlType;
                    comboBox.Items.Add(item);
                }
            }
            comboBox.SelectedIndex = config.STT.Model;

            ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
            modelPath = @"models\" + comboBoxItem.Content;
            if (modelPath.Contains(" (downloadable)"))
                modelPath = modelPath.Replace(" (downloadable)", "");
            modelPath = modelPath + ".bin";
        }

        private void STTModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.IsLoaded)
            {
                bool wasRunning = false;
                if (wisper.isRunning)
                {
                    wasRunning = true;
                    wisper.stop();
                }

                ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
                config.STT.Model = comboBox.SelectedIndex;
                if (comboBoxItem.IsLoaded)
                {
                    modelPath = @"models\" + comboBoxItem.Content;
                    if (modelPath.Contains(" (downloadable)"))
                        modelPath = modelPath.Replace(" (downloadable)", "");
                    modelPath = modelPath + ".bin";
                    if (wasRunning)
                        wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language), config.STT.Translate);
                }
            }

        }

        private void STTEnablex_Click(object sender, RoutedEventArgs e)
        {
            if (wisper.isRunning)
            {
                wisper.stop();
                STTRunning.Text = "STT is not active";
                STTRunning.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            else
            {
                wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language), config.STT.Translate);
                STTRunning.Text = "STT starting...";
                STTRunning.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
            }
        }

        private void STTLanguage_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            foreach (KeyValuePair<string, string> language in MicrophoneCapture.LANGUAGES)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = language.Value;
                item.Tag = language.Key;
                comboBox.Items.Add(item);
            }
            comboBox.SelectedIndex = config.STT.Language;
        }

        private void STTLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.IsLoaded)
            {
                ComboBoxItem comboBoxItem = comboBox.Items[comboBox.SelectedIndex] as ComboBoxItem;
                config.STT.Language = comboBox.SelectedIndex;
                bool wasRunning = false;
                if (wisper.isRunning)
                {
                    wasRunning = true;
                    wisper.stop();
                }

                if (wasRunning)
                {
                    wisper.start(modelPath, MicrophoneCapture.LANGUAGES.Keys.ElementAt(config.STT.Language), config.STT.Translate);
                }

            }
        }

        private void STTTranslate_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            checkBox.IsChecked = config.STT.Translate;
        }

        private void STTTranslate_Unchecked(object sender, RoutedEventArgs e)
        {
            config.STT.Translate = false;
        }

        private void STTTranslate_Checked(object sender, RoutedEventArgs e)
        {
            config.STT.Translate = true;
        }
    }
}
