using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RaywattOCTFFR.Services;
using Newtonsoft.Json.Linq;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class FileCopyDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileCopyDialogViewModel));

        private readonly SqlManager _sqlManager;

        private Dictionary<string, string> copyfiles;

        [ObservableProperty]
        private FileExport? _fileExport;

        [ObservableProperty]
        private IList<Patient>? _patients;

        [ObservableProperty]
        private IList<PatientCase>? _patientCases;

        [ObservableProperty]
        private string? _progressText;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string? _dbFilePath;

        [ObservableProperty]
        private string? _contents;

        [ObservableProperty]
        private string? _path;

        [ObservableProperty]
        private string? _annotationFilePath;

        [ObservableProperty]
        private string? _annotations;

        [ObservableProperty]
        private string? _saveFolder;

        [ObservableProperty]
        private bool enableDone = false;

        public FileCopyDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();

            //Export
            if (data.TryGetValue("fileImport", out var importObj) && importObj is Dictionary<string, string> importFiles)//Import
            {
                copyfiles = importFiles;
                Patients = (IList<Patient>)data["patients"];
                Path = data["path"].ToString();
                AnnotationFilePath = data["annotationFilePath"].ToString();

                if (copyfiles != null)
                    FileImportAction();
            }
            else if (data.TryGetValue("logExport", out var logObj) && logObj is Dictionary<string, string> logFiles)//logExport
            {
                copyfiles = logFiles;

                if (copyfiles != null)
                    LogExportAction();
            }
        }

        private async void FileImportAction()
        {
            await FileImport();

            ProgressText = Constants.ExportStatusCompleted;
            EnableDone = true;
        }

        private async void LogExportAction()
        {
            await LogExport();

            ProgressText = Constants.ExportStatusCompleted;
            EnableDone = true;
        }

        private async Task FileImport()
        {
            double progressSize = 90.0;
            double progressInsert = 10.0;
            if (copyfiles.Count > 0)
                await CommonUtil.CopyFiles(copyfiles, prog => Progress = prog, progressSize, progText => ProgressText = progText);
            else
                progressInsert = 100.0;
            await InsertData(Patients, Path, AnnotationFilePath, prog => Progress += prog, progressInsert, progText => ProgressText = progText);
        }

        private async Task InsertData(IList<Patient> patients, string path, string annotationPath, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {

            List<PatientCaseAnnotation> annotations = new List<PatientCaseAnnotation>();
            if (annotationPath != null && annotationPath.Length > 0)
            {
                Tuple<bool, string> result = CommonUtil.Decryptor(CommonUtil.GetDirectoryPath(path) + "\\" + annotationPath);

                if (result.Item1)
                {
                    string json = result.Item2;
                    if (!String.IsNullOrEmpty(json))
                    {
                        JArray arr = JArray.Parse(json);
                        foreach (JObject obj in arr)
                        {
                            PatientCaseAnnotation patientCaseAnnotation = new PatientCaseAnnotation();
                            patientCaseAnnotation.Id = obj["Id"]?.ToString() ?? "null";
                            patientCaseAnnotation.Bookmark = obj["Bookmark"]?.ToString() ?? "null";
                            patientCaseAnnotation.Longitude = obj["Longitude"]?.ToString() ?? "null";
                            patientCaseAnnotation.CrossSection = obj["CrossSection"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenContour = obj["LumenContour"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenSidebranch = obj["LumenSidebranch"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenStent = obj["LumenStent"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenGuidewire = obj["LumenGuidewire"]?.ToString() ?? "null";
                            patientCaseAnnotation.FfrPlaque = obj["FfrPlaque"]?.ToString() ?? "null";
                            patientCaseAnnotation.CoRegistration = obj["CoRegistration"]?.ToString() ?? "null";
                            annotations.Add(patientCaseAnnotation);
                        }
                    }
                }
            }

            int cnt = 0;
            foreach (Patient patient in patients)
            {
                cnt += patient.PatientCaseList.Count;
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();

            foreach (Patient patient in patients)
            {
                sqlParameters.Clear();
                sqlParameters["id"] = patient.Id;
                sqlParameters["lastname"] = patient.Lastname;
                sqlParameters["firstname"] = patient.Firstname;
                sqlParameters["birthdate"] = patient.Birthdate.HasValue ? patient.Birthdate.Value : (object)DBNull.Value;
                sqlParameters["gender"] = patient.Gender;
                sqlParameters["create_date"] = patient.CreateDate;
                sqlParameters["update_date"] = patient.UpdateDate;
                _sqlManager.UpsertPatient(sqlParameters);

                if (patient.PatientCaseList != null)
                {
                    foreach (PatientCase patientCase in patient.PatientCaseList)
                    {
                        await Task.Run(() => {
                            sqlParameters.Clear();
                            sqlParameters["id"] = patientCase.Id;
                            sqlParameters["patient_id"] = patientCase.PatientId;
                            sqlParameters["physician_name"] = patientCase.PhysicianName;
                            sqlParameters["accession_number"] = patientCase.AccessionNumber;
                            sqlParameters["comment"] = patientCase.Comment;
                            sqlParameters["vessel"] = patientCase.Vessel;
                            sqlParameters["location"] = patientCase.Location;
                            sqlParameters["procedure"] = patientCase.Procedure;
                            sqlParameters["pullback_type"] = patientCase.PullbackType;
                            sqlParameters["pullback_length"] = patientCase.PullbackLength;
                            sqlParameters["angio_yn"] = patientCase.AngioYn;
                            sqlParameters["angio_co_registration"] = patientCase.AngioCoRegistration;
                            sqlParameters["indicator_degree"] = patientCase.IndicatorDegree;
                            sqlParameters["flush_media"] = patientCase.FlushMedia;
                            sqlParameters["pullback_trigger"] = patientCase.PullbackTrigger;
                            sqlParameters["colormap"] = patientCase.Colormap;
                            sqlParameters["calcium_threshold"] = patientCase.CalciumThreshold;
                            sqlParameters["expansion_calculation"] = patientCase.ExpansionCalculation;
                            sqlParameters["expansion_threshold"] = patientCase.ExpansionThreshold;
                            sqlParameters["apposition_threshold"] = patientCase.AppositionThreshold;
                            sqlParameters["num_of_frames"] = patientCase.NumOfFrames;
                            sqlParameters["field_of_view"] = patientCase.FieldOfView;
                            sqlParameters["brightness"] = patientCase.Brightness;
                            sqlParameters["contrast"] = patientCase.Contrast;
                            sqlParameters["sheath_diameter"] = patientCase.SheathDiameter;
                            sqlParameters["section_proximal"] = patientCase.SectionProximal;
                            sqlParameters["section_distal"] = patientCase.SectionDistal;
                            sqlParameters["create_date"] = patientCase.CreateDate;
                            sqlParameters["update_date"] = patientCase.UpdateDate;
                            string srcPath = CommonUtil.GetDirectoryPath(path) + "\\" + patientCase.Image;
                            sqlParameters["image"] = System.IO.File.Exists(srcPath) ? patientCase.Image : "";
                            sqlParameters["image_resolution"] = patientCase.ImageResolution;
                            sqlParameters["guidewire_radius"] = patientCase.GuidewireRadius;
                            sqlParameters["z_offset"] = patientCase.ZOffset;

                            var nRows = _sqlManager.UpsertPatientCase(sqlParameters);
                            if (nRows == 1)
                            {
                                foreach (PatientCaseAnnotation annotation in annotations)
                                {
                                    if (annotation.Id == patientCase.Id)
                                    {
                                        sqlParameters.Clear();
                                        sqlParameters["id"] = patientCase.Id;
                                        sqlParameters["cross_section"] = annotation.CrossSection;
                                        sqlParameters["longitude"] = annotation.Longitude;
                                        sqlParameters["bookmark"] = annotation.Bookmark;
                                        sqlParameters["lumen_contour"] = annotation.LumenContour;
                                        sqlParameters["lumen_sidebranch"] = annotation.LumenSidebranch;
                                        sqlParameters["lumen_stent"] = annotation.LumenStent;
                                        sqlParameters["lumen_guidewire"] = annotation.LumenGuidewire;
                                        sqlParameters["ffr_plaque"] = annotation.FfrPlaque;
                                        sqlParameters["co_registration"] = annotation.CoRegistration;
                                        nRows = _sqlManager.UpsertPatientCaseAnnotation(sqlParameters);
                                        if (nRows == 0)
                                            _log.Error("Upsert Error");

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                _log.Error("Upsert Error");
                            }

                            progressCallback(progress / cnt);
                            progressTextCallback(Constants.ExportStatusSaveFile);
                        });
                    }
                }
            }

            if (cnt == 0)
            {
                await Task.Run(() => {
                    progressCallback(progress);
                    progressTextCallback(Constants.ExportStatusSaveFile);
                });
            }
        }

        private async Task LogExport()
        {
            await CommonUtil.CopyFiles(copyfiles, prog => Progress = prog, 100.0, progText => ProgressText = progText);
        }
    }
}
