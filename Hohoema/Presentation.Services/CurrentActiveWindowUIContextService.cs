using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Services
{
    public sealed class CurrentActiveWindowUIContextService
    {
        public UIContext UIContext { get; private set; }

        public XamlRoot XamlRoot { get; private set; }

        public static void SetUIContext(CurrentActiveWindowUIContextService service, UIContext uIContext, XamlRoot xamlRoot)
        {
            service.UIContext = uIContext;
            service.XamlRoot = xamlRoot;
        }
    }
}
