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
        if (TextColorPalettePopup.IsOpen)
        {
            return;
        }

        QuickToolbar.Visibility = Visibility.Collapsed;
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

    private void QuickButton_MouseEnter(
        object sender,
        MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.Background = ColorFromBrush("#FDE489");
            button.Foreground = ColorFromBrush("#5C4814");
        }
    }

    private void QuickButton_MouseLeave(
        object sender,
        MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.Background = Brushes.Transparent;
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
}
