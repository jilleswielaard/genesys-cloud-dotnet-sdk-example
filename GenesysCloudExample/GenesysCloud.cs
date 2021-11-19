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
        public UserMe me = null;
        public AuthTokenInfo accessTokenInfo = null;

        public async Task<AuthTokenInfo> Login()
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
            return accessTokenInfo;
        }

        public UserMe GetMe()
        {
            Debug.WriteLine("Get Me!!");
            List<string> expand = new List<string>();
            expand.Add("presence");
            me = _usersApi.GetUsersMe(expand);
            return me;
        }

        public NotificationHandler Subscribe()
        {
            Debug.WriteLine("Subscribe!!");
            NotificationHandler handler = new();
            handler.AddSubscription($"v2.users.{me.Id}.presence", typeof(PresenceEventUserPresence));
            return handler;
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

        public void Disconnect(string conversationId)
        {
            Debug.WriteLine("Disconnect: " + conversationId);
            _conversationsApi.PostConversationDisconnect(conversationId);
        }
    }
}
