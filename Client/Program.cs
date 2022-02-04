using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static IPEndPoint _server = new(IPAddress.Loopback, 9000);
        static IPEndPoint _client = new(IPAddress.Loopback, 0);
        static Socket _clientSocket = new(_client.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        static byte[] _bufferReceive = new byte[1024];
        
        static void Main(string[] args)
        {
            Console.WriteLine("Client");
            LoopConnect();
            SendLoop();
        }

        private static void LoopConnect()
        {
            int attempts = 0;
            while(!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    _clientSocket.Connect(_server);
                }
                catch(SocketException)
                {
                    Console.Clear();
                    Console.WriteLine("Connection attempts:" + attempts);
                }
            }
        }

        private static void SendLoop()
        {
            //Ricezione Asincrona
            _clientSocket.BeginReceive(_bufferReceive, 0, _bufferReceive.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), _clientSocket); //mi metto in attesa del primo pacchetto

            while(true)
            {
                byte[] sendBuffer = new byte[1024];

                Console.Write("Message to send: ");
                string strMsg = Console.ReadLine();

                sendBuffer = System.Text.Encoding.UTF8.GetBytes(strMsg);
                _clientSocket.Send(sendBuffer);
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            //End Receive
            int receivedMsgLng = socket.EndReceive(AR);
            //gestisco il pacchetto ricevuto
            if(receivedMsgLng == 0)
            {
                socket.Close();
            }
            else
            {
                byte[] dataBufRcv = new byte[receivedMsgLng];
                Array.Copy(_bufferReceive, dataBufRcv, receivedMsgLng);
                string receivedText = Encoding.UTF8.GetString(dataBufRcv);
                Console.WriteLine($"Messaggio ricevuto dal server: {receivedText}");

                //Messaggio di errore inviato dal server in caso la comunicazione sia occupata
                if(receivedText == "Server is busy, connection will be closed")
                {
                    Console.WriteLine("Chiusura della connessione.");
                    socket.Close();
                    _clientSocket.Close();
                    Environment.Exit(0);
                }
            }
            //begin receive() //Call back ricorsiva
            _clientSocket.BeginReceive(_bufferReceive, 0, _bufferReceive.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), _clientSocket); //mi metto in attesa del primo pacchetto
        }
    }
}
