using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace WPTaskClient
{
    class ControlDisabler : IDisposable
    {
        private Control control;
        public ControlDisabler(Control control)
        {
            this.control = control;
            this.control.IsEnabled = false;
        }
        public void Dispose() => this.control.IsEnabled = true;
    }
}
