using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Bases;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace RaywattOCTFFR.Common.Util
{
    public class Item : ObservableObject
    {
        private string _name;
        public string Name 
        { 
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _path;
        public string Path 
        { 
            get { return _path; }
            set { _path = value; OnPropertyChanged(nameof(Path)); }
        }

        private bool _isFile;
        public bool IsFile
        {
            get { return _isFile; }
            set { _isFile = value; OnPropertyChanged(nameof(IsFile)); }
        }
    }

    public class DirectoryItem : Item
    {
        public DirectoryItem()
        {
            Items = new ObservableCollection<DirectoryItem>();
        }

        private ObservableCollection<DirectoryItem> _items;
        public ObservableCollection<DirectoryItem> Items
        {
            get { return _items; }
            set { _items = value; OnPropertyChanged(nameof(Items)); }
        }

        public void AddDirItem(DirectoryItem directoryItem)
        {
            Items.Add(directoryItem);
        }

        public static ObservableCollection<Item> Traverse(DirectoryItem it)
        {
            var items = new ObservableCollection<Item>();

            foreach (var itm in it.Items)
            {
                Traverse(itm);
                items.Add(itm);
            }

            return items;
        }
    }

    public class DirectoryProvider
    {
        public readonly DirectoryItem _rootDirectoryItem;

        public DirectoryProvider()
        {
            _rootDirectoryItem = new DirectoryItem { Name = "Root", Path = "Root" };
        }

        public void GetDirectory(string drive)
        {
            _rootDirectoryItem.Items.Clear();

            ObservableCollection<DirectoryItem> directoryItems = new ObservableCollection<DirectoryItem>();
            directoryItems.Add(CreateDirectoryNode(new DirectoryInfo(drive)));

            _rootDirectoryItem.Items = directoryItems;
        }

        public void GetDirectoryWithExtension(string drive, string fileExtension)
        {
            _rootDirectoryItem.Items.Clear();

            if (String.IsNullOrEmpty(fileExtension))
                return;

            ObservableCollection<DirectoryItem> directoryItems = new ObservableCollection<DirectoryItem>();
            directoryItems.Add(CreateDirectoryNodeWithExtension(new DirectoryInfo(drive), fileExtension));

            _rootDirectoryItem.Items = directoryItems;
        }

        public static bool DuplicateCheck(string path, string name)
        {
            string fullPath = path + "\\" + name;

            if (Directory.Exists(fullPath))
                return false;

            return true;
        }

        public bool AddDirectory(string path, string name)
        {
            string fullPath = path + "\\" + name;

            Directory.CreateDirectory(fullPath);

            DirectoryItem findDirPosition = FindDirectory((DirectoryItem)DirItems[0], path);
            DirectoryItem newFolder = new DirectoryItem { Name = name, Path = fullPath };
            findDirPosition.AddDirItem(newFolder);
            findDirPosition.Items = SortDirectoryItem(findDirPosition.Items);


            return true;
        }

        private static ObservableCollection<DirectoryItem> SortDirectoryItem(ObservableCollection<DirectoryItem> items)
        {
            var tempObservableCollection = items.OrderBy(x => x.Name).ToList();
            foreach (var temp in tempObservableCollection)
            {
                int oldIndex = items.IndexOf(temp);
                int newIndex = tempObservableCollection.IndexOf(temp);
                items.Move(oldIndex, newIndex);
            }

            return items;
        }

        public static bool DuplicateCheckRename(string originPath, string orginName, string name)
        {
            int index = originPath.LastIndexOf(orginName, StringComparison.OrdinalIgnoreCase);
            string basePath = new string(originPath.AsSpan(0, index - 1));
            string newPath = Path.Combine(basePath, name);

            if (Directory.Exists(newPath) && !string.Equals(name, orginName, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public bool RenameDirectory(string originPath, string orginName, string name)
        {
            int index = originPath.LastIndexOf(orginName) - 1;
            string newPath = string.Concat(originPath.AsSpan(0, index), "\\", name);

            Directory.Move(originPath, newPath);

            DirectoryItem dirPosition = FindDirectory((DirectoryItem)DirItems[0], originPath);
            dirPosition.Path = newPath;
            dirPosition.Name = name;
            ChangeSubPath(dirPosition, originPath, newPath);

            string parentPath = originPath.Substring(0, originPath.LastIndexOf(orginName) - 1);
            DirectoryItem parentPosition = FindDirectory((DirectoryItem)DirItems[0], parentPath);
            parentPosition.Items = new ObservableCollection<DirectoryItem>(parentPosition.Items.OrderBy(x => x.Name));

            return true;
        }

        private static void ChangeSubPath(DirectoryItem it, string originPath, string newPath)
        {
            foreach(var itm in it.Items)
            {
                itm.Path = itm.Path.Replace(originPath, newPath);
                ChangeSubPath(itm, originPath, newPath);
            }
        }

        private static DirectoryItem FindDirectory(DirectoryItem it, string path)
        {
            if (it.Path.Equals(path))
            {
                return it;
            }

            DirectoryItem findDir = null;

            foreach (var itm in it.Items)
            {
                findDir = FindDirectory(itm, path);
                if(findDir != null)
                    break;
            }

            return findDir;
        }

        private static DirectoryItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            string dirName = directoryInfo.Name;
            string dirFullName = directoryInfo.FullName;

            if (directoryInfo.Parent == null)
            {
                dirName = dirName.Replace("\\", "");
                dirFullName = directoryInfo.FullName.Replace("\\", "");
            }

            DirectoryItem directoryItme = new DirectoryItem { Name = dirName, Path = dirFullName };

            foreach (var directory in directoryInfo.GetDirectories().OrderBy(f => f.Name))
            {
                if(directory.Attributes == FileAttributes.Directory)
                    directoryItme.AddDirItem(CreateDirectoryNode(directory));
            }

            return directoryItme;
        }

        private static DirectoryItem CreateDirectoryNodeWithExtension(DirectoryInfo directoryInfo, string fileExtension)
        {
            string dirName = directoryInfo.Name;
            string dirFullName = directoryInfo.FullName;

            if (directoryInfo.Parent == null)
            {
                // 굳이 Replace("\\","") 할 필요 없음 (루트 표기만 정규화하고 싶다면 Path.GetPathRoot 사용 고려)
                dirName = dirName.Replace("\\", "");
                dirFullName = dirFullName.Replace("\\", "");
            }

            var node = new DirectoryItem { Name = dirName, Path = dirFullName };

            // 1) 하위 디렉터리 처리: 먼저 하위 노드를 구성한 뒤, 내용이 있으면 추가
            IEnumerable<DirectoryInfo> subDirs;
            try
            {
                subDirs = directoryInfo.EnumerateDirectories().OrderBy(d => d.Name);
            }
            catch
            {
                subDirs = Enumerable.Empty<DirectoryInfo>();
            }

            foreach (var directory in subDirs)
            {
                if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue; // 심볼릭 링크 방지

                var child = CreateDirectoryNodeWithExtension(directory, fileExtension);
                if (child?.Items?.Any() == true) // DirectoryItem에 자식 보유 여부가 판단 가능하다고 가정
                    node.AddDirItem(child);
            }

            // 2) 현재 디렉터리의 파일
            string[] files;
            if (Constants.DicomFileExtension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                files = FindDicomFiles(directoryInfo.FullName, SearchOption.TopDirectoryOnly).ToArray();
            }
            else
            {
                try
                {
                    files = Directory.GetFiles(directoryInfo.FullName, "*." + fileExtension, SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    files = Array.Empty<string>();
                }
            }

            foreach (string file in files)
            {
                // 파일명만 사용 (안전/간단/빠름)
                string name = Path.GetFileName(file);
                var temp = new DirectoryItem { Name = name, Path = file, IsFile = true };
                node.AddDirItem(temp);
            }

            return node;
        }


        private static IEnumerable<string> FindDicomFiles(string directory, SearchOption searchOption)
        {
            var dirQueue = new Queue<DirectoryInfo>();
            dirQueue.Enqueue(new DirectoryInfo(directory));

            while (dirQueue.Count > 0)
            {
                DirectoryInfo? dir = dirQueue.Dequeue();

                // 접근 불가 방어
                if (dir == null || !dir.Exists) continue;

                // 하위 디렉터리 큐잉
                try
                {
                    if (searchOption == SearchOption.AllDirectories)
                    {
                        foreach (var sub in dir.EnumerateDirectories())
                        {
                            // 심볼릭 링크 무한루프 방지
                            if (sub.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                            dirQueue.Enqueue(sub);
                        }
                    }
                }
                catch
                {
                    // 접근권한/경합 문제 등 → 해당 디렉터리는 스킵
                }

                // 현재 디렉터리 파일 검사
                IEnumerable<FileInfo> files;
                try
                {
                    files = dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (var f in files)
                {
                    // 파일명으로 DICOMDIR 컷
                    if (string.Equals(f.Name, "DICOMDIR", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 빠른 확장자 선별 + 매직넘버 검사
                    if (IsDicomFile(f.FullName))
                        yield return f.FullName;
                }
            }
        }

        private static bool IsDicomFile(string filePath)
        {
            // DICOMDIR는 상위에서 이미 필터링한다고 해도 한 번 더 방어 가능
            var fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, "DICOMDIR", StringComparison.OrdinalIgnoreCase))
                return false;

            // 확장자 빠른 필터 (.dcm 또는 무확장자만 허용)
            var ext = Path.GetExtension(filePath);
            if (!string.IsNullOrEmpty(ext) && !ext.Equals(".dcm", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                // 132바이트 미만은 바로 컷
                var fi = new FileInfo(filePath);
                if (!fi.Exists || fi.Length <= 132) return false;

                using var fs = new FileStream(
                    filePath,
                    new FileStreamOptions
                    {
                        Access = FileAccess.Read,
                        Mode = FileMode.Open,
                        Share = FileShare.ReadWrite | FileShare.Delete, // 잠금 충돌 회피
                        Options = FileOptions.None,
                        BufferSize = 256 // 매우 작은 범위만 읽으므로 작게
                    });

                fs.Position = 128;
                Span<byte> sig = stackalloc byte[4];
                int read = fs.Read(sig);
                if (read != 4) return false;

                // 'D''I''C''M' 바이트 비교
                return sig[0] == (byte)'D' && sig[1] == (byte)'I' && sig[2] == (byte)'C' && sig[3] == (byte)'M';
            }
            catch
            {
                return false; // 접근불가/삭제경합 등은 DICOM 아님으로 처리
            }
        }

        public ObservableCollection<Item> DirItems => DirectoryItem.Traverse(_rootDirectoryItem);
    }
}
