﻿using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class SecondaryViewNavigatePayload : PagePayloadBase
    {
        public string ContentId { get; set; }
        public string Title { get; set; }
        public string ContentType { get; set; }
    }
}
