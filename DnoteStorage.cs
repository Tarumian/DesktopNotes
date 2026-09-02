using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DesktopNotes
{
    // =========================================================
    // .DNOTE ՆԻՇՔԻ ՊԱՀՊԱՆՈՒՄ ԵՒ ԲԵՌՆՈՒՄ
    // =========================================================

    public static class DnoteStorage
    {
        // -----------------------------------------------------
        // Լռելյայն .dnote նիշքի հասցեն
        // -----------------------------------------------------

        public static string FilePath
        {
            get
            {
                string folder =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.ApplicationData),
                        "DesktopNotes");

                Directory.CreateDirectory(folder);

                return Path.Combine(
                    folder,
                    "DesktopNotes.dnote");
            }
        }


        private static readonly object saveLock = new object();

        // =====================================================
        // ՊԱՀՊԱՆՈՒՄ
        // =====================================================

        public static void Save(
            NoteStore store)
        {
            lock (saveLock)
            {
                try
                {
                    DnoteFile file =
                        new DnoteFile
                        {
                            Version = 1,

                            NoteWidth =
                                store.NoteWidth,

                            NoteHeight =
                                store.NoteHeight,

                            NoteColor =
                                store.NoteColor,

                            TextColor =
                                store.TextColor,

                            FontFamily =
                                store.FontFamily,

                            FontSize =
                                store.FontSize,

                            IsBold =
                                store.IsBold,

                            IsItalic =
                                store.IsItalic,

                            PencilColor =
                                store.PencilColor,

                            PencilThickness =
                                store.PencilThickness,

                            AllowFreeResize =
                                store.AllowFreeResize,

                            StackAlignment =
                                store.StackAlignment,

                            CustomSizes =
                                store.CustomSizes,

                            CustomNoteColors =
                                store.CustomNoteColors,

                            CustomTextColors =
                                store.CustomTextColors,

                            CustomPencilColors =
                                store.CustomPencilColors,

                            Notes =
                                store.Notes.Values.ToList(),

                            Stacks =
                                store.Stacks.Values.ToList(),

                            DeletedNotes =
                                store.DeletedNotes.Values.ToList()
                        };

                    JsonSerializerOptions options =
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };

                    string json =
                        JsonSerializer.Serialize(
                            file,
                            options);

                    for (int retry = 0; retry < 5; retry++)
                    {
                        try
                        {
                            File.WriteAllText(
                                FilePath,
                                json);
                            break;
                        }
                        catch (IOException)
                        {
                            if (retry == 4) break;
                            System.Threading.Thread.Sleep(30);
                        }
                    }
                }
                catch
                {
                }
            }
        }


        // =====================================================
        // ԲԵՌՆՈՒՄ
        // =====================================================

        public static void Load(
            NoteStore store)
        {
            lock (saveLock)
            {
                try
                {
                    if (!File.Exists(FilePath))
                        return;

                    string json =
                        File.ReadAllText(
                            FilePath);

                    DnoteFile? file =
                        JsonSerializer.Deserialize<DnoteFile>(
                            json);

                    if (file == null)
                        return;


            // -------------------------------------------------
            // Ընդհանուր չափերը եւ նախընտրանքները
            // -------------------------------------------------

            store.NoteWidth =
                file.NoteWidth > 0 ? file.NoteWidth : 150;

            store.NoteHeight =
                file.NoteHeight > 0 ? file.NoteHeight : 220;

            if (!string.IsNullOrEmpty(file.NoteColor))
                store.NoteColor = file.NoteColor;

            if (!string.IsNullOrEmpty(file.TextColor))
                store.TextColor = file.TextColor;

            if (!string.IsNullOrEmpty(file.FontFamily))
                store.FontFamily = file.FontFamily;

            if (file.FontSize > 0)
                store.FontSize = file.FontSize;

            store.IsBold = file.IsBold;
            store.IsItalic = file.IsItalic;

            if (!string.IsNullOrEmpty(file.PencilColor))
                store.PencilColor = file.PencilColor;

            if (file.PencilThickness > 0)
                store.PencilThickness = file.PencilThickness;

            store.AllowFreeResize = file.AllowFreeResize;

            if (!string.IsNullOrEmpty(file.StackAlignment))
                store.StackAlignment = file.StackAlignment;

            if (file.CustomSizes != null)
                store.CustomSizes = file.CustomSizes;

            if (file.CustomNoteColors != null)
                store.CustomNoteColors = file.CustomNoteColors;

            if (file.CustomTextColors != null)
                store.CustomTextColors = file.CustomTextColors;

            if (file.CustomPencilColors != null)
                store.CustomPencilColors = file.CustomPencilColors;


            // -------------------------------------------------
            // Թերթիկները
            // -------------------------------------------------

            store.Notes.Clear();

            foreach (NoteData note in file.Notes)
            {
                store.Notes[note.Id] =
                    note;
            }


            // -------------------------------------------------
            // Տրցակները
            // -------------------------------------------------

            store.Stacks.Clear();

            foreach (NoteStack stack in file.Stacks)
            {
                store.Stacks[stack.Id] =
                    stack;
            }


            // -------------------------------------------------
            // Ջնջված թերթիկները
            // -------------------------------------------------

            store.DeletedNotes.Clear();

            foreach (NoteData note in file.DeletedNotes)
            {
                store.DeletedNotes[note.Id] =
                    note;
            }
                }
                catch
                {
                }
            }
        }
    }
}