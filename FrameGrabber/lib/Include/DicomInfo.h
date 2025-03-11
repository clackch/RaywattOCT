#if !defined(DICOMINFO_H_INCLUDED_)
#define DICOMINFO_H_INCLUDED_

typedef struct DicomInfoTag {
  char    *pszClientAeTitle;
  int      nClientPortNumber;
  char    *pszServerAeTitle;
  char    *pszServerIpAddress;
  int      nServerPortNumber;
  char    *pszDICOMFile;
} DICOM_INFO;

typedef BOOL  (CALLBACK *SHOWDICOMDIALOGPROC)( DICOM_INFO *DicomInfo );
typedef VOID* (CALLBACK *VOIDPPROC)( void );
typedef BOOL  (CALLBACK *OPENSTREAMPROC)( long hDicomDS, unsigned long ulCompressionType, long nClass, 
                                          long nBitCount, BOOL bLossy, long nWidth, long nHeight );
typedef BOOL  (CALLBACK *ADDFRAMEPROC)( void *pCompressedData, DWORD dwSizeImage );
typedef VOID  (CALLBACK *SAVEFILEPROC)( LPCTSTR lpszFileName, short nFlags );

#endif // !defined(DICOMINFO_H_INCLUDED_)
