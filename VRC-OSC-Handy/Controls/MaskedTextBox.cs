using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VRC_OSC_Handy.Controls
{
    public class MaskedTextBox : TextBox
    {
        private bool _internalUpdate;
        private readonly Stack<string> _undoStack = new();

        public static readonly DependencyProperty RealTextProperty =
            DependencyProperty.Register(
                nameof(RealText),
                typeof(string),
                typeof(MaskedTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnRealTextChanged));

        public static readonly DependencyProperty IsMaskedProperty =
            DependencyProperty.Register(
                nameof(IsMasked),
                typeof(bool),
                typeof(MaskedTextBox),
                new FrameworkPropertyMetadata(
                    true,
                    OnIsMaskedChanged));

        public static readonly DependencyProperty MaskCharacterProperty =
            DependencyProperty.Register(
                nameof(MaskCharacter),
                typeof(char),
                typeof(MaskedTextBox),
                new FrameworkPropertyMetadata('•'));

        public string RealText
        {
            get => (string)GetValue(RealTextProperty);
            set => SetValue(RealTextProperty, value ?? string.Empty);
        }

        public bool IsMasked
        {
            get => (bool)GetValue(IsMaskedProperty);
            set => SetValue(IsMaskedProperty, value);
        }

        public char MaskCharacter
        {
            get => (char)GetValue(MaskCharacterProperty);
            set => SetValue(MaskCharacterProperty, value);
        }

        public MaskedTextBox()
        {
            TextChanged += MaskedTextBox_TextChanged;
        }

        private static void OnRealTextChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is MaskedTextBox control)
            {
                control.UpdateDisplayedText();
                control.UpdatePlaceholderState();
            }
        }

        private static void OnIsMaskedChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is MaskedTextBox control)
            {
                control.UpdateDisplayedText();
            }
        }

        private void UpdateDisplayedText()
        {
            if (_internalUpdate)
                return;

            _internalUpdate = true;

            try
            {
                string displayText = IsMasked
                    ? new string(MaskCharacter, RealText.Length)
                    : RealText;

                int oldCaret = SelectionStart;
                int oldSelection = SelectionLength;

                Text = displayText;

                SelectionStart = Math.Min(
                    oldCaret,
                    displayText.Length);

                SelectionLength = Math.Min(
                    oldSelection,
                    displayText.Length - SelectionStart);
            }
            finally
            {
                _internalUpdate = false;
            }
        }

        private void MaskedTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_internalUpdate || IsMasked)
                return;

            RealText = Text;
        }

        protected override void OnPreviewTextInput(
            TextCompositionEventArgs e)
        {
            if (!IsMasked)
            {
                base.OnPreviewTextInput(e);
                return;
            }

            ReplaceSelection(e.Text);

            e.Handled = true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsMasked)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            bool ctrl = Keyboard.Modifiers.HasFlag(
                ModifierKeys.Control);

            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.A:
                        SelectAll();
                        e.Handled = true;
                        return;

                    case Key.C:
                        CopyRealText();
                        e.Handled = true;
                        return;

                    case Key.X:
                        CutRealText();
                        e.Handled = true;
                        return;

                    case Key.V:
                        PasteRealText();
                        e.Handled = true;
                        return;

                    case Key.Z:
                        UndoRealText();
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.Back:
                    DeletePreviousCharacter();
                    e.Handled = true;
                    return;

                case Key.Delete:
                    DeleteNextCharacter();
                    e.Handled = true;
                    return;

                case Key.Left:
                    MoveCaretLeft();
                    e.Handled = true;
                    return;

                case Key.Right:
                    MoveCaretRight();
                    e.Handled = true;
                    return;

                case Key.Home:
                    MoveCaretHome();
                    e.Handled = true;
                    return;

                case Key.End:
                    MoveCaretEnd();
                    e.Handled = true;
                    return;
            }

            base.OnPreviewKeyDown(e);
        }

        private void ReplaceSelection(string replacement)
        {
            SaveUndoState();

            int start = SelectionStart;
            int length = SelectionLength;

            string newText = RealText
                .Remove(start, length)
                .Insert(start, replacement);

            RealText = newText;

            UpdateDisplayedText();

            SelectionStart = start + replacement.Length;
            SelectionLength = 0;
        }

        private void DeletePreviousCharacter()
        {
            if (SelectionLength > 0)
            {
                ReplaceSelection(string.Empty);
                return;
            }

            if (SelectionStart <= 0)
                return;

            SaveUndoState();

            int position = SelectionStart;

            RealText = RealText.Remove(position - 1, 1);

            UpdateDisplayedText();

            SelectionStart = position - 1;
            SelectionLength = 0;
        }

        private void DeleteNextCharacter()
        {
            if (SelectionLength > 0)
            {
                ReplaceSelection(string.Empty);
                return;
            }

            if (SelectionStart >= RealText.Length)
                return;

            SaveUndoState();

            int position = SelectionStart;

            RealText = RealText.Remove(position, 1);

            UpdateDisplayedText();

            SelectionStart = position;
            SelectionLength = 0;
        }

        private void MoveCaretLeft()
        {
            if (SelectionLength > 0)
            {
                SelectionLength = 0;
                return;
            }

            if (SelectionStart > 0)
                SelectionStart--;

            SelectionLength = 0;
        }

        private void MoveCaretRight()
        {
            if (SelectionLength > 0)
            {
                SelectionStart += SelectionLength;
                SelectionLength = 0;
                return;
            }

            if (SelectionStart < RealText.Length)
                SelectionStart++;

            SelectionLength = 0;
        }

        private void MoveCaretHome()
        {
            SelectionStart = 0;
            SelectionLength = 0;
        }

        private void MoveCaretEnd()
        {
            SelectionStart = RealText.Length;
            SelectionLength = 0;
        }

        private void CopyRealText()
        {
            if (SelectionLength <= 0)
                return;

            Clipboard.SetText(
                RealText.Substring(
                    SelectionStart,
                    SelectionLength));
        }

        private void CutRealText()
        {
            if (SelectionLength <= 0)
                return;

            SaveUndoState();

            Clipboard.SetText(
                RealText.Substring(
                    SelectionStart,
                    SelectionLength));

            RealText = RealText.Remove(
                SelectionStart,
                SelectionLength);

            UpdateDisplayedText();

            SelectionLength = 0;
        }

        private void PasteRealText()
        {
            if (!Clipboard.ContainsText())
                return;

            ReplaceSelection(
                Clipboard.GetText());
        }

        private void SaveUndoState()
        {
            _undoStack.Push(RealText);
        }

        private void UndoRealText()
        {
            if (_undoStack.Count == 0)
                return;

            RealText = _undoStack.Pop();

            UpdateDisplayedText();

            SelectionStart = RealText.Length;
            SelectionLength = 0;
        }

        public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText),typeof(string),typeof(MaskedTextBox),new FrameworkPropertyMetadata(string.Empty,OnPlaceholderTextChanged));
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MaskedTextBox control)
            {
                control.UpdatePlaceholderState();
            }
        }

        private void UpdatePlaceholderState()
        {
            bool isPlaceholder = RealText == PlaceholderText;

            Foreground = isPlaceholder
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Lime;

            IsMasked = !isPlaceholder;
        }
    }
}