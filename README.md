# FoundationHttpClientDemo
Demo of Windows.Web.Http.HttpClient usage in SignalR UWP Client
Build and use instructions (VS 2017):
1. Edit Common\Constants.cs and fill valid for your LAN server (dev host) ip and port
2. Build Server project only
3. Go to bin and run _CreateAndRegisterDevCerts.bat as admin. This will do following:
	* Generate self-signed CA certificate and server-side certificate using makecert (you will be prompted for private key passwords on this step, feel free to type any or none)
	* Register this certificates in your LocalMachine certificate storage
	* Using netsh bind generated server certificate to the port specified in Constants.cs
	* Copy generated public certificates to Client project
4. At this point you can build Client project
5. Run client project on your PC\Phone\Emulator
6. Click all 3 buttons in top to bottom order
7. You should see "Message from server: ...." in the Visual Studio output console
8. Stop running client
9. Now comment WINDOWS_WEB_HTTP_CLIENT define at the top of MainPage.xaml.cs and uncomment UNSUPPORTED_EXCEPTION_IN_RUNTIME
10. Run application again and no "Message from server: ...." message will be displayed this time, instead you will see PlatfromUnsopportedException, due to X509Certificate2 is not supported in UWP apps.
11. Uncommenting WILL_NOT_COMPILE define will result in uncompilable code due to same reason as above
12. Go to server bin and run _UnregisterDevCerts.bat as admin. This will do following:	
	* Using netsh un-bind generated server certificate from the port specified in Constants.cs
	* Removes generated certificates from LocalMachine certificate storage
	* Deletes generated certificates from disk
