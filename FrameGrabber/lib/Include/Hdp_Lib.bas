Attribute VB_Name = "Hdp_Lib"
Option Explicit

Public Const HIDEF_ACCURA                As Integer = 4
Public Const HIDEF_I25                   As Integer = 5
Public Const HIDEF_I50                   As Integer = 6
Public Const HIDEF_I60                   As Integer = 7
Public Const HIDEF_I25MV                 As Integer = 8
Public Const HIDEF_I50MV                 As Integer = 9
Public Const HIDEF_I60MV                 As Integer = 10
Public Const HIDEF_ICOLOR                As Integer = 11
Public Const HIDEF_ICOLORMV              As Integer = 12
Public Const HIDEF_IRGB25                As Integer = 13
Public Const HIDEF_IRGB50                As Integer = 14
Public Const HIDEF_IRGB60                As Integer = 15
Public Const HIDEF_IRGB25MV              As Integer = 16
Public Const HIDEF_IRGB50MV              As Integer = 17
Public Const HIDEF_IRGB60MV              As Integer = 18
Public Const HIDEF_I75                   As Integer = 19
Public Const HIDEF_I75MV                 As Integer = 20
Public Const HIDEF_IRGB75                As Integer = 21
Public Const HIDEF_IRGB75MV              As Integer = 22
Public Const HIDEF_IRGB165               As Integer = 23
Public Const HIDEF_IRGB200               As Integer = 24
Public Const HIDEF_I50HSN                As Integer = 25
Public Const HIDEF_IRGB170               As Integer = 26
Public Const HIDEF_IRGBI64_170           As Integer = 27

Public Const HPEE_PORT_ASC               As Long = &H1
Public Const HPEE_PORT_BIN               As Long = &H2
Public Const HPEE_PIXEL_BITS             As Long = &H3
Public Const HPEE_BOARD_TYPE             As Long = &H4
Public Const HPEE_PROM                   As Long = &HFF
Public Const HPEE_FORMAT_REV             As Long = &H1000
Public Const HPEE_PARTNO                 As Long = &H2000
Public Const HPEE_BOARD_REV              As Long = &H3000
Public Const HPEE_SERIAL_NO              As Long = &H4000
Public Const HPEE_PHASE_ADJ              As Long = &H5000
Public Const HPEE_OVENS                  As Long = &H5100
Public Const HPEE_HD2_POTS               As Long = &H8000
Public Const HPEE_EOD                    As Long = &HFF00

Public Const HDXFR_PIXEL_MONO_8          As Long = &H1
Public Const HDXFR_PIXEL_YONLY_8         As Long = &H1
Public Const HDXFR_PIXEL_MONO_10         As Long = &H0
Public Const HDXFR_PIXEL_YCBCR422_16     As Long = &H0
Public Const HDXFR_PIXEL_YUVGRAY_16      As Long = &H2
Public Const HDXFR_PIXEL_RGB555_16       As Long = &H3
Public Const HDXFR_PIXEL_RGB888_32       As Long = &H4
Public Const HDXFR_PIXEL_RGB888_24       As Long = &H5
Public Const HDXFR_PIXEL_RGB565_16       As Long = &H6
Public Const HDXFR_PIXEL_RED_8           As Long = &H8
Public Const HDXFR_PIXEL_GREEN_8         As Long = &H9
Public Const HDXFR_PIXEL_BLUE_8          As Long = &HA
Public Const HDXFR_PIXEL_MASK            As Long = &HF
Public Const HDXFR_FGMEM_RETAIN          As Long = &H0
Public Const HDXFR_FGMEM_FLOAT           As Long = &H8
Public Const HDXFR_FGMEM_MASK            As Long = &H8
Public Const HDXFR_YMODE_DIB             As Long = &H100
Public Const HDXFR_YMODE_SKIP            As Long = &HF0
Public Const HDXFR_YMODE_SKIP_SHIFT      As Long = 4
Public Const HDXFR_XMODE_SKIP            As Long = &H200

Public Const HDSET_SYNCHR_OFF            As Long = &H0
Public Const HDSET_SYNCHR_ON             As Long = &H2

Public Const HDCHAN_CA1                  As Integer = 0
Public Const HDCHAN_CA2                  As Integer = 1
Public Const HDCHAN_CA3                  As Integer = 2
Public Const HDCHAN_CA4                  As Integer = 3
Public Const HDCHAN_SS12                 As Integer = 4
Public Const HDCHAN_SS12H                As Integer = 5
Public Const HDCHAN_SS12V                As Integer = 6
Public Const HDCHAN_SS12I                As Integer = 7
Public Const HDCHAN_CT1                  As Integer = 8
Public Const HDCHAN_CT2                  As Integer = 9
Public Const HDCHAN_CT3                  As Integer = 10
Public Const HDCHAN_CT4                  As Integer = 11
Public Const HDCHAN_CT1I                 As Integer = 12
Public Const HDCHAN_CT2I                 As Integer = 13
Public Const HDCHAN_CT3I                 As Integer = 14
Public Const HDCHAN_CT4I                 As Integer = 15
Public Const HDCHAN_SSA1T2               As Integer = 16
Public Const HDCHAN_SSA2T2               As Integer = 17
Public Const HDCHAN_SSA3T2               As Integer = 18
Public Const HDCHAN_SSA4T2               As Integer = 19
Public Const HD_NUM_SYNC_SOURCES         As Integer = 20
Public Const HD_NUM_SYNC_LINES           As Integer = 8

Public Const HPCMD_IDLE                  As Integer = &H0
Public Const HPCMD_SET                   As Integer = &H1
Public Const HPCMD_GRAB                  As Integer = &H2
Public Const HPCMD_XFER                  As Integer = &H3
Public Const HPCMD_ALT_IDLE              As Integer = &H4
Public Const HPCMD_CONT_SET              As Integer = &H5
Public Const HPCMD_CONT_GRAB             As Integer = &H6
Public Const HPCMD_CONT_XFER             As Integer = &H7
Public Const HPCMD_GRAB_ON_CT4R          As Integer = &HA
Public Const HPCMD_GRAB_ON_TRIG          As Integer = &HA
Public Const HPCMD_GRAB_ON_CT4F          As Integer = &HE
Public Const HPCMD_GRAB_TO_CT4R          As Integer = &H12
Public Const HPCMD_GRAB_TO_CT4F          As Integer = &H16

Public Const IDEA_INFO_NONE              As Integer = 0
Public Const IDEA_INFO_ERROR             As Integer = 1
Public Const IDEA_INFO_WARNING           As Integer = 2
Public Const IDEA_INFO_CONNECTION        As Integer = 3
Public Const IDEA_INFO_SYNC              As Integer = 4
Public Const IDEA_INFO_LOCK              As Integer = 5
Public Const IDEA_INFO_DROPPED_FRAMES    As Integer = 6
Public Const IDEA_INFO_TRIGGER_START     As Integer = 7
Public Const IDEA_INFO_TRIGGER_STOP      As Integer = 8
Public Const IDEA_INFO_VESA_SCAN_READY   As Integer = 9
Public Const IDEA_INFO_VESA_SCAN_START   As Integer = 10
Public Const IDEA_INFO_VESA_SCAN_DONE    As Integer = 11
Public Const IDEA_INFO_DIALOG_ACTION     As Integer = 12
Public Const IDEA_INFO_END               As Integer = 13

Public Type pFldType
    pFldArray(1 To 32) As Byte
End Type

Public Declare Function GetBoardInfo Lib "hdpw32.dll" _
Alias "nHP_GetBoardInfo" _
(ByVal BH As Integer, ByVal nFld As Integer, _
ByVal nMaxBytes As Integer, ByRef pFld As pFldType) As Integer

Public Declare Function ClaimBoard Lib "hdpw32.dll" _
Alias "bhHP_Claim" _
(ByVal wIOAddr As Integer, ByVal dw4KMem As Long) As Integer

Public Declare Sub UnclaimBoard Lib "hdpw32.dll" _
Alias "HP_UnClaim" _
(ByVal BH As Integer)

Public Type HD_sBoard
    nType As Integer
    wID As Integer
    wIO As Integer
    cID As Integer
    szSerial(1 To 8) As Byte
    BH As Integer
    bClaim As Integer
    nTest As Integer
    nResv As Integer
End Type

Public Type HD_sReport
    e As Long
    wRev As Integer
    nBrdCount As Integer
    nBrdList As Integer
    wResv As Integer
    dwFragDMA As Long
    dwFragHisto As Long
    bra(1 To 15) As HD_sBoard
End Type

Public Const SizeOfReport As Integer = 350

Public Declare Function nReport Lib "hdpw32.dll" _
Alias "nHP_Report" _
(ByVal wChecks As Integer, ByVal nRptSize As Integer, _
ByRef lpReport As HD_sReport) As Integer

Public Function MapBoardType(nType As Integer) As String
    Select Case nType
        Case HIDEF_ACCURA
            MapBoardType = "HI*DEF Accura"
        Case HIDEF_I25
            MapBoardType = "I-25"
        Case HIDEF_I50
            MapBoardType = "I-50"
        Case HIDEF_I60
            MapBoardType = "I-60"
        Case HIDEF_I25MV
            MapBoardType = "I-25 MV"
        Case HIDEF_I50MV
            MapBoardType = "I-50 MV"
        Case HIDEF_I60MV
            MapBoardType = "I-60 MV"
        Case HIDEF_ICOLOR
            MapBoardType = "I-Color"
        Case HIDEF_ICOLORMV
            MapBoardType = "I-Color MV"
        Case HIDEF_IRGB25
            MapBoardType = "I-RGB 25"
        Case HIDEF_IRGB50
            MapBoardType = "I-RGB 50"
        Case HIDEF_IRGB60
            MapBoardType = "I-RGB 60"
        Case HIDEF_IRGB25MV
            MapBoardType = "I-RGB 25 MV"
        Case HIDEF_IRGB50MV
            MapBoardType = "I-RGB 50 MV"
        Case HIDEF_IRGB60MV
            MapBoardType = "I-RGB 60 MV"
        Case HIDEF_I75
            MapBoardType = "I-75"
        Case HIDEF_I75MV
            MapBoardType = "I-75 MV"
        Case HIDEF_IRGB75
            MapBoardType = "I-RGB 75"
        Case HIDEF_IRGB75MV
            MapBoardType = "I-RGB 75 MV"
        Case HIDEF_IRGB165
            MapBoardType = "I-RGB 165"
        Case HIDEF_IRGB170
            MapBoardType = "I-RGB 170/DVI"
        Case HIDEF_IRGB200
            MapBoardType = "I-RGB 200"
        Case HIDEF_IRGBI64_170
            MapBoardType = "AccuStream 170"
        Case HIDEF_I50HSN
            MapBoardType = "I-50 HSN"
        Case Else
            MapBoardType = "Unknown"
    End Select
End Function
