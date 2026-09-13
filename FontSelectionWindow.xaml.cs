using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DesktopNotes
{
    public partial class FontSelectionWindow : Window
    {
        public string SelectedFontFamily { get; private set; } = "Comic Sans MS";
        public double SelectedFontSize { get; private set; } = 14;
        public bool SelectedIsBold { get; private set; }
        public bool SelectedIsItalic { get; private set; }

        private bool isInitializing = true;

        public FontSelectionWindow(string initialFont, double initialSize, bool initialBold, bool initialItalic)
        {
            InitializeComponent();

            PopulateFontFamilies(initialFont);

            SelectedFontSize = initialSize > 0 ? initialSize : 14;
            FontSizeTextBox.Text = SelectedFontSize.ToString("0");

            SelectedIsBold = initialBold;
            SelectedIsItalic = initialItalic;

            if (initialBold && initialItalic)
                FontStyleComboBox.SelectedIndex = 3;
            else if (initialItalic)
                FontStyleComboBox.SelectedIndex = 2;
            else if (initialBold)
                FontStyleComboBox.SelectedIndex = 1;
            else
                FontStyleComboBox.SelectedIndex = 0;

            isInitializing = false;
            UpdatePreview();
        }

        private void PopulateFontFamilies(string selectedFont)
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

            if (FontFamilyComboBox.Items.Count > 0)
            {
                FontFamilyComboBox.Items.Add(new Separator());
            }

            foreach (string font in systemFonts)
            {
                if (!topFonts.Contains(font))
                {
                    FontFamilyComboBox.Items.Add(new ComboBoxItem { Content = font, Tag = font });
                }
            }

            // Ընտրություն
            int selectIndex = 0;
            for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
            {
                if (FontFamilyComboBox.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Tag as string, selectedFont, StringComparison.OrdinalIgnoreCase))
                {
                    selectIndex = i;
                    break;
                }
            }

            FontFamilyComboBox.SelectedIndex = selectIndex;
            if (FontFamilyComboBox.SelectedItem is ComboBoxItem cbi && cbi.Tag is string fn)
            {
                SelectedFontFamily = fn;
            }
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            if (isInitializing) return;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (PreviewTextBlock == null) return;

            if (FontFamilyComboBox?.SelectedItem is ComboBoxItem cbi && cbi.Tag is string fontName)
            {
                SelectedFontFamily = fontName;
                PreviewTextBlock.FontFamily = new FontFamily(fontName);
            }

            if (double.TryParse(FontSizeTextBox?.Text?.Trim(), out double sz) && sz >= 6 && sz <= 120)
            {
                SelectedFontSize = sz;
                PreviewTextBlock.FontSize = Math.Min(24, sz); // Clamp preview size so it fits nicely
            }

            int styleIdx = FontStyleComboBox?.SelectedIndex ?? 0;
            SelectedIsBold = (styleIdx == 1 || styleIdx == 3);
            SelectedIsItalic = (styleIdx == 2 || styleIdx == 3);

            PreviewTextBlock.FontWeight = SelectedIsBold ? FontWeights.Bold : FontWeights.Normal;
            PreviewTextBlock.FontStyle = SelectedIsItalic ? FontStyles.Italic : FontStyles.Normal;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (FontFamilyComboBox.SelectedItem is ComboBoxItem cbi && cbi.Tag is string fontName)
            {
                SelectedFontFamily = fontName;
            }

            if (double.TryParse(FontSizeTextBox.Text.Trim(), out double sz) && sz >= 6 && sz <= 120)
            {
                SelectedFontSize = sz;
            }
            else
            {
                MessageBox.Show(this, "Կետաչափը պետք է լինի 6-ից 120 միջակայքում:", "Տառատեսակ", MessageBoxButton.OK, MessageBoxImage.Warning);
                FontSizeTextBox.Focus();
                return;
            }

            int styleIdx = FontStyleComboBox.SelectedIndex;
            SelectedIsBold = (styleIdx == 1 || styleIdx == 3);
            SelectedIsItalic = (styleIdx == 2 || styleIdx == 3);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
