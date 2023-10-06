using OpenCvSharp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using RaywattApp.Common.Bases;

namespace RaywattApp.Common.Angio
{
    public class AngioManager
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

        public AngioManager()
        {
            ConnectToServer();
        }
        private void ConnectToServer()
        {
            // Angio Server On
            Process[] processes = Process.GetProcessesByName("FGServer");
            if(processes.Length == 0)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                Process p = new Process();
                psi.FileName = Constants.FGFolderPath + "\\FGServer.exe";
                psi.CreateNoWindow = true;
                p.StartInfo = psi;
                p.Start();
            }

            // Client On
            _tcpClient = new TcpClient(serverIP, serverPort);
            AngioClient angioclient = new AngioClient(this);
            angioclient.ActivateClientThread();
        }
    }
}
