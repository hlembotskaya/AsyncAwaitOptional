using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsoleApp
{
    public class Client
    {
        List<string> listOfMessages = new List<string>() { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
        Random randomDelay = new Random();
        Random randomMessage = new Random();

        public async Task StartAsync()
        {
            using (var connection = new NamedPipeClientStream(".", "chat_pipe", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                Console.WriteLine("Connecting to server...");
                await connection.ConnectAsync();
                Console.WriteLine("Connected to server!");

                try
                {
                    var write = WriteAsync(connection);
                    var read = ReadAsync(connection);

                    await Task.WhenAll(write, read);
                }
                catch {
                    Console.WriteLine("Connection is lost");
                }
            }
        }


        public async Task ReadAsync(NamedPipeClientStream connection)
        {
            using (var reader = new StreamReader(connection, leaveOpen: true))
            {
                string? serverMessage;

                while (connection.IsConnected)
                {
                    serverMessage = await reader.ReadLineAsync();
                    if (serverMessage != null)
                    {
                        Console.WriteLine($"From server: {serverMessage}");
                    }
                    else
                    {
                        throw new Exception("Connection is lost");
                    }
                }
            }
        }

        public async Task WriteAsync(NamedPipeClientStream connection)
        {
            using (var writer = new StreamWriter(connection, leaveOpen: true) { AutoFlush = true })
            {
                var clientName = Guid.NewGuid().ToString();
                Console.WriteLine($"{clientName}");
                await writer.WriteLineAsync(clientName);

                for (int i = 0; i < listOfMessages.Count; i++)
                {
                    if (connection.IsConnected)
                    {
                        Console.WriteLine($"Send to server: {listOfMessages[i]}");
                        await writer.WriteLineAsync(listOfMessages[i]);
                        await Task.Delay(2000);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
