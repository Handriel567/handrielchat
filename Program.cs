using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketBroadcast
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, WebSocket> ConnectedClients = new ConcurrentDictionary<string, WebSocket>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting WebSocket server...");

            string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{port}/");
            httpListener.Start();
            Console.WriteLine($"Server started on ws://localhost:{port}/");

            while (true)
            {
                // Aceptar nuevas conexiones
                HttpListenerContext context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    ProcessClient(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private static async void ProcessClient(HttpListenerContext context)
        {
            string clientId = Guid.NewGuid().ToString();
            Console.WriteLine($"Client {clientId} connected.");

            WebSocket webSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
            ConnectedClients.TryAdd(clientId, webSocket);

            try
            {
                // Leer mensajes enviados por el cliente
                await HandleClientMessages(clientId, webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientId}: {ex.Message}");
            }
            finally
            {
                ConnectedClients.TryRemove(clientId, out _);
                Console.WriteLine($"Client {clientId} disconnected.");
                if (webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                }
                webSocket.Dispose();
            }
        }

        private static async Task HandleClientMessages(string clientId, WebSocket webSocket)
        {
            byte[] buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received from {clientId}: {message}");

                // Broadcast del mensaje a todos los clientes conectados
                await BroadcastMessageAsync(clientId, message);
            }
        }

        private static async Task BroadcastMessageAsync(string senderId, string message)
        {
            string broadcastMessage = $"Client {senderId}: {message}";
            byte[] messageBytes = Encoding.UTF8.GetBytes(broadcastMessage);

            foreach (var client in ConnectedClients)
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await client.Value.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
