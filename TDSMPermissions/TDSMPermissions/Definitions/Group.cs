﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Terraria_Server.Misc;

namespace TDSMPermissions.Definitions
{
    class Group
    {
        public struct _GroupInfo
        {
            public bool Default;
            public string Prefix;
            public string Suffix;
            public Color color;
        }

        public List<String> Inherits;
        public _GroupInfo GroupInfo;
        public string Name;
        public Dictionary<String, Boolean> permissions;

        public Group(string name)
        {
            Name = name;
            Inherits = new List<String>();
            permissions = new Dictionary<String, Boolean>();
            GroupInfo.Prefix = null;
            GroupInfo.Suffix = null;
            GroupInfo.color = ChatColor.Aqua;
            GroupInfo.Default = false;
        }

        public void SetGroupInfo(bool Default, string prefix, string suffix, Color color)
        {
            GroupInfo.Default = Default;
            GroupInfo.Prefix = prefix;
            GroupInfo.Suffix = suffix;
            GroupInfo.color = color;
        }
    }
}