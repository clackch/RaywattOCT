#include "pch.h"
#include "ScopeView.h"

// ScopeView
IMPLEMENT_DYNAMIC(ScopeView, CWnd)

ScopeView::ScopeView()
{
}

ScopeView::~ScopeView()
{
	DestroyWindow();

	if(m_scopeControl.p) m_scopeControl.p->Release();
	m_scopeControl.p = NULL;
}

bool ScopeView::Create(CWnd* parent, UINT nID)
{
	CLSID clsid;			// class ID

	// 우리가 시스템 레지스트리에서 ProgID에 대응하는 CLSID를 찾는 과정을 대신해주는 것이다. 
	// 우리는 이 함수를 사용하여 다음과 같이 실행 시에 ProgID에 대응하는 CLSID를 구할 수 있다.
	if (SUCCEEDED(CLSIDFromProgID(PX14_SCOPE_VI_PROGID, &clsid)))				 
	{
		CRect null_rect;

		// 함수는 클래스 참조 매개변수에게 만들 컨트롤 종류를 지시하도록 요청
		if (this->CreateControl(clsid, NULL, WS_CHILD | WS_VISIBLE, null_rect, parent, nID))
		{
			m_scopeControl = this->GetControlUnknown();

			if (!m_scopeControl)
				return false;
		}

		// COM
		// Set scope properties
		ComInterface()->put_MinSampleValue(0L);
		ComInterface()->put_MaxSampleValue(65535L);
	}

	return true;
}

bool ScopeView::AddChannel(LPCTSTR name, int channelSize)
{
	SAFEARRAY *scopeBuffer;
	UINT scopeSrcId;

	// Channel source
	scopeBuffer = SafeArrayCreateVector(VT_UI2, 0, channelSize);

	VARIANTARG varg;
	varg.vt = VT_UI2 | VT_ARRAY;
	varg.parray = scopeBuffer;

	CComBSTR bstrTitle(name);

	if (SUCCEEDED(m_scopeControl->AddBufferChannelSource(
		bstrTitle, varg, 1, channelSize, false, &scopeSrcId)))
	{
		m_vScopeBuffers.push_back(scopeBuffer);

		return true;

	}
	else
		return false;

}

bool ScopeView::SetChannelBuffer(int channelIndex, unsigned short *buffer, int count)
{
	if (!ComInterface())
		return false;

	SAFEARRAY *scopeBuffer = m_vScopeBuffers[channelIndex];
	unsigned short* rawp;

	SafeArrayAccessData(scopeBuffer, reinterpret_cast<void**>(&rawp));
	memcpy(rawp, buffer, sizeof(unsigned short) * count);
	SafeArrayUnaccessData(scopeBuffer);

	ComInterface()->RefreshScope();

	return true;
}

BEGIN_MESSAGE_MAP(ScopeView, CWnd)
END_MESSAGE_MAP()
