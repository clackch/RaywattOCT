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

## [LibTorch]

Download & copy dll files
* dll 파일 용량 문제로 git 으로 관리가 안됨
* [LibTorch](https://pytorch.org/get-started/locally/) 1.13.0 cuda 11.7 [release](https://download.pytorch.org/libtorch/cu117/libtorch-win-shared-with-deps-1.13.0%2Bcu117.zip) / [debug](https://download.pytorch.org/libtorch/cu117/libtorch-win-shared-with-deps-debug-1.13.0%2Bcu117.zip) ver. 각각 다운로드
* 압축 풀고, lib 폴더에서 asmjit / c10 / c10_cuda / caffe2_nvrtc / fbgemm / libiomp5md / nvToolsExt64_1 / torch / torch_cpu / torch_cuda / torch_cuda_cpp / torch_cuda_cu / uv / zlibwapi dll 파일들을 각각 extern/libtorch/lib 하위 release / debug 폴더에 복사

## [Axsun]

Download & Install OCT Host
1. [OCT Host (.exe)](https://docs.axsun.com/axsun-technologies-knowledge-base/other/downloads) 다운로드 후 설치
2. DLL Register

    (a) Command Prompt 관리자 권한으로 실행

    (b) ```C:\Program Files\Axsun\Axsun OCT Control``` 로 이동
    
    (c) ```c:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe AxsunOCTControl.dll /tlb:AxsunOCTControl.tlb /codebase``` 입력
    
    (d) ```C:\Program Files\Axsun\Axsun OCT Control\AxsunOCTControl.tlb``` 파일 생성 확인
