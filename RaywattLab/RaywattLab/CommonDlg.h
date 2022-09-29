#pragma once

#define DELAY_FOR_STOP_THREAD			50

typedef struct _ToggleButton {
	int id;
	bool pushed;
	CString caption[2];
}ToggleButton;

class CCommonDlg
{

public:
	CCommonDlg() {}
	virtual ~CCommonDlg() {}

protected:
	void setComboHeight(CComboBox& comboBox, int itemToShow);
	void initToggleButton(ToggleButton& toggleBtn, int id, CString caption, CString captionPushed);
	void toggleButton(CWnd* pWnd, ToggleButton& toggleBtn);
	void clearPictureBox(CStatic& pictureBox);
	void drawToPictureBox(CStatic& pictureBox, int width, int height, char* buffer);
	void setButtonColor(COLORREF fill, COLORREF edge, COLORREF text, CString caption, LPDRAWITEMSTRUCT lpDrawItemStruct);
	bool isPointInComponent(CWnd *pWnd, int nID, POINT point);
};

