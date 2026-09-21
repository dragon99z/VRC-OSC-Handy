using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VRC_OSC_Handy
{
    // Everything needed to build the VM_Controller panel (the strip of VoiceMeeter
    // A/B/Mute/Gain controls shown in the main window). Split out of MainWindow.xaml.cs
    // since it's a self-contained chunk of UI-building code that just happens to need
    // a few members (VM_Controller, remoteControle, vmToggle, vmValueChange,
    // setVRCParameterMV) declared in the other part of this partial class.
    public partial class MainWindow
    {
        // VoiceMeeter comes in three editions - Standard, Banana, and Potato - each
        // exposing a different number of strips and a different number of A/B routing
        // buttons per strip. `type` is the detected edition (1 = Standard, 2 = Banana,
        // 3 = Potato); anything else means VoiceMeeter wasn't found.
        //
        // Every strip's UI (header, A/B buttons, Mute, Gain header/value/slider) is
        // otherwise identical in structure, so it's built once by BuildVmStrip() and
        // driven by two small tables below instead of being hand-copied per strip per
        // edition (the old version of this method duplicated the same ~120 lines of
        // control setup 10 times over, once per strip per edition).
        private sealed class VmEditionLayout
        {
            public int StripCount;
            public int AButtonCount;
            public int BButtonCount;

            // Vertical gaps below the strip's Mute button; these are edition-specific
            // (Potato's panel is laid out a little tighter than Standard/Banana's) but
            // identical across all strips within one edition.
            public double GainHeaderGapFromMute;
            public double GainValueGapFromHeader;
            public double GainSliderGapFromValue;
        }

        private static readonly Dictionary<int, VmEditionLayout> vmEditionLayouts = new Dictionary<int, VmEditionLayout>
        {
            [1] = new VmEditionLayout // Standard
            {
                StripCount = 2,
                AButtonCount = 1,
                BButtonCount = 1,
                GainHeaderGapFromMute = 35,
                GainValueGapFromHeader = 30,
                GainSliderGapFromValue = 30,
            },
            [2] = new VmEditionLayout // Banana
            {
                StripCount = 3,
                AButtonCount = 3,
                BButtonCount = 2,
                GainHeaderGapFromMute = 35,
                GainValueGapFromHeader = 30,
                GainSliderGapFromValue = 25,
            },
            [3] = new VmEditionLayout // Potato
            {
                StripCount = 5,
                AButtonCount = 5,
                BButtonCount = 3,
                GainHeaderGapFromMute = 30,
                GainValueGapFromHeader = 20,
                GainSliderGapFromValue = 25,
            },
        };

        // Right-edge placement per strip index (0 = rightmost/first strip). These were
        // hand-tuned in the designer rather than following a clean arithmetic step -
        // strip 1 in particular sits 5px off from the pattern every other strip
        // follows - so they're kept as an explicit lookup rather than a formula that
        // would only be an approximation. The same table is reused for every edition:
        // strip 0 sits at the same offset whether VoiceMeeter is Standard, Banana, or
        // Potato.
        private static readonly (double Header, double Button, double GainLabel)[] vmStripRightMargins =
        {
            (290, 310, 290), // Strip 0
            (220, 235, 215), // Strip 1
            (140, 160, 140), // Strip 2
            (65, 85, 65),    // Strip 3
            (-10, 10, -10),  // Strip 4
        };

        private const double VmRowTop = 10;
        private const double VmFirstButtonTop = 35;
        private const double VmRowHeight = 30;
        private const double VmControlWidth = 65;
        private const double VmControlHeight = 30;

        private void LoadVMSettings(int type)
        {
            if (!vmEditionLayouts.TryGetValue(type, out VmEditionLayout layout))
            {
                VM_Controller.Children.Add(new TextBlock
                {
                    Text = "Voicemeeter not found!",
                    Margin = new Thickness(30, 10, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                });
                return;
            }

            for (int stripIndex = 0; stripIndex < layout.StripCount; stripIndex++)
                BuildVmStrip(stripIndex, layout);
        }

        // Builds one full VoiceMeeter strip panel: header, A-buttons, B-buttons, Mute,
        // and the Gain header/value/slider group underneath it.
        private void BuildVmStrip(int stripIndex, VmEditionLayout layout)
        {
            (double headerRight, double buttonRight, double gainLabelRight) = vmStripRightMargins[stripIndex];

            AddVmLabel($"Strip {stripIndex}", VmRowTop, headerRight);

            double y = VmFirstButtonTop;
            for (int i = 1; i <= layout.AButtonCount; i++, y += VmRowHeight)
                AddVmToggleButton($"A{i}", stripIndex, y, buttonRight);
            for (int i = 1; i <= layout.BButtonCount; i++, y += VmRowHeight)
                AddVmToggleButton($"B{i}", stripIndex, y, buttonRight);

            AddVmToggleButton("Mute", stripIndex, y, buttonRight);
            double muteY = y;

            double gainHeaderY = muteY + layout.GainHeaderGapFromMute;
            AddVmLabel("Gain", gainHeaderY, gainLabelRight);

            string gainUid = $"Strip[{stripIndex}].Gain";
            double gainValueY = gainHeaderY + layout.GainValueGapFromHeader;
            VM_Controller.Children.Add(new TextBlock
            {
                Text = Math.Round(remoteControle.getParameter(gainUid), 2).ToString(),
                Uid = gainUid + "_Value",
                Width = VmControlWidth,
                Height = VmControlHeight,
                Margin = new Thickness(0, gainValueY, gainLabelRight, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            });

            double gainSliderY = gainValueY + layout.GainSliderGapFromValue;
            Slider gainSlider = new Slider
            {
                Uid = gainUid,
                Margin = new Thickness(0, gainSliderY, buttonRight, 0),
                Width = VmControlWidth,
                Height = VmControlHeight,
                Maximum = 12,
                Minimum = -60,
                Value = remoteControle.getParameter(gainUid),
                LargeChange = 0.5,
                SmallChange = 0.01,
                Style = Resources["Horizontal_Slider"] as Style,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            gainSlider.ValueChanged += vmValueChange;
            gainSlider.MouseRightButtonDown += setVRCParameterMV;
            VM_Controller.Children.Add(gainSlider);
        }

        private void AddVmLabel(string text, double top, double right)
        {
            VM_Controller.Children.Add(new TextBlock
            {
                Text = text,
                Width = VmControlWidth,
                Height = VmControlHeight,
                Margin = new Thickness(0, top, right, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            });
        }

        // Adds one A/B/Mute toggle button for the given strip, wired to vmToggle for
        // left-click and setVRCParameterMV for right-click, and colored to reflect the
        // strip's *own* current VoiceMeeter state.
        private void AddVmToggleButton(string label, int stripIndex, double top, double right)
        {
            string uid = $"Strip[{stripIndex}].{label}";
            bool isOn = remoteControle.getBoolParameter(uid);

            Button button = new Button
            {
                Margin = new Thickness(0, top, right, 0),
                Width = VmControlWidth,
                Height = VmControlHeight,
                Content = label,
                Uid = uid,
                Style = Resources["RoundedButton"] as Style,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(isOn
                    ? Color.FromArgb(255, 0, 255, 0)
                    : Color.FromArgb(255, 255, 0, 0)),
                BorderBrush = null,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            button.Click += vmToggle;
            button.MouseRightButtonDown += setVRCParameterMV;
            VM_Controller.Children.Add(button);
        }

        private void VM_Controller_Loaded(object sender, RoutedEventArgs e)
        {
            vmt = updateVMToken.Token;
            int type = remoteControle.type;
            LoadVMSettings(type);
            Task.Run(() =>
            {
                while (!vmt.IsCancellationRequested)
                {
                    if (type != remoteControle.type)
                    {
                        type = remoteControle.type;

                        var uiAccess = VM_Controller.Dispatcher.CheckAccess();

                        if (uiAccess)
                        {
                            if (ct.IsCancellationRequested)
                                break;
                            VM_Controller.Children.Clear();
                            LoadVMSettings(type);
                        }
                        else
                        {
                            if (ct.IsCancellationRequested)
                                break;
                            VM_Controller.Dispatcher.Invoke(() => { VM_Controller.Children.Clear(); LoadVMSettings(type); });
                        }
                    }
                    Thread.Sleep(1000);
                }
            }, updateVMToken.Token);
        }

        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }
            return null;
        }

        public void vmValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider gain = sender as Slider;
            remoteControle.changeParameter(gain.Uid as string, (float)gain.Value);
            TextBlock value = (TextBlock)GetByUid(VM_Controller, gain.Uid + "_Value");
            value.Text = Math.Round(gain.Value, 2).ToString();
        }

        private void vmToggle(object sender, RoutedEventArgs e)
        {
            Button toggle = sender as Button;
            remoteControle.toggleParameter(toggle.Uid as string);

            if (remoteControle.getBoolParameter(toggle.Uid))
                toggle.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            else
                toggle.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        }
    }
}
