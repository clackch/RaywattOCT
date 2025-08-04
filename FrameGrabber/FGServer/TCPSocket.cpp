#include "TCPSocket.h"
#include <future>
#include <vector>
#define MAX_RECV_BUFFER_SIZE 1024

TCPSocket::TCPSocket() {

	try {
		if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
		{
			PLOGI.printf("Failed to initialize winsock. Error code: %d", WSAGetLastError());
			WSACleanup();
			//exit(0);
			throw std::runtime_error("WSAStartup failed");
		}

		serverSocket = socket(AF_INET, SOCK_STREAM, 0);
		if (serverSocket == INVALID_SOCKET)
		{
			PLOGI.printf("Failed to create socket. Error code: %d", WSAGetLastError());

			if (serverSocket != INVALID_SOCKET)
			{
				closesocket(serverSocket);
			}

			WSACleanup();
			//exit(0);
			throw std::runtime_error("Failed to create socket");
		}

		serverAddress.sin_family = AF_INET;
		serverAddress.sin_port = htons(8888); // host to network short
		serverAddress.sin_addr.s_addr = INADDR_ANY;

		if (::bind(serverSocket, (struct sockaddr*)&serverAddress, sizeof(serverAddress)) == SOCKET_ERROR)
		{
			PLOGI.printf("Failed to bind socket. Error code: %d", WSAGetLastError());
			closesocket(serverSocket);
			serverSocket = INVALID_SOCKET;
			WSACleanup();
			//exit(0);
			throw std::runtime_error("Failed to bind socket");
		}

		listen(serverSocket, 1);
		repo.Connect();
	}
	catch (const std::exception& ex) {
		PLOGI.printf("Exception occurred: %s", ex.what());
		if (serverSocket != INVALID_SOCKET)
			closesocket(serverSocket);  
		throw;
	}
}

TCPSocket::~TCPSocket()
{
	closesocket(clientSocket);
	closesocket(serverSocket);
	WSACleanup();
}

void TCPSocket::SetCommandPacket(char commandType) {
	type = PacketType::Command;

	memcpy(commandBuffer, &sof, sizeof(sof));
	memcpy(commandBuffer + 1, &type, sizeof(type));
	memcpy(commandBuffer + 2, &commandType, sizeof(commandType));

	checkSum = CalcCheckSum(commandBuffer, 3);
	memcpy(commandBuffer + 3, &checkSum, sizeof(checkSum));
	memcpy(commandBuffer + 4, &eof, sizeof(eof));
}

void TCPSocket::SetImagePacketHeader(FrameGrabber& fg) {
	imagePacketSize = IMAGE_HEADER_SIZE + repo.GetCropRegion().height * repo.GetCropRegion().width* fg.wBitsPerPixel / 8 + IMAGE_TAIL_SIZE;
	fg.sc.pRecvBuf = (uchar*)malloc(imagePacketSize - IMAGE_HEADER_SIZE - IMAGE_TAIL_SIZE);
	sendBuffer = new char[imagePacketSize];

	int offset = 0;
	type = PacketType::Image;
	memcpy(sendBuffer + offset, &sof, sizeof(sof));
	offset += sizeof(sof);
	memcpy(sendBuffer + offset, &type, sizeof(type));
	offset += sizeof(type);
	PLOGI.printf("%hd %hd", repo.GetCropRegion().height, repo.GetCropRegion().width);
	memcpy(sendBuffer + offset, &repo.GetCropRegion().height, sizeof(repo.GetCropRegion().height));
	offset += sizeof(repo.GetCropRegion().height);
	memcpy(sendBuffer + offset, &repo.GetCropRegion().width, sizeof(repo.GetCropRegion().width));
	offset += sizeof(repo.GetCropRegion().width);
	memcpy(sendBuffer + offset, &fg.wBitsPerPixel, sizeof(fg.wBitsPerPixel));
	offset += sizeof(fg.wBitsPerPixel);
}

void TCPSocket::SetDeviceInfoPacket(FrameGrabber& fg, char* buffer) {
	int offset = 0;
	char packetType = PacketType::Command;
	char commandType = CommandType::FGDeviceInfo;

	memcpy(buffer + offset++, &sof, sizeof(sof));
	memcpy(buffer + offset++, &packetType, sizeof(packetType));
	memcpy(buffer + offset++, &commandType, sizeof(commandType));
	memcpy(buffer + offset, &repo.GetCropRegion().height, sizeof(repo.GetCropRegion().height));
	offset += sizeof(repo.GetCropRegion().height);
	memcpy(buffer + offset, &repo.GetCropRegion().width, sizeof(repo.GetCropRegion().width));
	offset += sizeof(repo.GetCropRegion().width);
	memcpy(buffer + offset++, &fg.wBitsPerPixel, sizeof(fg.wBitsPerPixel));
	checkSum = CalcCheckSum(buffer, 8);
	memcpy(buffer + offset++, &checkSum, sizeof(checkSum));
	memcpy(buffer + offset, &eof, sizeof(eof));
}

/*
* ConnectClient
* int arg: 0: initial, 1: restart
*/
void TCPSocket::ConnectClient(FrameGrabber& fg, int arg) {
	if (arg == 1) {
		closesocket(clientSocket);
		clientSocket = INVALID_SOCKET;
		isConnected = false;

		// Stop Thread
		receiveCmdThreadRunning = false;
		portEventThreadRunning = false;
		checkClientThreadRunning = false;

		StopLiveFrameThread(fg);
	}
	while (!isConnected) {
		clientSocket = accept(serverSocket, NULL, NULL);
		if (clientSocket == INVALID_SOCKET)
		{
			PLOGI.printf("Connecting to client... Error code: %d", WSAGetLastError());

		}
		else {
			isConnected = true;

			// Start Thread
			portEventThreadRunning = true;
			receiveCmdThreadRunning = true;
			checkClientThreadRunning = true;
		}
		this_thread::sleep_for(chrono::milliseconds(1000));
	}
}

inline void ClampRecvBuffer(char* buffer, int& len, int maxSize) {
	const size_t bufferLimit = std::min(static_cast<size_t>(maxSize), sizeof(MAX_RECV_BUFFER_SIZE));

	if (len >= maxSize) {
		PLOGW.printf("Buffer overflow risk: tmpRecvBufferLen=%d exceeds MAX_RECV_BUFFER_SIZE (%d)", len, maxSize - 1);
	}

	if (len >= sizeof(MAX_RECV_BUFFER_SIZE)) {
		PLOGE.printf("Buffer overrun risk: tmpRecvBufferLen=%d >= sizeof(tmpRecvBuffer) (%zu)", len, sizeof(MAX_RECV_BUFFER_SIZE) - 1);
	}

	if (len >= bufferLimit) {
		len = bufferLimit - 1;
	}

	buffer[len] = '\0';
}

void TCPSocket::ReceivePacket(FrameGrabber& fg) {
	int bytesReceived = recv(clientSocket, recvBuffer, 100, 0);
	if (bytesReceived == SOCKET_ERROR) {
		PLOGI.printf("Failed to receive data from client. Error code : %d, %d", WSAGetLastError(), bytesReceived);
		exit(1);
		return;
	}

	if (bytesReceived > 0) {
		if (tmpRecvBufferLen + bytesReceived >= MAX_RECV_BUFFER_SIZE) {
			PLOGI.printf("Buffer overflow detected. Dropping packet.");
			tmpRecvBufferLen = 0;
			recvBuffer[0] = '\0';
			return;
		}

		strncat(tmpRecvBuffer, recvBuffer, bytesReceived);
		memset(recvBuffer, '\0', bytesReceived);
		tmpRecvBufferLen += bytesReceived;

		while (true) {
			CommandType commandType = CheckCommandType(tmpRecvBuffer);
			int sendResult;

			switch (commandType) {
			case CommandType::FGStarted:
				PLOGI.printf("FGStarted");
				tmpRecvBufferLen -= 5;
				memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
				ClampRecvBuffer(tmpRecvBuffer, tmpRecvBufferLen, MAX_RECV_BUFFER_SIZE);
				isStarted = true;
				StartLiveFrameThread(fg);
				break;

			case CommandType::FGStopped:
				PLOGI.printf("FGStopped");
				tmpRecvBufferLen -= 5;
				memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
				ClampRecvBuffer(tmpRecvBuffer, tmpRecvBufferLen, MAX_RECV_BUFFER_SIZE);
				isStarted = false;
				StopLiveFrameThread(fg);
				break;

			case CommandType::FGAskPort:
				PLOGI.printf("FGAskPort");
				tmpRecvBufferLen -= 5;
				memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
				ClampRecvBuffer(tmpRecvBuffer, tmpRecvBufferLen, MAX_RECV_BUFFER_SIZE);

				if (fg.portConnection == 0) {
					SetCommandPacket(CommandType::FGAngioDisconnected);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					PLOGI.printf("Send Port Disconnected");
				}
				else {
					fg.portConnection = -1;
					SetCommandPacket(CommandType::FGAngioConnected);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					PLOGI.printf("Send Port Connected");
				}
				break;

			case CommandType::FGAskBoard:
				PLOGI.printf("FGAskBoard");
				tmpRecvBufferLen -= 5;
				memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
				ClampRecvBuffer(tmpRecvBuffer, tmpRecvBufferLen, MAX_RECV_BUFFER_SIZE);

				if (fg.boardConnection == 0) {
					SetCommandPacket(CommandType::FGBoardNotExist);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					PLOGI.printf("Send FGBoardNotExist");
				}
				else {
					SetCommandPacket(CommandType::FGBoardExist);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					PLOGI.printf("Send FGBoardExist");
				}
				break;

			case CommandType::FGAskDeviceInfo:
				PLOGI.printf("FGAskDeviceInfo");
				tmpRecvBufferLen -= 5;
				memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
				ClampRecvBuffer(tmpRecvBuffer, tmpRecvBufferLen, MAX_RECV_BUFFER_SIZE);

				SetDeviceInfoPacket(fg, deviceInfoBuffer);
				sendResult = send(clientSocket, deviceInfoBuffer, 10, 0);
				PLOGI.printf("Send Device Info: %d", sendResult);
				break;

			case CommandType::FGChpFile:
				PLOGI.printf("FGChpFile");
				StopLiveFrameThread(fg);
				ChpFilePacketProcess(fg);
				break;

			case CommandType::FGNothing:
				break;
			}

			if (commandType == CommandType::FGNothing)
				break;
		}
	}
}

CommandType TCPSocket::CheckCommandType(const char* tmpRecvBuffer) {
	if (tmpRecvBuffer[0] == (char)0x3A) {
		if (tmpRecvBuffer[1] == PacketType::Command) {
			if (tmpRecvBuffer[2] == CommandType::FGChpFile) {
				short packetLen = tmpRecvBuffer[3];
				if ((unsigned char)tmpRecvBuffer[packetLen - 2] == CalcCheckSum((char*)tmpRecvBuffer, packetLen - 2)) {
					if (tmpRecvBuffer[packetLen - 1] == (char)0xA3) {
						return CommandType::FGChpFile;
					}
				}
			}
			else {
				if (tmpRecvBuffer[3] == (char)CalcCheckSum((char*)tmpRecvBuffer, 3)) {
					if (tmpRecvBuffer[4] == (char)0xA3) {
						switch (tmpRecvBuffer[2]) {
						case CommandType::FGStarted:
							return CommandType::FGStarted;
							break;
						case CommandType::FGStopped:
							return CommandType::FGStopped;
							break;
						case CommandType::FGAskPort:
							return CommandType::FGAskPort;
							break;


						case CommandType::FGAskBoard:
							return CommandType::FGAskBoard;
							break;
						case CommandType::FGAskDeviceInfo:
							return CommandType::FGAskDeviceInfo;
							break;
						}
					}
				}
			}

		}
	}
	return CommandType::FGNothing;
}
void TCPSocket::ChpFilePacketProcess(FrameGrabber& fg) {
	short packetLen = tmpRecvBuffer[3];
	int sendResult;

	fg.chpFileName = string(tmpRecvBuffer + 4, packetLen - 6);

	ERRTYPE e;
	if (fg.chpFileName.substr(0, 6) == "setup\\") {
		e = fg.ReadFormatFile((char*)(fg.chpFilePath + fg.chpFileName).c_str());
		PLOGI.printf("Chp File Name: %s", (fg.chpFilePath + fg.chpFileName).c_str());
	}
	else {
		e = fg.ReadFormatFile((char*)(fg.chpFilePath + "app\\" + fg.chpFileName).c_str());
		PLOGI.printf("Chp File Name: %s", (fg.chpFilePath + "app\\" + fg.chpFileName).c_str());
	}

	if (e) {
		PLOGI.printf(".chp file is missing");
		SetCommandPacket(CommandType::FGFailChangeChp);
		sendResult = send(clientSocket, commandBuffer, 5, 0);
		PLOGI.printf("Send FailChangeChp Info: %d ", sendResult);
	}
	else {
		PLOGI.printf("Success to read .chp file");
		free(fg.sc.pRecvBuf);
		fg.CreateFromFG();
		repo.InitCropRegion(fg); 
		SetImagePacketHeader(fg);
		SetCommandPacket(CommandType::FGSuccessChangeChp);
		sendResult = send(clientSocket, commandBuffer, 5, 0);
		if (sendResult == SOCKET_ERROR) {
			PLOGE.printf("Failed to send FailChangeChp Info. Error: %d", WSAGetLastError());
		}
		PLOGI.printf("Send SuccessChangeChp Info: %d ", sendResult);
		SetDeviceInfoPacket(fg, deviceInfoBuffer);
		sendResult = send(clientSocket, deviceInfoBuffer, 10, 0);
		PLOGI.printf("Send Device Info: %d", sendResult);
		Chp_selected = true;
	}

	if (sendResult == SOCKET_ERROR) {
		PLOGE.printf("Failed to send FailChangeChp Info. Error: %d", WSAGetLastError());
	}

	tmpRecvBufferLen -= packetLen;
	memmove(tmpRecvBuffer, tmpRecvBuffer + packetLen, tmpRecvBufferLen);
	tmpRecvBuffer[tmpRecvBufferLen] = '\0';

}

void TCPSocket::PortEventThread(FrameGrabber& fg)
{
	while (portEventThreadRunning)
	{
		std::this_thread::sleep_for(std::chrono::milliseconds(1000));

		// ✅ pIdeaInfo를 로컬 변수로 복사 (중간에 nullptr로 바뀌는 문제 방지)
		auto* pInfo = fg.pIdeaInfo;
		if (pInfo == nullptr)
			continue;

		HANDLE hEvent = pInfo->hInfoEvent;
		if (hEvent == nullptr)
			continue;

		DWORD status = WaitForSingleObject(hEvent, 100);

		switch (status)
		{
		case WAIT_TIMEOUT:
			PLOGI.printf("WAIT_TIMEOUT called");
			break;

		default:
			PLOGI.printf("PortEventThread called");

			ResetEvent(hEvent);
			pInfo->bNewInfo = FALSE;

			fg.m_bSyncValid = bHP_CSyncDetect(fg.m_BoardHandle);

			if (fg.m_bSyncValid && fg.portConnection != 1)
			{
				fg.portConnection = 1;
				SetCommandPacket(CommandType::FGAngioConnected);
				send(clientSocket, commandBuffer, 5, 0);
				PLOGI.printf("Send Port Connected");
			}
			else if (!fg.m_bSyncValid && fg.portConnection != 0)
			{
				fg.portConnection = 0;
				SetCommandPacket(CommandType::FGAngioDisconnected);
				send(clientSocket, commandBuffer, 5, 0);
				PLOGI.printf("Send Port Disconnected");
			}

			// ✅ 다시 로컬 변수 pInfo를 사용 (fg.pIdeaInfo 재접근 금지)
			HANDLE hInfoEvent = pInfo->hInfoEvent;
			if (hInfoEvent)
			{
				// DLL Thread 문제 방지를 위한 순서
				pInfo->hInfoEvent = nullptr;
				CloseHandle(hInfoEvent);
			}

			break;
		}
	}
}

void TCPSocket::ReceiveCmdThread(FrameGrabber& fg) {
	while (receiveCmdThreadRunning) {
		ReceivePacket(fg);
	}
	if (fg.sc.pRecvBuf != NULL) {

		free(fg.sc.pRecvBuf);
	}
}

void TCPSocket::CheckClientThread() {
	while (checkClientThreadRunning) {
		const char* empty = "";
		int emptySize = 0;
		int sendResult = send(clientSocket, empty, emptySize, 0);
		if (sendResult == SOCKET_ERROR)
		{
			int errorCode = WSAGetLastError();
			PLOGI.printf("Failed to send data to client. Error code: %d", errorCode);
			//exit(0);
			return;
		}
		Sleep(1000);
	}
}

void TCPSocket::StartInitThreads(FrameGrabber& fg) 
{
	auto portEventFuture = std::async(std::launch::async, &TCPSocket::PortEventThread, this, std::ref(fg));
	auto receiveCmdFuture = std::async(std::launch::async, &TCPSocket::ReceiveCmdThread, this, std::ref(fg));
	auto checkClientFuture = std::async(std::launch::async, &TCPSocket::CheckClientThread, this);

	if (fg.boardConnection) portEventFuture.get();
	receiveCmdFuture.get();
	checkClientFuture.get();
}

byte TCPSocket::CalcCheckSum(char* sendBuffer, int size) {
	size--;
	byte csum = 0;
	for (; size >= 0; size--) {
		csum += sendBuffer[size];
	}
	return (byte)~csum;
}

long long TCPSocket::timeSelect() {
	auto now = std::chrono::system_clock::now();
	auto epochDuration = now.time_since_epoch();
	auto milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(epochDuration).count();
	return milliseconds;
}

void TCPSocket::LiveFrame(FrameGrabber& fg) {
	HDVID_HEADER* pVidHeader = nullptr; 
	ERRTYPE bufferResult = eHD_GetStreamBuffer(fg.m_ImageHandle, &pVidHeader); // 이미지 버퍼헤더 가져오는 함수
	long long livetime = timeSelect();
	if (bufferResult != 0 || pVidHeader == nullptr) {
		retryCount++; 
		std::this_thread::sleep_for(std::chrono::milliseconds(10));
		if (retryCount > 100) { // 이미지를 1초 이상 받아오지 못하는 경우 새로고침
			RefreshLiveStream(fg);
			retryCount = 0;
		}
		return;
	}
	std::this_thread::sleep_for(std::chrono::milliseconds(10));
	//여기에서 Crop 기능을 추가해야하는데
	int cropsize = repo.GetCropRegion().height * repo.GetCropRegion().width;
	std::vector<unsigned char> croppedBuffer(cropsize * fg.wBitsPerPixel / 8);
	if (repo.ApplyCrop(pVidHeader, fg, croppedBuffer));
	else PLOGI.printf("[Crop] Failed to apply crop. Using full image.");
	int offset = 7; 
	memcpy(sendBuffer + offset, &livetime, sizeof(livetime));
	offset += sizeof(livetime);
	if(!croppedBuffer.empty()) memcpy(sendBuffer + offset, croppedBuffer.data(), cropsize * fg.wBitsPerPixel / 8);
	offset += cropsize * fg.wBitsPerPixel / 8;
	checkSum = CalcCheckSum(sendBuffer, offset); 
	memcpy(sendBuffer + offset, &checkSum, sizeof(checkSum)); 
	offset += sizeof(checkSum); 
	memcpy(sendBuffer + offset, &eof, sizeof(eof)); 
	offset += sizeof(eof); 
	int sendResult = send(clientSocket, sendBuffer, imagePacketSize, 0);
	if (sendResult == SOCKET_ERROR) { 
		PLOGI.printf("Failed to send data to client. Error code: %d", WSAGetLastError()); 
	}
	eHD_ReleaseStreamBuffer(fg.m_ImageHandle, pVidHeader); //버퍼 할당 해제
	retryCount = 0; 
}

void TCPSocket::StartLiveFrameThread(FrameGrabber& fg) {
	if (!liveFrameThreadRunning) {

		ERRTYPE result = eHD_LiveStreamMode(fg.m_ImageHandle, LVM_RUN); // LiveMode_RUN
		if (result != 0)
		{
			PLOGI.printf("Failed to set live stream mode. Error code: %d", result);
			return;
		}
		liveFrameThreadRunning = true;
		liveFrameThreadHandle = thread(&TCPSocket::LiveFrameThread, this, ref(fg));
	}
}

void TCPSocket::StopLiveFrameThread(FrameGrabber& fg) {
	if (liveFrameThreadRunning) { 
		liveFrameThreadRunning = false;
		ERRTYPE result = eHD_LiveStreamMode(fg.m_ImageHandle, LVM_STOP); // LiveMode_STOP
		eHD_LiveStreamClose(fg.m_ImageHandle, &fg.m_LiveStreamInfo); 
		if (liveFrameThreadHandle.joinable()) { 
			liveFrameThreadHandle.join(); 
		}
		PLOGI.printf("LiveFrameThread stopped.");
	}
}

void TCPSocket::LiveFrameThread(FrameGrabber& fg) {	
	while (liveFrameThreadRunning && isStarted && fg.portConnection) {
		LiveFrame(fg);
	}
}

void TCPSocket::RefreshLiveStream(FrameGrabber& fg) { 
	ERRTYPE result = eHD_LiveStreamMode(fg.m_ImageHandle, LVM_STOP);
	if (result != 0)return;
	eHD_LiveStreamClose(fg.m_ImageHandle, &fg.m_LiveStreamInfo);
	eHD_ReleaseStreamBuffer(fg.m_ImageHandle, fg.m_LiveStreamInfo.pVidHeaders);
	fg.InitializeLiveStreamInfo();
	result = eHD_LiveStreamInit(fg.m_ImageHandle, &fg.m_LiveStreamInfo);
	if (result != 0)return;
	result = eHD_LiveStreamMode(fg.m_ImageHandle, LVM_RUN);
	if (result != 0)return;
	PLOGI.printf("RefreshLiveStream");
}

// --------------------------------Snap 관련 함수--------------------------------------//

void TCPSocket::StartSnapFrameThread(FrameGrabber& fg) {
	if (!snapFrameThreadRunning) {
		snapFrameThreadRunning = true;
		snapFrameThreadHandle = thread(&TCPSocket::SnapFrameThread, this, ref(fg));
		PLOGI.printf("SnapFrameThread started.");
	}
}


void TCPSocket::StopSnapFrameThread() {
	if (snapFrameThreadRunning) {
		snapFrameThreadRunning = false;
		if (snapFrameThreadHandle.joinable()) {
			snapFrameThreadHandle.join();
		}
		PLOGI.printf("SnapFrameThread stopped.");
	}
}

void TCPSocket::SnapFrameThread(FrameGrabber& fg) { 
	while (snapFrameThreadRunning && isStarted && fg.portConnection) { 
		SnapFrame(fg); 
	}
}

void TCPSocket::SnapFrame(FrameGrabber& fg) {

	int offset = IMAGE_HEADER_SIZE;
	ERRTYPE e = eHD_SnapToBuffer(fg.m_ImageHandle, &fg.sc);
	if (e) {
		PLOGI.printf("Failed to snap frame");
		//exit(0);
		isStarted = false;
		return;
	}
	memcpy(sendBuffer + offset, fg.sc.pRecvBuf, fg.lHeight * fg.lWidth * fg.wBitsPerPixel / 8 * sizeof(uchar));
	offset += fg.lHeight * fg.lWidth * fg.wBitsPerPixel / 8 * sizeof(uchar);
	checkSum = CalcCheckSum(sendBuffer, offset);
	memcpy(sendBuffer + offset, &checkSum, sizeof(checkSum));
	offset += sizeof(checkSum);
	memcpy(sendBuffer + offset, &eof, sizeof(eof));
	offset += sizeof(eof);

	int sendResult = send(clientSocket, sendBuffer, imagePacketSize, 0);
	if (sendResult == SOCKET_ERROR)
	{
		PLOGI.printf("Failed to send data to client. Error code: %d", WSAGetLastError());
		//exit(0);
		return;
	}
}