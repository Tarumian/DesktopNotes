using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Ink;
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
    
    public NoteData Data;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOACTIVATE = 0x0010;

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

    EnsureOnScreen();


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
    UpdatePinVisual();

    UpdateRotationOrigin();

    LoadInkFromData();
    UpdatePencilDrawingAttributes();

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

    LoadInkFromData();
    UpdatePencilDrawingAttributes();
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

    SaveInkToData();
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

public void SaveCurrentState()
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
    double oldWidth = Note.Width > 0 ? Note.Width : Data.Width;
    double deltaW = width - oldWidth;

    Data.Width = width;
    Data.Height = height;

    Note.Width = width;
    Note.Height = height;

    NoteStore store = ((App)Application.Current).Store;
    if (store.RotationOrigin != "TopLeft" && Math.Abs(deltaW) > 0.001)
    {
        Left -= deltaW / 2.0;
        Data.Left = Left;
    }

    UpdateWindowSizeForRotation(false);
    UpdateDataFromWindow();

    if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
    {
        ApplyStackAlignment(stack, this);
    }

    DnoteStorage.Save(store);
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

private void Size220x220_Click(
    object sender,
    RoutedEventArgs e)
{
    SetCurrentNoteSize(220, 220);
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

            NoteStore store = ((App)Application.Current).Store;
            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack) &&
                stack.NoteIds.Count > 1)
            {
                StackNavigationBar.Visibility =
                    Visibility.Visible;
            }
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

            if (!isRotating &&
                !isMoving &&
                !StackNavigationBar.IsMouseOver)
            {
                StackNavigationBar.Visibility =
                    Visibility.Collapsed;
            }
        }

        private void StackNavigationBar_MouseEnter(
            object sender,
            MouseEventArgs e)
        {
            NoteStore store = ((App)Application.Current).Store;
            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack) &&
                stack.NoteIds.Count > 1)
            {
                StackNavigationBar.Visibility =
                    Visibility.Visible;
            }
        }

        private void StackNavigationBar_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (!Note.IsMouseOver)
            {
                StackNavigationBar.Visibility =
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

    AppUndoManager.PushAction(new DeleteNoteUndoAction { NoteId = Data.Id });

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

        private bool IsNoteContentEmpty()
        {
            if (NoteInkCanvas != null && NoteInkCanvas.Strokes.Count > 0)
                return false;

            TextRange range = new TextRange(
                NoteText.Document.ContentStart,
                NoteText.Document.ContentEnd);

            string plainText = range.Text.Trim();
            if (!string.IsNullOrEmpty(plainText))
                return false;

            foreach (Block block in NoteText.Document.Blocks)
            {
                if (block is Paragraph p)
                {
                    foreach (Inline inline in p.Inlines)
                    {
                        if (inline is InlineUIContainer || inline is Figure || inline is Floater)
                            return false;
                    }
                }
                else if (block is BlockUIContainer || block is Table || block is Section)
                {
                    return false;
                }
            }

            return true;
        }

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

        protected override void OnClosing(
            System.ComponentModel.CancelEventArgs e)
        {
            if (!isDeleting)
            {
                if (IsNoteContentEmpty())
                {
                    isDeleting = true;
                    NoteStore store =
                        ((App)Application.Current).Store;

                    if (Data.StackId != null &&
                        store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                    {
                        DetachFromStack(stack);
                    }

                    store.Notes.Remove(Data.Id);
                    store.DeletedNotes.Remove(Data.Id);
                    DnoteStorage.Save(store);
                }
                else
                {
                    Data.IsClosed = true;
                    SaveCurrentState();
                }
            }

            base.OnClosing(e);
        }

        private static MemoryStream? RenderFullNoteToStream(NoteData note)
        {
            try
            {
                double noteW = note.Width > 0 ? note.Width : 150;
                double noteH = note.Height > 0 ? note.Height : 220;

                int w = (int)Math.Max(120, noteW);
                int h = (int)Math.Max(160, noteH);

                var grid = new Grid
                {
                    Width = w,
                    Height = h
                };

                // 1. Background paper
                Color bgColor;
                try
                {
                    bgColor = (Color)ColorConverter.ConvertFromString(note.NoteColor ?? "#FFF9A6");
                }
                catch
                {
                    bgColor = (Color)ColorConverter.ConvertFromString("#FFF9A6");
                }

                var border = new Border
                {
                    Width = w,
                    Height = h,
                    Background = new SolidColorBrush(bgColor),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                grid.Children.Add(border);

                // 2. RichTextBox for formatted text
                var rtb = new RichTextBox
                {
                    Width = Math.Max(10, w - 8),
                    Height = Math.Max(10, h - 20),
                    Margin = new Thickness(4, 10, 4, 10),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    FontFamily = new FontFamily(note.FontFamily ?? "Arial"),
                    FontSize = note.FontSize > 0 ? note.FontSize : 14,
                    FontWeight = note.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = note.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
                };

                try
                {
                    Color txtColor = (Color)ColorConverter.ConvertFromString(note.TextColor ?? "#000000");
                    rtb.Foreground = new SolidColorBrush(txtColor);
                }
                catch
                {
                    rtb.Foreground = Brushes.Black;
                }

                if (!string.IsNullOrEmpty(note.RtfContent))
                {
                    try
                    {
                        using var rtfStream = new MemoryStream(Encoding.UTF8.GetBytes(note.RtfContent));
                        var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        range.Load(rtfStream, DataFormats.Rtf);
                    }
                    catch
                    {
                        rtb.Document.Blocks.Clear();
                        rtb.Document.Blocks.Add(new Paragraph(new Run(note.Text ?? "")));
                    }
                }
                else
                {
                    rtb.Document.Blocks.Clear();
                    rtb.Document.Blocks.Add(new Paragraph(new Run(note.Text ?? "")));
                }

                grid.Children.Add(rtb);

                // 3. InkCanvas for pencil strokes
                if (!string.IsNullOrEmpty(note.InkData))
                {
                    try
                    {
                        byte[] inkBytes = Convert.FromBase64String(note.InkData);
                        using var inkMs = new MemoryStream(inkBytes);
                        var strokes = new StrokeCollection(inkMs);
                        if (strokes.Count > 0)
                        {
                            var inkCanvas = new InkCanvas
                            {
                                Width = Math.Max(10, w - 8),
                                Height = Math.Max(10, h - 20),
                                Margin = new Thickness(4, 10, 4, 10),
                                Background = Brushes.Transparent,
                                IsHitTestVisible = false,
                                EditingMode = InkCanvasEditingMode.None,
                                Strokes = strokes
                            };
                            grid.Children.Add(inkCanvas);
                        }
                    }
                    catch { }
                }

                grid.Measure(new Size(w, h));
                grid.Arrange(new Rect(0, 0, w, h));
                grid.UpdateLayout();

                var renderBmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                renderBmp.Render(grid);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBmp));
                var outStream = new MemoryStream();
                encoder.Save(outStream);
                outStream.Position = 0;
                return outStream;
            }
            catch
            {
                return null;
            }
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
                    worksheet.Cell(1, 1).Value = "Ստեղծման ամսաթիվ";
                    worksheet.Cell(1, 2).Value = "Գրառում";
                    worksheet.Cell(1, 3).Value = "Նկար";
                    worksheet.Cell(1, 4).Value = "Լուծված";
                    worksheet.Cell(1, 5).Value = "Լուծման ամսաթիվ";

                    // Վերնագրերի ձեւավորում
                    var headerRange = worksheet.Range(1, 1, 1, 5);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    // Թերթիկները՝ ստեղծման հերթականությամբ
                    var notes = store.Notes.Values
                        .OrderBy(note => note.CreatedAt)
                        .ToList();

                    int row = 2;

                    foreach (NoteData note in notes)
                    {
                        worksheet.Cell(row, 1).Value = note.CreatedAt;
                        worksheet.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

                        worksheet.Cell(row, 2).Value = note.Text;

                        // Սյունակ 3. Թերթիկի ամբողջական միասնական պատկերը (գրառում + մատիտ)
                        var imgStream = RenderFullNoteToStream(note);
                        if (imgStream != null)
                        {
                            worksheet.Row(row).Height = 85;

                            int targetH = 105;
                            double aspect = (note.Width > 0 ? note.Width : 150) / (note.Height > 0 ? note.Height : 220);
                            int targetW = Math.Min(140, (int)(targetH * aspect));

                            worksheet.AddPicture(imgStream, $"Picture{row}")
                                     .MoveTo(worksheet.Cell(row, 3), 4, 3)
                                     .WithSize(targetW, targetH);
                        }

                        worksheet.Cell(row, 4).Value = note.IsCompleted ? "Այո" : "";
                        worksheet.Cell(row, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                        if (note.CompletedDate.HasValue)
                        {
                            worksheet.Cell(row, 5).Value = note.CompletedDate.Value;
                            worksheet.Cell(row, 5).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                        }

                        row++;
                    }

                    // Տեքստի տեղափոխում հաջորդ տող և ուղղահայաց կենտրոնադրում
                    worksheet.Column(2).Style.Alignment.WrapText = true;
                    worksheet.Columns(1, 5).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                    // Սյունակների լայնությունը
                    worksheet.Column(1).Width = 20;
                    worksheet.Column(2).Width = 45;
                    worksheet.Column(3).Width = 24;
                    worksheet.Column(4).Width = 12;
                    worksheet.Column(5).Width = 20;

                    // Ֆիլտր
                    if (row > 1)
                    {
                        worksheet.Range(1, 1, row - 1, 5).SetAutoFilter();
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

        private void HelpMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            HelpWindow help = new HelpWindow();
            help.Owner = this;
            help.ShowDialog();
        }

        private void AboutMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void UndoDelete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (AppUndoManager.CanUndo)
            {
                AppUndoManager.PerformUndo();
            }
        }

        private void Redo_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (AppUndoManager.CanRedo)
            {
                AppUndoManager.PerformRedo();
            }
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
                AppUndoManager.CanUndo;

            UndoDeleteMenuItem.Header =
                AppUndoManager.CurrentUndoDescription;

            RedoMenuItem.IsEnabled =
                AppUndoManager.CanRedo;

            RedoMenuItem.Header =
                AppUndoManager.CurrentRedoDescription;

            PinMenuItem.Header =
                Data.IsPinned ? "Ապագամել" : "Գամել ամենավերեւում";

            RotationOriginTopCenterMenuItem.IsChecked = (store.RotationOrigin != "TopLeft");
            RotationOriginTopLeftMenuItem.IsChecked = (store.RotationOrigin == "TopLeft");

            PencilMenuItem.IsChecked = isPencilActive;

            bool inStack = Data.StackId != null && store.Stacks.ContainsKey(Data.StackId.Value);
            DetachFromStackMenuItem.Visibility = inStack ? Visibility.Visible : Visibility.Collapsed;
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
                    e.GetPosition(this));

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
                    e.GetPosition(this));

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
                    current == NoteInkCanvas ||
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
                    e.GetPosition(this));

            Point center =
                GetRotationCenterScreen();

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
        private Point resizeStartMouseScreen;
        private double resizeStartWidth;
        private double resizeStartHeight;

        private void ResizeGrip_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!((App)Application.Current).Store.AllowFreeResize)
                return;

            isResizing = true;
            resizeStartMouseScreen = PointToScreen(e.GetPosition(this));
            resizeStartWidth = Note.Width > 0 ? Note.Width : Data.Width;
            resizeStartHeight = Note.Height > 0 ? Note.Height : Data.Height;
            ResizeGrip.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeGrip_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isResizing)
                return;

            Point currentMouseScreen = PointToScreen(e.GetPosition(this));
            double deltaX = currentMouseScreen.X - resizeStartMouseScreen.X;
            double deltaY = currentMouseScreen.Y - resizeStartMouseScreen.Y;

            if (Math.Abs(NoteRotation.Angle) > 0.001)
            {
                double angleRad = -NoteRotation.Angle * Math.PI / 180.0;
                double cos = Math.Cos(angleRad);
                double sin = Math.Sin(angleRad);
                double localDeltaX = deltaX * cos - deltaY * sin;
                double localDeltaY = deltaX * sin + deltaY * cos;
                deltaX = localDeltaX;
                deltaY = localDeltaY;
            }

            double newWidth = Math.Max(140, resizeStartWidth + deltaX);
            double newHeight = Math.Max(80, resizeStartHeight + deltaY);

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
            if (ResizeGrip.IsMouseCaptured)
                ResizeGrip.ReleaseMouseCapture();

            UpdateDataFromWindow();

            if (Data.StackId != null && ((App)Application.Current).Store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                ApplyStackAlignment(stack, this);
            }

            DnoteStorage.Save(((App)Application.Current).Store);
            e.Handled = true;
        }


        // =========================================================
        // ՄԱՏԻՏԻ ԳՈՐԾԻՔ (PENCIL / INK OVERLAY)
        // =========================================================

        private static Cursor? cachedPencilCursor;
        private static Cursor? cachedEraserCursor;
        private static Color cachedPencilColor;

        private static Cursor CreateCursorFromBitmap(System.Drawing.Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            using var ms = new MemoryStream();
            using var pngMs = new MemoryStream();
            bmp.Save(pngMs, System.Drawing.Imaging.ImageFormat.Png);
            byte[] pngBytes = pngMs.ToArray();

            using var bw = new BinaryWriter(ms);
            // ICONDIR
            bw.Write((short)0); // Reserved
            bw.Write((short)2); // Type 2 = CURSOR
            bw.Write((short)1); // Count = 1

            // ICONDIRENTRY
            bw.Write((byte)bmp.Width);
            bw.Write((byte)bmp.Height);
            bw.Write((byte)0); // Color count
            bw.Write((byte)0); // Reserved
            bw.Write((short)xHotSpot); // Hotspot X
            bw.Write((short)yHotSpot); // Hotspot Y
            bw.Write((int)pngBytes.Length); // Image size
            bw.Write((int)22); // Image offset (header size = 6 + 16 = 22)

            // Image data (PNG)
            bw.Write(pngBytes);
            bw.Flush();

            ms.Position = 0;
            return new Cursor(ms);
        }

        private static Cursor GetPencilCursor(bool isEraser, Color color)
        {
            if (isEraser && cachedEraserCursor != null && cachedPencilColor == color)
                return cachedEraserCursor;
            if (!isEraser && cachedPencilCursor != null && cachedPencilColor == color)
                return cachedPencilCursor;

            using var bmp = new System.Drawing.Bitmap(32, 32);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                var c = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
                using var brushBody = new System.Drawing.SolidBrush(c);

                if (!isEraser)
                {
                    // Sleek, compact 15px pencil matching toolbar icon
                    // Tip is at (1, 15)
                    // Lead tip
                    g.FillPolygon(System.Drawing.Brushes.Black, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 15), new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3.5f, 15)
                    });
                    // Wood cone
                    g.FillPolygon(System.Drawing.Brushes.BurlyWood, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3.5f, 15), new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(3, 11)
                    });
                    // Body (filled with active pencil color!)
                    g.FillPolygon(brushBody, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(3, 11), new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(12.5f, 6.5f), new System.Drawing.PointF(10, 4)
                    });
                    // Ferrule (silver band)
                    g.FillPolygon(System.Drawing.Brushes.Silver, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(10, 4), new System.Drawing.PointF(12.5f, 6.5f), new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(12, 2)
                    });
                    // Eraser cap (pink)
                    g.FillPolygon(System.Drawing.Brushes.DeepPink, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(12, 2), new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(16, 3), new System.Drawing.PointF(13.5f, 0.5f)
                    });

                    // Crisp black outline
                    g.DrawPolygon(System.Drawing.Pens.Black, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 15), new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3, 11),
                        new System.Drawing.PointF(10, 4), new System.Drawing.PointF(12, 2), new System.Drawing.PointF(13.5f, 0.5f),
                        new System.Drawing.PointF(16, 3), new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(12.5f, 6.5f),
                        new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(3.5f, 15)
                    });
                }
                else
                {
                    // 180° Inverted: Eraser pink tip at (1, 15)
                    // Eraser (pink)
                    g.FillPolygon(System.Drawing.Brushes.DeepPink, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 15), new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3.5f, 15), new System.Drawing.PointF(3, 11)
                    });
                    // Ferrule (silver band)
                    g.FillPolygon(System.Drawing.Brushes.Silver, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3.5f, 15), new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(3, 11)
                    });
                    // Body (filled with active pencil color!)
                    g.FillPolygon(brushBody, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(3, 11), new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(12.5f, 6.5f), new System.Drawing.PointF(10, 4)
                    });
                    // Wood cone
                    g.FillPolygon(System.Drawing.Brushes.BurlyWood, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(10, 4), new System.Drawing.PointF(12.5f, 6.5f), new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(12, 2)
                    });
                    // Lead tip at far end
                    g.FillPolygon(System.Drawing.Brushes.Black, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(12, 2), new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(16, 0.5f)
                    });

                    // Crisp black outline
                    g.DrawPolygon(System.Drawing.Pens.Black, new System.Drawing.PointF[] {
                        new System.Drawing.PointF(1, 15), new System.Drawing.PointF(1, 12.5f), new System.Drawing.PointF(3, 11),
                        new System.Drawing.PointF(10, 4), new System.Drawing.PointF(12, 2), new System.Drawing.PointF(16, 0.5f),
                        new System.Drawing.PointF(14.5f, 4.5f), new System.Drawing.PointF(12.5f, 6.5f),
                        new System.Drawing.PointF(5.5f, 13.5f), new System.Drawing.PointF(3.5f, 15)
                    });
                }
            }

            Cursor cur = CreateCursorFromBitmap(bmp, 1, 15);

            if (isEraser)
            {
                cachedEraserCursor = cur;
                cachedPencilColor = color;
            }
            else
            {
                cachedPencilCursor = cur;
                cachedPencilColor = color;
            }

            return cur;
        }

        private bool isPencilActive = false;
        private bool isEraserActive = false;

        public void SaveInkToData()
        {
            if (NoteInkCanvas != null && NoteInkCanvas.Strokes.Count > 0)
            {
                using MemoryStream ms = new MemoryStream();
                NoteInkCanvas.Strokes.Save(ms);
                Data.InkData = Convert.ToBase64String(ms.ToArray());
            }
            else
            {
                Data.InkData = string.Empty;
            }
        }

        public void LoadInkFromData()
        {
            if (NoteInkCanvas == null) return;

            if (!string.IsNullOrEmpty(Data.InkData))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(Data.InkData);
                    using MemoryStream ms = new MemoryStream(bytes);
                    NoteInkCanvas.Strokes = new StrokeCollection(ms);
                }
                catch
                {
                    NoteInkCanvas.Strokes = new StrokeCollection();
                }
            }
            else
            {
                NoteInkCanvas.Strokes = new StrokeCollection();
            }

            NoteInkCanvas.StrokeCollected -= NoteInkCanvas_StrokeCollected;
            NoteInkCanvas.StrokeCollected += NoteInkCanvas_StrokeCollected;
            NoteInkCanvas.StrokeErasing -= NoteInkCanvas_StrokeErasing;
            NoteInkCanvas.StrokeErasing += NoteInkCanvas_StrokeErasing;
            NoteInkCanvas.StrokeErased -= NoteInkCanvas_StrokeErased;
            NoteInkCanvas.StrokeErased += NoteInkCanvas_StrokeErased;
            NoteInkCanvas.PreviewMouseMove -= NoteInkCanvas_PreviewMouseMove;
            NoteInkCanvas.PreviewMouseMove += NoteInkCanvas_PreviewMouseMove;
        }

        private void NoteInkCanvas_StrokeCollected(object? sender, InkCanvasStrokeCollectedEventArgs e)
        {
            AppUndoManager.PushAction(new StrokeAddUndoAction(Data.Id, e.Stroke));
            SaveInkToData();
            SaveCurrentState();
        }

        private void NoteInkCanvas_StrokeErasing(object? sender, InkCanvasStrokeErasingEventArgs e)
        {
            AppUndoManager.PushAction(new StrokeEraseUndoAction(Data.Id, e.Stroke));
        }

        private void NoteInkCanvas_StrokeErased(object? sender, RoutedEventArgs e)
        {
            SaveInkToData();
            SaveCurrentState();
        }

        private void NoteInkCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isPencilActive)
            {
                bool isCtrlHeld = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                if (isCtrlHeld != isEraserActive)
                {
                    SetEraserMode(isCtrlHeld);
                }
            }
        }

        public void UpdatePencilDrawingAttributes()
        {
            if (NoteInkCanvas == null) return;

            NoteStore store = ((App)Application.Current).Store;
            Color color;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(store.PencilColor);
            }
            catch
            {
                color = Colors.Black;
            }

            double thickness = Math.Max(1, store.PencilThickness);

            DrawingAttributes da = new DrawingAttributes
            {
                Color = color,
                Width = thickness,
                Height = thickness,
                FitToCurve = true,
                IgnorePressure = true
            };
            NoteInkCanvas.DefaultDrawingAttributes = da;

            cachedPencilCursor = null; // Invalidate color cache
            cachedEraserCursor = null;

            if (isPencilActive)
            {
                if (isEraserActive)
                {
                    double eraserSize = Math.Max(2, thickness + 1);
                    NoteInkCanvas.EraserShape = new RectangleStylusShape(eraserSize, eraserSize);
                    NoteInkCanvas.Cursor = GetPencilCursor(true, color);
                }
                else
                {
                    NoteInkCanvas.Cursor = GetPencilCursor(false, color);
                }
            }
        }

        public void SetEraserMode(bool active)
        {
            if (!isPencilActive) return;
            if (isEraserActive == active) return;

            isEraserActive = active;
            NoteStore store = ((App)Application.Current).Store;
            Color color;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(store.PencilColor);
            }
            catch
            {
                color = Colors.Black;
            }

            if (isEraserActive)
            {
                NoteInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                double eraserSize = Math.Max(2, store.PencilThickness + 1);
                NoteInkCanvas.EraserShape = new RectangleStylusShape(eraserSize, eraserSize);
                NoteInkCanvas.Cursor = GetPencilCursor(true, color);
            }
            else
            {
                NoteInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                NoteInkCanvas.Cursor = GetPencilCursor(false, color);
            }
        }

        public void TogglePencilMode(bool? forceState = null)
        {
            isPencilActive = forceState ?? !isPencilActive;

            if (isPencilActive)
            {
                isEraserActive = false;
                UpdatePencilDrawingAttributes();
                NoteInkCanvas.IsHitTestVisible = true;
                NoteInkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                NoteStore store = ((App)Application.Current).Store;
                Color color;
                try
                {
                    color = (Color)ColorConverter.ConvertFromString(store.PencilColor);
                }
                catch
                {
                    color = Colors.Black;
                }
                NoteInkCanvas.Cursor = GetPencilCursor(false, color);

                QuickPencilButton.Background = GetNoteHoverBrush(0.70);
                QuickPencilButton.BorderBrush = GetNoteHoverForeground();
                QuickPencilButton.Foreground = GetNoteHoverForeground();
            }
            else
            {
                isEraserActive = false;
                PencilColorPalettePopup.IsOpen = false;
                NoteInkCanvas.EditingMode = InkCanvasEditingMode.None;
                NoteInkCanvas.IsHitTestVisible = false;
                NoteInkCanvas.Cursor = null;

                QuickPencilButton.Background = Brushes.Transparent;
                QuickPencilButton.BorderBrush = Brushes.Transparent;
                QuickPencilButton.Foreground = (Brush?)TryFindResource("TextBrush") ?? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
            }

            if (PencilMenuItem != null)
            {
                PencilMenuItem.IsChecked = isPencilActive;
            }
        }

        private void QuickPencilButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPencilActive)
            {
                TogglePencilMode(true);
                PencilColorPalettePopup.IsOpen = true;
            }
            else
            {
                if (PencilColorPalettePopup.IsOpen)
                {
                    // User clicked pencil button again while palette was open: close palette, accept current color and stay in pencil mode!
                    PencilColorPalettePopup.IsOpen = false;
                }
                else
                {
                    TogglePencilMode(false);
                }
            }
        }

        private void TogglePencil_Click(object sender, RoutedEventArgs e)
        {
            TogglePencilMode();
        }

        private void ClearInk_Click(object sender, RoutedEventArgs e)
        {
            if (NoteInkCanvas.Strokes.Count > 0)
            {
                // Create undo action for clearing all strokes
                Stroke[] allStrokes = NoteInkCanvas.Strokes.ToArray();
                foreach (Stroke s in allStrokes)
                {
                    AppUndoManager.PushAction(new StrokeEraseUndoAction(Data.Id, s));
                }
                NoteInkCanvas.Strokes.Clear();
                SaveInkToData();
                SaveCurrentState();
            }
        }

        private void PencilPaletteColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                NoteStore store = ((App)Application.Current).Store;
                store.PencilColor = hex;
                DnoteStorage.Save(store);

                foreach (MainWindow window in Application.Current.Windows.OfType<MainWindow>())
                {
                    window.UpdatePencilDrawingAttributes();
                }

                PencilColorPalettePopup.IsOpen = false;
            }
        }

        private void CustomPencilColor_Click(object sender, RoutedEventArgs e)
        {
            NoteStore store = ((App)Application.Current).Store;
            using var dialog = new Forms.ColorDialog();

            try
            {
                Color current = (Color)ColorConverter.ConvertFromString(store.PencilColor);
                dialog.Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B);
            }
            catch
            {
                dialog.Color = System.Drawing.Color.Black;
            }

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                store.PencilColor = hex;
                if (!store.CustomPencilColors.Contains(hex))
                {
                    store.CustomPencilColors.Add(hex);
                }
                DnoteStorage.Save(store);

                foreach (MainWindow window in Application.Current.Windows.OfType<MainWindow>())
                {
                    window.UpdatePencilDrawingAttributes();
                }

                PencilColorPalettePopup.IsOpen = false;
            }
        }


        // =========================================================
        // ROTATION START
        // =========================================================

        public void EnsureOnScreen()
        {
            double screenLeft = SystemParameters.VirtualScreenLeft;
            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            double noteVisualLeft = Left + 300;
            double noteVisualTop = Top + 300;
            double noteW = Note.Width > 0 ? Note.Width : 150;
            double noteH = Note.Height > 0 ? Note.Height : 220;

            bool isOffScreen = (noteVisualLeft + noteW < screenLeft + 50) ||
                               (noteVisualLeft > screenLeft + screenWidth - 50) ||
                               (noteVisualTop + noteH < screenTop + 50) ||
                               (noteVisualTop > screenTop + screenHeight - 50);

            if (isOffScreen)
            {
                double primaryW = SystemParameters.PrimaryScreenWidth;
                double primaryH = SystemParameters.PrimaryScreenHeight;

                Left = Math.Max(0, (primaryW - noteW) / 2.0 - 300);
                Top = Math.Max(0, (primaryH - noteH) / 2.0 - 300);

                Data.Left = Left;
                Data.Top = Top;
            }
        }

        public void UpdateRotationOrigin()
        {
            NoteStore store = ((App)Application.Current).Store;
            if (store.RotationOrigin == "TopLeft")
            {
                Note.RenderTransformOrigin = new Point(0, 0);
            }
            else
            {
                Note.RenderTransformOrigin = new Point(0.5, 0);
            }
        }

        private Point GetRotationCenterScreen()
        {
            NoteStore store = ((App)Application.Current).Store;
            double w = Note.ActualWidth > 0 ? Note.ActualWidth : Note.Width;
            if (store.RotationOrigin == "TopLeft")
            {
                return PointToScreen(new Point(300, 300));
            }
            else
            {
                return PointToScreen(new Point(300 + w / 2.0, 300));
            }
        }

        private void StartRotation(
            MouseButtonEventArgs e)
        {
            Point mouse =
                PointToScreen(
                    e.GetPosition(this));

            Point center =
                GetRotationCenterScreen();

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

        private void RotationOriginTopCenter_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetGlobalRotationOrigin("TopCenter");
        }

        private void RotationOriginTopLeft_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetGlobalRotationOrigin("TopLeft");
        }

        public static void SetGlobalRotationOrigin(
            string origin)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            store.RotationOrigin = origin;
            DnoteStorage.Save(store);

            foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
            {
                win.UpdateRotationOrigin();
            }
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
        private void ApplyFontSelection(string fontName, double fontSize, bool isBold, bool isItalic)
        {
            if (!NoteText.Selection.IsEmpty)
            {
                NoteText.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(fontName));
                NoteText.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
                NoteText.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, isBold ? FontWeights.Bold : FontWeights.Normal);
                NoteText.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, isItalic ? FontStyles.Italic : FontStyles.Normal);
            }
            else
            {
                Data.FontFamily = fontName;
                Data.FontSize = fontSize;
                Data.IsBold = isBold;
                Data.IsItalic = isItalic;

                NoteText.FontFamily = new FontFamily(fontName);
                NoteText.FontSize = fontSize;
                NoteText.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
                NoteText.FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal;
            }

            UpdateDataFromWindow();
            DnoteStorage.Save(((App)Application.Current).Store);
            NoteText.Focus();
        }

        private void FontCustom_Click(object sender, RoutedEventArgs e)
        {
            string currentFont = Data.FontFamily ?? "Comic Sans MS";
            double currentSize = Data.FontSize > 0 ? Data.FontSize : 14;
            bool currentBold = Data.IsBold;
            bool currentItalic = Data.IsItalic;

            if (!NoteText.Selection.IsEmpty)
            {
                object fontVal = NoteText.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
                if (fontVal is FontFamily ff) currentFont = ff.Source;

                object sizeVal = NoteText.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                if (sizeVal is double sz) currentSize = sz;

                object weightVal = NoteText.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                if (weightVal is FontWeight fw) currentBold = (fw == FontWeights.Bold || fw == FontWeights.ExtraBold || fw == FontWeights.SemiBold);

                object styleVal = NoteText.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                if (styleVal is FontStyle fs) currentItalic = (fs == FontStyles.Italic);
            }

            FontSelectionWindow dialog = new FontSelectionWindow(currentFont, currentSize, currentBold, currentItalic);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                ApplyFontSelection(dialog.SelectedFontFamily, dialog.SelectedFontSize, dialog.SelectedIsBold, dialog.SelectedIsItalic);
            }
        }

        // =========================================================
        // ԹԵՐԹԻԿԻ ԳՈՒՅՆ (ԹԵՐԹԻԿԻ ՏԵՂԱՅԻՆ ՑԱՆԿ)
        // =========================================================

        private void SetCurrentNoteColor(string hex)
        {
            Data.NoteColor = hex;
            Note.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            UpdatePinVisual();
            UpdateDataFromWindow();
            DnoteStorage.Save(((App)Application.Current).Store);
        }

        private void NoteColorSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string hex)
            {
                SetCurrentNoteColor(hex);
                NoteContextMenu.IsOpen = false;
                e.Handled = true;
            }
        }

        private void NoteColorCustom_Click(object sender, RoutedEventArgs e)
        {
            Forms.ColorDialog dialog = new Forms.ColorDialog { FullOpen = true };
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string hex = string.Format("#{0:X2}{1:X2}{2:X2}", dialog.Color.R, dialog.Color.G, dialog.Color.B);
                SetCurrentNoteColor(hex);
                NoteContextMenu.IsOpen = false;
            }
        }

        private void NoteColorCustom_Click(object sender, MouseButtonEventArgs e)
        {
            NoteContextMenu.IsOpen = false;
            NoteColorCustom_Click(sender, (RoutedEventArgs)e);
            e.Handled = true;
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
            TogglePin_Click(sender, e);
        }

        private void TogglePin_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool newPinned = !Data.IsPinned;
            NoteStore store = ((App)Application.Current).Store;

            if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                foreach (Guid noteId in stack.NoteIds)
                {
                    if (store.Notes.TryGetValue(noteId, out NoteData? noteData))
                    {
                        noteData.IsPinned = newPinned;
                    }
                }

                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        if (newPinned)
                            win.PinNote();
                        else
                            win.UnpinNote();
                    }
                }

                DnoteStorage.Save(store);
            }
            else
            {
                if (newPinned)
                    PinNote();
                else
                    UnpinNote();
            }
        }

        public void UpdatePinVisual()
        {
            if (Data.IsPinned)
            {
                if (Data.StackId != null)
                {
                    NoteStore store = ((App)Application.Current).Store;
                    if (store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                    {
                        if (stack.CurrentNoteId != Data.Id)
                        {
                            PinButton.Visibility = Visibility.Collapsed;
                            return;
                        }
                    }
                }

                PinButton.Tag = PinImageGenerator.GetPinImageForColor(Data.NoteColor);
                PinButton.Visibility = Visibility.Visible;
            }
            else
            {
                PinButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PinNote()
        {
            Data.IsPinned = true;
            Topmost = true;
            UpdatePinVisual();
            PinMenuItem.Header = "Ապագամել";
            SaveCurrentState();
        }

        private void UnpinNote()
        {
            Data.IsPinned = false;
            Topmost = false;
            UpdatePinVisual();
            PinMenuItem.Header = "Գամել ամենավերեւում";
            SaveCurrentState();
        }


        // =========================================================
        // ՈՒՂՂԱՀԱՅԱՑ ԴԻՐՔ (0° ԱՆԿՅՈՒՆ)
        // =========================================================

        private void ResetRotationZero_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store = ((App)Application.Current).Store;
            Dictionary<Guid, double> previousAngles = new Dictionary<Guid, double>();

            if (Data.StackId != null && store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                foreach (Guid noteId in stack.NoteIds)
                {
                    if (store.Notes.TryGetValue(noteId, out NoteData? noteData))
                    {
                        if (Math.Abs(noteData.Rotation) > 0.001)
                        {
                            previousAngles[noteId] = noteData.Rotation;
                            noteData.Rotation = 0;
                        }
                    }
                }

                foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
                {
                    if (stack.NoteIds.Contains(win.NoteId))
                    {
                        win.NoteRotation.Angle = 0;
                        win.Data.Rotation = 0;
                        win.UpdateWindowSizeForRotation(false);
                        win.SaveCurrentState();
                    }
                }

                ApplyStackAlignment(stack);

                DnoteStorage.Save(store);
            }
            else
            {
                if (Math.Abs(Data.Rotation) > 0.001)
                {
                    previousAngles[Data.Id] = Data.Rotation;
                    NoteRotation.Angle = 0;
                    Data.Rotation = 0;
                    UpdateWindowSizeForRotation(false);
                    SaveCurrentState();
                }
            }

            if (previousAngles.Count > 0)
            {
                AppUndoManager.PushAction(new RotationResetUndoAction { PreviousAngles = previousAngles });
            }
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
                stack.NoteIds.Count > 1)
            {
                if (IsActive || Note.IsMouseOver || StackNavigationBar.IsMouseOver)
                {
                    StackNavigationBar.Visibility = Visibility.Visible;
                }
                else
                {
                    StackNavigationBar.Visibility = Visibility.Collapsed;
                }

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

            UpdatePinVisual();
        }

        private bool isNavigatingStack = false;

        public void SetActiveStackNote(NoteStack stack, Guid noteId, string? buttonType = null)
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

                // 2. Թարմացնում ենք տեսանելիությունը և Z-Order-ը
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
                            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                            if (hwnd != IntPtr.Zero)
                            {
                                SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                            }
                        }
                    }
                }

                // 3. Ակտիվացնում ենք ընտրված թերթիկը
                MainWindow? targetWin = openNotes.FirstOrDefault(n => n.NoteId == noteId);
                if (targetWin != null)
                {
                    targetWin.Visibility = Visibility.Visible;
                    IntPtr targetHwnd = new System.Windows.Interop.WindowInteropHelper(targetWin).Handle;
                    if (targetHwnd != IntPtr.Zero)
                    {
                        SetWindowPos(targetHwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    }

                    targetWin.ApplyStackAlignment(stack, targetWin);
                    targetWin.UpdateLayout();
                    targetWin.Activate();
                    targetWin.Focus();

                    // 4. Թարմացնում ենք UI-ները
                    foreach (MainWindow win in openNotes)
                    {
                        if (stack.NoteIds.Contains(win.NoteId))
                        {
                            win.UpdateStackUI();
                        }
                    }

                    targetWin.StackNavigationBar.Visibility = Visibility.Visible;

                    if (buttonType != null)
                    {
                        targetWin.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                        {
                            targetWin.UpdateLayout();
                            targetWin.StackNavigationBar.Visibility = Visibility.Visible;

                            Button? targetButton = buttonType switch
                            {
                                "Next" => targetWin.StackNextButton.IsEnabled ? targetWin.StackNextButton : targetWin.StackPrevButton,
                                "Prev" => targetWin.StackPrevButton.IsEnabled ? targetWin.StackPrevButton : targetWin.StackNextButton,
                                "First" => targetWin.StackFirstButton.IsEnabled ? targetWin.StackFirstButton : targetWin.StackNextButton,
                                "Last" => targetWin.StackLastButton.IsEnabled ? targetWin.StackLastButton : targetWin.StackPrevButton,
                                _ => null
                            };

                            if (targetButton != null)
                            {
                                targetButton.Focus();
                                try
                                {
                                    double bw = targetButton.ActualWidth > 0 ? targetButton.ActualWidth : 14.0;
                                    double bh = targetButton.ActualHeight > 0 ? targetButton.ActualHeight : 16.0;
                                    Point buttonCenter = targetButton.PointToScreen(new Point(bw / 2.0, bh / 2.0));
                                    SetCursorPos((int)Math.Round(buttonCenter.X), (int)Math.Round(buttonCenter.Y));
                                }
                                catch
                                {
                                    try
                                    {
                                        Point navCenter = targetWin.StackNavigationBar.PointToScreen(
                                            new Point(targetWin.StackNavigationBar.ActualWidth / 2.0, targetWin.StackNavigationBar.ActualHeight / 2.0));
                                        SetCursorPos((int)Math.Round(navCenter.X), (int)Math.Round(navCenter.Y));
                                    }
                                    catch { }
                                }
                            }
                        }));
                    }
                    else
                    {
                        targetWin.Focus();
                    }
                }
                else
                {
                    // 4. Թարմացնում ենք UI-ները
                    foreach (MainWindow win in openNotes)
                    {
                        if (stack.NoteIds.Contains(win.NoteId))
                        {
                            win.UpdateStackUI();
                        }
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
            if (isPencilActive && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl))
            {
                SetEraserMode(true);
            }

            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (isCtrl)
            {
                if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                {
                    if (isPencilActive)
                    {
                        if (AppUndoManager.CanUndo)
                        {
                            AppUndoManager.PerformUndo();
                            e.Handled = true;
                            return;
                        }
                    }
                    else
                    {
                        if (NoteText.IsFocused && NoteText.CanUndo)
                        {
                            return; // RichTextBox handles typing undo
                        }

                        if (AppUndoManager.CanUndo)
                        {
                            AppUndoManager.PerformUndo();
                            e.Handled = true;
                            return;
                        }
                    }
                }
                else if (e.Key == Key.Y || (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
                {
                    if (isPencilActive)
                    {
                        if (AppUndoManager.CanRedo)
                        {
                            AppUndoManager.PerformRedo();
                            e.Handled = true;
                            return;
                        }
                    }
                    else
                    {
                        if (NoteText.IsFocused && NoteText.CanRedo)
                        {
                            return; // RichTextBox handles typing redo
                        }

                        if (AppUndoManager.CanRedo)
                        {
                            AppUndoManager.PerformRedo();
                            e.Handled = true;
                            return;
                        }
                    }
                }
                else if (e.Key == Key.N)
                {
                    NewNote_Click(sender, e);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.F)
                {
                    SearchNotes_Click(sender, e);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.W)
                {
                    CloseMenu_Click(sender, e);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.P)
                {
                    TogglePencilMode();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Escape && isPencilActive)
            {
                TogglePencilMode(false);
                e.Handled = true;
                return;
            }

            if (Data.StackId != null)
            {
                if (e.Key == Key.Left)
                {
                    NavigateStack(-1, "Prev");
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    NavigateStack(1, "Next");
                    e.Handled = true;
                }
            }
        }

        private void NoteWindow_PreviewKeyUp(
            object sender,
            KeyEventArgs e)
        {
            if (isPencilActive && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl))
            {
                SetEraserMode(false);
            }
        }

        private void StackFirst_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStackAbsolute(0, "First");
        }

        private void StackPrev_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStack(-1, "Prev");
        }

        private void StackNext_Click(
            object sender,
            RoutedEventArgs e)
        {
            NavigateStack(1, "Next");
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
                NavigateStackAbsolute(stack.NoteIds.Count - 1, "Last");
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
            int targetIndex,
            string? buttonType = null)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId == null ||
                !store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                return;

            if (targetIndex < 0 || targetIndex >= stack.NoteIds.Count) return;

            Guid targetNoteId =
                stack.NoteIds[targetIndex];

            SetActiveStackNote(stack, targetNoteId, buttonType);
        }

        private void NavigateStack(
            int step,
            string? buttonType = null)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId == null ||
                !store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
                return;

            int currentIndex =
                stack.NoteIds.IndexOf(Data.Id);

            if (currentIndex < 0)
                currentIndex = stack.NoteIds.IndexOf(stack.CurrentNoteId ?? Data.Id);

            if (currentIndex < 0)
                currentIndex = 0;

            NavigateStackAbsolute(currentIndex + step, buttonType);
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

            // Հեռացնում ենք թերթիկների հին տրցակների գրառումները
            HashSet<Guid> oldStackIds = notes
                .Where(n => n.StackId != null)
                .Select(n => n.StackId!.Value)
                .ToHashSet();

            foreach (Guid oldId in oldStackIds)
            {
                store.Stacks.Remove(oldId);
            }

            NoteStack stack = new NoteStack
            {
                Id = Guid.NewGuid(),
                NoteIds = notes.Select(n => n.Id).ToList(),
                CurrentNoteId = notes.Last().Id, // ամենանորը վերեւում
                Alignment = (store.StackAlignment == "TopLeft" || string.IsNullOrEmpty(store.StackAlignment)) ? "TopCenter" : store.StackAlignment
            };

            store.Stacks[stack.Id] = stack;

            foreach (NoteData n in notes)
            {
                n.StackId = stack.Id;
            }

            anchorWin.ApplyStackAlignment(stack, anchorWin);

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

        private void DetachFromStackMenu_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            if (Data.StackId != null &&
                store.Stacks.TryGetValue(Data.StackId.Value, out NoteStack? stack))
            {
                DetachFromStack(stack);
            }
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
            NoteStack stack,
            MainWindow? anchorWin = null)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            MainWindow[] openNotes =
                Application.Current.Windows.OfType<MainWindow>().ToArray();

            MainWindow refWin = anchorWin
                ?? openNotes.FirstOrDefault(w => w.NoteId == (stack.CurrentNoteId ?? stack.NoteIds.LastOrDefault()))
                ?? this;

            double refW = refWin.Note.Width > 0 ? refWin.Note.Width : refWin.Data.Width;
            double refLeft = refWin.Left;
            double refTop = refWin.Top;

            string alignment = stack.Alignment;
            if (string.IsNullOrEmpty(alignment) || alignment == "TopLeft")
            {
                alignment = (store.StackAlignment == "TopLeft" || string.IsNullOrEmpty(store.StackAlignment)) ? "TopCenter" : store.StackAlignment;
                stack.Alignment = alignment;
            }

            foreach (Guid id in stack.NoteIds)
            {
                MainWindow? win =
                    openNotes.FirstOrDefault(w => w.NoteId == id);

                if (win != null && win != refWin)
                {
                    if (stack.IsZeroRotation)
                    {
                        win.NoteRotation.Angle = 0;
                        win.Data.Rotation = 0;
                        win.UpdateWindowSizeForRotation(false);
                    }

                    double winW = win.Note.Width > 0 ? win.Note.Width : win.Data.Width;
                    if (alignment == "TopLeft")
                    {
                        win.Left = refLeft;
                        win.Top = refTop;
                    }
                    else if (alignment == "TopRight")
                    {
                        win.Left = refLeft + (refW - winW);
                        win.Top = refTop;
                    }
                    else // "TopCenter" (լռելյայն)
                    {
                        // Վերին եզրի միջնակետի համընկնում բոլոր թերթիկների համար՝
                        // refWin.Left + refW / 2 == win.Left + winW / 2 => win.Left = refLeft + (refW - winW) / 2
                        win.Left = refLeft + (refW - winW) / 2.0;
                        win.Top = refTop;
                    }

                    win.Data.Left = win.Left;
                    win.Data.Top = win.Top;
                }
            }

            refWin.Data.Left = refWin.Left;
            refWin.Data.Top = refWin.Top;
        }

        public Rect GetScreenBounds()
        {
            double w = Note.ActualWidth > 0 ? Note.ActualWidth : Note.Width;
            double h = Note.ActualHeight > 0 ? Note.ActualHeight : Note.Height;
            if (w <= 0) w = 150;
            if (h <= 0) h = 220;

            double angleRad = NoteRotation.Angle * Math.PI / 180.0;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);

            // Pivot point is top center in Note: (w / 2.0, 0)
            // In window space: (300 + w / 2.0, 300)
            double cx = Left + 300 + w / 2.0;
            double cy = Top + 300;

            Point[] localCorners = new Point[]
            {
                new Point(-w / 2.0, 0),
                new Point(w / 2.0, 0),
                new Point(w / 2.0, h),
                new Point(-w / 2.0, h)
            };

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var p in localCorners)
            {
                double rx = cx + p.X * cos - p.Y * sin;
                double ry = cy + p.X * sin + p.Y * cos;

                if (rx < minX) minX = rx;
                if (rx > maxX) maxX = rx;
                if (ry < minY) minY = ry;
                if (ry > maxY) maxY = ry;
            }

            return new Rect(minX, minY, Math.Max(10, maxX - minX), Math.Max(10, maxY - minY));
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

    // =========================================================
    // UNDO MANAGER (ՀԵՏԱՐԿՄԱՆ ՀԱՄԱԿԱՐԳ)
    // =========================================================

    public interface IAppUndoAction
    {
        string Description { get; }
        string RedoDescription { get; }
        void Undo();
        void Redo();
    }

    public class RotationResetUndoAction : IAppUndoAction
    {
        public string Description => "Հետարկել ուղղահայաց դիրքը";
        public string RedoDescription => "Վերարկել ուղղահայաց դիրքը";
        public Dictionary<Guid, double> PreviousAngles { get; set; } = new Dictionary<Guid, double>();

        public void Undo()
        {
            NoteStore store = ((App)Application.Current).Store;
            foreach (var kvp in PreviousAngles)
            {
                if (store.Notes.TryGetValue(kvp.Key, out NoteData? noteData))
                {
                    noteData.Rotation = kvp.Value;
                }

                MainWindow? win = Application.Current.Windows.OfType<MainWindow>()
                    .FirstOrDefault(w => w.NoteId == kvp.Key);

                if (win != null)
                {
                    win.Data.Rotation = kvp.Value;
                    win.NoteRotation.Angle = kvp.Value;
                    win.UpdateWindowSizeForRotation(false);
                    win.SaveCurrentState();
                }
            }
            DnoteStorage.Save(store);
        }

        public void Redo()
        {
            NoteStore store = ((App)Application.Current).Store;
            foreach (var kvp in PreviousAngles)
            {
                if (store.Notes.TryGetValue(kvp.Key, out NoteData? noteData))
                {
                    noteData.Rotation = 0;
                }

                MainWindow? win = Application.Current.Windows.OfType<MainWindow>()
                    .FirstOrDefault(w => w.NoteId == kvp.Key);

                if (win != null)
                {
                    win.Data.Rotation = 0;
                    win.NoteRotation.Angle = 0;
                    win.UpdateWindowSizeForRotation(false);
                    win.SaveCurrentState();
                }
            }
            DnoteStorage.Save(store);
        }
    }

    public class DeleteNoteUndoAction : IAppUndoAction
    {
        public string Description => "Հետարկել ջնջումը";
        public string RedoDescription => "Վերարկել ջնջումը";
        public Guid NoteId { get; set; }

        public void Undo()
        {
            NoteStore store = ((App)Application.Current).Store;
            if (!store.DeletedNotes.TryGetValue(NoteId, out NoteData? restoredData))
            {
                if (store.DeletedNotes.Count > 0)
                {
                    NoteId = new List<Guid>(store.DeletedNotes.Keys)[store.DeletedNotes.Count - 1];
                    restoredData = store.DeletedNotes[NoteId];
                }
                else return;
            }

            store.DeletedNotes.Remove(NoteId);
            store.Notes[NoteId] = restoredData;

            MainWindow restoredNote = new MainWindow(restoredData);
            restoredNote.ShowInTaskbar = false;
            restoredNote.Show();
            restoredNote.Activate();
            restoredNote.SaveCurrentState();
            DnoteStorage.Save(store);
        }

        public void Redo()
        {
            NoteStore store = ((App)Application.Current).Store;
            MainWindow? win = Application.Current.Windows.OfType<MainWindow>()
                .FirstOrDefault(w => w.NoteId == NoteId);

            if (win != null)
            {
                win.Close();
            }
            else if (store.Notes.TryGetValue(NoteId, out NoteData? data))
            {
                store.Notes.Remove(NoteId);
                store.DeletedNotes[NoteId] = data;
                DnoteStorage.Save(store);
            }
        }
    }

    public class StrokeAddUndoAction : IAppUndoAction
    {
        public string Description => "Հետարկել մատիտի գիծը";
        public string RedoDescription => "Վերարկել մատիտի գիծը";
        private readonly Guid noteId;
        private readonly Stroke stroke;

        public StrokeAddUndoAction(Guid noteId, Stroke stroke)
        {
            this.noteId = noteId;
            this.stroke = stroke;
        }

        public void Undo()
        {
            MainWindow? win = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w.NoteId == noteId);
            if (win != null && win.NoteInkCanvas.Strokes.Contains(stroke))
            {
                win.NoteInkCanvas.Strokes.Remove(stroke);
                win.SaveInkToData();
                win.SaveCurrentState();
            }
        }

        public void Redo()
        {
            MainWindow? win = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w.NoteId == noteId);
            if (win != null && !win.NoteInkCanvas.Strokes.Contains(stroke))
            {
                win.NoteInkCanvas.Strokes.Add(stroke);
                win.SaveInkToData();
                win.SaveCurrentState();
            }
        }
    }

    public class StrokeEraseUndoAction : IAppUndoAction
    {
        public string Description => "Հետարկել մատիտի ջնջումը";
        public string RedoDescription => "Վերարկել մատիտի ջնջումը";
        private readonly Guid noteId;
        private readonly Stroke stroke;

        public StrokeEraseUndoAction(Guid noteId, Stroke stroke)
        {
            this.noteId = noteId;
            this.stroke = stroke;
        }

        public void Undo()
        {
            MainWindow? win = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w.NoteId == noteId);
            if (win != null && !win.NoteInkCanvas.Strokes.Contains(stroke))
            {
                win.NoteInkCanvas.Strokes.Add(stroke);
                win.SaveInkToData();
                win.SaveCurrentState();
            }
        }

        public void Redo()
        {
            MainWindow? win = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w.NoteId == noteId);
            if (win != null && win.NoteInkCanvas.Strokes.Contains(stroke))
            {
                win.NoteInkCanvas.Strokes.Remove(stroke);
                win.SaveInkToData();
                win.SaveCurrentState();
            }
        }
    }

    public class NotePropertiesSnapshot
    {
        public Guid NoteId { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public string NoteColor { get; set; } = string.Empty;
        public string TextColor { get; set; } = string.Empty;
        public string FontFamily { get; set; } = string.Empty;
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
    }

    public class ApplyPreferencesUndoAction : IAppUndoAction
    {
        public string Description => "Հետարկել նախընտրանքների կիրառումը";
        public string RedoDescription => "Վերարկել նախընտրանքների կիրառումը";

        public List<NotePropertiesSnapshot> PreviousSnapshots { get; set; } = new List<NotePropertiesSnapshot>();
        public List<NotePropertiesSnapshot> NewSnapshots { get; set; } = new List<NotePropertiesSnapshot>();
        public Dictionary<Guid, string> PreviousStackAlignments { get; set; } = new Dictionary<Guid, string>();
        public string NewStackAlignment { get; set; } = string.Empty;

        public void Undo()
        {
            RestoreSnapshots(PreviousSnapshots, PreviousStackAlignments);
        }

        public void Redo()
        {
            Dictionary<Guid, string> newAlignments = new Dictionary<Guid, string>();
            NoteStore store = ((App)Application.Current).Store;
            foreach (var stackId in store.Stacks.Keys)
            {
                newAlignments[stackId] = NewStackAlignment;
            }
            RestoreSnapshots(NewSnapshots, newAlignments);
        }

        private void RestoreSnapshots(List<NotePropertiesSnapshot> snapshots, Dictionary<Guid, string> stackAlignments)
        {
            NoteStore store = ((App)Application.Current).Store;
            MainWindow[] openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();

            foreach (var snap in snapshots)
            {
                if (store.Notes.TryGetValue(snap.NoteId, out NoteData? data))
                {
                    data.Width = snap.Width;
                    data.Height = snap.Height;
                    data.Left = snap.Left;
                    data.Top = snap.Top;
                    data.NoteColor = snap.NoteColor;
                    data.TextColor = snap.TextColor;
                    data.FontFamily = snap.FontFamily;
                    data.FontSize = snap.FontSize;
                    data.IsBold = snap.IsBold;
                    data.IsItalic = snap.IsItalic;
                }

                MainWindow? win = openNotes.FirstOrDefault(w => w.NoteId == snap.NoteId);
                if (win != null)
                {
                    win.Note.Width = snap.Width;
                    win.Note.Height = snap.Height;
                    win.Left = snap.Left;
                    win.Top = snap.Top;

                    if (!string.IsNullOrEmpty(snap.NoteColor))
                    {
                        win.Note.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(snap.NoteColor));
                    }
                    if (!string.IsNullOrEmpty(snap.FontFamily))
                    {
                        win.NoteText.FontFamily = new FontFamily(snap.FontFamily);
                    }
                    if (snap.FontSize > 0)
                    {
                        win.NoteText.FontSize = snap.FontSize;
                    }
                    win.NoteText.FontWeight = snap.IsBold ? FontWeights.Bold : FontWeights.Normal;
                    win.NoteText.FontStyle = snap.IsItalic ? FontStyles.Italic : FontStyles.Normal;
                    if (!string.IsNullOrEmpty(snap.TextColor))
                    {
                        win.NoteText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(snap.TextColor));
                    }

                    win.UpdateWindowSizeForRotation(false);
                    win.SaveCurrentState();
                }
            }

            foreach (var kvp in stackAlignments)
            {
                if (store.Stacks.TryGetValue(kvp.Key, out NoteStack? stack))
                {
                    stack.Alignment = kvp.Value;
                    MainWindow? topWin = openNotes.FirstOrDefault(w => w.NoteId == (stack.CurrentNoteId ?? stack.NoteIds.LastOrDefault()));
                    if (topWin != null)
                    {
                        topWin.ApplyStackAlignment(stack);
                    }
                }
            }

            DnoteStorage.Save(store);
        }
    }

    public static class AppUndoManager
    {
        private static readonly List<IAppUndoAction> undoHistory = new List<IAppUndoAction>();
        private static readonly List<IAppUndoAction> redoHistory = new List<IAppUndoAction>();
        private const int MaxHistory = 5;

        public static bool CanUndo =>
            undoHistory.Count > 0 || ((App)Application.Current).Store.DeletedNotes.Count > 0;

        public static bool CanRedo =>
            redoHistory.Count > 0;

        public static string CurrentUndoDescription
        {
            get
            {
                if (undoHistory.Count > 0)
                    return undoHistory[undoHistory.Count - 1].Description;
                if (((App)Application.Current).Store.DeletedNotes.Count > 0)
                    return "Հետարկել ջնջումը";
                return "Հետարկել";
            }
        }

        public static string CurrentRedoDescription
        {
            get
            {
                if (redoHistory.Count > 0)
                    return redoHistory[redoHistory.Count - 1].RedoDescription;
                return "Վերարկել";
            }
        }

        public static void PushAction(IAppUndoAction action)
        {
            undoHistory.Add(action);
            if (undoHistory.Count > MaxHistory)
            {
                undoHistory.RemoveAt(0);
            }
            redoHistory.Clear();
        }

        public static void PerformUndo()
        {
            if (undoHistory.Count > 0)
            {
                IAppUndoAction action = undoHistory[undoHistory.Count - 1];
                undoHistory.RemoveAt(undoHistory.Count - 1);
                action.Undo();

                redoHistory.Add(action);
                if (redoHistory.Count > MaxHistory)
                {
                    redoHistory.RemoveAt(0);
                }
            }
            else
            {
                NoteStore store = ((App)Application.Current).Store;
                if (store.DeletedNotes.Count > 0)
                {
                    Guid id = new List<Guid>(store.DeletedNotes.Keys)[store.DeletedNotes.Count - 1];
                    var action = new DeleteNoteUndoAction { NoteId = id };
                    action.Undo();

                    redoHistory.Add(action);
                    if (redoHistory.Count > MaxHistory)
                    {
                        redoHistory.RemoveAt(0);
                    }
                }
            }
        }

        public static void PerformRedo()
        {
            if (redoHistory.Count > 0)
            {
                IAppUndoAction action = redoHistory[redoHistory.Count - 1];
                redoHistory.RemoveAt(redoHistory.Count - 1);
                action.Redo();

                undoHistory.Add(action);
                if (undoHistory.Count > MaxHistory)
                {
                    undoHistory.RemoveAt(0);
                }
            }
        }
    }

    // =========================================================
    // PIN IMAGE GENERATOR (ԳԱՄԻ ՊԱՏԿԵՐԻ ԳՈՒՆԱՎՈՐՈՒՄ)
    // =========================================================

    public static class PinImageGenerator
    {
        private static BitmapSource? originalPinSource;
        private static readonly Dictionary<string, BitmapSource> cachedTintedPins =
            new Dictionary<string, BitmapSource>();

        public static BitmapSource GetPinImageForColor(string noteColorHex)
        {
            if (string.IsNullOrEmpty(noteColorHex)) noteColorHex = "#FFF9A6";
            noteColorHex = noteColorHex.ToUpperInvariant();

            if (cachedTintedPins.TryGetValue(noteColorHex, out BitmapSource? cached))
            {
                return cached;
            }

            if (originalPinSource == null)
            {
                originalPinSource = LoadOriginalPin();
            }

            if (originalPinSource == null)
            {
                return new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);
            }

            Color targetColor;
            try
            {
                targetColor = (Color)ColorConverter.ConvertFromString(noteColorHex);
            }
            catch
            {
                targetColor = (Color)ColorConverter.ConvertFromString("#FFF9A6");
            }

            BitmapSource tinted = CreateTintedPin(originalPinSource, targetColor);
            tinted.Freeze();
            cachedTintedPins[noteColorHex] = tinted;
            return tinted;
        }

        private static BitmapSource? LoadOriginalPin()
        {
            try
            {
                Uri uri = new Uri("pack://application:,,,/Assets/Pin.png", UriKind.Absolute);
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                try
                {
                    string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Pin.png");
                    if (!System.IO.File.Exists(path))
                    {
                        path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pin.png");
                    }
                    if (System.IO.File.Exists(path))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
                catch { }
            }
            return null;
        }

        private static BitmapSource CreateTintedPin(BitmapSource src, Color targetColor)
        {
            FormatConvertedBitmap converted = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            ColorToHsl(targetColor, out double targetH, out double targetS, out double targetL);

            // Enhance saturation for pastels so the plastic pin looks rich and solid
            if (targetS < 0.15 && targetL > 0.8) // Near white / gray
            {
                targetS = 0.04;
            }
            else
            {
                targetS = Math.Min(1.0, Math.Max(0.70, targetS * 1.5));
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                if (a == 0) continue;

                int max = Math.Max(r, Math.Max(g, b));
                int min = Math.Min(r, Math.Min(g, b));
                double chroma = (max - min) / 255.0;

                // Soft black/gray drop shadow — preserve untouched
                if (chroma < 0.08 && max < 120)
                {
                    continue;
                }

                ColorToHsl(Color.FromArgb(a, r, g, b), out double origH, out double origS, out double origL);

                double finalH = targetH;
                double finalS = targetS;
                double finalL = origL;

                // Bright specular highlight fades saturation to crisp white
                if (origL > 0.82)
                {
                    finalS = targetS * Math.Max(0, (1.0 - (origL - 0.82) / 0.18));
                }

                HslToRgb(finalH, finalS, finalL, out byte outR, out byte outG, out byte outB);

                pixels[i] = outB;
                pixels[i + 1] = outG;
                pixels[i + 2] = outR;
            }

            WriteableBitmap result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        private static void ColorToHsl(Color color, out double h, out double s, out double l)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = (l <= 0.5) ? (delta / (max + min)) : (delta / (2.0 - max - min));

                if (r == max)
                    h = ((g - b) / delta) + (g < b ? 6 : 0);
                else if (g == max)
                    h = ((b - r) / delta) + 2;
                else
                    h = ((r - g) / delta) + 4;

                h /= 6.0;
            }
        }

        private static void HslToRgb(double h, double s, double l, out byte r, out byte g, out byte b)
        {
            double rVal, gVal, bVal;

            if (s == 0)
            {
                rVal = gVal = bVal = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
                double p = 2.0 * l - q;
                rVal = HueToRgb(p, q, h + 1.0 / 3.0);
                gVal = HueToRgb(p, q, h);
                bVal = HueToRgb(p, q, h - 1.0 / 3.0);
            }

            r = (byte)Math.Clamp((int)Math.Round(rVal * 255.0), 0, 255);
            g = (byte)Math.Clamp((int)Math.Round(gVal * 255.0), 0, 255);
            b = (byte)Math.Clamp((int)Math.Round(bVal * 255.0), 0, 255);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }
    }
}
