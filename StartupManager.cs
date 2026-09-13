using Microsoft.Win32;
using System;

namespace DesktopNotes;

public static class StartupManager
{
    private const string AppName =
        "DesktopNotes";


    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";


    private static string GetStartupShortcutPath()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return System.IO.Path.Combine(startupFolder, "DesktopNotes.lnk");
    }

    // =====================================================
    // ՄԻԱՑՎԱ՞Ծ Է
    // =====================================================

    public static bool IsEnabled()
    {
        // 1. Ստուգում ենք Registry-ն
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            if (key != null && key.GetValue(AppName) != null)
            {
                return true;
            }
        }
        catch { }

        // 2. Ստուգում ենք Startup թղթապանակի դյուրանցումը (Inno Setup-ի ստեղծած)
        try
        {
            string shortcutPath = GetStartupShortcutPath();
            if (System.IO.File.Exists(shortcutPath))
            {
                return true;
            }
        }
        catch { }

        return false;
    }


    // =====================================================
    // ՄԻԱՑՆԵԼ
    // =====================================================

    public static void Enable()
    {
        string exePath =
            Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "Չհաջողվեց որոշել DesktopNotes.exe-ի հասցեն։");

        using RegistryKey key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                true)
            ?? throw new InvalidOperationException(
                "Չհաջողվեց բացել Windows Startup-ի կարգավորումը։");

        key.SetValue(
            AppName,
            $"\"{exePath}\"");
    }


    // =====================================================
    // ԱՆՋԱՏԵԼ
    // =====================================================

    public static void Disable()
    {
        // 1. Մաքրում ենք Registry-ից
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key != null)
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { }

        // 2. Մաքրում ենք Startup թղթապանակի դյուրանցումը (եթե կա)
        try
        {
            string shortcutPath = GetStartupShortcutPath();
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
        }
        catch { }
    }
}