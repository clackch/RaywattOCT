using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaywattApp.Common.Paging
{
    public partial class PagingBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PagingBase));

        private ICommand _pagingFirstCommand;
        public ICommand PagingFirstCommand
        {
            get { return this._pagingFirstCommand ?? (this._pagingFirstCommand = new RelayCommand(PagingFirst)); }
        }

        private ICommand _pagingPreviousCommand;
        public ICommand PagingPreviousCommand
        {
            get { return this._pagingPreviousCommand ?? (this._pagingPreviousCommand = new RelayCommand(PagingPrevious)); }
        }

        private ICommand _pagingNextCommand;
        public ICommand PagingNextCommand
        {
            get { return this._pagingNextCommand ?? (this._pagingNextCommand = new RelayCommand(PagingNext)); }
        }

        private ICommand _pagingLastCommand;
        public ICommand PagingLastCommand
        {
            get { return this._pagingLastCommand ?? (this._pagingLastCommand = new RelayCommand(PagingLast)); }
        }

        [ObservableProperty]
        private IList<int> _pagingPageSize;

        protected bool bCheckInit = false;

        private int _pagingSelectedPageSize;
        public int PagingSelectedPageSize
        {
            set
            {
                //Page Size가 달라졌을 때, Offset 초기화
                this._pagingSelectedPageSize = value;
               
                if (bCheckInit)
                {
                    PagingOffset = 0;
                    PagingNoIdx = 0;
                    ShowPageNo(1);
                    Search();
                }
                    
            }
            get { return _pagingSelectedPageSize; }
        }

        [ObservableProperty]
        protected int _pagingOffset;

        private int _pagingTotalCnt;
        public int PagingTotalCnt
        {
            set
            {
                //검색 결과가 달라졌을 때, Offset 초기화
                if (this._pagingTotalCnt != value)
                {
                    this._pagingTotalCnt = value;
                    OnPropertyChanged(nameof(PagingTotalCnt));

                    PagingOffset = 0;
                    PagingNoIdx = 0;
                    ShowPageNo(1);
                }
            }
            get { return _pagingTotalCnt; }
        }

        private ICommand _columOrderCommand;
        public ICommand ColumOrderCommand
        {
            get { return this._columOrderCommand ?? (this._columOrderCommand = new RelayCommand<DataGridSortingEventArgs>(ColumnOrder)); }
        }

        [ObservableProperty]
        private string _columOrderField;

        protected bool bColumnOrderBy;
        protected string strColumnOrder;

        private ICommand _pagingNoCommand;
        public ICommand PagingNoCommand
        {
            get { return this._pagingNoCommand ?? (this._pagingNoCommand = new RelayCommand<string>(MovePageNo)); }
        }

        [ObservableProperty]
        private string _pagingNo1;

        [ObservableProperty]
        private string _pagingNo2;

        [ObservableProperty]
        private string _pagingNo3;

        [ObservableProperty]
        private string _pagingNo4;

        [ObservableProperty]
        private string _pagingNo5;

        [ObservableProperty]
        private Visibility _pagingVisibilityNo1;

        [ObservableProperty]
        private Visibility _pagingVisibilityNo2;

        [ObservableProperty]
        private Visibility _pagingVisibilityNo3;

        [ObservableProperty]
        private Visibility _pagingVisibilityNo4;

        [ObservableProperty]
        private Visibility _pagingVisibilityNo5;

        [ObservableProperty]
        private int _pagingNoCnt;

        [ObservableProperty]
        private int _pagingNoIdx;

        public PagingBase()
        {
            _log.Debug("PagingBase");

            //Page Size
            PagingPageSize = new List<int>();
            PagingPageSize.Add(10);
            PagingPageSize.Add(30);
            PagingPageSize.Add(50);
            PagingSelectedPageSize = 10;
        }

        virtual protected void Search() { }

        virtual protected void SetHeaderNameInit() { }

        private void PagingFirst()
        {
            if (PagingTotalCnt == 0)
            {
                _log.Debug("PagingTotalCnt == 0");
                return;
            }

            _log.Debug("PagingFirst");

            ShowPageNo(1);

            MovePageNo("PagingNo1");
        }

        private void PagingPrevious()
        {
            if (PagingNoIdx < 5)
            {
                _log.Debug("PagingNoIdx < 5");
                return;
            }

            _log.Debug("PagingPrevious");

            int nCurrPageGroup = PagingNoIdx / 5;
            int nPagePrevGroupNo = int.Parse(PagingNo1) - 5 * nCurrPageGroup;

            ShowPageNo(nPagePrevGroupNo);

            MovePageNo("PagingNo5");
        }

        private void PagingNext()
        {
            if (PagingNoIdx/5 >= PagingTotalCnt/PagingSelectedPageSize/5)
            {
                _log.Debug("PagingNoIdx/5 >= PagingTotalCnt/PagingSelectedPageSize/5");
                return;
            }

            _log.Debug("PagingNext");

            int nCurrPageGroup = PagingNoIdx / 5;
            int nPageNextGroupNo = int.Parse(PagingNo1) + 5 * (nCurrPageGroup + 1);

            ShowPageNo(nPageNextGroupNo);

            MovePageNo("PagingNo1");
        }

        private void PagingLast()
        {
            if (PagingTotalCnt == 0)
            {
                _log.Debug("PagingTotalCnt == 0");
                return;
            }

            _log.Debug("PagingLast");

            int nTotalPageGroup = PagingNoCnt / 5;
            int nPageLastGroupNo = 1 + 5 * nTotalPageGroup;

            ShowPageNo(nPageLastGroupNo);

            MovePageNo("PagingNo" + (PagingNoCnt % 5 + 1));
        }

        private void MovePageNo(string param)
        {
            _log.Debug("MovePageNo : " + param);

            int pagingIndex = 0;

            PropertyInfo piPagingNoName = GetType().GetProperty(param);
            if (piPagingNoName != null)
                pagingIndex = int.Parse(piPagingNoName.GetValue(this).ToString());

            PagingOffset = (pagingIndex - 1) * PagingSelectedPageSize;
            PagingNoIdx = pagingIndex - 1;

            Search();
        }

        protected void ShowPageNo(int nPageGroupNo)
        {
            _log.Debug("ShowPageNo : " + nPageGroupNo);

            PagingNoCnt = PagingTotalCnt / PagingSelectedPageSize + (PagingTotalCnt % PagingSelectedPageSize == 0 ? -1 : 0);
            int nShowEndNo = PagingNoCnt % 5;

            if (PagingNoCnt / 5 == nPageGroupNo / 5)
            {
                PagingVisibilityNo1 = Visibility.Collapsed;
                PagingVisibilityNo2 = Visibility.Collapsed;
                PagingVisibilityNo3 = Visibility.Collapsed;
                PagingVisibilityNo4 = Visibility.Collapsed;
                PagingVisibilityNo5 = Visibility.Collapsed;

                if (nShowEndNo > 3)
                {
                    PagingNo5 = (nPageGroupNo + 4).ToString();
                    PagingVisibilityNo5 = Visibility.Visible;
                }

                if (nShowEndNo > 2)
                {
                    PagingNo4 = (nPageGroupNo + 3).ToString();
                    PagingVisibilityNo4 = Visibility.Visible;
                }

                if (nShowEndNo > 1)
                {
                    PagingNo3 = (nPageGroupNo + 2).ToString();
                    PagingVisibilityNo3 = Visibility.Visible;
                }

                if (nShowEndNo > 0)
                {
                    PagingNo2 = (nPageGroupNo + 1).ToString();
                    PagingVisibilityNo2 = Visibility.Visible;
                }

                if (nShowEndNo > -1)
                {
                    PagingNo1 = (nPageGroupNo).ToString();
                    PagingVisibilityNo1 = Visibility.Visible;
                }
            }
            else
            {
                PagingNo5 = (nPageGroupNo + 4).ToString();
                PagingNo4 = (nPageGroupNo + 3).ToString();
                PagingNo3 = (nPageGroupNo + 2).ToString();
                PagingNo2 = (nPageGroupNo + 1).ToString();
                PagingNo1 = (nPageGroupNo).ToString();

                PagingVisibilityNo1 = Visibility.Visible;
                PagingVisibilityNo2 = Visibility.Visible;
                PagingVisibilityNo3 = Visibility.Visible;
                PagingVisibilityNo4 = Visibility.Visible;
                PagingVisibilityNo5 = Visibility.Visible;
            }
        }     

        private void ColumnOrder(DataGridSortingEventArgs e)
        {
            _log.Debug("ColumnOrder");

            e.Handled = true;

            SetHeaderNameInit();
            //Header 필드명은 Header + Binding Field가 되도록 작성해야 속성 값을 읽을 수 있음
            PropertyInfo piHeaderName = GetType().GetProperty("Header" + e.Column.SortMemberPath);

            if (ColumOrderField.Equals(e.Column.SortMemberPath.ToLower()))
            {
                if (bColumnOrderBy)
                {
                    bColumnOrderBy = false;
                    strColumnOrder = ColumOrderField + " DESC";
                    if (piHeaderName != null)
                        piHeaderName.SetValue(this, piHeaderName.GetValue(this) + " ▼");
                } 
                else
                {
                    bColumnOrderBy = true;
                    strColumnOrder = ColumOrderField + " ASC";
                    if (piHeaderName != null)
                        piHeaderName.SetValue(this, piHeaderName.GetValue(this) + " ▲");
                }
            }
            else
            {
                ColumOrderField = e.Column.SortMemberPath.ToLower();
                bColumnOrderBy = true;
                strColumnOrder = ColumOrderField + " ASC";
                if (piHeaderName != null)
                    piHeaderName.SetValue(this, piHeaderName.GetValue(this) + " ▲");
            }

            Search();
        }
    }
}
