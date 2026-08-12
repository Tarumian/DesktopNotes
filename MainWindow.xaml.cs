using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;

namespace DesktopNotes
{
    public partial class MainWindow : Window
    {
        private bool isRotating = false;

        private double rotationStartAngle;
        private double noteStartAngle;

        private const double CenterRadius = 15;

        private FontFamily currentFontFamily =
            new FontFamily("Arial");

        private double currentFontSize = 18;

        private FontWeight currentFontWeight =
            FontWeights.Normal;

        private FontStyle currentFontStyle =
            FontStyles.Normal;

        private Brush currentForeground =
            new SolidColorBrush(
                Color.FromRgb(34, 34, 34));

        public MainWindow()
        {
            InitializeComponent();

            NoteText.Document.Blocks.Clear();

            Paragraph paragraph =
                new Paragraph();

            Run run =
                new Run("Իմ առաջին թղթիկը");

            run.FontFamily =
                currentFontFamily;

            run.FontSize =
                currentFontSize;

            run.FontWeight =
                currentFontWeight;

            run.FontStyle =
                currentFontStyle;

            run.Foreground =
                currentForeground;

            paragraph.Inlines.Add(run);

            NoteText.Document.Blocks.Add(
                paragraph);
        }

        private void Note_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            CloseButton.Visibility =
                Visibility.Visible;
        }

        private void Note_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (!isRotating &&
                !CloseButton.IsMouseOver)
            {
                CloseButton.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void CloseButton_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            CloseButton.Visibility =
                Visibility.Visible;
        }

        private void CloseButton_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (!Note.IsMouseOver)
            {
                CloseButton.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void MoveArea_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private bool IsInsideCenter(
            Point point)
        {
            double centerX =
                Note.ActualWidth / 2;

            double centerY =
                Note.ActualHeight / 2;

            double dx =
                point.X - centerX;

            double dy =
                point.Y - centerY;

            double distance =
                Math.Sqrt(
                    dx * dx +
                    dy * dy);

            return distance < CenterRadius;
        }

        private bool IsMouseOverText(
            DependencyObject? source)
        {
            if (source == null)
                return false;

            DependencyObject? current =
                source;

            while (current != null)
            {
                if (current == NoteText)
                    return true;

                current =
                    VisualTreeHelper.GetParent(
                        current);
            }

            return false;
        }

        private void Note_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsMouseOverText(
                    e.OriginalSource as DependencyObject))
            {
                return;
            }

            Point point =
                e.GetPosition(Note);

            if (IsInsideCenter(point))
                return;

            StartRotation(e);
        }

        private void StartRotation(
            MouseButtonEventArgs e)
        {
            Point mouse =
                e.GetPosition(MainGrid);

            Point center =
                Note.TranslatePoint(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2),
                    MainGrid);

            rotationStartAngle =
                GetAngle(
                    mouse,
                    center);

            noteStartAngle =
                NoteRotation.Angle;

            isRotating = true;

            Note.CaptureMouse();

            e.Handled = true;
        }

        private void Note_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isRotating)
                return;

            Point mouse =
                e.GetPosition(MainGrid);

            Point center =
                Note.TranslatePoint(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2),
                    MainGrid);

            double currentAngle =
                GetAngle(
                    mouse,
                    center);

            double difference =
                NormalizeAngle(
                    currentAngle -
                    rotationStartAngle);

            NoteRotation.Angle =
                noteStartAngle +
                difference;

            e.Handled = true;
        }

        private void Note_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!isRotating)
                return;

            isRotating = false;

            Note.ReleaseMouseCapture();

            if (!Note.IsMouseOver &&
                !CloseButton.IsMouseOver)
            {
                CloseButton.Visibility =
                    Visibility.Collapsed;
            }

            e.Handled = true;
        }

        private void NoteText_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            NoteContextMenu.PlacementTarget =
                Note;

            NoteContextMenu.IsOpen = true;

            e.Handled = true;
        }

        private void FontFamily_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            string? name =
                item.Header?.ToString();

            if (string.IsNullOrWhiteSpace(name))
                return;

            FontFamily family =
                new FontFamily(name);

            currentFontFamily =
                family;

            if (HasSelection())
            {
                GetSelectedRange()
                    .ApplyPropertyValue(
                        TextElement.FontFamilyProperty,
                        family);
            }
            else
            {
                NoteText.FontFamily =
                    family;
            }
        }

        private void FontSize_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            if (!double.TryParse(
                    item.Tag?.ToString(),
                    out double size))
            {
                return;
            }

            currentFontSize =
                size;

            if (HasSelection())
            {
                GetSelectedRange()
                    .ApplyPropertyValue(
                        TextElement.FontSizeProperty,
                        size);
            }
            else
            {
                NoteText.FontSize =
                    size;
            }
        }

        private void Bold_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            FontWeight weight =
                item.IsChecked
                    ? FontWeights.Bold
                    : FontWeights.Normal;

            currentFontWeight =
                weight;

            ApplyProperty(
                TextElement.FontWeightProperty,
                weight);
        }

        private void Italic_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            FontStyle style =
                item.IsChecked
                    ? FontStyles.Italic
                    : FontStyles.Normal;

            currentFontStyle =
                style;

            ApplyProperty(
                TextElement.FontStyleProperty,
                style);
        }

        private void BoldItalic_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            if (item.IsChecked)
            {
                currentFontWeight =
                    FontWeights.Bold;

                currentFontStyle =
                    FontStyles.Italic;

                ApplyProperty(
                    TextElement.FontWeightProperty,
                    FontWeights.Bold);

                ApplyProperty(
                    TextElement.FontStyleProperty,
                    FontStyles.Italic);
            }
            else
            {
                currentFontWeight =
                    FontWeights.Normal;

                currentFontStyle =
                    FontStyles.Normal;

                ApplyProperty(
                    TextElement.FontWeightProperty,
                    FontWeights.Normal);

                ApplyProperty(
                    TextElement.FontStyleProperty,
                    FontStyles.Normal);
            }
        }

        private void TextColor_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not MenuItem item)
                return;

            string? value =
                item.Tag?.ToString();

            if (string.IsNullOrWhiteSpace(value))
                return;

            Color color =
                (Color)ColorConverter
                    .ConvertFromString(value);

            SolidColorBrush brush =
                new SolidColorBrush(color);

            currentForeground =
                brush;

            ApplyProperty(
                TextElement.ForegroundProperty,
                brush);
        }

        private TextRange GetSelectedRange()
        {
            return new TextRange(
                NoteText.Selection.Start,
                NoteText.Selection.End);
        }

        private bool HasSelection()
        {
            return NoteText.Selection.Start.CompareTo(
                       NoteText.Selection.End) != 0;
        }

        private void ApplyProperty(
            DependencyProperty property,
            object value)
        {
            if (HasSelection())
            {
                GetSelectedRange()
                    .ApplyPropertyValue(
                        property,
                        value);
            }
            else
            {
                NoteText.Selection
                    .ApplyPropertyValue(
                        property,
                        value);
            }
        }

        private static double GetAngle(
            Point point,
            Point center)
        {
            return Math.Atan2(
                       point.Y - center.Y,
                       point.X - center.X)
                   * 180 / Math.PI;
        }

        private static double NormalizeAngle(
            double angle)
        {
            while (angle > 180)
                angle -= 360;

            while (angle < -180)
                angle += 360;

            return angle;
        }
    }
}