using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace RaywattApp.Common.Util
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

        public void GetDirectoryWithExtension(string drive)
        {
            _rootDirectoryItem.Items.Clear();

            ObservableCollection<DirectoryItem> directoryItems = new ObservableCollection<DirectoryItem>();
            directoryItems.Add(CreateDirectoryNodeWithExtension(new DirectoryInfo(drive)));

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

        private static DirectoryItem CreateDirectoryNodeWithExtension(DirectoryInfo directoryInfo)
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
                if (directory.Attributes == FileAttributes.Directory || directory.Attributes == (FileAttributes.ReadOnly | FileAttributes.Directory))
                {
                    string[] check = Directory.GetFiles(directory.FullName, "*." + Constants.FileExtension , SearchOption.AllDirectories);

                    if(check.Length > 0)
                        directoryItme.AddDirItem(CreateDirectoryNodeWithExtension(directory));
                }
            }

            string[] files = Directory.GetFiles(directoryInfo.FullName, "*." + Constants.FileExtension, SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string name = file.Replace(directoryInfo.FullName, "").Replace("\\","");
                DirectoryItem temp = new DirectoryItem { Name = name, Path = file };
                directoryItme.AddDirItem(temp);
            }

            return directoryItme;
        }

        public ObservableCollection<Item> DirItems => DirectoryItem.Traverse(_rootDirectoryItem);
    }
}
