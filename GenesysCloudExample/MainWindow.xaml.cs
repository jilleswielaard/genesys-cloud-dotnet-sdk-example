using System;
using System.Windows;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Model;
using PureCloudPlatform.Client.V2.Extensions.Notifications;
using System.Diagnostics;

namespace GenesysCloudExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AuthTokenInfo _accessTokenInfo = null;
        UserMe _me = null;
        string _conversationId = null;
        GenesysCloud _genesysCloud = new GenesysCloud();

        public MainWindow()
        {
            InitializeComponent();
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
                _accessTokenInfo = await GenesysCloud.Login();
                this.Activate();
                output("get me");
                _me = _genesysCloud.GetMe();
                output("subscribe");
                Subscribe(_me);
                btnCall.IsEnabled = true;
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        private void Call_Click(object sender, RoutedEventArgs e)
        {
            if (_accessTokenInfo != null && string.IsNullOrEmpty(_conversationId))
            {
                try
                {
                    const string phoneNumber = "0642453275";
                    output("Call: " + phoneNumber);
                    _conversationId = _genesysCloud.Call(phoneNumber);
                    btnDisconnect.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    output(ex.Message);
                }
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_conversationId))
            {
                try
                {
                    output("Disconnect: " + _conversationId);
                    _genesysCloud.Disconnect(_conversationId);
                    _conversationId = null;
                    btnDisconnect.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    output(ex.Message);
                }
            }
        }

        private void Subscribe(UserMe me)
        {
            NotificationHandler handler = GenesysCloud.Subscribe(me);
            handler.NotificationReceived += (data) =>
            {
                if (data.GetType() == typeof(NotificationData<PresenceEventUserPresence>))
                {
                    NotificationData<PresenceEventUserPresence> presence = (NotificationData<PresenceEventUserPresence>)data;
                    string status = presence.EventBody.PresenceDefinition.SystemPresence;
                    Debug.WriteLine($"New presence: { status }");
                    this.Dispatcher.Invoke(() =>
                    {
                        lblStatus.Content = status;
                    });
                }
            };
        }
    }
}
