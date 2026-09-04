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
            Visibility = Visibility.Visible
        };

        try
        {
            var streamInfo = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/DesktopNotes.ico", UriKind.Absolute));
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                trayIcon.Icon = new System.Drawing.Icon(stream);
            }
        }
        catch
        {
            trayIcon.IconSource = new BitmapImage(
                new Uri("pack://application:,,,/Assets/DesktopNotes.ico", UriKind.Absolute));
        }


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
                    data.IsClosed = false;

                    MainWindow? openNote =
                        openNotes.FirstOrDefault(
                            note => note.NoteId == data.Id);

                    if (openNote != null)
                    {
                        openNote.Visibility = Visibility.Visible;
                        openNote.WindowState = WindowState.Normal;
                        openNote.Show();
                        openNote.Activate();
                        continue;
                    }

                    MainWindow note =
                        new MainWindow(data);

                    note.ShowInTaskbar = false;
                    note.Show();
                    note.WindowState = WindowState.Normal;
                    note.Activate();
                }

                // Restore active note in every stack
                openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();
                foreach (NoteStack stack in Store.Stacks.Values)
                {
                    Guid targetId = stack.CurrentNoteId ?? stack.NoteIds.LastOrDefault();
                    MainWindow? topWin = openNotes.FirstOrDefault(w => w.NoteId == targetId);
                    if (topWin != null)
                    {
                        topWin.SetActiveStackNote(stack, targetId);
                    }
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

// -------------------------------------------------
// Նախընտրություններ
// -------------------------------------------------

MenuItem preferencesItem =
    new MenuItem
    {
        Header = "Նախընտրություններ"
    };

preferencesItem.Click +=
    (sender, args) =>
    {
        PreferencesWindow[] prefWindows =
            Application.Current.Windows
                .OfType<PreferencesWindow>()
                .ToArray();

        if (prefWindows.Length > 0)
        {
            prefWindows[0].Show();
            prefWindows[0].Activate();
            return;
        }

        PreferencesWindow prefWindow =
            new PreferencesWindow();

        prefWindow.Show();
        prefWindow.Activate();
    };

        menu.Items.Add(searchItem);
        menu.Items.Add(preferencesItem);
        menu.Items.Add(showAllItem);
        menu.Items.Add(closeAllItem);
        menu.Items.Add(startupItem);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(exitItem);


        trayIcon.ContextMenu = menu;

        try
        {
            trayIcon.ForceCreate();
        }
        catch
        {
        }


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
        }

        // Տրցակների վերին թերթիկների ակտիվացում
        foreach (NoteStack stack in Store.Stacks.Values)
        {
            Guid targetId = stack.CurrentNoteId ?? stack.NoteIds.LastOrDefault();
            MainWindow? topWin = Application.Current.Windows.OfType<MainWindow>()
                .FirstOrDefault(w => w.NoteId == targetId);
            if (topWin != null)
            {
                topWin.ApplyStackAlignment(stack);
                topWin.SetActiveStackNote(stack, targetId);
            }
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
