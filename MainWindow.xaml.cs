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
using System.Linq;
using Forms = System.Windows.Forms;


namespace DesktopNotes
{
    public partial class MainWindow : Window
{
    
    private TextPointer? selectionAnchor;

    private Point selectionStartPoint;

    private bool isTextSelecting;

    private DispatcherTimer? textSaveTimer;    

    // Ջնջման փակումը չպետք է վերածվի սովորական
    // «փակել ու պահել» գործողության։
    private bool isDeleting;
    
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

public Guid NoteId => Data.Id;

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

UpdateCompleteToolTip(); 

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
    NoteStore currentStore = ((App)Application.Current).Store;

    Data.Width = currentStore.NoteWidth;
    Data.Height = currentStore.NoteHeight;
    Data.NoteColor = currentStore.NoteColor;
    Data.TextColor = currentStore.TextColor;
    Data.FontFamily = currentStore.FontFamily;
    Data.FontSize = currentStore.FontSize;
    Data.IsBold = currentStore.IsBold;
    Data.IsItalic = currentStore.IsItalic;

    Note.Width = Data.Width;
    Note.Height = Data.Height;
    NoteRotation.Angle = 0;

    Note.Background =
        new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(
                Data.NoteColor));

    NoteText.FontFamily =
        new FontFamily(Data.FontFamily);

    NoteText.FontSize = Data.FontSize;

    NoteText.FontWeight =
        Data.IsBold ? FontWeights.Bold : FontWeights.Normal;

    NoteText.FontStyle =
        Data.IsItalic ? FontStyles.Italic : FontStyles.Normal;

    NoteText.Foreground =
        new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(
                Data.TextColor));

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

    InitializeTextSaveTimer();

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

private void SetCurrentNoteSize(
    double width,
    double height)
{
    Data.Width = width;
    Data.Height = height;

    Note.Width = width;
    Note.Height = height;

    UpdateWindowSizeForRotation(false);
    UpdateDataFromWindow();

    DnoteStorage.Save(((App)Application.Current).Store);
}

private void Size150x220_Click(
    object sender,
    RoutedEventArgs e)
{
    SetCurrentNoteSize(150, 220);
}

private void Size300x220_Click(
    object sender,
    RoutedEventArgs e)
{
    SetCurrentNoteSize(300, 220);
}

private void Size150x450_Click(
    object sender,
    RoutedEventArgs e)
{
    SetCurrentNoteSize(150, 450);
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
            UpdateResizeGripState();
        }

        public void UpdateResizeGripState()
        {
            bool allow = ((App)Application.Current).Store.AllowFreeResize;
            ResizeGrip.Visibility = allow ? Visibility.Visible : Visibility.Collapsed;
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


        private void SearchNotes_Click(
    object sender,
    RoutedEventArgs e)
{
    SearchWindow[] searchWindows =
        Application.Current.Windows
            .OfType<SearchWindow>()
            .ToArray();

    if (searchWindows.Length > 0)
    {
        searchWindows[0].Show();
        searchWindows[0].Activate();
        return;
    }

    SearchWindow searchWindow =
        new SearchWindow();

    searchWindow.Show();
    searchWindow.Activate();
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
            ColorFromHex("#555555"));

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
            ColorFromHex("#555555"));

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

    UpdateDataFromWindow();


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

    // Միայն հիմա ենք գրում պահոցը, որպեսզի ֆայլում
    // թերթիկն այլեւս գործող Notes-ում չմնա։
    isDeleting = true;

    DnoteStorage.Save(store);


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
    Data.IsClosed = true;

    SaveCurrentState();

    Close();
}


private void CloseMenu_Click(
    object sender,
    RoutedEventArgs e)
{
    Data.IsClosed = true;

    SaveCurrentState();

    Close();
}


protected override void OnClosing(
    System.ComponentModel.CancelEventArgs e)
{
    // Սա ներառում է նաեւ ծրագրային Close()-ը (օր.՝
    // «Փակել բոլոր թերթիկները»)։ Ջնջման դեպքում տվյալն
    // արդեն հեռացվել ու պահպանվել է DeleteButton_Click-ում։
    if (!isDeleting)
    {
        Data.IsClosed = true;
        SaveCurrentState();
    }

    base.OnClosing(e);
}

private void ExportExcel_Click(
    object sender,
    RoutedEventArgs e)
{
    // Նախ պահում ենք բոլոր բաց թերթիկների
    // տվյալների վերջին վիճակը։
    foreach (Window window in Application.Current.Windows)
    {
        if (window is MainWindow noteWindow)
        {
            noteWindow.SaveCurrentState();
        }
    }

    NoteStore store =
        ((App)Application.Current).Store;

    Microsoft.Win32.SaveFileDialog dialog =
        new Microsoft.Win32.SaveFileDialog
        {
            Title = "Արտահանել Excel",
            Filter = "Excel ֆայլ (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            FileName =
                "DesktopNotes_" +
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
        };

    if (dialog.ShowDialog() != true)
        return;

    try
    {
        using (var workbook =
               new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet =
                workbook.Worksheets.Add("Թերթիկներ");

            // Վերնագրեր
            worksheet.Cell(1, 1).Value =
                "Ստեղծման ամսաթիվ";

            worksheet.Cell(1, 2).Value =
                "Գրառում";

            worksheet.Cell(1, 3).Value =
                "Լուծված";

            worksheet.Cell(1, 4).Value =
                "Լուծման ամսաթիվ";

            // Վերնագրերի ձեւավորում
            var headerRange =
                worksheet.Range(1, 1, 1, 4);

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Fill.BackgroundColor =
                ClosedXML.Excel.XLColor.LightGray;

            // Թերթիկները՝ ստեղծման հերթականությամբ
            var notes =
                store.Notes.Values
                    .OrderBy(note => note.CreatedAt)
                    .ToList();

            int row = 2;

            foreach (NoteData note in notes)
            {
                worksheet.Cell(row, 1).Value =
                    note.CreatedAt;

                worksheet.Cell(row, 1)
                    .Style.DateFormat.Format =
                    "dd.MM.yyyy HH:mm";

                worksheet.Cell(row, 2).Value =
                    note.Text;

                worksheet.Cell(row, 3).Value =
                    note.IsCompleted
                        ? "Այո"
                        : "";

                if (note.CompletedDate.HasValue)
                {
                    worksheet.Cell(row, 4).Value =
                        note.CompletedDate.Value;

                    worksheet.Cell(row, 4)
                        .Style.DateFormat.Format =
                        "dd.MM.yyyy HH:mm";
                }

                row++;
            }

            // Տեքստի տեղափոխում հաջորդ տող
            worksheet.Column(2)
                .Style.Alignment.WrapText = true;

            // Սյունակների լայնությունը
            worksheet.Column(1).Width = 20;
            worksheet.Column(2).Width = 60;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 20;

            // Ֆիլտր
            if (row > 1)
            {
                worksheet.Range(
                    1, 1,
                    row - 1, 4)
                    .SetAutoFilter();
            }

            // Վերնագիրը միշտ տեսանելի
            worksheet.SheetView.FreezeRows(1);

            workbook.SaveAs(dialog.FileName);
        }

        MessageBox.Show(
            "Excel ֆայլը հաջողությամբ ստեղծվեց։",
            "DesktopNotes");
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            "Excel ֆայլը ստեղծել չհաջողվեց։\n\n" +
            ex.Message,
            "DesktopNotes",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
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
                    current == MoveArea ||
                    current == ResizeGrip)
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


// =================================================
// Որոնման պատուհանը առաջ բերել
// =================================================

private void NoteWindow_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
{
    SearchWindow[] searchWindows =
        Application.Current.Windows
            .OfType<SearchWindow>()
            .ToArray();

    if (searchWindows.Length == 0)
        return;

    SearchWindow searchWindow = searchWindows[0];

    if (!searchWindow.IsVisible)
        searchWindow.Show();

    searchWindow.Activate();
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
        // ԱԶԱՏ ՉԱՓԱՓՈԽՈՒՄ (RESIZE)
        // =========================================================

        private bool isResizing = false;

        private void ResizeGrip_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!((App)Application.Current).Store.AllowFreeResize)
                return;

            isResizing = true;
            ResizeGrip.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeGrip_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isResizing)
                return;

            Point currentPos = e.GetPosition(Note);
            double newWidth = Math.Max(80, currentPos.X);
            double newHeight = Math.Max(80, currentPos.Y);

            Note.Width = newWidth;
            Note.Height = newHeight;
            Data.Width = newWidth;
            Data.Height = newHeight;

            UpdateWindowSizeForRotation(false);
            e.Handled = true;
        }

        private void ResizeGrip_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!isResizing)
                return;

            isResizing = false;
            ResizeGrip.ReleaseMouseCapture();

            UpdateDataFromWindow();
            DnoteStorage.Save(((App)Application.Current).Store);
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
        // ՏԱՌԱՏԵՍԱԿ (ԹԵՐԹԻԿԻ ՏԵՂԱՅԻՆ ՑԱՆԿ)
        // =========================================================

        private void SetCurrentNoteFont(string fontName)
        {
            if (!NoteText.Selection.IsEmpty)
            {
                NoteText.Selection.ApplyPropertyValue(
                    TextElement.FontFamilyProperty,
                    new FontFamily(fontName));
            }
            else
            {
                Data.FontFamily = fontName;
                NoteText.FontFamily = new FontFamily(fontName);
            }

            UpdateDataFromWindow();
            DnoteStorage.Save(((App)Application.Current).Store);
            NoteText.Focus();
        }

        private void FontComicSans_Click(object sender, RoutedEventArgs e) => SetCurrentNoteFont("Comic Sans MS");
        private void FontCalibri_Click(object sender, RoutedEventArgs e) => SetCurrentNoteFont("Calibri");
        private void FontCourier_Click(object sender, RoutedEventArgs e) => SetCurrentNoteFont("Courier New");
        private void FontArial_Click(object sender, RoutedEventArgs e) => SetCurrentNoteFont("Arial");
        private void FontTimes_Click(object sender, RoutedEventArgs e) => SetCurrentNoteFont("Times New Roman");
        private void FontCustom_Click(object sender, RoutedEventArgs e)
        {
            Forms.FontDialog dialog = new Forms.FontDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                SetCurrentNoteFont(dialog.Font.FontFamily.Name);
            }
        }

        // =========================================================
        // ԹԵՐԹԻԿԻ ԳՈՒՅՆ (ԹԵՐԹԻԿԻ ՏԵՂԱՅԻՆ ՑԱՆԿ)
        // =========================================================

        private void SetCurrentNoteColor(string hex)
        {
            Data.NoteColor = hex;
            Note.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            UpdateDataFromWindow();
            DnoteStorage.Save(((App)Application.Current).Store);
        }

        private void NoteColorYellow_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#FFF59D");
        private void NoteColorGreen_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#C8E6C9");
        private void NoteColorBlue_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#BBDEFB");
        private void NoteColorPink_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#F8BBD0");
        private void NoteColorPurple_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#E1BEE7");
        private void NoteColorWhite_Click(object sender, RoutedEventArgs e) => SetCurrentNoteColor("#FFFFFF");
        private void NoteColorCustom_Click(object sender, RoutedEventArgs e)
        {
            Forms.ColorDialog dialog = new Forms.ColorDialog { FullOpen = true };
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string hex = string.Format("#{0:X2}{1:X2}{2:X2}", dialog.Color.R, dialog.Color.G, dialog.Color.B);
                SetCurrentNoteColor(hex);
            }
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
                ColorFromHex("#000000"));
        }


        private void TextDarkGray_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                ColorFromHex("#464646"));
        }


        private void TextRed_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                ColorFromHex("#B40000"));
        }


        private void TextBlue_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                ColorFromHex("#0046B4"));
        }


        private void TextGreen_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                ColorFromHex("#00783C"));
        }


        private void TextBrown_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetTextColor(
                ColorFromHex("#78461E"));
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

        private void CompleteButton_Click(
    object sender,
    RoutedEventArgs e)
{
    if (!Data.IsCompleted)
    {
        Data.IsCompleted = true;
        Data.CompletedDate = DateTime.Now;
    }
    else
    {
        Data.IsCompleted = false;
        Data.CompletedDate = null;
    }

    UpdateCompleteButtonAppearance();
    UpdateCompleteToolTip();

    DnoteStorage.Save(
        ((App)Application.Current).Store);
}

private void UpdateCompleteToolTip()
{
    string created =
        Data.CreatedAt.ToString("dd.MM.yyyy HH:mm");

    if (Data.IsCompleted &&
        Data.CompletedDate.HasValue)
    {
        string completed =
            Data.CompletedDate.Value.ToString(
                "dd.MM.yyyy HH:mm");

        CompleteButton.ToolTip =
            created + "\n" + completed;
    }
    else
    {
        CompleteButton.ToolTip = created;
    }
}

private void UpdateCompleteButtonAppearance()
{
    if (Data.IsCompleted)
    {
        CompleteButton.Foreground = Brushes.ForestGreen;
        CompleteButton.FontWeight = FontWeights.ExtraBold;
    }
    else
    {
        CompleteButton.Foreground =
            new SolidColorBrush(
                ColorFromHex("#555555"));

        CompleteButton.FontWeight =
            FontWeights.Normal;
    }
}

private void CompleteButton_MouseEnter(
    object sender,
    MouseEventArgs e)
{
    if (Data.IsCompleted)
    {
        CompleteButton.Foreground =
            Brushes.ForestGreen;
        CompleteButton.FontWeight =
            FontWeights.ExtraBold;
    }
    else
    {
        CompleteButton.Foreground =
            new SolidColorBrush(
                ColorFromHex("#555555"));

        CompleteButton.FontWeight =
            FontWeights.ExtraBold;
    }
}

private void CompleteButton_MouseLeave(
    object sender,
    MouseEventArgs e)
{
    if (Data.IsCompleted)
    {
        CompleteButton.Foreground =
            Brushes.ForestGreen;
        CompleteButton.FontWeight =
            FontWeights.ExtraBold;
    }
    else
    {
        CompleteButton.Foreground =
            new SolidColorBrush(
                ColorFromHex("#555555"));

        CompleteButton.FontWeight =
            FontWeights.Normal;
    }
}

    }
}
