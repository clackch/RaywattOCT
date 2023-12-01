using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaywattApp.Models
{
    internal partial class CoRegistration : ObservableObject
    {
        [ObservableProperty]
        private string _id;

        [ObservableProperty]
        private string _trackPoint;
    }
}
