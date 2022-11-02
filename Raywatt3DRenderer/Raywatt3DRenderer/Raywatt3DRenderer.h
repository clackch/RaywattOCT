#pragma once

extern "C" {
	_declspec(dllexport) void InitializeDevice(void *pDevice, void *pContext, void *hWnd, int width, int height);
	_declspec(dllexport) void FinalizeDevice();
	_declspec(dllexport) void CreateVolumeData(int width, int height, int frames, char* data);
	_declspec(dllexport) void RenderVolumeData(void* pDevice, void *pImmediateContext, void *pRenderTargetView);
}