-- Role: rv_user
-- DROP ROLE IF EXISTS rv_user;

CREATE ROLE rv_user WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  ENCRYPTED PASSWORD 'SCRAM-SHA-256$4096:OYFWQj0UrQVE8bVIbRjxvg==$5xnFI1lMWAfM8/EvicvvldRM/jp77UeqrJ4wlu2qX3w=:8uk7uxtbInA1Dkra90RP/Ew61hzwwqVWa0MySADjrT0=';


-- Tablespace: rv_tablespace

-- DROP TABLESPACE IF EXISTS rv_tablespace;

CREATE TABLESPACE rv_tablespace
  OWNER rv_user
  LOCATION 'C:\Tablespace';

ALTER TABLESPACE rv_tablespace
  OWNER TO rv_user;	


-- Database: rv_database

-- DROP DATABASE IF EXISTS rv_database;

CREATE DATABASE rv_database
    WITH
    OWNER = rv_user
    ENCODING = 'UTF8'
    LC_COLLATE = 'Korean_Korea.949'
    LC_CTYPE = 'Korean_Korea.949'
    TABLESPACE = rv_tablespace
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
