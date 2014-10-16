using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class Node
    {
        public Node(string nodename, string nodesize)
        {
            this.NodeName = nodename;
            this.NodeSize = nodesize;
        }

        public string NodeName { get; set; }

        public string NodeSize { get; set; }
    }
}