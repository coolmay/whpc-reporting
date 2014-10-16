using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.ObjectModel;
using HPCOnlineReports.Web.Utils;
using HPCOnlineReports.Web.Models;

namespace HPCOnlineReports.Web.Controllers
{
    public class ReportsController : Controller
    {
        private DataService ds = new DataService();

        #region Capacity

        public ActionResult Capacity(string c, string s, string e)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime start = today.AddDays(-1);
            DateTime end = today;

            try
            {
                if (!string.IsNullOrEmpty(s))
                {
                    start = Convert.ToDateTime(s);
                }

                if (!string.IsNullOrEmpty(e))
                {
                    end = Convert.ToDateTime(e);
                }
            }
            catch
            { }

            ViewBag.Clusters = CreateClusterSelectList(ds.GetClusters(), c);
            ViewBag.StartTime = start.ToShortDateString();
            ViewBag.EndTime = end.ToShortDateString();

            return View();
        }

        public string GetCapacityCharts(string c, string s, string e)
        {
            List<EChartData> charts = new List<EChartData>();

            Collection<NodeGroupMembership> nodegroupmapping = ds.GetNodeGroupMemberships(c);
            Collection<Collection<CapacityPlanningData>> resultcache = ds.GetCapacityPlanningData(c, s, e);

            // Collect information on group-basis
            Collection<string> netNodeGroupNames = new Collection<string>();
            Collection<string> cpuNodeGroupNames = new Collection<string>();
            Collection<string> memNodeGroupNames = new Collection<string>();
            Collection<NodeGroupSeries> cpuAreaSeriesPerNodeGroup = new Collection<NodeGroupSeries>();
            Collection<NodeGroupSeries> memAreaSeriesPerNodeGroup = new Collection<NodeGroupSeries>();
            Collection<NodeGroupSeries> netAreaSeriesPerNodeGroup = new Collection<NodeGroupSeries>();
            for (int i = 0; i < resultcache.Count; ++i)
            {
                Collection<string> groupnames = this.GetGroupNames(resultcache[i][0].NodeName, nodegroupmapping);

                if (resultcache[i][0].Metric.Contains("HPCCpuUsage"))
                {
                    for (int k = 0; k < groupnames.Count; k++)
                    {
                        string name = groupnames[k];
                        int l = 0;
                        for (l = 0; l < cpuNodeGroupNames.Count && cpuNodeGroupNames[l] != name; l++) ;
                        if (l == cpuNodeGroupNames.Count)
                        {
                            cpuNodeGroupNames.Add(name);
                            cpuAreaSeriesPerNodeGroup.Add(new NodeGroupSeries());
                        }
                        cpuAreaSeriesPerNodeGroup[l].AddNodeSeries(resultcache[i]);
                    }
                }
                else if (resultcache[i][0].Metric.Contains("HPCPhysicalMem"))
                {
                    for (int k = 0; k < groupnames.Count; k++)
                    {
                        string name = groupnames[k];
                        int l = 0;
                        for (l = 0; l < memNodeGroupNames.Count && memNodeGroupNames[l] != name; l++) ;
                        if (l == memNodeGroupNames.Count)
                        {
                            memNodeGroupNames.Add(name);
                            memAreaSeriesPerNodeGroup.Add(new NodeGroupSeries());
                        }
                        memAreaSeriesPerNodeGroup[l].AddNodeSeries(resultcache[i]);
                    }
                }
                else if (resultcache[i][0].Metric.Contains("HPCNetwork"))
                {
                    for (int k = 0; k < groupnames.Count; k++)
                    {
                        string name = groupnames[k];
                        int l = 0;
                        for (l = 0; l < netNodeGroupNames.Count && netNodeGroupNames[l] != name; l++) ;
                        if (l == netNodeGroupNames.Count)
                        {
                            netNodeGroupNames.Add(name);
                            netAreaSeriesPerNodeGroup.Add(new NodeGroupSeries());
                        }
                        netAreaSeriesPerNodeGroup[l].AddNodeSeries(resultcache[i]);
                    }
                }
            }

            // Collect overall information
            Collection<Collection<CapacityPlanningData>> netAreaSeries = new Collection<Collection<CapacityPlanningData>>();
            Collection<Collection<CapacityPlanningData>> cpuAreaSeries = new Collection<Collection<CapacityPlanningData>>();
            Collection<Collection<CapacityPlanningData>> memAreaSeries = new Collection<Collection<CapacityPlanningData>>();
            // Displaying Cpu usage chart based on node group
            for (int i = 0; i < cpuAreaSeriesPerNodeGroup.Count; ++i)
            {
                cpuAreaSeries.Add(cpuAreaSeriesPerNodeGroup[i].GetGroupSeries());
            }

            // Displaying memory usage chart based on node group
            for (int i = 0; i < memAreaSeriesPerNodeGroup.Count; ++i)
            {
                memAreaSeries.Add(memAreaSeriesPerNodeGroup[i].GetGroupSeries());
            }

            // Displaying network usage chart based on node group
            for (int i = 0; i < netAreaSeriesPerNodeGroup.Count; ++i)
            {
                netAreaSeries.Add(netAreaSeriesPerNodeGroup[i].GetGroupSeries());
            }

            // Create charts
            EChartData chart = null;

            // Cpu usage chart
            chart = CreateCapacityChart("cpu_usage_all", "CPU Usage (%)", "Overall", cpuNodeGroupNames, cpuAreaSeries);
            if(chart != null) charts.Add(chart);
            for (int i = 0; i < cpuAreaSeriesPerNodeGroup.Count; ++i)
            {
                Collection<Collection<CapacityPlanningData>> seriesOfNodeGroup = cpuAreaSeriesPerNodeGroup[i].GetNodeSeries();
                Collection<string> nodeNames = cpuAreaSeriesPerNodeGroup[i].GetNodeNames();
                chart = CreateCapacityChart("cpu_usage_" + cpuNodeGroupNames[i].ToLower(), "CPU Usage (%)", cpuNodeGroupNames[i], nodeNames, seriesOfNodeGroup, true);
                if(chart != null) charts.Add(chart);
            }

            // Memeory usage chart
            chart = CreateCapacityChart("memory_usage_all", "Memory Usage (M)", "Overall", memNodeGroupNames, memAreaSeries);
            if(chart != null) charts.Add(chart);
            for (int i = 0; i < memAreaSeriesPerNodeGroup.Count; ++i)
            {
                Collection<Collection<CapacityPlanningData>> seriesOfNodeGroup = memAreaSeriesPerNodeGroup[i].GetNodeSeries();
                Collection<string> nodeNames = memAreaSeriesPerNodeGroup[i].GetNodeNames();
                chart = CreateCapacityChart("memory_usage_" + memNodeGroupNames[i].ToLower(), "Memory Usage (M)", memNodeGroupNames[i], nodeNames, seriesOfNodeGroup, true);
                if(chart != null) charts.Add(chart);
            }

            // Network usage chart
            chart = CreateCapacityChart("network_usage_all", "Network Usage (Bytes/s)", "Overall", netNodeGroupNames, netAreaSeries);
            if(chart != null) charts.Add(chart);
            for (int i = 0; i < netAreaSeriesPerNodeGroup.Count; ++i)
            {
                Collection<Collection<CapacityPlanningData>> seriesOfNodeGroup = netAreaSeriesPerNodeGroup[i].GetNodeSeries();
                Collection<string> nodeNames = netAreaSeriesPerNodeGroup[i].GetNodeNames();
                chart = CreateCapacityChart("network_usage_" + netNodeGroupNames[i].ToLower(), "Network Usage (Bytes/s)", netNodeGroupNames[i], nodeNames, seriesOfNodeGroup, true);
                if(chart != null) charts.Add(chart);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(charts);
        }

        private EChartData CreateCapacityChart(string id, string title, string subtitle, Collection<string> legend, Collection<Collection<CapacityPlanningData>> series, bool isSmallSize = false)
        {
            EChartData chart = null;
            List<string> xAxisData = new List<string>();
            List<SeriesData> seriesData = new List<SeriesData>();

            if (series.Count > 0)
            {
                for (int i = 0; i < series[0].Count; i++)
                {
                    string x = series[0][i].Time.ToString("MM-dd HH:mm");
                    xAxisData.Add(x);
                }

                for (int i = 0; i < series.Count; i++)
                {
                    SeriesData s = new SeriesData();
                    s.name = legend[i];
                    s.type = "line";

                    List<double> yAxisData = new List<double>();
                    for (int j = 0; j < series[i].Count; j++)
                    {
                        double y = series[i][j].Value;
                        yAxisData.Add(y);
                    }
                    s.data = yAxisData.ToArray();

                    seriesData.Add(s);
                }

                chart = new EChartData();
                chart.Id = id;
                chart.Title = title;
                chart.Subtitle = subtitle;
                chart.Legend = legend.ToArray();
                chart.XAxisData = xAxisData.ToArray();
                chart.YAxisData = seriesData.ToArray();
                chart.IsSmallSize = isSmallSize;
            }

            return chart;
        }

        #endregion

        #region ChargeBack

        public ActionResult ChargeBack(string c, string t, string d)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime date = today.AddDays(-1);
            string type = "daily";

            try
            {
                if (!string.IsNullOrEmpty(t))
                {
                    type = t.Trim();
                }
                if (!string.IsNullOrEmpty(d))
                {
                    date = Convert.ToDateTime(d);
                }
            }
            catch
            { }

            ViewBag.Clusters = CreateClusterSelectList(ds.GetClusters(), c);
            ViewBag.ReportType = type;
            ViewBag.StartDate = date.ToShortDateString();

            return View();
        }

        public string GetChargeBackReports(string c, string t, string d)
        {
            DateTime startDate = Convert.ToDateTime(d);
            DateTime endDate = startDate;

            switch (t)
            {
                case "daily":
                    endDate = startDate.AddDays(1);
                    break;
                case "weekly":
                    endDate = startDate.AddDays(7);
                    break;
                case "monthly":
                    endDate = startDate.AddMonths(1);
                    break;
                default:
                    break;
            }

            Collection<ChargeBackOwner> owners = ds.GetChargeBackOwnerData(c, startDate, endDate);

            return Newtonsoft.Json.JsonConvert.SerializeObject(owners);
        }

        public string GetChargeBackJobData(string c, string t, string d, string o)
        {
            DateTime startDate = Convert.ToDateTime(d);
            DateTime endDate = startDate;

            switch (t)
            {
                case "daily":
                    endDate = startDate.AddDays(1);
                    break;
                case "weekly":
                    endDate = startDate.AddDays(7);
                    break;
                case "monthly":
                    endDate = startDate.AddMonths(1);
                    break;
                default:
                    break;
            }

            Collection<ChargeBackJob> jobs = ds.GetChargeBackJobData(c, startDate, endDate, o);
            return Newtonsoft.Json.JsonConvert.SerializeObject(jobs);
        }

        public string GetChargeBackJobDetails(string c, string t, string d, string o, int j)
        {
            DateTime startDate = Convert.ToDateTime(d);
            DateTime endDate = startDate;

            switch (t)
            {
                case "daily":
                    endDate = startDate.AddDays(1);
                    break;
                case "weekly":
                    endDate = startDate.AddDays(7);
                    break;
                case "monthly":
                    endDate = startDate.AddMonths(1);
                    break;
                default:
                    break;
            }

            Collection<ChargeBackJobDetails> jobdetails = ds.GetChargeBackJobDetails(c, startDate, endDate, o, j);
            return Newtonsoft.Json.JsonConvert.SerializeObject(jobdetails);
        }

        #endregion


        #region ClusterUtilization

        public ActionResult ClusterUtilization(string c, string s, string e)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime start = today.AddDays(-1);
            DateTime end = today;

            try
            {
                if (!string.IsNullOrEmpty(s))
                {
                    start = Convert.ToDateTime(s);
                }

                if (!string.IsNullOrEmpty(e))
                {
                    end = Convert.ToDateTime(e);
                }
            }
            catch
            { }

            ViewBag.Clusters = CreateClusterSelectList(ds.GetClusters(), c);
            ViewBag.StartTime = start.ToShortDateString();
            ViewBag.EndTime = end.ToShortDateString();

            return View();
        }

        public string GetClusterUtilizationCharts(string c, string s, string e)
        {
            List<EChartData> charts = new List<EChartData>();
            DateTime startDate = Convert.ToDateTime(s);
            DateTime endDate = Convert.ToDateTime(e);

            Collection<NodeGroup> nodegroups = ds.GetNodeGroups(c);
            Collection<Node> nodes = ds.GetNodes(c);
            Collection<NodeGroupMembership> nodegroupmapping = ds.GetNodeGroupMemberships(c);

            // Collect all node information
            Dictionary<string, Collection<ClusterUtilizationData>> totalnodesdata = new Dictionary<string, Collection<ClusterUtilizationData>>();
            Dictionary<string, Collection<ClusterUtilizationData>> availablenodesdata = new Dictionary<string, Collection<ClusterUtilizationData>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string nodename = nodes[i].NodeName;

                Collection<ClusterUtilizationData> totalnode = ds.GetClusterUtilization_Total_Node(c, startDate, endDate, nodename);
                totalnodesdata.Add(nodename, totalnode);

                Collection<ClusterUtilizationData> availablenode = ds.GetClusterUtilization_Available_Node(c, startDate, endDate, nodename);
                availablenodesdata.Add(nodename, availablenode);
            }

            // Create charts
            Collection<string> legend = new Collection<string>();
            Collection<Collection<ClusterUtilizationData>> series = new Collection<Collection<ClusterUtilizationData>>();
            EChartData chart = null;

            // Overall Cluster Utilization chart
            legend.Clear();
            series.Clear();
            Collection<ClusterUtilizationData> totalall = ds.GetClusterUtilization_Total_All(c, startDate, endDate);
            if (totalall.Count > 0)
            {
                legend.Add(totalall[0].Type);
                series.Add(totalall);
            }
            Collection<ClusterUtilizationData> availableall = ds.GetClusterUtilization_Available_All(c, startDate, endDate);
            if (availableall.Count > 0)
            {
                legend.Add(availableall[0].Type);
                series.Add(availableall);
            }
            chart = CreateClusterUtilizationChart("utilization_chart_overall", "Cluster Utilization (%)", "Overall", legend, series, false);
            if(chart != null) charts.Add(chart);

            // Cluster Utilization (Total-Time-Based) for all node groups
            legend.Clear();
            series.Clear();
            for (int i = 0; i < nodegroups.Count; i++)
            {
                string groupname = nodegroups[i].GroupName;
                Collection<ClusterUtilizationData> totalnodegroups = ds.GetClusterUtilization_Total_NodeGroup(c, startDate, endDate, groupname);
                if (totalnodegroups.Count > 0)
                {
                    legend.Add(groupname);
                    series.Add(totalnodegroups);
                }
            }
            chart = CreateClusterUtilizationChart("utilization_chart_total_nodegroups", "Cluster Utilization (%)", "(Total-Time-Based) for all node groups", legend, series, false);
            if(chart != null) charts.Add(chart);

            // Cluster Utilization (Total-Time-Based) for all nodes in a node group
            for (int i = 0; i < legend.Count; i++)
            {
                Collection<string> legend1 = new Collection<string>();
                Collection<Collection<ClusterUtilizationData>> series1 = new Collection<Collection<ClusterUtilizationData>>();

                string groupname = legend[i];
                Collection<string> nodenames = GetNodeNames(groupname, nodegroupmapping);
                for (int j = 0; j < nodenames.Count; j++)
                {
                    string nodename = nodenames[j];
                    Collection<ClusterUtilizationData> totalnode = totalnodesdata[nodename];
                    if (totalnode.Count > 0)
                    {
                        legend1.Add(nodename);
                        series1.Add(totalnode);
                    }
                }
                chart = CreateClusterUtilizationChart("utilization_chart_total_nodeingroup_" + groupname, "Cluster Utilization (%)", "(Total-Time-Based) for all nodes in " + groupname, legend1, series1, true);
                if(chart != null) charts.Add(chart);
            }

            // Cluster Utilization (Available-Time-Based) for all node groups
            legend.Clear();
            series.Clear();
            for (int i = 0; i < nodegroups.Count; i++)
            {
                string groupname = nodegroups[i].GroupName;
                Collection<ClusterUtilizationData> totalnodegroups = ds.GetClusterUtilization_Available_NodeGroup(c, startDate, endDate, groupname);
                if (totalnodegroups.Count > 0)
                {
                    legend.Add(groupname);
                    series.Add(totalnodegroups);
                }
            }
            chart = CreateClusterUtilizationChart("utilization_chart_available_nodegroups", "Cluster Utilization (%)", "(Available-Time-Based) for all node groups", legend, series, false);
            if(chart != null) charts.Add(chart);

            // Cluster Utilization (Available-Time-Based) for all nodes in a node group
            for (int i = 0; i < legend.Count; i++)
            {
                Collection<string> legend1 = new Collection<string>();
                Collection<Collection<ClusterUtilizationData>> series1 = new Collection<Collection<ClusterUtilizationData>>();

                string groupname = legend[i];
                Collection<string> nodenames = GetNodeNames(groupname, nodegroupmapping);
                for (int j = 0; j < nodenames.Count; j++)
                {
                    string nodename = nodenames[j];
                    Collection<ClusterUtilizationData> availablenode = availablenodesdata[nodename];
                    if (availablenode.Count > 0)
                    {
                        legend1.Add(nodename);
                        series1.Add(availablenode);
                    }
                }
                chart = CreateClusterUtilizationChart("utilization_chart_available_nodeingroup_" + groupname, "Cluster Utilization (%)", "(Available-Time-Based) for all nodes in " + groupname, legend1, series1, true);
                if(chart != null) charts.Add(chart);
            }

            // Cluster Utilization for one node
            legend.Clear();
            series.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                legend.Clear();
                series.Clear();

                string nodename = nodes[i].NodeName;
                Collection<ClusterUtilizationData> totalone = totalnodesdata[nodename];
                if (totalone.Count > 0)
                {
                    legend.Add(totalone[0].Type);
                    series.Add(totalone);
                }
                Collection<ClusterUtilizationData> availableone = availablenodesdata[nodename];
                if (availableone.Count > 0)
                {
                    legend.Add(availableone[0].Type);
                    series.Add(availableone);
                }
                chart = CreateClusterUtilizationChart("utilization_chart_onenode_" + nodename, "Cluster Utilization (%)", "for " + nodename, legend, series, false);
                if(chart != null) charts.Add(chart);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(charts);
        }

        private EChartData CreateClusterUtilizationChart(string id, string title, string subtitle, Collection<string> legend, Collection<Collection<ClusterUtilizationData>> series, bool isSmallSize = false)
        {
            EChartData chart = null;
            List<string> xAxisData = new List<string>();
            List<SeriesData> seriesData = new List<SeriesData>();

            if (series.Count > 0)
            {
                for (int i = 0; i < series[0].Count; i++)
                {
                    string x = series[0][i].Date.ToString("MM-dd");
                    xAxisData.Add(x);
                }

                for (int i = 0; i < series.Count; i++)
                {
                    SeriesData s = new SeriesData();
                    s.name = legend[i];
                    s.type = "line";

                    List<double> yAxisData = new List<double>();
                    for (int j = 0; j < series[i].Count; j++)
                    {
                        double y = series[i][j].Utilization;
                        yAxisData.Add(y);
                    }
                    s.data = yAxisData.ToArray();

                    seriesData.Add(s);
                }

                chart = new EChartData();
                chart.Id = id;
                chart.Title = title;
                chart.Subtitle = subtitle;
                chart.Legend = legend.ToArray();
                chart.XAxisData = xAxisData.ToArray();
                chart.YAxisData = seriesData.ToArray();
                chart.IsSmallSize = isSmallSize;
            }

            return chart;
        }

        #endregion

        #region Utils

        private Collection<string> GetGroupNames(string nodename, Collection<NodeGroupMembership> nodegroupmapping)
        {
            Collection<string> groupnames = new Collection<string>();
            foreach (NodeGroupMembership mapping in nodegroupmapping)
            {
                if (mapping.NodeName == nodename)
                    groupnames.Add(mapping.GroupName);
            }
            return groupnames;
        }

        private Collection<string> GetNodeNames(string groupname, Collection<NodeGroupMembership> nodegroupmapping)
        {
            Collection<string> nodenames = new Collection<string>();
            foreach (NodeGroupMembership mapping in nodegroupmapping)
            {
                if (mapping.GroupName == groupname)
                    nodenames.Add(mapping.NodeName);
            }
            return nodenames;
        }

        private List<SelectListItem>  CreateClusterSelectList (Collection<Cluster> clusters, string currentClusterId)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            foreach(Cluster c in clusters)
            {
                items.Add(new SelectListItem { Text = c.ClusterName, Value = c.ClusterId, Selected = (c.ClusterId == currentClusterId) });
            }
            return items;
        }
        #endregion
    }
}
