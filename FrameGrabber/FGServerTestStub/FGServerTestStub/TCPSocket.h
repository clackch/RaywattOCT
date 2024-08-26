#include "FGServer.h"
#include <ws2tcpip.h>

class TCPSocket {
public:
	TCPSocket();
	~TCPSocket();

	WSADATA wsaData;
	SOCKET serverSocket;
	sockaddr_in serverAddress;
	SOCKET clientSocket;

	char sof;
	char type;
	byte checkSum;
	char eof;

	int imagePacketSize;
	byte* buffer;

	char commandBuffer[5];

	int imageHeaderSize;

	bool isConnected;

	char commandType;
	bool start;

	char recvBuffer[100];
	char tmpRecvBuffer[200];

	int tmpRecvBufferLen;

	void SnapFrame(FrameGrabber& fg);

	void ConnectClient(int arg = 0);

	void ReceivePacket(FrameGrabber& fg);

	void ChpFilePacketProcess(FrameGrabber* fg);

	CommandType CheckCommandType(const char* receivedBuffer);

	void SetDeviceInfoPacket(FrameGrabber& fg, char* buffer);

	void SetCommandPacket(char commandType);

	void SetDeviceInfoCommandPacket(FrameGrabber& fg, char* commandDeviceInfoBuffer);

	void SetImagePacketHeader(FrameGrabber *fg);

private:
	int selectResult;

	fd_set readSet;
	struct timeval timeout;

};