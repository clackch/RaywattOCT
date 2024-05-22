using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RaywattApp.Common.Behaviors
{
    public class DataGridLoadedBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += DataGrid_Loaded;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= DataGrid_Loaded;
            base.OnDetaching();
        }

        private void DataGrid_Loaded(object sender, System.EventArgs e)
        {
            var dg = sender as DataGrid;
            if(dg != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dg.SelectedItem != null)
                    {
                        dg.ScrollIntoView(dg.SelectedItem);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}
