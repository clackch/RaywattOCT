using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public partial class Bookmark : ObservableObject
    {
        [ObservableProperty]
        private int frameNumber;

        [ObservableProperty]
        private double longitudeX;
    }
}
