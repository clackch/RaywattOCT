#pragma once

#include <vector>
#include "px14.h"
#include "IScopePX14.h"

// ScopeView

class ScopeView : public CWnd
{
	DECLARE_DYNAMIC(ScopeView)

private:
	CComQIPtr<IScopePX14> m_scopeControl;
	std::vector<SAFEARRAY*> m_vScopeBuffers;

public:
	bool Create(CWnd* parent, UINT nID);
	CComQIPtr<IScopePX14> ComInterface() { return m_scopeControl; }

	bool AddChannel(LPCTSTR name, int channelSize);
	bool SetChannelBuffer(int channelIndex, unsigned short *buffer, int count);

public:
	ScopeView();
	virtual ~ScopeView();

protected:
	DECLARE_MESSAGE_MAP()
};


