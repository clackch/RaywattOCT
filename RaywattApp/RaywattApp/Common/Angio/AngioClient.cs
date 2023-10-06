using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
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
    FGConnected,
    FGDisconnected,
    FGStarted,
    FGStopped,
    FGNothing,
};

namespace RaywattApp.Common.Angio
{
    public partial class AngioClient
    {
        public static Thread threadFuncLiveAngioImage;

        public static bool threadOnLiveAngioImage;

        private readonly AngioManager AngioManager;

        public AngioClient(AngioManager AngioManager)
        {
            Array.Fill<byte>(AngioManager.tmpBuffer, 0);

            if (AngioManager.Instance.Connected == true)
                AngioManager.isConnected = true;

            AngioManager = AngioManager;
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

        protected bool ReadPacket()
        {
            try
            {
                AngioManager.bytesRead = AngioManager.Instance.GetStream().Read(AngioManager.buffer, 0, 10000000);

                Array.Copy(AngioManager.buffer, 0, AngioManager.tmpBuffer, AngioManager.tmpBufferLen, AngioManager.bytesRead);
                AngioManager.tmpBufferLen += AngioManager.bytesRead;

                while (true)
                {
                    PacketType type = CheckPacketType(AngioManager.tmpBuffer);
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
                AngioManager.isConnected = false;
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
            short height = BitConverter.ToInt16(AngioManager.tmpBuffer, offset);
            offset += sizeof(short);
            short width = BitConverter.ToInt16(AngioManager.tmpBuffer, offset);
            offset += sizeof(short);
            char BitsPerPixel = (char)AngioManager.tmpBuffer[offset++];
            int imageSize = height * width * BitsPerPixel / 8;
            if (AngioManager.tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 2] == CalcCheckSum(AngioManager.tmpBuffer, offset + imageSize)
                && AngioManager.tmpBuffer[imageSize + Constants.imageHeaderSize + Constants.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(height, width, MatType.CV_8UC(BitsPerPixel / 8));
                Marshal.Copy(AngioManager.tmpBuffer, offset, image.Data, imageSize);
                offset += imageSize;
                char checksum = BitConverter.ToChar(AngioManager.tmpBuffer, offset++);
                char eof = BitConverter.ToChar(AngioManager.tmpBuffer, offset++);

                Array.Copy(AngioManager.tmpBuffer, imageSize + Constants.imageHeaderSize + Constants.imageTailSize, AngioManager.tmpBuffer, 0, 20000000 - imageSize - Constants.imageHeaderSize - Constants.imageTailSize);
                AngioManager.tmpBufferLen -= imageSize + Constants.imageHeaderSize + Constants.imageTailSize;

                Cv2.Flip(image, image, 0);

                Mat imgRecv = CommonUtil.ByteMemoryToCvMat(image.Data, width, height, BitsPerPixel / 8);
                AngioManager.imgAngio = imgRecv;
            }
        }

        private void CommandPacketProcess()
        {
            int offset = 2;
            int command = AngioManager.tmpBuffer[offset++];
            char checksum = BitConverter.ToChar(AngioManager.tmpBuffer, offset++);
            char eof = BitConverter.ToChar(AngioManager.tmpBuffer, offset++);

            if ((byte)checksum == CalcCheckSum(AngioManager.tmpBuffer, 3))
            {
                if (command == (int)CommandType.FGDisconnected)
                {
                    AngioManager.portConnection = false;
                    AngioManager.imgAngio = ShowNoSignal();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = AngioManager.portConnection;
                    });
                }
                else if (command == (int)CommandType.FGConnected)
                {
                    AngioManager.portConnection = true;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewModelBase._deviceStatus.IsAngioConnected = AngioManager.portConnection;
                    });
                }

                Array.Copy(AngioManager.tmpBuffer, Constants.commandPacketSize, AngioManager.tmpBuffer, 0, 20000000 - Constants.commandPacketSize);
                AngioManager.tmpBufferLen -= Constants.commandPacketSize;
            }
        }

        public static void CloseLiveAngioImageThread()
        {
            threadOnLiveAngioImage = false;
            threadFuncLiveAngioImage.Join();
        }

        public PacketType CheckPacketType(byte[] tmpBuffer)
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
            Mat image = new Mat(800, 1000, MatType.CV_8UC3);
            image.SetTo(new Scalar(0, 0, 0));

            Scalar textColor = new Scalar(0, 0, 255);
            HersheyFonts fontFace = HersheyFonts.HersheyComplex;
            double fontScale = 2.0;
            int thickness = 5;

            Point textPosition = new Point(300, 500);
            Cv2.PutText(image, "No Signal", textPosition, fontFace, fontScale, textColor, thickness);

            return image;
        }
    }
}
