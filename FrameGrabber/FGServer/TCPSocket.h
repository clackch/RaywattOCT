#ifndef TCPSOCKET_H
#define TCPSOCKET_H

#include "FGServer.h"
#include "FrameGrabber.h"
#include <ws2tcpip.h>
#include "Repository.h"

enum PacketType {
	Image,
	Command,
	Nothing
};

enum CommandType {
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

class TCPSocket {
public:
	TCPSocket();
	~TCPSocket();

	void ConnectClient(FrameGrabber& fg, int arg = 0);
	void StartInitThreads(FrameGrabber& fg);

private:
	void SnapFrame(FrameGrabber& fg);
	void LiveFrame(FrameGrabber& fg);
	void SetImagePacketHeader(FrameGrabber& fg);
	void SetCommandPacket(char commandType);
	void ReceivePacket(FrameGrabber& fg);
	void SetDeviceInfoPacket(FrameGrabber& fg, char* buffer);
	void ChpFilePacketProcess(FrameGrabber& fg);
	void RefreshLiveStream(FrameGrabber& fg);
	byte CalcCheckSum(char* sendBuffer, int size); 
	CommandType CheckCommandType(const char* receivedBuffer);
	long long timeSelect();

	// thread
	void StartSnapFrameThread(FrameGrabber& fg);
	void StopSnapFrameThread();
	void StartLiveFrameThread(FrameGrabber& fg);
	void StopLiveFrameThread(FrameGrabber& fg);
	void PortEventThread(FrameGrabber& fg);
	void ReceiveCmdThread(FrameGrabber& fg);
	void SnapFrameThread(FrameGrabber& fg);
	void LiveFrameThread(FrameGrabber& fg);
	void CheckClientThread();

	char sof = 0x3A;
	char type = 0x00;
	byte checkSum = 0x00;
	char eof = 0xA3;

	WSADATA wsaData;
	SOCKET serverSocket;
	sockaddr_in serverAddress;
	fd_set readSet;
	SOCKET clientSocket = INVALID_SOCKET;
	bool isConnected = false;
	bool isStarted = false;
	ERRTYPE m_ErrorCode;

	char deviceInfoBuffer[10];
	char commandBuffer[5];
	char* sendBuffer;
	char recvBuffer[100];
	char tmpRecvBuffer[200];
	int tmpRecvBufferLen = 0;
	int imagePacketSize;
	bool Chp_selected;
	int retryCount;
	int retryConnectCount = 0;
	Repository repo;

	// thread
	bool receiveCmdThreadRunning = true;
	bool snapFrameThreadRunning = false;
	bool liveFrameThreadRunning = false;
	bool portEventThreadRunning = true;
	bool checkClientThreadRunning = true;
	thread snapFrameThreadHandle;
	thread liveFrameThreadHandle;
};

#endif