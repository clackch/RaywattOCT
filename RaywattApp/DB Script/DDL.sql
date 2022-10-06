-- SCHEMA: rv_schema

-- DROP SCHEMA IF EXISTS rv_schema ;

CREATE SCHEMA IF NOT EXISTS rv_schema
    AUTHORIZATION rv_user;


-- SEQUENCE: rv_schema.log_file_index_seq

-- DROP SEQUENCE IF EXISTS rv_schema.log_file_index_seq;

CREATE SEQUENCE IF NOT EXISTS rv_schema.log_file_index_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 2147483647
    CACHE 1;

ALTER SEQUENCE rv_schema.log_file_index_seq
    OWNER TO rv_user;


-- SEQUENCE: rv_schema.physician_index_seq

-- DROP SEQUENCE IF EXISTS rv_schema.physician_index_seq;

CREATE SEQUENCE IF NOT EXISTS rv_schema.physician_index_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 2147483647
    CACHE 1;

ALTER SEQUENCE rv_schema.physician_index_seq
    OWNER TO rv_user;


-- Table: rv_schema.code

-- DROP TABLE IF EXISTS rv_schema.code;

CREATE TABLE IF NOT EXISTS rv_schema.code
(
    classification character varying(4) COLLATE pg_catalog."default" NOT NULL,
    key character varying(4) COLLATE pg_catalog."default" NOT NULL,
    value character varying(50) COLLATE pg_catalog."default",
    buffer1 character varying(50) COLLATE pg_catalog."default",
    buffer2 character varying(50) COLLATE pg_catalog."default",
    description character varying(100) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT code_pkey PRIMARY KEY (classification, key)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.code
    OWNER to rv_user;


-- Table: rv_schema.log_file

-- DROP TABLE IF EXISTS rv_schema.log_file;

CREATE TABLE IF NOT EXISTS rv_schema.log_file
(
    index integer NOT NULL DEFAULT nextval('rv_schema.log_file_index_seq'::regclass),
    classification character varying(4) COLLATE pg_catalog."default",
    name character varying(200) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT log_file_pkey PRIMARY KEY (index)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.log_file
    OWNER to rv_user;
	
-- Table: rv_schema.message

-- DROP TABLE IF EXISTS rv_schema.message;

CREATE TABLE IF NOT EXISTS rv_schema.message
(
    language character varying(2) COLLATE pg_catalog."default" NOT NULL,
    key character varying(4) COLLATE pg_catalog."default" NOT NULL,
    value character varying(200) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT message_pkey PRIMARY KEY (language, key)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.message
    OWNER to rv_user;


-- Table: rv_schema.patient

-- DROP TABLE IF EXISTS rv_schema.patient;

CREATE TABLE IF NOT EXISTS rv_schema.patient
(
    id character varying(9) COLLATE pg_catalog."default" NOT NULL,
    lastname character varying(20) COLLATE pg_catalog."default",
    firstname character varying(20) COLLATE pg_catalog."default",
    birthdate date,
    gender character varying(1) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT patient_pkey PRIMARY KEY (id)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.patient
    OWNER to rv_user;


-- Table: rv_schema.patient_case

-- DROP TABLE IF EXISTS rv_schema.patient_case;

CREATE TABLE IF NOT EXISTS rv_schema.patient_case
(
    id character varying(24) COLLATE pg_catalog."default" NOT NULL,
    patient_name character varying(9) COLLATE pg_catalog."default",
    physician_id character varying(9) COLLATE pg_catalog."default",
    accession_number character varying(6) COLLATE pg_catalog."default",
    accession_name character varying(40) COLLATE pg_catalog."default",
    comment character varying(200) COLLATE pg_catalog."default",
    vessel character varying(4) COLLATE pg_catalog."default",
    procedure character varying(4) COLLATE pg_catalog."default",
    thumbnail_no integer,
    still_image_yn character varying(1) COLLATE pg_catalog."default",
    image oid,
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT patient_case_pkey PRIMARY KEY (id)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.patient_case
    OWNER to rv_user;


-- Table: rv_schema.physician

-- DROP TABLE IF EXISTS rv_schema.physician;

CREATE TABLE IF NOT EXISTS rv_schema.physician
(
    index integer NOT NULL DEFAULT nextval('rv_schema.physician_index_seq'::regclass),
    id character varying(9) COLLATE pg_catalog."default",
    name character varying(40) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT physician_pkey PRIMARY KEY (index)
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.physician
    OWNER to rv_user;


-- FUNCTION: rv_schema.fn_code(character varying, character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_code(character varying, character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_code(
	arg_classification character varying,
	arg_key character varying)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value character varying;
	BEGIN
		SELECT "value" into res_value
		FROM rv_schema.code
		WHERE "classification" = arg_classification AND "key" = arg_key;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_code(character varying, character varying)
    OWNER TO rv_user;