using System;

namespace RaywattApp.Models
{
    public class UpdateItem
    {
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }
        public string ModuleId { get; private set; }
        public Version Version => new Version(Major, Minor, Patch);
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public DateTime LastModified { get; private set; }

        // 기본 생성자
        public UpdateItem()
        {
            ModuleId = string.Empty;
            FilePath = string.Empty;
            LastModified = DateTime.Now;
            Major = 0;
            Minor = 0;
            Patch = 0;
            Name = string.Empty;
        }

        // 문자열 파싱 생성자
        public UpdateItem(string moduleAndVersion, string name, string filePath, DateTime lastModified)
        {
            Name = name ?? string.Empty;
            FilePath = filePath ?? string.Empty;
            LastModified = lastModified;

            ParseModuleAndVersion(moduleAndVersion);
        }

        public bool IsNewerThan(UpdateItem other)
        {
            try
            {
                return CompareTo(other) > 0;
            }
            catch
            {
                return false;
            }
        }

        private int CompareTo(UpdateItem other)
        {
            if (other == null) return 1;

            if (!string.Equals(ModuleId, other.ModuleId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Cannot compare different modules: '{ModuleId}' vs '{other.ModuleId}'");
            }

            return Version.CompareTo(other.Version);
        }

        private void ParseModuleAndVersion(string moduleAndVersion)
        {
            if (string.IsNullOrWhiteSpace(moduleAndVersion))
            {
                ModuleId = string.Empty;
                Major = 0;
                Minor = 0;
                Patch = 0;
                return;
            }

            // "Main 1.1.8" 또는 "Main_1.1.8" 형태를 파싱
            var parts = moduleAndVersion.Trim().Split(new char[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                ModuleId = parts[0];

                // 버전 문자열 파싱
                if (Version.TryParse(parts[1], out Version parsedVersion))
                {
                    Major = parsedVersion.Major;
                    Minor = parsedVersion.Minor;
                    Patch = parsedVersion.Build >= 0 ? parsedVersion.Build : 0;
                }
                else
                {
                    // 버전 파싱 실패시 기본값
                    Major = 0;
                    Minor = 0;
                    Patch = 0;
                }
            }
            else if (parts.Length == 1)
            {
                // 버전 정보가 없는 경우
                ModuleId = parts[0];
                Major = 0;
                Minor = 0;
                Patch = 0;
            }
            else
            {
                // 빈 문자열인 경우
                ModuleId = string.Empty;
                Major = 0;
                Minor = 0;
                Patch = 0;
            }
        }
    }
}