using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace multipleClients
{
    class Program
    {
        //dichiarazione globale della socket di ascolto e della lista che conterrà tutti i socket di comunicazione con i vari client
        private static IPEndPoint _endPoint = new IPEndPoint(IPAddress.Loopback, 9000);
        private static Socket _serverSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        private static List<Socket> _clientSocketList = new List<Socket>();
        private static byte[] _buffer = new byte[1024];

        static void Main(string[] args)
        {
            //metto in piedi il server e poi attendo con un Console.ReadLine ( dato che tutte le funzioni sono asincrone, se non attendo il programma terminerebbe immediatamente)
            SetupServer();
            Console.WriteLine("Server. Press any key to interrupt communications.");
            Console.ReadLine();
            _serverSocket.Close();//chiudo la socket di ascolto e termino il programma
        }

        private static void SetupServer()
        {
            _serverSocket.Bind(_endPoint);
            _serverSocket.Listen(10);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);//inizio ad accettare il primo client e gestisco l'evento con una callback asincrona
        }
        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket clientSocket = _serverSocket.EndAccept(AR); //come la Accept sincrona, anche la EndAccept mi ritorna la socket di comunicazione con il client

            if (_clientSocketList.Count < 2)
            {
                _clientSocketList.Add(clientSocket);
                //Ora che ho gestito questo client specifico torno in ascolto per i client successivi.
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
                //inizio a ricevere messaggi dal client, fondamentale mandare come object state la socket di comunicazione ( mi servirà all'interno della callback)
                clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), clientSocket);
            }
            else
            {
                string text = "Server is busy, connection will be closed";
                byte[] bufferSend = Encoding.UTF8.GetBytes(text);
                clientSocket.Send(bufferSend);
                clientSocket.Close();
                //Reincomincio ad accettare messaggi in modo che se un client provi a connettersi gli verrà mostrato il messaggio di linea occupata
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState; //l'oggetto che ho passato nella BeginReceive lo trovo dentro AR come AsyncState

            int indexSocket = _clientSocketList.IndexOf(socket); //Ottengo l'indice del client che invia il messaggio
            Socket otherClient;

            //Ottengo il client a cui dovrò inviare il messaggio 
            if (indexSocket == 0)
                otherClient = _clientSocketList[1];
            else
                otherClient = _clientSocketList[0];

            int receivedMsgLng = socket.EndReceive(AR);
            //gestisco l'evento di disconnessione lato Client
            if (receivedMsgLng == 0)
            {
                socket.Close();
            }
            else
            {
                byte[] dataBufRcv = new byte[receivedMsgLng];
                Array.Copy(_buffer, dataBufRcv, receivedMsgLng);

                string receivedText = Encoding.UTF8.GetString(dataBufRcv);
                //string responseText = string.Empty;
                //se il client mi chiede l'orario lo mando altrimenti chiudo la socket di comunicazione(dorvò gestire la cosa anche lato client)
                //if (receivedText == "time")
                //{
                //    responseText = DateTime.Now.ToLongTimeString();
                //    byte[] dataBufSnd = Encoding.UTF8.GetBytes(responseText);
                //    //in questo caso la Send avremmo potuto farla anche in modo sincrono in realtà, perchè appena ricevo mando immediatamente il messaggio di risposta
                //    socket.BeginSend(dataBufSnd, 0, dataBufSnd.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                //    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                //}
                //else if (receivedText == "close")
                //{
                //    socket.Close();
                //}
                //else //Echo server
                //{
                //    byte[] dataBufSnd = Encoding.UTF8.GetBytes(receivedText);
                //    socket.BeginSend(dataBufSnd, 0, dataBufSnd.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
                //    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                //}

                //Invio messaggio al client
                byte[] bufferSend = Encoding.UTF8.GetBytes(receivedText);
                otherClient.BeginSend(bufferSend, 0, bufferSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), otherClient);
                otherClient.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), otherClient);
            }
        }
        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }
    }
}
