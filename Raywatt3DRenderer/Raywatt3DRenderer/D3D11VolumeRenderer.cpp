
#include "D3D11VolumeRenderer.h"

CModelViewerCamera								g_Camera;

ID3D11Device* g_pD3D11Device;
IDXGISwapChain* g_pSwapChain;
DXGI_SURFACE_DESC* g_pBackBufferSurfaceDesc;
void* g_pUserContext;

std::shared_ptr<VolumeTexture>					g_pVolumeTexture;
std::shared_ptr<TransferTexture>				g_pTransferTexture;
std::shared_ptr<CubeObject>						g_pCubeObject;
std::shared_ptr<RaycastVolumeRenderingEffect>	g_pRaycastVolumeRenderingEffect;
std::shared_ptr<BackgroundRenderingEffect>		g_pBackgroundRenderingEffect;
std::shared_ptr<RenderTarget>					g_pRenderTarget;

void InitVolumeData(int width, int height, int frames, BYTE* data) {
	VolumeDataInfo datainfo;
	HRESULT hr;

	// 볼륨 데이터 정보 입력 부분
	datainfo.width = width;
	datainfo.height = height;
	datainfo.depth = frames;

	g_pCubeObject = std::make_shared<CubeObject>();
	hr = g_pCubeObject->Initialize(g_pD3D11Device, datainfo, &g_Camera);
	printf("CubeObject Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());
	g_pCubeObject->Translate(XMFLOAT3(0, 0, g_pCubeObject->GetRatio().z * 2));
	/*
	void Translate(XMFLOAT3 dist); cube 이동
	void DeltaRotate(XMFLOAT3 rot); cube 조금씩 회전 (delta time만큼)
	void DeltaTranslate(XMFLOAT3 dist); cube 조금씩 이동 (delta time만큼)
	*/

	g_pVolumeTexture = std::make_shared<VolumeTexture>();
	hr = g_pVolumeTexture->Initialize(g_pD3D11Device, datainfo, data);
	printf("VolumeTexture Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());

	g_pRaycastVolumeRenderingEffect = std::make_shared<RaycastVolumeRenderingEffect>();
	hr = g_pRaycastVolumeRenderingEffect->Initialize(g_pD3D11Device, g_pVolumeTexture, g_pTransferTexture);
	printf("RaycastVolumeRenderingEffect Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());
	g_pRaycastVolumeRenderingEffect->SetParams(&g_Camera, g_pCubeObject);

	hr = g_pRaycastVolumeRenderingEffect->OnD3D11ResizedSwapChain(g_pD3D11Device, g_pSwapChain, g_pBackBufferSurfaceDesc, g_pUserContext);
	printf("RaycastVolumeRenderingEffect OnD3D11ResizedSwapChain [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());
}

bool CALLBACK IsD3D11DeviceAcceptable(const CD3D11EnumAdapterInfo* AdapterInfo, UINT Output, const CD3D11EnumDeviceInfo* DeviceInfo,
	DXGI_FORMAT BackBufferFormat, bool bWindowed, void* pUserContext)
{
	return true;
}

bool CALLBACK ModifyDeviceSettings(DXUTDeviceSettings* pDeviceSettings, void* pUserContext)
{
	return true;
}

HRESULT CALLBACK OnD3D11CreateDevice(ID3D11Device* pd3dDevice, const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc,
	void* pUserContext)
{
	HRESULT hr = S_OK;

	g_pD3D11Device = pd3dDevice;
	printf("OnD3D11CreateDevice : 0x%x\n", pd3dDevice);

	//auto pd3dImmediateContext = DXUTGetD3D11DeviceContext();

	static const XMVECTORF32 s_Eye = { 0.0f, 0.0f, 0.0f, 0.f };
	static const XMVECTORF32 s_At = { 0.0f, 0.0f, 1.0f, 0.f };

	g_Camera.SetViewParams(s_Eye, s_At);

	g_pTransferTexture = std::make_shared<TransferTexture>();
	std::vector<std::shared_ptr<ControlNode>> controlPoints;

	/*
	컨트롤 포인트 입력 부분
	*/
	controlPoints.push_back(std::make_shared<ControlNode>(15.f / 255.f, 0, 0, 0, 0));
	controlPoints.push_back(std::make_shared<ControlNode>(255.f / 255.f, 255, 255, 255, 255));
	hr = g_pTransferTexture->Initialize(pd3dDevice, controlPoints);
	printf("TransferTexture Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());

	// 배경색 부분
	g_pBackgroundRenderingEffect = std::make_shared<BackgroundRenderingEffect>();
	hr = g_pBackgroundRenderingEffect->Initialize(pd3dDevice, Utility::GetColorFloat4(0, 0, 0), Utility::GetColorFloat4(80, 80, 80));
	printf("BackgroundRenderingEffect Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());

	g_pRenderTarget = std::make_shared<RenderTarget>();

	return hr;
}

HRESULT CALLBACK OnD3D11ResizedSwapChain(ID3D11Device* pd3dDevice, IDXGISwapChain* pSwapChain,
	const DXGI_SURFACE_DESC* pBackBufferSurfaceDesc, void* pUserContext)
{
	HRESULT hr;

	printf("OnD3D11ResizedSwapChain : 0x%x\n", pd3dDevice);
	float fAspect = static_cast<float>(pBackBufferSurfaceDesc->Width) / static_cast<float>(pBackBufferSurfaceDesc->Height);
	g_Camera.SetProjParams(XM_PI * 0.25f, fAspect, 0.1f, 1000.f);
	g_Camera.SetWindow(pBackBufferSurfaceDesc->Width, pBackBufferSurfaceDesc->Height);
	g_Camera.SetButtonMasks(MOUSE_MIDDLE_BUTTON, MOUSE_WHEEL, MOUSE_LEFT_BUTTON);

	g_pRenderTarget->Destroy();
	hr = g_pRenderTarget->Initialize(pd3dDevice, pBackBufferSurfaceDesc);
	printf("RenderTarget Initialize [0x%x] : %s\n", hr, std::system_category().message(hr).c_str());

	g_pSwapChain = pSwapChain;
	g_pBackBufferSurfaceDesc = (DXGI_SURFACE_DESC *) pBackBufferSurfaceDesc;
	g_pUserContext = pUserContext;

	return S_OK;
}

void CALLBACK OnFrameMove(double fTime, float fElapsedTime, void* pUserContext)
{
	g_Camera.FrameMove(fElapsedTime);
	g_pRaycastVolumeRenderingEffect->Update(fElapsedTime);
}

void CALLBACK OnD3D11FrameRender(ID3D11Device* pd3dDevice, ID3D11DeviceContext* pd3dImmediateContext,
	double fTime, float fElapsedTime, void* pUserContext)
{
	ID3D11RenderTargetView* pRTV = DXUTGetD3D11RenderTargetView();
	ID3D11DepthStencilView* pDSV = DXUTGetD3D11DepthStencilView();

	g_pRaycastVolumeRenderingEffect->Draw(pd3dImmediateContext, g_pRenderTarget->GetRenderTargetView(), pDSV);
	g_pBackgroundRenderingEffect->SetParam(g_pRenderTarget->GetShaderResourceView())->Draw(pd3dImmediateContext, pRTV, pDSV);
}

void CALLBACK OnD3D11FrameRender(ID3D11DeviceContext* pd3dImmediateContext, ID3D11RenderTargetView* pRenderTargetView) {
	ID3D11RenderTargetView* pRTV = DXUTGetD3D11RenderTargetView();
	ID3D11DepthStencilView* pDSV = DXUTGetD3D11DepthStencilView();

	g_pRaycastVolumeRenderingEffect->Draw(pd3dImmediateContext, pRenderTargetView, pDSV);
	g_pBackgroundRenderingEffect->Draw(pd3dImmediateContext, pRTV, pDSV);
}

void CALLBACK OnD3D11ReleasingSwapChain(void* pUserContext)
{
}

void CALLBACK OnD3D11DestroyDevice(void* pUserContext)
{
	g_pVolumeTexture.reset();
	g_pCubeObject.reset();
	g_pRaycastVolumeRenderingEffect.reset();
	g_pBackgroundRenderingEffect.reset();
	g_pTransferTexture.reset();
	g_pRenderTarget.reset();
}

LRESULT CALLBACK MsgProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam,
	bool* pbNoFurtherProcessing, void* pUserContext)
{
	if (g_pCubeObject != nullptr)
		g_pCubeObject->MsgProc(hWnd, uMsg, wParam, lParam, pbNoFurtherProcessing, pUserContext);
	return 0;
}

void CALLBACK OnKeyboard(UINT nChar, bool bKeyDown, bool bAltDown, void* pUserContext)
{
	g_pCubeObject->OnKeyboard(nChar, bKeyDown, bAltDown, pUserContext);
}

void CALLBACK OnMouse(bool bLeftButtonDown, bool bRightButtonDown, bool bMiddleButtonDown,
	bool bSideButton1Down, bool bSideButton2Down, int nMouseWheelDelta,
	int xPos, int yPos, void* pUserContext)
{

}

bool CALLBACK OnDeviceRemoved(void* pUserContext)
{
	return true;
}