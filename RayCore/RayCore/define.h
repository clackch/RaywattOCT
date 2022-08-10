#pragma once

typedef void (*FunctionPtr)(int, int);
typedef void (*FunctionImgPtr)(void*, int, int, int);

enum class RayError {
	OK = 0,
	InvalidArgument = -1000,
};

enum class RayProperty {
	Unknown = 0,
	Brightness = 1,
	Contrast,
};

enum class RayViewMode {
	Unknown = 0,
	StandBy,
	LiveView
};