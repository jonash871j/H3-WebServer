using System.Net;
using WebServerLib;

class Program
{
    static readonly WebServer webServer = new(IPAddress.Parse("127.0.0.1"), 81);

    static void Main()
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        webServer.ServerInfo += OnServerInfo;
        webServer.Start();
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        webServer.Stop();
        Thread.Sleep(500);
    }

    private static void OnServerInfo(string message)
    {
        Console.WriteLine($"<{DateTime.Now}> {message}");
    }
}