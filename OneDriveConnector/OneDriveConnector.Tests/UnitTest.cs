using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.Maker.Storage;


namespace OneDriveConnector.Tests
{
    [TestClass]
    public class OneDriveConnectorTests
    {
        [TestMethod]
        public async void TestLoginFailsAsExpected()
        {
            // arange
            var oneDriveConnector = new Microsoft.Maker.Storage.OneDrive.OneDriveConnector();

            //act
            try
            {
                await oneDriveConnector.LoginAsync("noClientID", "noClientSecret", "noRedirectUrl", "noAccessCode");
            }
            catch (Exception e)
            {

            }

            //assert
        }
    }
}
