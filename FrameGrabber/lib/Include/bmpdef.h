/* BMP File Format */

/* File Header */
#define   wFType                                           0
#define  dwFSize                          wFType          +2
#define   wFReserved1                    dwFSize          +4
#define   wFReserved2                     wFReserved1     +2
#define  dwFOffBits                       wFReserved2     +2
#define  SIZEOFBITMAPFILEHEADER          dwFOffBits       +4

/* Info Header */
#define  dwISize                      SIZEOFBITMAPFILEHEADER
#define   lIWidth                        dwISize          +4
#define   lIHeight                        lIWidth         +4
#define   wIPlanes                        lIHeight        +4
#define   wIBitCount                      wIPlanes        +2
#define  dwICompression                   wIBitCount      +2
#define  dwISizeImage                    dwICompression   +4
#define   lIXPelsPerMeter                dwISizeImage     +4
#define   lIYPelsPerMeter                lIXPelsPerMeter  +4
#define  dwIClrUsed                      lIYPelsPerMeter  +4
#define  dwIClrImportant                dwIClrUsed        +4
#define  SIZEOFBITMAPINFO               dwIClrImportant   +4

#define  SIZEOFBITMAPINFOHEADER ((SIZEOFBITMAPINFO)-(SIZEOFBITMAPFILEHEADER))

/* Color Map */
#define   aIColorMap                  SIZEOFBITMAPINFO

#define  SIZEOFBMPHEADERNOPALETTE        (aIColorMap    ) 
#define  SIZEOFBMPHEADER                 (aIColorMap    +1024) 

/* Data */
#define   aIData                      SIZEOFBMPHEADER

#if defined (LINUX)
#define BI_RGB 0
typedef struct tagRGBQUAD
{
  BYTE rgbBlue;
  BYTE rgbGreen;
  BYTE rgbRed;
  BYTE rgbReserved;
} RGBQUAD;

typedef struct tagBmp {
  unsigned long  dwSize;
  unsigned long  lWidth;
  unsigned long  lHeight;
  unsigned short  wPlanes;
  unsigned short  wBitCount;
  unsigned long  dwCompression;
  unsigned long  dwSizeImage;
  unsigned long lXPelsPerMeter;
  unsigned long lYPelsPerMeter;
  unsigned long dwClrUsed;
  unsigned long dwClrImportant;
} BITMAPINFOHEADER;

typedef struct tagBmpInfo
{
  BITMAPINFOHEADER bmiHeader;
  RGBQUAD           bmiColors[1];
} BITMAPINFO;
#endif