using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.Maker.Storage;
using System.Net;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

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
            string receivedExceptionMessage = "";
            string expectedExceptionMessage = "Bad request (400).\r\n\r\nResponse status code does not indicate success: 400 (Bad Request).";

            //act
            try
            {
                await oneDriveConnector.LoginAsync("noClientID", "noClientSecret", "noRedirectUrl", "noAccessCode");
            }
            catch (Exception e)
            {
                receivedExceptionMessage = e.Message;
            }

            //assert
            Assert.AreEqual(expectedExceptionMessage, receivedExceptionMessage);
        }

        [TestMethod]
        public async Task TestUploadResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("test.test", CreationCollisionOption.ReplaceExisting);
            string receivedExceptionMessage = "";
            string expectedExceptionMessage = "Unauthorized (401).\r\n\r\nResponse status code does not indicate success: 401 (Unauthorized).";

            //act
            try
            {
                await oneDriveConnector.UploadFileAsync(file, "");
            }
            catch (Exception e)
            {
                receivedExceptionMessage = e.Message;
            }

            //assert
            Assert.AreEqual(expectedExceptionMessage, receivedExceptionMessage);
        }

        [TestMethod]
        public async Task TestListResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            IList<string> list;
            string receivedExceptionMessage = "";
            string expectedExceptionMessage = "Unauthorized (401).\r\n\r\nResponse status code does not indicate success: 401 (Unauthorized).";

            //act
            try
            {
                var response = await oneDriveConnector.ListFilesAsync("");
                list = response.Value;
            }
            catch (Exception e)
            {
                receivedExceptionMessage = e.Message;
            }

            //assert
            Assert.AreEqual(expectedExceptionMessage, receivedExceptionMessage);
        }

        [TestMethod]
        public async Task TestDeleteResponseWithoutCredentials()
        {
            // arrange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();
            string receivedExceptionMessage = "";
            string expectedExceptionMessage = "Unauthorized (401).\r\n\r\nResponse status code does not indicate success: 401 (Unauthorized).";

            //act
            try
            {
                await oneDriveConnector.DeleteFileAsync("name", "");
            }
            catch (Exception e)
            {
                receivedExceptionMessage = e.Message;
            }

            //assert
            Assert.AreEqual(expectedExceptionMessage, receivedExceptionMessage);
        }
    }
}
