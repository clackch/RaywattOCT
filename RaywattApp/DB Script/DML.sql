-- rv_schema.configuration
DELETE FROM rv_schema.configuration;
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'en-US', 'Y', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('L10N', 'ko-KR', 'N', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Terms&Cond', 'AgreeYN', 'N', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'brightness', '0', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'contrast', '20', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Present', 'FoV', '10.0', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('LocalHost', 'AeTitle', '', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('LogoutTime', 'TotalTime', '60', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('LogoutTime', 'PreTime', '57', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Password', 'ExpiryDay', '90', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Password', 'MaxCount', '5', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('Password', 'WaitSecond', '30', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('AutoPB', 'LumenMin', '2', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('AutoPB', 'LumenMax', '70', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('AutoPB', 'SNR', '1', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('AutoPB', 'Count', '2', '');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('AutoPB', 'ShowGuide', 'N', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('RefraIndex', 'AIR', '1', 'Refractive Index - Air');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('RefraIndex', 'SALI', '1.333', 'Refractive Index - Saline');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('RefraIndex', 'CONT', '1.44', 'Refractive Index - Contrast');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'Power', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'RJ', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'FG', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'AutoPB', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'Sidebranch', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'Compensate', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'CertIgnore', 'Y', 'Raywatt;');
INSERT INTO rv_schema.configuration(classification, key, value, buffer) VALUES ('TestMode', 'AIFFR', 'Y', 'Raywatt;');

-- rv_schema.code
DELETE FROM rv_schema.code;
-- GENDER
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'M', 'Male', 1, 'Gender Male', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'F', 'Female', 2, 'Gender Female', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('GEND', 'O', 'Other', 3, 'Gender Other', now(), now());
-- FLUSH MEDIA
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('FLMD', 'SALI', 'Saline', 1, 'Flush Media Saline', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('FLMD', 'CONT', 'Contrast', 2, 'Flush Media Contrast', now(), now());
-- PULLBACK TRIGGER
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PBTG', 'MANL', 'Manual', 1, 'Pullback Trigger Manual', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PBTG', 'AUTO', 'Auto', 2, 'Pullback Trigger Auto', now(), now());
-- PULLBACK TYPE
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'HISH', 'High Resolution(50um)', '60|20|3', 1, 'Pullback Type High Resolution(50um)', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'HILO', 'High Resolution - Long(100um)', '100|40|2.5', 2, 'Pullback Type High Resolution - Long(100um)', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'STSH', 'Standard(150um)', '60|60|1', 3, 'Pullback Type Standard(150um)', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'STLO', 'Standard - Long(250um)', '100|100|1', 4, 'Pullback Type Standard - Long(250um)', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'FAST', 'Faster for specialized care(300um)', '60|120|0.5', 5, 'Pullback Type Faster for specialized care(300um)', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, sort_order, description, create_date, update_date) VALUES ('PBTY', 'NOPB', 'No Pullback', '0|0|3', 6, 'Pullback Type No Pullback', now(), now());
-- COLORMAP
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'GRGR', 'Green-gray', 1, 'Colormap Green-gray', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'GRAY', 'Gray', 2, 'Colormap Gray', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'ORNG', 'Orange', 3, 'Colormap Orange', now(), now());
-- INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('CLMP', 'EHOR', 'Enhanced-orange', 4, 'Colormap Enhanced-gray (acting in app as orange LUT but actually it is gray converting LUT)', now(), now());
-- PROCEDURE
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$001', 'Pre-PCI', 1, 'Procedure Pre-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$002', 'Post-PCI', 2, 'Procedure Post-PCI', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('PROC', '$003', 'Follow-Up', 3, 'Procedure Follow-Up', now(), now());
-- VESSEL
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$000', 'Not Selected', '$000', '', 0, 'Vessel Not Selected', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$001', 'Left Main', '$001', '', 1, 'Vessel Left Main', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$002', 'LAD', '$002', '', 2, 'Vessel LAD', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$003', 'Diagonal 1', '$002', 'D1', 3, 'Vessel Diagonal 1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$004', 'Diagonal 2', '$002', 'D2', 4, 'Vessel Diagonal 2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$005', 'LCX', '$005', '', 5, 'Vessel LCX', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$006', 'Obtuse Marginal 1', '$005', 'OM1', 6, 'Vessel LCX OM1', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$007', 'Obtuse Marginal 2', '$005', 'OM2', 7, 'Vessel LCX OM2', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$008', 'RCA', '$008', '', 8, 'Vessel RCA', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$009', 'PDA', '$008', 'PDA', 9, 'Vessel PDA', now(), now());
INSERT INTO rv_schema.code( classification, key, value, buffer1, buffer2, sort_order, description, create_date, update_date) VALUES ('VESS', '$OTH', 'Other', '$OTH', '', 10, 'Vessel Other', now(), now());
-- LOCATION
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$000', 'Not Selected', 0, 'Location Not Selected', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$001', 'Proximal', 1, 'Location Proximal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$002', 'Proximal-Mid', 2, 'Location Proximal-Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$003', 'Mid', 3, 'Location Mid', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$004', 'Mid-Distal', 4, 'Location Mid-Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$005', 'Distal', 5, 'Location Distal', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('LOCT', '$006', 'Other', 6, 'Location Other', now(), now());
-- DICOM SERVER TYPE
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('DICM', '0', 'PACS', 0, 'PACS', now(), now());
INSERT INTO rv_schema.code( classification, key, value, sort_order, description, create_date, update_date) VALUES ('DICM', '1', 'MWL', 1, 'MWL', now(), now());

-- rv_schema.dicom_property
DELETE FROM rv_schema.dicom_property;
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('ORG_RT', 'Organization Root', '1.2.410.200124');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('APP_ID', 'Application ID', '1');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00080060', 'Modality', 'OCT');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00080064', 'Conversion Type', 'SI');
INSERT INTO rv_schema.dicom_property( tag, tag_name, value) VALUES ('00180015', 'Body Part Examined', 'HEART');

-- rv_schema.user
INSERT INTO rv_schema.user(id, password, admin, comment, create_date, update_date) VALUES ('admin', 'admin', true, 'Administrator', now(), now());
