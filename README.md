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
2. RaywattApp Solution 폴더로 이동
3. 아래 sql script 를 차례대로 실행

    (a) ```> psql --dbname=postgres --username=postgres --file=".\DB Script\Prerequisite.sql"```

    (b) PostgreSQL 설치 시, 입력한 기본 계정 암호 입력

    (c) ```> psql --dbname=rv_database --username=rv_user --file=".\DB Script\DDL.sql"```

    (d) rv_user 암호 (raywatt) 입력

    (e) ```> psql --dbname=rv_database --username=rv_user --file=".\DB Script\DML.sql"```

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

## [Tensorflow]

Download & Copy/Paste
*모델 파일은 상시로 바뀔 수 있으므로 LFS로 관리하지 않음
1. (https://github.com/Raywatt/RaywattOCT/issues/170) 에서 Raywatt One-Drive 링크 접속
2. ML 폴더를 다운로드 후, 하위 디렉토리에 있는 model 폴더를 RaywattOCT\RayCore\extern\libTensorflow에 복사/붙여넣기

## [Environment Variable]

C:\Raywatt\system\runtime 에서 App 실행을 위한 빌드 경로 설정
1. Windows 검색창에 "시스템 환경 변수 편집"을 검색 후 클릭
2. "고급"탭에서 하단 "환경 변수" 버튼 클릭
3. "시스템 변수"에 "새로 만들기"버튼 클릭
4. 변수 이름 : RAYWATT_PATH , 변수 값 : C:\Raywatt\system\runtime\ 입력 후 확인
5. 적용까지 완료 하면 OK

## [3D Related DLL]

Download & Copy/Paste
1. (https://github.com/Raywatt/RaywattOCT/issues/170) 에서 Raywatt One-Drive 링크 접속
2. 3D_Related_DLL 폴더를 다운로드 후 내부 dll 파일들을 C:\Raywatt\system\runtime\ 에 추가


