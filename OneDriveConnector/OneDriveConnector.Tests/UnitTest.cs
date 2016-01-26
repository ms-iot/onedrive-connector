using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;
using Windows.Web.Http;

namespace OneDriveConnector.Tests
{
    [TestClass]
    public class OneDriveConnectorTests
    {
        [TestMethod]
        public void TestAccessTokenRequestFormatter()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            string receivedMessage = "";
            string expectedMessage = "https://login.live.com/oauth20_authorize.srf?client_id=clientId&scope=wl.offline_access onedrive.readwrite&response_type=code&redirect_uri=redirectUri";

            //act
            receivedMessage = oneDriveConnector.FormatAccessTokenUriString("clientId", "redirectUri");

            //assert
            Assert.AreEqual(expectedMessage, receivedMessage);
        }

        [TestMethod]
        public async Task TestLoginResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            HttpResponseMessage response;
            HttpStatusCode receivedStatus;
            HttpStatusCode expectedStatus = HttpStatusCode.BadRequest;

            //act
            response = await oneDriveConnector.LoginAsync("noClientID", "noClientSecret", "noRedirectUrl", "noAccessCode");
            receivedStatus = response.StatusCode;

            //assert
            Assert.AreEqual(expectedStatus, receivedStatus);
        }

        [TestMethod]
        public async Task TestUploadResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("test.test", CreationCollisionOption.ReplaceExisting);
            HttpResponseMessage response;
            HttpStatusCode receivedStatus;
            HttpStatusCode expectedStatus = HttpStatusCode.Unauthorized;

            //act
            response = await oneDriveConnector.UploadFileAsync(file, "");
            receivedStatus = response.StatusCode;

            //assert
            Assert.AreEqual(expectedStatus, receivedStatus);
        }

        [TestMethod]
        public async Task TestListResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            KeyValuePair<HttpResponseMessage, IList<string>> response;
            IList<string> list;
            HttpStatusCode receivedStatus;
            HttpStatusCode expectedStatus = HttpStatusCode.Unauthorized;

            //act
            response = await oneDriveConnector.ListFilesAsync("");
            receivedStatus = response.Key.StatusCode;

            //assert
            Assert.AreEqual(expectedStatus, receivedStatus);
        }

        [TestMethod]
        public async Task TestDeleteResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            HttpResponseMessage response;
            HttpStatusCode receivedStatus;
            HttpStatusCode expectedStatus = HttpStatusCode.Unauthorized;

            //act
            response = await oneDriveConnector.DeleteFileAsync("name", "");
            receivedStatus = response.StatusCode;

            //assert
            Assert.AreEqual(expectedStatus, receivedStatus);
        }
    }
}
