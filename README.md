# RaywattOCT
Intravascular OCT System Development Project

***

# Setting
## [CommunityToolkit]

.Net SDK Issue
* [참고](https://github.com/dotnet/wpf/issues/6792)
* Download & Install [.Net 6.0.304](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.304-windows-x64-installer)

## [Database]

Install PostgreSQL
* Windows x86, x64 v14.5 [installer](https://www.postgresql.org/download/windows/)
* Port : 5432
* 환경 변수 PATH 에 PostgreSQL (C:\Program Files\PostgreSQL\14\bin) 추가
* PW : 1111

테이블 설계서(2022.10.11 Updated)
[테이블 설계서.xlsx](https://github.com/raywatt-jeansu/RaywattOCT/files/9762372/default.xlsx)

Database Initial Setting
1. Tablespace를 위한 폴더 생성 (C:\Tablespace)
2. SQL 파일이 있는 폴더로 이동
3. 아래 sql script 를 차례대로 실행

    (a) ```> psql --dbname=postgres --username=postgres --file=".\Prerequisite.sql"```

    (b) PostgreSQL 설치 시, 입력한 기본 계정 암호 입력

    (c) ```> psql --dbname=rv_database --username=rv_user --file=".\DDL.sql"```

    (d) rv_user 암호 (raywatt) 입력

    (e) ```> psql --dbname=rv_database --username=rv_user --file=".\DML.sql"```

    (f) rv_user 암호 (raywatt) 입력

* DB Table 변경 시에는 (c) ~ (f) 만 실행

* DB Export/Import 방법

![image](https://github.com/Raywatt/RaywattOCT/assets/110812182/01b59980-fc50-487e-bf83-46a3f7d2fc51)

(Tool에서 Import/Export 기능 불가 시 [링크](https://velog.io/@myway00/Postgre-error-Utility-file-not-found.-Please-correct-the-Binary-Path-in-the-Preferences-dialog-오류-해결-1분만-투자하면-해결-ㅆㄱㄴ) 참고)

* DB Schema 변경 시 Import/Export
  - Import/Export -> Columns -> Import/Export 할 Columns 선택 (헤더 불필요)

## [LibTorch]
(※ 신규 장비 Set-up 시에 필요없음. RaywattApp에 dll 파일 존재)

Download & copy dll files
* dll 파일 용량 문제로 git 으로 관리가 안됨
* [LibTorch](https://pytorch.org/get-started/locally/) 1.13.0 cuda 11.7 [release](https://download.pytorch.org/libtorch/cu117/libtorch-win-shared-with-deps-1.13.0%2Bcu117.zip) / [debug](https://download.pytorch.org/libtorch/cu117/libtorch-win-shared-with-deps-debug-1.13.0%2Bcu117.zip) ver. 각각 다운로드
* 압축 풀고, lib 폴더에서 asmjit / c10 / c10_cuda / caffe2_nvrtc / fbgemm / libiomp5md / nvToolsExt64_1 / torch / torch_cpu / torch_cuda / torch_cuda_cpp / torch_cuda_cu / uv / zlibwapi dll 파일들을 각각 extern/libtorch/lib 하위 release / debug 폴더에 복사

## [CUDA & CuDNN]

CUDA
* CUDA Toolkit 11.7.0 Download(https://pytorch.org/get-started/locally/) & 설치

CuDNN
* [cuDNN for CUDA 11.x](https://developer.nvidia.com/rdp/cudnn-download) download
* 압축 풀고, bin 폴더의 dll 파일들을 CUDA 설치 경로 (C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.7\bin) 에 복사

## [Axsun]

Download & Install OCT Host
1. [OCT Host (.exe)](https://docs.axsun.com/axsun-technologies-knowledge-base/other/downloads) 다운로드 후 설치
2. DLL Register

    (a) Command Prompt 관리자 권한으로 실행

    (b) ```C:\Program Files\Axsun\Axsun OCT Control``` 로 이동
    
    (c) ```c:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe AxsunOCTControl.dll /tlb:AxsunOCTControl.tlb /codebase``` 입력
    
    (d) ```C:\Program Files\Axsun\Axsun OCT Control\AxsunOCTControl.tlb``` 파일 생성 확인

## [DAQ]
* ATS9371_Driver_V7.8.6.exe 파일 설치

## [Font]

Download & Install Font
1. [Pretendard](https://github.com/orioncactus/pretendard) 최신 버전 다운로드 후 압축 해제
2. /Pretendard/public/static/alternative/Pretendard-Bold / Pretendard-Medium / Pretendard-Regular / Pretendard-SemiBold.ttf 실행, 설치

설치확인 방법
1. 설정 > 개인 설정 > 글꼴
2. Pretendard 검색
3. 확인

## [Cursor]
Download & Create Folder, Copy/Paste
1. [cursor.zip](https://github.com/Raywatt/RaywattOCT/files/11419744/cursor.zip) 다운로드 후 압축 해제
2. C:\Raywatt\system\image\cursor 폴더 생성 및 복사/붙여넣기

Windows Default Mouse Cursor Setting (※ 필요 시 진행)
1. 설정-> Bluethooth 및 장치 -> 마우스
2. 관련설정 -> 더 많은 마우스 설정
3. 포인터
    - 찾아보기에서 cur 파일

## [Executable file]
1. Visual Studio Debug/Release Solution Clean > Release Build
2. RaywattApp에서 bin 폴더를 외장 저장장치에 복사 및 폴더명 변경(bin -> runtime)
3. runtime 폴더에서 Release/Debug 폴더 삭제
4. 외장 장치에서 장비 PC의 C:\Raywatt\system 경로에 runtime 폴더 복사 (※ C:\Raywatt\system\runtime)
  
## [Windows]
1. 사용자 계정 추가
   
   (a) 제어판 > 사용자 계정 > 사용자 계정 > 다른 계정 관리 > PC 설정에서 새 사용자 추가 > 계정 추가 > 이 사람의 로그인 정보를 가지고 있지 않습니다. > Microsoft 계정 없이 사용자 추가
   
   (b) 사용자 이름 : FASTER / 암호 : faster / 보안 질문 1,2,3 답변 : raywatt

2. 사용자 계정 설정
   
   (a) 설정 > 개인 설정 > 색 - 폭풍 선택 (※ Raywatt, FASTER 모두 적용)

   (b) 설정 > 개인 설정 > 잠금 화면 - 로그인 화면에 잠금 화면 배경 그림 표시 (켬 -> 끔) (※ Raywatt, FASTER 모두 적용)

   (c) 마우스 포인터 설정 (※ FASTER에 적용)

3. 윈도우 잠금화면 해제
   
   (a) 윈도우 + R > gpedit.msc

   (b) 컴퓨터 구성 > 관리 템플릿 > 제어판 > 개인 설정 > 잠금 화면 표시 안 함 > 사용

4. 로그인 창의 네트워크 아이콘 숨기기
   
   (a) 윈도우 + R > gpedit.msc
   
   (b) 컴퓨터 구성 > 관리 템플릿 > 시스템 -> 로그온 > 네트워크 선택 UI 표시 안 함 > 사용   

5. Shell Launcher (고정 프로그램)
   
   (a) 제어판 > 프로그램 > 프로그램 및 기능 > Windows 기능 켜기/끄기 > Device Lockdown(디바이스 잠금) > Shell Launcher(셸 시작 관리자) 체크
   
   (b) 관리자 권한 PowerShell 실행 > `Set-ExecutionPolicy Unrestricted` 입력 후 `Y`

   (c) [ShellLauncher.zip](https://github.com/Raywatt/RaywattOCT/files/12583810/ShellLauncher.zip) 다운로드 및 `ShellLauncher_enable.ps1` 실행
   
       ※ ShellLauncher_enable.ps1에서 C:\Raywatt\system\runtime\RaywattApp.exe를 실행하게 설정
   
       ※ 설정이 잘 못되었을 경우, ShellLauncher_disable.ps1 실행해서 설정 내용 해제 가능
   
   (d) 스크립트 실행 (경고 발생)

6. 부팅 로고 변경

    (a) [HackBGRT-1.5.1_Raywatt.zip](https://github.com/Raywatt/RaywattOCT/files/12582646/HackBGRT-1.5.1_Raywatt.zip) 다운로드 후 setup.exe 실행

    (b) 'I', 'I' 입력 후 메모장 닫기 > 저장

    (c) Bios Logo Disabled

       (1) 안전 모드 진입 (shift + 다시 시작)
       (2) 문제 해결 > 고급옵션 > UEFI 펌웨어 설정 > 다시 시작
       (3) 설정 화면 (※ BARCO 모니터의 경우, 설정 화면이 안 보이는 경우가 있어 다른 모니터 이용하는 편이 좋음)
       (4) SETTING > Boot > Full Screen Logo Display (Enabled -> Disabled 변경) > Exit
