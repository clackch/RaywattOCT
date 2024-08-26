#pragma once
#include "stdint.h"
//#include "minwindef.h"

typedef enum
{
  IDEA_GPU_NONE = 0,
  IDEA_GPU_NVIDIA,
  IDEA_GPU_AMD
} IDEA_GPU_TYPE;

typedef enum
{
  IDEA_RENDERER_OPENGL = 0,
  IDEA_RENDERER_DX9,
  IDEA_RENDERER_DX11,
  IDEA_RENDERER_CUDA
} IDEA_RENDERER_TYPE;

typedef uint64_t DVPBufferHandle;
typedef uint64_t DVPSyncObjectHandle;
struct cudaGraphicsResource;
typedef unsigned int GLuint;

typedef struct
{
  volatile uint32_t* sem;
  volatile uint32_t* semOrg;
  volatile uint32_t releaseValue;
  volatile uint32_t acquireValue;
  DVPSyncObjectHandle syncObj;
} GPU_SYNC_INFO;

typedef struct
{
  int nBufferIndex;
  BOOL  bTexture;      //texture or unformatted buffer
  void* sysMemAlloc;
  void* sysMemBuffer;

  void* extDevAlloc;
  void* extDevBuffer;

  DVPBufferHandle  dvpSysMemHandle;
  GPU_SYNC_INFO    extSync;

  DVPBufferHandle  dvpGpuObjectHandle;
  GPU_SYNC_INFO    gpuSync;

  volatile uint32_t       lastFrameSyncValue;
  uint32_t width;
  uint32_t height;
  uint32_t size;
  uint32_t numChunks;
  void* device;

  GLuint                  gpuGlInputHandle;
  struct cudaGraphicsResource* pgpuCudaInputResource;

  //ID3D11Texture2D* gpuID3D11TextureHandle;
  //ID3D11Buffer* gpuID3D11BufferHandle;
  //IDirect3DTexture9* gpuIDirect3DTexture9Handle;
  //IDirect3DVertexBuffer9* gpuIDirect3DVertexBuffer9Handle;
  //CUarray                 gpuCudaTextureHandle;
  //CUdeviceptr             gpuCudaBufferHandle;
} GPU_BUFFER_DESCRIPTOR;


typedef struct
{
  IDEA_RENDERER_TYPE RendererType;
  uint32_t Width;
  uint32_t Height;
  uint32_t bUseTexture;
  uint32_t NumberOfBuffers;
  HDVID_HEADER* pVidHeaders;
  IDEA_TYPE DataType;

  uint32_t BufferAddrAlignment;
  uint32_t BufferGPUStrideAlignment;
  uint32_t SemaphoreAddrAlignment;
  uint32_t SemaphoreAllocSize;
  uint32_t SemaphorePayloadOffset;
  uint32_t SemaphorePayloadSize;
} GPU_PARAMETERS;

