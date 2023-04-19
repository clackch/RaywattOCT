/******************************************************************************
 *                                                                         
 * Copyright (C) Acqiris SA 2018-2023
 *
 *****************************************************************************/

#ifndef __AQSSOCT_HEADER
#define __AQSSOCT_HEADER

#include <IviVisaType.h>

#if defined(__cplusplus) || defined(__cplusplus__)
extern "C" {
#endif

/**************************************************************************** 
 *---------------------------- Attribute Defines ---------------------------* 
 ****************************************************************************/
#ifndef IVI_ATTR_BASE
#define IVI_ATTR_BASE                 1000000
#endif

#ifndef IVI_INHERENT_ATTR_BASE		        
#define IVI_INHERENT_ATTR_BASE        (IVI_ATTR_BASE +  50000)   /* base for inherent capability attributes */
#endif

#ifndef IVI_CLASS_ATTR_BASE           
#define IVI_CLASS_ATTR_BASE           (IVI_ATTR_BASE + 250000)   /* base for IVI-defined class attributes */
#endif

#ifndef IVI_LXISYNC_ATTR_BASE         
#define IVI_LXISYNC_ATTR_BASE         (IVI_ATTR_BASE + 950000)   /* base for IviLxiSync attributes */
#endif

#ifndef IVI_SPECIFIC_ATTR_BASE        
#define IVI_SPECIFIC_ATTR_BASE        (IVI_ATTR_BASE + 150000)   /* base for attributes of specific drivers */
#endif


/*===== IVI Inherent Instrument Attributes ==============================*/    

/*- Driver Identification */

#define AQSSOCT_ATTR_SPECIFIC_DRIVER_DESCRIPTION              (IVI_INHERENT_ATTR_BASE + 514L)  /* ViString, read-only */
#define AQSSOCT_ATTR_SPECIFIC_DRIVER_PREFIX                   (IVI_INHERENT_ATTR_BASE + 302L)  /* ViString, read-only */
#define AQSSOCT_ATTR_SPECIFIC_DRIVER_VENDOR                   (IVI_INHERENT_ATTR_BASE + 513L)  /* ViString, read-only */
#define AQSSOCT_ATTR_SPECIFIC_DRIVER_REVISION                 (IVI_INHERENT_ATTR_BASE + 551L)  /* ViString, read-only */
#define AQSSOCT_ATTR_SPECIFIC_DRIVER_CLASS_SPEC_MAJOR_VERSION (IVI_INHERENT_ATTR_BASE + 515L)  /* ViInt32, read-only */
#define AQSSOCT_ATTR_SPECIFIC_DRIVER_CLASS_SPEC_MINOR_VERSION (IVI_INHERENT_ATTR_BASE + 516L)  /* ViInt32, read-only */

/*- User Options */

#define AQSSOCT_ATTR_RANGE_CHECK                            (IVI_INHERENT_ATTR_BASE + 2L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_QUERY_INSTRUMENT_STATUS                (IVI_INHERENT_ATTR_BASE + 3L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_CACHE                                  (IVI_INHERENT_ATTR_BASE + 4L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_SIMULATE                               (IVI_INHERENT_ATTR_BASE + 5L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_RECORD_COERCIONS                       (IVI_INHERENT_ATTR_BASE + 6L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_INTERCHANGE_CHECK                      (IVI_INHERENT_ATTR_BASE + 21L)  /* ViBoolean, read-write */

/*- Advanced Session Information */

#define AQSSOCT_ATTR_LOGICAL_NAME                           (IVI_INHERENT_ATTR_BASE + 305L)  /* ViString, read-only */
#define AQSSOCT_ATTR_IO_RESOURCE_DESCRIPTOR                 (IVI_INHERENT_ATTR_BASE + 304L)  /* ViString, read-only */
#define AQSSOCT_ATTR_DRIVER_SETUP                           (IVI_INHERENT_ATTR_BASE + 7L)  /* ViString, read-only */

/*- Driver Capabilities */

#define AQSSOCT_ATTR_GROUP_CAPABILITIES                     (IVI_INHERENT_ATTR_BASE + 401L)  /* ViString, read-only */
#define AQSSOCT_ATTR_SUPPORTED_INSTRUMENT_MODELS            (IVI_INHERENT_ATTR_BASE + 327L)  /* ViString, read-only */

/*- Instrument Identification */

#define AQSSOCT_ATTR_INSTRUMENT_FIRMWARE_REVISION           (IVI_INHERENT_ATTR_BASE + 510L)  /* ViString, read-only */
#define AQSSOCT_ATTR_INSTRUMENT_MANUFACTURER                (IVI_INHERENT_ATTR_BASE + 511L)  /* ViString, read-only */
#define AQSSOCT_ATTR_INSTRUMENT_MODEL                       (IVI_INHERENT_ATTR_BASE + 512L)  /* ViString, read-only */


/*===== Instrument-Specific Attributes =====================================*/

/*- Instrument Specific */

#define AQSSOCT_ATTR_INSTRUMENT_SESSION                     (IVI_SPECIFIC_ATTR_BASE + 25L)  /* ViSession, read-only */

/*- AScan */

#define AQSSOCT_ATTR_ASCAN_DECIMATION_RATIO                 (IVI_SPECIFIC_ATTR_BASE + 1L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_ASCAN_PRIMARY_SWEEP_SYNC               (IVI_SPECIFIC_ATTR_BASE + 9L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_ASCAN_SECONDARY_SWEEP_ENABLED          (IVI_SPECIFIC_ATTR_BASE + 10L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_ASCAN_TRIGGER_LEVEL                    (IVI_SPECIFIC_ATTR_BASE + 11L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_ASCAN_TRIGGER_DELAY_PRIMARY_SWEEP      (IVI_SPECIFIC_ATTR_BASE + 12L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_ASCAN_TRIGGER_DELAY_SECONDARY_SWEEP    (IVI_SPECIFIC_ATTR_BASE + 13L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_ASCAN_TRIGGER_RATE                     (IVI_SPECIFIC_ATTR_BASE + 14L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_ASCAN_ACQUISITION_SIZE                 (IVI_SPECIFIC_ATTR_BASE + 27L)  /* ViInt32, read-write */

/*- Image */

#define AQSSOCT_ATTR_IMAGE_ASCAN_SIZE                       (IVI_SPECIFIC_ATTR_BASE + 18L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_BSCAN_SIZE                       (IVI_SPECIFIC_ATTR_BASE + 19L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_BSCAN_DELAY                      (IVI_SPECIFIC_ATTR_BASE + 20L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_BSCAN_TRIGGER_MODE               (IVI_SPECIFIC_ATTR_BASE + 21L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_CSCAN_SIZE                       (IVI_SPECIFIC_ATTR_BASE + 22L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_CSCAN_DELAY                      (IVI_SPECIFIC_ATTR_BASE + 23L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_CSCAN_TRIGGER_MODE               (IVI_SPECIFIC_ATTR_BASE + 24L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_DATA_FORMAT                      (IVI_SPECIFIC_ATTR_BASE + 28L)  /* ViInt32, read-write */

/*- Processing */

#define AQSSOCT_ATTR_IMAGE_PROCESSING_ASCAN_NUMBER_OF_AVERAGES   (IVI_SPECIFIC_ATTR_BASE + 15L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_PROCESSING_BACKGROUND_REMOVAL_ENABLED (IVI_SPECIFIC_ATTR_BASE + 16L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_IMAGE_PROCESSING_FFT_WINDOW_TYPE            (IVI_SPECIFIC_ATTR_BASE + 29L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_PROCESSING_RESAMPLING_MODE            (IVI_SPECIFIC_ATTR_BASE + 51L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_PROCESSING_RESAMPLING_STEP            (IVI_SPECIFIC_ATTR_BASE + 52L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_IMAGE_PROCESSING_ASCAN_AVERAGE_TYPE         (IVI_SPECIFIC_ATTR_BASE + 55L)  /* ViInt32, read-write */

/*- Advanced */

#define AQSSOCT_ATTR_IMAGE_ADVANCED_HEADER_ENABLED          (IVI_SPECIFIC_ATTR_BASE + 32L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_IMAGE_ADVANCED_IS_STREAM_OVERFLOW      (IVI_SPECIFIC_ATTR_BASE + 46L)  /* ViBoolean, read-only */
#define AQSSOCT_ATTR_IMAGE_ADVANCED_DESCRIPTOR_ENABLED      (IVI_SPECIFIC_ATTR_BASE + 54L)  /* ViBoolean, read-write */

/*- KClock */

#define AQSSOCT_ATTR_KCLOCK_DELAY_PRIMARY_SWEEP             (IVI_SPECIFIC_ATTR_BASE + 2L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_KCLOCK_DELAY_SECONDARY_SWEEP           (IVI_SPECIFIC_ATTR_BASE + 3L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_KCLOCK_HILBERT_GAIN                    (IVI_SPECIFIC_ATTR_BASE + 4L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_KCLOCK_HILBERT_OFFSET                  (IVI_SPECIFIC_ATTR_BASE + 5L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_KCLOCK_CENTER_FREQUENCY                (IVI_SPECIFIC_ATTR_BASE + 6L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_KCLOCK_FILTER_MODE                     (IVI_SPECIFIC_ATTR_BASE + 26L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_KCLOCK_REMAPPING_MODE                  (IVI_SPECIFIC_ATTR_BASE + 8L)  /* ViInt32, read-write */

/*- InstrumentInfo */

#define AQSSOCT_ATTR_INSTRUMENT_INFO_SERIAL_NUMBER_STRING   (IVI_SPECIFIC_ATTR_BASE + 30L)  /* ViString, read-only */
#define AQSSOCT_ATTR_INSTRUMENT_INFO_OPTIONS                (IVI_SPECIFIC_ATTR_BASE + 31L)  /* ViString, read-only */

/*- Calibration */

#define AQSSOCT_ATTR_CALIBRATION_IS_REQUIRED                (IVI_SPECIFIC_ATTR_BASE + 34L)  /* ViBoolean, read-only */

/*- ScannerControl */

#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_MODE        (IVI_SPECIFIC_ATTR_BASE + 35L)  /* ViInt32, read-write */

/*- AnalogOut */

#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_COUNT          (IVI_SPECIFIC_ATTR_BASE + 36L)  /* ViInt32, read-only */
#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_ENABLED        (IVI_SPECIFIC_ATTR_BASE + 37L)  /* ViBoolean, read-write */
#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_VOLTAGE_RANGE  (IVI_SPECIFIC_ATTR_BASE + 38L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_VOLTAGE_OFFSET (IVI_SPECIFIC_ATTR_BASE + 39L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_SETTLING_DELAY (IVI_SPECIFIC_ATTR_BASE + 40L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_VOLTAGE_LEVEL  (IVI_SPECIFIC_ATTR_BASE + 47L)  /* ViReal64, read-write */

/*- ControlIO */

#define AQSSOCT_ATTR_CONTROL_IO_COUNT                       (IVI_SPECIFIC_ATTR_BASE + 41L)  /* ViInt32, read-only */
#define AQSSOCT_ATTR_CONTROL_IO_SIGNAL                      (IVI_SPECIFIC_ATTR_BASE + 42L)  /* ViString, read-write */
#define AQSSOCT_ATTR_SYNC_OUTPUT_PULSE_WIDTH                (IVI_SPECIFIC_ATTR_BASE + 45L)  /* ViInt32, read-write */

/*- Channel */

#define AQSSOCT_ATTR_CHANNEL_COUNT                          (IVI_SPECIFIC_ATTR_BASE + 48L)  /* ViInt32, read-only */
#define AQSSOCT_ATTR_VERTICAL_OFFSET                        (IVI_SPECIFIC_ATTR_BASE + 49L)  /* ViReal64, read-write */
#define AQSSOCT_ATTR_VERTICAL_RANGE                         (IVI_SPECIFIC_ATTR_BASE + 50L)  /* ViReal64, read-write */

/*- Temperature */

#define AQSSOCT_ATTR_BOARD_TEMPERATURE                      (IVI_SPECIFIC_ATTR_BASE + 43L)  /* ViReal64, read-only */
#define AQSSOCT_ATTR_TEMPERATURE_UNITS                      (IVI_SPECIFIC_ATTR_BASE + 44L)  /* ViInt32, read-write */
#define AQSSOCT_ATTR_CHANNEL_TEMPERATURE                    (IVI_SPECIFIC_ATTR_BASE + 53L)  /* ViReal64, read-only */


/**************************************************************************** 
 *------------------------ Attribute Value Defines -------------------------* 
 ****************************************************************************/

/*- Defined values for 
	attribute AQSSOCT_ATTR_IMAGE_DATA_FORMAT
	parameter DataFormat in function AqSsoct_FetchImageInt16
	parameter DataFormat in function AqSsoct_ImageConfigure
	parameter DataFormat in function AqSsoct_QueryMinDataMemory
	parameter DataFormat in function AqSsoct_FetchImageInt8 */

#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_RAW                        0
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LOG          1
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LIN          2
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LOG_PHASE    3
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LIN_PHASE    4
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_COMPLEX                5
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_REMAPPED                   6
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LIN_HALF_LOW 7
#define AQSSOCT_VAL_IMAGE_DATA_FORMAT_FFT_MAGNITUDE_LOG_HALF_LOW 8

/*- Defined values for 
	attribute AQSSOCT_ATTR_KCLOCK_REMAPPING_MODE */

#define AQSSOCT_VAL_KCLOCK_REMAPPING_MODE_CONTINUOUS        0
#define AQSSOCT_VAL_KCLOCK_REMAPPING_MODE_CALIBRATED_ONCE   1

/*- Defined values for 
	attribute AQSSOCT_ATTR_ASCAN_PRIMARY_SWEEP_SYNC */

#define AQSSOCT_VAL_ASCAN_PRIMARY_SWEEP_SYNC_EDGE_POSITIVE  0
#define AQSSOCT_VAL_ASCAN_PRIMARY_SWEEP_SYNC_EDGE_NEGATIVE  1
#define AQSSOCT_VAL_ASCAN_PRIMARY_SWEEP_SYNC_ASCAN_PULSE    2

/*- Defined values for 
	attribute AQSSOCT_ATTR_IMAGE_BSCAN_TRIGGER_MODE
	attribute AQSSOCT_ATTR_IMAGE_CSCAN_TRIGGER_MODE
	parameter BscanTrigMode in function AqSsoct_ImageConfigure
	parameter CscanTrigMode in function AqSsoct_ImageConfigure */

#define AQSSOCT_VAL_SCAN_TRIGGER_MODE_SOFTWARE              0
#define AQSSOCT_VAL_SCAN_TRIGGER_MODE_PIO_TRIGGER           1
#define AQSSOCT_VAL_SCAN_TRIGGER_MODE_FREE_RUNNING          2

/*- Defined values for 
	attribute AQSSOCT_ATTR_KCLOCK_FILTER_MODE */

#define AQSSOCT_VAL_KCLOCK_FILTER_MODE_DISABLED             0
#define AQSSOCT_VAL_KCLOCK_FILTER_MODE_DEFAULT              1
#define AQSSOCT_VAL_KCLOCK_FILTER_MODE_CUSTOM               2

/*- Defined values for 
	attribute AQSSOCT_ATTR_IMAGE_PROCESSING_FFT_WINDOW_TYPE */

#define AQSSOCT_VAL_IMAGE_PROCESSING_FFT_WINDOW_NONE        0
#define AQSSOCT_VAL_IMAGE_PROCESSING_FFT_WINDOW_DEFAULT     1
#define AQSSOCT_VAL_IMAGE_PROCESSING_FFT_WINDOW_CUSTOM      2

/*- Defined values for 
	parameter Sweep in function AqSsoct_ProcessingLoadFftWindow */

#define AQSSOCT_VAL_SWEEP_IDENTIFIER_PRIMARY                0
#define AQSSOCT_VAL_SWEEP_IDENTIFIER_SECONDARY              1

/*- Defined values for 
	attribute AQSSOCT_ATTR_SCANNER_CONTROL_ANALOG_OUT_MODE */

#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_SAWTOOTH       0
#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_DUAL_SWEEP     1
#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_CUSTOM         2
#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_CUSTOM_XA_YB   3
#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_CUSTOM_XA_YA   4
#define AQSSOCT_VAL_SCANNER_CONTROL_ANALOG_OUT_MODE_CUSTOM_XBA_YBA 5

/*- Defined values for 
	attribute AQSSOCT_ATTR_TEMPERATURE_UNITS */

#define AQSSOCT_VAL_CELSIUS                                 0
#define AQSSOCT_VAL_FAHRENHEIT                              1
#define AQSSOCT_VAL_KELVIN                                  2

/*- Defined values for 
	attribute AQSSOCT_ATTR_IMAGE_PROCESSING_RESAMPLING_MODE */

#define AQSSOCT_VAL_RESAMPLING_MODE_DEFAULT                 0
#define AQSSOCT_VAL_RESAMPLING_MODE_ADVANCED                1

/*- Defined values for 
	attribute AQSSOCT_ATTR_IMAGE_PROCESSING_ASCAN_AVERAGE_TYPE */

#define AQSSOCT_VAL_ASCAN_AVERAGE_TYPE_DEFAULT              0
#define AQSSOCT_VAL_ASCAN_AVERAGE_TYPE_MOVING               1


/**************************************************************************** 
 *---------------- Instrument Driver Function Declarations -----------------* 
 ****************************************************************************/

/*- AqSsoct */

ViStatus _VI_FUNC AqSsoct_init(ViRsrc ResourceName, ViBoolean IdQuery, ViBoolean Reset, ViSession* Vi);
ViStatus _VI_FUNC AqSsoct_close(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_InitWithOptions(ViRsrc ResourceName, ViBoolean IdQuery, ViBoolean Reset, ViConstString OptionsString, ViSession* Vi);

/*- AScan */

ViStatus _VI_FUNC AqSsoct_AScanStart(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_AScanAbort(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_AScanConfigure(ViSession Vi, ViInt32 AcquisitionSize, ViInt32 DecimationRatio, ViInt32 PrimarySweepSync, ViBoolean SecondarySweepEnabled);
ViStatus _VI_FUNC AqSsoct_AScanConfigureTrigger(ViSession Vi, ViReal64 Level, ViInt32 TrDelayPrimarySweep, ViInt32 TrDelaySecondarySweep);
ViStatus _VI_FUNC AqSsoct_SendBScanSoftwareTrigger(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_SendCScanSoftwareTrigger(ViSession Vi);

/*- Image */

ViStatus _VI_FUNC AqSsoct_FetchImageInt16(ViSession Vi, ViConstString ChannelName, ViInt64 NbrAscansToFetch, ViInt64 ArrayBufferSize, ViInt16 Array[], ViInt32* ArrayActualSize, ViInt32* DataFormat, ViInt64* HeaderSize, ViInt64* DataSize, ViInt64* AvailableAscans, ViInt64* ActualAscans, ViInt64* FirstValidElement);
ViStatus _VI_FUNC AqSsoct_ImageConfigure(ViSession Vi, ViInt32 DataFormat, ViInt64 AscanSize, ViInt32 BscanSize, ViInt32 BscanDelay, ViInt32 BscanTrigMode, ViInt32 CscanSize, ViInt32 CscanDelay, ViInt32 CscanTrigMode);
ViStatus _VI_FUNC AqSsoct_QueryMinDataMemory(ViSession Vi, ViInt32 DataFormat, ViInt32 DataWidth, ViInt64 NumAScans, ViInt64* NumSamples);
ViStatus _VI_FUNC AqSsoct_FetchImageInt8(ViSession Vi, ViConstString ChannelName, ViInt64 NbrAscansToFetch, ViInt64 ArrayBufferSize, ViInt8 Array[], ViInt32* ArrayActualSize, ViInt32* DataFormat, ViInt64* HeaderSize, ViInt64* DataSize, ViInt64* AvailableAscans, ViInt64* ActualAscans, ViInt64* FirstValidElement);
ViStatus _VI_FUNC AqSsoct_QueryMinDescriptorMemory(ViSession Vi, ViInt32 DataWidth, ViInt64 NumAScans, ViInt64* NumElements);
ViStatus _VI_FUNC AqSsoct_FetchDescriptorInt32(ViSession Vi, ViInt64 NbrDescriptorToFetch, ViInt64 ArrayBufferSize, ViInt32 Array[], ViInt32* ArrayActualSize, ViInt64* DescriptorSize, ViInt64* AvailableDescriptors, ViInt64* ActualDescriptors, ViInt64* FirstValidElement);

/*- Processing */

ViStatus _VI_FUNC AqSsoct_ProcessingLoadFftWindow(ViSession Vi, ViInt32 Sweep, ViInt32 CoefficientsBufferSize, ViInt16 Coefficients[]);
ViStatus _VI_FUNC AqSsoct_ProcessingAcquireBackground(ViSession Vi, ViInt32 MaxTimeMilliseconds);
ViStatus _VI_FUNC AqSsoct_ProcessingLoadBackground(ViSession Vi, ViConstString ChannelName, ViInt32 ArrayBufferSize, ViInt32 Array[]);
ViStatus _VI_FUNC AqSsoct_ProcessingReadBackground(ViSession Vi, ViConstString ChannelName, ViInt32 ArrayBufferSize, ViInt32 Array[], ViInt32* ArrayActualSize);
ViStatus _VI_FUNC AqSsoct_ProcessingAcquireKClock(ViSession Vi, ViInt32 MaxTimeMilliseconds);

/*- KClock */

ViStatus _VI_FUNC AqSsoct_KClockLoadFilter(ViSession Vi, ViInt32 CoefficientsBufferSize, ViReal64 Coefficients[]);

/*- Calibration */

ViStatus _VI_FUNC AqSsoct_SelfCalibrate(ViSession Vi);

/*- AnalogOut */

ViStatus _VI_FUNC AqSsoct_GetAnalogOutName(ViSession Vi, ViInt32 Index, ViInt32 NameBufferSize, ViChar Name[]);
ViStatus _VI_FUNC AqSsoct_AnalogOutLoadPattern(ViSession Vi, ViConstString AnalogOut, ViInt32 PatternBufferSize, ViReal64 Pattern[]);

/*- ControlIO */

ViStatus _VI_FUNC AqSsoct_GetControlIOName(ViSession Vi, ViInt32 Index, ViInt32 NameBufferSize, ViChar Name[]);

/*- Channel */

ViStatus _VI_FUNC AqSsoct_GetChannelName(ViSession Vi, ViInt32 Index, ViInt32 NameBufferSize, ViChar Name[]);
ViStatus _VI_FUNC AqSsoct_LoadChannelFilter(ViSession Vi, ViConstString ChannelName, ViInt32 CoefficientsBufferSize, ViReal64 Coefficients[]);
ViStatus _VI_FUNC AqSsoct_ConfigureChannel(ViSession Vi, ViConstString ChannelName, ViReal64 Range, ViReal64 Offset);

/*- Utility */

ViStatus _VI_FUNC AqSsoct_revision_query(ViSession Vi, ViChar DriverRev[], ViChar InstrRev[]);
ViStatus _VI_FUNC AqSsoct_error_message(ViSession Vi, ViStatus ErrorCode, ViChar ErrorMessage[]);
ViStatus _VI_FUNC AqSsoct_GetError(ViSession Vi, ViStatus* ErrorCode, ViInt32 ErrorDescriptionBufferSize, ViChar ErrorDescription[]);
ViStatus _VI_FUNC AqSsoct_ClearError(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_ClearInterchangeWarnings(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_GetNextCoercionRecord(ViSession Vi, ViInt32 CoercionRecordBufferSize, ViChar CoercionRecord[]);
ViStatus _VI_FUNC AqSsoct_GetNextInterchangeWarning(ViSession Vi, ViInt32 InterchangeWarningBufferSize, ViChar InterchangeWarning[]);
ViStatus _VI_FUNC AqSsoct_InvalidateAllAttributes(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_ResetInterchangeCheck(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_Disable(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_error_query(ViSession Vi, ViInt32* ErrorCode, ViChar ErrorMessage[]);
ViStatus _VI_FUNC AqSsoct_LockSession(ViSession Vi, ViBoolean* CallerHasLock);
ViStatus _VI_FUNC AqSsoct_reset(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_ResetWithDefaults(ViSession Vi);
ViStatus _VI_FUNC AqSsoct_self_test(ViSession Vi, ViInt16* TestResult, ViChar TestMessage[]);
ViStatus _VI_FUNC AqSsoct_UnlockSession(ViSession Vi, ViBoolean* CallerHasLock);

/*- Attribute Accessors */

ViStatus _VI_FUNC AqSsoct_GetAttributeViInt32(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViInt32* AttributeValue);
ViStatus _VI_FUNC AqSsoct_GetAttributeViReal64(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViReal64* AttributeValue);
ViStatus _VI_FUNC AqSsoct_GetAttributeViBoolean(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViBoolean* AttributeValue);
ViStatus _VI_FUNC AqSsoct_GetAttributeViSession(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViSession* AttributeValue);
ViStatus _VI_FUNC AqSsoct_GetAttributeViString(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViInt32 AttributeValueBufferSize, ViChar AttributeValue[]);
ViStatus _VI_FUNC AqSsoct_SetAttributeViInt32(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViInt32 AttributeValue);
ViStatus _VI_FUNC AqSsoct_SetAttributeViReal64(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViReal64 AttributeValue);
ViStatus _VI_FUNC AqSsoct_SetAttributeViBoolean(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViBoolean AttributeValue);
ViStatus _VI_FUNC AqSsoct_SetAttributeViSession(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViSession AttributeValue);
ViStatus _VI_FUNC AqSsoct_SetAttributeViString(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViConstString AttributeValue);
ViStatus _VI_FUNC AqSsoct_GetAttributeViInt64(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViInt64* AttributeValue);
ViStatus _VI_FUNC AqSsoct_SetAttributeViInt64(ViSession Vi, ViConstString RepCapIdentifier, ViAttr AttributeID, ViInt64 AttributeValue);


/**************************************************************************** 
 *----------------- Instrument Error And Completion Codes ------------------* 
 ****************************************************************************/
#ifndef _IVIC_ERROR_BASE_DEFINES_
#define _IVIC_ERROR_BASE_DEFINES_

#define IVIC_WARN_BASE                           (0x3FFA0000L)
#define IVIC_CROSS_CLASS_WARN_BASE               (IVIC_WARN_BASE + 0x1000)
#define IVIC_CLASS_WARN_BASE                     (IVIC_WARN_BASE + 0x2000)
#define IVIC_SPECIFIC_WARN_BASE                  (IVIC_WARN_BASE + 0x4000)

#define IVIC_ERROR_BASE                          (0xBFFA0000L)
#define IVIC_CROSS_CLASS_ERROR_BASE              (IVIC_ERROR_BASE + 0x1000)
#define IVIC_CLASS_ERROR_BASE                    (IVIC_ERROR_BASE + 0x2000)
#define IVIC_SPECIFIC_ERROR_BASE                 (IVIC_ERROR_BASE + 0x4000)
#define IVIC_LXISYNC_ERROR_BASE                  (IVIC_ERROR_BASE + 0x2000)

#endif


#define AQSSOCT_ERROR_CANNOT_RECOVER                        (IVIC_ERROR_BASE + 0x0000)
#define AQSSOCT_ERROR_INSTRUMENT_STATUS                     (IVIC_ERROR_BASE + 0x0001)
#define AQSSOCT_ERROR_CANNOT_OPEN_FILE                      (IVIC_ERROR_BASE + 0x0002)
#define AQSSOCT_ERROR_READING_FILE                          (IVIC_ERROR_BASE + 0x0003)
#define AQSSOCT_ERROR_WRITING_FILE                          (IVIC_ERROR_BASE + 0x0004)
#define AQSSOCT_ERROR_INVALID_PATHNAME                      (IVIC_ERROR_BASE + 0x000B)
#define AQSSOCT_ERROR_INVALID_ATTRIBUTE                     (IVIC_ERROR_BASE + 0x000C)
#define AQSSOCT_ERROR_IVI_ATTR_NOT_WRITABLE                 (IVIC_ERROR_BASE + 0x000D)
#define AQSSOCT_ERROR_IVI_ATTR_NOT_READABLE                 (IVIC_ERROR_BASE + 0x000E)
#define AQSSOCT_ERROR_INVALID_VALUE                         (IVIC_ERROR_BASE + 0x0010)
#define AQSSOCT_ERROR_FUNCTION_NOT_SUPPORTED                (IVIC_ERROR_BASE + 0x0011)
#define AQSSOCT_ERROR_ATTRIBUTE_NOT_SUPPORTED               (IVIC_ERROR_BASE + 0x0012)
#define AQSSOCT_ERROR_VALUE_NOT_SUPPORTED                   (IVIC_ERROR_BASE + 0x0013)
#define AQSSOCT_ERROR_TYPES_DO_NOT_MATCH                    (IVIC_ERROR_BASE + 0x0015)
#define AQSSOCT_ERROR_NOT_INITIALIZED                       (IVIC_ERROR_BASE + 0x001D)
#define AQSSOCT_ERROR_UNKNOWN_CHANNEL_NAME                  (IVIC_ERROR_BASE + 0x0020)
#define AQSSOCT_ERROR_TOO_MANY_OPEN_FILES                   (IVIC_ERROR_BASE + 0x0023)
#define AQSSOCT_ERROR_CHANNEL_NAME_REQUIRED                 (IVIC_ERROR_BASE + 0x0044)
#define AQSSOCT_ERROR_MISSING_OPTION_NAME                   (IVIC_ERROR_BASE + 0x0049)
#define AQSSOCT_ERROR_MISSING_OPTION_VALUE                  (IVIC_ERROR_BASE + 0x004A)
#define AQSSOCT_ERROR_BAD_OPTION_NAME                       (IVIC_ERROR_BASE + 0x004B)
#define AQSSOCT_ERROR_BAD_OPTION_VALUE                      (IVIC_ERROR_BASE + 0x004C)
#define AQSSOCT_ERROR_OUT_OF_MEMORY                         (IVIC_ERROR_BASE + 0x0056)
#define AQSSOCT_ERROR_OPERATION_PENDING                     (IVIC_ERROR_BASE + 0x0057)
#define AQSSOCT_ERROR_NULL_POINTER                          (IVIC_ERROR_BASE + 0x0058)
#define AQSSOCT_ERROR_UNEXPECTED_RESPONSE                   (IVIC_ERROR_BASE + 0x0059)
#define AQSSOCT_ERROR_FILE_NOT_FOUND                        (IVIC_ERROR_BASE + 0x005B)
#define AQSSOCT_ERROR_INVALID_FILE_FORMAT                   (IVIC_ERROR_BASE + 0x005C)
#define AQSSOCT_ERROR_STATUS_NOT_AVAILABLE                  (IVIC_ERROR_BASE + 0x005D)
#define AQSSOCT_ERROR_ID_QUERY_FAILED                       (IVIC_ERROR_BASE + 0x005E)
#define AQSSOCT_ERROR_RESET_FAILED                          (IVIC_ERROR_BASE + 0x005F)
#define AQSSOCT_ERROR_RESOURCE_UNKNOWN                      (IVIC_ERROR_BASE + 0x0060)
#define AQSSOCT_ERROR_ALREADY_INITIALIZED                   (IVIC_ERROR_BASE + 0x0061)
#define AQSSOCT_ERROR_CANNOT_CHANGE_SIMULATION_STATE        (IVIC_ERROR_BASE + 0x0062)
#define AQSSOCT_ERROR_INVALID_NUMBER_OF_LEVELS_IN_SELECTOR  (IVIC_ERROR_BASE + 0x0063)
#define AQSSOCT_ERROR_INVALID_RANGE_IN_SELECTOR             (IVIC_ERROR_BASE + 0x0064)
#define AQSSOCT_ERROR_UNKOWN_NAME_IN_SELECTOR               (IVIC_ERROR_BASE + 0x0065)
#define AQSSOCT_ERROR_BADLY_FORMED_SELECTOR                 (IVIC_ERROR_BASE + 0x0066)
#define AQSSOCT_ERROR_UNKNOWN_PHYSICAL_IDENTIFIER           (IVIC_ERROR_BASE + 0x0067)
#define AQSSOCT_ERROR_INVALID_SESSION_HANDLE                (IVIC_ERROR_BASE + 0x1190)



#define AQSSOCT_SUCCESS                                     0
#define AQSSOCT_WARN_NSUP_ID_QUERY                          (IVIC_WARN_BASE + 0x0065)
#define AQSSOCT_WARN_NSUP_RESET                             (IVIC_WARN_BASE + 0x0066)
#define AQSSOCT_WARN_NSUP_SELF_TEST                         (IVIC_WARN_BASE + 0x0067)
#define AQSSOCT_WARN_NSUP_ERROR_QUERY                       (IVIC_WARN_BASE + 0x0068)
#define AQSSOCT_WARN_NSUP_REV_QUERY                         (IVIC_WARN_BASE + 0x0069)



#define AQSSOCT_ERROR_IO_GENERAL                            (IVIC_SPECIFIC_ERROR_BASE + 0x0800)
#define AQSSOCT_ERROR_IO_TIMEOUT                            (IVIC_SPECIFIC_ERROR_BASE + 0x0801)
#define AQSSOCT_ERROR_ACQUISITION_RUNNING                   (IVIC_SPECIFIC_ERROR_BASE + 0x0802)
#define AQSSOCT_ERROR_STREAM_OVERFLOW                       (IVIC_SPECIFIC_ERROR_BASE + 0x0803)
#define AQSSOCT_ERROR_NOT_SUPPORTED_IN_CURRENT_STATE        (IVIC_SPECIFIC_ERROR_BASE + 0x0804)
#define AQSSOCT_ERROR_CALIBRATION_REQUIRED                  (IVIC_SPECIFIC_ERROR_BASE + 0x0805)
#define AQSSOCT_ERROR_MODULE_OPTION_REQUIRED                (IVIC_SPECIFIC_ERROR_BASE + 0x0806)
#define AQSSOCT_ERROR_MAX_TIME_EXCEEDED                     (IVIC_SPECIFIC_ERROR_BASE + 0x0807)




/**************************************************************************** 
 *---------------------------- End Include File ----------------------------* 
 ****************************************************************************/
#if defined(__cplusplus) || defined(__cplusplus__)
}
#endif
#endif // __AQSSOCT_HEADER
