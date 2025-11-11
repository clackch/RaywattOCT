using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RaywattApp.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DicomWorklist
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        private byte[] _patientId = new byte[65];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 130)]
        private byte[] _patientName = new byte[130];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private byte[] _patientSex = new byte[2];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        private byte[] _patientBirthDate = new byte[9];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] _accessionNumber = new byte[17];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] _modality = new byte[17];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] _scheduledStationAET = new byte[17];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        private byte[] _spsStartDate = new byte[18];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] _requestedProcedureId = new byte[17];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        private byte[] _referencedSOPClassUID = new byte[65];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        private byte[] _referencedSOPInstanceUID = new byte[65];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
        private byte[] _issuerOfPatientID = new byte[65];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        private byte[] _typeOfPatientID = new byte[17];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        private byte[] _patientBirthTime = new byte[15];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        private byte[] _patientAge = new byte[5];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10240)]
        private byte[] _patientComments = new byte[10240];

        private static string GetString(byte[] bytes) =>
            Encoding.UTF8.GetString(bytes).Split('\0')[0];

        private static void SetString(string value, byte[] target)
        {
            Array.Clear(target, 0, target.Length);
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            Array.Copy(bytes, target, Math.Min(bytes.Length, target.Length - 1));
        }

        public string PatientId
        {
            get => GetString(_patientId);
            set => SetString(value, _patientId);
        }

        public string PatientName
        {
            get => GetString(_patientName);
            set => SetString(value, _patientName);
        }

        public string PatientSex
        {
            get => GetString(_patientSex);
            set => SetString(value, _patientSex);
        }

        public DateTime? PatientBirthDate
        {
            get => ParseDate(GetString(_patientBirthDate));
            set => SetString(value?.ToString("yyyyMMdd") ?? string.Empty, _patientBirthDate);
        }

        public string AccessionNumber
        {
            get => GetString(_accessionNumber);
            set => SetString(value, _accessionNumber);
        }

        public string Modality
        {
            get => GetString(_modality);
            set => SetString(value, _modality);
        }

        public string ScheduledStationAET
        {
            get => GetString(_scheduledStationAET);
            set => SetString(value, _scheduledStationAET);
        }

        public DateTime? SPSStartDate
        {
            get => ParseDate(GetString(_spsStartDate));
            set => SetString(value?.ToString("yyyyMMdd") ?? string.Empty, _spsStartDate);
        }

        public string RequestedProcedureId
        {
            get => GetString(_requestedProcedureId);
            set => SetString(value, _requestedProcedureId);
        }

        public string ReferencedSOPClassUID
        {
            get => GetString(_referencedSOPClassUID);
            set => SetString(value, _referencedSOPClassUID);
        }

        public string ReferencedSOPInstanceUID
        {
            get => GetString(_referencedSOPInstanceUID);
            set => SetString(value, _referencedSOPInstanceUID);
        }

        public string IssuerOfPatientID
        {
            get => GetString(_issuerOfPatientID);
            set => SetString(value, _issuerOfPatientID);
        }

        public string TypeOfPatientID
        {
            get => GetString(_typeOfPatientID);
            set => SetString(value, _typeOfPatientID);
        }

        public string PatientBirthTime
        {
            get => GetString(_patientBirthTime);
            set => SetString(value, _patientBirthTime);
        }

        public string PatientAge
        {
            get => GetString(_patientAge);
            set => SetString(value, _patientAge);
        }

        public string PatientComments
        {
            get => GetString(_patientComments);
            set => SetString(value, _patientComments);
        }

        private DateTime? ParseDate(string dateString)
        {
            return DateTime.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime result) ? result : (DateTime?)null;
        }
    }
}