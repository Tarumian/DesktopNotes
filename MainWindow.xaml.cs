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
                = new SolidColorBrush(
                    Color.FromRgb(255, 245, 157));

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
        // WINDOW-Ի ԵՐԿՐԱՉԱՓՈՒԹՅՈՒՆ
        // =========================================================

        // Թերթիկի շուրջ նվազագույն ազատ տարածությունը
        // յուրաքանչյուր կողմում։
        private const double WindowPadding = 10;

        // Window-ի չափերը փոխվում են այս քայլերով։
        private const double WindowStep = 20;


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

            UpdateWindowSizeForRotation(false);
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
                    Visibility.Collapsed;

                NextButton.Visibility =
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
            // Մկնիկի դիրքը վերցնում ենք էկրանի կոորդինատներով։
            Point mouse =
                PointToScreen(
                    e.GetPosition(MainGrid));

            // Թերթիկի կենտրոնը նույնպես վերցնում ենք
            // էկրանի կոորդինատներով։
            Point center =
                Note.PointToScreen(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2));

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

            // Մկնիկը եւ կենտրոնը երկուսն էլ
            // նույն՝ էկրանի կոորդինատային համակարգում են։
            Point mouse =
                PointToScreen(
                    e.GetPosition(MainGrid));

            Point center =
                Note.PointToScreen(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2));

            double currentAngle =
                GetAngle(
                    mouse,
                    center);

            double difference =
                NormalizeAngle(
                    currentAngle -
                    rotationStartAngle);

            double newAngle =
                noteStartAngle +
                difference;

            NoteRotation.Angle =
                newAngle;

            // Անհրաժեշտության դեպքում մեծացնում ենք Window-ը։
            // Փոքրացում չենք կատարում։
            UpdateWindowSizeForRotation(true);

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
        // WINDOW-Ի ՉԱՓԸ՝ ՊՏՏՎԱԾ ԹԵՐԹԻԿԻ ՀԱՄԱՐ
        // =========================================================

        private void UpdateWindowSizeForRotation(
            bool keepCenter)
        {
            if (Note.ActualWidth <= 0 ||
                Note.ActualHeight <= 0)
            {
                return;
            }

            double angle =
                NoteRotation.Angle;

            double radians =
                angle * Math.PI / 180.0;

            double cos =
                Math.Abs(
                    Math.Cos(radians));

            double sin =
                Math.Abs(
                    Math.Sin(radians));

            // Պտտված ուղղանկյան bounding box-ը։
            double rotatedWidth =
                Note.ActualWidth * cos +
                Note.ActualHeight * sin;

            double rotatedHeight =
                Note.ActualWidth * sin +
                Note.ActualHeight * cos;

            // 10 px պահուստ յուրաքանչյուր կողմում։
            double requiredWidth =
                rotatedWidth +
                WindowPadding * 2;

            double requiredHeight =
                rotatedHeight +
                WindowPadding * 2;

            // Window-ի չափերը կլորացնում ենք 20 px քայլերի։
            double newWidth =
                RoundUpToStep(
                    requiredWidth,
                    WindowStep);

            double newHeight =
                RoundUpToStep(
                    requiredHeight,
                    WindowStep);

            // Window-ը փոքրացնել չենք անում։
            if (newWidth <= Width &&
                newHeight <= Height)
            {
                return;
            }

            // Պահում ենք թերթիկի կենտրոնի էկրանի
            // կոորդինատը՝ մինչեւ Window-ի չափափոխումը։
            Point centerOnScreen =
                Note.PointToScreen(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2));

            double oldWidth =
                Width;

            double oldHeight =
                Height;

            Width =
                Math.Max(
                    Width,
                    newWidth);

            Height =
                Math.Max(
                    Height,
                    newHeight);

            if (keepCenter)
            {
                // Window-ի չափի փոփոխությունից հետո
                // կենտրոնը վերադարձնում ենք նույն
                // էկրանի կետին։
                Left =
                    centerOnScreen.X -
                    Width / 2;

                Top =
                    centerOnScreen.Y -
                    Height / 2;
            }
            else
            {
                // Սովորական սկզբնական վիճակ։
                if (!double.IsNaN(Left))
                {
                    Left =
                        centerOnScreen.X -
                        Width / 2;
                }

                if (!double.IsNaN(Top))
                {
                    Top =
                        centerOnScreen.Y -
                        Height / 2;
                }
            }
        }


        private static double RoundUpToStep(
            double value,
            double step)
        {
            return Math.Ceiling(
                       value / step)
                   * step;
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
            ApplyFontFamily(
                "Arial");
        }


        private void FontTimes_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontFamily(
                "Times New Roman");
        }


        private void FontCourier_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontFamily(
                "Courier New");
        }


        private void ApplyFontFamily(
            string fontName)
        {
            TextRange range =
                new TextRange(
                    NoteText.Selection.Start,
                    NoteText.Selection.End);

            if (!range.IsEmpty)
            {
                range.ApplyPropertyValue(
                    TextElement.FontFamilyProperty,
                    new FontFamily(fontName));
            }
            else
            {
                NoteText.FontFamily =
                    new FontFamily(fontName);
            }

            SaveCurrentText();

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
                NoteText.FontWeight =
                    weight;
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
                NoteText.FontStyle =
                    style;
            }

            SaveCurrentText();
        }


        // =========================================================
        // ՏԵՔՍՏԻ ԳՈՒՅՆ
        // =========================================================

        private void TextBlack_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(0, 0, 0));
        }


        private void TextDarkGray_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(70, 70, 70));
        }


        private void TextRed_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(180, 0, 0));
        }


        private void TextBlue_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(0, 70, 180));
        }


        private void TextGreen_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(0, 120, 60));
        }


        private void TextBrown_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                Color.FromRgb(120, 70, 30));
        }


        private void SetTextColor(
            Color color)
        {
            SolidColorBrush brush =
                new SolidColorBrush(color);

            TextRange range =
                new TextRange(
                    NoteText.Selection.Start,
                    NoteText.Selection.End);

            if (!range.IsEmpty)
            {
                range.ApplyPropertyValue(
                    TextElement.ForegroundProperty,
                    brush);
            }
            else
            {
                NoteText.Foreground =
                    brush;
            }

            SaveCurrentText();
        }
    }
}