using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace RaywattApp.Angio
{
    public class TcpClientSingleton
    {
        string serverIP = "127.0.0.1";
        int serverPort = 8888;

        private TcpClient _tcpClient;
        public TcpClient Instance => _tcpClient;

        public bool isConnected = false; // Server - Client Connection
        public bool portConnection = false; // FG Conenction
        public byte[] buffer = new byte[10000000];
        public byte[] tmpBuffer = new byte[20000000];
        public int bytesRead;
        public int tmpBufferLen = 0;
        public byte[] startCommand = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGStarted, 0x07, 0xA3 };
        public byte[] stopCommand = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGStopped, 0x07, 0xA3 };
        public Mat imgAngio = AngioClient.ShowNoSignal();

        public TcpClientSingleton()
        {
            Thread clientThread = new Thread(ConnectToServer);
            clientThread.Start();
        }
        private void ConnectToServer()
        {
            // Angio Server On
            ProcessStartInfo psi = new ProcessStartInfo();
            Process p = new Process();
            psi.FileName = "C:\\Raywatt\\FGServer\\FGServer.exe";
            psi.CreateNoWindow = true;
            p.StartInfo = psi;
            p.Start();

            // Client On
            _tcpClient = new TcpClient(serverIP, serverPort);
            AngioClient angioclient = new AngioClient(this);
            angioclient.ActivateClientThread();
        }
    }
}
