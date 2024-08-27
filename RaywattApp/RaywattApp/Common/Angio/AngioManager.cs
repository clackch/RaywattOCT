using OpenCvSharp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using RaywattApp.Common.Bases;
using System;
using System.Runtime.InteropServices;
using System.IO;
using log4net;
using System.Text;
using System.Collections.Generic;
using RaywattApp.Models;
using RaywattApp.Common.Dialog;
using RaywattApp.Views.Dialog;
using System.Xml;
using System.Threading.Tasks;
using RaywattApp.Common.Localization;
using RaywattApp.Common.Util;
using System.Linq;

namespace RaywattApp.Common.Angio
{
    public enum PacketType
    {
        Image,
        Command,
        Nothing,
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
        FGChpFile,
        FGSuccessChangeChp,
        FGFailChangeChp,
        FGNothing,
    };

    public enum ConnectionStatus
    {
        Default,
        Success,
        BoardFailure,
    }

    public class AngioManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AngioManager));

        protected readonly DynamicResource _l10n;

        private IDialogService? _dialogService;

        private TcpClient _tcpClient;

        private Mat imgAngio;
        public Mat ImgAngio { get { return imgAngio; } set { imgAngio = value; }  }

        private bool isBoardInited = false;
        private bool boardConnection = false;

        private byte[] buffer;
        private byte[] tmpBuffer;
        private List<byte[]> angioSaveBuffer;
        public List<byte[]> AngioSaveBuffer { get { return angioSaveBuffer; } set { angioSaveBuffer = value; } }

        private int bytesRead;
        private int tmpBufferLen;
        private int angioSaveFrameNum;
        public int AngioSaveFrameNum { get { return angioSaveFrameNum; } set { angioSaveFrameNum = value; } }

        private byte[] commandBuffer = { Constants.SOF, (byte)PacketType.Command, (byte)CommandType.FGUnknown, 0x00, Constants.EOF };

        private Thread threadFuncLiveAngioImage;
        private bool threadOnLiveAngioImage;

        private Thread threadFuncSaveAngioFrames;
        private bool threadOnSaveAngioFrames;

        private short angioFrameWidth;
        public short AngioFrameWidth { get { return angioFrameWidth; } set { angioFrameWidth = value; } }
        private short angioFrameHeight;
        public short AngioFrameHeight { get { return angioFrameHeight; } set { angioFrameHeight = value; } }
        private char angioBitsPerPixel;
        public char AngioBitsPerPixel { get { return angioBitsPerPixel; } set { angioBitsPerPixel = value; } }
        private int angioImageSize;

        private bool readyToRecv = false;
        public bool ReadyToRecv { get { return readyToRecv; } set { readyToRecv = value; } }

        private short isChpFileChangeSuccess = 0;
        public short IsChpFileChangeSuccess { get { return isChpFileChangeSuccess; } set { isChpFileChangeSuccess = value; } }

        private bool isCathRoomDialogOpen = false;

        public AngioManager(IDialogService dialogService)
        {
            _log.Debug("AngioManager");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];
            _dialogService = dialogService;

            imgAngio = ShowNoSignal();

            buffer = new byte[256];
            tmpBuffer = new byte[512];
            angioSaveBuffer = new List<byte[]>();

            Array.Fill<byte>(buffer, 0);
            Array.Fill<byte>(tmpBuffer, 0);

            tmpBufferLen = 0;
            angioSaveFrameNum = 0;

            threadOnLiveAngioImage = false;
            threadOnSaveAngioFrames = false;
        }

        private void StartFGServerProc(ProcessStartInfo startInfo)
        {
            Process p = new Process();
            startInfo.CreateNoWindow = true;
            p.StartInfo = startInfo;
            p.Start();
        }

        public ConnectionStatus ConnectToServer()
        {
            Process[] processes;
            ProcessStartInfo psi = new ProcessStartInfo();
            string processName = CommonUtil.IsTestMode(ViewModelBase._deviceStatus.TestMode, "FG") ? "FGServerTestStub" : "FGServer";
            processes = Process.GetProcessesByName(processName);
            psi.FileName = Constants.FGFolderPath + "\\" + processName + ".exe";

            if (processes.Length == 0)
            {
                StartFGServerProc(psi);
            }
            else
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                StartFGServerProc(psi);
            }

            _tcpClient = new TcpClient(Constants.ServerIP, Constants.ServerPort);

            ActivateClientThreads();
            bool init = InitAngioBoard();
            if (init)
            {
                return ConnectionStatus.Success;
            }
            else
            {
                if (!CommonUtil.IsTestMode(ViewModelBase._deviceStatus.TestMode, "FG"))
                {
                    return ConnectionStatus.BoardFailure;
                }
                return ConnectionStatus.BoardFailure;
            }
        }

        private bool InitAngioBoard()
        {
            AskBoardConnection();

            while (!isBoardInited)
            {
                Thread.Sleep(500);
            }

            if (boardConnection)
            {
                return true;
            }
            return false;
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

        private void ThreadFuncLiveAngioImage()
        {
            while (threadOnLiveAngioImage)
            {
                ReadPacket();
            }
        }

        private void ThreadFuncSaveAngioFrames(PatientCase patientCase)
        {
            string angioFilePath = patientCase.ImageFullPath.Substring(0, patientCase.ImageFullPath.Length - 3);
            try
            {
                while (threadOnSaveAngioFrames)
                {
                    Thread.Sleep(500);
                }

                FileStream fs = new FileStream(angioFilePath + Constants.AngioImageExtension, FileMode.Create, FileAccess.Write);

                angioSaveFrameNum = angioSaveBuffer.Count - 1;

                while (angioSaveFrameNum >= 0)
                {
                    fs.Write(angioSaveBuffer[angioSaveFrameNum--], 0, angioImageSize);
                }
                fs.Close();

                using (XmlWriter xw = XmlWriter.Create(angioFilePath + Constants.AngioParmasExtension, new XmlWriterSettings { Indent = true }))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("config");

                    xw.WriteElementString("AngioFrameHeight", angioFrameHeight.ToString());
                    xw.WriteElementString("AngioFrameWidth", angioFrameWidth.ToString());
                    xw.WriteElementString("AngioFrameNumber", angioSaveBuffer.Count.ToString());
                    xw.WriteElementString("BitsPerPixel", ((int)angioBitsPerPixel).ToString());
                    xw.WriteElementString("Frequency", "60"); // 임시값

                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("File Creation Error: " + ex.Message);
            }
        }
        
        private void ActivateClientThreads()
        {
            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            StartLiveAngioThread();
        }

        public void CloseAngioManager()
        {
            _tcpClient.GetStream().Close();

            if (threadFuncLiveAngioImage != null && threadFuncLiveAngioImage.IsAlive)
                StopLiveAngioThread();
            if (threadFuncSaveAngioFrames != null && threadFuncSaveAngioFrames.IsAlive)
                StopSaveAngioThread();

            string processName = CommonUtil.IsTestMode(ViewModelBase._deviceStatus.TestMode, "FG") ? "FGServerTestStub" : "FGServer";
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }

        }

        private bool ReadPacket()
        {
            try
            {
                if (tmpBuffer.Length >= tmpBufferLen + buffer.Length)
                {
                    bytesRead = _tcpClient.GetStream().Read(buffer, 0, buffer.Length);
                    Array.Copy(buffer, 0, tmpBuffer, tmpBufferLen, bytesRead);
                    tmpBufferLen += bytesRead;
                }

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

            Mat image = new Mat(angioFrameHeight, angioFrameWidth, MatType.CV_8UC(angioBitsPerPixel / 8));
            Marshal.Copy(tmpBuffer, offset, image.Data, angioImageSize);

            int shiftSize = angioImageSize + Constants.ImageHeaderSize + Constants.ImageTailSize;
            Array.Copy(tmpBuffer, shiftSize, tmpBuffer, 0, tmpBufferLen - shiftSize);
            tmpBufferLen -= shiftSize;

            Cv2.Flip(image, image, 0);

            if (threadOnSaveAngioFrames)
            {
                angioSaveBuffer.Add(new byte[angioImageSize]);
                Marshal.Copy(image.Data, angioSaveBuffer.Last(), 0, angioImageSize);
            }

            imgAngio = image;
        }

        private void CommandPacketProcess()
        {
            byte command = tmpBuffer[2];

            if (command == (byte)CommandType.FGDeviceInfo)
            {
                DeviceInfoPacketProcess();
            }
            else
            {
                if (command == (byte)CommandType.FGAngioDisconnected)
                {
                    imgAngio = ShowNoSignal();

                    if (readyToRecv)
                    {
                        SendCommandPacket(CommandType.FGStopped);
                    }

                    ViewModelBase._deviceStatus.IsAngioInitialized = false;

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

                    if (!ViewModelBase._deviceStatus.IsAngioInitialized && !ViewModelBase._deviceStatus.IsAngioConnected)
                    {
                        Task.Run(() =>
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                SelectCathRoom();
                            });
                        });
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = true;
                    });
                }
                else if (command == (byte)CommandType.FGBoardExist)
                {
                    isBoardInited = true;
                    boardConnection = true;
                    threadOnLiveAngioImage = true;
                    AskAngioConnection();
                }
                else if (command == (byte)CommandType.FGBoardNotExist)
                {
                    isBoardInited = true;
                    threadOnLiveAngioImage = false;
                }
                else if (command == (byte)CommandType.FGSuccessChangeChp)
                {
                    AskDeviceInfo();
                    isChpFileChangeSuccess = 1;

                    ViewModelBase._deviceStatus.IsAngioInitialized = true;
                }
                else if (command == (byte)CommandType.FGFailChangeChp)
                {
                    isChpFileChangeSuccess = -1;
                }
                Array.Copy(tmpBuffer, Constants.CommandPacketSize, tmpBuffer, 0, tmpBuffer.Length - Constants.CommandPacketSize);
                tmpBufferLen -= Constants.CommandPacketSize;
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

            buffer = new byte[angioImageSize * 60]; // framerate(임시값)
            tmpBuffer = new byte[angioImageSize * 120];

            Array.Fill<byte>(buffer, 0);
            Array.Fill<byte>(tmpBuffer, 0);

            tmpBufferLen = 0;
        }

        private PacketType CheckPacketType(byte[] tmpBuffer)
        {
            int offset = 0;
            if (tmpBuffer[offset++] == Constants.SOF && tmpBufferLen > 0)
            {
                switch (tmpBuffer[offset++])
                {
                    case (byte)PacketType.Command:
                        if (tmpBufferLen >= Constants.CommandPacketSize && tmpBuffer[Constants.CommandPacketSize - 1] == Constants.EOF)
                        {
                            char checksum = (char)tmpBuffer[Constants.CommandPacketSize - 2];
                            if ((byte)checksum == CalcCheckSum(tmpBuffer, Constants.CommandPacketSize - 2))
                            {
                                return PacketType.Command;
                            }
                        }
                        else if (tmpBufferLen >= Constants.DeviceInfoPacketSize && tmpBuffer[Constants.DeviceInfoPacketSize - 1] == Constants.EOF)
                        {
                            char checksum = (char)tmpBuffer[Constants.DeviceInfoPacketSize - 2];
                            if ((byte)checksum == CalcCheckSum(tmpBuffer, Constants.DeviceInfoPacketSize - 2))
                            {
                                return PacketType.Command;
                            }
                        }
                        break;

                    case (byte)PacketType.Image:
                        short height = BitConverter.ToInt16(tmpBuffer, offset);
                        offset += sizeof(short);
                        short width = BitConverter.ToInt16(tmpBuffer, offset);
                        offset += sizeof(short);
                        char BitsPerPixel = (char)tmpBuffer[offset++];
                        int imageSize = height * width * BitsPerPixel / 8;

                        if (tmpBufferLen >= imageSize)
                        {
                            if (tmpBuffer[Constants.ImageHeaderSize + imageSize + Constants.ImageTailSize - 1] == Constants.EOF)
                            {
                                if (tmpBuffer[Constants.ImageHeaderSize + imageSize] == CalcCheckSum(tmpBuffer, Constants.ImageHeaderSize + imageSize))
                                {
                                    return PacketType.Image;
                                }
                            }
                        }
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
            HersheyFonts fontFace = HersheyFonts.HersheyPlain;
            double fontScale = 20;
            int thickness = 10;

            Size textSize = Cv2.GetTextSize("No Signal", fontFace, fontScale, thickness, out int baseline);

            Point textPosition = new Point(
                (image.Width - textSize.Width) / 2,
                (image.Height + textSize.Height) / 2
            );
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
            if (_tcpClient.Connected)
            {
                _tcpClient.GetStream().Write(commandBuffer, 0, commandBuffer.Length);
            }
            _log.Debug("Send Command: " + (CommandType)commandType);
        }

        public void SendChpFilePacket(String chpFilePath)
        {
            byte[] chpFileBuffer = new byte[6 + chpFilePath.Length];
            int offset = 0;
            chpFileBuffer[offset++] = Constants.SOF;
            chpFileBuffer[offset++] = (byte)PacketType.Command;
            chpFileBuffer[offset++] = (byte)CommandType.FGChpFile;
            chpFileBuffer[offset++] = (byte)(6 + chpFilePath.Length);

            byte[] byteChpFilePath = Encoding.UTF8.GetBytes(chpFilePath);
            Array.Copy(byteChpFilePath, 0, chpFileBuffer, offset, chpFilePath.Length);
            offset += chpFilePath.Length;

            byte checksum = CalcCheckSum(chpFileBuffer, offset);
            chpFileBuffer[offset++] = checksum;
            chpFileBuffer[offset++] = Constants.EOF;

            _tcpClient.GetStream().Write(chpFileBuffer, 0, chpFileBuffer.Length);
        }

        private void StartLiveAngioThread()
        {
            threadOnLiveAngioImage = true;
            threadFuncLiveAngioImage.Start();
        }
        private void StopLiveAngioThread()
        {
            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
        }
        public void StartSaveAngioThread(PatientCase patientCase)
        {
            threadFuncSaveAngioFrames = new Thread(() => ThreadFuncSaveAngioFrames(patientCase));
            threadOnSaveAngioFrames = true;
            threadFuncSaveAngioFrames.Start();
        }
        public void StopSaveAngioThread()
        {
            threadOnSaveAngioFrames = false;
            if (threadFuncSaveAngioFrames != null) threadFuncSaveAngioFrames.Join();

            SendCommandPacket(CommandType.FGStopped);
        }

        public void SelectCathRoom()
        {
            _log.Debug("SelectCathRoom");

            if (isCathRoomDialogOpen)
                return;

            isCathRoomDialogOpen = true;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedCathRoomId"] = ViewModelBase._deviceStatus.SelectedCathRoom == null ? 0 : ViewModelBase._deviceStatus.SelectedCathRoom.Id;

            var result = _dialogService.OpenDialog(new CathRoomDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                ViewModelBase._deviceStatus.SelectedCathRoom = (CathRoom)data["selectedCathRoom"];
            }

            isCathRoomDialogOpen = false;
        }

        public bool GetServerConnection()
        {
            if (_tcpClient == null)
                return false;

            return _tcpClient.Connected;
        }
    }
}
