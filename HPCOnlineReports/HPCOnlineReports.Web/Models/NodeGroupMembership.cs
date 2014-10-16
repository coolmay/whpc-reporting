using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class NodeGroupMembership
    {
        public NodeGroupMembership(string nodename, string groupname)
        {
            this.NodeName = nodename;
            this.GroupName = groupname;
        }

        public string NodeName { get; set; }

        public string GroupName { get; set; }
    }
}