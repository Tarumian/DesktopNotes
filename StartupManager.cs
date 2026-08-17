using Microsoft.Win32;
using System;

namespace DesktopNotes;

public static class StartupManager
{
    private const string AppName =
        "DesktopNotes";


    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";


    // =====================================================
    // ՄԻԱՑՎԱ՞Ծ Է
    // =====================================================

    public static bool IsEnabled()
    {
        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                false);

        if (key == null)
            return false;

        return key.GetValue(AppName) != null;
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
        using RegistryKey? key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                true);

        if (key == null)
            return;

        key.DeleteValue(
            AppName,
            false);
    }
}