#include "pch.h"
#include "MoriaConfiguration.h"

#define INI_FILE_NAME _T(".\\newmoria.ini")

CMoriaConfiguration::CMoriaConfiguration():
	isInit(false),
	nAScan(0),
	nAScanPadding(0),
	nBScan(0),
	nBufferSize(0)
{
}

CMoriaConfiguration::~CMoriaConfiguration() 
{
	releaseCircularizeMap();
}

CMoriaConfiguration& CMoriaConfiguration::GetInstance() {
	static CMoriaConfiguration pInstance;
	return pInstance;
}

void CMoriaConfiguration::Initialize()
{
	this->nAScan = ::GetPrivateProfileInt(_T("Imaging"), _T("AScan"), 1920, INI_FILE_NAME);
	this->nAScanPadding = ::GetPrivateProfileInt(_T("Imaging"), _T("AScanPadding"), 0, INI_FILE_NAME);
	this->nBScan = ::GetPrivateProfileInt(_T("Imaging"), _T("BScan"), 500, INI_FILE_NAME);

	TCHAR sIniValueString[2048] = _T("");
	int nIniValueInt = -1;
	char converted[2048];

	this->nDmaChannels = ::GetPrivateProfileInt(_T("Imaging"), _T("DmaChannels"), 2, INI_FILE_NAME);
	this->nBufferSize = (this->nDmaChannels * this->nBScan * (this->nAScan + this->nAScanPadding));
	this->nAcqBufCount = ::GetPrivateProfileInt(_T("Alazar"), _T("AcqBufferCount"), 4, INI_FILE_NAME);
	this->nTriggerDelaySample = ::GetPrivateProfileInt(_T("Alazar"), _T("TriggerDelaySample"), 0, INI_FILE_NAME);

	::GetPrivateProfileString(_T("Patient"), _T("RootPath"), _T("D:\\DataSave\\"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	this->patientFileRootPath = sIniValueString;
	_tmkdir(this->patientFileRootPath.c_str());

	this->nFftLength = ::GetPrivateProfileInt(_T("Imaging"), _T("FFTLength"), 1024, INI_FILE_NAME);
	this->nLaserSpeed = ::GetPrivateProfileInt(_T("Imaging"), _T("LaserSpeed"), 200000, INI_FILE_NAME);
	this->nCircleSize = ::GetPrivateProfileInt(_T("Imaging"), _T("CircleSize"), 1024, INI_FILE_NAME);

	::GetPrivateProfileString(_T("Measurement"), _T("AxialResolutionScale"), _T("8.3"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->measurementValues.fAxialResolutionScale = ::atof(converted);
	this->measurementValues.nNoiseSkip = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseSkip"), 300, INI_FILE_NAME);
	this->measurementValues.nNoiseAverage = ::GetPrivateProfileInt(_T("Measurement"), _T("NoiseAverage"), 100, INI_FILE_NAME);

	this->settingsOpenMP.numThread = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumThread"), 8, INI_FILE_NAME);
	this->settingsOpenMP.numDynamic = ::GetPrivateProfileInt(_T("OpenMP"), _T("NumDynamic"), 1, INI_FILE_NAME);

	this->settingsAlazar.nAcqBufferCount = ::GetPrivateProfileInt(_T("Alazar"), _T("AcqBufferCount"), 4, INI_FILE_NAME);
	this->settingsAlazar.bUserMoriaImaging = ::GetPrivateProfileInt(_T("Alazar"), _T("UseMoriaImaging"), 1, INI_FILE_NAME);
	this->settingsAlazar.msAtsTimeOut = (U32)::GetPrivateProfileInt(_T("Alazar"), _T("TimeOutInMilliSecond"), 5000, INI_FILE_NAME);

	::GetPrivateProfileString(_T("Coloring"), _T("R"), _T("255.f"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.R = ::atof(converted);
	::GetPrivateProfileString(_T("Coloring"), _T("G"), _T("255.f"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.G = ::atof(converted);
	::GetPrivateProfileString(_T("Coloring"), _T("B"), _T("255.f"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->coloring.B = ::atof(converted);

	::GetPrivateProfileString(_T("Invert"), _T("LowLevel"), _T("40.0f"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->invert.lowLevel = ::atof(converted);
	::GetPrivateProfileString(_T("Invert"), _T("HighLevel"), _T("65.0f"), sIniValueString, sizeof(sIniValueString), INI_FILE_NAME);
	wcstombs(converted, sIniValueString, wcslen(sIniValueString) + 1);
	this->invert.highLevel = ::atof(converted);

	::GetPrivateProfileString(_T("Zaber"), _T("Pullback"), _T(""), this->zaber.pullback, sizeof(this->zaber.pullback), INI_FILE_NAME);
	::GetPrivateProfileString(_T("Zaber"), _T("Interferometer"), _T(""), this->zaber.interferometer, sizeof(this->zaber.interferometer), INI_FILE_NAME);
	this->zaber.pullbackDistance = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackDistance"), 10, INI_FILE_NAME);
	this->zaber.pullbackSpeed = ::GetPrivateProfileInt(_T("Zaber"), _T("PullbackSpeed"), 10, INI_FILE_NAME);

	this->motor.velocity = ::GetPrivateProfileInt(_T("Motor"), _T("Velocity"), 800, INI_FILE_NAME);
	this->motor.settleDown = ::GetPrivateProfileInt(_T("Motor"), _T("SettleDown"), 1000, INI_FILE_NAME);

	this->shutterSerial = ::GetPrivateProfileInt(_T("Shutter"), _T("Serial"), 478, INI_FILE_NAME);

	this->catheter.position = ::GetPrivateProfileInt(_T("Catheter"), _T("Position"), 75, INI_FILE_NAME);
	this->catheter.speed = ::GetPrivateProfileInt(_T("Catheter"), _T("Speed"), 5, INI_FILE_NAME);
	this->catheter.velocity = ::GetPrivateProfileInt(_T("Catheter"), _T("MotorVelocity"), 50, INI_FILE_NAME);
	this->catheter.rotationTime = ::GetPrivateProfileInt(_T("Catheter"), _T("RotationTime"), 10000, INI_FILE_NAME);
	this->catheter.waitingTime = ::GetPrivateProfileInt(_T("Catheter"), _T("WaitingTime"), 10000, INI_FILE_NAME);

	releaseCircularizeMap();
	initCircularizeMap();

	isInit = true;
}

void CMoriaConfiguration::SaveZaberSettings() {
	tstring strValue = _T("");

	strValue = this->zaber.pullbackDistance;
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackDistance"), strValue.c_str(), INI_FILE_NAME);

	strValue = this->zaber.pullbackSpeed;
	::WritePrivateProfileString(_T("Zaber"), _T("PullbackSpeed"), strValue.c_str(), INI_FILE_NAME);
}
void CMoriaConfiguration::SaveMotorSettings() {
	tstring strValue = _T("");

	strValue = this->motor.velocity;
	::WritePrivateProfileString(_T("Motor"), _T("Velocity"), strValue.c_str(), INI_FILE_NAME);

	strValue = this->motor.settleDown;
	::WritePrivateProfileString(_T("Motor"), _T("SettleDown"), strValue.c_str(), INI_FILE_NAME);
}
void CMoriaConfiguration::initCircularizeMap(){
	int circOffset = 0;
	
	pXMap.create(1024, 1024, CV_32FC1);
	pYMap.create(1024, 1024, CV_32FC1);

	pXMap.setTo(cv::Scalar::all(0));
	pYMap.setTo(cv::Scalar::all(0));

	for (int i = 0; i < 1024; i++)
	{
		for (int j = 0; j < 1024; j++)
		{
			double fi = (double) i;
			double fj = (double) j;

			float rvalue = (float) (1024.0 - 2.0f * sqrt( (fi-511.5)*(fi-511.5) + (fj-511.5)*(fj-511.5))) + (float)circOffset;

			pXMap.at<float>(i+j*1024) = rvalue;
			pYMap.at<float>(i+j*1024) = (float) ( ((atan2((fi-511.5),(fj-511.5))/this->constantValues.Pi)+1.0)*0.5*(nBScan-1) );
		}
	}
}

void CMoriaConfiguration::releaseCircularizeMap(){
	pXMap.release();
	pYMap.release();
}

int CMoriaConfiguration::getDmaXferSamples() {
	int nSamples = nDmaChannels * nBScan * (nAScan + nAScanPadding);
	return nSamples;
}

int CMoriaConfiguration::getDmaBufferSamples()
{
	return 2 * getDmaXferSamples();
}

int CMoriaConfiguration::getScopeLength()
{
	return nAScan + nAScanPadding;
}

double CMoriaConfiguration::GetLoadCatheterTime() {
	return (catheter.rotationTime + catheter.waitingTime) / 1000;
}