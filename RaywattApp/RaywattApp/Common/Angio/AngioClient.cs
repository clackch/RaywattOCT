using OpenCvSharp;
using OpenCvSharp.Internal;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public enum PacketType
{
    Image,
    Command,
    Nothing
};

enum CommandType
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

namespace RaywattApp.Common.Angio
{
    public partial class AngioClient
    {
        public static Thread threadFuncLiveAngioImage;

        public static bool threadOnLiveAngioImage;

        private readonly AngioManager _angioManager;

        public AngioClient(AngioManager angioManager)
        {
            Array.Fill<byte>(angioManager.tmpBuffer, 0);

            if (angioManager.Instance.Connected == true)
                angioManager.isConnected = true;

            _angioManager = angioManager;
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

        public void AskPortConnection()
        {
            _angioManager.Instance.GetStream().Write(_angioManager.askPortCommand, 0, _angioManager.askPortCommand.Length);
        }

        public void AskBoardConnection()
        {
            _angioManager.Instance.GetStream().Write(_angioManager.askBoardCommand, 0, _angioManager.askBoardCommand.Length);
        }

        private bool ReadPacket()
        {
            try
            {
                _angioManager.bytesRead = _angioManager.Instance.GetStream().Read(_angioManager.buffer, 0, 10000000);

                Array.Copy(_angioManager.buffer, 0, _angioManager.tmpBuffer, _angioManager.tmpBufferLen, _angioManager.bytesRead);
                _angioManager.tmpBufferLen += _angioManager.bytesRead;

                while (true)
                {
                    PacketType type = CheckPacketType(_angioManager.tmpBuffer);
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

        static byte CalcCheckSum(byte[] buffer, int size)
        {
            size--;
            byte csum = 0;
            for (; size >= 0; size--)
            {
                csum += buffer[size];
            }
            return (byte)~csum;
        }

        private void ImagePacketProcess()
        {
            int offset = 2;
            short height = BitConverter.ToInt16(_angioManager.tmpBuffer, offset);
            offset += sizeof(short);
            short width = BitConverter.ToInt16(_angioManager.tmpBuffer, offset);
            offset += sizeof(short);
            char BitsPerPixel = (char)_angioManager.tmpBuffer[offset++];
            int imageSize = height * width * BitsPerPixel / 8;
            if (_angioManager.tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 2] == CalcCheckSum(_angioManager.tmpBuffer, offset + imageSize)
                && _angioManager.tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(height, width, MatType.CV_8UC(BitsPerPixel / 8));
                Marshal.Copy(_angioManager.tmpBuffer, offset, image.Data, imageSize);
                offset += imageSize;
                char checksum = BitConverter.ToChar(_angioManager.tmpBuffer, offset++);
                char eof = BitConverter.ToChar(_angioManager.tmpBuffer, offset++);

                Array.Copy(_angioManager.tmpBuffer, imageSize + Constants.imageHeaderSize + Constants.imageTailSize, _angioManager.tmpBuffer, 0, 20000000 - imageSize - Constants.imageHeaderSize - Constants.imageTailSize);
                _angioManager.tmpBufferLen -= imageSize + Constants.imageHeaderSize + Constants.imageTailSize;

                Cv2.Flip(image, image, 0);

                int t = 500, l = 1000, b = 1000, r = 1900;
                Rect roi = new Rect(l, t, r - l, b - t);
                image = image.SubMat(roi);

                _angioManager.imgAngio = image;
            }
        }

        private void CommandPacketProcess()
        {
            int offset = 2;
            int command = _angioManager.tmpBuffer[offset++];
            char checksum = BitConverter.ToChar(_angioManager.tmpBuffer, offset++);
            char eof = BitConverter.ToChar(_angioManager.tmpBuffer, offset++);

            if ((byte)checksum == CalcCheckSum(_angioManager.tmpBuffer, 3))
            {
                if (command == (int)CommandType.FGDisconnected)
                {
                    _angioManager.portConnection = false;
                    _angioManager.imgAngio = ShowNoSignal();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = _angioManager.portConnection;
                    });
                }
                else if (command == (int)CommandType.FGConnected)
                {
                    _angioManager.portConnection = true;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = _angioManager.portConnection;
                    });
                }
                else if (command == (int)CommandType.FGBoardExist)
                {
                    _angioManager.boardConnection = true;
                }
                else if (command == (int)CommandType.FGBoardNotExist)
                {
                    _angioManager.boardConnection = false;
                }

                Array.Copy(_angioManager.tmpBuffer, Constants.commandPacketSize, _angioManager.tmpBuffer, 0, 20000000 - Constants.commandPacketSize);
                _angioManager.tmpBufferLen -= Constants.commandPacketSize;
            }
        }

        public static void CloseLiveAngioImageThread()
        {
            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
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
        public static Mat ShowNoSignal()
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
    }
}
