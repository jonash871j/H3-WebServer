using System.Net;
using WebServerLib;

class Program
{
    static readonly WebServer webServer = new(IPAddress.Parse("127.0.0.1"), 81);

    static void Main()
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        webServer.ServerInfo += OnServerInfo;
        webServer.Content = File.ReadAllText("index.html"); // The data to be shown in the browser
        webServer.Start();

        // Try it out in the browser http://127.0.0.1:81/
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        webServer.Stop();
    }

    private static void OnServerInfo(string message)
    {
        Console.WriteLine($"<{DateTime.Now}> {message}");
    }
}