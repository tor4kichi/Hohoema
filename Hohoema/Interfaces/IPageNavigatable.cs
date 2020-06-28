﻿using Hohoema.Services;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Interfaces
{
    public interface IPageNavigatable
    {
        HohoemaPageType PageType { get; }
        INavigationParameters Parameter { get; }
    }
}
