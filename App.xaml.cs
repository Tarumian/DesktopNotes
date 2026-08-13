using System.Windows;

namespace DesktopNotes;

public partial class App : Application
{
    // =========================================================
    // DESKTOPNOTES-Ի ԿԵՆՏՐՈՆԱԿԱՆ ՏՎՅԱԼՆԵՐԻ ՊԱՀՈՑ
    // =========================================================

    public NoteStore Store { get; } =
        new NoteStore();
}