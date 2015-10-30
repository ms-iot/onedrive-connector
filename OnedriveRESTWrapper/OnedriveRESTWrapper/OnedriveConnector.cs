using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace Microsoft.Maker.Storage.OneDrive
{
    public sealed class OnedriveConnector
    {
        public bool isLoggedIn { get; private set; } = false;

        private const int ReauthSpanHours = 0;
        private const int ReauthSpanMinutes = 50;
        private const int ReauthSpanSeconds = 0;
              
        private const string LoginUriFormat = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";
        private const string LogoutUriFormat = "https://login.live.com/oauth20_logout.srf?client_id={0}&redirect_uri={1}";
        private const string ScopeForAuthRequests = "wl.offline_access onedrive.readwrite";
        private const string UploadUrlFormat = "https://api.onedrive.com/v1.0/drive/root:{1}/{2}:/content";
        private const string TokenUri = "https://login.live.com/oauth20_token.srf";
        private const string TokenContentFormat = "client_id={0}&redirect_uri={1}&client_secret={2}&{3}={4}&grant_type={5}";

        private HttpClient httpClient;
        private Timer refreshTimer;
        private string clientId = "";
        private string clientSecret = "";
        private string redirectUrl = "";
        private string accessToken = "";
        private string refreshToken = "";    

        public OnedriveConnector()
        {
            var filter = new HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            httpClient = new HttpClient(filter);
        }

        public async void Login(string clientId, string clientSecret, string redirectUrl, string accessCode)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.redirectUrl = redirectUrl;

            await GetTokens(accessCode, "code", "authorization_code");

            StartTimer();
        }

        /// <summary>
        /// Uploads a file to OneDrive. This method is NOT thread safe. It assumes that the contents of the file will not change during the upload process. 
        /// </summary>
        /// <param name="file"></param> The file to upload to OneDrive. The file will be read, and a copy uploaded. The original file object will not be modified.
        /// <param name="destinationPath"></param> The path to the destination on Onedrive. Passing in an empty string will place the file in the root of Onedrive. Other folder paths should be passed in with a leading '/' character, such as "/Documents" or "/Pictures/Random"
        public async void UploadFile(StorageFile file, string destinationPath)
        {
            string uploadUri = String.Format(UploadUrlFormat, destinationPath, file.Name);

            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                using (HttpStreamContent streamContent = new HttpStreamContent(stream.AsInputStream()))
                {
                    using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(uploadUri)))
                    {
                        requestMessage.Content = streamContent;

                        using (HttpResponseMessage responseMessage = await httpClient.SendRequestAsync(requestMessage))
                        {
                            responseMessage.EnsureSuccessStatusCode();
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Deletes a file to OneDrive.
        /// </summary>
        /// <param name="fileName"></param> The name of the file to delete
        /// <param name="pathToFile"></param> The path to the file on Onedrive. Passing in an empty string will look for the file in the root of Onedrive. Other folder paths should be passed in with a leading '/' character, such as "/Documents" or "/Pictures/Random"
        public async void DeleteFile(string fileName, string pathToFile)
        {
            string deleteUri = String.Format("https://api.onedrive.com/v1.0/drive/root:{1}/{2}", pathToFile, fileName);
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, new Uri(deleteUri)))
            {
                using (HttpResponseMessage response = await httpClient.SendRequestAsync(requestMessage))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public void Logout()
        {
            clientId = "";
            clientSecret = "";
            redirectUrl = "";
            accessToken = "";
            refreshToken = "";
            refreshTimer.Dispose();
            httpClient.DefaultRequestHeaders.Clear();
            isLoggedIn = false;
        }

        private async void Reauthorize(object stateInfo)
        {
            await GetTokens(refreshToken, "refresh_token", "refresh_token");
        }

        private void StartTimer()
        {
            //Set up timer to reauthenticate OneDrive login
            TimerCallback callBack = this.Reauthorize;
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            TimeSpan dueTime = new TimeSpan(0);
            TimeSpan period = new TimeSpan(ReauthSpanHours, ReauthSpanMinutes, ReauthSpanSeconds);
            refreshTimer = new Timer(callBack, autoEvent, dueTime, period);
        }

        private async Task GetTokens(string accessCodeOrRefreshToken, string requestType, string grantType)
        {          
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(TokenUri)))
            {
                string requestContent = string.Format(TokenContentFormat, clientId, redirectUrl, clientSecret, requestType, accessCodeOrRefreshToken, grantType);
                requestMessage.Content = new HttpStringContent(requestContent);
                requestMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage responseMessage = await httpClient.SendRequestAsync(requestMessage))
                {
                    responseMessage.EnsureSuccessStatusCode();
                    string responseContentString = await responseMessage.Content.ReadAsStringAsync();
                    accessToken = GetAccessToken(responseContentString);
                    refreshToken = GetRefreshToken(responseContentString);
                    httpClient.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", accessToken);
                    isLoggedIn = true;
                }
            }

        } 

        private string GetAccessToken(string responseContent)
        {
            string identifier = "\"access_token\":\"";
            int startIndex = responseContent.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }

        private string GetRefreshToken(string responseContent)
        {
            string identifier = "\"refresh_token\":\"";
            int startIndex = responseContent.IndexOf(identifier) + identifier.Length;
            int endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }

    }
}
