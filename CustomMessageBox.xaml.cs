using System.Windows;

namespace LuaToolsGameChecker
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxButton
        {
            OK,
            YesNo
        }

        public enum MessageBoxResult
        {
            None,
            OK,
            Yes,
            No
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private CustomMessageBox(string message, string title, MessageBoxButton button)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            if (button == MessageBoxButton.YesNo)
            {
                Button1.Content = "Yes";
                Button2.Content = "No";
                Button2.Visibility = Visibility.Visible;
            }
            else
            {
                Button1.Content = "OK";
                Button2.Visibility = Visibility.Collapsed;
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = Button1.Content.ToString() == "Yes" ? MessageBoxResult.Yes : MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            DialogResult = false;
            Close();
        }

        public static MessageBoxResult Show(string message, string title = "Message", MessageBoxButton button = MessageBoxButton.OK)
        {
            var dialog = new CustomMessageBox(message, title, button);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}
