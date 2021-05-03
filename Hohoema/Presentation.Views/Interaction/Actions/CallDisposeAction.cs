using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.Interaction.Actions
{
    public sealed class CallDisposeAction : DependencyObject, IAction
    {
        public IDisposable Target
        {
            get { return (IDisposable)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(IDisposable), typeof(CallDisposeAction), new PropertyMetadata(null));


        public object Execute(object sender, object parameter)
        {
            Target?.Dispose();
            return Target != null;
        }
    }
}
