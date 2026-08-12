using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopNotes
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // ԹԵՐԹԻԿԻ ՏՎՅԱԼՆԵՐ
        // =========================================================

        private class NoteData
        {
            public DateTime Created { get; set; }

            public string TextRtf { get; set; } = "";

            public double Width { get; set; }
            public double Height { get; set; }

            public double Left { get; set; }
            public double Top { get; set; }

            public double Rotation { get; set; }

            public Brush Background { get; set; }
                = new SolidColorBrush(Color.FromRgb(255, 245, 157));

            public string FontFamily { get; set; }
                = "Arial";

            public double FontSize { get; set; }
                = 18;

            public FontWeight FontWeight { get; set; }
                = FontWeights.Normal;

            public FontStyle FontStyle { get; set; }
                = FontStyles.Normal;
        }


        // =========================================================
        // ԹԵՐԹԻԿՆԵՐԻ ՑԱՆԿ
        // =========================================================

        private readonly List<NoteData> notes =
            new List<NoteData>();

        private int currentNoteIndex = 0;


        // =========================================================
        // ՊՏՏՈՒՄ
        // =========================================================

        private bool isRotating = false;

        private double rotationStartAngle;
        private double noteStartAngle;

        private const double CenterRadius = 15;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            CreateFirstNote();

            UpdateNavigationButtons();
        }


        // =========================================================
        // ԱՌԱՋԻՆ ԹԵՐԹԻԿ
        // =========================================================

        private void CreateFirstNote()
        {
            NoteData note = new NoteData
            {
                Created = DateTime.Now,
                Width = 300,
                Height = 220,
                Left = double.NaN,
                Top = double.NaN,
                Rotation = 0,
                FontFamily = "Arial",
                FontSize = 18,
                FontWeight = FontWeights.Normal,
                FontStyle = FontStyles.Normal,
                Background =
                    new SolidColorBrush(
                        Color.FromRgb(255, 245, 157))
            };

            notes.Add(note);

            LoadNote(note);

            NoteText.Document.Blocks.Clear();

            Paragraph paragraph =
                new Paragraph();

            paragraph.Inlines.Add(
                new Run("Իմ առաջին թղթիկը"));

            NoteText.Document.Blocks.Add(paragraph);

            SaveCurrentText();
        }


        // =========================================================
        // ՆՈՐ ԹԵՐԹԻԿ
        // =========================================================

        private void NewNote_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveCurrentNote();

            NoteData current =
                notes[currentNoteIndex];

            NoteData newNote = new NoteData
            {
                Created = DateTime.Now,

                // Ժառանգում ենք տեսքը
                Width = current.Width,
                Height = current.Height,

                Left = current.Left,
                Top = current.Top,

                Rotation = current.Rotation,

                Background = current.Background,

                FontFamily = current.FontFamily,
                FontSize = current.FontSize,
                FontWeight = current.FontWeight,
                FontStyle = current.FontStyle,

                // Տեքստը նոր թերթիկում դատարկ է
                TextRtf = ""
            };

            notes.Add(newNote);

            currentNoteIndex =
                notes.Count - 1;

            LoadNote(newNote);

            NoteText.Document.Blocks.Clear();

            Paragraph paragraph =
                new Paragraph();

            NoteText.Document.Blocks.Add(paragraph);

            NoteText.Focus();

            UpdateNavigationButtons();
        }


        // =========================================================
        // ԹԵՐԹԻԿԻ ԲԵՌՆՈՒՄ
        // =========================================================

        private void LoadNote(
            NoteData note)
        {
            Note.Width = note.Width;
            Note.Height = note.Height;

            Note.Background = note.Background;

            NoteRotation.Angle =
                note.Rotation;

            NoteText.FontFamily =
                new FontFamily(note.FontFamily);

            NoteText.FontSize =
                note.FontSize;

            NoteText.FontWeight =
                note.FontWeight;

            NoteText.FontStyle =
                note.FontStyle;

            if (!string.IsNullOrEmpty(note.TextRtf))
            {
                try
                {
                    byte[] bytes =
                        Convert.FromBase64String(
                            note.TextRtf);

                    using MemoryStream stream =
                        new MemoryStream(bytes);

                    TextRange range =
                        new TextRange(
                            NoteText.Document.ContentStart,
                            NoteText.Document.ContentEnd);

                    range.Load(
                        stream,
                        DataFormats.Rtf);
                }
                catch
                {
                    NoteText.Document.Blocks.Clear();
                }
            }
        }


        // =========================================================
        // ՏԵՔՍՏԻ ՊԱՀՊԱՆՈՒՄ
        // =========================================================

        private void SaveCurrentText()
        {
            if (notes.Count == 0)
                return;

            NoteData note =
                notes[currentNoteIndex];

            TextRange range =
                new TextRange(
                    NoteText.Document.ContentStart,
                    NoteText.Document.ContentEnd);

            using MemoryStream stream =
                new MemoryStream();

            range.Save(
                stream,
                DataFormats.Rtf);

            note.TextRtf =
                Convert.ToBase64String(
                    stream.ToArray());
        }


        // =========================================================
        // ԸՆԹԱՑԻԿ ԹԵՐԹԻԿԻ ՊԱՀՊԱՆՈՒՄ
        // =========================================================

        private void SaveCurrentNote()
        {
            if (notes.Count == 0)
                return;

            NoteData note =
                notes[currentNoteIndex];

            SaveCurrentText();

            note.Width =
                Note.Width;

            note.Height =
                Note.Height;

            note.Rotation =
                NoteRotation.Angle;

            note.Background =
                Note.Background;

            note.FontFamily =
                NoteText.FontFamily.Source;

            note.FontSize =
                NoteText.FontSize;

            note.FontWeight =
                NoteText.FontWeight;

            note.FontStyle =
                NoteText.FontStyle;

            note.Left =
                Left;

            note.Top =
                Top;
        }


        // =========================================================
        // ՆԱԽՈՐԴ ԹԵՐԹԻԿ
        // =========================================================

        private void PreviousButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentNoteIndex <= 0)
                return;

            SaveCurrentNote();

            currentNoteIndex--;

            LoadNote(
                notes[currentNoteIndex]);

            UpdateNavigationButtons();
        }


        // =========================================================
        // ՀԱՋՈՐԴ ԹԵՐԹԻԿ
        // =========================================================

        private void NextButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (currentNoteIndex >= notes.Count - 1)
                return;

            SaveCurrentNote();

            currentNoteIndex++;

            LoadNote(
                notes[currentNoteIndex]);

            UpdateNavigationButtons();
        }


        // =========================================================
        // ՆԱՎԻԳԱՑԻԱՅԻ ԿՈՃԱԿՆԵՐ
        // =========================================================

        private void UpdateNavigationButtons()
        {
            if (currentNoteIndex > 0)
                PreviousButton.Visibility =
                    Visibility.Visible;
            else
                PreviousButton.Visibility =
                    Visibility.Collapsed;

            if (currentNoteIndex <
                notes.Count - 1)
                NextButton.Visibility =
                    Visibility.Visible;
            else
                NextButton.Visibility =
                    Visibility.Collapsed;
        }


        // =========================================================
        // HOVER
        // =========================================================

        private void Note_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            CloseButton.Visibility =
                Visibility.Visible;

            UpdateNavigationButtons();
        }


        private void Note_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (!isRotating &&
                !CloseButton.IsMouseOver &&
                !PreviousButton.IsMouseOver &&
                !NextButton.IsMouseOver)
            {
                CloseButton.Visibility =
                    Visibility.Collapsed;

                PreviousButton.Visibility =
                    currentNoteIndex > 0
                        ? Visibility.Collapsed
                        : Visibility.Collapsed;

                NextButton.Visibility =
                    currentNoteIndex <
                    notes.Count - 1
                        ? Visibility.Collapsed
                        : Visibility.Collapsed;
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


        // =========================================================
        // ՓԱԿԵԼ
        // =========================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        private void CloseMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        // =========================================================
        // RIGHT CLICK
        // =========================================================

        private void Note_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            OpenNoteContextMenu(e);
        }


        private void NoteText_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            OpenNoteContextMenu(e);
        }


        private void OpenNoteContextMenu(
            MouseButtonEventArgs e)
        {
            NoteContextMenu.PlacementTarget =
                Note;

            NoteContextMenu.IsOpen =
                true;

            e.Handled = true;
        }


        private void NoteContextMenu_Opened(
            object sender,
            RoutedEventArgs e)
        {
            SaveCurrentText();
        }


        // =========================================================
        // MOVE
        // =========================================================

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


        // =========================================================
        // ԿԵՆՏՐՈՆԱԿԱՆ ՇՐՋԱՆ
        // =========================================================

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

            return distance <
                   CenterRadius;
        }


        // =========================================================
        // ԻՆՏԵՐԱԿՏԻՎ ՏԱՐՐԻ ՎՐԱ՞ ԵՆՔ
        // =========================================================

        private bool IsMouseOverInteractive(
            DependencyObject? source)
        {
            if (source == null)
                return false;

            DependencyObject? current =
                source;

            while (current != null)
            {
                if (current == NoteText ||
                    current == CloseButton ||
                    current == PreviousButton ||
                    current == NextButton ||
                    current == MoveArea)
                {
                    return true;
                }

                current =
                    VisualTreeHelper.GetParent(
                        current);
            }

            return false;
        }


        // =========================================================
        // ԹԵՐԹԻԿԻ ՎՐԱ ՍԵՂՄՈՒՄ
        // =========================================================

        private void Note_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (IsMouseOverInteractive(
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


        // =========================================================
        // ROTATION
        // =========================================================

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

            SaveCurrentNote();

            e.Handled = true;
        }


        // =========================================================
        // ԱՆԿՅՈՒՆ
        // =========================================================

        private static double GetAngle(
            Point point,
            Point center)
        {
            return Math.Atan2(
                       point.Y - center.Y,
                       point.X - center.X)
                   * 180 /
                   Math.PI;
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


        // =========================================================
        // ՏԱՌԱՏԵՍԱԿ
        // =========================================================

        private void FontArial_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteText.FontFamily =
                new FontFamily("Arial");

            SaveCurrentNote();
        }


        private void FontTimes_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteText.FontFamily =
                new FontFamily("Times New Roman");

            SaveCurrentNote();
        }


        private void FontCourier_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteText.FontFamily =
                new FontFamily("Courier New");

            SaveCurrentNote();
        }


        // =========================================================
        // ԿԵՏԱՉԱՓ
        // =========================================================

        private void FontSize14_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(14);
        }


        private void FontSize16_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(16);
        }


        private void FontSize18_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(18);
        }


        private void FontSize20_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(20);
        }


        private void FontSize24_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(24);
        }


        private void FontSize28_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetFontSize(28);
        }


        private void SetFontSize(
            double size)
        {
            TextRange range =
                new TextRange(
                    NoteText.Selection.Start,
                    NoteText.Selection.End);

            if (!range.IsEmpty)
            {
                range.ApplyPropertyValue(
                    TextElement.FontSizeProperty,
                    size);
            }
            else
            {
                NoteText.FontSize = size;
            }

            SaveCurrentText();
        }


        // =========================================================
        // ԹԱՎ
        // =========================================================

        private void Bold_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontWeight(
                FontWeights.Bold);
        }


        // =========================================================
        // ՇԵՂ
        // =========================================================

        private void Italic_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontStyle(
                FontStyles.Italic);
        }


        // =========================================================
        // ԹԱՎ ՇԵՂ
        // =========================================================

        private void BoldItalic_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontWeight(
                FontWeights.Bold);

            ApplyFontStyle(
                FontStyles.Italic);
        }


        // =========================================================
        // ՍՈՎՈՐԱԿԱՆ
        // =========================================================

        private void Normal_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontWeight(
                FontWeights.Normal);

            ApplyFontStyle(
                FontStyles.Normal);
        }


        private void ApplyFontWeight(
            FontWeight weight)
        {
            TextRange range =
                new TextRange(
                    NoteText.Selection.Start,
                    NoteText.Selection.End);

            if (!range.IsEmpty)
            {
                range.ApplyPropertyValue(
                    TextElement.FontWeightProperty,
                    weight);
            }
            else
            {
                NoteText.FontWeight = weight;
            }

            SaveCurrentText();
        }


        private void ApplyFontStyle(
            FontStyle style)
        {
            TextRange range =
                new TextRange(
                    NoteText.Selection.Start,
                    NoteText.Selection.End);

            if (!range.IsEmpty)
            {
                range.ApplyPropertyValue(
                    TextElement.FontStyleProperty,
                    style);
            }
            else
            {
                NoteText.FontStyle = style;
            }

            SaveCurrentText();
        }
    }
}