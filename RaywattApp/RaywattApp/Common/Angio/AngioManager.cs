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
using System.Collections.Concurrent;
using static RaywattOCT.RayCoreWrapper;

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
        public Mat ImgAngio { get { return imgAngio; } set { imgAngio = value; } }

        private bool isBoardInited = false;
        private bool boardConnection = false;

        private byte[] buffer;
        private byte[] tmpBuffer;
        private List<byte[]> angioSaveBuffer;
        public ConcurrentQueue<byte[]> angioBuffer;
        public List<byte[]> AngioSaveBuffer { get { return angioSaveBuffer; } set { angioSaveBuffer = value; } }
        public List<double> angioSaveTimes;
        private int bytesRead;
        private int tmpBufferLen;
        private int angioSaveFrameNum;
        public int AngioSaveFrameNum { get { return angioSaveFrameNum; } set { angioSaveFrameNum = value; } }

        private byte[] commandBuffer = { Constants.SOF, (byte)PacketType.Command, (byte)CommandType.FGUnknown, 0x00, Constants.EOF };

        private Thread threadFuncLiveAngioImage;
        private bool threadOnLiveAngioImage;

        private Thread isSocketConnected;
        private bool isSocketAlive;

        private Thread threadFuncSaveAngioFrames;
        private bool threadOnSaveAngioFrames;
        private bool threadOnSaveAsFile;
        public bool threadOnSaveFinished;

        private Thread get_image;
        private bool liveView;
        private static ConcurrentQueue<Mat> imageList;

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
        private double live_time;

        public AngioManager(IDialogService dialogService)
        {
            _log.Debug("AngioManager");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];
            _dialogService = dialogService;

            imgAngio = ShowNoSignal();
            buffer = new byte[256];
            tmpBuffer = new byte[512];
            angioSaveBuffer = new List<byte[]>();
            angioSaveTimes = new List<double>();

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
            processes = Process.GetProcessesByName("FGServer");
            processes = processes.Concat(Process.GetProcessesByName("FGServerTestStub")).ToArray();
            psi.FileName = Constants.FGFolderPath + "\\" + processName + ".exe";

            foreach (Process process in processes)
            {
                process.Kill();
            }
            StartFGServerProc(psi);

            _tcpClient = new TcpClient(Constants.ServerIP, Constants.ServerPort);

            ActivateClientThreads();
            LiveViewThreads();
            SoketCheckThreads();
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
        private void IsSocketConnected()
        {
            while (isSocketAlive)
            {
                Thread.Sleep(1000);
                if(GetServerConnection() == false && !ViewModelBase._deviceStatus.IsPowerOff)
                {
                    _log.Debug("server down");
                    CommonUtil.Exit(ViewModelBase._deviceStatus, this, true);
                    isSocketAlive = false;
                }
            }
        }
        private void LiveViewThread()
        {
            while (liveView)
            {
                if (!readyToRecv) Thread.Sleep(500);
                if (imageList.TryDequeue(out var mat))
                {
                    imgAngio = mat;
                    Thread.Sleep(10);
                }
            }
        }

        private void ThreadFuncSaveAngioFrames(PatientCase patientCase)
        {
            string angioFilePath = patientCase.ImageFullPath.Substring(0, patientCase.ImageFullPath.Length - 3);
            try
            {
                while (!threadOnSaveAsFile)
                {
                    Thread.Sleep(500);
                }

                angioSaveFrameNum = angioSaveBuffer.Count;
                int closestIndex = angioSaveFrameNum;
                double OCTStartTime = RayGetProperty(Property.PullbackStartTime) / 2.0;
                double minGap = double.MaxValue;
                double angioTime = double.MaxValue;

                // Buffer 전달
                for (int i = 0; i < angioSaveFrameNum; i++)
                {
                    angioTime = angioSaveTimes[i] / 1000.0;
                    double gap = Math.Abs(angioTime - OCTStartTime);

                    if(gap <= minGap)
                    {
                        minGap = gap;
                        closestIndex = i;
                    }
                }
                _log.Debug($"gap = {minGap} Angio Time = {angioTime}, OCT Time = {OCTStartTime} closestIndex = {closestIndex}" +
                    $"maxIndex = {angioSaveFrameNum}");

                int angioTargetFrameNum = 20;
                switch (patientCase.PullbackType)
                {
                    case "HISH": // 1초
                        angioTargetFrameNum = (int)(angioTargetFrameNum * 3.0); ;
                        break;
                    case "HILO": // 2.5초
                        angioTargetFrameNum = (int)(angioTargetFrameNum * 2.5);
                        break;
                    case "STSH": // 1초
                        angioTargetFrameNum = angioTargetFrameNum;
                        break; 
                    case "STLO": // 1초
                        angioTargetFrameNum = angioTargetFrameNum;
                        break;
                    case "FAST": // 0.5초
                        angioTargetFrameNum = (int)(angioTargetFrameNum * 0.5);
                        break;
                }

                angioBuffer = new ConcurrentQueue<byte[]>();
                int availableFrames = angioSaveFrameNum - 1 - closestIndex;
                int desiredFrameCount = angioTargetFrameNum;

                // 충분한 프레임이 있는 경우, 일정 간격으로 샘플링
                if (availableFrames >= desiredFrameCount)
                {
                    angioSaveFrameNum = desiredFrameCount;
                    double step = (double)availableFrames / desiredFrameCount;

                    for (int i = 0; i < desiredFrameCount; i++)
                    {
                        int index = closestIndex + (int)Math.Round(i * step);
                        if (index >= angioSaveBuffer.Count) break;
                        angioBuffer.Enqueue(angioSaveBuffer[index]);
                    }
                }
                else
                {
                    // 부족한 경우, 전부 사용
                    int start = closestIndex;
                    int end = angioSaveFrameNum;

                    for (int i = start; i < end; i++)
                    {
                        angioBuffer.Enqueue(angioSaveBuffer[i]);
                    }
                    angioSaveFrameNum = end - start;
                }

                angioSaveBuffer.Clear();

                // 버퍼 저장 (.angioFrames, .params)
                foreach (byte[] tmpBuffer in angioBuffer)
                {
                    angioSaveBuffer.Add(tmpBuffer);
                }
                angioSaveBuffer.Reverse();

                List<byte[]> bufferCopy = new List<byte[]>();

                foreach(var buffer in angioSaveBuffer)
                {
                    bufferCopy.Add(buffer);
                }

                Thread saveThread = new Thread(() =>
                {
                    SaveAngioBufferToFile(angioFilePath, bufferCopy, angioImageSize);
                });
                saveThread.Start();
            }
            catch (Exception ex)
            {
                _log.Debug("Error :" + ex.Message);
            }
        }

        private void SaveAngioBufferToFile(string filePath, List<byte[]> bufferToSave, int imageSize)
        {
            try
            {
                using (XmlWriter xw = XmlWriter.Create(filePath + Constants.AngioParmasExtension, new XmlWriterSettings { Indent = true }))
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

                FileStream fs = new FileStream(filePath + Constants.AngioImageExtension, FileMode.Create, FileAccess.Write);
                for (int i = 0; i < bufferToSave.Count; i++)
                {
                    fs.Write(bufferToSave[i], 0, imageSize);
                }
                fs.Close();
                threadOnSaveFinished = true;
                _log.Debug("Angio buffer successfully saved.");
            }
            catch (Exception ex)
            {
                _log.Debug("Error while saving angio buffer: " + ex.Message);
            }
        }


        private void ActivateClientThreads()
        {
            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            StartLiveAngioThread();
        }

        private void SoketCheckThreads()
        {
            isSocketConnected = new Thread(() => IsSocketConnected());
            StartSoketCheck();
        }
        private void LiveViewThreads()
        {
            get_image = new Thread(() => LiveViewThread());
            StartLiveView();
        }

        public void CloseAngioManager()
        {
            if (GetServerConnection())
            {
                _tcpClient.GetStream().Close();
            }
            if (threadFuncLiveAngioImage != null && threadFuncLiveAngioImage.IsAlive)
                StopLiveAngioThread();
                StopLiveView();
            if (threadFuncSaveAngioFrames != null && threadFuncSaveAngioFrames.IsAlive)
                StopGettingAngioImageThread();

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
                    if (type == PacketType.Command) {
                        CommandPacketProcess();
                        _log.Debug("packet end");
                    }
                        
                    else if (type == PacketType.Image)
                        ImagePacketProcess();
                    else if (type == PacketType.Nothing)
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (ex.InnerException is System.Net.Sockets.SocketException socketException)
                {
                    int errorCode = socketException.ErrorCode;
                    if (GetServerConnection() == false) // 서버 연결이 끊어졌을 때의 에러 코드
                    {
                        threadOnLiveAngioImage = false;
                    }
                }
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
            live_time = BitConverter.ToInt64(tmpBuffer, offset); // Time Stamp

            offset += sizeof(long);

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
                angioSaveTimes.Add(live_time);
                Marshal.Copy(image.Data, angioSaveBuffer.Last(), 0, angioImageSize);
            }
            if (!ViewModelBase._deviceStatus.IsAngioConnected)return;

            imageList.Enqueue(image);
        }

        private void CommandPacketProcess()
        {
            byte command = tmpBuffer[2];
            _log.Debug(command);
            if (command == (byte)CommandType.FGDeviceInfo)
            {
                DeviceInfoPacketProcess();
            }
            else
            {
                if (command == (byte)CommandType.FGAngioDisconnected)
                {
                    imgAngio = ShowNoSignal();
                    _log.Debug("FGAngio Disconnected command");
                    if (isCathRoomDialogOpen)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ViewModelBase._deviceStatus.IsAngioConnected = false;
                            Dictionary<string, object> parameter = new Dictionary<string, object>();
                            parameter["title"] = _l10n["Error"];
                            parameter["message"] = _l10n["$MSG023"];
                            _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                        });
                        isCathRoomDialogOpen = false;
                    }

                    if (readyToRecv) 
                    {
                        SendCommandPacket(CommandType.FGStopped);
                    }

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

                    if (!ViewModelBase._deviceStatus.IsAngioInitialized && !ViewModelBase._deviceStatus.IsAngioConnected && readyToRecv)
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
                        offset += sizeof(long);
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
            _log.Debug("No signal");
            Mat image = new Mat(1080, 1920, MatType.CV_8UC3);
            image.SetTo(new Scalar(0, 0, 0));
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
        private void StartLiveView()
        {
            liveView = true;
            imageList = new ConcurrentQueue<Mat>();
            get_image.Start();
        }
        private void StopLiveView()
        {
            imageList.Clear();
            liveView = false;
            get_image.Join();
        }
        private void StartSoketCheck()
        {
            isSocketAlive = true;
            isSocketConnected.Start();
        }
        public void StopSoketCheck()
        {
            isSocketAlive = false;
            isSocketConnected.Join();
        }
        public void ReadyToSaveAngioThread(PatientCase patientCase)
        {
            threadFuncSaveAngioFrames = new Thread(() => ThreadFuncSaveAngioFrames(patientCase));
            threadOnSaveAngioFrames = true;
            threadOnSaveFinished = false;
            threadOnSaveAsFile = false;
            threadFuncSaveAngioFrames.Start();
        }
        public void StopGettingAngioImageThread()
        {
            threadOnSaveAngioFrames = false;
            SendCommandPacket(CommandType.FGStopped);
        }

        public void StartSaveAngioFrames()
        {
            threadOnSaveAsFile = true;
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
