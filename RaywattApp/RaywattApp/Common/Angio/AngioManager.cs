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

        private bool isBoardInited;
        private bool boardConnection;

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

        public Thread threadFuncSaveAngioFrames;
        private bool threadOnSaveAngioFrames;
        private bool threadOnSaveAsFile;
        public bool threadOnRedoPullback;
        public bool threadOnSaveFinished;
        public bool fromRecording;

        private Thread get_image;
        private bool liveView;
        private static ConcurrentQueue<Mat> imageList;

        public Action OnAngioAvailabilityChanged;

        private short angioFrameWidth;
        public short AngioFrameWidth { get { return angioFrameWidth; } set { angioFrameWidth = value; } }
        private short angioFrameHeight;
        public short AngioFrameHeight { get { return angioFrameHeight; } set { angioFrameHeight = value; } }
        private char angioBitsPerPixel;
        public char AngioBitsPerPixel { get { return angioBitsPerPixel; } set { angioBitsPerPixel = value; } }
        private int angioImageSize;

        private bool readyToRecv;
        public bool ReadyToRecv { get { return readyToRecv; } set { readyToRecv = value; } }

        private short isChpFileChangeSuccess;
        public short IsChpFileChangeSuccess { get { return isChpFileChangeSuccess; } set { isChpFileChangeSuccess = value; } }

        public short isChpFileConnected;

        private bool isCathRoomDialogOpen;

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

        private static void StartFGServerProc(ProcessStartInfo startInfo)
        {
            _log.Debug("StartFGServerProc");

            Process p = new Process();
            startInfo.CreateNoWindow = true;
            p.StartInfo = startInfo;
            p.Start();
        }

        public ConnectionStatus ConnectToServer()
        {
            _log.Debug("ConnectToServer");

            Process[] processes;
            ProcessStartInfo psi = new ProcessStartInfo();
            string processName = CommonUtil.IsTestMode(ViewModelBase.DeviceStatus.TestMode, "FG") ? "FGServerTestStub" : "FGServer";
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
            SoketCheckThreads();
            bool init = InitAngioBoard();
            if (init)
            {
                return ConnectionStatus.Success;
            }
            else
            {
                if (!CommonUtil.IsTestMode(ViewModelBase.DeviceStatus.TestMode, "FG"))
                {
                    return ConnectionStatus.BoardFailure;
                }
                return ConnectionStatus.BoardFailure;
            }
        }

        private bool InitAngioBoard()
        {
            _log.Debug("InitAngioBoard");

            AskBoardConnection();

            while (!isBoardInited)
            {
                Thread.Sleep(3000);
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
            _log.Debug("ThreadFuncLiveAngioImage");

            while (threadOnLiveAngioImage)
            {
                ReadPacket();
            }

            _log.Debug("[Done]ThreadFuncLiveAngioImage");
        }
        private void IsSocketConnected()
        {
            _log.Debug("IsSocketConnected");

            while (isSocketAlive)
            {
                Thread.Sleep(1000);
                if(GetServerConnection() == false && !ViewModelBase.DeviceStatus.IsPowerOff)
                {
                    _log.Debug("Disconnected from the Angio server.");

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase.DeviceStatus.IsAngioConnected = false;
                        Dictionary<string, object> popupParameter = new Dictionary<string, object>();
                        popupParameter["title"] = _l10n["Information"];
                        popupParameter["message"] = _l10n["Disconnected from the Angio server."];
                        var popupResult = _dialogService.OpenDialog(new AlertDialogControl(), popupParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    });

                    isSocketAlive = false;
                }
            }

            _log.Debug("[Done]IsSocketConnected");
        }
        private void LiveViewThread()
        {
            _log.Debug("LiveViewThread");

            while (liveView)
            {
                if (!readyToRecv) Thread.Sleep(500);
                if (imageList.TryDequeue(out var mat))
                {
                    imgAngio = mat;
                    Thread.Sleep(10);
                }
            }

            _log.Debug("[Done]LiveViewThread");
        }

        private void ThreadFuncSaveAngioFrames(PatientCase patientCase)
        {
            _log.Debug("ThreadFuncSaveAngioFrames");

            string angioFilePath = patientCase.ImageFullPath.Substring(0, patientCase.ImageFullPath.Length - 3);
            try
            {
                while (!threadOnSaveAsFile)
                {
                    if(threadOnRedoPullback)
                    {
                        _log.Debug("threadOnRedoPullback");
                        return;
                    }
                    Thread.Sleep(500);
                }

                angioSaveFrameNum = angioSaveBuffer.Count > angioSaveTimes.Count ? angioSaveTimes.Count : angioSaveBuffer.Count;
                int closestIndex = 0;
                double OCTStartTime = RayGetProperty(Property.PullbackStartTime) / 2.0;
                double minGap = double.MaxValue;
                double angioTime = double.MaxValue;

                if (angioSaveTimes.Count != 0)
                {
                    for (int i = 0; i < angioSaveFrameNum; i++)
                    {
                        angioTime = angioSaveTimes[i] / 1000.0;
                        double gap = Math.Abs(angioTime - OCTStartTime);

                        if (gap <= minGap)
                        {
                            minGap = gap;
                            closestIndex = i;
                        }
                    }
                    angioSaveTimes.Clear();
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

                if(angioBuffer != null)
                {
                    angioBuffer.Clear();
                }

                angioBuffer = new ConcurrentQueue<byte[]>();
                int availableFrames = angioSaveFrameNum - (closestIndex + 1);
                int desiredFrameCount = angioTargetFrameNum;

                angioSaveFrameNum = desiredFrameCount;
                double step = (double)availableFrames / desiredFrameCount;

                for (int i = 0; i < desiredFrameCount; i++)
                {
                    int index = closestIndex + (int)Math.Round(i * step);
                    if (index >= angioSaveBuffer.Count) break;
                    angioBuffer.Enqueue(angioSaveBuffer[index]);
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
            finally
            {
                _log.Debug("[Done]ThreadFuncSaveAngioFrames");
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
            _log.Debug("ActivateClientThreads");

            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            StartLiveAngioThread();
        }

        private void SoketCheckThreads()
        {
            _log.Debug("SoketCheckThreads");

            isSocketConnected = new Thread(() => IsSocketConnected());
            StartSoketCheck();
        }
        private void LiveViewThreads()
        {
            if (liveView == false)
            {
                _log.Debug("LiveViewThreads");
                get_image = new Thread(() => LiveViewThread());
                StartLiveView();
            }
        }

        public void CloseAngioManager()
        {
            _log.Debug("CloseAngioManager");

            if (threadFuncSaveAngioFrames != null && threadFuncSaveAngioFrames.IsAlive)
                StopGettingAngioImageThread();

            if (isSocketConnected != null && isSocketConnected.IsAlive)
                StopSoketCheck();

            StopLiveView();

            if (GetServerConnection())
                _tcpClient.GetStream().Close();

            if (threadFuncLiveAngioImage != null && threadFuncLiveAngioImage.IsAlive)
                StopLiveAngioThread();

            if (_tcpClient != null)
                _tcpClient.Close();

            ViewModelBase.DeviceStatus.IsAngioConnected = false;

            string processName = CommonUtil.IsTestMode(ViewModelBase.DeviceStatus.TestMode, "FG") ? "FGServerTestStub" : "FGServer";
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
            double live_time = BitConverter.ToInt64(tmpBuffer, offset); // Time Stamp
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
            if (!ViewModelBase.DeviceStatus.IsAngioConnected)return;

            if(readyToRecv)
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
                    _log.Debug("FGAngio Disconnected command ");
                    if (isCathRoomDialogOpen)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ViewModelBase.DeviceStatus.IsAngioConnected = false;
                            Dictionary<string, object> parameter = new Dictionary<string, object>();
                            parameter["title"] = _l10n["Error"];
                            parameter["message"] = _l10n["$MSG023"];
                            _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                        }));
                        isCathRoomDialogOpen = false;
                    }

                    if (readyToRecv) 
                    {
                        SendCommandPacket(CommandType.FGStopped);
                        ToggleLive(false);
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase.DeviceStatus.IsAngioConnected = false;
                        OnAngioAvailabilityChanged?.Invoke();
                    });
                }
                else if (command == (byte)CommandType.FGAngioConnected)
                {
                    if (readyToRecv)
                    {
                        SendCommandPacket(CommandType.FGStarted);
                        ToggleLive(true);
                    }

                    if (!ViewModelBase.DeviceStatus.IsAngioInitialized && !ViewModelBase.DeviceStatus.IsAngioConnected && readyToRecv)
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
                        ViewModelBase.DeviceStatus.IsAngioConnected = true;
                        _log.Debug("Now angio is connected");
                        OnAngioAvailabilityChanged?.Invoke();
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
                    _log.Debug("IsChpFileChangeSuccess = 1");
                    isChpFileConnected = 1;
                    isChpFileChangeSuccess = 1;
                    ViewModelBase.DeviceStatus.IsAngioInitialized = true;
                    ToggleLive(true);
                }
                else if (command == (byte)CommandType.FGFailChangeChp)
                {
                    isChpFileConnected = 0;
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

        public static Mat ShowNoSignal()
        {
            Mat image = new Mat(1080, 1920, MatType.CV_8UC3);
            image.SetTo(new Scalar(0, 0, 0));
            return image;
        }

        private static byte CalcCheckSum(byte[] buffer, int size)
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
            _log.Debug("StartLiveAngioThread");

            threadOnLiveAngioImage = true;
            threadFuncLiveAngioImage.Start();
        }
        private void StopLiveAngioThread() 
        {
            _log.Debug("StopLiveAngioThread");

            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
        }
        private void StartLiveView()
        {
            _log.Debug("StartLiveView");

            liveView = true;
            imageList = new ConcurrentQueue<Mat>();
            get_image.Start();
        }
        private void StopLiveView()
        {
            _log.Debug("StopLiveView");

            liveView = false;

            if (imageList != null)
            {
                imageList.Clear();
                _log.Debug("Image_List Clear");
            }

            if (get_image != null && get_image.IsAlive)
            {
                get_image.Join();
            }
        }
        private void StartSoketCheck()
        {
            _log.Debug("StartSoketCheck");

            if (isSocketConnected != null)
            {
                isSocketAlive = true;
                isSocketConnected.Start();
            }
        }
        public void StopSoketCheck()
        {
            _log.Debug("StopSoketCheck");

            if (isSocketConnected != null)
            {
                isSocketAlive = false;
                isSocketConnected.Join();
            }
        }
        public void ReadyToSaveAngioThread(PatientCase patientCase)
        {
            _log.Debug("ReadyToSaveAngioThread");

            threadFuncSaveAngioFrames = new Thread(() => ThreadFuncSaveAngioFrames(patientCase));
            threadOnRedoPullback = false;
            threadOnSaveAsFile = false;
            threadOnSaveAngioFrames = true;
            threadOnSaveFinished = false;
            fromRecording = false;
            threadFuncSaveAngioFrames.Start();
        }
        public void StopGettingAngioImageThread()
        {
            _log.Debug("StopGettingAngioImageThread");

            threadOnSaveAngioFrames = false;
            SendCommandPacket(CommandType.FGStopped);
            ToggleLive(false); 
        }

        public void StartSaveAngioFrames()
        {
            _log.Debug("StartSaveAngioFrames");

            threadOnSaveAsFile = true;
        }

        public void SelectCathRoom()
        {
            _log.Debug("SelectCathRoom");
            
            if (isCathRoomDialogOpen)
                return;

            isCathRoomDialogOpen = true;
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedCathRoomId"] = ViewModelBase.DeviceStatus.SelectedCathRoom == null ? 0 : ViewModelBase.DeviceStatus.SelectedCathRoom.Id;

            var result = _dialogService.OpenDialog(new CathRoomDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                ViewModelBase.DeviceStatus.SelectedCathRoom = (CathRoom)data["selectedCathRoom"];
                OnAngioAvailabilityChanged?.Invoke();
            }

            isCathRoomDialogOpen = false;
        }

        public bool GetServerConnection()
        {
            if (_tcpClient == null)
                return false;

            return _tcpClient.Connected;
        }
        public void ToggleLive(bool toggle)
        {
            if (toggle)
                LiveViewThreads();
            else
                StopLiveView();

        }
    }
}
