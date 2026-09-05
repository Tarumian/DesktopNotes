using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopNotes
{
    public partial class SearchWindow : Window
    {
        public SearchWindow()
        {
            InitializeComponent();
            LoadResults();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void OpenUncompletedButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteStore store =
                ((App)Application.Current).Store;

            foreach (NoteData noteData in
                store.Notes.Values
                    .Where(note => !note.IsCompleted))
            {
                MainWindow? existingWindow =
                    Application.Current.Windows
                        .OfType<MainWindow>()
                        .FirstOrDefault(
                            window =>
                                window.NoteId == noteData.Id);

                if (existingWindow != null)
                {
                    existingWindow.Show();
                    existingWindow.Activate();
                    continue;
                }

                noteData.IsClosed = false;

                MainWindow note =
                    new MainWindow(noteData);

                note.ShowInTaskbar = false;
                note.Show();
                note.Activate();
            }

            DnoteStorage.Save(store);
        }

        private void ResultsListBox_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (ResultsListBox.SelectedItem
                is not ListBoxItem item)
            {
                return;
            }

            if (item.Tag is not Guid noteId)
            {
                return;
            }

            NoteStore store =
                ((App)Application.Current).Store;

            if (!store.Notes.TryGetValue(
                    noteId,
                    out NoteData? noteData))
            {
                return;
            }

            MainWindow? existingWindow =
                Application.Current.Windows
                    .OfType<MainWindow>()
                    .FirstOrDefault(
                        window =>
                            window.NoteId == noteData.Id);

            if (existingWindow != null)
            {
                noteData.IsClosed = false;
                existingWindow.Visibility = Visibility.Visible;
                existingWindow.WindowState = WindowState.Normal;
                existingWindow.EnsureOnScreen();

                if (noteData.StackId != null && store.Stacks.TryGetValue(noteData.StackId.Value, out NoteStack? stack))
                {
                    existingWindow.SetActiveStackNote(stack, existingWindow.NoteId);
                }

                existingWindow.Show();
                existingWindow.Activate();
                existingWindow.Focus();
                DnoteStorage.Save(store);
                return;
            }

            noteData.IsClosed = false;
            DnoteStorage.Save(store);

            MainWindow note =
                new MainWindow(noteData);

            note.ShowInTaskbar = false;
            note.EnsureOnScreen();
            note.Show();
            note.Activate();
            note.Focus();

            if (noteData.StackId != null && store.Stacks.TryGetValue(noteData.StackId.Value, out NoteStack? st))
            {
                note.SetActiveStackNote(st, note.NoteId);
            }
        }

        private void KeywordTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            LoadResults();
        }

        private void CreatedDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            LoadResults();
        }

        private void CompletedDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            LoadResults();
        }

        private void ClearButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            KeywordTextBox.Clear();

            CreatedFromPicker.SelectedDate = null;
            CreatedToPicker.SelectedDate = null;

            CompletedFromPicker.SelectedDate = null;
            CompletedToPicker.SelectedDate = null;

            LoadResults();
        }

        private void LoadResults()
        {
            NoteStore store =
                ((App)Application.Current).Store;

            string keyword =
                KeywordTextBox.Text.Trim();

            DateTime? createdFrom =
                CreatedFromPicker.SelectedDate;

            DateTime? createdTo =
                CreatedToPicker.SelectedDate;

            DateTime? completedFrom =
                CompletedFromPicker.SelectedDate;

            DateTime? completedTo =
                CompletedToPicker.SelectedDate;

            var notes =
                store.Notes.Values
                    .Where(note =>
                        string.IsNullOrWhiteSpace(keyword) ||
                        (!string.IsNullOrEmpty(note.Text) &&
                         note.Text.Contains(
                             keyword,
                             StringComparison.CurrentCultureIgnoreCase)))
                    .Where(note =>
                        !createdFrom.HasValue ||
                        note.CreatedAt.Date >=
                        createdFrom.Value.Date)
                    .Where(note =>
                        !createdTo.HasValue ||
                        note.CreatedAt.Date <=
                        createdTo.Value.Date)
                    .Where(note =>
                        !completedFrom.HasValue ||
                        (note.CompletedDate.HasValue &&
                         note.CompletedDate.Value.Date >=
                         completedFrom.Value.Date))
                    .Where(note =>
                        !completedTo.HasValue ||
                        (note.CompletedDate.HasValue &&
                         note.CompletedDate.Value.Date <=
                         completedTo.Value.Date))
                    .OrderByDescending(note => note.CreatedAt)
                    .ToList();

            ResultsListBox.Items.Clear();

            foreach (NoteData note in notes)
            {
                ListBoxItem item =
                    new ListBoxItem();

                StackPanel panel =
                    new StackPanel
                    {
                        Orientation =
                            Orientation.Horizontal
                    };

                TextBlock statusBlock =
                    new TextBlock
                    {
                        Text = note.IsCompleted
                            ? "✓ "
                            : ""
                    };

                TextBlock datesBlock =
                    new TextBlock
                    {
                        Text =
                            note.CreatedAt.ToString(
                                "dd.MM.yyyy HH:mm")
                    };

                if (note.CompletedDate.HasValue)
                {
                    datesBlock.Text +=
                        " → " +
                        note.CompletedDate.Value.ToString(
                            "dd.MM.yyyy HH:mm");
                }

                TextBlock separatorBlock =
                    new TextBlock
                    {
                        Text = "    "
                    };

                panel.Children.Add(statusBlock);
                panel.Children.Add(datesBlock);
                panel.Children.Add(separatorBlock);

                string text =
                    note.Text
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    if (text.Length > 90)
                        text =
                            text.Substring(0, 90) + "...";

                    panel.Children.Add(
                        new TextBlock
                        {
                            Text = text
                        });
                }
                else
                {
                    int matchIndex =
                        text.IndexOf(
                            keyword,
                            StringComparison.CurrentCultureIgnoreCase);

                    if (matchIndex < 0)
                    {
                        if (text.Length > 90)
                            text =
                                text.Substring(0, 90) + "...";

                        panel.Children.Add(
                            new TextBlock
                            {
                                Text = text
                            });
                    }
                    else
                    {
                        const int contextBefore = 35;
                        const int contextAfter = 45;

                        int start =
                            Math.Max(
                                0,
                                matchIndex -
                                contextBefore);

                        int end =
                            Math.Min(
                                text.Length,
                                matchIndex +
                                keyword.Length +
                                contextAfter);

                        string before =
                            text.Substring(
                                start,
                                matchIndex - start);

                        string match =
                            text.Substring(
                                matchIndex,
                                keyword.Length);

                        string after =
                            text.Substring(
                                matchIndex +
                                keyword.Length,
                                end -
                                (matchIndex +
                                 keyword.Length));

                        if (start > 0)
                            before = "... " + before;

                        if (end < text.Length)
                            after += " ...";

                        panel.Children.Add(
                            new TextBlock
                            {
                                Text = before
                            });

                        panel.Children.Add(
                            new TextBlock
                            {
                                Text = match,
                                FontWeight =
                                    FontWeights.Bold
                            });

                        panel.Children.Add(
                            new TextBlock
                            {
                                Text = after
                            });
                    }
                }

                item.Content = panel;
                item.Tag = note.Id;

                ResultsListBox.Items.Add(item);
            }

            ResultCountText.Text =
                "Գտնված է՝ " + notes.Count;
        }
    }
}