using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Hushak
{
    // =========================================================
    // .DNOTE ՆԻՇՔԻ ՊԱՀՊԱՆՈՒՄ ԵՒ ԲԵՌՆՈՒՄ
    // =========================================================

    public static class DnoteStorage
    {
        // -----------------------------------------------------
        // Լռելյայն .dnote նիշքի հասցեն
        // -----------------------------------------------------

        public static string FolderPath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Hushak");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        public static string FilePath
        {
            get
            {
                string newPath = Path.Combine(FolderPath, "Hushak.dnote");

                // Եթե նոր նիշքը դեռ չկա, ստուգենք նախորդ տարբերակների պահպանված նիշքերը և պատճենենք
                if (!File.Exists(newPath))
                {
                    try
                    {
                        string sameFolderLegacy = Path.Combine(FolderPath, "DesktopNotes.dnote");
                        string oldFolderLegacy = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "DesktopNotes",
                            "DesktopNotes.dnote");

                        if (File.Exists(sameFolderLegacy))
                        {
                            File.Copy(sameFolderLegacy, newPath, true);
                        }
                        else if (File.Exists(oldFolderLegacy))
                        {
                            File.Copy(oldFolderLegacy, newPath, true);
                        }
                    }
                    catch
                    {
                    }
                }

                return newPath;
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

                            RotationOrigin =
                                store.RotationOrigin,

                            RandomRotationForNewNotes =
                                store.RandomRotationForNewNotes,

                            CustomSizes =
                                store.CustomSizes,

                            CustomNoteColors =
                                store.CustomNoteColors,

                            CustomTextColors =
                                store.CustomTextColors,

                            CustomPencilColors =
                                store.CustomPencilColors,

                            Language =
                                store.Language,

                            StoreAppId =
                                store.StoreAppId,

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

            // Լռելյայն չափը միշտ 150 × 220 է, եթե հատուկ չի ընտրված (կամ եթե 130x300 է / 300x220 է առաջացել)
            if (file.NoteWidth == 150 && file.NoteHeight == 220)
            {
                store.NoteWidth = 150;
                store.NoteHeight = 220;
            }
            else if (file.NoteWidth > 0 && file.NoteHeight > 0 && !(Math.Abs(file.NoteWidth - 130) < 0.1 && Math.Abs(file.NoteHeight - 300) < 0.1) && !(Math.Abs(file.NoteWidth - 300) < 0.1 && Math.Abs(file.NoteHeight - 220) < 0.1))
            {
                store.NoteWidth = file.NoteWidth;
                store.NoteHeight = file.NoteHeight;
            }
            else
            {
                store.NoteWidth = 150;
                store.NoteHeight = 220;
            }

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

            if (!string.IsNullOrEmpty(file.StackAlignment) && file.StackAlignment != "TopLeft")
                store.StackAlignment = file.StackAlignment;
            else
                store.StackAlignment = "TopCenter";

            if (!string.IsNullOrEmpty(file.RotationOrigin))
                store.RotationOrigin = file.RotationOrigin;

            store.RandomRotationForNewNotes = file.RandomRotationForNewNotes;

            if (file.CustomSizes != null)
            {
                store.CustomSizes = file.CustomSizes;
                store.CustomSizes.RemoveAll(s => s.Contains("130") && s.Contains("300"));
            }

            if (file.CustomNoteColors != null)
                store.CustomNoteColors = file.CustomNoteColors;

            if (file.CustomTextColors != null)
                store.CustomTextColors = file.CustomTextColors;

            if (file.CustomPencilColors != null)
                store.CustomPencilColors = file.CustomPencilColors;

            if (!string.IsNullOrEmpty(file.Language))
                store.Language = file.Language;

            if (!string.IsNullOrEmpty(file.StoreAppId))
                store.StoreAppId = file.StoreAppId;


            // -------------------------------------------------
            // Թերթիկները
            // -------------------------------------------------

            store.Notes.Clear();

            double screenLeft = SystemParameters.VirtualScreenLeft;
            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            int recoverIndex = 0;
            foreach (NoteData note in file.Notes)
            {
                double noteVisualLeft = note.Left + 300;
                double noteVisualTop = note.Top + 300;
                double noteW = note.Width > 0 ? note.Width : 150;
                double noteH = note.Height > 0 ? note.Height : 220;

                bool isOffScreen = (noteVisualLeft + noteW < screenLeft + 50) ||
                                   (noteVisualLeft > screenLeft + screenWidth - 50) ||
                                   (noteVisualTop + noteH < screenTop + 50) ||
                                   (noteVisualTop > screenTop + screenHeight - 50);

                if (isOffScreen)
                {
                    note.Left = 100 + (recoverIndex % 8) * 30;
                    note.Top = 100 + (recoverIndex % 8) * 30;
                    recoverIndex++;
                }

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

            // Մաքրում ենք հին/կրկնվող տրցակները
            HashSet<Guid> validStackIds = store.Notes.Values
                .Where(n => n.StackId != null)
                .Select(n => n.StackId!.Value)
                .ToHashSet();

            List<Guid> staleStackIds = store.Stacks.Keys
                .Where(k => !validStackIds.Contains(k))
                .ToList();

            foreach (Guid staleId in staleStackIds)
            {
                store.Stacks.Remove(staleId);
            }

            foreach (NoteStack stack in store.Stacks.Values.ToList())
            {
                stack.Alignment = "TopCenter";

                stack.NoteIds = store.Notes.Values
                    .Where(n => n.StackId == stack.Id)
                    .OrderBy(n => n.CreatedAt)
                    .Select(n => n.Id)
                    .ToList();

                if (stack.NoteIds.Count <= 1)
                {
                    foreach (Guid id in stack.NoteIds)
                    {
                        if (store.Notes.TryGetValue(id, out NoteData? d))
                            d.StackId = null;
                    }
                    store.Stacks.Remove(stack.Id);
                }
                else if (stack.CurrentNoteId == null || !stack.NoteIds.Contains(stack.CurrentNoteId.Value))
                {
                    stack.CurrentNoteId = stack.NoteIds.LastOrDefault();
                }
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