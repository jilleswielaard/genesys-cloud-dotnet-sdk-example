using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Model;
using PureCloudPlatform.Client.V2.Extensions.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace GenesysCloudExample
{
    class GenesysCloud
    {
        // private
        private const string _clientID = "8e2837eb-5a7b-4930-a648-5691e756be0a";
        private const string _clientSecret = "CAuDKbAvEA32R_yNFMwI2_iDRXuRV1k1BVsxYilh_zI";
        private const string _authorizationEndpoint = "https://login.mypurecloud.de/oauth/authorize";
        private ConversationsApi _conversationsApi = new();
        private UsersApi _usersApi = new();

        // public
        public UserMe me;
        public AuthTokenInfo accessTokenInfo;
        public Dictionary<string, ConversationCallEventTopicCallConversation> conversations = new();

        private async Task Login()
        {
            Debug.WriteLine("Login!!");
            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = "http://localhost:52532/";
            Debug.WriteLine("Redirect URI: " + redirectURI);

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}?response_type=code&client_id={1}&redirect_uri={2}", _authorizationEndpoint, _clientID, Uri.EscapeDataString(redirectURI));
            Debug.WriteLine("Authorization Request: " + authorizationRequest);

            // Creates an HttpListener to listen for requests on that redirect URI.
            HttpListener listener = new();
            listener.Prefixes.Add(redirectURI);
            listener.Start();
            Debug.WriteLine("Listening...");

            // Opens request in the browser.
            ProcessStartInfo psi = new()
            {
                FileName = authorizationRequest,
                UseShellExecute = true
            };
            Process.Start(psi);

            // Waits for the OAuth authorization response.
            HttpListenerContext context = await listener.GetContextAsync();

            // extracts the code
            string code = context.Request.QueryString.Get("code");
            Debug.WriteLine("Authorization code: " + code);

            // Create response
            HttpListenerResponse response = context.Response;
            string responseString = "<html><head></head><body><h2>close this page</h2></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            // Sends an HTTP response to the browser
            response.Close(buffer, true);
            // stop listener
            listener.Stop();

            PureCloudRegionHosts region = PureCloudRegionHosts.eu_central_1;
            Configuration.Default.ApiClient.setBasePath(region);

            accessTokenInfo = Configuration.Default.ApiClient.PostToken(_clientID, _clientSecret, redirectURI, code);
        }

        private void GetMe()
        {
            Debug.WriteLine("Get Me!!");
            List<string> expand = new List<string>();
            expand.Add("presence");
            me = _usersApi.GetUsersMe(expand);
        }

        private void Subscribe()
        {
            Debug.WriteLine("Subscribe!!");
            NotificationHandler handler = new();
            List<Tuple<string, Type>> subscriptions = new();
            subscriptions.Add(new Tuple<string, Type>($"v2.users.{me.Id}.presence", typeof(PresenceEventUserPresence)));
            subscriptions.Add(new Tuple<string, Type>($"v2.users.{me.Id}.conversations.calls", typeof(ConversationCallEventTopicCallConversation)));
            handler.AddSubscriptions(subscriptions);
            handler.NotificationReceived += (data) =>
            {
                if (data.GetType() == typeof(NotificationData<PresenceEventUserPresence>))
                {
                    NotificationData<PresenceEventUserPresence> presence = (NotificationData<PresenceEventUserPresence>)data;
                    string status = presence.EventBody.PresenceDefinition.SystemPresence;
                    Debug.WriteLine($"New presence: { status }");
                    me.Presence.PresenceDefinition.SystemPresence = status;
                }
                if (data.GetType() == typeof(NotificationData<ConversationCallEventTopicCallConversation>))
                {
                    NotificationData<ConversationCallEventTopicCallConversation> conversation = (NotificationData<ConversationCallEventTopicCallConversation>)data;
                    conversations[conversation.EventBody.Id] = conversation.EventBody;
                    conversations = conversations.Where(conversation => conversation.Value.Participants.FindLast(x => x.User?.Id == me.Id).State != ConversationCallEventTopicCallMediaParticipant.StateEnum.Terminated).ToDictionary(p => p.Key, p => p.Value);
                }
            };
        }

        public async Task Connect()
        {
            await Login();
            GetMe();
            Subscribe();
        }

        public string Call(string number)
        {
            Debug.WriteLine("Call: " + number);
            CreateCallRequest request = new CreateCallRequest();
            request.PhoneNumber = number;
            CreateCallResponse response = _conversationsApi.PostConversationsCalls(request);
            Debug.WriteLine(response.Id);
            return response.Id;
        }

        public void Disconnect()
        {
            Debug.WriteLine("Disconnect");
            foreach (var conversation in conversations.Values)
            {
                var conversationId = conversation.Id;
                var participantId = conversation.Participants.FindLast(c => c.User?.Id == me.Id).Id;
                var body = new MediaParticipantRequest();
                body.State = MediaParticipantRequest.StateEnum.Disconnected;
                Debug.WriteLine("Disconnect conversation: " + conversationId + " participant: " + participantId);
                _conversationsApi.PatchConversationsCallParticipant(conversationId, participantId, body);
            }
        }
    }
}
