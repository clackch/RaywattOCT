#pragma once

#ifndef _DEBUG
#pragma comment (lib, "opencv_core460")
#pragma comment (lib, "opencv_highgui460")
#pragma comment (lib, "opencv_imgproc460")
#pragma comment (lib, "opencv_imgcodecs460")
#else
#pragma comment (lib, "opencv_core460d")
#pragma comment (lib, "opencv_highgui460d")
#pragma comment (lib, "opencv_imgproc460d")
#pragma comment (lib, "opencv_imgcodecs460d")
#endif

#pragma comment(lib, "AtsApi.lib")
#pragma comment(lib, "ippac.lib")
#pragma comment(lib, "ippi.lib")
#pragma comment(lib, "ipps.lib")
#pragma comment(lib, "ippvm.lib")
#pragma comment(lib, "libusb-1.0.lib")
#pragma comment(lib, "RayImaging.lib")
#pragma comment(lib, "RayAcquisition.lib")
#pragma comment(lib, "RayRotaryJunction.lib")