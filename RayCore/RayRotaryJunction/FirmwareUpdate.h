#pragma once
#include "Config.h"

// Firmware Download State
enum class eFWDownloadState : BYTE {
	Idle = 0,
	Downloading,
	Success,
	Failed,
	Cancelled
};

// Firmware Metadata Structure (last 16 bytes of firmware file)
struct SFirmwareMetadata {
	UINT hwver;		// Hardware version
	UINT fwver;		// Firmware version
	UINT chkver;	// Checksum verification: ((hwver & 0xffff) << 16) + fwver
	UINT length;	// Flash address (0x08010000 ~ 0x08080000)
};

// Callback function types
typedef void (*FWProgressCallback)(int progress);			// Progress: 0~100
typedef void (*FWStatusCallback)(eFWDownloadState state);	// State change callback
