// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.

#include "Raywatt3DRenderer.h"
#include "D3D11VolumeRenderer.h"
#include "DXUT.h"
#include <thread>
#include <system_error>

_declspec(dllexport) void InitializeDevice(void* pDevice, void* pContext, void* hWnd, int width, int height) {
	HRESULT hr;

	hr = DXUTInit(true, true, nullptr); // Parse the command line, show msgboxes on error, no extra command line params
	printf("DXUTInit (0x%x) %s\n", hr, std::system_category().message(hr).c_str());
	
	DXUTSetCursorSettings(true, true); // Show the cursor and clip it when in full screen	
		
	hr = DXUTSetWindow((HWND) hWnd, (HWND)hWnd, (HWND)hWnd);
	printf("DXUTSetWindow (0x%x) %s\n", hr, std::system_category().message(hr).c_str());

	hr = DXUTSetDevice((ID3D11Device*)pDevice, (ID3D11DeviceContext*)pContext, true, width, height);

	DXUTPause(false, false);
}

_declspec(dllexport) void CreateVolumeData(int width, int height, int frames, char* data) {
	printf("CreateVolumeData : %d %d %d\n", width, height, frames);
	InitVolumeData(width, height, frames, (BYTE*)data);
}

_declspec(dllexport) void RenderVolumeData(void* pDevice, void* pImmediateContext, void* pRenderTargetView) {
	OnD3D11FrameRender((ID3D11DeviceContext*)pImmediateContext, (ID3D11RenderTargetView *)pRenderTargetView);
	OnFrameMove(0, 0.005f, nullptr);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
		AllocConsole();
		freopen("CONOUT$", "w", stdout);
		// Set general DXUT callbacks
		DXUTSetCallbackFrameMove(OnFrameMove);
		DXUTSetCallbackKeyboard(OnKeyboard);
		DXUTSetCallbackMouse(OnMouse);
		DXUTSetCallbackMsgProc(MsgProc);
		DXUTSetCallbackDeviceChanging(ModifyDeviceSettings);
		DXUTSetCallbackDeviceRemoved(OnDeviceRemoved);

		// Set the D3D11 DXUT callbacks. Remove these sets if the app doesn't need to support D3D11
		DXUTSetCallbackD3D11DeviceAcceptable(IsD3D11DeviceAcceptable);
		DXUTSetCallbackD3D11DeviceCreated(OnD3D11CreateDevice);
		DXUTSetCallbackD3D11SwapChainResized(OnD3D11ResizedSwapChain);
		DXUTSetCallbackD3D11FrameRender(OnD3D11FrameRender);
		DXUTSetCallbackD3D11SwapChainReleasing(OnD3D11ReleasingSwapChain);
		DXUTSetCallbackD3D11DeviceDestroyed(OnD3D11DestroyDevice);
		break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

