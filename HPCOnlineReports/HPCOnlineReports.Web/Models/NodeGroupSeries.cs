using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace HPCOnlineReports.Web.Models
{
    public class NodeGroupSeries
    {
        private Collection<Collection<CapacityPlanningData>> nodeSeries;
        private Collection<string> nodeNames;
        private Collection<CapacityPlanningData> groupSeries;

        public NodeGroupSeries()
        {
            this.nodeSeries = new Collection<Collection<CapacityPlanningData>>();
            this.nodeNames = new Collection<string>();
            this.groupSeries = null;
        }

        public void AddNodeSeries(Collection<CapacityPlanningData> series)
        {
            Collection<CapacityPlanningData> localseries = series.MakeCopy();
            string nodeName = localseries[0].NodeName;
            int l = 0;

            // Add node series
            for (l = 0; l < this.nodeNames.Count && this.nodeNames[l] != nodeName; l++) ;
            if (l == this.nodeNames.Count)
            {
                this.nodeSeries.Add(localseries);
                this.nodeNames.Add(nodeName);
            }
            else
            {
                for (int k = 0; k < this.nodeSeries[l].Count && k < localseries.Count; ++k)
                {
                    if (nodeSeries[l][k].Time == localseries[k].Time)
                    {
                        nodeSeries[l][k].Value += localseries[k].Value;
                    }
                }
            }

            // Calculate group series
            if (this.groupSeries == null)
            {
                this.groupSeries = localseries.MakeCopy();
            }
            else
            {
                for (int k = 0; k < this.groupSeries.Count && k < localseries.Count; ++k)
                {
                    if (groupSeries[k].Time == localseries[k].Time)
                    {
                        groupSeries[k].Value += localseries[k].Value;
                    }
                }
            }
        }

        public Collection<Collection<CapacityPlanningData>> GetNodeSeries()
        {
            return this.nodeSeries;
        }

        public Collection<CapacityPlanningData> GetGroupSeries()
        {
            return this.groupSeries;
        }

        public Collection<string> GetNodeNames()
        {
            return this.nodeNames;
        }
    }
}