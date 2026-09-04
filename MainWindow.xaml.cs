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

    ShowInTaskbar = false;

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

    Width = Math.Max(1200, Data.Width + 600);
    Height = Math.Max(1200, Data.Height + 600);

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

    // ԳԱՄ (PIN / ALWAYS ON TOP)
    Topmost = Data.IsPinned;
    PinButton.Visibility = Data.IsPinned ? Visibility.Visible : Visibility.Collapsed;

    UpdateStackUI();
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
            // Ստեղծում ենք լրիվ նոր թերթիկ՝ նախընտրությունների կարգավորումներով
            // -----------------------------------------------------

            MainWindow newNote =
                new MainWindow();

            newNote.ShowInTaskbar = false;

            // Տեղադրում ենք ընթացիկ թերթիկից փոքր շեղումով (30px)
            newNote.Left = Left + 30;
            newNote.Top = Top + 30;
            newNote.Data.Left = newNote.Left;
            newNote.Data.Top = newNote.Top;

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

            PinMenuItem.Header =
                Data.IsPinned ? "Ապագամել" : "Գամել (Ամենավերևում)";

            bool inStack = Data.StackId != null && store.Stacks.ContainsKey(Data.StackId.Value);
            ConvertToStackMenuItem.Visibility = inStack ? Visibility.Collapsed : Visibility.Visible;
        }


        // =========================================================
        // MOVE START
        // =========================================================

        private bool isCtrlDrag = false;
        private System.Collections.Generic.Dictionary<MainWindow, Point> stackInitialPositions =
            new System.Collections.Generic.Dictionary<MainWindow, Point>();

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

            isCtrlDrag =
                Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl);

            stackInitialPositions.Clear();
            NoteStore store = ((App)Application.Current).Store;
            if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        stackInitialPositions[win] = new Point(win.Left, win.Top);
                    }
                }
            }

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

            Left =
                moveStartLeft +
                deltaX;

            Top =
                moveStartTop +
                deltaY;

            // Եթե Ctrl սեղմված ՉԷ եւ թերթիկը տրցակում է, տեղափոխում ենք ամբողջ տրցակը
            if (!isCtrlDrag && Data.StackId != null)
            {
                foreach (var kvp in stackInitialPositions)
                {
                    if (kvp.Key != this)
                    {
                        kvp.Key.Left = kvp.Value.X + deltaX;
                        kvp.Key.Top = kvp.Value.Y + deltaY;
                    }
                }
            }
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

            NoteStore store = ((App)Application.Current).Store;

            // 1. Եթե քաշվել է Ctrl սեղմած եւ տրցակում է՝ պոկվում է տրցակից
            if (isCtrlDrag && Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                DetachFromStack(stack);
                SaveCurrentState();
                return;
            }

            // 2. Եթե տրցակում է եւ առանց Ctrl է տեղափոխվել՝ պահպանում ենք տրցակի բոլոր թերթիկների նոր դիրքերը
            if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? currentStack))
            {
                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (currentStack.NoteIds.Contains(win.NoteId))
                    {
                        win.Data.Left = win.Left;
                        win.Data.Top = win.Top;
                    }
                }
                SaveCurrentState();
            }
            else
            {
                SaveCurrentState();
            }

            // 3. Ստուգում ենք՝ արդյոք գցվել է մեկ այլ թերթիկի կամ տրցակի վրա
            CheckDragDropMerge();
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
                    current == ResizeGrip ||
                    current == PinButton ||
                    current == StackNavigationBar)
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


private void NoteWindow_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
{
    if (Data.StackId != null)
    {
        NoteStore store = ((App)Application.Current).Store;
        if (store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
        {
            if (stack.CurrentNoteId != Data.Id)
            {
                SetActiveStackNote(stack, Data.Id);
            }
        }
    }

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

            Point topLeft =
                Note.PointToScreen(
                    new Point(0, 0));

            double currentAngle =
                GetAngle(
                    mouse,
                    topLeft);

            double difference =
                NormalizeAngle(
                    currentAngle -
                    rotationStartAngle);

            double newAngle =
                noteStartAngle +
                difference;

            NoteRotation.Angle =
                newAngle;

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
            double newWidth = Math.Max(130, currentPos.X);
            double newHeight = Math.Max(80, currentPos.Y);

            Note.Width = newWidth;
            Note.Height = newHeight;
            Data.Width = newWidth;
            Data.Height = newHeight;

            Width = Math.Max(Width, newWidth + 600);
            Height = Math.Max(Height, newHeight + 600);

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

            Point topLeft =
                Note.PointToScreen(
                    new Point(0, 0));

            rotationStartAngle =
                GetAngle(
                    mouse,
                    topLeft);

            noteStartAngle =
                NoteRotation.Angle;

            isRotating = true;

            Note.CaptureMouse();

            e.Handled = true;
        }


        // =========================================================
        // WINDOW-Ի ՉԱՓԸ
        // =========================================================

        public void UpdateWindowSizeForRotation(
            bool keepCenter = false)
        {
            double w = Note.ActualWidth > 0 ? Note.ActualWidth : Note.Width;
            double h = Note.ActualHeight > 0 ? Note.ActualHeight : Note.Height;
            if (w > 0) Width = Math.Max(Width, w + 600);
            if (h > 0) Height = Math.Max(Height, h + 600);
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


        // =========================================================
        // ԳԱՄ (PIN / ALWAYS ON TOP)
        // =========================================================

        private void PinButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            UnpinNote();
        }

        private void TogglePin_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (Data.IsPinned)
            {
                UnpinNote();
            }
            else
            {
                PinNote();
            }
        }

        private void PinNote()
        {
            Data.IsPinned = true;
            Topmost = true;
            PinButton.Visibility = Visibility.Visible;
            PinMenuItem.Header = "Ապագամել";
            SaveCurrentState();
        }

        private void UnpinNote()
        {
            Data.IsPinned = false;
            Topmost = false;
            PinButton.Visibility = Visibility.Collapsed;
            PinMenuItem.Header = "Գամել (Ամենավերևում)";
            SaveCurrentState();
        }


        // =========================================================
        // ՈՒՂՂԱՀԱՅԱՑ ԴԻՐՔ (0° ԱՆԿՅՈՒՆ)
        // =========================================================

        private void ResetRotationZero_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteRotation.Angle = 0;
            Data.Rotation = 0;
            UpdateWindowSizeForRotation(false);
            SaveCurrentState();
        }


        // =========================================================
        // ԼՈՒՍԱՐՁԱԿՈՒՄ (OUTER GLOW EFFECT)
        // =========================================================

        public void PlayStackGlowEffect()
        {
            System.Windows.Media.Effects.DropShadowEffect glow =
                new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FFB300"),
                    BlurRadius = 26,
                    ShadowDepth = 0,
                    Opacity = 0.9
                };

            Note.Effect = glow;

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Note.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 4,
                    Opacity = 0.35
                };
            };

            timer.Start();
        }


        // =========================================================
        // ՏՐՑԱԿԻ UI ԵՒ ԹԵՐԹՈՒՄ (STACK NAVIGATION)
        // =========================================================

        public void UpdateStackUI()
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack) &&
                stack.NoteIds.Count > 1 &&
                stack.CurrentNoteId == Data.Id)
            {
                StackNavigationBar.Visibility =
                    Visibility.Visible;

                int index =
                    stack.NoteIds.IndexOf(Data.Id);

                if (index < 0) index = 0;

                StackPageIndicator.Text =
                    string.Format("{0} / {1}", index + 1, stack.NoteIds.Count);

                StackFirstButton.IsEnabled =
                    (index > 0);

                StackPrevButton.IsEnabled =
                    (index > 0);

                StackNextButton.IsEnabled =
                    (index < stack.NoteIds.Count - 1);

                StackLastButton.IsEnabled =
                    (index < stack.NoteIds.Count - 1);
            }
            else
            {
                StackNavigationBar.Visibility =
                    Visibility.Collapsed;
            }
        }

        private bool isNavigatingStack = false;

        private void SetActiveStackNote(NoteStack stack, Guid noteId)
        {
            if (isNavigatingStack) return;
            isNavigatingStack = true;

            try
            {
                stack.CurrentNoteId = noteId;
                int activeIndex = stack.NoteIds.IndexOf(noteId);
                if (activeIndex < 0) return;

                NoteStore store = ((App)Application.Current).Store;
                MainWindow[] openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();

                // 1. Ապահովում ենք, որ բոլոր թերթիկները բացված են
                for (int i = 0; i < stack.NoteIds.Count; i++)
                {
                    Guid id = stack.NoteIds[i];
                    MainWindow? w = openNotes.FirstOrDefault(n => n.NoteId == id);
                    if (w == null && store.Notes.TryGetValue(id, out NoteData? d))
                    {
                        w = new MainWindow(d);
                        w.Show();
                    }
                }

                openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();

                // 2. Թարմացնում ենք տեսանելիությունը (ավելի նոր թերթիկները Collapsed, ընթացիկը և ավելի հիները Visible)
                for (int i = 0; i < stack.NoteIds.Count; i++)
                {
                    Guid id = stack.NoteIds[i];
                    MainWindow? w = openNotes.FirstOrDefault(n => n.NoteId == id);
                    if (w != null)
                    {
                        if (i > activeIndex)
                        {
                            w.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            w.Visibility = Visibility.Visible;
                        }
                    }
                }

                // 3. Ակտիվացնում ենք ընտրված թերթիկը
                MainWindow? targetWin = openNotes.FirstOrDefault(n => n.NoteId == noteId);
                if (targetWin != null)
                {
                    targetWin.Visibility = Visibility.Visible;
                    targetWin.Activate();
                    targetWin.Focus();
                }

                // 4. Թարմացնում ենք UI-ները
                foreach (MainWindow win in openNotes)
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        win.UpdateStackUI();
                    }
                }

                DnoteStorage.Save(store);
            }
            finally
            {
                isNavigatingStack = false;
            }
        }

        private void NoteWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (Data.StackId != null)
            {
                if (e.Key == Key.Left)
                {
                    NavigateStack(-1);
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    NavigateStack(1);
                    e.Handled = true;
                }
            }
        }

        private void StackFirst_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStackAbsolute(0);
        }

        private void StackPrev_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStack(-1);
        }

        private void StackNext_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStack(1);
        }

        private void StackLast_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                NavigateStackAbsolute(stack.NoteIds.Count - 1);
            }
        }

        private void StackNavButton_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            if (sender is Button b)
            {
                b.Background = GetNoteHoverBrush(0.78);
                b.Foreground = Brushes.Black;
            }
        }

        private void StackNavButton_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (sender is Button b)
            {
                b.Background = Brushes.Transparent;
                b.Foreground = new SolidColorBrush(ColorFromHex("#444444"));
            }
        }

        private void NavigateStackAbsolute(
            int targetIndex)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId == null ||
                !store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                return;

            if (targetIndex < 0 || targetIndex >= stack.NoteIds.Count) return;

            Guid targetNoteId =
                stack.NoteIds[targetIndex];

            SetActiveStackNote(stack, targetNoteId);
        }

        private void NavigateStack(
            int step)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId == null ||
                !store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                return;

            int currentIndex =
                stack.NoteIds.IndexOf(stack.CurrentNoteId ?? Data.Id);

            if (currentIndex < 0) currentIndex = 0;

            NavigateStackAbsolute(currentIndex + step);
        }


        // =========================================================
        // ՏՐՑԱԿԻ ՍՏԵՂԾՈՒՄ ԵՒ ՔԱՆԴՈՒՄ
        // =========================================================

        private void ConvertToStack_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            Rect thisRect =
                GetScreenBounds();

            List<MainWindow> overlapping =
                new List<MainWindow>();

            overlapping.Add(this);

            foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
            {
                if (win != this && win.Visibility == Visibility.Visible)
                {
                    Rect otherRect = win.GetScreenBounds();
                    Rect intersect = Rect.Intersect(thisRect, otherRect);
                    if (!intersect.IsEmpty && intersect.Width > 50 && intersect.Height > 50)
                    {
                        overlapping.Add(win);
                    }
                }
            }

            if (overlapping.Count < 2)
            {
                MessageBox.Show(
                    this,
                    "Տրցակ կազմելու համար անհրաժեշտ է, որ այս թերթիկը ծածկի առնվազն մեկ այլ թերթիկ։",
                    "Տրցակ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            CreateOrMergeStack(overlapping, this);
        }

        private void CreateOrMergeStack(
            List<MainWindow> windowsToStack,
            MainWindow anchorWin)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            List<NoteData> notes =
                windowsToStack.Select(w => w.Data).GroupBy(d => d.Id).Select(g => g.First()).ToList();

            // Դասավորում ըստ ստեղծման ամսաթվի (հին -> նոր)
            notes = notes.OrderBy(n => n.CreatedAt).ToList();

            NoteStack stack = new NoteStack
            {
                Id = Guid.NewGuid(),
                NoteIds = notes.Select(n => n.Id).ToList(),
                CurrentNoteId = notes.Last().Id, // ամենանորը վերեւում
                Alignment = store.StackAlignment ?? "TopLeft"
            };

            store.Stacks[stack.Id] = stack;

            foreach (NoteData n in notes)
            {
                n.StackId = stack.Id;
            }

            anchorWin.ApplyStackAlignment(stack);

            // Բոլոր թերթիկները տեսանելի են տակից (եթե ավելի մեծ են կամ այլ անկյան տակ)
            foreach (MainWindow w in windowsToStack.OrderBy(win => notes.FindIndex(n => n.Id == win.Data.Id)))
            {
                w.Visibility = Visibility.Visible;
                w.UpdateStackUI();
            }

            MainWindow? topWin = windowsToStack.FirstOrDefault(w => w.Data.Id == stack.CurrentNoteId);
            if (topWin != null)
            {
                topWin.PlayStackGlowEffect();
                topWin.Visibility = Visibility.Visible;
                topWin.UpdateStackUI();
                topWin.Activate();
                topWin.Focus();
            }

            DnoteStorage.Save(store);
        }

        public void DetachFromStack(
            NoteStack stack)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            Data.StackId = null;
            stack.NoteIds.Remove(Data.Id);

            if (stack.CurrentNoteId == Data.Id)
            {
                stack.CurrentNoteId =
                    stack.NoteIds.LastOrDefault();
            }

            UpdateStackUI();

            if (stack.NoteIds.Count <= 1)
            {
                if (stack.NoteIds.Count == 1)
                {
                    Guid remainingId = stack.NoteIds[0];
                    if (store.Notes.TryGetValue(remainingId, out NoteData? remainingData))
                    {
                        remainingData.StackId = null;
                    }

                    MainWindow? remainingWin =
                        Application.Current.Windows.OfType<MainWindow>()
                            .FirstOrDefault(w => w.NoteId == remainingId);

                    if (remainingWin != null)
                    {
                        remainingWin.Data.StackId = null;
                        remainingWin.Visibility = Visibility.Visible;
                        remainingWin.UpdateStackUI();
                    }
                }

                store.Stacks.Remove(stack.Id);
            }
            else
            {
                MainWindow? topWin =
                    Application.Current.Windows.OfType<MainWindow>()
                        .FirstOrDefault(w => w.NoteId == stack.CurrentNoteId);

                if (topWin != null)
                {
                    topWin.Visibility = Visibility.Visible;
                    topWin.UpdateStackUI();
                }

                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        win.UpdateStackUI();
                    }
                }
            }

            DnoteStorage.Save(store);
        }


        // =========================================================
        // ՏՐՑԱԿԻ ՀԱՎԱՍԱՐԵՑՈՒՄ
        // =========================================================

        private void StackAlignTopLeft_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetStackAlignment("TopLeft");
        }

        private void StackAlignTopCenter_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetStackAlignment("TopCenter");
        }

        private void StackAlignTopRight_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetStackAlignment("TopRight");
        }

        private void StackAlignZeroRotation_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                stack.IsZeroRotation = true;

                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        win.NoteRotation.Angle = 0;
                        win.Data.Rotation = 0;
                        win.UpdateWindowSizeForRotation(false);
                    }
                }

                ApplyStackAlignment(stack);

                DnoteStorage.Save(store);
            }
        }

        private void SetStackAlignment(
            string align)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                stack.Alignment = align;

                ApplyStackAlignment(stack);

                DnoteStorage.Save(store);
            }
        }

        public void ApplyStackAlignment(
            NoteStack stack)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            MainWindow[] openNotes =
                Application.Current.Windows.OfType<MainWindow>().ToArray();

            Guid refId = stack.CurrentNoteId ?? stack.NoteIds.LastOrDefault();
            MainWindow? refWin = openNotes.FirstOrDefault(w => w.NoteId == refId) ?? this;

            Point refPoint;
            if (stack.Alignment == "TopCenter")
            {
                refPoint = refWin.Note.PointToScreen(new Point(refWin.Note.ActualWidth / 2.0, 0));
            }
            else if (stack.Alignment == "TopRight")
            {
                refPoint = refWin.Note.PointToScreen(new Point(refWin.Note.ActualWidth, 0));
            }
            else // "TopLeft" (լռելյայն)
            {
                refPoint = refWin.Note.PointToScreen(new Point(0, 0));
            }

            foreach (Guid id in stack.NoteIds)
            {
                MainWindow? win =
                    openNotes.FirstOrDefault(w => w.NoteId == id);

                if (win != null)
                {
                    if (stack.IsZeroRotation)
                    {
                        win.NoteRotation.Angle = 0;
                        win.Data.Rotation = 0;
                        win.UpdateWindowSizeForRotation(false);
                    }

                    Point curPoint;
                    if (stack.Alignment == "TopCenter")
                    {
                        curPoint = win.Note.PointToScreen(new Point(win.Note.ActualWidth / 2.0, 0));
                    }
                    else if (stack.Alignment == "TopRight")
                    {
                        curPoint = win.Note.PointToScreen(new Point(win.Note.ActualWidth, 0));
                    }
                    else
                    {
                        curPoint = win.Note.PointToScreen(new Point(0, 0));
                    }

                    double shiftX = refPoint.X - curPoint.X;
                    double shiftY = refPoint.Y - curPoint.Y;

                    win.Left += shiftX;
                    win.Top += shiftY;
                    win.Data.Left = win.Left;
                    win.Data.Top = win.Top;
                }
            }
        }

        public Rect GetScreenBounds()
        {
            if (Note.ActualWidth > 0 && Note.ActualHeight > 0)
            {
                Point p0 = Note.PointToScreen(new Point(0, 0));
                Point p1 = Note.PointToScreen(new Point(Note.ActualWidth, 0));
                Point p2 = Note.PointToScreen(new Point(Note.ActualWidth, Note.ActualHeight));
                Point p3 = Note.PointToScreen(new Point(0, Note.ActualHeight));

                double minX = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X));
                double maxX = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
                double minY = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y));
                double maxY = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));

                return new Rect(minX, minY, Math.Max(10, maxX - minX), Math.Max(10, maxY - minY));
            }

            return new Rect(
                Left,
                Top,
                Math.Max(ActualWidth, Note.ActualWidth),
                Math.Max(ActualHeight, Note.ActualHeight));
        }

        private void CheckDragDropMerge()
        {
            NoteStore store =
                ((App)Application.Current).Store;

            Rect thisRect =
                GetScreenBounds();

            MainWindow? targetWin = null;

            foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
            {
                bool isSameStack = (Data.StackId != null && win.Data.StackId != null && Data.StackId == win.Data.StackId);
                if (win != this && !isSameStack && win.Visibility == Visibility.Visible)
                {
                    Rect otherRect = win.GetScreenBounds();
                    Rect intersect = Rect.Intersect(thisRect, otherRect);
                    if (!intersect.IsEmpty && intersect.Width > 50 && intersect.Height > 50)
                    {
                        targetWin = win;
                        break;
                    }
                }
            }

            if (targetWin == null)
                return;

            List<MainWindow> toMerge =
                new List<MainWindow>();

            if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? thisStack))
            {
                toMerge.AddRange(
                    Application.Current.Windows.OfType<MainWindow>()
                        .Where(w => thisStack.NoteIds.Contains(w.NoteId)));
                store.Stacks.Remove(thisStack.Id);
            }
            else
            {
                toMerge.Add(this);
            }

            if (targetWin.Data.StackId != null &&
                store.Stacks.TryGetValue(targetWin.Data.StackId.Value, out NoteStack? targetStack))
            {
                toMerge.AddRange(
                    Application.Current.Windows.OfType<MainWindow>()
                        .Where(w => targetStack.NoteIds.Contains(w.NoteId)));
                store.Stacks.Remove(targetStack.Id);
            }
            else
            {
                toMerge.Add(targetWin);
            }

            CreateOrMergeStack(toMerge, targetWin);
        }

    }
}
