using System;
using VRC_OSC_Handy.Config;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VRC_OSC_Handy
{
    // The small text-box popup used to hand-edit an OSC parameter path when right-
    // clicking an A/B/Mute/Gain control in the VM, Spotify, or Other panel. Split out
    // of MainWindow.xaml.cs since it's a self-contained UI behavior.
    public partial class MainWindow
    {
        // Right-click on an A/B/Mute/Gain control (in any of the three panels: VM,
        // Spotify, or Other) opens a small text box over it so the underlying OSC
        // parameter path can be edited directly. All three call sites only differed in
        // which panel they add to, which side the box opens on, and which
        // ParameterInputUpdate* handler commits the edit.
        private void ShowParameterEditor(UIElement element, Panel controller, HorizontalAlignment alignment, KeyEventHandler onKeyDown)
        {
            string jsonPath = element.Uid.Replace("[", "").Replace("]", "");
            var token = vrcJson.SelectToken(jsonPath);

            Thickness elementMargin = (element as FrameworkElement).Margin;

            TextBox ParameterBox = new TextBox();
            ParameterBox.Text = token.ToString();
            ParameterBox.Uid = jsonPath;
            ParameterBox.Width = 100;
            ParameterBox.Height = 30;
            ParameterBox.HorizontalAlignment = alignment;
            ParameterBox.VerticalAlignment = VerticalAlignment.Top;
            ParameterBox.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
            ParameterBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 107, 109, 113));
            ParameterBox.Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
            ParameterBox.KeyDown += onKeyDown;
            ParameterBox.ContextMenu = null;

            // Get controller bounds
            double controllerWidth = controller.ActualWidth;
            double controllerHeight = controller.ActualHeight;

            // Box position from Margin
            double boxLeft = elementMargin.Left;
            double boxTop = elementMargin.Top;

            // Calculate right & bottom edges of ParameterBox
            double boxRight = elementMargin.Right;
            double boxBottom = elementMargin.Bottom;

            // Check right overflow
            if (boxLeft + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxLeft = controllerWidth - ParameterBox.Width;
            }

            // Check left overflow
            if (boxRight + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxRight = controllerWidth - ParameterBox.Width;
            }

            if (boxBottom + ParameterBox.Width > controllerWidth)
            {
                // Move box to the left of the element (flip horizontally)
                boxBottom = controllerWidth - ParameterBox.Width;
            }

            // Check bottom overflow
            if (boxTop + ParameterBox.Height > controllerHeight)
            {
                // Move box upward (flip vertically)
                boxTop = controllerHeight - ParameterBox.Height;
            }

            // Ensure it doesn't go negative (top-left overrun)
            boxLeft = Math.Max(0, boxLeft);
            boxTop = Math.Max(0, boxTop);
            boxRight = Math.Max(0, boxRight);
            boxBottom = Math.Max(0, boxBottom);

            // Apply corrected Margin
            ParameterBox.Margin = new Thickness(boxLeft, boxTop, boxRight, boxBottom);

            controller.Children.Add(ParameterBox);
            ParameterBox.Focus();
        }

        private void setVRCParameterMV(object sender, MouseButtonEventArgs e) =>
            ShowParameterEditor((UIElement)sender, VM_Controller, HorizontalAlignment.Right, ParameterInputUpdateMV);

        private void setVRCParameterSpotify(object sender, MouseButtonEventArgs e) =>
            ShowParameterEditor((UIElement)sender, Spotify_Controller, HorizontalAlignment.Left, ParameterInputUpdateSpotify);

        private void setVRCParameterOther(object sender, MouseButtonEventArgs e) =>
            ShowParameterEditor((UIElement)sender, Other_Controller, HorizontalAlignment.Left, ParameterInputUpdateOther);

        // Committing (Enter) or cancelling (Escape) an edit from ShowParameterEditor
        // above writes the value back to vrc_config.json and removes the text box.
        // These three differ only in which panel the box came from.
        private void HideParameterEditor(TextBox parameterBox, Panel controller)
        {
            string[] path = parameterBox.Uid.Split('.');
            vrcJson[path[0]][path[1]] = parameterBox.Text;
            vrcConfig = vrcJson.ToObject<VRCParameterConfig>();
            controller.Children.Remove(parameterBox);
        }

        private void ParameterInputUpdateMV(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
                HideParameterEditor((TextBox)sender, VM_Controller);
        }

        private void ParameterInputUpdateSpotify(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
                HideParameterEditor((TextBox)sender, Spotify_Controller);
        }

        private void ParameterInputUpdateOther(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
                HideParameterEditor((TextBox)sender, Other_Controller);
        }
    }
}
