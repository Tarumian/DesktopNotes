using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;

namespace DesktopNotes;

public partial class App : Application
{
    public NoteStore Store { get; } =
        new NoteStore();

    private TaskbarIcon? trayIcon;


    public App()
    {
        DnoteStorage.Load(Store);
    }


    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);


        // -------------------------------------------------
        // SYSTEM TRAY
        // -------------------------------------------------

        trayIcon = new TaskbarIcon
{
    ToolTipText = "DesktopNotes",
    Visibility = Visibility.Visible,
    IconSource = new BitmapImage(
        new Uri(
            "pack://application:,,,/Assets/DesktopNotes.ico",
            UriKind.Absolute))
};

        trayIcon.ForceCreate();


        // -------------------------------------------------
        // SYSTEM TRAY — ՑԱՆԿ
        // -------------------------------------------------

        ContextMenu menu =
            new ContextMenu();


        // -------------------------------------------------
        // Նոր թերթիկ
        // -------------------------------------------------

        MenuItem newNoteItem =
            new MenuItem
            {
                Header = "Նոր թերթիկ"
            };

        newNoteItem.Click +=
            (sender, args) =>
            {
                MainWindow newNote =
                    new MainWindow();

                newNote.Show();
                newNote.Activate();
                newNote.NoteText.Focus();
            };


        // -------------------------------------------------
        // Ցույց տալ բոլոր թերթիկները
        // -------------------------------------------------

        MenuItem showAllItem =
            new MenuItem
            {
                Header = "Ցույց տալ բոլոր թերթիկները"
            };

        showAllItem.Click +=
            (sender, args) =>
            {
                MainWindow[] openNotes =
                    Application.Current.Windows
                        .OfType<MainWindow>()
                        .ToArray();


                // Բացում ենք բոլոր գործող թերթիկները,
                // ներառյալ ×-ով փակվածները։

                foreach (NoteData data in Store.Notes.Values)
                {
                    MainWindow? openNote =
                        openNotes.FirstOrDefault(
                            note => note.NoteId == data.Id);

                    if (openNote != null)
                    {
                        openNote.Show();
                        openNote.Activate();
                        continue;
                    }

                    data.IsClosed = false;

                    MainWindow note =
                        new MainWindow(data);

                    note.ShowInTaskbar = false;
                    note.Show();
                }

                DnoteStorage.Save(Store);
            };


        // -------------------------------------------------
        // Փակել բոլոր թերթիկները
        // -------------------------------------------------

        MenuItem closeAllItem =
            new MenuItem
            {
                Header = "Փակել բոլոր թերթիկները"
            };

        closeAllItem.Click +=
            (sender, args) =>
            {
                CloseAllNotes();
            };


        // -------------------------------------------------
        // Գործարկել Windows-ի հետ
        // -------------------------------------------------

        MenuItem startupItem =
            new MenuItem
            {
                Header = "Գործարկել Windows-ի հետ",
                IsCheckable = true,
                IsChecked = StartupManager.IsEnabled()
            };

        startupItem.Click +=
            (sender, args) =>
            {
                if (startupItem.IsChecked)
                {
                    StartupManager.Enable();
                }
                else
                {
                    StartupManager.Disable();
                }
            };


        // -------------------------------------------------
        // Ելք
        // -------------------------------------------------

        MenuItem exitItem =
            new MenuItem
            {
                Header = "Ելք"
            };

        exitItem.Click +=
            (sender, args) =>
            {
                CloseAllNotes();
                Shutdown();
            };


        // -------------------------------------------------
        // ՑԱՆԿԻ ԿԱՌՈՒՑՈՒՄ
        // -------------------------------------------------

        menu.Items.Add(newNoteItem);

// -------------------------------------------------
// Որոնել թերթիկներ
// -------------------------------------------------

MenuItem searchItem =
    new MenuItem
    {
        Header = "Որոնել թերթիկներ"
    };

searchItem.Click +=
    (sender, args) =>
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
    };

        menu.Items.Add(searchItem);
        menu.Items.Add(showAllItem);
        menu.Items.Add(closeAllItem);
        menu.Items.Add(startupItem);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(exitItem);


        trayIcon.ContextMenu = menu;


        // -------------------------------------------------
        // Պահպանված թերթիկները
        // -------------------------------------------------

foreach (NoteData data in Store.Notes.Values)
{
    // Բացելուց առաջ ապահովության համար նշում ենք, որ բաց է
    data.IsClosed = false;

    MainWindow note =
        new MainWindow(data);

    note.ShowInTaskbar = false;
    note.Show();
    note.Activate();
}


        // -------------------------------------------------
        // --new-note
        // -------------------------------------------------

        bool createNewNote =
            Array.Exists(
                e.Args,
                argument =>
                    string.Equals(
                        argument,
                        "--new-note",
                        StringComparison.OrdinalIgnoreCase));

        if (createNewNote)
        {
            MainWindow newNote =
                new MainWindow();

            newNote.Show();
            newNote.Activate();
            newNote.NoteText.Focus();
        }
    }


    // =====================================================
    // ՓԱԿԵԼ ԲՈԼՈՐ ԹԵՐԹԻԿՆԵՐԸ
    // =====================================================

    private void CloseAllNotes()
    {
        MainWindow[] notes =
            Application.Current.Windows
                .OfType<MainWindow>()
                .ToArray();

        foreach (MainWindow note in notes)
        {
            note.Close();
        }
    }


    // =====================================================
    // ԵԼՔ
    // =====================================================

    protected override void OnExit(
        ExitEventArgs e)
    {
        trayIcon?.Dispose();

        base.OnExit(e);
    }
}
