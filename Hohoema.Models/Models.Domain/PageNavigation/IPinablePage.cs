﻿using Hohoema.Models.Domain.Pins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.PageNavigation
{
    public interface IPinablePage
    {
        HohoemaPin GetPin();
    }
}
