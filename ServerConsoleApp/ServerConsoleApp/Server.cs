using System.Collections.Concurrent;
using System.IO.Pipes;

namespace ServerConsoleApp
{
    public class Server
    {
        public ConcurrentDictionary<string, NamedPipeServerStream> ClientsList = new ConcurrentDictionary<string, NamedPipeServerStream>();
        private ConcurrentQueue<string> historyQueue = new ConcurrentQueue<string>();
        private int historyLength = 10;

        public async Task StartAsync()
        {
            while (true)
            {
                var connection = new NamedPipeServerStream("chat_pipe", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                
                Console.WriteLine("Waiting for a client to connect...");

                await connection.WaitForConnectionAsync();

                string clientName;
                try
                {
                    clientName = await GetClientNameAsync(connection);
                    
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine($"{ex.Message}");
                    connection.Dispose();

                    break;
                }

                Console.WriteLine($"Client {clientName} connected!");
                ClientsList[clientName] = connection;

                var x = HandClientAsync(clientName, connection);
            }
        }

        private async Task<string> GetClientNameAsync(NamedPipeServerStream connection)
        {
            if (connection.IsConnected)
            {
                using (var reader = new StreamReader(connection, leaveOpen: true))
                {
                    var result = await reader.ReadLineAsync();

                    if (String.IsNullOrEmpty(result))
                    {
                        throw new Exception("Name can't be empty");
                    }

                    return result;
                }
            }
            else
            { 
                throw new Exception("Client is not connected");
            }
        }

        private async Task HandClientAsync(string clienName, NamedPipeServerStream connection)
        {
            try 
            {
                var read = ReadClientAsync(clienName, connection);
                var write = WriteClientAsync(clienName, connection);

                await Task.WhenAll(read, write);
            }
            catch (Exception e) 
            { 
                Console.WriteLine($"{e.Message}");
            }
            finally 
            {
                NamedPipeServerStream? namedPipeServerStream;
                ClientsList.TryRemove(clienName, out namedPipeServerStream);
                
                connection.Dispose();
            }

            
        }

        private async Task ReadClientAsync(string clienName, NamedPipeServerStream connection)
        {
            using (var reader = new StreamReader(connection, leaveOpen: true))
            {
                while (connection.IsConnected)
                {
                    var message = await reader.ReadLineAsync();

                    if (!String.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"{clienName}: {message}");
                        AddToHistory($"{clienName}: {message}");
                        await BroadcastMessageAsync($"{clienName}: {message}");
                    }
                    else
                    {
                        throw new Exception("Connection is lost");
                    }
                }
            }
        }

        private async Task WriteClientAsync(string clienName, NamedPipeServerStream connection)
        {
             await SendHistory(connection);
        }

        private async Task SendHistory(NamedPipeServerStream connection)
        {
            using (var writer = new StreamWriter(connection, leaveOpen: true))
            {
                foreach (var item in historyQueue)
                {
                    await writer.WriteLineAsync(item);
                }
                await writer.FlushAsync();
            }
        }

        private async Task BroadcastMessageAsync(string message)
        {
            var broadcastTasks = ClientsList.Select(item => Task.Run(async () =>
            {
                if (item.Value.IsConnected)
                {
                    using (var writer = new StreamWriter(item.Value, leaveOpen: true) { AutoFlush = true })
                    {
                        await writer.WriteLineAsync(message);
                    }
                }
            }));

            await Task.WhenAll(broadcastTasks);
        }

        private void AddToHistory(string message)
        {
            if (historyQueue.Count >= historyLength)
            {
                string x;
                historyQueue.TryDequeue(out x);
            }

            historyQueue.Enqueue(message);
        }
    }
}
