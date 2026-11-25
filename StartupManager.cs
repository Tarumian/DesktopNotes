using Microsoft.Win32;
using System;

namespace Hushak;

public static class StartupManager
{
    private const string AppName = "Hushak";
    private const string LegacyAppName = "DesktopNotes";

    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private static string GetStartupShortcutPath()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return System.IO.Path.Combine(startupFolder, "Hushak.lnk");
    }

    private static string GetLegacyStartupShortcutPath()
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return System.IO.Path.Combine(startupFolder, "DesktopNotes.lnk");
    }

    // =====================================================
    // ՄԻԱՑՎԱ՞Ծ Է
    // =====================================================

    public static bool IsEnabled()
    {
        // 1. Ստուգում ենք Registry-ն (Hushak կամ հին DesktopNotes)
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            if (key != null && (key.GetValue(AppName) != null || key.GetValue(LegacyAppName) != null))
            {
                return true;
            }
        }
        catch { }

        // 2. Ստուգում ենք Startup թղթապանակի դյուրանցումը
        try
        {
            if (System.IO.File.Exists(GetStartupShortcutPath()) || System.IO.File.Exists(GetLegacyStartupShortcutPath()))
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
                "Չհաջողվեց որոշել ծրագրի հասցեն։");

        using RegistryKey key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                true)
            ?? throw new InvalidOperationException(
                "Չհաջողվեց բացել Windows Startup-ի կարգավորումը։");

        // Մաքրում ենք հին գրառումը եթե կար
        try { key.DeleteValue(LegacyAppName, false); } catch { }

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
                key.DeleteValue(LegacyAppName, false);
            }
        }
        catch { }

        // 2. Մաքրում ենք Startup թղթապանակի դյուրանցումը
        try
        {
            string shortcutPath = GetStartupShortcutPath();
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
            string legacyPath = GetLegacyStartupShortcutPath();
            if (System.IO.File.Exists(legacyPath))
            {
                System.IO.File.Delete(legacyPath);
            }
        }
        catch { }
    }
}