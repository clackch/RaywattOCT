#pragma once

#include "DXUT.h"

#define RESULT_FAIL_RETURN_NULL(x)   if (x != S_OK) { return nullptr; }

using namespace DirectX;

class Utility
{
public:
	static DWORD GetShaderFlag()
	{
		DWORD dwShaderFlags = D3DCOMPILE_ENABLE_STRICTNESS;
#ifdef _DEBUG
		dwShaderFlags |= D3DCOMPILE_DEBUG;
		dwShaderFlags |= D3DCOMPILE_SKIP_OPTIMIZATION;
#endif
		return dwShaderFlags;
	}

	template <typename T>
	static T clip(const T& n, const T& lower, const T& upper) {
		return std::max(lower, std::min(n, upper));
	}

	template <typename T>
	static T lerp(T begin, T end, float ratio)
	{
		return clip<T>(begin + (T)(ratio * (end - begin)), begin, end);
	}

	static XMFLOAT4 GetColorFloat4(BYTE r, BYTE g, BYTE b)
	{
		return XMFLOAT4((float)r / 255.f, (float)g / 255.f, (float)b / 255.f, 1);
	}

	static XMFLOAT4 GetColorFloat4(BYTE r, BYTE g, BYTE b, BYTE a)
	{
		return XMFLOAT4((float)r / 255.f, (float)g / 255.f, (float)b / 255.f, (float)a / 255.f);
	}
};

