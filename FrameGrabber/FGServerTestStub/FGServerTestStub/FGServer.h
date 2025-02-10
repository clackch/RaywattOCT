#pragma once
#include <iostream>
#include <WinSock2.h>
#pragma comment(lib, "ws2_32.lib")

#pragma once
#ifndef __IPL_H__
typedef unsigned char uchar;
typedef unsigned short ushort;
#endif

#include <Windows.h>
#include "hdp_lib.h"
#include "FSILiveDisplay.h"
#include "shlobj_core.h"
#include "string.h"
#include <chrono>
#include <thread>
#include <tchar.h>

#include "FrameGrabber.h"
#include "TCPSocket.h"
#include <opencv2/opencv.hpp>