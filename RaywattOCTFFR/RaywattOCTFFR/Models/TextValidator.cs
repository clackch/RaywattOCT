using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.Models
{
    public partial class TextValidator : ObservableObject
    {
        private string _text = "";
        public string Text
        {
            get{ return _text; }
            set
            {
                if (typeNumber && !string.IsNullOrEmpty(value) && !CommonUtil.ValidateNumber(value))
                    return;

                _text = value;
                OnPropertyChanged(nameof(Text));
                Msg = "";
            }

        }

        [ObservableProperty]
        private string _msg;

        private bool typeNumber;

        public void SetSpecialType(int type)
        {
            switch (type)
            {
                case 1://Number
                    typeNumber = true;
                    break;
                default:
                    break;
            }
        }
    }
}
