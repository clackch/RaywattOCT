# Microsoft Developer Studio Generated NMAKE File, Based on IdeaVA.dsp
!IF "$(CFG)" == ""
CFG=IdeaVA - Win32 Debug
!MESSAGE No configuration specified. Defaulting to IdeaVA - Win32 Debug.
!ENDIF 

!IF "$(CFG)" != "IdeaVA - Win32 Release" && "$(CFG)" != "IdeaVA - Win32 Debug"
!MESSAGE Invalid configuration "$(CFG)" specified.
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE 
!MESSAGE NMAKE /f "IdeaVA.mak" CFG="IdeaVA - Win32 Debug"
!MESSAGE 
!MESSAGE Possible choices for configuration are:
!MESSAGE 
!MESSAGE "IdeaVA - Win32 Release" (based on "Win32 (x86) Dynamic-Link Library")
!MESSAGE "IdeaVA - Win32 Debug" (based on "Win32 (x86) Dynamic-Link Library")
!MESSAGE 
!ERROR An invalid configuration is specified.
!ENDIF 

!IF "$(OS)" == "Windows_NT"
NULL=
!ELSE 
NULL=nul
!ENDIF 

!IF  "$(CFG)" == "IdeaVA - Win32 Release"

OUTDIR=.\Release
INTDIR=.\Release
# Begin Custom Macros
OutDir=.\Release
# End Custom Macros

ALL : "$(OUTDIR)\IdeaVA.dll"


CLEAN :
	-@erase "$(INTDIR)\DlgIColorFilter.obj"
	-@erase "$(INTDIR)\IdeaVA.obj"
	-@erase "$(INTDIR)\IdeaVA.pch"
	-@erase "$(INTDIR)\IdeaVA.res"
	-@erase "$(INTDIR)\StdAfx.obj"
	-@erase "$(INTDIR)\vc60.idb"
	-@erase "$(INTDIR)\VideoAdjust.obj"
	-@erase "$(INTDIR)\WBalThread.obj"
	-@erase "$(INTDIR)\WhiteBalance.obj"
	-@erase "$(OUTDIR)\IdeaVA.dll"
	-@erase "$(OUTDIR)\IdeaVA.exp"
	-@erase "$(OUTDIR)\IdeaVA.lib"

"$(OUTDIR)" :
    if not exist "$(OUTDIR)/$(NULL)" mkdir "$(OUTDIR)"

CPP=cl.exe
CPP_PROJ=/nologo /MD /W3 /GX /O2 /I "..\include" /I "..\lib\src" /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_WINDLL" /D "_AFXDLL" /D "_MBCS" /D "_USRDLL" /Fp"$(INTDIR)\IdeaVA.pch" /Yu"stdafx.h" /Fo"$(INTDIR)\\" /Fd"$(INTDIR)\\" /FD /c 

.c{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cpp{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cxx{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.c{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cpp{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cxx{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

MTL=midl.exe
MTL_PROJ=/nologo /D "NDEBUG" /mktyplib203 /win32 
RSC=rc.exe
RSC_PROJ=/l 0x409 /fo"$(INTDIR)\IdeaVA.res" /d "NDEBUG" /d "_AFXDLL" 
BSC32=bscmake.exe
BSC32_FLAGS=/nologo /o"$(OUTDIR)\IdeaVA.bsc" 
BSC32_SBRS= \
	
LINK32=link.exe
LINK32_FLAGS=version.lib /nologo /subsystem:windows /dll /incremental:no /pdb:"$(OUTDIR)\IdeaVA.pdb" /machine:I386 /def:".\IdeaVA.def" /out:"$(OUTDIR)\IdeaVA.dll" /implib:"$(OUTDIR)\IdeaVA.lib" 
DEF_FILE= \
	".\IdeaVA.def"
LINK32_OBJS= \
	"$(INTDIR)\DlgIColorFilter.obj" \
	"$(INTDIR)\IdeaVA.obj" \
	"$(INTDIR)\StdAfx.obj" \
	"$(INTDIR)\VideoAdjust.obj" \
	"$(INTDIR)\WBalThread.obj" \
	"$(INTDIR)\WhiteBalance.obj" \
	"$(INTDIR)\IdeaVA.res" \
	"..\lib\W32\hdpw32.lib"

"$(OUTDIR)\IdeaVA.dll" : "$(OUTDIR)" $(DEF_FILE) $(LINK32_OBJS)
    $(LINK32) @<<
  $(LINK32_FLAGS) $(LINK32_OBJS)
<<

!ELSEIF  "$(CFG)" == "IdeaVA - Win32 Debug"

OUTDIR=.\Debug
INTDIR=.\Debug
# Begin Custom Macros
OutDir=.\Debug
# End Custom Macros

ALL : "$(OUTDIR)\IdeaVA.dll"


CLEAN :
	-@erase "$(INTDIR)\DlgIColorFilter.obj"
	-@erase "$(INTDIR)\IdeaVA.obj"
	-@erase "$(INTDIR)\IdeaVA.pch"
	-@erase "$(INTDIR)\IdeaVA.res"
	-@erase "$(INTDIR)\StdAfx.obj"
	-@erase "$(INTDIR)\vc60.idb"
	-@erase "$(INTDIR)\vc60.pdb"
	-@erase "$(INTDIR)\VideoAdjust.obj"
	-@erase "$(INTDIR)\WBalThread.obj"
	-@erase "$(INTDIR)\WhiteBalance.obj"
	-@erase "$(OUTDIR)\IdeaVA.dll"
	-@erase "$(OUTDIR)\IdeaVA.exp"
	-@erase "$(OUTDIR)\IdeaVA.ilk"
	-@erase "$(OUTDIR)\IdeaVA.lib"
	-@erase "$(OUTDIR)\IdeaVA.pdb"

"$(OUTDIR)" :
    if not exist "$(OUTDIR)/$(NULL)" mkdir "$(OUTDIR)"

CPP=cl.exe
CPP_PROJ=/nologo /MDd /W3 /Gm /GX /ZI /Od /I "..\include" /I "..\lib\src" /D "WIN32" /D "_DEBUG" /D "_WINDOWS" /D "_WINDLL" /D "_AFXDLL" /D "_MBCS" /D "_USRDLL" /Fp"$(INTDIR)\IdeaVA.pch" /Yu"stdafx.h" /Fo"$(INTDIR)\\" /Fd"$(INTDIR)\\" /FD /GZ /c 

.c{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cpp{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cxx{$(INTDIR)}.obj::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.c{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cpp{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

.cxx{$(INTDIR)}.sbr::
   $(CPP) @<<
   $(CPP_PROJ) $< 
<<

MTL=midl.exe
MTL_PROJ=/nologo /D "_DEBUG" /mktyplib203 /win32 
RSC=rc.exe
RSC_PROJ=/l 0x409 /fo"$(INTDIR)\IdeaVA.res" /d "_DEBUG" /d "_AFXDLL" 
BSC32=bscmake.exe
BSC32_FLAGS=/nologo /o"$(OUTDIR)\IdeaVA.bsc" 
BSC32_SBRS= \
	
LINK32=link.exe
LINK32_FLAGS=version.lib /nologo /subsystem:windows /dll /incremental:yes /pdb:"$(OUTDIR)\IdeaVA.pdb" /debug /machine:I386 /def:".\IdeaVA.def" /out:"$(OUTDIR)\IdeaVA.dll" /implib:"$(OUTDIR)\IdeaVA.lib" /pdbtype:sept 
DEF_FILE= \
	".\IdeaVA.def"
LINK32_OBJS= \
	"$(INTDIR)\DlgIColorFilter.obj" \
	"$(INTDIR)\IdeaVA.obj" \
	"$(INTDIR)\StdAfx.obj" \
	"$(INTDIR)\VideoAdjust.obj" \
	"$(INTDIR)\WBalThread.obj" \
	"$(INTDIR)\WhiteBalance.obj" \
	"$(INTDIR)\IdeaVA.res" \
	"..\lib\W32\hdpw32.lib"

"$(OUTDIR)\IdeaVA.dll" : "$(OUTDIR)" $(DEF_FILE) $(LINK32_OBJS)
    $(LINK32) @<<
  $(LINK32_FLAGS) $(LINK32_OBJS)
<<

!ENDIF 


!IF "$(NO_EXTERNAL_DEPS)" != "1"
!IF EXISTS("IdeaVA.dep")
!INCLUDE "IdeaVA.dep"
!ELSE 
!MESSAGE Warning: cannot find "IdeaVA.dep"
!ENDIF 
!ENDIF 


!IF "$(CFG)" == "IdeaVA - Win32 Release" || "$(CFG)" == "IdeaVA - Win32 Debug"
SOURCE=.\DlgIColorFilter.cpp

"$(INTDIR)\DlgIColorFilter.obj" : $(SOURCE) "$(INTDIR)" "$(INTDIR)\IdeaVA.pch"


SOURCE=.\IdeaVA.cpp

"$(INTDIR)\IdeaVA.obj" : $(SOURCE) "$(INTDIR)" "$(INTDIR)\IdeaVA.pch"


SOURCE=.\IdeaVA.rc

"$(INTDIR)\IdeaVA.res" : $(SOURCE) "$(INTDIR)"
	$(RSC) $(RSC_PROJ) $(SOURCE)


SOURCE=.\StdAfx.cpp

!IF  "$(CFG)" == "IdeaVA - Win32 Release"

CPP_SWITCHES=/nologo /MD /W3 /GX /O2 /I "..\include" /I "..\lib\src" /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_WINDLL" /D "_AFXDLL" /D "_MBCS" /D "_USRDLL" /Fp"$(INTDIR)\IdeaVA.pch" /Yc"stdafx.h" /Fo"$(INTDIR)\\" /Fd"$(INTDIR)\\" /FD /c 

"$(INTDIR)\StdAfx.obj"	"$(INTDIR)\IdeaVA.pch" : $(SOURCE) "$(INTDIR)"
	$(CPP) @<<
  $(CPP_SWITCHES) $(SOURCE)
<<


!ELSEIF  "$(CFG)" == "IdeaVA - Win32 Debug"

CPP_SWITCHES=/nologo /MDd /W3 /Gm /GX /ZI /Od /I "..\include" /I "..\lib\src" /D "WIN32" /D "_DEBUG" /D "_WINDOWS" /D "_WINDLL" /D "_AFXDLL" /D "_MBCS" /D "_USRDLL" /Fp"$(INTDIR)\IdeaVA.pch" /Yc"stdafx.h" /Fo"$(INTDIR)\\" /Fd"$(INTDIR)\\" /FD /GZ /c 

"$(INTDIR)\StdAfx.obj"	"$(INTDIR)\IdeaVA.pch" : $(SOURCE) "$(INTDIR)"
	$(CPP) @<<
  $(CPP_SWITCHES) $(SOURCE)
<<


!ENDIF 

SOURCE=.\VideoAdjust.cpp

"$(INTDIR)\VideoAdjust.obj" : $(SOURCE) "$(INTDIR)" "$(INTDIR)\IdeaVA.pch"


SOURCE=.\WBalThread.cpp

"$(INTDIR)\WBalThread.obj" : $(SOURCE) "$(INTDIR)" "$(INTDIR)\IdeaVA.pch"


SOURCE=.\WhiteBalance.cpp

"$(INTDIR)\WhiteBalance.obj" : $(SOURCE) "$(INTDIR)" "$(INTDIR)\IdeaVA.pch"



!ENDIF 

