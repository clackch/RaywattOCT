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
