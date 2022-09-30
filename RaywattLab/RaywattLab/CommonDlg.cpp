#include "pch.h"
#include "CommonDlg.h"

void CCommonDlg::setComboHeight(CComboBox& comboBox, int itemToShow) {
	CRect rect, rectDropped;

	comboBox.GetClientRect(&rect);
	comboBox.GetDroppedControlRect(&rectDropped);
	int itemHeight = comboBox.GetItemHeight(-1);
	comboBox.GetParent()->ScreenToClient(&rectDropped);
	rectDropped.bottom = rectDropped.top + rect.Height() + itemHeight * itemToShow;
	comboBox.MoveWindow(&rectDropped);
}
void CCommonDlg::initToggleButton(ToggleButton& toggleBtn, int id, CString caption, CString captionPushed) {
	toggleBtn.id = id;
	toggleBtn.pushed = false;
	toggleBtn.caption[0] = caption;
	toggleBtn.caption[1] = captionPushed;
}
void CCommonDlg::toggleButton(CWnd *pWnd, ToggleButton& toggleBtn) {
	toggleBtn.pushed = !toggleBtn.pushed;
	pWnd->GetDlgItem(toggleBtn.id)->SetWindowText((toggleBtn.pushed) ? toggleBtn.caption[1] : toggleBtn.caption[0]);
}
void CCommonDlg::clearPictureBox(CStatic& pictureBox) {
	const int width = 100;
	const int height = 100;
	const int channel = 3;
	char imgBlack[width * height * channel];
	
	memset(imgBlack, 0x00, width * height * channel);

	drawToPictureBox(pictureBox, width, height, imgBlack);
}
void CCommonDlg::drawToPictureBox(CStatic& pictureBox, int width, int height, char* buffer) {
	CDC* pDC = pictureBox.GetDC();
	CRect rect;
	pictureBox.GetWindowRect(&rect);

	BITMAPINFO bmpInfo;
	bmpInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bmpInfo.bmiHeader.biWidth = width;
	bmpInfo.bmiHeader.biHeight = -1 * height;
	bmpInfo.bmiHeader.biPlanes = 1;
	bmpInfo.bmiHeader.biBitCount = 8 * 3;
	bmpInfo.bmiHeader.biCompression = BI_RGB;
	bmpInfo.bmiHeader.biXPelsPerMeter = 100;
	bmpInfo.bmiHeader.biYPelsPerMeter = 100;
	bmpInfo.bmiHeader.biClrUsed = 0;
	bmpInfo.bmiHeader.biClrImportant = 0;

	// To-Do : double buffering?
	pDC->SetStretchBltMode(COLORONCOLOR);
	StretchDIBits(pDC->GetSafeHdc(), 0, 0, rect.Width(), rect.Height(), 0, 0, width, height, buffer, &bmpInfo, DIB_RGB_COLORS, SRCCOPY);

	pictureBox.ReleaseDC(pDC);
}
void CCommonDlg::setButtonColor(COLORREF fill, COLORREF edge, COLORREF text, CString caption, LPDRAWITEMSTRUCT lpDrawItemStruct) {
	CDC dc;
	RECT rect;

	dc.Attach(lpDrawItemStruct->hDC);
	rect = lpDrawItemStruct->rcItem;

	dc.Draw3dRect(&rect, edge, edge);
	dc.FillSolidRect(&rect, fill);

	UINT state = lpDrawItemStruct->itemState;
	if (state & ODS_SELECTED)
	{
		dc.DrawEdge(&rect, EDGE_SUNKEN, BF_RECT);
	}
	else {
		dc.DrawEdge(&rect, EDGE_RAISED, BF_RECT);
	}

	dc.SetBkColor(fill);
	dc.SetTextColor(text);

	dc.DrawText(caption, &rect, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
	dc.Detach();
}
bool CCommonDlg::isPointInComponent(CWnd* pWnd, int nID, POINT point) {
	RECT rect;
	pWnd->GetDlgItem(nID)->GetWindowRect(&rect);
	pWnd->ScreenToClient(&rect);
	pWnd->ScreenToClient(&point);

	if (point.x >= rect.left && point.x <= rect.right && point.y >= rect.top && point.y <= rect.bottom) {
		return true;
	}
	return false;
}