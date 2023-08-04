-- rv_schema.configuration
DELETE FROM rv_schema.configuration;
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'en-US', 'Y', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'ko-KR', 'N', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Terms&Cond', 'AgreeYN', 'N', '');

-- rv_schema.code
DELETE FROM rv_schema.code;
-- GENDER
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'M', 'Male', 1, 'Gender Male', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'F', 'Female', 2, 'Gender Female', now(), now());
-- VESSEL
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$000', 'Not Selected', 0, 'Vessel Not Selected', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$001', 'RCA Prox', 1, 'Vessel RCA Prox', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$002', 'RCA Mid', 2, 'Vessel RCA Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$003', 'RCA Distal', 3, 'Vessel RCA Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$004', 'PDA', 4, 'Vessel PDA', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$005', 'Left Main', 5, 'Vessel Left Main', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$006', 'LAD Prox', 6, 'Vessel LAD Prox', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$007', 'LAD Mid', 7, 'Vessel LAD Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$008', 'LAD Distal', 8, 'Vessel LAD Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$009', 'Diagonal 1', 9, 'Vessel Diagonal 1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$010', 'Diagonal 2', 10, 'Vessel Diagonal 2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$011', 'LCX Prox', 11, 'Vessel LCX Prox', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$012', 'LCX OM1', 12, 'Vessel LCX OM1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$013', 'LCX Mid', 13, 'Vessel LCX Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$014', 'LCX OM2', 14, 'Vessel LCX OM2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$015', 'LCX Distal', 15, 'Vessel LCX Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$OTH', 'Other', 16, 'Vessel Other', now(), now());
-- PROCEDURE
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$001', 'Pre-PCI', 1, 'Procedure Pre-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$002', 'Post-PCI', 2, 'Procedure Post-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$003', 'Follow-Up', 3, 'Procedure Follow-Up', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$OTH', 'Other', 4, 'Procedure Other', now(), now());
-- EXPANSION CALCULATION
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('EXCC', 'TAPE', 'Tapered', 1, 'Expansion Calculation Tapered', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('EXCC', 'OTHE', 'Other', 2, 'Expansion Calculation Other', now(), now());

-- rv_schema.patient_case_preset
DELETE FROM rv_schema.patient_case_preset WHERE id = 'Default';
INSERT INTO rv_schema.patient_case_preset(id, preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold, default_set, create_date, update_date) VALUES ('Default', 'Default', 180, 'TAPE', 90, 0.3, TRUE, now(), now());

-- rv_schema.dicom_property
DELETE FROM rv_schema.dicom_property;
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('ORG_RT', 'Organization Root', '1.2.410.200124');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('APP_ID', 'Application ID', '1');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00020013', 'Implementation Version Name', 'FasterDx');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00020016', 'Source Application Entity Title', 'Raywatt Inc.');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00080060', 'Modality', 'OCT');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00080064', 'Conversion Type', 'SI');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00080070', 'Manufacturer', 'Raywatt Inc.');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00081090', 'Manufacturer''s Model Name', 'FASTER');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00180015', 'Body Part Examined', 'HEART');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00181016', 'Secondary Capture Device Manufacturer', 'Raywatt Inc.');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00181018', 'Secondary Capture Device Manufacturer''s Model Name', 'FASTER');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00181019', 'Secondary Capture Device Software Versions', '1.00.00');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00181020', 'Software Version(s)', '1.00.00');
