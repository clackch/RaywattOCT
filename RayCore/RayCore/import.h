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

#pragma comment(lib, "AtsApi")
#pragma comment(lib, "ippac")
#pragma comment(lib, "ippi")
#pragma comment(lib, "ipps")
#pragma comment(lib, "ippvm")
#pragma comment(lib, "libusb-1.0.lib")
#pragma comment (lib, "c10")
#pragma comment (lib, "c10_cuda")
#pragma comment (lib, "caffe2_nvrtc")
#pragma comment (lib, "torch")
#pragma comment (lib, "torch_cuda")
#pragma comment (lib, "torch_cuda_cu")
#pragma comment (lib, "torch_cuda_cpp")
#pragma comment (lib, "torch_cpu")
#pragma comment (lib, "tiff.lib")

#pragma comment(lib, "RayImaging")
#pragma comment(lib, "RayAcquisition")
#pragma comment(lib, "RayRotaryJunction")
#pragma comment(lib, "RayLearning")