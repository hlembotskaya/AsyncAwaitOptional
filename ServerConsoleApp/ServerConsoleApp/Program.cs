using ServerConsoleApp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new Server();

        await server.StartAsync();
    }
}