#include "FGServer.h"
#include "FrameGrabber.h"
#include "TCPSocket.h"

FrameGrabber fg;
TCPSocket* ts;

int main(int argc, char** argv)
{
	// 로그 파일 이름 설정
	time_t timer = time(nullptr);
	tm t;
	errno_t err = localtime_s(&t, &timer);

	char logFile[_MAX_PATH];
	sprintf(logFile, "C:\\Raywatt\\log\\FGserver_%d-%02d-%02d.log", (t.tm_year + 1900), (t.tm_mon + 1), t.tm_mday);

	// 로그 파일 초기화
	plog::init(plog::info, new plog::RollingFileAppender<plog::TxtFormatter>(logFile, 1024 * 1024 * 10, 5));

	ts = new TCPSocket();
	ts->ConnectClient(fg);
	PLOGI.printf("FGServer Start");

	fg = FrameGrabber();

	FGError error = fg.InitBoard();
	if (error) {
		PLOGI.printf("Failed to init board");
		fg.boardConnection = false;


	}
	else {

		PLOGI.printf("Success to init board");
		fg.boardConnection = true;

		char initChpFileName[] = R"(C:\Program Files\Foresight\IDEA\Chp\VESACHP\DVI.chp)";
		ERRTYPE e = fg.ReadFormatFile(initChpFileName);

		if (e) {
			PLOGI.printf(".chp file is missing");
			return e;
		}
		else {
			PLOGI.printf("Success to read .chp file");
			fg.CreateFromFG();
		}
	}

	fg.CheckPortConnection();

	ts->StartInitThreads(fg);
}