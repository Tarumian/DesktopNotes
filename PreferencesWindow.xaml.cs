using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DesktopNotes
{
    public partial class PreferencesWindow : Window
    {
        private readonly NoteStore store;

        // Ամրագրված չափեր
        private readonly List<string> defaultSizes = new()
        {
            "150 × 220",
            "300 × 220",
            "150 × 450"
        };

        // Ամրագրված գույներ (Հիմնական գույներ թերթիկի, տեքստի եւ մատիտի համար)
        private readonly List<string> defaultNoteColors = new()
        {
            "#FFF59D", // Դեղին
            "#C8E6C9", // Կանաչ
            "#BBDEFB", // Կապույտ
            "#F8BBD0", // Վարդագույն
            "#E1BEE7", // Մանուշակագույն
            "#FFFFFF"  // Սպիտակ
        };

        private readonly List<string> defaultTextColors = new()
        {
            "#000000", // Սեւ
            "#868686", // Գորշ
            "#FF0000", // Կարմիր
            "#00B050", // Կանաչ
            "#0000FF", // Կապույտ
            "#8A00FF", // Մանուշակագույն
            "#FFFFFF"  // Սպիտակ
        };

        private readonly List<string> defaultPencilColors = new()
        {
            "#000000", // Սեւ
            "#FF0000", // Կարմիր
            "#00B050", // Կանաչ
            "#0000FF", // Կապույտ
            "#FFC000"  // Նարնջագույն
        };

        private readonly List<string> defaultFonts = new()
        {
            "Comic Sans MS",
            "Calibri",
            "Courier New",
            "Arial",
            "Segoe UI",
            "Times New Roman"
        };

        public PreferencesWindow()
        {
            InitializeComponent();

            store = ((App)Application.Current).Store;

            LoadAllPreferences();
        }

        // =========================================================
        // ԲԵՌՆԵԼ ՏՎՅԱԼՆԵՐԸ
        // =========================================================

        private void LoadAllPreferences()
        {
            // 1. Չափեր
            PopulateSizeComboBox(string.Format("{0} × {1}", (int)store.NoteWidth, (int)store.NoteHeight));

            // 2. Թերթիկի գույն
            PopulateColorComboBox(NoteColorComboBox, defaultNoteColors, store.CustomNoteColors, store.NoteColor);

            // 3. Տեքստի գույն
            PopulateColorComboBox(TextColorComboBox, defaultTextColors, store.CustomTextColors, store.TextColor);

            // 4. Տառատեսակ
            PopulateFontComboBox(store.FontFamily);

            // 5. Կետաչափ
            FontSizeTextBox.Text = store.FontSize.ToString("0");

            // 6. Սկզբնական ոճ
            if (store.IsBold && store.IsItalic)
                FontStyleComboBox.SelectedIndex = 3;
            else if (store.IsItalic)
                FontStyleComboBox.SelectedIndex = 2;
            else if (store.IsBold)
                FontStyleComboBox.SelectedIndex = 1;
            else
                FontStyleComboBox.SelectedIndex = 0;

            // 7. Մատիտի գույն
            PopulateColorComboBox(PencilColorComboBox, defaultPencilColors, store.CustomPencilColors, store.PencilColor);

            // 8. Մատիտի հաստություն
            PencilThicknessTextBox.Text = store.PencilThickness.ToString("0");

            // 9. Ազատ մեծացում
            AllowFreeResizeCheckBox.IsChecked = store.AllowFreeResize;

            // 10. Windows Startup
            AutostartCheckBox.IsChecked = StartupManager.IsEnabled();
        }

        // =========================================================
        // ՉԱՓԵՐԻ ԿԱՌԱՎԱՐՈՒՄ
        // =========================================================

        private void PopulateSizeComboBox(string selectedSize)
        {
            NoteSizeComboBox.Items.Clear();

            foreach (string s in defaultSizes)
            {
                NoteSizeComboBox.Items.Add(new ComboBoxItem { Content = s, Tag = s });
            }

            foreach (string s in store.CustomSizes)
            {
                if (!defaultSizes.Contains(s))
                {
                    NoteSizeComboBox.Items.Add(new ComboBoxItem { Content = s, Tag = s });
                }
            }

            NoteSizeComboBox.Items.Add(new ComboBoxItem { Content = "Այլ...", Tag = "__OTHER__" });

            SelectSizeItem(selectedSize);
        }

        private void SelectSizeItem(string sizeText)
        {
            for (int i = 0; i < NoteSizeComboBox.Items.Count - 1; i++)
            {
                if (NoteSizeComboBox.Items[i] is ComboBoxItem item && (string)item.Content == sizeText)
                {
                    NoteSizeComboBox.SelectedIndex = i;
                    UpdateRemoveSizeButtonState();
                    return;
                }
            }

            // Եթե ցանկում չկա, ավելացնենք որպես custom
            if (!string.IsNullOrWhiteSpace(sizeText) && sizeText.Contains("×"))
            {
                store.CustomSizes.Add(sizeText);
                PopulateSizeComboBox(sizeText);
                return;
            }

            if (NoteSizeComboBox.Items.Count > 0)
                NoteSizeComboBox.SelectedIndex = 0;

            UpdateRemoveSizeButtonState();
        }

        private void NoteSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NoteSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                if ((string)item.Tag == "__OTHER__")
                {
                    PromptCustomSize();
                }
            }

            UpdateRemoveSizeButtonState();
        }

        private void PromptCustomSize()
        {
            // Պարզ մուտքագրման պատուհան
            Window prompt = new Window
            {
                Title = "Նոր չափ",
                Width = 320,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"))
            };

            Grid grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock header = new TextBlock
            {
                Text = "Մուտքագրեք թերթիկի չափսերը (օր․՝ 200 × 300)․",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(header, 0);

            StackPanel inputs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            TextBox wBox = new TextBox { Width = 60, Height = 26, Text = "200", VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(4, 0, 4, 0) };
            TextBlock cross = new TextBlock { Text = "×", Margin = new Thickness(8, 4, 8, 0), FontSize = 14 };
            TextBox hBox = new TextBox { Width = 60, Height = 26, Text = "300", VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(4, 0, 4, 0) };
            inputs.Children.Add(wBox);
            inputs.Children.Add(cross);
            inputs.Children.Add(hBox);
            Grid.SetRow(inputs, 1);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button okBtn = new Button { Content = "Ավելացնել", Width = 85, Height = 26, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            Button cancelBtn = new Button { Content = "Չեղարկել", Width = 75, Height = 26, IsCancel = true };
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            Grid.SetRow(buttons, 2);

            grid.Children.Add(header);
            grid.Children.Add(inputs);
            grid.Children.Add(buttons);
            prompt.Content = grid;

            okBtn.Click += (s, args) =>
            {
                if (double.TryParse(wBox.Text.Trim(), out double w) && double.TryParse(hBox.Text.Trim(), out double h) && w >= 80 && h >= 80)
                {
                    string newSize = string.Format("{0} × {1}", (int)w, (int)h);
                    if (!store.CustomSizes.Contains(newSize) && !defaultSizes.Contains(newSize))
                    {
                        store.CustomSizes.Add(newSize);
                    }
                    prompt.DialogResult = true;
                    prompt.Close();
                    PopulateSizeComboBox(newSize);
                }
                else
                {
                    MessageBox.Show(prompt, "Խնդրում ենք մուտքագրել վավեր թվային չափսեր (նվազագույնը 80)։", "Սխալ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            if (prompt.ShowDialog() != true)
            {
                // Վերականգնում ենք նախկին ընտրությունը
                SelectSizeItem(string.Format("{0} × {1}", (int)store.NoteWidth, (int)store.NoteHeight));
            }
        }

        private void RemoveSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (NoteSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string sizeText)
            {
                if (store.CustomSizes.Contains(sizeText))
                {
                    store.CustomSizes.Remove(sizeText);
                    PopulateSizeComboBox(defaultSizes[0]);
                }
            }
        }

        private void UpdateRemoveSizeButtonState()
        {
            if (NoteSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string sizeText)
            {
                RemoveSizeButton.IsEnabled = store.CustomSizes.Contains(sizeText) && !defaultSizes.Contains(sizeText);
            }
            else
            {
                RemoveSizeButton.IsEnabled = false;
            }
        }

        // =========================================================
        // ԳՈՒՅՆԵՐԻ ԿԱՌԱՎԱՐՈՒՄ
        // =========================================================

        private void PopulateColorComboBox(ComboBox combo, List<string> defaults, List<string> customs, string selectedHex)
        {
            combo.Items.Clear();

            foreach (string hex in defaults)
            {
                combo.Items.Add(CreateColorComboBoxItem(hex, false));
            }

            foreach (string hex in customs)
            {
                if (!defaults.Contains(hex, StringComparer.OrdinalIgnoreCase))
                {
                    combo.Items.Add(CreateColorComboBoxItem(hex, true));
                }
            }

            combo.Items.Add(new ComboBoxItem { Content = "Այլ...", Tag = "__OTHER__" });

            SelectColorInComboBox(combo, selectedHex);
        }

        private ComboBoxItem CreateColorComboBoxItem(string hex, bool isCustom)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Border swatch = new Border
            {
                Width = 14,
                Height = 14,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 8, 0)
            };

            TextBlock label = new TextBlock
            {
                Text = hex.ToUpperInvariant(),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            };

            sp.Children.Add(swatch);
            sp.Children.Add(label);

            return new ComboBoxItem
            {
                Content = sp,
                Tag = hex
            };
        }

        private void SelectColorInComboBox(ComboBox combo, string hex)
        {
            for (int i = 0; i < combo.Items.Count - 1; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && item.Tag is string itemHex &&
                    string.Equals(itemHex, hex, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    UpdateRemoveColorButtonState(combo);
                    return;
                }
            }

            // Եթե ցանկում չկա, ավելացնենք որպես custom
            List<string> customs = GetCustomListForCombo(combo);
            if (!customs.Contains(hex, StringComparer.OrdinalIgnoreCase))
            {
                customs.Add(hex);
                List<string> defs = GetDefaultListForCombo(combo);
                PopulateColorComboBox(combo, defs, customs, hex);
                return;
            }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;

            UpdateRemoveColorButtonState(combo);
        }

        private List<string> GetCustomListForCombo(ComboBox combo)
        {
            if (combo == NoteColorComboBox) return store.CustomNoteColors;
            if (combo == TextColorComboBox) return store.CustomTextColors;
            return store.CustomPencilColors;
        }

        private List<string> GetDefaultListForCombo(ComboBox combo)
        {
            if (combo == NoteColorComboBox) return defaultNoteColors;
            if (combo == TextColorComboBox) return defaultTextColors;
            return defaultPencilColors;
        }

        private void HandleColorSelection(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && (string)item.Tag == "__OTHER__")
            {
                PromptCustomColor(combo);
            }
            UpdateRemoveColorButtonState(combo);
        }

        private void PromptCustomColor(ComboBox combo)
        {
            Forms.ColorDialog dialog = new Forms.ColorDialog
            {
                FullOpen = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string newHex = string.Format("#{0:X2}{1:X2}{2:X2}", dialog.Color.R, dialog.Color.G, dialog.Color.B);
                List<string> customs = GetCustomListForCombo(combo);
                List<string> defs = GetDefaultListForCombo(combo);

                if (!customs.Contains(newHex, StringComparer.OrdinalIgnoreCase) &&
                    !defs.Contains(newHex, StringComparer.OrdinalIgnoreCase))
                {
                    customs.Add(newHex);
                }

                PopulateColorComboBox(combo, defs, customs, newHex);
            }
            else
            {
                // Վերականգնել նախորդը
                string prev = combo == NoteColorComboBox ? store.NoteColor :
                              combo == TextColorComboBox ? store.TextColor : store.PencilColor;
                SelectColorInComboBox(combo, prev);
            }
        }

        private void NoteColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleColorSelection(NoteColorComboBox);
        }

        private void TextColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleColorSelection(TextColorComboBox);
        }

        private void PencilColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleColorSelection(PencilColorComboBox);
        }

        private void RemoveNoteColorButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedCustomColor(NoteColorComboBox);
        }

        private void RemoveTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedCustomColor(TextColorComboBox);
        }

        private void RemovePencilColorButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedCustomColor(PencilColorComboBox);
        }

        private void RemoveSelectedCustomColor(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                List<string> customs = GetCustomListForCombo(combo);
                List<string> defs = GetDefaultListForCombo(combo);

                if (customs.RemoveAll(c => string.Equals(c, hex, StringComparison.OrdinalIgnoreCase)) > 0)
                {
                    PopulateColorComboBox(combo, defs, customs, defs[0]);
                }
            }
        }

        private void UpdateRemoveColorButtonState(ComboBox combo)
        {
            Button btn = combo == NoteColorComboBox ? RemoveNoteColorButton :
                         combo == TextColorComboBox ? RemoveTextColorButton : RemovePencilColorButton;

            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                List<string> customs = GetCustomListForCombo(combo);
                List<string> defs = GetDefaultListForCombo(combo);
                btn.IsEnabled = customs.Any(c => string.Equals(c, hex, StringComparison.OrdinalIgnoreCase)) &&
                                !defs.Any(d => string.Equals(d, hex, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                btn.IsEnabled = false;
            }
        }

        // =========================================================
        // ՏԱՌԱՏԵՍԱԿՆԵՐԻ ԿԱՌԱՎԱՐՈՒՄ
        // =========================================================

        private void PopulateFontComboBox(string selectedFont)
        {
            FontFamilyComboBox.Items.Clear();

            var systemFonts = Fonts.SystemFontFamilies
                                   .Select(f => f.Source)
                                   .OrderBy(name => name)
                                   .ToList();

            List<string> topFonts = new() { "Comic Sans MS", "Calibri", "Courier New", "Arial", "Segoe UI", "Times New Roman", "Tahoma" };

            foreach (string font in topFonts)
            {
                if (systemFonts.Contains(font))
                {
                    FontFamilyComboBox.Items.Add(new ComboBoxItem { Content = font, Tag = font });
                }
            }

            foreach (string font in systemFonts)
            {
                if (!topFonts.Contains(font))
                {
                    FontFamilyComboBox.Items.Add(new ComboBoxItem { Content = font, Tag = font });
                }
            }

            SelectFontItem(selectedFont);
        }

        private void SelectFontItem(string fontName)
        {
            for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
            {
                if (FontFamilyComboBox.Items[i] is ComboBoxItem item && (string)item.Tag == fontName)
                {
                    FontFamilyComboBox.SelectedIndex = i;
                    return;
                }
            }

            ComboBoxItem customItem = new ComboBoxItem { Content = fontName, Tag = fontName };
            FontFamilyComboBox.Items.Insert(0, customItem);
            FontFamilyComboBox.SelectedIndex = 0;
        }

        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        // =========================================================
        // ՊԱՀՊԱՆԵԼ ԵՒ ԿԻՐԱՌԵԼ
        // =========================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // 1. Չափ
            if (NoteSizeComboBox.SelectedItem is ComboBoxItem sizeItem && sizeItem.Tag is string sizeStr && sizeStr != "__OTHER__")
            {
                string[] parts = sizeStr.Split('×', 'x', 'X');
                if (parts.Length == 2 && double.TryParse(parts[0].Trim(), out double w) && double.TryParse(parts[1].Trim(), out double h))
                {
                    store.NoteWidth = w;
                    store.NoteHeight = h;
                }
            }

            // 2. Գույներ
            if (NoteColorComboBox.SelectedItem is ComboBoxItem ncItem && ncItem.Tag is string ncHex && ncHex != "__OTHER__")
                store.NoteColor = ncHex;

            if (TextColorComboBox.SelectedItem is ComboBoxItem tcItem && tcItem.Tag is string tcHex && tcHex != "__OTHER__")
                store.TextColor = tcHex;

            if (PencilColorComboBox.SelectedItem is ComboBoxItem pcItem && pcItem.Tag is string pcHex && pcHex != "__OTHER__")
                store.PencilColor = pcHex;

            // 3. Տառատեսակ
            if (FontFamilyComboBox.SelectedItem is ComboBoxItem fontItem && fontItem.Tag is string fontName && fontName != "__OTHER__")
                store.FontFamily = fontName;

            // 4. Կետաչափ
            if (double.TryParse(FontSizeTextBox.Text.Trim(), out double fs) && fs >= 6 && fs <= 120)
                store.FontSize = fs;

            // 5. Ոճ
            int styleIdx = FontStyleComboBox.SelectedIndex;
            store.IsBold = (styleIdx == 1 || styleIdx == 3);
            store.IsItalic = (styleIdx == 2 || styleIdx == 3);

            // 6. Մատիտի հաստություն
            if (double.TryParse(PencilThicknessTextBox.Text.Trim(), out double pt) && pt >= 1 && pt <= 50)
                store.PencilThickness = pt;

            // 7. Ազատ մեծացում
            store.AllowFreeResize = (AllowFreeResizeCheckBox.IsChecked == true);

            // 8. Windows Startup
            if (AutostartCheckBox.IsChecked == true)
            {
                StartupManager.Enable();
            }
            else
            {
                StartupManager.Disable();
            }

            // 9. Պահպանել .dnote նիշքում
            DnoteStorage.Save(store);

            // 10. Թարմացնել ազատ չափափոխման հնարավորությունը բոլոր բաց թերթիկների վրա
            foreach (MainWindow window in Application.Current.Windows.OfType<MainWindow>())
            {
                window.UpdateResizeGripState();
            }

            // 11. Եթե նշված է «Կիրառել նոր նախընտրանքները նաև արդեն բացված թերթիկների վրա»
            if (ApplyToOpenNotesCheckBox.IsChecked == true)
            {
                ApplyPreferencesToOpenNotes();
            }

            Close();
        }

        private void ApplyPreferencesToOpenNotes()
        {
            MainWindow[] openNotes = Application.Current.Windows.OfType<MainWindow>().ToArray();

            foreach (MainWindow window in openNotes)
            {
                // Թարմացնում ենք NoteData-ի հատկությունները
                if (store.Notes.TryGetValue(window.NoteId, out NoteData? data))
                {
                    data.Width = store.NoteWidth;
                    data.Height = store.NoteHeight;
                    data.NoteColor = store.NoteColor;
                    data.TextColor = store.TextColor;
                    data.FontFamily = store.FontFamily;
                    data.FontSize = store.FontSize;
                    data.IsBold = store.IsBold;
                    data.IsItalic = store.IsItalic;
                }

                // Թարմացնում ենք պատուհանի վիզուալ տեսքը
                window.Note.Width = store.NoteWidth;
                window.Note.Height = store.NoteHeight;

                window.Note.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(store.NoteColor));
                window.NoteText.FontFamily = new FontFamily(store.FontFamily);
                window.NoteText.FontSize = store.FontSize;
                window.NoteText.FontWeight = store.IsBold ? FontWeights.Bold : FontWeights.Normal;
                window.NoteText.FontStyle = store.IsItalic ? FontStyles.Italic : FontStyles.Normal;
                window.NoteText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(store.TextColor));
            }

            DnoteStorage.Save(store);
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}

