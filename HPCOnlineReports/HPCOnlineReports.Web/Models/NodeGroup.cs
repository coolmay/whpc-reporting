using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class NodeGroup
    {
        public NodeGroup(string groupname)
        {
            this.GroupName = groupname;
        }

        public string GroupName { get; set; }
    }
}