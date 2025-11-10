// pch.h: 미리 컴파일된 헤더 파일입니다.
// 아래 나열된 파일은 한 번만 컴파일되었으며, 향후 빌드에 대한 빌드 성능을 향상합니다.
// 코드 컴파일 및 여러 코드 검색 기능을 포함하여 IntelliSense 성능에도 영향을 미칩니다.
// 그러나 여기에 나열된 파일은 빌드 간 업데이트되는 경우 모두 다시 컴파일됩니다.
// 여기에 자주 업데이트할 파일을 추가하지 마세요. 그러면 성능이 저하됩니다.

#ifndef PCH_H
#define PCH_H

// 여기에 미리 컴파일하려는 헤더 추가
#include "framework.h"

// linking libraries
#pragma comment(lib, "AtsApi.lib")
#pragma comment(lib, "ippac.lib")
#pragma comment(lib, "ippi.lib")
#pragma comment(lib, "ipps.lib")
#pragma comment(lib, "ippvm.lib")
#pragma comment(lib, "ippcore.lib")
#ifndef _DEBUG
#pragma comment (lib, "opencv_core460")
#pragma comment (lib, "opencv_highgui460")
#pragma comment (lib, "opencv_imgproc460")
#pragma comment (lib, "opencv_imgcodecs460")
#pragma comment (lib, "opencv_videoio460")
#else
#pragma comment (lib, "opencv_core460d")
#pragma comment (lib, "opencv_highgui460d")
#pragma comment (lib, "opencv_imgproc460d")
#pragma comment (lib, "opencv_imgcodecs460d")
#pragma comment (lib, "opencv_videoio460d")
#endif
#pragma comment(lib, "PX14_64.lib")
#pragma comment(lib, "PiUsb.lib")
#pragma comment(lib, "libusb-1.0.lib")
#pragma comment(lib, "tiff.lib")

#pragma comment(lib, "RayImaging.lib")
#pragma comment(lib, "RayAcquisition.lib")
#pragma comment(lib, "RayRotaryJunction.lib")

// to use IScopePX14 COM object
[module(type = exe, name = "NewMoria", version = "1.0")];

#endif //PCH_H
