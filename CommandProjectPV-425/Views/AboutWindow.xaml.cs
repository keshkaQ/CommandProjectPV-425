using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CommandProjectPV_425.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.AboutWindowViewModel();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Открывает ссылку в браузере по умолчанию
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true; // Предотвращает дальнейшую обработку события WPF
        }
    }
}