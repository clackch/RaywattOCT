#pragma once
#pragma comment(lib, "ws2_32.lib")

#include <iostream>
#include <WinSock2.h>
#include <string>
#include <opencv2/opencv.hpp>
#include <Windows.h>
#include <chrono>
#include <thread>

#include "hdp_lib.h"
#include "FSILiveDisplay.h"
#include "shlobj_core.h"
#include "string.h"

#include <plog/Log.h>
#include "plog/Initializers/RollingFileInitializer.h"

using namespace std;

#define IMAGE_HEADER_SIZE 15
#define IMAGE_TAIL_SIZE 2