using System.Windows;

namespace DesktopNotes
{
    public partial class PreferencesWindow : Window
    {
        public PreferencesWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
