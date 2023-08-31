using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using static RaywattOCT.Ray3DWrapper;

namespace RaywattApp.Models
{
    public partial class AngioClient : ViewModelBase
    {
        public static Thread threadFuncLiveAngioImage;

        public static bool threadOnLiveAngioImage;

        public void ThreadFuncLiveAngioImage()
        {
            while (threadOnLiveAngioImage)
            {
                DrawAngioImage();
            }
        }

        public void ConnectServer()
        {
            Array.Fill<byte>(TcpClientSingleton.tmpBuffer, 0);

            if (TcpClientSingleton.Instance.Connected == true)
                TcpClientSingleton.isConnected = true;
        }

        public void ActivateClientThread()
        {
            threadOnLiveAngioImage = true;
            threadFuncLiveAngioImage = new Thread(() => ThreadFuncLiveAngioImage());
            threadFuncLiveAngioImage.Start();
        }

        protected bool DrawAngioImage()
        {
            try
            {
                TcpClientSingleton.bytesRead = TcpClientSingleton.Instance.GetStream().Read(TcpClientSingleton.buffer, 0, 10000000);

                Array.Copy(TcpClientSingleton.buffer, 0, TcpClientSingleton.tmpBuffer, TcpClientSingleton.tmpBufferLen, TcpClientSingleton.bytesRead);
                TcpClientSingleton.tmpBufferLen += TcpClientSingleton.bytesRead;

                while (true)
                {
                    PacketType type = FrameGrabber.checkPacketType(TcpClientSingleton.tmpBuffer);
                    if (type == PacketType.Command)
                        commandPacketProcess();
                    else if (type == PacketType.Image)
                        imagePacketProcess();
                    else if (type == PacketType.Nothing)
                        break;
                }
            }
            catch (Exception ex)
            {
                TcpClientSingleton.isConnected = false;
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

        private void imagePacketProcess()
        {
            int offset = 2;
            short height = BitConverter.ToInt16(TcpClientSingleton.tmpBuffer, offset);
            offset += sizeof(short);
            short width = BitConverter.ToInt16(TcpClientSingleton.tmpBuffer, offset);
            offset += sizeof(short);
            char BitsPerPixel = (char)TcpClientSingleton.tmpBuffer[offset++];
            int imageSize = height * width * BitsPerPixel / 8;
            if (TcpClientSingleton.tmpBuffer[imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize - 2] == CalcCheckSum(TcpClientSingleton.tmpBuffer, offset + imageSize)
                && TcpClientSingleton.tmpBuffer[imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize - 1] == 0xA3)
            {
                Mat image = new Mat(height, width, MatType.CV_8UC(BitsPerPixel / 8));
                Marshal.Copy(TcpClientSingleton.tmpBuffer, offset, image.Data, imageSize);
                offset += imageSize;
                char checksum = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);
                char eof = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);

                Array.Copy(TcpClientSingleton.tmpBuffer, imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize, TcpClientSingleton.tmpBuffer, 0, 20000000 - imageSize - Sizes.imageHeaderSize - Sizes.imageTailSize);
                TcpClientSingleton.tmpBufferLen -= imageSize + Sizes.imageHeaderSize + Sizes.imageTailSize;

                Cv2.Flip(image, image, 0);

                Mat imgRecv = CommonUtil.ByteMemoryToCvMat(image.Data, width, height, BitsPerPixel / 8);
                TcpClientSingleton.imgAngio = imgRecv;
            }
        }

        private void commandPacketProcess()
        {
            int offset = 2;
            int command = TcpClientSingleton.tmpBuffer[offset++];
            char checksum = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);
            char eof = BitConverter.ToChar(TcpClientSingleton.tmpBuffer, offset++);

            if ((byte)checksum == CalcCheckSum(TcpClientSingleton.tmpBuffer, 3))
            {
                if (command == (int)CommandType.FGDisconnected)
                {
                    TcpClientSingleton.portConnection = false;
                    TcpClientSingleton.imgAngio = TcpClientSingleton.ShowNoSignal();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        DeviceStatus.IsAngioConnected = TcpClientSingleton.portConnection;
                    });
                }
                else if (command == (int)CommandType.FGConnected)
                {
                    TcpClientSingleton.portConnection = true;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        DeviceStatus.IsAngioConnected = TcpClientSingleton.portConnection;
                    });
                }

                Array.Copy(TcpClientSingleton.tmpBuffer, Sizes.commandPacketSize, TcpClientSingleton.tmpBuffer, 0, 20000000 - Sizes.commandPacketSize);
                TcpClientSingleton.tmpBufferLen -= Sizes.commandPacketSize;
            }
        }
    }
}
