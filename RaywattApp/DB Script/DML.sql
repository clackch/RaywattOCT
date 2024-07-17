-- rv_schema.configuration
DELETE FROM rv_schema.configuration;
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'en-US', 'Y', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'ko-KR', 'N', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Terms&Cond', 'AgreeYN', 'N', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'brightness', '0', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'contrast', '20', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'FoV', '10.0', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'Power', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'RJ', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'FG', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'Image', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'ML', 'N', 'Raywatt;');

-- rv_schema.code
DELETE FROM rv_schema.code;
-- GENDER
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'M', 'Male', 1, 'Gender Male', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'F', 'Female', 2, 'Gender Female', now(), now());
-- FLUSH MEDIA
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('FLMD', 'SALI', 'Saline', 1, 'Flush Media Saline', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('FLMD', 'CONT', 'Contrast', 2, 'Flush Media Contrast', now(), now());
-- PULLBACK TRIGGER
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PBTG', 'MANL', 'Manual', 1, 'Pullback Trigger Manual', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PBTG', 'AUTO', 'Auto', 2, 'Pullback Trigger Auto', now(), now());
-- PULLBACK TYPE
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'STSH', 'Standard', '60|60|1', 1, 'Pullback Type Standard', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'STLO', 'Standard - Long', '100|100|1', 2, 'Pullback Type Standard - Long', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'HISH', 'High Resolution', '60|20|3', 3, 'Pullback Type High Resolution', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'HILO', 'High Resolution - Long', '100|40|2.5', 4, 'Pullback Type High Resolution - Long', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'FAST', 'Faster for specialized care', '60|120|0.5', 5, 'Pullback Type Faster for specialized care', now(), now());
-- COLORMAP
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'GRGR', 'Green-gray', 1, 'Colormap Green-gray', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'GRAY', 'Gray', 2, 'Colormap Gray', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'ORNG', 'Orange', 3, 'Colormap Orange', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'SPEC', 'K-Gray', 4, 'Colormap Enhenced-gray', now(), now());
-- PROCEDURE
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$001', 'Pre-PCI', 1, 'Procedure Pre-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$002', 'Post-PCI', 2, 'Procedure Post-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$003', 'Follow-Up', 3, 'Procedure Follow-Up', now(), now());
-- VESSEL
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$000', 'Not Selected', 0, 'Vessel Not Selected', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$001', 'Left Main', 1, 'Vessel Left Main', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$002', 'LAD', 2, 'Vessel LAD', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$003', 'Diagonal 1', 3, 'Vessel Diagonal 1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$004', 'Diagonal 2', 4, 'Vessel Diagonal 2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$005', 'LCX', 5, 'Vessel LCX', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$006', 'LCX OM1', 6, 'Vessel LCX OM1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$007', 'LCX OM2', 7, 'Vessel LCX OM2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$008', 'RCA', 8, 'Vessel RCA', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$009', 'PDA', 9, 'Vessel PDA', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('VESS', '$OTH', 'Other', 10, 'Vessel Other', now(), now());
-- LOCATION
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$000', 'Not Selected', 0, 'Location Not Selected', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$001', 'Proximal', 1, 'Location Proximal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$002', 'Proximal-Mid', 2, 'Location Proximal-Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$003', 'Mid', 3, 'Location Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$004', 'Mid-Distal', 4, 'Location Mid-Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$005', 'Distal', 5, 'Location Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$006', 'Other', 6, 'Location Other', now(), now());

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
