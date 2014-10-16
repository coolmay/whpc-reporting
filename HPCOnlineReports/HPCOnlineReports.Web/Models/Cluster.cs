using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class Cluster
    {
        public Cluster(string clusterid, string clustername)
        {
            this.ClusterId = clusterid;
            this.ClusterName = clustername;
        }

        public string ClusterId { get; set; }
        public string ClusterName { get; set; }
    }
}