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

        public double NoteWidth { get; set; } = 150;

        public double NoteHeight { get; set; } = 220;

        // -----------------------------------------------------
        // ՆԱԽԸՆՏՐՈՒԹՅՈՒՆՆԵՐ
        // -----------------------------------------------------

        public string NoteColor { get; set; } = "#FFF59D";

        public string TextColor { get; set; } = "#000000";

        public string FontFamily { get; set; } = "Comic Sans MS";

        public double FontSize { get; set; } = 14;

        public bool IsBold { get; set; } = false;

        public bool IsItalic { get; set; } = false;

        public string PencilColor { get; set; } = "#000000";

        public double PencilThickness { get; set; } = 2;

        public bool AllowFreeResize { get; set; } = false;

        // Օգտվողի կողմից ավելացված չափեր (օրինակ՝ "200 × 300")
        public List<string> CustomSizes { get; set; } = new List<string>();

        // Օգտվողի կողմից ավելացված գույներ (օրինակ՝ "#AABBCC")
        public List<string> CustomNoteColors { get; set; } = new List<string>();

        public List<string> CustomTextColors { get; set; } = new List<string>();

        public List<string> CustomPencilColors { get; set; } = new List<string>();
    }
}