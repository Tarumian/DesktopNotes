using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;

namespace Hushak;

public partial class App : Application
{
    public NoteStore Store { get; } = new NoteStore();

    private TaskbarIcon? trayIcon;

    private const string AppMutexName = @"Local\Hushak_SingleInstance_Mutex";
    private const string AppWakeEventName = @"Local\Hushak_Wake_Event";
    private const string AppNewNoteEventName = @"Local\Hushak_NewNote_Event";

    private System.Threading.Mutex? appMutex;
    private System.Threading.EventWaitHandle? wakeEvent;
    private System.Threading.EventWaitHandle? newNoteEvent;
    private System.Threading.RegisteredWaitHandle? registeredWakeWait;
    private System.Threading.RegisteredWaitHandle? registeredNewNoteWait;

    public App()
    {
        DnoteStorage.Load(Store);
        LocalizationManager.Instance.CurrentLanguage = Store.Language;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (trayIcon != null)
        {
            trayIcon.ToolTipText = LocalizationManager.Get("AppName");
        }
        BuildTrayMenu();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        bool createNewNoteArg = Array.Exists(
            e.Args,
            argument => string.Equals(argument, "--new-note", StringComparison.OrdinalIgnoreCase));

        bool createdNew;
        appMutex = new System.Threading.Mutex(true, AppMutexName, out createdNew);

        if (!createdNew)
        {
            // Եթե ծրագիրն արդեն աշխատում է, արթնացնում ենք առաջին նմուշը
            try
            {
                if (createNewNoteArg)
                {
                    using var existingNewNoteEvent = System.Threading.EventWaitHandle.OpenExisting(AppNewNoteEventName);
                    existingNewNoteEvent.Set();
                }
                else
                {
                    using var existingEvent = System.Threading.EventWaitHandle.OpenExisting(AppWakeEventName);
                    existingEvent.Set();
                }
            }
            catch
            {
                try
                {
                    using var existingEvent = System.Threading.EventWaitHandle.OpenExisting(AppWakeEventName);
                    existingEvent.Set();
                }
                catch { }
            }

            Shutdown();
            return;
        }

        try
        {
            wakeEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, AppWakeEventName);
            registeredWakeWait = System.Threading.ThreadPool.RegisterWaitForSingleObject(
                wakeEvent,
                (state, timedOut) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ShowAllNotes();
                    }));
                },
                null,
                -1,
                false);

            newNoteEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, AppNewNoteEventName);
            registeredNewNoteWait = System.Threading.ThreadPool.RegisterWaitForSingleObject(
                newNoteEvent,
                (state, timedOut) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        CreateAndShowNewNote();
                    }));
                },
                null,
                -1,
                false);
        }
        catch
        {
        }

        base.OnStartup(e);

        // -------------------------------------------------
        // SYSTEM TRAY
        // -------------------------------------------------
        trayIcon = new TaskbarIcon
        {
            ToolTipText = LocalizationManager.Get("AppName"),
            Visibility = Visibility.Visible
        };

        trayIcon.TrayLeftMouseDown += (s, args) =>
        {
            ShowAllNotes();
        };

        try
        {
            var streamInfo = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/Hushak.ico", UriKind.Absolute));
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                trayIcon.Icon = new System.Drawing.Icon(stream);
            }
        }
        catch
        {
            trayIcon.IconSource = new BitmapImage(
                new Uri("pack://application:,,,/Assets/Hushak.ico", UriKind.Absolute));
        }

        BuildTrayMenu();

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
        int openedNotesCount = 0;
        foreach (NoteData data in Store.Notes.Values)
        {
            data.IsClosed = false;

            MainWindow note = new MainWindow(data);
            note.ShowInTaskbar = false;
            note.Show();
            openedNotesCount++;
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

        // Եթե ոչ մի թերթիկ դեռ չկա (օրինակ՝ առաջին տեղադրումից հետո),
        // լռելյայն բացում ենք առաջին թերթիկը։
        if (openedNotesCount == 0)
        {
            CreateAndShowNewNote();
        }
        else if (createNewNoteArg)
        {
            CreateAndShowNewNote();
        }
    }

    public void CreateAndShowNewNote()
    {
        // Առաջին հերթին ապահովում ենք, որ բոլոր առկա թերթիկները տեսանելի են
        ShowAllNotes();

        MainWindow newNote = new MainWindow();
        newNote.ShowInTaskbar = false;

        // Տեղադրում ենք վերջին ակտիվ թերթիկից փոքր շեղումով (եթե կա)
        MainWindow? lastWin = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(w => w != newNote && w.Visibility == Visibility.Visible);
        if (lastWin != null)
        {
            newNote.Left = lastWin.Left + 30;
            newNote.Top = lastWin.Top + 30;
            newNote.Data.Left = newNote.Left;
            newNote.Data.Top = newNote.Top;
        }

        newNote.BringToFrontAndActivate();
        newNote.SaveCurrentState();
        newNote.NoteText.Focus();
    }

    private static void DismissShowDesktopMode()
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType != null)
            {
                object? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    try
                    {
                        shellType.InvokeMember(
                            "UndoMinimizeALL",
                            System.Reflection.BindingFlags.InvokeMethod,
                            null,
                            shell,
                            null);
                    }
                    finally
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void ShowAllNotes()
    {
        // Դուրս ենք բերում Windows-ը «Show Desktop» ռեժիմից, որպեսզի սեղանին կտտացնելիս թերթիկները չանհետանան
        DismissShowDesktopMode();

        MainWindow[] openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();

        foreach (NoteData data in Store.Notes.Values)
        {
            data.IsClosed = false;
            MainWindow? openNote = openNotes.FirstOrDefault(note => note.NoteId == data.Id);

            if (openNote != null)
            {
                openNote.BringToFrontAndActivate();
                continue;
            }

            MainWindow note = new MainWindow(data);
            note.ShowInTaskbar = false;
            note.BringToFrontAndActivate();
        }

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
    }

    // =====================================================
    // BUILD SYSTEM TRAY MENU
    // =====================================================
    private void BuildTrayMenu()
    {
        if (trayIcon == null) return;

        ContextMenu menu = new ContextMenu();

        // Նոր թերթիկ
        MenuItem newNoteItem = new MenuItem
        {
            Header = LocalizationManager.Get("NewNote")
        };
        newNoteItem.Click += (sender, args) =>
        {
            CreateAndShowNewNote();
        };

        // Որոնել թերթիկներ
        MenuItem searchItem = new MenuItem
        {
            Header = LocalizationManager.Get("SearchNotes")
        };
        searchItem.Click += (sender, args) =>
        {
            SearchWindow[] searchWindows = Application.Current.Windows.OfType<SearchWindow>().ToArray();
            if (searchWindows.Length > 0)
            {
                searchWindows[0].Show();
                searchWindows[0].Activate();
                return;
            }
            SearchWindow searchWindow = new SearchWindow();
            searchWindow.Show();
            searchWindow.Activate();
        };

        // Նախընտրություններ
        MenuItem preferencesItem = new MenuItem
        {
            Header = LocalizationManager.Get("Preferences")
        };
        preferencesItem.Click += (sender, args) =>
        {
            PreferencesWindow[] prefWindows = Application.Current.Windows.OfType<PreferencesWindow>().ToArray();
            if (prefWindows.Length > 0)
            {
                prefWindows[0].Show();
                prefWindows[0].Activate();
                return;
            }
            PreferencesWindow prefWindow = new PreferencesWindow();
            prefWindow.Show();
            prefWindow.Activate();
        };

        // Ստուգել արդիացումները
        MenuItem updateItem = new MenuItem
        {
            Header = LocalizationManager.Get("CheckForUpdates")
        };
        updateItem.Click += (sender, args) =>
        {
            UpdateWindow[] updateWindows = Application.Current.Windows.OfType<UpdateWindow>().ToArray();
            if (updateWindows.Length > 0)
            {
                updateWindows[0].Show();
                updateWindows[0].Activate();
                return;
            }
            UpdateWindow updateWindow = new UpdateWindow();
            updateWindow.Show();
            updateWindow.Activate();
        };

        // Ցույց տալ բոլոր թերթիկները
        MenuItem showAllItem = new MenuItem
        {
            Header = LocalizationManager.Get("TrayShowAll")
        };
        showAllItem.Click += (sender, args) =>
        {
            ShowAllNotes();
        };

        // Փակել բոլոր թերթիկները
        MenuItem closeAllItem = new MenuItem
        {
            Header = LocalizationManager.Get("TrayCloseAll")
        };
        closeAllItem.Click += (sender, args) =>
        {
            CloseAllNotes();
        };

        // Գործարկել Windows-ի հետ
        MenuItem startupItem = new MenuItem
        {
            Header = LocalizationManager.Get("TrayStartup"),
            IsCheckable = true,
            IsChecked = StartupManager.IsEnabled()
        };
        startupItem.Click += (sender, args) =>
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

        // Ծրագրի մասին
        MenuItem aboutItem = new MenuItem
        {
            Header = LocalizationManager.Get("About")
        };
        aboutItem.Click += (sender, args) =>
        {
            AboutWindow[] aboutWindows = Application.Current.Windows.OfType<AboutWindow>().ToArray();
            if (aboutWindows.Length > 0)
            {
                aboutWindows[0].Show();
                aboutWindows[0].Activate();
                return;
            }
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
            aboutWindow.Activate();
        };

        // Ելք
        MenuItem exitItem = new MenuItem
        {
            Header = LocalizationManager.Get("TrayExit")
        };
        exitItem.Click += (sender, args) =>
        {
            CloseAllNotes();
            Shutdown();
        };

        // ՑԱՆԿԻ ԿԱՌՈՒՑՈՒՄ
        menu.Items.Add(newNoteItem);
        menu.Items.Add(searchItem);
        menu.Items.Add(preferencesItem);
        menu.Items.Add(updateItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(showAllItem);
        menu.Items.Add(closeAllItem);
        menu.Items.Add(startupItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(aboutItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        trayIcon.ContextMenu = menu;
    }

    // =====================================================
    // ՓԱԿԵԼ ԲՈԼՈՐ ԹԵՐԹԻԿՆԵՐԸ
    // =====================================================
    private void CloseAllNotes()
    {
        MainWindow[] notes = Application.Current.Windows.OfType<MainWindow>().ToArray();
        foreach (MainWindow note in notes)
        {
            note.Close();
        }
    }

    // =====================================================
    // ԵԼՔ
    // =====================================================
    protected override void OnExit(ExitEventArgs e)
    {
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
        trayIcon?.Dispose();

        try
        {
            registeredWakeWait?.Unregister(null);
            registeredNewNoteWait?.Unregister(null);
            wakeEvent?.Dispose();
            newNoteEvent?.Dispose();
            appMutex?.ReleaseMutex();
            appMutex?.Dispose();
        }
        catch
        {
        }

        base.OnExit(e);
    }
}
