using System.Windows;

namespace CsAC_Client
{
    public partial class CloseOptionWindow : Window
    {
        public CloseOptionWindow()
        {
            InitializeComponent();
        }

        public CloseAction Action { get; private set; } = CloseAction.Cancel;
        public bool RememberChoice { get; private set; } = false;

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Action = CloseAction.MinimizeToTray;
            RememberChoice = ChkRemember.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Action = CloseAction.Exit;
            RememberChoice = ChkRemember.IsChecked == true;
            DialogResult = true;
            Close();
        }
    }

    public enum CloseAction
    {
        Cancel,
        MinimizeToTray,
        Exit
    }
}