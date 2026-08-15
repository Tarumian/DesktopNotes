using System;

namespace DesktopNotes
{
    public class NoteData
    {
        // Կայուն, եզակի նույնացուցիչ
        public Guid Id { get; set; } = Guid.NewGuid();

        // Թերթիկի ստեղծման պահը
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Թերթիկի պարունակությունը
        public string Text { get; set; } = string.Empty;

        // RichTextBox-ի ամբողջական ձեւավորումը
        public string RtfContent { get; set; } = string.Empty;

        // Թերթիկի տեսողական հատկությունները
        public double Width { get; set; }
        public double Height { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }

        public double Rotation { get; set; }

        // Թերթիկի գույնը
        public string NoteColor { get; set; } = "#FFF9A6";

        // Տեքստի ձեւավորման հիմնական տվյալները
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 18;

        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }

        public string TextColor { get; set; } = "#000000";

        // Տրցակի ID-ն։
        // null՝ եթե թերթիկը ոչ մի տրցակի չի պատկանում։
        public Guid? StackId { get; set; }

        // Տրցակում էջի հերթականությունը։
        // Հետագայում սա կօգտագործվի տրցակի կառուցվածքը
        // վերականգնելու համար։
        public int StackOrder { get; set; }
    }
}