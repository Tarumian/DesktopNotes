using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;


namespace DesktopNotes
{
    public partial class MainWindow : Window
{
    
    private TextPointer? selectionAnchor;

    private Point selectionStartPoint;

    private bool isTextSelecting;

    private DispatcherTimer? textSaveTimer;    
    
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

    InitializeNoteVisuals();

    InitializeTextSaveTimer();

    // -----------------------------------------------------
    // Այս Window-ը taskbar-ի միակ ներկայացուցիչն է։
    // -----------------------------------------------------

    ShowInTaskbar = true;

    Loaded += MainWindow_Loaded;
}


private void ApplyDataToWindow()
{
    // =====================================================
    // ՉԱՓԵՐ ԵՒ ԴԻՐՔ
    // =====================================================

    Note.Width =
        Data.Width;

    Note.Height =
        Data.Height;

    Left =
        Data.Left;

    Top =
        Data.Top;


    // =====================================================
    // ՊՏՏՈՒՄ
    // =====================================================

    NoteRotation.Angle =
        Data.Rotation;


    // =====================================================
    // ԹԵՐԹԻԿԻ ԳՈՒՅՆ
    // =====================================================

    Note.Background =
        new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(
                Data.NoteColor));


    // =====================================================
    // ՏԱՌԱՏԵՍԱԿ
    // =====================================================

    NoteText.FontFamily =
        new FontFamily(
            Data.FontFamily);


    // =====================================================
    // ԿԵՏԱՉԱՓ
    // =====================================================

    NoteText.FontSize =
        Data.FontSize;


    // =====================================================
    // ԹԱՎ
    // =====================================================

    NoteText.FontWeight =
        Data.IsBold
            ? FontWeights.Bold
            : FontWeights.Normal;


    // =====================================================
    // ՇԵՂ
    // =====================================================

    NoteText.FontStyle =
        Data.IsItalic
            ? FontStyles.Italic
            : FontStyles.Normal;


    // =====================================================
    // ՏԵՔՍՏԻ ԳՈՒՅՆ
    // =====================================================

    NoteText.Foreground =
        new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(
                Data.TextColor));


    // =====================================================
    // RTF
    // =====================================================

    if (!string.IsNullOrEmpty(Data.RtfContent))
    {
        using (MemoryStream stream =
               new MemoryStream(
                   Encoding.UTF8.GetBytes(
                       Data.RtfContent)))
        {
            TextRange range =
                new TextRange(
                    NoteText.Document.ContentStart,
                    NoteText.Document.ContentEnd);

            range.Load(
                stream,
                DataFormats.Rtf);
        }
    }
    else
    {
        NoteText.Document.Blocks.Clear();

        Paragraph paragraph =
            new Paragraph();

        paragraph.Inlines.Add(
            new Run(Data.Text));

        NoteText.Document.Blocks.Add(
            paragraph);
    }
}


private void NoteText_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
{
    selectionStartPoint =
        e.GetPosition(NoteText);

    selectionAnchor =
        NoteText.GetPositionFromPoint(
            selectionStartPoint,
            true);

    isTextSelecting = false;
}


private void NoteText_PreviewMouseMove(
    object sender,
    MouseEventArgs e)
{
    if (selectionAnchor == null ||
        e.LeftButton != MouseButtonState.Pressed)
        return;

    Point currentPoint =
        e.GetPosition(NoteText);

    double dx =
        Math.Abs(
            currentPoint.X -
            selectionStartPoint.X);

    double dy =
        Math.Abs(
            currentPoint.Y -
            selectionStartPoint.Y);

    // -----------------------------------------------------
    // Սկսում ենք ընտրությունը միայն փոքր տեղաշարժից հետո
    // -----------------------------------------------------

    if (!isTextSelecting)
    {
        if (dx < 2 && dy < 2)
            return;

        isTextSelecting = true;

        NoteText.CaptureMouse();
    }

    TextPointer currentPosition =
        NoteText.GetPositionFromPoint(
            currentPoint,
            true);

    if (currentPosition == null)
        return;

    NoteText.Selection.Select(
        selectionAnchor,
        currentPosition);

    e.Handled = true;
}


private void NoteText_PreviewMouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs e)
{
    if (isTextSelecting)
    {
        e.Handled = true;

        NoteText.ReleaseMouseCapture();
    }

    selectionAnchor = null;
    isTextSelecting = false;
}


private void InitializeNoteVisuals()
{
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

NoteText.Document.Blocks.Add(
    new Paragraph());
}

public MainWindow(
    NoteData existingData)
{
    InitializeComponent();

    Data = existingData;

    ApplyDataToWindow();

    ShowInTaskbar = false;

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

private void InitializeTextSaveTimer()
{
    textSaveTimer =
        new DispatcherTimer
        {
            Interval =
                TimeSpan.FromMilliseconds(500)
        };

    textSaveTimer.Tick +=
        TextSaveTimer_Tick;
}


private void NoteText_TextChanged(
    object sender,
    TextChangedEventArgs e)
{
    if (textSaveTimer == null)
        return;

    textSaveTimer.Stop();
    textSaveTimer.Start();
}


private void TextSaveTimer_Tick(
    object? sender,
    EventArgs e)
{
    if (textSaveTimer == null)
        return;

    textSaveTimer.Stop();

    SaveCurrentState();
}

private void SaveCurrentState()
{
    UpdateDataFromWindow();

    DnoteStorage.Save(
        ((App)Application.Current).Store);
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

    DnoteStorage.Save(store);

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

            newNote.SaveCurrentState();

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
    // -----------------------------------------------------
    // Նախ թարմացնում ենք Data-ն էկրանի իրական վիճակից
    // -----------------------------------------------------

    SaveCurrentState();


    // -----------------------------------------------------
    // Կենտրոնական պահոցը
    // -----------------------------------------------------

    NoteStore store =
        ((App)Application.Current).Store;


    // -----------------------------------------------------
    // Պահում ենք ջնջված թերթիկը Undo-ի համար
    // -----------------------------------------------------

    store.DeletedNotes[Data.Id] =
        Data;


    // -----------------------------------------------------
    // Հեռացնում ենք գործող թերթիկների պահոցից
    // -----------------------------------------------------

    store.Notes.Remove(
        Data.Id);


    // -----------------------------------------------------
    // Փակում ենք թերթիկը
    // -----------------------------------------------------

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
    NoteStore store =
        ((App)Application.Current).Store;

    if (store.DeletedNotes.Count == 0)
        return;


    // -----------------------------------------------------
    // Վերցնում ենք վերջին ջնջված թերթիկը
    // -----------------------------------------------------

    Guid id =
        new List<Guid>(
            store.DeletedNotes.Keys)[
                store.DeletedNotes.Count - 1];

    NoteData restoredData =
        store.DeletedNotes[id];


    // -----------------------------------------------------
    // Վերադարձնում ենք տվյալը հիմնական պահոց
    // -----------------------------------------------------

    store.DeletedNotes.Remove(id);

    store.Notes[id] =
        restoredData;


    // -----------------------------------------------------
    // Ստեղծում ենք պատուհանը՝ արդեն գոյություն ունեցող տվյալով
    // -----------------------------------------------------

    MainWindow restoredNote =
        new MainWindow(restoredData);


    // -----------------------------------------------------
    // Վերականգնում ենք թերթիկի տեսողական հատկությունները
    // -----------------------------------------------------

    restoredNote.Note.Width =
        restoredData.Width;

    restoredNote.Note.Height =
        restoredData.Height;

    restoredNote.Left =
        restoredData.Left;

    restoredNote.Top =
        restoredData.Top;

    restoredNote.NoteRotation.Angle =
        restoredData.Rotation;

    restoredNote.Note.Background =
        new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(
                restoredData.NoteColor));

    restoredNote.NoteText.FontFamily =
        new FontFamily(
            restoredData.FontFamily);

    restoredNote.NoteText.FontSize =
        restoredData.FontSize;

    restoredNote.NoteText.FontWeight =
        restoredData.IsBold
            ? FontWeights.Bold
            : FontWeights.Normal;

    restoredNote.NoteText.FontStyle =
        restoredData.IsItalic
            ? FontStyles.Italic
            : FontStyles.Normal;


    // -----------------------------------------------------
    // Վերականգնում ենք տեքստը
    // -----------------------------------------------------

    using (MemoryStream stream =
       new MemoryStream(
           Encoding.UTF8.GetBytes(
               restoredData.RtfContent)))
{
    TextRange range =
        new TextRange(
            restoredNote.NoteText.Document.ContentStart,
            restoredNote.NoteText.Document.ContentEnd);

    range.Load(
        stream,
        DataFormats.Rtf);
}


    // -----------------------------------------------------
    // Ցուցադրում ենք
    // -----------------------------------------------------

    restoredNote.ShowInTaskbar =
        false;

    restoredNote.Show();

    restoredNote.Activate();

    SaveCurrentState();
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
    NoteStore store =
        ((App)Application.Current).Store;

    UndoDeleteMenuItem.IsEnabled =
        store.DeletedNotes.Count > 0;
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

            SaveCurrentState();

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

            SaveCurrentState();

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