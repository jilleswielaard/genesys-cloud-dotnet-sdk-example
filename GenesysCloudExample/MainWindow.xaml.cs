using System;
using System.Windows;
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
                await _genesysCloud.Login();
                this.Activate();
                output("get me");
                _genesysCloud.GetMe();
                lblStatus.Content = _genesysCloud.me.Presence.PresenceDefinition.SystemPresence.ToUpper();
                output("subscribe");
                Subscribe();
                btnCall.IsEnabled = true;
            }
            catch (Exception ex)
            {
                output(ex.Message);
            }
        }

        private void Call_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_conversationId))
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

        private void Subscribe()
        {
            NotificationHandler handler = _genesysCloud.Subscribe();
            handler.NotificationReceived += (data) =>
            {
                if (data.GetType() == typeof(NotificationData<PresenceEventUserPresence>))
                {
                    NotificationData<PresenceEventUserPresence> presence = (NotificationData<PresenceEventUserPresence>) data;
                    string status = presence.EventBody.PresenceDefinition.SystemPresence;
                    Debug.WriteLine($"New presence: { status }");
                    this.Dispatcher.Invoke(() =>
                    {
                        lblStatus.Content = status;
                    });
                }
                if (data.GetType() == typeof(NotificationData<ConversationCallEventTopicCallConversation>))
                {
                    NotificationData<ConversationCallEventTopicCallConversation> conversation = (NotificationData<ConversationCallEventTopicCallConversation>) data;
                    string id = conversation.EventBody.Id;
                    Debug.WriteLine($"Conversation Updated: { id }");
                }
            };
        }
    }
}
