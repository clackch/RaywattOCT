#define  _CRT_SECURE_NO_WARNINGS

#include "FGServer.h"

#define IMAGE_HEADER_SIZE 7
#define IMAGE_END_SIZE 2

enum TestColor {
	testColor = 0x50,
	firstSettingColor = 0xFF
};

bool liveThread = true;
bool checkThread = true;

std::string folderPath = "C:\\Raywatt\\FrameGrabber\\testImage\\";

byte CalcCheckSum(byte* buffer, int size);
void LiveFrameThread(FrameGrabber& fg, TCPSocket& ts);
void SetImg(FrameGrabber& fg);
void CheckClientThread(TCPSocket& ts);
std::string FileFormat(int number);

int main(int argc, char** argv)
{
	TCPSocket ts = TCPSocket();
	ts.ConnectClient();
	printf("FGServer Start\n");

	FrameGrabber fg = FrameGrabber();

	cv::Mat image = cv::imread(FileFormat(fg.currentIMG));
	if (image.empty()) {
		std::cout << "Read Image Failed." << std::endl;
	}
	fg.origin_Height = image.rows;
	fg.origin_Width = image.cols;
	fg.lHeight = fg.origin_Height;
	fg.lWidth = fg.origin_Width;
	fg.wBitsPerPixel = image.step.buf[1] * 8;

	ts.SetImagePacketHeader(&fg);
	fg.originIMG = new unsigned char[fg.origin_Width * fg.origin_Height * (fg.wBitsPerPixel / 8)];

	std::thread live(&LiveFrameThread, std::ref(fg), std::ref(ts));
	printf("Start live thread\n");

	std::thread check(&CheckClientThread, std::ref(ts));
	printf("Start check thread\n");

	live.join();
	check.join();
}

byte CalcCheckSum(byte* buffer, int size) {
	size--;
	byte csum = 0;
	for (; size >= 0; size--) {
		csum += buffer[size];
	}
	return (byte)~csum;
}

FrameGrabber::FrameGrabber() {
	portConnection = true;
	boardConnection = true;


}

FrameGrabber::~FrameGrabber()
{
}



void LiveFrameThread(FrameGrabber& fg, TCPSocket& ts) {

	while (liveThread) {
		ts.ReceivePacket(fg);
		if (ts.start) {
			SetImg(fg);
			if (fg.portConnection) {
				ts.SnapFrame(fg);
			}
		}
	}
	free(fg.pRecvBuf);
}

std::string FileFormat(int number) {
	std::stringstream ss;
	ss << "Angio_" << std::setw(3) << std::setfill('0') << number << ".png";
	return folderPath + ss.str();
}

void SetImg(FrameGrabber& fg) {
	if (fg.currentIMG >= 31) fg.currentIMG = 0;
	else fg.currentIMG++;

	cv::Mat image = cv::imread(FileFormat(fg.currentIMG));
	if (image.empty()) {
		std::cout << "Read Image Failed." << std::endl;
		return;
	}
	fg.origin_Height = image.rows;
	fg.origin_Width = image.cols;

	int index = 0;
	for (int i = fg.origin_Height-1; i >= 0; --i) {
		for (int j = 0; j < fg.origin_Width; ++j) {
			cv::Vec3b pixel = image.at<cv::Vec3b>(i, j);
			fg.originIMG[index++] = pixel[0]; // Blue 채널
			fg.originIMG[index++] = pixel[1]; // Green 채널
			fg.originIMG[index++] = pixel[2]; // Red 채널
		}
	}
	
	index = (fg.origin_Height - fg.HeightPorch - fg.lHeight) * fg.wBitsPerPixel/8 * fg.origin_Width;
	int sendIndex = 0;
	for (int i = 0; i < fg.lHeight; ++i) {
		index += fg.WidthPorch * fg.wBitsPerPixel/8;
		for (int j = 0; j < fg.lWidth * fg.wBitsPerPixel / 8; ++j) {
			fg.pRecvBuf[sendIndex++] = fg.originIMG[index++];
		}
		index += (fg.origin_Width - fg.lWidth - fg.WidthPorch) * fg.wBitsPerPixel/8;
	}
}

TCPSocket::TCPSocket() {
	sof = 0x3A;
	type = 0x00;
	checkSum = 0x00;
	eof = 0xA3;

	isConnected = false;

	start = false;

	tmpRecvBufferLen = 0;

	clientSocket = INVALID_SOCKET;

	timeout.tv_sec = 0; // timeout 0s 0ms = 즉시 반환
	timeout.tv_usec = 0;

	if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
	{
		std::cout << "failed to initialize winsock. Error code: " << WSAGetLastError() << std::endl;
		WSACleanup();
	}

	serverSocket = socket(AF_INET, SOCK_STREAM, 0);
	if (serverSocket == INVALID_SOCKET)
	{
		std::cout << "failed to create socket. Error code: " << WSAGetLastError() << std::endl;
		closesocket(serverSocket);
		WSACleanup();
	}

	serverAddress.sin_family = AF_INET;
	serverAddress.sin_port = htons(8888); // host to network short
	serverAddress.sin_addr.s_addr = INADDR_ANY;

	if (bind(serverSocket, (struct sockaddr*)&serverAddress, sizeof(serverAddress)) == SOCKET_ERROR)
	{
		std::cout << "Failed to bind socket. Error code: " << WSAGetLastError() << std::endl;
		closesocket(serverSocket);
		WSACleanup();
	}

	listen(serverSocket, 1);
}

TCPSocket::~TCPSocket()
{
	closesocket(clientSocket);
	closesocket(serverSocket);
	WSACleanup();
}

void TCPSocket::SetDeviceInfoPacket(FrameGrabber& fg, char* buffer) {
	int offset = 0;
	char packetType = PacketType::Command;
	char commandType = CommandType::FGDeviceInfo;

	memcpy(buffer + offset++, &sof, sizeof(sof));
	memcpy(buffer + offset++, &packetType, sizeof(packetType));
	memcpy(buffer + offset++, &commandType, sizeof(commandType));
	memcpy(buffer + offset, &fg.lHeight, sizeof(fg.lHeight));
	offset += sizeof(fg.lHeight);
	memcpy(buffer + offset, &fg.lWidth, sizeof(fg.lWidth));
	offset += sizeof(fg.lWidth);
	memcpy(buffer + offset++, &fg.wBitsPerPixel, sizeof(fg.wBitsPerPixel));
	checkSum = CalcCheckSum((byte*)buffer, 8);
	memcpy(buffer + offset++, &checkSum, sizeof(checkSum));
	memcpy(buffer + offset++, &eof, sizeof(eof));
}

void TCPSocket::SetCommandPacket(char commandType) {
	type = PacketType::Command;

	memcpy(commandBuffer, &sof, sizeof(sof));
	memcpy(commandBuffer + 1, &type, sizeof(type));
	memcpy(commandBuffer + 2, &commandType, sizeof(commandType));

	checkSum = CalcCheckSum((byte*)commandBuffer, 3);
	memcpy(commandBuffer + 3, &checkSum, sizeof(checkSum));
	memcpy(commandBuffer + 4, &eof, sizeof(eof));
}

void TCPSocket::SetDeviceInfoCommandPacket(FrameGrabber& fg, char* commandDeviceInfoBuffer) {
	int offset = 0;
	char packetType = PacketType::Command;
	char commandType = CommandType::FGDeviceInfo;

	memcpy(commandDeviceInfoBuffer + offset++, &sof, sizeof(sof));
	memcpy(commandDeviceInfoBuffer + offset++, &packetType, sizeof(packetType));
	memcpy(commandDeviceInfoBuffer + offset++, &commandType, sizeof(commandType));
	memcpy(commandDeviceInfoBuffer + offset, &fg.lHeight, sizeof(fg.lHeight));
	offset += sizeof(fg.lHeight);
	memcpy(commandDeviceInfoBuffer + offset, &fg.lWidth, sizeof(fg.lWidth));
	offset += sizeof(fg.lWidth);
	memcpy(commandDeviceInfoBuffer + offset++, &fg.wBitsPerPixel, sizeof(fg.wBitsPerPixel));
	checkSum = CalcCheckSum((byte*)commandDeviceInfoBuffer, 8);
	memcpy(commandDeviceInfoBuffer + offset++, &checkSum, sizeof(checkSum));
	memcpy(commandDeviceInfoBuffer + offset++, &eof, sizeof(eof));
}

void TCPSocket::SetImagePacketHeader(FrameGrabber *fg) {
	imagePacketSize = IMAGE_HEADER_SIZE + fg->lHeight * fg->lWidth * fg->wBitsPerPixel / 8 + IMAGE_END_SIZE;

	fg->pRecvBuf = new unsigned char[fg->lWidth * fg->lHeight * (fg->wBitsPerPixel / 8)];
	memset(fg->pRecvBuf, 0xFF, fg->lWidth * fg->lHeight * (fg->wBitsPerPixel / 8) * sizeof(uchar));
	buffer = (byte*)malloc(imagePacketSize);

	int offset = 0;
	type = PacketType::Image;
	memcpy(buffer + offset, &sof, sizeof(sof));
	offset += sizeof(sof);
	memcpy(buffer + offset, &type, sizeof(type));
	offset += sizeof(type);
	memcpy(buffer + offset, &fg->lHeight, sizeof(fg->lHeight));
	offset += sizeof(fg->lHeight);
	memcpy(buffer + offset, &fg->lWidth, sizeof(fg->lWidth));
	offset += sizeof(fg->lWidth);
	memcpy(buffer + offset, &fg->wBitsPerPixel, sizeof(fg->wBitsPerPixel));
	offset += sizeof(fg->wBitsPerPixel);
}

ERRTYPE FrameGrabber::ReadFormatFile(char* m_CHPFilePath) {
	ERRTYPE e = 0;
	DWORD dwBoardCaps;

	std::basic_string<TCHAR> configFilePath = m_CHPFilePath;
	TCHAR sIniValueString[MAX_PATH] = _T("");
	// [Imaging]
	DWORD Image_Height = ::GetPrivateProfileInt(_T("HIDEFPLUS"), _T("Image Height"), 1080, configFilePath.c_str());
	DWORD Image_Width = ::GetPrivateProfileInt(_T("HIDEFPLUS"), _T("Image Width"), 1920, configFilePath.c_str());

	DWORD Vertical_Back_Porch = ::GetPrivateProfileInt(_T("HIDEFPLUS"), _T("Vertical Back Porch"), 0, configFilePath.c_str());
	DWORD Horizontal_Back_Porch = ::GetPrivateProfileInt(_T("HIDEFPLUS"), _T("Horizontal Back Porch"), 8, configFilePath.c_str());
	if (origin_Height < Image_Height + Vertical_Back_Porch) {
		std::cout << "This chp file's height is invalid" << std::endl;
		e = 1;
		return e;
	}
	else if (origin_Width < Image_Width + Horizontal_Back_Porch){
		std::cout << "This chp file's width is invalid" << std::endl;
		e = 1;
		return e;
	}
	else {
		m_RSet.lRegisters.lImageHeight = Image_Height;
		m_RSet.lRegisters.lImageWidth = Image_Width;
		m_RSet.lRegisters.lVerticalBackPorch = Vertical_Back_Porch;
		m_RSet.lRegisters.lHorizontalBackPorch = Horizontal_Back_Porch;

		lHeight = Image_Height;
		lWidth = Image_Width;
		HeightPorch = Vertical_Back_Porch;
		WidthPorch = Horizontal_Back_Porch;
		return e;
	}
	
}

void CheckClientThread(TCPSocket& ts) {
	int error = 0;
	socklen_t len = sizeof(error);
	while (checkThread) {
		const char* empty = "";
		int emptySize = 0;
		int sendResult = send(ts.clientSocket, empty, emptySize, 0);
		if (sendResult == SOCKET_ERROR)
		{
			exit(0);
		}
	}
}

void TCPSocket::ChpFilePacketProcess(FrameGrabber* fg) {
	short packetLen = tmpRecvBuffer[3];
	int sendResult;

	fg->chpFileName = std::string(tmpRecvBuffer + 4, packetLen - 6);
	std::cout << "Chp File Name: " << fg->chpFileName << std::endl;

	ERRTYPE e;
	if (fg->chpFileName.substr(0, 6) == "setup\\")
		e = fg->ReadFormatFile((char*)(fg->chpFilePath + fg->chpFileName).c_str());
	else
		e = fg->ReadFormatFile((char*)(fg->chpFilePath + "app\\" + fg->chpFileName).c_str());

	if (e) {
		std::cout << ".chp file is invalid" << std::endl;
		SetCommandPacket(CommandType::FGFailChangeChp);
		sendResult = send(clientSocket, commandBuffer, 5, 0);
	}
	else {
		std::cout << "Success to read .chp file" << std::endl;
		free(fg->pRecvBuf);
		SetCommandPacket(CommandType::FGSuccessChangeChp);
		sendResult = send(clientSocket, commandBuffer, 5, 0);

		char deviceInfoBuffer[10] = { 0 };
		SetDeviceInfoPacket(*fg, deviceInfoBuffer);
		sendResult = send(clientSocket, deviceInfoBuffer, 10, 0);
		std::cout << "Send Device Info" << std::endl;
	}

	tmpRecvBufferLen -= packetLen;
	memmove(tmpRecvBuffer, tmpRecvBuffer + packetLen, tmpRecvBufferLen);
	tmpRecvBuffer[tmpRecvBufferLen] = '\0';

	SetImagePacketHeader(fg);
}

/*
* ConnectClient
* int arg: 0이면 초기 연결, 1이면 연결 재시도
*/
void TCPSocket::ConnectClient(int arg) {
	if (arg == 1) {
		closesocket(clientSocket);
		clientSocket = INVALID_SOCKET;
		isConnected = false;

		// Stop Thread
		liveThread = false;
		checkThread = false;
	}
	while (!isConnected) {
		clientSocket = accept(serverSocket, NULL, NULL);
		if (clientSocket == INVALID_SOCKET)
		{
			std::cout << "Connecting to client... Error code: " << WSAGetLastError() << std::endl;

		}
		else {
			isConnected = true;

			// Start Thread
			liveThread = true;
			checkThread = true;
		}
		std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	}
}

void TCPSocket::SnapFrame(FrameGrabber& fg) {

	int offset = IMAGE_HEADER_SIZE;


	if (start == true) {
		
		memcpy(buffer + offset, fg.pRecvBuf, fg.lHeight * fg.lWidth * fg.wBitsPerPixel / 8 * sizeof(uchar));
		offset += fg.lHeight * fg.lWidth * fg.wBitsPerPixel / 8 * sizeof(uchar);
		checkSum = CalcCheckSum(buffer, offset);
		memcpy(buffer + offset, &checkSum, sizeof(checkSum));
		offset += sizeof(checkSum);
		memcpy(buffer + offset, &eof, sizeof(eof));
		offset += sizeof(eof);


		int sendResult = send(clientSocket, (char*)buffer, imagePacketSize, 0);
		if (sendResult == SOCKET_ERROR)
		{
			exit(0);
		}
	}
}

void TCPSocket::ReceivePacket(FrameGrabber& fg) {
	FD_ZERO(&readSet);
	FD_SET(clientSocket, &readSet);

	selectResult = select(clientSocket + 1, &readSet, NULL, NULL, &timeout);
	if (selectResult > 0) {
		int bytesReceived = recv(clientSocket, recvBuffer, 100, 0);

		if (bytesReceived == SOCKET_ERROR)
		{
			exit(0);
		}
		else
		{
			int sendResult;
			strcat(tmpRecvBuffer, recvBuffer);
			memset(recvBuffer, '\0', strlen(recvBuffer));
			tmpRecvBufferLen += bytesReceived;
			while (true) {
				CommandType type = CheckCommandType(tmpRecvBuffer);
				switch (type) {
				case CommandType::FGStarted:
					std::cout << "FGStarted" << std::endl;
					tmpRecvBufferLen -= 5;
					memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
					tmpRecvBuffer[tmpRecvBufferLen] = '\0';
					start = true;
					break;
				case CommandType::FGStopped:
					std::cout << "FGStopped" << std::endl;
					tmpRecvBufferLen -= 5;
					memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
					tmpRecvBuffer[tmpRecvBufferLen] = '\0';
					start = false;
					break;
				case CommandType::FGAskPort:
					std::cout << "FGAskPort" << std::endl;
					tmpRecvBufferLen -= 5;
					memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
					tmpRecvBuffer[tmpRecvBufferLen] = '\0';

					SetCommandPacket(CommandType::FGAngioConnected);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					break;
				case CommandType::FGAskBoard:
					std::cout << "FGAskBoard" << std::endl;
					tmpRecvBufferLen -= 5;
					memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
					tmpRecvBuffer[tmpRecvBufferLen] = '\0';

					SetCommandPacket(CommandType::FGBoardExist);
					sendResult = send(clientSocket, commandBuffer, 5, 0);
					break;
				case CommandType::FGAskDeviceInfo:
					std::cout << "FGAskDeviceInfo" << std::endl;
					tmpRecvBufferLen -= 5;
					memmove(tmpRecvBuffer, tmpRecvBuffer + 5, tmpRecvBufferLen);
					tmpRecvBuffer[tmpRecvBufferLen] = '\0';
					char commandDeviceInfoBuffer[10];

					SetDeviceInfoCommandPacket(fg, commandDeviceInfoBuffer);
					sendResult = send(clientSocket, commandDeviceInfoBuffer, 10, 0);
					break;
				case CommandType::FGChpFile:
					std::cout << "FGChpFile" << std::endl;
					ChpFilePacketProcess(&fg);
					break;
				}
				if (type == CommandType::FGNothing)
					break;
			}
		}
	}
}

CommandType TCPSocket::CheckCommandType(const char* tmpRecvBuffer) {
	if (tmpRecvBuffer[0] == (char)0x3A) {
		if (tmpRecvBuffer[1] == PacketType::Command) {
			if (tmpRecvBuffer[2] == CommandType::FGChpFile) {
				short packetLen = tmpRecvBuffer[3];
				if (tmpRecvBuffer[packetLen - 2] == (char)CalcCheckSum((byte*)tmpRecvBuffer, packetLen - 2)) {
					if (tmpRecvBuffer[packetLen - 1] == (char)0xA3) {
						return CommandType::FGChpFile;
					}
				}
			}
			else {
				if (tmpRecvBuffer[3] == (char)CalcCheckSum((byte*)tmpRecvBuffer, 3)) {
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
