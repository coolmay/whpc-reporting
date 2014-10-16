using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class ClusterUtilizationData
    {
        public ClusterUtilizationData(string name, string type, DateTime date, double utilization)
        {
            Name = name;
            Type = type;
            Date = date;
            Utilization = utilization;
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public DateTime Date { get; set; }

        public double Utilization { get; set; }
    }
}