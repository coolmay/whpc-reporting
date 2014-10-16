using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class CapacityPlanningData
    {
        public CapacityPlanningData()
        {
        }

        public CapacityPlanningData(string nodename, string metric, string counter, Single value, DateTime time, string type)
        {
            this.NodeName = nodename;
            this.Metric = metric;
            this.Counter = counter;
            this.Value = value;
            this.Time = time;
            this.Type = type;
        }

        public string NodeName { get; set; }

        public string Metric { get; set; }

        public string Counter { get; set; }

        public System.Single Value { get; set; }

        public DateTime Time { get; set; }

        public string Type { get; set; }
    }

    public static class Extensions
    {
        public static CapacityPlanningData MakeCopy(this CapacityPlanningData data)
        {
            CapacityPlanningData copy = new CapacityPlanningData();
            copy.Counter = data.Counter;
            copy.Metric = data.Metric;
            copy.NodeName = data.NodeName;
            copy.Time = data.Time;
            copy.Type = data.Type;
            copy.Value = data.Value;
            return copy;
        }

        public static Collection<CapacityPlanningData> MakeCopy(this Collection<CapacityPlanningData> data)
        {
            Collection<CapacityPlanningData> localseries = new Collection<CapacityPlanningData>();
            foreach (CapacityPlanningData cachdata in data)
            {
                CapacityPlanningData localdata = cachdata.MakeCopy();
                localseries.Add(localdata);
            }

            return localseries;
        }
    }
}