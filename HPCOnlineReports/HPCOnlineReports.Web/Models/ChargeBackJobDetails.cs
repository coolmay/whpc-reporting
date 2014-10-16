using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class ChargeBackJobDetails
    {
        public ChargeBackJobDetails(string node, string group, string nodesize,
            string duration, string rate, string cost)
        {
            this.NodeName = node;
            this.NodeRole = group;
            this.NodeSize = nodesize;
            this.Duration = duration;
            this.Rate = rate;
            this.Cost = cost;
        }

        public string NodeName { get; set; }

        public string NodeRole { get; set; }

        public string NodeSize { get;set; }

        public string Duration { get; set; }

        public string Rate { get; set; }

        public string Cost { get; set; }
    }
}