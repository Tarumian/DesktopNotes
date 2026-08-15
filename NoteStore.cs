using System;
using System.Collections.Generic;

namespace DesktopNotes
{
    // =========================================================
    // DESKTOPNOTES-Ի ՏՎՅԱԼՆԵՐԻ ԿԵՆՏՐՈՆԱԿԱՆ ՊԱՀՈՑ
    // =========================================================

    public class NoteStore
    {
        // -----------------------------------------------------
        // Բոլոր ընթացիկ թերթիկները
        // -----------------------------------------------------

        public Dictionary<Guid, NoteData> Notes { get; } =
            new Dictionary<Guid, NoteData>();


        // -----------------------------------------------------
        // Բոլոր տրցակները
        // -----------------------------------------------------

        public Dictionary<Guid, NoteStack> Stacks { get; } =
            new Dictionary<Guid, NoteStack>();


        // -----------------------------------------------------
        // Ջնջված թերթիկները
        // Undo-ի համար
        // -----------------------------------------------------

        public Dictionary<Guid, NoteData> DeletedNotes { get; } =
            new Dictionary<Guid, NoteData>();


        // -----------------------------------------------------
        // Թերթիկների ընդհանուր չափը
        // -----------------------------------------------------

        public double NoteWidth { get; set; } = 300;

        public double NoteHeight { get; set; } = 220;
    }
}