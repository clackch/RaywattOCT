#ifndef INCLUDED_IDEADSHOW_H
#define INCLUDED_IDEADSHOW_H

/* ----------------------------------------------------------------- */

#ifndef __IIdeaWDM_INTERFACE_DEFINED__
#define __IIdeaWDM_INTERFACE_DEFINED__

#ifdef __cplusplus
extern "C"{
#endif 


EXTERN_C const IID IID_IIdeaWDM;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B34C2BA3-904B-4080-B4DC-D77EE8920B81")
    IIdeaWDM : public IUnknown
    {
    public:
				virtual HRESULT STDMETHODCALLTYPE Get(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nDataSize,
					/* [retval][out] */ void* pData) = 0;

				virtual __int32 STDMETHODCALLTYPE GetValue(
					/* [in] */ __int32 nControl) = 0;

				virtual __int32 STDMETHODCALLTYPE GetSymbolID(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nIndex) = 0;

				virtual __int32 STDMETHODCALLTYPE GetSymbolIndex(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nSymbolID) = 0;

				virtual HRESULT STDMETHODCALLTYPE GetValueText(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nBufferSize,
					/* [retval][out] */ unsigned char* pszBuffer) = 0;

				virtual HRESULT STDMETHODCALLTYPE GetAssignmentText(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nBufferSize,
					/* [retval][out] */ unsigned char* pszBuffer) = 0;

				virtual HRESULT STDMETHODCALLTYPE Set(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nDataSize,
					/* [in] */ void* pData) = 0;

				virtual HRESULT STDMETHODCALLTYPE SetValue(
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nValue) = 0;

				virtual HRESULT STDMETHODCALLTYPE SetValueWithText(
					/* [in] */ __int32 nControl,
					/* [in] */ unsigned char* pszText) = 0;

				virtual HRESULT STDMETHODCALLTYPE SetWithText(
					/* [in] */ unsigned char* pszText) = 0;

				virtual HRESULT STDMETHODCALLTYPE Execute(
					/* [in] */ __int32 nCommand,
					/* [in] */ __int32 nDataSize,
					/* [retval][out] */ void* pData) = 0;

    };
    
#else 	/* C style interface */

    typedef struct IIdeaWDMVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IIdeaWDM * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IIdeaWDM * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IIdeaWDM * This);
        
				HRESULT(STDMETHODCALLTYPE* Get)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nDataSize,
					/* [retval][out] */ void* pData);

				__int32 (STDMETHODCALLTYPE* GetValue)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl);

				__int32 (STDMETHODCALLTYPE* GetSymbolID)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nIndex);

				__int32 (STDMETHODCALLTYPE* GetSymbolIndex)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nSymbolID);

				HRESULT(STDMETHODCALLTYPE* GetValueText)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nBufferSize,
					/* [retval][out] */ unsigned char* pszBuffer);

				HRESULT(STDMETHODCALLTYPE* GetAssignmentText)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nBufferSize,
					/* [retval][out] */ unsigned char* pszBuffer);

				HRESULT(STDMETHODCALLTYPE* Set)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nDataSize,
					/* [in] */ void* pData);

				HRESULT(STDMETHODCALLTYPE* SetValue)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ __int32 nValue);

				HRESULT(STDMETHODCALLTYPE* SetValueWithText)(
					IIdeaWDM* This,
					/* [in] */ __int32 nControl,
					/* [in] */ unsigned char* pszText);

				HRESULT(STDMETHODCALLTYPE* SetWithText)(
					IIdeaWDM* This,
					/* [in] */ unsigned char* pszText);

				HRESULT(STDMETHODCALLTYPE* Execute)(
					IIdeaWDM* This,
					/* [in] */ __int32 nCommand,
					/* [in] */ __int32 nDataSize,
					/* [retval][out] */ void* pData);

        END_INTERFACE
    } IIdeaWDMVtbl;

    interface IIdeaWDM
    {
        CONST_VTBL struct IIdeaWDMVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IIdeaWDM_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IIdeaWDM_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IIdeaWDM_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IIdeaWDM_Get(This,nControl,nDataSize,pData)	\
    ( (This)->lpVtbl -> Get(This,nControl,nDataSize,pData) ) 

#define IIdeaWDM_GetValue(This,nControl)	\
    ( (This)->lpVtbl -> GetValue(This,nControl) ) 

#define IIdeaWDM_GetSymbolID(This,nControl,nIndex)	\
    ( (This)->lpVtbl -> GetSymbolID(This,nControl,nIndex) ) 

#define IIdeaWDM_GetValueText(This,nControl,nBufferSize,pszBuffer)	\
    ( (This)->lpVtbl -> GetValueText(This,nControl,nBufferSize,pszBuffer) ) 

#define IIdeaWDM_GetAssignmentText(This,nControl,nBufferSize,pszBuffer)	\
    ( (This)->lpVtbl -> GetAssignmentText(This,nControl,nBufferSize,pszBuffer) ) 

#define IIdeaWDM_Set(This,nControl,nDataSize,pData)	\
    ( (This)->lpVtbl -> Set(This,nControl,nDataSize,pData) ) 

#define IIdeaWDM_SetValue(This,nControl,nValue)	\
    ( (This)->lpVtbl -> SetValue(This,nControl,nValue) ) 

#define IIdeaWDM_SetValueWithText(This,nControl,pszText)	\
    ( (This)->lpVtbl -> SetValueWithText(This,nControl,pszText) ) 

#define IIdeaWDM_SetWithText(This,pszText)	\
    ( (This)->lpVtbl -> SetWithText(This,pszText) ) 

#define IIdeaWDM_Execute(This,nCommand,nDataSize,pData)	\
    ( (This)->lpVtbl -> Execute(This,nCommand,nDataSize,pData) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */

#ifdef __cplusplus
}
#endif

#endif 	/* __IIdeaWDM_INTERFACE_DEFINED__ */

#endif // INCLUDED_IDEADSHOW_H
