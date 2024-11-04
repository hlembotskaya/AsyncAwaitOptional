using ClientConsoleApp;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new Client();
        await client.StartAsync();
        Console.ReadLine();
    }
}