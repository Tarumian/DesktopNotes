using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace DesktopNotes;

public partial class MainWindow
{
    private void QuickToolbar_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        UpdateTextColorPreview();
    }

    private void NoteWindow_MouseEnter(
        object sender,
        MouseEventArgs e)
    {
        QuickToolbar.Visibility = Visibility.Visible;
    }

    private void NoteWindow_MouseLeave(
        object sender,
        MouseEventArgs e)
    {
        if (TextColorPalettePopup.IsOpen || PencilColorPalettePopup.IsOpen)
        {
            return;
        }

        QuickToolbar.Visibility = Visibility.Collapsed;
    }

    private void TextColorPalettePopup_Closed(object? sender, EventArgs e)
    {
        if (!IsMouseOver)
        {
            QuickToolbar.Visibility = Visibility.Collapsed;
        }
    }

    private void PencilColorPalettePopup_Closed(object? sender, EventArgs e)
    {
        if (!IsMouseOver)
        {
            QuickToolbar.Visibility = Visibility.Collapsed;
        }
    }

    private void QuickBoldButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyFontWeight(
            IsSelectionBold()
                ? FontWeights.Normal
                : FontWeights.Bold);

        NoteText.Focus();
    }

    private void QuickItalicButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyFontStyle(
            IsSelectionItalic()
                ? FontStyles.Normal
                : FontStyles.Italic);

        NoteText.Focus();
    }

    private void QuickTextColorButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        TextColorPalettePopup.IsOpen = !TextColorPalettePopup.IsOpen;
    }

    private void PaletteColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex })
        {
            ApplySelectedTextColor(ColorFromHex(hex));
        }

        TextColorPalettePopup.IsOpen = false;
    }

    private void CustomTextColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (NoteText.Selection.IsEmpty)
        {
            TextColorPalettePopup.IsOpen = false;
            return;
        }

        Color displayedColor = GetDisplayedTextColor();

        Forms.ColorDialog dialog = new()
        {
            FullOpen = true,
            Color = Drawing.Color.FromArgb(
                displayedColor.R,
                displayedColor.G,
                displayedColor.B)
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            ApplySelectedTextColor(
                ColorFromHex(
                    $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"));
        }

        TextColorPalettePopup.IsOpen = false;
    }

    private void NoteText_SelectionChanged(
        object sender,
        RoutedEventArgs e)
    {
        UpdateTextColorPreview();
    }

    private void ApplySelectedTextColor(Color color)
    {
        if (NoteText.Selection.IsEmpty)
        {
            return;
        }

        NoteText.Selection.ApplyPropertyValue(
            TextElement.ForegroundProperty,
            new SolidColorBrush(color));

        UpdateTextColorPreview();
        NoteText.Focus();
    }

    private void UpdateTextColorPreview()
    {
        Border? preview = QuickTextColorButton.Template.FindName(
            "QuickTextColorPreview",
            QuickTextColorButton) as Border;

        UIElement? mixedIndicator = QuickTextColorButton.Template.FindName(
            "MixedColorIndicator",
            QuickTextColorButton) as UIElement;

        if (preview == null || mixedIndicator == null)
        {
            return;
        }

        object foreground = NoteText.Selection.GetPropertyValue(
            TextElement.ForegroundProperty);

        if (!NoteText.Selection.IsEmpty &&
            foreground == DependencyProperty.UnsetValue)
        {
            preview.Background = ColorFromBrush("#E7E7E7");
            mixedIndicator.Visibility = Visibility.Visible;
            return;
        }

        preview.Background = foreground is SolidColorBrush brush
            ? brush
            : ColorFromBrush(Data.TextColor);

        mixedIndicator.Visibility = Visibility.Collapsed;
    }

    private Color GetDisplayedTextColor()
    {
        object foreground = NoteText.Selection.GetPropertyValue(
            TextElement.ForegroundProperty);

        return foreground is SolidColorBrush brush
            ? brush.Color
            : ColorFromHex(Data.TextColor);
    }

    private bool IsSelectionBold()
    {
        object value = NoteText.Selection.GetPropertyValue(
            TextElement.FontWeightProperty);

        return value is FontWeight weight && weight == FontWeights.Bold;
    }

    private bool IsSelectionItalic()
    {
        object value = NoteText.Selection.GetPropertyValue(
            TextElement.FontStyleProperty);

        return value is FontStyle style && style == FontStyles.Italic;
    }

    public SolidColorBrush GetNoteHoverBrush(double factor = 0.85)
    {
        if (Note.Background is SolidColorBrush solid)
        {
            Color c = solid.Color;
            byte r = (byte)Math.Max(0, Math.Min(255, (int)(c.R * factor)));
            byte g = (byte)Math.Max(0, Math.Min(255, (int)(c.G * factor)));
            byte b = (byte)Math.Max(0, Math.Min(255, (int)(c.B * factor)));
            return new SolidColorBrush(Color.FromArgb(c.A, r, g, b));
        }
        return new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
    }

    public SolidColorBrush GetNoteHoverForeground()
    {
        if (Note.Background is SolidColorBrush solid)
        {
            Color c = solid.Color;
            byte r = (byte)Math.Max(0, (int)(c.R * 0.35));
            byte g = (byte)Math.Max(0, (int)(c.G * 0.35));
            byte b = (byte)Math.Max(0, (int)(c.B * 0.35));
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        return new SolidColorBrush(ColorFromHex("#222222"));
    }

    private void QuickButton_MouseEnter(
        object sender,
        MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.Background = GetNoteHoverBrush(0.85);
            button.Foreground = GetNoteHoverForeground();
        }
    }

    private void QuickButton_MouseLeave(
        object sender,
        MouseEventArgs e)
    {
        if (sender is Button button)
        {
            if (button == QuickPencilButton && isPencilActive)
            {
                button.Background = GetNoteHoverBrush(0.70);
                button.BorderBrush = GetNoteHoverForeground();
                button.Foreground = GetNoteHoverForeground();
                return;
            }

            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.Foreground = ColorFromBrush("#555555");
        }
    }

    private static SolidColorBrush ColorFromBrush(string hex)
    {
        return new SolidColorBrush(ColorFromHex(hex));
    }

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }

    // =========================================================
    // ՏԱՌԱՉԱՓ (+ / -)
    // =========================================================

    private void QuickFontSizeIncrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustFontSize(1);
    }

    private void QuickFontSizeDecrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustFontSize(-1);
    }

    private void AdjustFontSize(double delta)
    {
        if (!NoteText.Selection.IsEmpty)
        {
            object val = NoteText.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            double current = (val is double d) ? d : NoteText.FontSize;
            double next = Math.Clamp(current + delta, 6, 96);
            NoteText.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, next);
        }
        else
        {
            NoteText.FontSize = Math.Clamp(NoteText.FontSize + delta, 6, 96);
        }

        UpdateDataFromWindow();
        DnoteStorage.Save(((App)Application.Current).Store);
        NoteText.Focus();
    }

    // =========================================================
    // ՏՈՂԱՄԻՋՅԱՆ ՀԵՌԱՎՈՐՈՒԹՅՈՒՆ (+ / -)
    // =========================================================

    private void QuickLineSpacingIncrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustLineSpacing(2);
    }

    private void QuickLineSpacingDecrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustLineSpacing(-2);
    }

    private void AdjustLineSpacing(double delta)
    {
        List<Paragraph> targetParagraphs = GetTargetParagraphs();
        foreach (Paragraph p in targetParagraphs)
        {
            double current = double.IsNaN(p.LineHeight) || p.LineHeight <= 0 ? (p.FontSize * 1.2) : p.LineHeight;
            p.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            p.LineHeight = Math.Clamp(current + delta, 8, 120);
        }

        UpdateDataFromWindow();
        DnoteStorage.Save(((App)Application.Current).Store);
        NoteText.Focus();
    }

    // =========================================================
    // ՊԱՐԲԵՐՈՒԹՅՈՒՆՆԵՐԻ ՀԵՌԱՎՈՐՈՒԹՅՈՒՆ (+ / -)
    // =========================================================

    private void QuickParagraphSpacingIncrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustParagraphSpacing(2);
    }

    private void QuickParagraphSpacingDecrease_Click(
        object sender,
        RoutedEventArgs e)
    {
        AdjustParagraphSpacing(-2);
    }

    private void AdjustParagraphSpacing(double delta)
    {
        List<Paragraph> targetParagraphs = GetTargetParagraphs();
        foreach (Paragraph p in targetParagraphs)
        {
            double currentBottom = p.Margin.Bottom;
            double nextBottom = Math.Clamp(currentBottom + delta, 0, 60);
            p.Margin = new Thickness(p.Margin.Left, p.Margin.Top, p.Margin.Right, nextBottom);
        }

        UpdateDataFromWindow();
        DnoteStorage.Save(((App)Application.Current).Store);
        NoteText.Focus();
    }

    private System.Collections.Generic.List<Paragraph> GetTargetParagraphs()
    {
        System.Collections.Generic.List<Paragraph> list = new();
        if (!NoteText.Selection.IsEmpty)
        {
            TextPointer start = NoteText.Selection.Start;
            TextPointer end = NoteText.Selection.End;

            Paragraph? p1 = start.Paragraph;
            Paragraph? p2 = end.Paragraph;

            if (p1 != null && p2 != null)
            {
                Block? cur = p1;
                while (cur != null)
                {
                    if (cur is Paragraph para)
                    {
                        list.Add(para);
                    }
                    if (cur == p2) break;
                    cur = cur.NextBlock;
                }
            }
        }

        if (list.Count == 0)
        {
            Paragraph? curPara = NoteText.CaretPosition.Paragraph;
            if (curPara != null)
            {
                list.Add(curPara);
            }
            else
            {
                foreach (Block b in NoteText.Document.Blocks)
                {
                    if (b is Paragraph p) list.Add(p);
                }
            }
        }
        return list;
    }
}
