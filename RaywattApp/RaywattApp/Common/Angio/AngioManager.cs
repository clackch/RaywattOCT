using OpenCvSharp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using RaywattApp.Common.Bases;
using System;
using System.Runtime.InteropServices;
using System.Printing.IndexedProperties;

namespace RaywattApp.Common.Angio
{
    public enum PacketType
    {
        Image,
        Command,
        Nothing
    };

    public enum CommandType
    {
        FGConnected, // Port
        FGDisconnected, // Port
        FGStarted,
        FGStopped,
        FGAskPort,
        FGAskBoard,
        FGBoardExist,
        FGBoardNotExist,
        FGNothing,
    };

    public class AngioManager
    {
        private string serverIP;
        private int serverPort;

        private TcpClient _tcpClient;
        public TcpClient Instance => _tcpClient;

        public Mat imgAngio;

        public bool isConnected; // Server - Client Connection
        public bool portConnection; // FG Conenction
        public bool boardConnection; // FG Board Connection

        public byte[] buffer;
        public byte[] tmpBuffer;

        public int bytesRead;
        public int tmpBufferLen;

        public byte[] commandBuffer = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGStarted, 0x00, 0xA3 };

        public Thread threadFuncLiveAngioImage;
        public bool threadOnLiveAngioImage;

        public AngioManager()
        {
            serverIP = "127.0.0.1";
            serverPort = 8888;

            imgAngio = ShowNoSignal();

            isConnected = false;

            portConnection = false;
            boardConnection = false;

            buffer = new byte[10000000];
            tmpBuffer = new byte[20000000];
            Array.Fill<byte>(tmpBuffer, 0);
            Array.Fill<byte>(tmpBuffer, 0);

            tmpBufferLen = 0;

            ConnectToServer();
        }
        private void ConnectToServer()
        {
            // Angio Server On
            Process[] processes = Process.GetProcessesByName("FGServer");
            if (processes.Length == 0)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                Process p = new Process();
                // psi.FileName = Constants.FGFolderPath + "\\FGServer.exe";
                psi.FileName = "C:\\github\\Sejong\\FrameGrabber\\FGServer\\x64\\Release\\FGServer.exe";
            
                psi.CreateNoWindow = true;
                p.StartInfo = psi;
                p.Start();
            }

            // Client On
            _tcpClient = new TcpClient(serverIP, serverPort);

            if (Instance.Connected == true)
                isConnected = true;

            ActivateClientThread();
            AskBoardConnection();
            AskPortConnection();
        }
        public void AskPortConnection()
        {
            SetCommandPacket(CommandType.FGAskPort);
            Instance.GetStream().Write(commandBuffer, 0, commandBuffer.Length);
        }

        public void AskBoardConnection()
        {
            SetCommandPacket(CommandType.FGAskBoard);
            Instance.GetStream().Write(commandBuffer, 0, commandBuffer.Length);
        }

        public void ThreadFuncLiveAngioImage()
        {
            while (threadOnLiveAngioImage)
            {
                ReadPacket();
            }
        }

        public void ActivateClientThread()
        {
            threadOnLiveAngioImage = true;
            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            threadFuncLiveAngioImage.Start();
        }

        public void CloseLiveAngioImageThread()
        {
            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
        }

        private bool ReadPacket()
        {
            try
            {
                bytesRead = Instance.GetStream().Read(buffer, 0, 10000000);

                Array.Copy(buffer, 0, tmpBuffer, tmpBufferLen, bytesRead);
                tmpBufferLen += bytesRead;

                while (true)
                {
                    PacketType type = CheckPacketType(tmpBuffer);
                    if (type == PacketType.Command)
                        CommandPacketProcess();
                    else if (type == PacketType.Image)
                        ImagePacketProcess();
                    else if (type == PacketType.Nothing)
                        break;
                }
            }
            catch (Exception ex)
            {
                //_angioManager.isConnected = false;
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private void ImagePacketProcess()
        {
            int offset = 2;
            short height = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            short width = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            char BitsPerPixel = (char)tmpBuffer[offset++];
            int imageSize = height * width * BitsPerPixel / 8;
            if (tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 2] == CalcCheckSum(tmpBuffer, offset + imageSize)
                && tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(height, width, MatType.CV_8UC(BitsPerPixel / 8));
                Marshal.Copy(tmpBuffer, offset, image.Data, imageSize);
                offset += imageSize;
                char checksum = BitConverter.ToChar(tmpBuffer, offset++);
                char eof = BitConverter.ToChar(tmpBuffer, offset++);

                Array.Copy(tmpBuffer, imageSize + Constants.imageHeaderSize + Constants.imageTailSize, tmpBuffer, 0, 20000000 - imageSize - Constants.imageHeaderSize - Constants.imageTailSize);
                tmpBufferLen -= imageSize + Constants.imageHeaderSize + Constants.imageTailSize;

                Cv2.Flip(image, image, 0);

                int t = 500, l = 1000, b = 1000, r = 1900;
                Rect roi = new Rect(l, t, r - l, b - t);
                image = image.SubMat(roi);

                imgAngio = image;
            }
        }

        private void CommandPacketProcess()
        {
            int offset = 2;
            int command = tmpBuffer[offset++];
            char checksum = BitConverter.ToChar(tmpBuffer, offset++);
            char eof = BitConverter.ToChar(tmpBuffer, offset++);

            if ((byte)checksum == CalcCheckSum(tmpBuffer, 3))
            {
                if (command == (int)CommandType.FGDisconnected)
                {
                    portConnection = false;
                    imgAngio = ShowNoSignal();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = portConnection;
                    });
                }
                else if (command == (int)CommandType.FGConnected)
                {
                    portConnection = true;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = portConnection;
                    });
                }
                else if (command == (int)CommandType.FGBoardExist)
                {
                    boardConnection = true;
                    threadOnLiveAngioImage = false;
                }
                else if (command == (int)CommandType.FGBoardNotExist)
                {
                    boardConnection = false;
                }

                Array.Copy(tmpBuffer, Constants.commandPacketSize, tmpBuffer, 0, 20000000 - Constants.commandPacketSize);
                tmpBufferLen -= Constants.commandPacketSize;
            }
        }

        private PacketType CheckPacketType(byte[] tmpBuffer)
        {
            int offset = 0;
            if (tmpBuffer[offset++] == 0x3A)
            {
                switch (tmpBuffer[offset++])
                {
                    case (byte)PacketType.Command:
                        if (tmpBuffer[4] == 0xA3)
                        {
                            return PacketType.Command;
                        }
                        break;

                    case (byte)PacketType.Image:
                        short height = BitConverter.ToInt16(tmpBuffer, offset);
                        offset += sizeof(short);
                        short width = BitConverter.ToInt16(tmpBuffer, offset);
                        offset += sizeof(short);
                        char BitsPerPixel = (char)tmpBuffer[offset++];
                        int imageSize = height * width * BitsPerPixel / 8;

                        if (tmpBuffer[Constants.imageHeaderSize + imageSize + Constants.imageTailSize - 1] == 0xA3)
                        {
                            return (int)PacketType.Image;
                        }
                        break;

                    case (byte)PacketType.Nothing:
                        break;
                }
            }
            return PacketType.Nothing;
        }
        public Mat ShowNoSignal()
        {
            Mat image = new Mat(1080, 1920, MatType.CV_8UC3);
            image.SetTo(new Scalar(0, 0, 0));

            Scalar textColor = new Scalar(0, 0, 255);
            HersheyFonts fontFace = HersheyFonts.HersheyComplex;
            double fontScale = 5.0;
            int thickness = 5;

            Point textPosition = new Point(500, 500);
            Cv2.PutText(image, "No Signal", textPosition, fontFace, fontScale, textColor, thickness);

            return image;
        }
        byte CalcCheckSum(byte[] buffer, int size)
        {
            size--;
            byte csum = 0;
            for (; size >= 0; size--)
            {
                csum += buffer[size];
            }
            return (byte)~csum;
        }

        public void SetCommandPacket(CommandType commandType)
        {
            commandBuffer[2] = (byte)commandType;
            byte checksum = CalcCheckSum(commandBuffer, 3);
            commandBuffer[3] = checksum;
        }
    }
}
