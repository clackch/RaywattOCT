/** @file	IScopePX14.h
	@brief	Define interface for PX14 Scope ActiveX control
*/
#pragma once

#ifdef _WIN64
# define PX14_SCOPE_VI_PROGID					L"PX14UI64.ScopeCtrl"
# define PX14_SCOPE_CHANSRC_SIMPLE_VI_PROGID	L"PX14UI64.SimpleChannelSrc"
#else
# define PX14_SCOPE_VI_PROGID					L"PX14UI.ScopeCtrl"
# define PX14_SCOPE_CHANSRC_SIMPLE_VI_PROGID	L"PX14UI.SimpleChannelSrc"
#endif

#define SRCID_INVALID			(UINT_MAX)
#define CHANID_INVALID			(UINT_MAX)

#define PX14DISPID_LABELXL				1
#define PX14DISPID_LABELXR				2
#define PX14DISPID_LABELXM				3
#define PX14DISPID_LABELYT				4
#define PX14DISPID_LABELYM				5
#define PX14DISPID_LABELYB				6
#define PX14DISPID_LABELYT2				7
#define PX14DISPID_LABELYM2				8
#define PX14DISPID_LABELYB2				9
#define PX14DISPID_AUTOXLABELS			10
#define PX14DISPID_AUTOYLABELS			11
#define PX14DISPID_AUTOXLABEL_RADIX		12
#define PX14DISPID_AUTOYLABEL_RADIX		13
#define PX14DISPID_ALLOW_SELECTION		14
#define PX14DISPID_MARGINLEFT			15
#define PX14DISPID_MARGINRIGHT			16
#define PX14DISPID_MARGINTOP			17
#define PX14DISPID_MARGINBOTTOM			18
#define PX14DISPID_STARTSAMPLE			19
#define PX14DISPID_PAGE_WIDTH			20
#define PX14DISPID_MAXSAMPLECOUNT		21
#define PX14DISPID_MINSAMPLEVALUE		22
#define PX14DISPID_MAXSAMPLEVALUE		23
#define PX14DISPID_HSCROLLBAR			24
#define PX14DISPID_POINTRADIUS			25
#define PX14DISPID_GRIDROWS				26
#define PX14DISPID_GRIDCOLUMNS			27
#define PX14DISPID_GRIDLINESTYLE		28
#define PX14DISPID_AXISFONT				29
#define PX14DISPID_REDRAW				30
#define PX14DISPID_TRIGGERMARKSTYLE1	31
#define PX14DISPID_TRIGMARKLEVEL1		32
#define PX14DISPID_TRIGMARKIMAGE1		33
#define PX14DISPID_TRIGMARKIMAGEALPHA1	34
#define PX14DISPID_TRIGMARKLINECOLOR1	35
#define PX14DISPID_TRIGGERMARKSTYLE2	36
#define PX14DISPID_TRIGMARKLEVEL2		37
#define PX14DISPID_TRIGMARKIMAGE2		38
#define PX14DISPID_TRIGMARKIMAGEALPHA2	39
#define PX14DISPID_TRIGMARKLINECOLOR2	40
#define PX14DISPID_SCOPEFORECOLOR		41
#define PX14DISPID_SCOPEBACKCOLOR		42
#define PX14DISPID_TRACE_COLOR			43
#define PX14DISPID_CHANNEL_COLOR		44
#define PX14DISPID_CHANNEL_VISIBLE		45
#define PX14DISPID_MAX_PAGE_WIDTH		46
#define PX14DISPID_SEGMENT_SIZE			47
#define PX14DISPID_SEGMENT_LINESTYLE	48
#define PX14DISPID_SEGMENT_COLOR		49
#define PX14DISPID_CTXMENU_ENABLE		50
#define PX14DISPID_SAMPTIPS_ENABLE		51
#define PX14DISPID_SAMPTIPS_CHANID		52
#define PX14DISPID_SAMPTIPS_RADIX		53

#define PX14DISPID_RESET_SCOPE_DATA		100
#define PX14DISPID_USE_EXAMPLE_DATA		101
#define PX14DISPID_REFRESH_SCOPE		102
#define PX14DISPID_ADD_CHAN_SRC			103
#define PX14DISPID_REM_CHAN_SRC			104
#define PX14DISPID_REFRESH_CHAN_SRC		105
#define PX14DISPID_SET_SEL				106
#define PX14DISPID_GET_SEL				107
#define PX14DISPID_ZOOM_TO_SEL			108
#define PX14DISPID_GET_CHAN_SRC			109
#define PX14DISPID_GET_FIRST_CHANID		110
#define PX14DISPID_GET_SRCID_FROM_CHANID 111
#define PX14DISPID_AUTO_SET				112
#define PX14DISPID_ADD_CHAN_SRC2		113
#define PX14DISPID_GET_SOURCE_COUNT		114
#define PX14DISPID_ENUM_SOURCE_IDS		115
#define PX14DISPID_SET_LABEL_PROVIDER	116

#define PX14DISPID_ADD_FILE_CHAN_SRC	200
#define PX14DISPID_ADD_BUF_CHAN_SRC		201
#define PX14DISPID_ADD_BOARD_CHAN_SRC	202

[export]
enum PX14ScopeTrigMark {
	TrigMarkNone,		///< No trigger mark
	TrigMarkImage,		///< Trigger icon only
	TrigMarkLine,		///< Trigger line only
	TrigMarkAll			///< Trigger line and icon
};

[export]
enum PX14GridLineStyle {
	LineSolid = PS_SOLID,			///< Solid line
	LineDashed = PS_DASH,			///< Dashed line
	LineDotted = PS_DOT,			///< Dotted line
	LineDashDot = PS_DASHDOT,		///< Dashed-dotted line
	LineDashDotDot = PS_DASHDOTDOT,	///< Dash-dot-dotted line
	LineNone = PS_NULL				///< Invisible line
};

[export]
enum PX14ScrollBar {
	ScrollbarAuto,			///< Use a scrollbar only when needed
	ScrollbarYes,			///< Use a scrollbar
	ScrollbarNo				///< Do not use a scrollbar
};

#define MSBORDERSTYLE_NONE		0
#define MSBORDERSTYLE_LINE		1
#define MSBORDERSTYLE_3D		2
#define MSBORDERSTYLEMAX		3	 // Not a valid entry.

// _IScopePX14Events
[
	object,
#ifdef _WIN64
	uuid("D755952B-6892-4D1C-BD8A-D522CA3313A8"),		// 64-bit
#else
	uuid("464B70B7-8CA0-4ADF-ABC3-E0137E9EF6DB"),		// 32-bit
#endif
	helpstring("_IScopePX14Events Interface")
]
__interface _IScopePX14Events : public IUnknown
{
	[id(100)] HRESULT OnSelectionChange ([in] UINT64 startSample, [in] UINT64 endSample);
	[id(101)] HRESULT OnSourceAdded ([in] UINT srcId);
	[id(102)] HRESULT OnSourceRemoved ([in] UINT srcId);
};

// IScopePX14ChannelSource
[
	object,
#ifdef _WIN64
	uuid("DDD3A53D-10CC-435A-8AE4-958E2DEB2BCA"),		// 64-bit
#else
	uuid("D662D503-1DD3-43FB-9F54-8F7CC981A21C"),		// 32-bit
#endif
	helpstring("A source for PX14 Scope control channel data"),
	pointer_default(unique)
]
__interface IScopePX14ChannelSource : public IUnknown
{	
	// -- Attributes

	[helpstring("User friendly name for this channel of data")]
	HRESULT GetChannelSourceName ([out] BSTR* pbstrName);
	[helpstring("Number of channels of data in this source")]
	HRESULT GetChannelCount ([out] UINT* valp);
	[helpstring("Channel's sample data type (VT_*)")]
	HRESULT GetChannelSampleType ([in] UINT chan, [out] USHORT* pvt);
	[helpstring("Total samples available in source (per channel)")]
	HRESULT GetTotalSampleCount ([in] UINT chan, [out] ULONGLONG* valp);

	// -- Methods

	[helpstring("Identifies sources that contain interleaved data")]
	HRESULT IsSampleInterleavedData ([out] VARIANT_BOOL* pVal);
	[helpstring("Refresh total sample count; optional")]
	HRESULT RefreshTotalSampleCount ([in] UINT newTotalSampCount);
	
	// For sources that contain interleaved data:
	//  1) The chan parameter is ignored. The method obtain all channel's data
	//  2) The caller specified buffer must be large enough to hold requested
	//     sample count for _all_ channels in this source
	[helpstring("Obtain channel data")]
	HRESULT GetChannelData ([in] UINT chan, [in] ULONGLONG offset, 
		[in] ULONG samples, [in,out] VARIANTARG* psav,
		[out] ULONG* pSampsCopied);

	[helpstring("Obtain a single sample")]
	HRESULT GetSampleValue ([in] UINT chan, [in] ULONGLONG offset,
		[in,out] VARIANTARG* argp);

	[helpstring("Optional; resize source's total sample count")]
	HRESULT ResizeSourceSampleCount ([in] UINT newCount);
};

[
	object,
#ifdef _WIN64
	uuid("E7C01994-4957-4266-A306-8F494AD96F03"),		// 64-bit
#else
	uuid("4288BEFD-C11E-422B-90FF-98D3AFBF4D60"),		// 32-bit
#endif
	helpstring ("Interface used for custom plot labels"),
	pointer_default(unique)
]
__interface IScopePX14CustomLabelProvider : public IUnknown
{
	// Called to get X-axis label for given X position
	HRESULT GetXAxisLabel ([in] ULONGLONG x, [out] BSTR* pbstrLabel);
	// Called to get Y-axis label for given X position
	HRESULT GetYAxisLabel ([in] DOUBLE y, [out] BSTR* pbstrLabel);

	// Called to get x-axis label for selected X position
	HRESULT GetXSelLabel  ([in] ULONGLONG x, [out] BSTR* pbstrLabel);
	// Called to get x-axis label for span selected points
	HRESULT GetXSelSpanLabel ([in] ULONGLONG x1, [in] ULONGLONG x2, [out] BSTR* pbstrLabel);

	// Called to get text for sample value tool-tip
	HRESULT GetSampleTipLabel ([in] UINT chanId, 
		[in] ULONGLONG dwSample, [in] VARIANT var, [out] BSTR* pbstrLabel);
};

[
	object,
#ifdef _WIN64
	uuid("52AAB3A2-DD4D-4219-8ED2-C6B129AC49D8"),		// 64-bit
#else
	uuid("F361CA13-B5B3-4FA5-B5ED-C2F3C398BEBC"),		// 32-bit
#endif
	helpstring("Interface used to determine if its safe/allowable to obtain channel data"),
	pointer_default(unique)
]
__interface IScopePX14ChannelArbiter : public IUnknown
{
	[helpstring("Returns S_OK if it's safe to obtain channel data")]
	HRESULT SafeToObtainChannelData (UINT srcId);

	[helpstring("Notifies arbiter that we need data when available")]
	HRESULT OnScopeNeedsDataWhenAvailable (UINT srcId);
};


// IScopePX14
[
	object,
#ifdef _WIN64
	uuid("59E8B91D-4D18-4AF8-B26E-9B96BAFCF709"),		// 64-bit
#else
	uuid("80B6F618-9585-4653-A4ED-3C59D064D02F"),		// 32-bit
#endif
	dual,
	helpstring("IScopePX14 Interface"),
	pointer_default(unique)
]
__interface IScopePX14 : public IDispatch
{
	// Begin a whole lot of properties
	[propput, bindable, id(DISPID_BACKCOLOR)]
	HRESULT BackColor([in]OLE_COLOR clr);
	[propget, bindable, id(DISPID_BACKCOLOR)]
	HRESULT BackColor([out,retval]OLE_COLOR* pclr);
	[propput, bindable, id(DISPID_BORDERCOLOR)]
	HRESULT BorderColor([in]OLE_COLOR clr);
	[propget, bindable, id(DISPID_BORDERCOLOR)]
	HRESULT BorderColor([out, retval]OLE_COLOR* pclr);
	[propput, bindable, id(DISPID_BORDERSTYLE)]
	HRESULT BorderStyle([in]long style);
	[propget, bindable, id(DISPID_BORDERSTYLE)]
	HRESULT BorderStyle([out, retval]long* pstyle);
	[propputref, bindable, id(DISPID_FONT)]
	HRESULT Font([in]IFontDisp* pFont);
	[propput, bindable, id(DISPID_FONT)]
	HRESULT Font([in]IFontDisp* pFont);
	[propget, bindable, id(DISPID_FONT)]
	HRESULT Font([out, retval]IFontDisp** ppFont);
	[propput, bindable, id(DISPID_FORECOLOR)]
	HRESULT ForeColor([in]OLE_COLOR clr);
	[propget, bindable, id(DISPID_FORECOLOR)]
	HRESULT ForeColor([out,retval]OLE_COLOR* pclr);
	[propput, bindable, id(DISPID_ENABLED)]
	HRESULT Enabled([in]VARIANT_BOOL vbool);
	[propget, bindable, id(DISPID_ENABLED)]
	HRESULT Enabled([out,retval]VARIANT_BOOL* pbool);
	[propput, bindable, id(DISPID_CAPTION), helpstring("Text displayed at top of plot")]
	HRESULT Caption([in]BSTR strCaption);
	[propget, bindable, id(DISPID_CAPTION), helpstring("Text displayed at top of plot")]
	HRESULT Caption([out,retval]BSTR* pstrCaption);
	[propput, bindable, id(DISPID_BORDERVISIBLE), helpstring("Display selected border type?")]
	HRESULT BorderVisible([in]VARIANT_BOOL vbool);
	[propget, bindable, id(DISPID_BORDERVISIBLE), helpstring("Display selected border type?")]
	HRESULT BorderVisible([out, retval]VARIANT_BOOL* pbool);

	[propget, id(PX14DISPID_LABELXL), helpstring("Left X-axis label"), bindable] 
	HRESULT LabelXL([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELXL), helpstring("Left X-axis label"), bindable] 
	HRESULT LabelXL([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELXR), helpstring("Right X-axis label"), bindable] 
	HRESULT LabelXR([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELXR), helpstring("Right X-axis label"), bindable] 
	HRESULT LabelXR([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELXM), helpstring("Middle X-axis label"), bindable] 
	HRESULT LabelXM([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELXM), helpstring("Middle X-axis label"), bindable] 
	HRESULT LabelXM([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYT), helpstring("Top Y-axis label"), bindable] 
	HRESULT LabelYT([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYT), helpstring("Top Y-axis label"), bindable] 
	HRESULT LabelYT([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYM), helpstring("Middle Y-axis label"), bindable] 
	HRESULT LabelYM([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYM), helpstring("Middle Y-axis label"), bindable] 
	HRESULT LabelYM([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYB), helpstring("Bottom Y-axis label"), bindable] 
	HRESULT LabelYB([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYB), helpstring("Bottom Y-axis label"), bindable] 
	HRESULT LabelYB([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYT2), helpstring("Top Y2-axis label"), bindable] 
	HRESULT LabelYT2([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYT2), helpstring("Top Y2-axis label"), bindable] 
	HRESULT LabelYT2([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYM2), helpstring("Middle Y2-axis label"), bindable] 
	HRESULT LabelYM2([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYM2), helpstring("Middle Y2-axis label"), bindable] 
	HRESULT LabelYM2([in] BSTR newVal);
	[propget, id(PX14DISPID_LABELYB2), helpstring("Bottom Y2-axis label"), bindable] 
	HRESULT LabelYB2([out, retval] BSTR* pVal);
	[propput, id(PX14DISPID_LABELYB2), helpstring("Bottom Y2-axis label"), bindable] 
	HRESULT LabelYB2([in] BSTR newVal);
	[propget, id(PX14DISPID_AUTOXLABELS), helpstring("Scope automatically generates and updates X-axis labels"), bindable] 
	HRESULT AutoXLabels([out, retval] VARIANT_BOOL* pVal);
	[propput, id(PX14DISPID_AUTOXLABELS), helpstring("Scope automatically generates and updates X-axis labels"), bindable] 
	HRESULT AutoXLabels([in] VARIANT_BOOL newVal);
	[propget, id(PX14DISPID_AUTOYLABELS), helpstring("Scope automatically generates and updates y-axis labels"), bindable] 
	HRESULT AutoYLabels([out, retval] VARIANT_BOOL* pVal);
	[propput, id(PX14DISPID_AUTOYLABELS), helpstring("Scope automatically generates and updates y-axis labels"), bindable] 
	HRESULT AutoYLabels([in] VARIANT_BOOL newVal);
	[propget, id(PX14DISPID_AUTOXLABEL_RADIX), helpstring ("Radix to use for auto-generated x-axis labels"), bindable]
	HRESULT AutoXLabelRadix ([out,retval] LONG* pVal);
	[propput, id(PX14DISPID_AUTOXLABEL_RADIX), helpstring ("Radix to use for auto-generated x-axis labels"), bindable]
	HRESULT AutoXLabelRadix ([in] LONG newVal);
	[propget, id(PX14DISPID_AUTOYLABEL_RADIX), helpstring ("Radix to use for auto-generated y-axis labels"), bindable]
	HRESULT AutoYLabelRadix ([out,retval] LONG* pVal);
	[propput, id(PX14DISPID_AUTOYLABEL_RADIX), helpstring ("Radix to use for auto-generated y-axis labels"), bindable]
	HRESULT AutoYLabelRadix ([in] LONG newVal);
	[propget, id(PX14DISPID_ALLOW_SELECTION), helpstring("Allow user selection of plotted data"), bindable]
	HRESULT AllowDataSelection ([out,retval] VARIANT_BOOL* valp);
	[propput, id(PX14DISPID_ALLOW_SELECTION), helpstring("Allow user selection of plotted data"), bindable]
	HRESULT AllowDataSelection ([in] VARIANT_BOOL val);
	[propget, id(PX14DISPID_MARGINLEFT), helpstring("Left window-scope margin"), bindable] 
	HRESULT MarginLeft([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MARGINLEFT), helpstring("Left window-scope margin"), bindable] 
	HRESULT MarginLeft([in] LONG newVal);
	[propget, id(PX14DISPID_MARGINRIGHT), helpstring("Right window-scope margin"), bindable] 
	HRESULT MarginRight([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MARGINRIGHT), helpstring("Right window-scope margin"), bindable] 
	HRESULT MarginRight([in] LONG newVal);
	[propget, id(PX14DISPID_MARGINTOP), helpstring("Top window-scope margin"), bindable] 
	HRESULT MarginTop([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MARGINTOP), helpstring("Top window-scope margin"), bindable] 
	HRESULT MarginTop([in] LONG newVal);
	[propget, id(PX14DISPID_MARGINBOTTOM), helpstring("Bottom window-scope margin"), bindable] 
	HRESULT MarginBottom([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MARGINBOTTOM), helpstring("Bottom window-scope margin"), bindable] 
	HRESULT MarginBottom([in] LONG newVal);
	[propget, id(PX14DISPID_STARTSAMPLE), helpstring("First sample to display in scope (with respect to data source)"), bindable] 
	HRESULT StartSample([out, retval] UINT64* pVal);
	[propput, id(PX14DISPID_STARTSAMPLE), helpstring("First sample to display in scope (with respect to data source)"), bindable] 
	HRESULT StartSample([in] UINT64 newVal);
	[propget, id(PX14DISPID_PAGE_WIDTH), helpstring("Scope's page width in samples"), bindable] 
	HRESULT PageWidth([out, retval] ULONG* pVal);
	[propput, id(PX14DISPID_PAGE_WIDTH), helpstring("Scope's page width in samples"), bindable] 
	HRESULT PageWidth([in] ULONG newVal);
	[propget, id(PX14DISPID_MAX_PAGE_WIDTH), helpstring("Maximum page width in samples"), bindable] 
	HRESULT MaxPageWidth([out, retval] ULONG* pVal);
	[propput, id(PX14DISPID_MAX_PAGE_WIDTH), helpstring("Maximum page width in samples"), bindable] 
	HRESULT MaxPageWidth([in] ULONG newVal);
	[propget, id(PX14DISPID_MAXSAMPLEVALUE), helpstring("Maximum sample value"), bindable] 
	HRESULT MaxSampleValue([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MAXSAMPLEVALUE), helpstring("Maximum sample value"), bindable] 
	HRESULT MaxSampleValue([in] LONG newVal);
	[propget, id(PX14DISPID_MINSAMPLEVALUE), helpstring("Minimum sample value"), bindable] 
	HRESULT MinSampleValue([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_MINSAMPLEVALUE), helpstring("Minimum sample value"), bindable] 
	HRESULT MinSampleValue([in] LONG newVal);
	[propget, id(PX14DISPID_MAXSAMPLECOUNT), helpstring("Maximum channel sample count"), bindable] 
	HRESULT MaxSampleCount([out, retval] UINT64* pVal);
	[propget, id(PX14DISPID_HSCROLLBAR), helpstring("Enables or disables the scope's horizontal scroll bar"), bindable] 
	HRESULT HScrollBar([out, retval] PX14ScrollBar* pVal);
	[propput, id(PX14DISPID_HSCROLLBAR), helpstring("Enables or disables the scope's horizontal scroll bar"), bindable] 
	HRESULT HScrollBar([in] PX14ScrollBar newVal);
	[propget, id(PX14DISPID_POINTRADIUS), helpstring("Plotted point radius"), bindable]
	HRESULT PointRadius ([out, retval] LONG *pVal);
	[propput, id(PX14DISPID_POINTRADIUS), helpstring("Plotted point radius"), bindable]
	HRESULT PointRadius ([in] LONG newVal);
	[propget, id(PX14DISPID_GRIDROWS), helpstring("Row count for scope grid"), bindable] 
	HRESULT GridRows([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_GRIDROWS), helpstring("Row count for scope grid"), bindable] 
	HRESULT GridRows([in] LONG newVal);
	[propget, id(PX14DISPID_GRIDCOLUMNS), helpstring("Column count for scope grid"), bindable] 
	HRESULT GridColumns([out, retval] LONG* pVal);
	[propput, id(PX14DISPID_GRIDCOLUMNS), helpstring("Column count for scope grid"), bindable] 
	HRESULT GridColumns([in] LONG newVal);
	[propget, id(PX14DISPID_GRIDLINESTYLE), helpstring("Style of line used for scope grid"), bindable] 
	HRESULT GridLineStyle([out, retval] PX14GridLineStyle* pVal);
	[propput, id(PX14DISPID_GRIDLINESTYLE), helpstring("Style of line used for scope grid"), bindable] 
	HRESULT GridLineStyle([in] PX14GridLineStyle newVal);
	[propget, id(PX14DISPID_AXISFONT), helpstring("Font used for scope axes"), bindable] 
	HRESULT AxisFont([out, retval] IFontDisp** pVal);
	[propput, id(PX14DISPID_AXISFONT), helpstring("Font used for scope axes"), bindable] 
	HRESULT AxisFont([in] IFontDisp* newVal);
	[propget, id(PX14DISPID_REDRAW), helpstring("Enables/disables scope GUI updates."), bindable, nonbrowsable]
	HRESULT Redraw ([out, retval] VARIANT_BOOL *pbool);
	[propput, id(PX14DISPID_REDRAW), helpstring("Enables/disables scope GUI updates."), bindable, nonbrowsable]
	HRESULT Redraw ([in] VARIANT_BOOL vbool);
	[propget, id(PX14DISPID_TRIGGERMARKSTYLE1), helpstring("Trigger level marking"), bindable]
	HRESULT TriggerMarkStyle1 ([out, retval] PX14ScopeTrigMark* pVal);
	[propput, id(PX14DISPID_TRIGGERMARKSTYLE1), helpstring("Trigger level marking"), bindable]
	HRESULT TriggerMarkStyle1 ([in] PX14ScopeTrigMark newVal);
	[propget, id(PX14DISPID_TRIGMARKLEVEL1), helpstring("Trigger mark level as percent [0,1]"), bindable] 
	HRESULT TrigMarkLevel1([out, retval] FLOAT* pVal);
	[propput, id(PX14DISPID_TRIGMARKLEVEL1), helpstring("Trigger mark level as percent [0,1]"), bindable] 
	HRESULT TrigMarkLevel1([in] FLOAT newVal);
	[propget, id(PX14DISPID_TRIGMARKIMAGE1), helpstring("Trigger-level mark image"), bindable] 
	HRESULT TrigMarkImage1([out, retval] IPictureDisp** pVal);
	[propput, id(PX14DISPID_TRIGMARKIMAGE1), helpstring("Trigger-level mark image"), bindable] 
	HRESULT TrigMarkImage1([in] IPictureDisp* newVal);
	[propget, id(PX14DISPID_TRIGMARKIMAGEALPHA1), helpstring("Alpha channel for trigger-level mark image. (0:Transparent, 1:Opaque)"), bindable]
	HRESULT TrigMarkImageAlpha1 ([out, retval] DOUBLE *pVal);
	[propput, id(PX14DISPID_TRIGMARKIMAGEALPHA1), helpstring("Alpha channel for trigger-level mark image. (0:Transparent, 1:Opaque)"), bindable]
	HRESULT TrigMarkImageAlpha1 ([in] DOUBLE newVal);
	[propget, id(PX14DISPID_TRIGMARKLINECOLOR1), helpstring("Trigger-level marker line color"), bindable] 
	HRESULT TrigMarkLineColor1([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_TRIGMARKLINECOLOR1), helpstring("Trigger-level marker line color"), bindable] 
	HRESULT TrigMarkLineColor1([in] OLE_COLOR newVal);
	[propget, id(PX14DISPID_TRIGGERMARKSTYLE2), helpstring("Trigger level marking"), bindable]
	HRESULT TriggerMarkStyle2 ([out, retval] PX14ScopeTrigMark* pVal);
	[propput, id(PX14DISPID_TRIGGERMARKSTYLE2), helpstring("Trigger level marking"), bindable]
	HRESULT TriggerMarkStyle2 ([in] PX14ScopeTrigMark newVal);
	[propget, id(PX14DISPID_TRIGMARKLEVEL2), helpstring("Trigger mark level as percent [0,1]"), bindable] 
	HRESULT TrigMarkLevel2([out, retval] FLOAT* pVal);
	[propput, id(PX14DISPID_TRIGMARKLEVEL2), helpstring("Trigger mark level as percent [0,1]"), bindable] 
	HRESULT TrigMarkLevel2([in] FLOAT newVal);
	[propget, id(PX14DISPID_TRIGMARKIMAGE2), helpstring("Trigger-level mark image"), bindable] 
	HRESULT TrigMarkImage2([out, retval] IPictureDisp** pVal);
	[propput, id(PX14DISPID_TRIGMARKIMAGE2), helpstring("Trigger-level mark image"), bindable] 
	HRESULT TrigMarkImage2([in] IPictureDisp* newVal);
	[propget, id(PX14DISPID_TRIGMARKIMAGEALPHA2), helpstring("Alpha channel for trigger-level mark image. (0:Transparent, 1:Opaque)"), bindable]
	HRESULT TrigMarkImageAlpha2 ([out, retval] DOUBLE *pVal);
	[propput, id(PX14DISPID_TRIGMARKIMAGEALPHA2), helpstring("Alpha channel for trigger-level mark image. (0:Transparent, 1:Opaque)"), bindable]
	HRESULT TrigMarkImageAlpha2 ([in] DOUBLE newVal);
	[propget, id(PX14DISPID_TRIGMARKLINECOLOR2), helpstring("Trigger-level marker line color"), bindable] 
	HRESULT TrigMarkLineColor2([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_TRIGMARKLINECOLOR2), helpstring("Trigger-level marker line color"), bindable] 
	HRESULT TrigMarkLineColor2([in] OLE_COLOR newVal);
	[propget, id(PX14DISPID_SCOPEFORECOLOR), helpstring("Scope foreground color"), bindable] 
	HRESULT ScopeForeColor([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_SCOPEFORECOLOR), helpstring("Scope foreground color"), bindable] 
	HRESULT ScopeForeColor([in] OLE_COLOR newVal);
	[propget, id(PX14DISPID_SCOPEBACKCOLOR), helpstring("Scope background color"), bindable] 
	HRESULT ScopeBackColor([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_SCOPEBACKCOLOR), helpstring("Scope background color"), bindable] 
	HRESULT ScopeBackColor([in] OLE_COLOR newVal);
	[propget, id(PX14DISPID_TRACE_COLOR), helpstring("Selection trace color"), bindable] 
	HRESULT TraceColor([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_TRACE_COLOR), helpstring("Selection trace color"), bindable] 
	HRESULT TraceColor([in] OLE_COLOR newVal);
	[propget, id(PX14DISPID_CHANNEL_COLOR), helpstring("Channel color"), bindable, nonbrowsable]
	HRESULT ChannelColor ([in] ULONG chanId, [out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_CHANNEL_COLOR), helpstring("Channel color"), bindable, nonbrowsable]
	HRESULT ChannelColor ([in] ULONG chanId, [in] OLE_COLOR val);
	[propput, id(PX14DISPID_CHANNEL_VISIBLE), helpstring("Visibility of given channel"), bindable, nonbrowsable]
	HRESULT ChannelVisibility([in] ULONG chanId, [in]VARIANT_BOOL newVal);
	[propget, id(PX14DISPID_CHANNEL_VISIBLE), helpstring("Visibility of given channel"), bindable, nonbrowsable]
	HRESULT ChannelVisibility([in] ULONG chanId, [out,retval]VARIANT_BOOL* pVal);
	[propget, id(PX14DISPID_SEGMENT_SIZE), helpstring("Defines 'segment' size or 0 for none"), bindable]
	HRESULT SegmentSize ([out, retval] ULONG* pVal);
	[propput, id(PX14DISPID_SEGMENT_SIZE), helpstring("Defines 'segment' size or 0 for none"), bindable]
	HRESULT SegmentSize ([in] ULONG newVal);
	[propget, id(PX14DISPID_SEGMENT_LINESTYLE), helpstring("Line style for segments"), bindable]
	HRESULT SegmentLineStyle ([out, retval] PX14GridLineStyle* pVal);
	[propput, id(PX14DISPID_SEGMENT_LINESTYLE), helpstring("Line style for segments"), bindable]
	HRESULT SegmentLineStyle ([in] PX14GridLineStyle newVal);
	[propget, id(PX14DISPID_SEGMENT_COLOR), helpstring("Segments marker color"), bindable]
	HRESULT SegmentLineColor ([out, retval] OLE_COLOR* pVal);
	[propput, id(PX14DISPID_SEGMENT_COLOR), helpstring("Segments marker color"), bindable]
	HRESULT SegmentLineColor ([in] OLE_COLOR newVal);
	[propput, id(PX14DISPID_CTXMENU_ENABLE), helpstring("Determines if scope context menu is ever allowed"), bindable]
	HRESULT ContextMenuEnabled([in]VARIANT_BOOL vbool);
	[propget, id(PX14DISPID_CTXMENU_ENABLE), helpstring("Determines if scope context menu is ever allowed"), bindable]
	HRESULT ContextMenuEnabled([out,retval]VARIANT_BOOL* pbool);
	[propput, id(PX14DISPID_SAMPTIPS_ENABLE), helpstring("Determines if sample value tooltips are enabled"), bindable]
	HRESULT SampleValueTipsEnabled([in]VARIANT_BOOL vbool);
	[propget, id(PX14DISPID_SAMPTIPS_ENABLE), helpstring("Determines if sample value tooltips are enabled"), bindable]
	HRESULT SampleValueTipsEnabled([out,retval]VARIANT_BOOL* pbool);
	[propget, id(PX14DISPID_SAMPTIPS_CHANID), helpstring("Channel to use for sample value tooltips"), bindable, nonbrowsable]
	HRESULT SampleValueTipsChannelId ([out,retval] ULONG* pVal);
	[propput, id(PX14DISPID_SAMPTIPS_CHANID), helpstring("Channel to use for sample value tooltips"), bindable, nonbrowsable]
	HRESULT SampleValueTipsChannelId ([in] ULONG chanId);
	[propget, id(PX14DISPID_SAMPTIPS_RADIX), helpstring("Radix to use for sample value tooltips"), bindable, nonbrowsable]
	HRESULT SampleValueTipsRadix ([out,retval] INT* pVal);
	[propput, id(PX14DISPID_SAMPTIPS_RADIX), helpstring("Radix to use for sample value tooltips"), bindable, nonbrowsable]
	HRESULT SampleValueTipsRadix ([in] INT radix);

	// Begin methods
	[id(PX14DISPID_RESET_SCOPE_DATA), helpstring("Resets all scope data; removes all channels")] 
	HRESULT ResetScopeData(void);
	[id(PX14DISPID_USE_EXAMPLE_DATA), helpstring("Makes the current channel's data a simple ramp."), nonbrowsable, hidden]
	HRESULT UseExampleData(void);
	[id(PX14DISPID_REFRESH_SCOPE), helpstring("Redraw scope content")]
	HRESULT RefreshScope(void);
	[id(PX14DISPID_ADD_CHAN_SRC), helpstring("Add a channel source")]
	HRESULT AddChannelSource ([in] IScopePX14ChannelSource* pChanSrc, [out] UINT* pSrcId);
	[id(PX14DISPID_ADD_CHAN_SRC2), helpstring("Add a channel source with optional arbiter")]
	HRESULT AddChannelSource2 (
		[in]  IScopePX14ChannelSource* pChanSrc, 
		[in]  IScopePX14ChannelArbiter* pArbiter,	// optional = NULL
		[in]  UINT max_chan_count,					// optional = 0
		[out] UINT* pSrcId);
	[id(PX14DISPID_REM_CHAN_SRC), helpstring("Remove a channel source")]
	HRESULT RemoveChannelSource ([in] UINT srcId);
	[id(PX14DISPID_REFRESH_CHAN_SRC), helpstring("Call when channel source data may have changed")]
	HRESULT RefreshChannelSource ([in] UINT srcId);
	[id(PX14DISPID_GET_CHAN_SRC), helpstring("Obtain channel source interface")]
	HRESULT GetChannelSource ([in] UINT srcId, [out] IScopePX14ChannelSource** srcpp);
	[id(PX14DISPID_GET_FIRST_CHANID), helpstring("Get first channel ID for given source")]
	HRESULT GetFirstChannelIdForSource ([in] UINT srcId, [out] UINT* pChanId);
	[id(PX14DISPID_GET_SRCID_FROM_CHANID), helpstring("Get source ID of given channel ID")]
	HRESULT GetSourceIdFromChanId ([in] UINT chanId, [out] UINT* srcId);
	[id(PX14DISPID_GET_SOURCE_COUNT), helpstring("Obtain number of data sources")]
	HRESULT GetChannelSourceCount ([in] UINT* pVal);
	[id(PX14DISPID_ENUM_SOURCE_IDS), helpstring("Obtain enumeration of all data source IDs")]
	HRESULT EnumChannelSourceIds ([in,out] UINT* bufSize, [out] UINT* ids);

	[id(PX14DISPID_SET_SEL), helpstring("Set current selection region")]
	HRESULT SetSelection ([in] UINT64 start, [in] UINT64 end);
	[id(PX14DISPID_GET_SEL), helpstring("Get current selection region")]
	HRESULT GetSelection ([out] UINT64* pStart, [out] UINT64* pEnd);
	[id(PX14DISPID_ZOOM_TO_SEL), helpstring("Zoom to selection")]
	HRESULT ZoomToSelection(void);
	[id(PX14DISPID_AUTO_SET), helpstring("Auto set scaling to fit current data")]
	HRESULT AutoSetScaling();
	[id(PX14DISPID_SET_LABEL_PROVIDER), helpstring("Set custom label provider interface")]
	HRESULT SetCustomLabelProvider ([in] IScopePX14CustomLabelProvider* p);

	// Pre-defined channel source creation methods

	[id(PX14DISPID_ADD_FILE_CHAN_SRC), helpstring("Insert a file channel source"), local]
	HRESULT AddFileChannelSource ([in] BSTR bstrPath, [in] BSTR bstrName, 
		[in] USHORT vt, [in] LONG nChannels, [in] UINT skip_hdr_bytes,
		[out] UINT* pSrcId);

	[id(PX14DISPID_ADD_BUF_CHAN_SRC), helpstring("Insert a buffer channel source")]
	HRESULT AddBufferChannelSource ([in] BSTR bstrName, [in] VARIANTARG varBuf, 
		[in] UINT nChannels, [in] UINT totalSamples, [in] VARIANT_BOOL bFreeWhenDone,
		[out] UINT* pSrcId);

	[id(PX14DISPID_ADD_BOARD_CHAN_SRC), helpstring("Insert a PX14 device channel source"), local]
	HRESULT AddPX14ChannelSource ([in] void* hBrd, [in] UINT total_samples, 
		[in] BSTR bstrName, [in] IScopePX14ChannelArbiter* pArbiter, [out] UINT* pSrcId);
};

