using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace Microsoft.Maker.Storage.OneDrive
{
    public sealed class OneDriveConnector
    {
        /// <summary>
        /// Is true if currently logged in to OneDrive, false otherwise.
        /// </summary>
        public bool isLoggedIn { get; private set; } = false;
        public string accessToken { get; private set; } = string.Empty;
        public string refreshToken { get; private set; } = string.Empty;

        private const int ReauthSpanHours = 0;
        private const int ReauthSpanMinutes = 50;
        private const int ReauthSpanSeconds = 0;

        private const string LoginUriFormat = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope=wl.offline_access onedrive.readwrite&response_type=code&redirect_uri={1}";
        private const string LogoutUriFormat = "https://login.live.com/oauth20_logout.srf?client_id={0}&redirect_uri={1}";
        private const string UploadUrlFormat = "https://api.onedrive.com/v1.0/drive/root:{0}/{1}:/content";
        private const string DeleteUrlFormat = "https://api.onedrive.com/v1.0/drive/root:{0}/{1}";
        private const string ListUrlFormat = "https://api.onedrive.com/v1.0/drive/root:{0}:/children";
        private const string TokenUri = "https://login.live.com/oauth20_token.srf";
        private const string TokenContentFormat = "client_id={0}&redirect_uri={1}&client_secret={2}&{3}={4}&grant_type={5}";

        private HttpClient httpClient;
        private Timer refreshTimer;
        private string clientId = string.Empty;
        private string clientSecret = string.Empty;
        private string redirectUrl = string.Empty;

        public event EventHandler<string> TokensChangedEvent;

        /// <summary>
        /// Instantiates a OneDrive connector object. Requires a call to "login" function to complete authorization.
        /// </summary>
        public OneDriveConnector()
        {
            var filter = new HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            httpClient = new HttpClient(filter);
        }

        public string FormatAccessTokenUriString(string clientId, string redirectUri)
        {
            return string.Format(LoginUriFormat, clientId, redirectUri);
        }

        /// <summary>
        /// Obtains authorization codes from OneDrive login service. Requires access code to be obtained from OneDrive as described in the OneDrive authorization documentation.
        /// </summary>
        /// <param name="clientId"></param> Client ID obtained from app registration
        /// <param name="clientSecret"></param> Client secret obtained from app registration
        /// <param name="redirectUrl"></param> Redirect URL obtained from app registration
        /// <param name="accessCode"></param> Access Code obtained from earlier login prompt.
        public IAsyncAction LoginAsync(string clientId, string clientSecret, string redirectUrl, string accessCode)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.redirectUrl = redirectUrl;

            return Task.Run(async () =>
            {
                await GetTokens(accessCode, "code", "authorization_code");
                StartTimer();
            }).AsAsyncAction();
           
        }

        /// <summary>
        /// Reauthorizes the connection to OneDrive with the provided access and refresh tokens, and saves those tokens internally for future use
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public IAsyncAction Reauthorize(string refreshToken)
        {
            return Task.Run(async () =>
            {
                await GetTokens(refreshToken, "refresh_token", "refresh_token");
            }).AsAsyncAction();
        }

        /// <summary>
        /// Calls the OneDrive reauth service with current authorization tokens
        /// </summary>
        /// <returns></returns>
        public IAsyncAction Reauthorize()
        {
            return Task.Run(async () =>
            {
                await Reauthorize(refreshToken);
            }).AsAsyncAction();
        }

        /// <summary>
        /// Uploads a file to OneDrive. This method is NOT thread safe. It assumes that the contents of the file will not change during the upload process. 
        /// </summary>
        /// <param name="file"></param> The file to upload to OneDrive. The file will be read, and a copy uploaded. The original file object will not be modified.
        /// <param name="destinationPath"></param> The path to the destination on Onedrive. Passing in an empty string will place the file in the root of Onedrive. Other folder paths should be passed in with a leading '/' character, such as "/Documents" or "/Pictures/Random"
        public IAsyncAction UploadFileAsync(StorageFile file, string destinationPath)
        {
            string uploadUri = String.Format(UploadUrlFormat, destinationPath, file.Name);

            return Task.Run(async () =>
            {
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
             }).AsAsyncAction(); 
        }

        /// <summary>
        /// Deletes a file to OneDrive.
        /// </summary>
        /// <param name="fileName"></param> The name of the file to delete
        /// <param name="pathToFile"></param> The path to the file on Onedrive. Passing in an empty string will look for the file in the root of Onedrive. Other folder paths should be passed in with a leading '/' character, such as "/Documents" or "/Pictures/Random"
        public IAsyncAction DeleteFileAsync(string fileName, string pathToFile)
        {
            string deleteUri = String.Format(DeleteUrlFormat, pathToFile, fileName);
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, new Uri(deleteUri)))
            {
                return Task.Run(async () =>
                {
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(requestMessage))
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }).AsAsyncAction();
            }
        }

        /// <summary>
        /// List the names of all the files in a OneDrive folder.
        /// </summary>
        /// <param name="folderPath"></param> The path to the folder on OneDrive. Passing in an empty string will list the files in the root of Onedrive. Other folder paths should be passed in with a leading '/' character, such as "/Documents" or "/Pictures/Random".
        public async Task<List<string>> ListFilesAsync(string fileName, string folderPath)
        {
            string listUri = String.Format(ListUrlFormat, folderPath, fileName);
            List<string> files = null;

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(listUri)))
            {
                using (HttpResponseMessage response = await httpClient.SendRequestAsync(requestMessage))
                {
                    if (response.StatusCode == HttpStatusCode.Ok)
                    {
                        files = new List<string>();
                        using (var inputStream = await response.Content.ReadAsInputStreamAsync())
                        {
                            using (var memStream = new MemoryStream())
                            {
                                using (Stream testStream = inputStream.AsStreamForRead())
                                {
                                    await testStream.CopyToAsync(memStream);
                                    memStream.Position = 0;
                                    using (StreamReader reader = new StreamReader(memStream))
                                    {
                                        //Get file name
                                        string result = reader.ReadToEnd();

                                        //TODO: Find the filenames in the string and add to files list.
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return files;
        }

        /// <summary>
        /// Disposes of any user specific data obtained during login process.
        /// </summary>
        public IAsyncAction LogoutAsync()
        {
            clientId = string.Empty;
            clientSecret = string.Empty;
            redirectUrl = string.Empty;
            accessToken = string.Empty;
            refreshToken = string.Empty;
            refreshTimer.Dispose();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.Dispose();
            isLoggedIn = false;

            string logoutUri = string.Format(LogoutUriFormat, clientId, redirectUrl);
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(logoutUri)))
            {
                return Task.Run(async () =>
                {
                    using (HttpResponseMessage response = await httpClient.SendRequestAsync(requestMessage))
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }).AsAsyncAction();
            }

        }

        private async void ReauthorizeOnTimer(object stateInfo)
        {
            await Reauthorize();
        }

        private void StartTimer()
        {
            //Set up timer to reauthenticate OneDrive login
            TimerCallback callBack = this.ReauthorizeOnTimer;
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

                    EventHandler<string> handler = TokensChangedEvent;

                    if (null != handler)
                    {
                        handler(this, "Tokens Changed");
                    }

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
