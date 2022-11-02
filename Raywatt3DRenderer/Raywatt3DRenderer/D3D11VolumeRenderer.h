#pragma once

#include "DXUT.h"
#include "SDKmisc.h"
#include "DXUTgui.h"
#include <iostream>
#include "DXUTcamera.h"
#include "VolumeTexture.h"
#include "TransferTexture.h"
#include "RenderTarget.h"
#include "CubeObject.h"
#include "ScreenPlaneObject.h"
#include "RaycastVolumeRenderingEffect.h"
#include "BackgroundRenderingEffect.h"
#include "TransferFunc.h"

#include <d3dx11effect.h>

#pragma warning( disable : 4100 )

#pragma comment(lib, "d3dcompiler.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "winmm.lib")
#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "usp10.lib")
#pragma comment(lib, "imm32.lib")
#pragma comment(lib, "version.lib")

using namespace DirectX;

struct CBChangesEveryFrame
{
	XMFLOAT4X4 mWorldViewProj;
};

void InitVolumeData(int width, int height, int frames, BYTE* data);

bool CALLBACK IsD3D11DeviceAcceptable(const CD3D11EnumAdapterInfo* AdapterInfo, UINT Output, const CD3D11EnumDeviceInfo* DeviceInfo,
	DXGI_FORMAT BackBufferFormat, bool bWindowed, void* pUserContext);

bool CALLBACK ModifyDeviceSettings(DXUTDeviceSettings* pDeviceSettings, void* pUserContext);

HRESULT CALLBACK OnD3D11CreateDevice(ID3D11Device* pd3dDevice, const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc,	void* pUserContext);

HRESULT CALLBACK OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
	const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext);

void CALLBACK OnFrameMove(double fTime, float fElapsedTime, void* pUserContext);

void CALLBACK OnD3D11FrameRender(ID3D11Device* pd3dDevice, ID3D11DeviceContext* pd3dImmediateContext,
	double fTime, float fElapsedTime, void* pUserContext);

void CALLBACK OnD3D11FrameRender(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pRenderTargetView);

void CALLBACK OnD3D11ReleasingSwapChain(void* pUserContext);

void CALLBACK OnD3D11DestroyDevice(void* pUserContext);

LRESULT CALLBACK MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam, bool* pbNoFurtherProcessing, void* pUserContext);

void CALLBACK OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext);

void CALLBACK OnMouse(bool bLeftButtonDown, bool bRightButtonDown, bool bMiddleButtonDown,
	bool bSideButton1Down, bool bSideButton2Down, int nMouseWheelDelta,
	int xPos, int yPos, void* pUserContext);

bool CALLBACK OnDeviceRemoved(void* pUserContext);