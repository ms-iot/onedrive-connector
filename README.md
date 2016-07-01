# onedrive-connector

This code block demonstrates how to use the OneDrive APIs from a Universal Application and accelerates the process of using OneDrive in other projects.

## Block Specific Setup - OneDrive Authorization
The OneDrive Dev Center can be located at https://dev.onedrive.com/
Follow the instructions on the OneDrive Dev Center to register your application. OneDrive requires registration before it will accept requests from you application, and will provide a Client ID and Secret that need to be presented as part of the authorization process your application users will go through.

At this time, this OneDrive connector only does part of the authentication process. Specifically, it implements steps 2 and 3 of the Token Flow, as described in the OneDrive Dev Center. You will need to present a OneDrive login page to the user, and retrieve an access code. That code can then be passed into the "Login" method of this connector. After logging in, commands for file upload to and deletion from OneDrive become one line calls!

## Usage
To use this block in your project, you need to perform the following steps:

1. Navigate to your git project folder using Command Prompt and run `git submodule add https://github.com/ms-iot/onedrive-connector`
2. Next, run `git submodule update`
3. Open your project solution on Visual Studio and right click on Solution -> Add -> Existing Project. Select OneDriveConnector.
4. Once OneDriveConnector is added to the solution explorer, right click on References on your project -> Add Reference -> Projects -> Solution. Check OneDriveConnector and select OK.

You should now be able to use onedrive-connector objects in your project.

Note: Everytime you clone your project after it's initial creation, you must run the following commands in the project's root folder: 
- `git submodule init`
- `git submodule update`

If the solution is already open in Visual Studio, you may need to reload the solution.

##Constructors
**OneDriveConnector()**: Initializes the HttpClient that will be used for web calls

##Methods
**Login(string clientId, string clientSecret, string redirectUrl, string accessCode)**: Logs in to OneDrive. Specifically, exchanges the accessCode for access and refresh tokens, which are used in future requests.

**UploadFile(StorageFile file, string destinationPath)**: Uploads the given storage file object to the specified location on OneDrive. 

**DeleteFile(string fileName, string pathToFile)**: Deletes the specified file from the specified location on OneDrive.

**Logout()**: Sends a logout message to OneDrive to cancel active tokens, and disposes of all user specific information obtained from OneDrive or provided to the Login method.

===

This project has adopted the [Microsoft Open Source Code of Conduct](http://microsoft.github.io/codeofconduct). For more information see the [Code of Conduct FAQ](http://microsoft.github.io/codeofconduct/faq.md) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments. 
