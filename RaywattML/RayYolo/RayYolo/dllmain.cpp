// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "pch.h"
//#include "stdio.h"
//#pragma warning(disable:4996)


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
        /*AllocConsole();
        freopen("CONOUT$", "w", stdout);*/
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

