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


        // =====================================================
        // ՊԱՀՊԱՆՈՒՄ
        // =====================================================

        public static void Save(
            NoteStore store)
        {
            DnoteFile file =
                new DnoteFile
                {
                    Version = 1,

                    NoteWidth =
                        store.NoteWidth,

                    NoteHeight =
                        store.NoteHeight,

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


            File.WriteAllText(
                FilePath,
                json);
        }


        // =====================================================
        // ԲԵՌՆՈՒՄ
        // =====================================================

        public static void Load(
            NoteStore store)
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
            // Ընդհանուր չափերը
            // -------------------------------------------------

            store.NoteWidth =
                file.NoteWidth;

            store.NoteHeight =
                file.NoteHeight;


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
    }
}