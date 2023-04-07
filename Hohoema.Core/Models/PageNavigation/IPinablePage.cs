using Hohoema.Models.Pins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.PageNavigation
{
    public interface IPinablePage
    {
        HohoemaPin GetPin();
    }
}
