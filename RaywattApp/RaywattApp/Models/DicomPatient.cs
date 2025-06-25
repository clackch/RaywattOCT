using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RaywattApp.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    sealed class DicomPatient
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] _patientId = new byte[64];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] _patientName = new byte[64];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private byte[] _patientSex = new byte[16];

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        private byte[] _patientBirthDate = new byte[32];

        private static string GetString(byte[] bytes) =>
            Encoding.ASCII.GetString(bytes).Split('\0')[0];

        private static void SetString(string value, byte[] target)
        {
            Array.Clear(target, 0, target.Length);
            var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
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

        public string PatientBirthDate
        {
            get => GetString(_patientBirthDate);
            set => SetString(value, _patientBirthDate);
        }
    }
}
