using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        trayIcon =
            new TaskbarIcon
            {
                ToolTipText = "DesktopNotes",
                Visibility = Visibility.Visible,

                IconSource =
                    new GeneratedIconSource
                    {
                        Text = "D",
                        FontSize = 32,
                        FontWeight = FontWeights.Bold
                    }
            };

        trayIcon.ForceCreate();


        // -------------------------------------------------
        // SYSTEM TRAY — ՑԱՆԿ
        // -------------------------------------------------

        ContextMenu menu =
            new ContextMenu();


        // Նոր թերթիկ
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


        // Ցույց տալ բոլոր թերթիկները
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

        // Եթե թերթիկներ արդեն բաց են՝
        // պարզապես բերում ենք առաջ
        if (openNotes.Length > 0)
        {
            foreach (MainWindow note in openNotes)
            {
                note.Show();
                note.Activate();
            }

            return;
        }

        // Եթե բաց թերթիկներ չկան՝
        // վերականգնում ենք պահպանվածները
        foreach (NoteData data in Store.Notes.Values)
        {
            MainWindow note =
                new MainWindow(data);

            note.ShowInTaskbar = false;
            note.Show();
        }
    };


        // Փակել բոլոր թերթիկները
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


        // Ելք
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


        menu.Items.Add(newNoteItem);
        menu.Items.Add(showAllItem);
        menu.Items.Add(closeAllItem);

        menu.Items.Add(
            new Separator());

        menu.Items.Add(exitItem);


        trayIcon.ContextMenu = menu;


        // -------------------------------------------------
        // Պահպանված թերթիկները
        // -------------------------------------------------

        foreach (NoteData data in Store.Notes.Values)
        {
            MainWindow note =
                new MainWindow(data);

            note.ShowInTaskbar = false;

            note.Show();
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