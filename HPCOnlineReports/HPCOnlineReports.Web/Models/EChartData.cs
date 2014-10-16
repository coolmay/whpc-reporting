using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class EChartData
    {
        public string Id { get; set; }

        public string Title { get;set; }

        public string Subtitle { get;set; } 

        public string[] Legend { get;set; }
         
        public string[] XAxisData { get;set; }

        public SeriesData[] YAxisData { get;set; }

        public bool IsSmallSize { get;set; }

    }

    public class SeriesData
    {
        public string name { get;set; }

        public string type { get;set; }

        public double[] data { get;set; }
    }
}