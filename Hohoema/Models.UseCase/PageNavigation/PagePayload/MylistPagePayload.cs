﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class MylistPagePayload : PagePayloadBase
    {
        public string Id { get; set; }

        public MylistPagePayload() { }

        public MylistPagePayload(string id)
        {
            Id = id;
        }
    }
}
