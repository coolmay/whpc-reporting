using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class ChargeBackJob
    {
        public ChargeBackJob(int jobId, string jobname, Single cost)
        {
            this.JobId = jobId;
            this.JobName = jobname;
            this.Cost = cost;
        }

        public int JobId { get; set; }

        public string JobName { get; set; }

        public Single Cost { get; set; }
    }
}