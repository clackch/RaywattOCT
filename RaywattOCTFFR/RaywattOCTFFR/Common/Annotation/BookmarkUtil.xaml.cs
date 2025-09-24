using log4net;
using RaywattOCTFFR.Common.Annotation.Models;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace RaywattOCTFFR.Common.Annotation
{
    /// <summary>
    /// BookmarkUtil.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BookmarkUtil : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BookmarkUtil));

        //---------------------------------------------------------------------------------------------------- Field

        public ObservableCollection<Bookmark> Bookmarks
        {
            get { return (ObservableCollection<Bookmark>)GetValue(BookmarksProperty); }
            set { SetValue(BookmarksProperty, value); }
        }

        private static readonly DependencyProperty BookmarksProperty =
            DependencyProperty.Register("Bookmarks", typeof(ObservableCollection<Bookmark>), typeof(BookmarkUtil), new PropertyMetadata(null));

        public int BookmarkFrameNumber
        {
            get { return (int)GetValue(BookmarkFrameNumberProperty); }
            set { this.SetValue(BookmarkFrameNumberProperty, value); }
        }

        private static readonly DependencyProperty BookmarkFrameNumberProperty =
            DependencyProperty.Register("BookmarkFrameNumber", typeof(int), typeof(BookmarkUtil), new PropertyMetadata(default(int), OnPropertyChanged));

        public double BookmarkLongitudeX
        {
            get { return (double)GetValue(BookmarkLongitudeXProperty); }
            set { this.SetValue(BookmarkLongitudeXProperty, value); }
        }

        private static readonly DependencyProperty BookmarkLongitudeXProperty =
            DependencyProperty.Register("BookmarkLongitudeX", typeof(double), typeof(BookmarkUtil), new PropertyMetadata(default(double)));

        public int BookmarkOutFrameNumber
        {
            get { return (int)GetValue(BookmarkOutFrameNumberProperty); }
            set { this.SetValue(BookmarkOutFrameNumberProperty, value); }
        }

        private static readonly DependencyProperty BookmarkOutFrameNumberProperty =
            DependencyProperty.Register("BookmarkOutFrameNumber", typeof(int), typeof(BookmarkUtil), new PropertyMetadata(default(int)));

        //---------------------------------------------------------------------------------------------------- Constructor
        public BookmarkUtil()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------------------------------------------- Event
        private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        { 
            var bookmarkUtil = dependencyObject as BookmarkUtil;

            bool isExist = false;

            if (bookmarkUtil.Bookmarks == null)
                return;

            foreach (Bookmark bookmark in bookmarkUtil.Bookmarks)
            {
                if (bookmark.FrameNumber == bookmarkUtil.BookmarkFrameNumber)
                {
                    isExist = true;
                    break;
                }
            }

            bookmarkUtil.MarkBtn.IsChecked = isExist;
        }

        private void toggle_Bookmark(object sender, RoutedEventArgs e)
        {
            int existIndex = -1;

            for (int i= 0; i<Bookmarks.Count; i++)
            {
                if(Bookmarks[i].FrameNumber == this.BookmarkFrameNumber)
                {
                    existIndex = i;
                    break;
                }
            }

            if (existIndex > -1)
            {
                Bookmarks.RemoveAt(existIndex);
                MarkBtn.IsChecked = false;
            }
            else
            {
                Bookmarks.Add(new Bookmark { FrameNumber = this.BookmarkFrameNumber, LongitudeX = this.BookmarkLongitudeX });
                MarkBtn.IsChecked = true;
            }            
        }

        private void delete_All(object sender, RoutedEventArgs e)
        {
            Bookmarks.Clear();
            MarkBtn.IsChecked = false;
        }

        private void click_bookmark(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Image indicator = (Image)sender;
            Bookmark bookmark = (Bookmark)indicator.DataContext;

            BookmarkOutFrameNumber = bookmark.FrameNumber;
        }

        //---------------------------------------------------------------------------------------------------- Function

    }
}
