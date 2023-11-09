using OpenCvSharp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using RaywattApp.Common.Bases;
using System;
using System.Runtime.InteropServices;
using System.IO;
using log4net;

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
        FGUnknown,
        FGStarted,
        FGStopped,
        FGAskPort,
        FGAskBoard,
        FGAskDeviceInfo,
        FGAngioConnected, // Port
        FGAngioDisconnected, // Port
        FGBoardExist,
        FGBoardNotExist,
        FGDeviceInfo,
        FGNothing,
    };

    public class AngioManager
    {
        private string serverIP;
        private int serverPort;

        private TcpClient _tcpClient;
        private TcpClient Instance => _tcpClient;

        private Mat imgAngio;
        public Mat ImgAngio {  get { return imgAngio; } }

        private bool serverConnection; // Server - Client Connection
        private CommandType boardConnection; // FG Board Connection
        private CommandType portConnection; // FG Port Connection

        private byte[] buffer;
        private byte[] tmpBuffer;
        private byte[] angioSaveBuffer;

        private int bytesRead;
        private int tmpBufferLen;
        private int angioSaveFrameNum;
        private int angioSaveFrameTotalNum;

        private byte[] commandBuffer = { 0x3A, (byte)PacketType.Command, (byte)CommandType.FGUnknown, 0x00, 0xA3 };

        private Thread threadFuncLiveAngioImage;
        private bool threadOnLiveAngioImage;

        private Thread threadFuncSaveAngioFrames;
        private bool threadOnSaveAngioFrames;

        private Thread threadFuncBufferRealloc;

        private string angioFramesPath;
        private short angioFrameWidth;
        private short angioFrameHeight;
        private char angioBitsPerPixel;
        private int angioImageSize;

        public bool readyToRecv;

        public AngioManager()
        {
            serverIP = "127.0.0.1";
            serverPort = 8888;

            imgAngio = ShowNoSignal();

            serverConnection = false;

            boardConnection = CommandType.FGUnknown;
            portConnection = CommandType.FGUnknown;
            readyToRecv = false;

            buffer = new byte[100];
            tmpBuffer = new byte[200];
            angioSaveBuffer = new byte[10];

            Array.Fill<byte>(buffer, 0);
            Array.Fill<byte>(tmpBuffer, 0);
            Array.Fill<byte>(angioSaveBuffer, 0);

            tmpBufferLen = 0;
            angioSaveFrameNum = 0;
            angioSaveFrameTotalNum = 0;

            threadOnLiveAngioImage = false;
            threadOnSaveAngioFrames = false;

            angioFrameHeight = -1;
            angioFrameWidth = -1;
            

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
                psi.FileName = Constants.FGFolderPath + "\\FGServer.exe";
                
                psi.CreateNoWindow = true;
                p.StartInfo = psi;
                p.Start();
            }

            // Client On
            _tcpClient = new TcpClient(serverIP, serverPort);

            if (Instance.Connected == true)
                serverConnection = true;
            
            int read = 0;
            while(read != 0){
                read = _tcpClient.GetStream().Read(buffer, 0, buffer.Length);
            }
            Array.Fill<byte>(buffer, 0);

            ActivateClientThreads();
            AskBoardConnection();
            AskAngioConnection();
            AskDeviceInfo();
        }

        private void AskAngioConnection()
        {
            SendCommandPacket(CommandType.FGAskPort);
        }

        private void AskBoardConnection()
        {
            SendCommandPacket(CommandType.FGAskBoard);
        }
        private void AskDeviceInfo()
        {
            SendCommandPacket(CommandType.FGAskDeviceInfo);
        }

        private void ThreadFuncBufferRealloc()
        {
            while(true)
            {
                if(boardConnection != CommandType.FGUnknown && portConnection != CommandType.FGUnknown && angioFrameHeight != -1 && angioFrameWidth != -1)
                {
                    buffer = new byte[angioImageSize * 5];
                    tmpBuffer = new byte[angioImageSize * 10];
                    angioSaveBuffer = new byte[angioImageSize * 100];

                    Array.Fill<byte>(buffer, 0);
                    Array.Fill<byte>(tmpBuffer, 0);
                    Array.Fill<byte>(angioSaveBuffer, 0);

                    break;
                }
            }
        }

        private void ThreadFuncLiveAngioImage()
        {
            while (threadOnLiveAngioImage)
            {
                ReadPacket();
            }
        }

        private void ThreadFuncSaveAngioFrames(string angioFilePath)
        {
            try
            {
                FileStream fs = new FileStream(angioFilePath, FileMode.Create, FileAccess.Write);
                
                while (threadOnSaveAngioFrames)
                {
                    if (angioSaveFrameTotalNum > angioSaveFrameNum)
                    {
                        fs.Write(angioSaveBuffer, angioSaveFrameNum * angioImageSize, angioImageSize);
                        angioSaveFrameNum++;
                    }
                }
                fs.Close();
            }catch (Exception ex)
            {
                Debug.WriteLine("File Creation Error: " + ex.Message);
            }
        }

        private void ActivateClientThreads()
        {
            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            StartLiveAngioThread();

            threadFuncBufferRealloc = new Thread(() => ThreadFuncBufferRealloc());
            threadFuncBufferRealloc.Start();
            
        }

        public void CloseAngioManager()
        {
            Instance.GetStream().Close();

            if (threadFuncLiveAngioImage != null && threadFuncLiveAngioImage.IsAlive)
                StopLiveAngioThread();
            if (threadFuncSaveAngioFrames != null && threadFuncSaveAngioFrames.IsAlive)
                StopSaveAngioThread();
            if (threadFuncBufferRealloc != null && threadFuncBufferRealloc.IsAlive)
                threadFuncBufferRealloc.Join();
        }

        private bool ReadPacket()
        {
            try
            {
                bytesRead = Instance.GetStream().Read(buffer, 0, buffer.Length);

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
                Debug.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private void ImagePacketProcess()
        {
            int offset = 2;
            angioFrameHeight = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            angioFrameWidth = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            angioBitsPerPixel = (char)tmpBuffer[offset++];
            angioImageSize = angioFrameHeight * angioFrameWidth * angioBitsPerPixel / 8;
            if (tmpBuffer[angioImageSize + Constants.imageHeaderSize + Constants.imageTailSize - 2] == CalcCheckSum(tmpBuffer, offset + angioImageSize)
                && tmpBuffer[angioImageSize + Constants.imageHeaderSize + Constants.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(angioFrameHeight, angioFrameWidth, MatType.CV_8UC(angioBitsPerPixel / 8));
                Marshal.Copy(tmpBuffer, offset, image.Data, angioImageSize);
                offset += angioImageSize;
                
                char checksum = BitConverter.ToChar(tmpBuffer, offset++);
                char eof = BitConverter.ToChar(tmpBuffer, offset++);

                Array.Copy(tmpBuffer, angioImageSize + Constants.imageHeaderSize + Constants.imageTailSize, tmpBuffer, 0, tmpBuffer.Length - angioImageSize - Constants.imageHeaderSize - Constants.imageTailSize);
                tmpBufferLen -= angioImageSize + Constants.imageHeaderSize + Constants.imageTailSize;

                Cv2.Flip(image, image, 0);

                if (threadOnSaveAngioFrames)
                {
                    Marshal.Copy(image.Data, angioSaveBuffer, angioSaveFrameTotalNum * angioImageSize, angioImageSize);
                    angioSaveFrameTotalNum++;
                }

                int t = 0, l = 0, b = angioFrameHeight/2, r = angioFrameWidth/2;
                Rect roi = new Rect(l, t, r - l, b - t);
                image = image.SubMat(roi);

                imgAngio = image;
            }
        }

        private void CommandPacketProcess()
        {
            int offset = 2;
            byte command = tmpBuffer[offset++];
            char checksum = (char)tmpBuffer[offset++];
            char eof = (char)tmpBuffer[offset++];

            if(command == (byte)CommandType.FGDeviceInfo)
            {
                checksum = (char)tmpBuffer[Constants.deviceInfoPacketSize - 2];
                if ((byte)checksum == CalcCheckSum(tmpBuffer, Constants.deviceInfoPacketSize - 2))
                {
                    DeviceInfoPacketProcess();

                    Array.Copy(tmpBuffer, Constants.deviceInfoPacketSize, tmpBuffer, 0, tmpBuffer.Length - Constants.deviceInfoPacketSize);
                    tmpBufferLen -= Constants.deviceInfoPacketSize;
                }
            }

            else if ((byte)checksum == CalcCheckSum(tmpBuffer, 3))
            {
                if (command == (byte)CommandType.FGAngioDisconnected)
                {
                    imgAngio = ShowNoSignal();
                    if (readyToRecv)
                    {
                        SendCommandPacket(CommandType.FGStopped);
                        readyToRecv = false;
                    }
                    portConnection = CommandType.FGAngioDisconnected;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = false;
                    });
                }
                else if (command == (byte)CommandType.FGAngioConnected)
                {
                    if (readyToRecv)
                    {
                        SendCommandPacket(CommandType.FGStarted);
                    }
                    portConnection = CommandType.FGAngioConnected;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = true;
                    });
                }
                else if (command == (byte)CommandType.FGBoardExist)
                {
                    boardConnection = CommandType.FGBoardExist;
                }
                else if (command == (byte)CommandType.FGBoardNotExist)
                {
                    boardConnection = (CommandType)CommandType.FGBoardNotExist;
                    threadOnLiveAngioImage = false;
                }

                Array.Copy(tmpBuffer, Constants.commandPacketSize, tmpBuffer, 0, tmpBuffer.Length - Constants.commandPacketSize);
                tmpBufferLen -= Constants.commandPacketSize;
            }
        }

        private void DeviceInfoPacketProcess()
        {
            int offset = 3;
            angioFrameHeight = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            angioFrameWidth = BitConverter.ToInt16(tmpBuffer, offset);
            offset += sizeof(short);
            angioBitsPerPixel = (char)tmpBuffer[offset++];

            angioImageSize = angioFrameHeight * angioFrameWidth * angioBitsPerPixel / 8;
        }

        private PacketType CheckPacketType(byte[] tmpBuffer)
        {
            int offset = 0;
            if (tmpBuffer[offset++] == 0x3A)
            {
                switch (tmpBuffer[offset++])
                {
                    case (byte)PacketType.Command:
                        if (tmpBuffer[Constants.commandPacketSize-1] == 0xA3)
                        {
                            return PacketType.Command;
                        }else if (tmpBuffer[Constants.deviceInfoPacketSize-1] == 0xA3)
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
                            return PacketType.Image;
                        }
                        break;

                    case (byte)PacketType.Nothing:
                        break;
                }
            }
            return PacketType.Nothing;
        }

        private Mat ShowNoSignal()
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

        private byte CalcCheckSum(byte[] buffer, int size)
        {
            size--;
            byte csum = 0;
            for (; size >= 0; size--)
            {
                csum += buffer[size];
            }
            return (byte)~csum;
        }

        public void SendCommandPacket(CommandType commandType)
        {
            commandBuffer[2] = (byte)commandType;
            byte checksum = CalcCheckSum(commandBuffer, 3);
            commandBuffer[3] = checksum;

            Instance.GetStream().Write(commandBuffer, 0, commandBuffer.Length);
        }

        public void StartLiveAngioThread()
        {
            threadOnLiveAngioImage = true;
            threadFuncLiveAngioImage.Start();
        }
        public void StopLiveAngioThread()
        {
            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
        }
        public void StartSaveAngioThread(string angioFilePath)
        {
            threadFuncSaveAngioFrames = new Thread(() => ThreadFuncSaveAngioFrames(angioFilePath));
            threadOnSaveAngioFrames = true;
            threadFuncSaveAngioFrames.Start();
        }
        public void StopSaveAngioThread()
        {
            threadOnSaveAngioFrames = false;
            threadFuncSaveAngioFrames.Join();
        }
    }
}
