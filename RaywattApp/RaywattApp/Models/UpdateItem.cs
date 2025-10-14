using System;

namespace RaywattApp.Models
{
    public class UpdateItem
    {
        public int Major { get; private set; }
        public int Minor { get; private set; }
        public int Patch { get; private set; }
        public Version Version => new Version(Major, Minor, Patch);
        public string FilePath { get; private set; }
        public DateTime LastModified { get; private set; }

        // 기본 생성자
        public UpdateItem()
        {
            FilePath = string.Empty;
            LastModified = DateTime.Now;
            Major = 0;
            Minor = 0;
            Patch = 0;
        }

        public UpdateItem(string version, string filePath, DateTime lastModified)
        {
            FilePath = filePath ?? string.Empty;
            LastModified = lastModified;

            if (Version.TryParse(version, out Version parsedVersion))
            {
                Major = parsedVersion.Major;
                Minor = parsedVersion.Minor;
                Patch = parsedVersion.Build >= 0 ? parsedVersion.Build : 0;
            }
            else
            {
                Major = 0;
                Minor = 0;
                Patch = 0;
            }
        }

        public bool IsNewerThan(UpdateItem other)
        {
            return Version.CompareTo(other.Version) > 0;
        }
    }
}