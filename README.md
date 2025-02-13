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

테이블 설계서(2023.09.14 Updated)
[테이블 설계서_20230914.xlsx](https://github.com/Raywatt/RaywattOCT/files/12605239/_20230914.xlsx)

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

## [CUDA]

* CUDA Toolkit 11.8.0 Download(https://developer.nvidia.com/cuda-11-8-0-download-archive?target_os=Windows&target_arch=x86_64&target_version=11&target_type=exe_local) & 설치
<!--* CUDA Toolkit 11.8.0 Download(https://pytorch.org/get-started/locally/) & 설치 -->

## [Axsun]

Download & Install OCT Host
1. [OCT Host (.exe)](https://docs.axsun.com/axsun-technologies-knowledge-base/other/downloads) 다운로드 후 설치 (ver 1.17.15)
2. DLL Register

    (a) Command Prompt 관리자 권한으로 실행

    (b) ```C:\Program Files\Axsun\Axsun OCT Control``` 로 이동
    
    (c) ```c:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe AxsunOCTControl.dll /tlb:AxsunOCTControl.tlb /codebase``` 입력
    
    (d) ```C:\Program Files\Axsun\Axsun OCT Control\AxsunOCTControl.tlb``` 파일 생성 확인

## [DAQ]
* ATS9371_Driver_V7.11.1.exe(https://www.alazartech.com/en/product/ats9371/4/) 파일 설치

## [Python]

1. Python Download & Install
   - https://www.python.org/downloads/

    ※ 설치 시, 환경변수 추가 (Add Python 3.XX to PATH 선택)
2. Python Package Install
   - cmd에서 pip install numpy pillow scipy imageio pywin32 실행
     
     ※ 인터넷 연결이 안되어 있을 경우 처리방안
     
     [인터넷 없는 환경 package 설치.docx](https://github.com/user-attachments/files/18434123/package.docx)

3. 환경 변수 추가
   - 시스템 변수에 PYTHON_DLL - C:\Users\Raywatt\AppData\Local\Programs\Python\Python312\python312.dll 추가

     ※ 경로 및 Python dll은 해당 PC에 맞춰서 적용
4. 코드 복사
   - RaywattExt\Python\ImageProcess.py 파일을 runtime에 복사

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
1. [cursor.zip](https://github.com/user-attachments/files/18683414/cursor.zip) 다운로드 후 압축 해제
2. C:\Raywatt\system\image\cursor 폴더 생성 및 복사/붙여넣기

Windows Default Mouse Cursor Setting (※ 필요 시 진행)
1. 설정-> Bluethooth 및 장치 -> 마우스
2. 관련설정 -> 더 많은 마우스 설정
3. 포인터
    - 찾아보기에서 cur 파일

## [Environment Variable]
1. Windows 검색창에 "시스템 환경 변수 편집"을 검색 후 클릭
2. "고급"탭에서 하단 "환경 변수" 버튼 클릭
3. "시스템 변수"항목 중 변수 이름 : Path(or PATH) 더블클릭
4. "환경 변수 편집" 창이 뜨면 "새로 만들기"를 클릭한 후 C:\Raywatt\system\3rdparty를 추가
5. 적용까지 완료 하면 OK

## [3rdparty]
Download & Copy/Paste
1. (https://github.com/Raywatt/RaywattOCT/issues/170) 에서 Raywatt One-Drive 링크 접속
2. 3rdparty 폴더를 다운로드 한 후, 폴더 자체를 C:\Raywatt\system에 추가

## [Executable file]
1. Visual Studio Debug/Release Solution Clean > Release Build
2. RaywattApp에서 bin 폴더를 외장 저장장치에 복사 및 폴더명 변경(bin -> runtime)
3. runtime 폴더에서 Release/Debug 폴더 삭제
4. 외장 장치에서 장비 PC의 C:\Raywatt\system 경로에 runtime 폴더 복사 (※ C:\Raywatt\system\runtime)

## [FrameGrabber]
1. (https://github.com/Raywatt/RaywattOCT/issues/170) 에서 Raywatt One-Drive 링크 접속
2. FrameGrabber 폴더를 다운로드 한 후, 폴더를 C:\Raywatt에 추가
3. https://github.com/Raywatt/RaywattOCT/issues/209#issue-2097526663 에서 라이브러리 및 매뉴얼 다운로드
4. 매뉴얼 파일의 목차 1, 2번 진행

## [Windows]
1. 사용자 계정 추가
   
   (a) 제어판 > 사용자 계정 > 사용자 계정 > 다른 계정 관리 > PC 설정에서 새 사용자 추가 > 계정 추가 > 이 사람의 로그인 정보를 가지고 있지 않습니다. > Microsoft 계정 없이 사용자 추가
   
   (b) 사용자 이름 : FASTER / 암호 : faster / 보안 질문 1,2,3 답변 : raywatt

2. 사용자 계정 설정
   
   (a) 설정 > 개인 설정 > 색 - 폭풍 선택 (※ Raywatt, FASTER 모두 적용)

   (b) 설정 > 개인 설정 > 잠금 화면 - 로그인 화면에 잠금 화면 배경 그림 표시 (켬 -> 끔) (※ Raywatt, FASTER 모두 적용)

   (c) Font 설정 (※ Raywatt, FASTER 모두 적용)

   (d) [raywattLogoAccount.zip](https://github.com/Raywatt/RaywattOCT/files/12602861/raywattLogoAccount.zip) 다운로드 및 설정 > 계정 사진 변경 > 찾아보기 - 다운받은 이미지 선택 (※ Raywatt, FASTER 모두 적용)

   (e) 마우스 포인터 설정 (※ FASTER에 적용)

3. PC 설정

   (a) 전원 관리 옵션 설정 편집 - 디스플레이 끄기/절전 모드 설정 > 해당 없음으로 설정

   (b) 디스플레이 설정 - 디스플레이 복제로 설정

4. 윈도우 잠금화면 해제
   
   (a) 윈도우 + R > gpedit.msc

   (b) 컴퓨터 구성 > 관리 템플릿 > 제어판 > 개인 설정 > 잠금 화면 표시 안 함 > 사용

5. 로그인 창의 네트워크 아이콘 숨기기
   
   (a) 윈도우 + R > gpedit.msc
   
   (b) 컴퓨터 구성 > 관리 템플릿 > 시스템 -> 로그온 > 네트워크 선택 UI 표시 안 함 > 사용   

6. Raywatt App 관리자 권한으로 작업 스케줄러 등록

   (a) PowerShell 관리자 권한으로 실행

   (b) 아래 내용 실행
   
       $taskAction = New-ScheduledTaskAction -Execute "C:\Raywatt\system\runtime\RaywattApp.exe"
   
       $taskPrincipal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
   
       $taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd
   
       Register-ScheduledTask -TaskName "RaywattAppAdmin" -Action $taskAction -Principal $taskPrincipal -Settings $taskSettings
   
   (c) 적용 확인 방법
   
       Win + R → taskschd.msc 입력 후 Enter
   
       "작업 스케줄러 라이브러리" 클릭
   
       오른쪽 창에서 "RaywattAppAdmin" 작업이 있는지 확인

8. Shell Launcher (고정 프로그램)
   
   (a) 제어판 > 프로그램 > 프로그램 및 기능 > Windows 기능 켜기/끄기 > Device Lockdown(디바이스 잠금) > Shell Launcher(셸 시작 관리자) 체크
   
   (b) 관리자 권한 PowerShell 실행 > `Set-ExecutionPolicy Unrestricted` 입력 후 `Y`

   (c) [ShellLauncher.zip](https://github.com/user-attachments/files/18777482/ShellLauncher.zip) 다운로드 및 `ShellLauncher_enable.ps1` 실행
   
       ※ ShellLauncher_enable.ps1에서 스케줄러에 등록된 RaywattApp을 실행하게 설정
   
       ※ 설정이 잘 못되었을 경우, ShellLauncher_disable.ps1 실행해서 설정 내용 해제 가능
   
   (d) 스크립트 실행 (경고 발생)

9. 부팅 로고 변경

    (a) [HackBGRT-1.5.1_Raywatt.zip](https://github.com/Raywatt/RaywattOCT/files/12582646/HackBGRT-1.5.1_Raywatt.zip) 다운로드 후 setup.exe 실행

    (b) 'I', 'I' 입력 후 메모장 닫기 > 저장

    (c) Bios Logo Disabled

       (1) 안전 모드 진입 (shift + 다시 시작)
       (2) 문제 해결 > 고급옵션 > UEFI 펌웨어 설정 > 다시 시작
       (3) 설정 화면 (※ BARCO 모니터의 경우, 설정 화면이 안 보이는 경우가 있어 다른 모니터 이용하는 편이 좋음)
       (4) SETTING > Boot > Full Screen Logo Display (Enabled -> Disabled 변경) > Exit

## Local 환경 설정 참고

- App 종료 시, Power Off/Switch User가 호출되지 않도록 설정 방법(https://github.com/Raywatt/RaywattOCT/pull/173#issue-1892013269)

  ※ 아래와 같이 buffer 컬럼에 본인 windows의 계정 추가 필요
  
  ![image](https://github.com/Raywatt/RaywattOCT/assets/110812182/03acbcd1-8f77-40fa-a29d-cfaac5860787)


## 설정 후, 꼭 확인해야 하는 항목 (App Test)

- Manual Calibration 실행해서 ML 기능 확인
  - 첫 실행이면 C:\Raywatt\system\3rdparty\model\yolo에서 engine 파일이 실행일자로 생성되었는지 확인
- Tiff 저장 기능 확인
