-- SCHEMA: rv_schema

-- DROP SCHEMA IF EXISTS rv_schema ;

CREATE SCHEMA IF NOT EXISTS rv_schema
    AUTHORIZATION rv_user;


-- Table: rv_schema.configuration

-- DROP TABLE IF EXISTS rv_schema.configuration;

CREATE TABLE IF NOT EXISTS rv_schema.configuration
(
    classification character varying(10) COLLATE pg_catalog."default" NOT NULL,
    key character varying(10) COLLATE pg_catalog."default" NOT NULL,
    value character varying(100) COLLATE pg_catalog."default",
    buffer character varying(100) COLLATE pg_catalog."default",
    CONSTRAINT configuration_pkey PRIMARY KEY (classification, key)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.configuration
    OWNER to rv_user;
	

-- Table: rv_schema.code

-- DROP TABLE IF EXISTS rv_schema.code;

CREATE TABLE IF NOT EXISTS rv_schema.code
(
    classification character varying(4) COLLATE pg_catalog."default" NOT NULL,
    key character varying(4) COLLATE pg_catalog."default" NOT NULL,
    value character varying(50) COLLATE pg_catalog."default",
    buffer1 character varying(50) COLLATE pg_catalog."default",
    buffer2 character varying(50) COLLATE pg_catalog."default",
    sort_order integer,
    description character varying(100) COLLATE pg_catalog."default",
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT code_pkey PRIMARY KEY (classification, key)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.code
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
    patient_id character varying(9) COLLATE pg_catalog."default",
    physician_name character varying(40) COLLATE pg_catalog."default",
    accession_number character varying(6) COLLATE pg_catalog."default",
    accession_name character varying(40) COLLATE pg_catalog."default",
    comment character varying(200) COLLATE pg_catalog."default",
    vessel character varying(20) COLLATE pg_catalog."default",
    procedure character varying(20) COLLATE pg_catalog."default",
    num_of_frames integer,
    image character varying(200) COLLATE pg_catalog."default",
    pullback_type character varying(4) COLLATE pg_catalog."default",	
    pullback_length character varying(4) COLLATE pg_catalog."default",
	angio_co_registration boolean,
	indicator_degree real,
	preset_name character varying(40) COLLATE pg_catalog."default",
    calcium_threshold integer,
    expansion_calculation character varying(4) COLLATE pg_catalog."default",
    expansion_threshold integer,
    apposition_threshold real,
	brightness integer,
    contrast integer,
	section_proximal integer,
    section_distal integer,
    create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT patient_case_pkey PRIMARY KEY (id)
        USING INDEX TABLESPACE rv_tablespace,
    CONSTRAINT patient_case_patient_id_fkey FOREIGN KEY (patient_id)
        REFERENCES rv_schema.patient (id) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE CASCADE
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.patient_case
    OWNER to rv_user;


-- Table: rv_schema.patient_case_annotation

-- DROP TABLE IF EXISTS rv_schema.patient_case_annotation;

CREATE TABLE IF NOT EXISTS rv_schema.patient_case_annotation
(
    id character varying(24) COLLATE pg_catalog."default" NOT NULL,
    bookmark text COLLATE pg_catalog."default",
    longitude text COLLATE pg_catalog."default",
    cross_section text COLLATE pg_catalog."default",
    lumen_contour text COLLATE pg_catalog."default",
	create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT patient_case_annotation_pkey PRIMARY KEY (id)
        USING INDEX TABLESPACE rv_tablespace,
    CONSTRAINT patient_case_annotation_id_fkey FOREIGN KEY (id)
        REFERENCES rv_schema.patient_case (id) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE CASCADE
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.patient_case_annotation
    OWNER to rv_user;


-- Table: rv_schema.physician

-- DROP TABLE IF EXISTS rv_schema.physician;

CREATE TABLE IF NOT EXISTS rv_schema.physician
(
    name character varying(40) COLLATE pg_catalog."default" NOT NULL,
    create_date timestamp without time zone,
    CONSTRAINT physician_pkey PRIMARY KEY (name)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.physician
    OWNER to rv_user;
	

-- Table: rv_schema.patient_case_preset

-- DROP TABLE IF EXISTS rv_schema.patient_case_preset;

CREATE TABLE IF NOT EXISTS rv_schema.patient_case_preset
(
    id character varying(24) COLLATE pg_catalog."default" NOT NULL,
    preset_name character varying(40) COLLATE pg_catalog."default",
    calcium_threshold integer,
    expansion_calculation character varying(4) COLLATE pg_catalog."default",
    expansion_threshold integer,
    apposition_threshold real,
    default_set boolean,
	create_date timestamp without time zone,
    update_date timestamp without time zone,
    CONSTRAINT patient_case_set_pkey PRIMARY KEY (id)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.patient_case_preset
    OWNER to rv_user;
	

-- Table: rv_schema.dicom_property

-- DROP TABLE IF EXISTS rv_schema.dicom_property;

CREATE TABLE IF NOT EXISTS rv_schema.dicom_property
(
    tag character varying(8) COLLATE pg_catalog."default" NOT NULL,
    tag_name character varying(200) COLLATE pg_catalog."default" NOT NULL,
    value character varying(200) COLLATE pg_catalog."default",
    CONSTRAINT dicom_property_pkey PRIMARY KEY (tag_name)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.dicom_property
    OWNER to rv_user;


-- Table: rv_schema.cath_room

-- DROP TABLE IF EXISTS rv_schema.cath_room;

CREATE TABLE IF NOT EXISTS rv_schema.cath_room
(
    num integer NOT NULL,
    name character varying(50) COLLATE pg_catalog."default",
    chp_file character varying(200) COLLATE pg_catalog."default",
    rect_left real,
    rect_top real,
    rect_right real,
    rect_bottom real,
    CONSTRAINT cath_room_pkey PRIMARY KEY (num)
        USING INDEX TABLESPACE rv_tablespace
)

TABLESPACE rv_tablespace;

ALTER TABLE IF EXISTS rv_schema.cath_room
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
	RETURN COALESCE(res_value, arg_key);
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_code(character varying, character varying)
    OWNER TO rv_user;
	

-- FUNCTION: rv_schema.fn_patient(character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_patient(character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_patient(
	arg_id character varying)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value character varying;
	BEGIN
		SELECT concat("lastname", ', ', "firstname") into res_value
		FROM rv_schema.patient
		WHERE "id" = arg_id;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_patient(character varying)
    OWNER TO rv_user;


-- FUNCTION: rv_schema.fn_patient_birth(character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_patient_birth(character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_patient_birth(
	arg_id character varying)
    RETURNS date
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value date;
	BEGIN
		SELECT birthdate into res_value
		FROM rv_schema.patient
		WHERE "id" = arg_id;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_patient_birth(character varying)
    OWNER TO rv_user;


-- FUNCTION: rv_schema.fn_patient_gender(character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_patient_gender(character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_patient_gender(
	arg_id character varying)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value character varying;
	BEGIN
		SELECT gender into res_value
		FROM rv_schema.patient
		WHERE "id" = arg_id;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_patient_gender(character varying)
    OWNER TO rv_user;


-- FUNCTION: rv_schema.fn_datel10n(timestamp without time zone)

-- DROP FUNCTION IF EXISTS rv_schema.fn_datel10n(timestamp without time zone);

CREATE OR REPLACE FUNCTION rv_schema.fn_datel10n(
	arg_date timestamp without time zone)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
res_value character varying;
lang_code character varying;
BEGIN
	SELECT key INTO lang_code
	FROM rv_schema.configuration
	WHERE classification = 'L10N' AND value = 'Y';
	
	CASE lang_code
	WHEN 'en-US' THEN RETURN TO_CHAR(arg_date, 'MM/dd/yyyy');
	WHEN 'ko-KR' THEN RETURN TO_CHAR(arg_date, 'yyyy-MM-dd');
	ELSE RETURN TO_CHAR(arg_date, 'MM/dd/yyyy');
	END CASE;
	
RETURN res_value;
END;
$BODY$;

ALTER FUNCTION rv_schema.fn_datel10n(timestamp without time zone)
    OWNER TO rv_user;


-- FUNCTION: rv_schema.fn_lastcase(character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_lastcase(character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_lastcase(
	arg_id character varying)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value character varying;
	BEGIN
		SELECT 
			concat(
			(SELECT to_char("create_date", 'yyyy-MM-dd') FROM rv_schema.patient_case WHERE "patient_id" = arg_id ORDER BY "create_date" DESC LIMIT 1)
			, ' ('
			,(SELECT count(*) FROM rv_schema.patient_case WHERE "patient_id" = arg_id)
			, ')'
			) 
			into res_value;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_lastcase(character varying)
    OWNER TO rv_user;


-- FUNCTION: rv_schema.fn_displaylastcase(character varying)

-- DROP FUNCTION IF EXISTS rv_schema.fn_displaylastcase(character varying);

CREATE OR REPLACE FUNCTION rv_schema.fn_displaylastcase(
	arg_id character varying)
    RETURNS character varying
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
	DECLARE
	res_value character varying;
	BEGIN
		SELECT 
			concat(
			(SELECT rv_schema.fn_dateL10n("create_date") FROM rv_schema.patient_case WHERE "patient_id" = arg_id ORDER BY "create_date" DESC LIMIT 1)
			, ' ('
			,(SELECT count(*) FROM rv_schema.patient_case WHERE "patient_id" = arg_id)
			, ')'
			) 
			into res_value;
	RETURN res_value;
	END;
$BODY$;

ALTER FUNCTION rv_schema.fn_displaylastcase(character varying)
    OWNER TO rv_user;
