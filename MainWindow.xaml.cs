using System;
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
    private NoteData Data;

    // =========================================================
    // WINDOW-Ի ԵՐԿՐԱՉԱՓՈՒԹՅՈՒՆ
    // =========================================================

        private const double WindowPadding = 10;
        private const double WindowStep = 20;


        // =========================================================
        // ՊՏՏՈՒՄ
        // =========================================================

        private bool isRotating = false;

        private double rotationStartAngle;
        private double noteStartAngle;

        private const double CenterRadius = 15;


        // =========================================================
        // ՏԵՂԱՓՈԽՈՒՄ
        // =========================================================

        private bool isMoving = false;

        private Point moveStartMouseScreen;
        private double moveStartLeft;
        private double moveStartTop;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            Data = new NoteData();

            ((App)Application.Current).Store.Notes[Data.Id] = Data;

            Note.Width =
    ((App)Application.Current).Store.NoteWidth;

Note.Height =
    ((App)Application.Current).Store.NoteHeight;

            NoteRotation.Angle = 0;

            NoteText.FontFamily =
                new FontFamily("Comic Sans MS");

            NoteText.FontSize = 14;

            NoteText.FontWeight =
                FontWeights.Normal;

            NoteText.FontStyle =
                FontStyles.Normal;

            NoteText.Document.Blocks.Clear();

            Paragraph paragraph =
                new Paragraph();

            paragraph.Inlines.Add(
                new Run("Իմ առաջին թղթիկը"));

            NoteText.Document.Blocks.Add(paragraph);

            // -----------------------------------------------------
            // Այս Window-ը taskbar-ի միակ ներկայացուցիչն է։
            // -----------------------------------------------------

            ShowInTaskbar = true;

            Loaded += MainWindow_Loaded;
        }


        // =========================================================
// NOTE DATA — WINDOW-Ի ԸՆԹԱՑԻԿ ՎԻՃԱԿԸ
// =========================================================

private void UpdateDataFromWindow()
{
    Data.Width = Note.Width;
    Data.Height = Note.Height;

    Data.Left = Left;
    Data.Top = Top;

    Data.Rotation = NoteRotation.Angle;

    Data.NoteColor =
        (Note.Background as SolidColorBrush)?.Color.ToString()
        ?? "#FFF9A6";

    Data.FontFamily =
        NoteText.FontFamily.Source;

    Data.FontSize =
        NoteText.FontSize;

    Data.IsBold =
        NoteText.FontWeight == FontWeights.Bold;

    Data.IsItalic =
        NoteText.FontStyle == FontStyles.Italic;

    Data.TextColor =
        (NoteText.Foreground as SolidColorBrush)?.Color.ToString()
        ?? "#000000";

    TextRange range =
        new TextRange(
            NoteText.Document.ContentStart,
            NoteText.Document.ContentEnd);

    Data.Text =
        range.Text.TrimEnd('\r', '\n');

    using (var stream =
           new MemoryStream())
    {
        range.Save(
            stream,
            DataFormats.Rtf);

        stream.Position = 0;

        using (var reader =
               new StreamReader(stream))
        {
            Data.RtfContent =
                reader.ReadToEnd();
        }
    }
}

// =========================================================
// ԹԵՐԹԻԿՆԵՐԻ ԸՆԴՀԱՆՈՒՐ ՉԱՓ
// =========================================================

private void ApplyNoteSize(
    double width,
    double height)
{
    // -----------------------------------------------------
    // Պահում ենք նոր ընդհանուր չափը
    // -----------------------------------------------------

    NoteStore store =
        ((App)Application.Current).Store;

    store.NoteWidth = width;
    store.NoteHeight = height;


    // -----------------------------------------------------
    // Չափափոխում ենք բոլոր բաց թերթիկները
    // -----------------------------------------------------

    foreach (Window window in
             Application.Current.Windows)
    {
        if (window is MainWindow noteWindow)
        {
            noteWindow.Note.Width = width;
            noteWindow.Note.Height = height;

            noteWindow.UpdateWindowSizeForRotation(false);

            noteWindow.UpdateDataFromWindow();
        }
    }
}

private void Size150x220_Click(
    object sender,
    RoutedEventArgs e)
{
    ApplyNoteSize(150, 220);
}

        // =========================================================
        // WINDOW LOADED
        // =========================================================

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            UpdateWindowSizeForRotation(false);
        }


        // =========================================================
        // ՆՈՐ ԹԵՐԹԻԿ
        // =========================================================

        private void NewNote_Click(
            object sender,
            RoutedEventArgs e)
        {
            // -----------------------------------------------------
            // Ընթացիկ թերթիկի կենտրոնը՝ էկրանի կոորդինատներով։
            // -----------------------------------------------------

            Point centerOnScreen =
                Note.PointToScreen(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2));


            // -----------------------------------------------------
            // Ստեղծում ենք լրիվ նոր Window instance։
            // -----------------------------------------------------

            MainWindow newNote =
                new MainWindow();


            // -----------------------------------------------------
            // Նոր թերթիկը առանձին taskbar կոճակ ՉՈՒՆԻ։
            // -----------------------------------------------------

            newNote.ShowInTaskbar = false;


            // -----------------------------------------------------
            // Ժառանգում ենք ընթացիկ թերթիկի տեսքը։
            // -----------------------------------------------------

            newNote.Note.Width =
                Note.Width;

            newNote.Note.Height =
                Note.Height;

            newNote.Note.Background =
                Note.Background;

            newNote.NoteRotation.Angle =
                NoteRotation.Angle;

            newNote.NoteText.FontFamily =
                NoteText.FontFamily;

            newNote.NoteText.FontSize =
                NoteText.FontSize;

            newNote.NoteText.FontWeight =
                NoteText.FontWeight;

            newNote.NoteText.FontStyle =
                NoteText.FontStyle;


            // -----------------------------------------------------
            // Նոր թերթիկը դատարկ է։
            // -----------------------------------------------------

            newNote.NoteText.Document.Blocks.Clear();

            Paragraph paragraph =
                new Paragraph();

            newNote.NoteText.Document.Blocks.Add(
                paragraph);


            // -----------------------------------------------------
            // Window-ը չափափոխում ենք նոր թերթիկի համար։
            // -----------------------------------------------------

            newNote.UpdateWindowSizeForRotation(false);


            // -----------------------------------------------------
            // Նոր Window-ի թերթիկի կենտրոնը
            // դնում ենք հնի կենտրոնի վրա։
            // -----------------------------------------------------

            newNote.Left =
                centerOnScreen.X -
                newNote.Width / 2;

            newNote.Top =
                centerOnScreen.Y -
                newNote.Height / 2;


            // -----------------------------------------------------
            // Ցուցադրում ենք նոր պատուհանը։
            // -----------------------------------------------------

            newNote.Show();

            newNote.Activate();

            newNote.NoteText.Focus();
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

    EditButtons.Visibility =
        Visibility.Visible;
}


        private void Note_MouseLeave(
    object sender,
    MouseEventArgs e)
{
    if (!isRotating &&
        !isMoving &&
        !CloseButton.IsMouseOver)
    {
        CloseButton.Visibility =
            Visibility.Collapsed;
    }

    if (!isRotating &&
        !isMoving &&
        !CloseButton.IsMouseOver)
    {
        EditButtons.Visibility =
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

private void NewButton_MouseEnter(
    object sender,
    MouseEventArgs e)
{
    NewButton.Foreground =
        Brushes.Black;

    NewButton.FontWeight =
        FontWeights.ExtraBold;
}


private void NewButton_MouseLeave(
    object sender,
    MouseEventArgs e)
{
    NewButton.Foreground =
        new SolidColorBrush(
            Color.FromRgb(85, 85, 85));

    NewButton.FontWeight =
        FontWeights.Normal;
}


private void DeleteButton_MouseEnter(
    object sender,
    MouseEventArgs e)
{
    if (!DeleteButton.IsEnabled)
        return;

    DeleteButton.Foreground =
        Brushes.Black;

    DeleteButton.FontWeight =
        FontWeights.ExtraBold;
}


private void DeleteButton_MouseLeave(
    object sender,
    MouseEventArgs e)
{
    DeleteButton.Foreground =
        new SolidColorBrush(
            Color.FromRgb(85, 85, 85));

    DeleteButton.FontWeight =
        FontWeights.Normal;
}

private void DeleteButton_Click(
    object sender,
    RoutedEventArgs e)
{
    NoteStore store =
        ((App)Application.Current).Store;

    store.DeletedNotes[Data.Id] =
        Data;

    store.Notes.Remove(
        Data.Id);

    Close();
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

        private void UndoDelete_Click(
    object sender,
    RoutedEventArgs e)
{
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
        }


        // =========================================================
        // MOVE START
        // =========================================================

        private void MoveArea_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            Point mouseScreen =
                PointToScreen(
                    e.GetPosition(MoveArea));

            moveStartMouseScreen =
                mouseScreen;

            moveStartLeft =
                Left;

            moveStartTop =
                Top;

            isMoving = true;

            MoveArea.CaptureMouse();

            e.Handled = true;
        }


        // =========================================================
        // MOVE
        // =========================================================

        private void MoveNote(
            MouseEventArgs e)
        {
            if (!isMoving)
                return;

            Point mouseScreen =
                PointToScreen(
                    e.GetPosition(MoveArea));

            double deltaX =
                mouseScreen.X -
                moveStartMouseScreen.X;

            double deltaY =
                mouseScreen.Y -
                moveStartMouseScreen.Y;

            // -----------------------------------------------------
            // ՍԱ ԴԻՏԱՎՈՐՅԱԼ ՉԵՆՔ ՓՈԽՈՒՄ։
            // Top-ը կարող է լինել բացասական։
            // -----------------------------------------------------

            Left =
                moveStartLeft +
                deltaX;

            Top =
                moveStartTop +
                deltaY;
        }


        // =========================================================
        // MOVE END
        // =========================================================

        private void MoveNoteEnd()
        {
            if (!isMoving)
                return;

            isMoving = false;

            if (MoveArea.IsMouseCaptured)
                MoveArea.ReleaseMouseCapture();

            UpdateDataFromWindow();

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
        // ԻՆՏԵՐԱԿՏԻՎ ՏԱՐՐ
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
        // MOUSE MOVE
        // =========================================================

        private void Note_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            // -----------------------------------------------------
            // Նախ՝ տեղափոխում։
            // -----------------------------------------------------

            if (isMoving)
            {
                MoveNote(e);

                e.Handled = true;

                return;
            }


            // -----------------------------------------------------
            // Հետո՝ պտտում։
            // -----------------------------------------------------

            if (!isRotating)
                return;

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

            UpdateWindowSizeForRotation(true);

            e.Handled = true;
        }


        // =========================================================
        // MOVE / ROTATION END
        // =========================================================

        private void Note_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (isMoving)
            {
                MoveNoteEnd();

                e.Handled = true;

                return;
            }

            if (!isRotating)
                return;

            isRotating = false;

            Note.ReleaseMouseCapture();

            UpdateDataFromWindow();

            e.Handled = true;
        }


        // =========================================================
        // ROTATION START
        // =========================================================

        private void StartRotation(
            MouseButtonEventArgs e)
        {
            Point mouse =
                PointToScreen(
                    e.GetPosition(MainGrid));

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
                angle *
                Math.PI /
                180.0;

            double cos =
                Math.Abs(
                    Math.Cos(radians));

            double sin =
                Math.Abs(
                    Math.Sin(radians));

            double rotatedWidth =
                Note.ActualWidth * cos +
                Note.ActualHeight * sin;

            double rotatedHeight =
                Note.ActualWidth * sin +
                Note.ActualHeight * cos;

            double requiredWidth =
                rotatedWidth +
                WindowPadding * 2;

            double requiredHeight =
                rotatedHeight +
                WindowPadding * 2;

            double newWidth =
                RoundUpToStep(
                    requiredWidth,
                    WindowStep);

            double newHeight =
                RoundUpToStep(
                    requiredHeight,
                    WindowStep);

            if (newWidth <= Width &&
                newHeight <= Height)
            {
                return;
            }

            Point centerOnScreen =
                Note.PointToScreen(
                    new Point(
                        Note.ActualWidth / 2,
                        Note.ActualHeight / 2));

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
                Left =
                    centerOnScreen.X -
                    Width / 2;

                Top =
                    centerOnScreen.Y -
                    Height / 2;
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
            ApplyFontFamily("Arial");
        }


        private void FontTimes_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontFamily("Times New Roman");
        }


        private void FontCourier_Click(
            object sender,
            RoutedEventArgs e)
        {
            ApplyFontFamily("Courier New");
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
                NoteText.FontSize =
                    size;
            }
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
        }
    }
}