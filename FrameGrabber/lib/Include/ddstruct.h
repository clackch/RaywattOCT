#if !defined (_FSI_DDSTRUCT_H_)
#define _FSI_DDSTRUCT_H_

typedef struct tagDDSurface {
  void  *pBaseAddress;
  DWORD dwWidth;
  DWORD dwHeight;
  DWORD dwPitch;
  DWORD dwBitsPerPixel;
  DWORD dwFourCC;
} DD_SURFACE;

#endif