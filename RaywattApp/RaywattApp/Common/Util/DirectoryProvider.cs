using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

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
        public string Path { get; set; }
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

        public ObservableCollection<Item> Traverse(DirectoryItem it)
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

        public string AddDirectory(string path, string name)
        {
            string fullPath = path + "\\" + name;

            if (Directory.Exists(fullPath))
                return "D";

            Directory.CreateDirectory(fullPath);

            DirectoryItem findDirPosition = FindDirectory((DirectoryItem)DirItems[0], path);
            DirectoryItem newFolder = new DirectoryItem { Name = name, Path = fullPath };
            findDirPosition.AddDirItem(newFolder);

            return "S";
        }

        public string RenameDirectory(string originPath, string orginName, string name)
        {
            string newPath = originPath.Substring(0, originPath.LastIndexOf(orginName) - 1) + "\\" + name;
            
            if (Directory.Exists(newPath))
                return "D";

            Directory.Move(originPath, newPath);

            DirectoryItem dirPosition = FindDirectory((DirectoryItem)DirItems[0], originPath);
            dirPosition.Name = name;
            
            return "S";
        }

        private DirectoryItem FindDirectory(DirectoryItem it, string path)
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

        private DirectoryItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            DirectoryItem directoryItme = new DirectoryItem { Name = directoryInfo.Name, Path = directoryInfo.FullName };

            foreach (var directory in directoryInfo.GetDirectories())
            {
                if(directory.Attributes == FileAttributes.Directory)
                    directoryItme.AddDirItem(CreateDirectoryNode(directory));
            }

            return directoryItme;
        }

        public ObservableCollection<Item> DirItems => _rootDirectoryItem.Traverse(_rootDirectoryItem);
    }
}
