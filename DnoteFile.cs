using System;
using System.Collections.Generic;

namespace DesktopNotes
{
    // =========================================================
    // .DNOTE ՆԻՇՔԻ ՊԱՀՎՈՂ ՏՎՅԱԼՆԵՐԸ
    // =========================================================

    public class DnoteFile
    {
        // -----------------------------------------------------
        // Նիշքի տարբերակը
        // -----------------------------------------------------

        public int Version { get; set; } = 1;


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

        public string StackAlignment { get; set; } = "TopLeft";

        public List<string> CustomSizes { get; set; } = new List<string>();

        public List<string> CustomNoteColors { get; set; } = new List<string>();

        public List<string> CustomTextColors { get; set; } = new List<string>();

        public List<string> CustomPencilColors { get; set; } = new List<string>();


        // -----------------------------------------------------
        // Բոլոր ընթացիկ թերթիկները
        // -----------------------------------------------------

        public List<NoteData> Notes { get; set; } =
            new List<NoteData>();


        // -----------------------------------------------------
        // Բոլոր տրցակները
        // -----------------------------------------------------

        public List<NoteStack> Stacks { get; set; } =
            new List<NoteStack>();


        // -----------------------------------------------------
        // Ջնջված թերթիկները՝ Undo-ի համար
        // -----------------------------------------------------

        public List<NoteData> DeletedNotes { get; set; } =
            new List<NoteData>();
    }
}