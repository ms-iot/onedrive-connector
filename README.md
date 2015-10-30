# onedrive-connector

This repository contains the code block for a OneDrive helper. This demonstrates how to use the OneDrive APIs from a Universal Application that can run on Windows 10, including IoT Core, and accelerates the process of using OneDrive in other projects.

## OneDrive Authorization
The OneDrive Dev Center can be located at https://dev.onedrive.com/
Follow the instructions on the OneDrive Dev Center to register your application. OneDrive requires registration before it will accept requests from you application, and will provide a Client ID and Secret that need presented as part of the authorization process your application users will go through.

At this time, this OneDrive connector only does part of the authentication process. Specifically, it implements steps 2 and 3 of the Token Flow, as described in the OneDrive Dev Center. You will need to present a OneDrive login page to the user, and retrieve an access code. That code can then be passed into the "Login" method of this connector. After logging in, commands for file upload to and deletion from OneDrive become one line calls!

## Usage
To use this block in your project, you need to perform the following steps:

1. Navigate to your git project folder using Command Prompt and run `git submodule add https://github.com/ms-iot/onedrive-connector`
2. Next, run `git submodule update`
3. Open your project solution on Visual Studio and right click on Solution -> Add -> Existing Project. Select OneDriveConnector.
4. Once OneDriveConnector is added to the solution explorer, right click on References on your project -> Add Reference -> Projects -> Solution. Check OneDriveConnector and select OK.
5. You should now be able to use onedrive-connector objects in your project.

Note: Everytime you clone your project after it's initial creation, you must run the following commands in the project's root folder: 
- `git submodule init`
- `git submodule update`

If the solution is already open in Visual Studio, you may need to reload the solution.
