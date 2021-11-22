using System;
using System.Windows;

namespace GenesysCloudExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GenesysCloud _genesysCloud = new GenesysCloud();

        public MainWindow()
        {
            InitializeComponent();
            lblStatus.DataContext = _genesysCloud;
            dgConversations.DataContext = _genesysCloud;
        }

        private void output(string output)
        {
            textBoxOutput.Text = textBoxOutput.Text + output + Environment.NewLine;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                output("login");
                await _genesysCloud.Connect();
                this.Activate();
                btnCall.IsEnabled = true;
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        private void Call_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                const string phoneNumber = "0642453275";
                output("Call: " + phoneNumber);
                var conversationId = _genesysCloud.Call(phoneNumber);
                output(conversationId);
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                output("Disconnect");
                _genesysCloud.Disconnect();
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }
    }
}
