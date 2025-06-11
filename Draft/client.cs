using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class client
{
    private static UdpClient UdpClient;                 // UDP client for communication with server
    private static IPEndPoint serverEndPoint;           // Server's endpoint (IP and port)
    private static bool isRunning = true;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Poker Client ===");
        
        // Setup connection with server
        Console.WriteLine("Enter server's IP: ");
        string serverIp = Console.ReadLine();
        Console.WriteLine("Enter server's port number: ");
        int serverPort = int.Parse(Console.ReadLine());

        // Initialize UDP client and server endpoint
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        // Get player name
        // TODO: Fetch data from database/client
        Console.Write("Enter player name: ")
        string playerName = Console.ReadLine();

        // Notify server
        SendMessage($"join {playerName}");

        // Start a separate thread to listen for messages from server
        Thread listenerThread = new Thread(ListenForMessages);
        listenerThread.Start();

        while (isRunning)
        {
            // Accept user command
            // TODO: Integrate into Unity
            Console.WriteLine("Enter a command (bet, fold, check, allin, exit)");
            string command = Console.ReadLine();

            if (command.ToLower() == "exit")
            {
                isRunning = false;
                udpClient.Close();
                break;
            }

            // TODO: Check whether command comply to game rules
            SendMessage($"{command} {playerName}");
        }

        Console.WriteLine("Client shutting down.");
    }

    private static void ListenForMessages()
    {
        while (isRunning)
        {
            try
            {
                // Receive message from server
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(receivedData);
                Console.WriteLine($"Server: {message}"); // Display message
            }
            catch (SocketException)
            {
                Console.WriteLine("Error receiving message: SocketException.");
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving message: {e.Message}");
            }
        }
    }

    private static void SendMessage(string message)
    {
        // Send message to server
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, serverEndPoint);
    }
}