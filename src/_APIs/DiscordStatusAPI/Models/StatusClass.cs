﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DiscordStatusAPI.Models
{
    public partial class StatusClass
    {
        [JsonProperty("indicator")]
        public string Indicator { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
