using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Configuration;
using HPCOnlineReports.Web.Models;

namespace HPCOnlineReports.Web.Utils
{
    public class DataService
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ToString();
        private static double numofbins = 24;   // number of values to display on each chart of capacity planning reports

        /// <summary>
        /// Update database connection for the service
        /// </summary>
        /// <param name="connstr">SQL db connection string to the reporting database</param>
        public bool UpdateConnectionSetting(string connstr)
        {
            try
            {
                connectionString = connstr;
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get all metric value history data during the specified time period
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>A set of metric value history records</returns>
        public Collection<Collection<CapacityPlanningData>> GetCapacityPlanningData(string clusterId, string startDateText, string endDateText)
        {
            DateTime startDate = Convert.ToDateTime(startDateText);
            DateTime endDate = Convert.ToDateTime(endDateText);

            string commandText;
            Collection<Collection<CapacityPlanningData>> series = new Collection<Collection<CapacityPlanningData>>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // obtain min and max time of records in database
                    string commandText1 = string.Format(
                        @"SELECT MIN([Time])
                        FROM [MetricValueHistory]
                        WHERE [Time] BETWEEN '{0}' AND '{1}'
                            AND [ClusterId] = '{2}'",
                        startDate.ToString(), endDate.ToString(), clusterId);
                    string commandText2 = string.Format(
                        @"SELECT MAX([Time])
                        FROM [MetricValueHistory]
                        WHERE [Time] BETWEEN '{0}' AND '{1}'
                            AND [ClusterId] = '{2}'",
                        startDate.ToString(), endDate.ToString(), clusterId);

                    DateTime? starttime = null;
                    DateTime? endtime = null;
                    using (SqlCommand command = new SqlCommand(commandText1, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tmp = reader[0].ToString();
                                if (string.IsNullOrEmpty(tmp))
                                    continue;
                                starttime = Convert.ToDateTime(reader[0].ToString());
                            }
                        }
                    }
                    using (SqlCommand command = new SqlCommand(commandText2, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tmp = reader[0].ToString();
                                if (string.IsNullOrEmpty(tmp))
                                    continue;
                                endtime = Convert.ToDateTime(tmp);
                            }
                        }
                    }
                    if (starttime == null || endtime == null)
                    {
                        return series;
                    }

                    // obtain each bin value
                    Collection<CapacityPlanningData> result = new Collection<CapacityPlanningData>();
                    TimeSpan interval = (DateTime)endtime - (DateTime)starttime;
                    TimeSpan step = TimeSpan.FromTicks(interval.Ticks / Convert.ToInt32(numofbins));
                    for (DateTime start = (DateTime)starttime; start < (DateTime)endtime; start += step)
                    {
                        DateTime end = start + step;
                        if (end > (DateTime)endtime)
                            end = (DateTime)endtime;

                        //commandText = @"SELECT m.NodeName, m.Metric, m.[Counter], AVG(m.Value) AS Value, '" + start.Month.ToString() + "/" + start.Day.ToString() + "-" + start.ToShortTimeString() + @"' AS [Time], n.[Type]
                        commandText = string.Format(
                            @"SELECT m.[NodeName], m.[Metric], m.[Counter], AVG(m.[Value]) AS [Value], '{0}' AS [Time], n.[Type]
                            FROM [MetricValueHistory] AS m
                            LEFT OUTER JOIN [Network] AS n 
                                ON m.[Counter] = n.[Name] AND m.[ClusterId] = n.[ClusterId]
                            WHERE (m.[Metric] = 'HPCCpuUsage' OR m.[Metric] = 'HPCPhysicalMem' OR m.[Metric] = 'HPCNetwork')
                                AND m.[Time] BETWEEN '{1}' AND '{2}'
                                AND m.[ClusterId] = '{3}'
                            GROUP BY m.[NodeName], m.[Metric], m.[Counter], n.[Type]",
                            start.ToString(), start.ToString(), end.ToString(), clusterId);

                        using (SqlCommand command = new SqlCommand(commandText, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    result.Add(new CapacityPlanningData(reader[0].ToString(), reader[1].ToString(), reader[2].ToString(), Single.Parse(reader[3].ToString()), Convert.ToDateTime(reader[4].ToString()), reader[5].ToString()));
                                }
                            }
                        }
                    }

                    connection.Close();

                    for (int i = 0; i < result.Count; ++i)
                    {
                        int j;
                        for (j = 0; j < series.Count; ++j)
                        {
                            if (result[i].NodeName.CompareTo(series[j][0].NodeName) == 0 && result[i].Metric.CompareTo(series[j][0].Metric) == 0 && result[i].Counter.CompareTo(series[j][0].Counter) == 0)
                                break;
                        }

                        if (j == series.Count)
                        {
                            series.Add(new Collection<CapacityPlanningData>());
                        }
                        series[j].Add(result[i]);
                    }
                }
                return series;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get the charge back data
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>A set of charge back values grouped by owners</returns>
        public Collection<ChargeBackOwner> GetChargeBackOwnerData(string clusterId, DateTime startDate, DateTime endDate)
        {
            string commandText;
            Collection<ChargeBackOwner> result = new Collection<ChargeBackOwner>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    commandText = string.Format(
                        @"SELECT [Owner], SUM([Rate] * T.[Duration]) AS [Cost]
                        FROM [dbo].[Node] AS N
                        INNER JOIN (
	                        SELECT JH.[Owner], AH.[NodeName], SUM(CAST(DATEDIFF(MILLISECOND, AH.[StartTime], AH.[EndTime]) AS REAL) / 3600000.0) AS [Duration], JH.[ClusterId]
		                    FROM (
			                    SELECT [Owner], [JobID], [RequeueID], [ClusterId]
			                    FROM [dbo].[JobHistory]
			                    WHERE [EventTime] >= '{0}' AND [EventTime] < '{1}' AND [ClusterId] = '{2}'
		                    ) AS JH
                            INNER JOIN [dbo].[AllocationHistory] AS AH 
		                        ON AH.[JobID] = JH.[JobID]
                                    AND AH.[RequeueID] = JH.[RequeueId] AND AH.[EndTime] IS NOT NULL AND AH.[ClusterId] = JH.[ClusterId]
		                    GROUP BY JH.[ClusterId], JH.[Owner], AH.[NodeName]
                        ) AS T
                            ON T.[NodeName] = N.[NodeName] AND T.[ClusterId] = N.[ClusterId]
                        INNER JOIN [ChargeRate] AS CR
                            ON N.[AzureInstanceSize] = CR.[NodeSize] AND CR.[Rate] IS NOT NULL
                        GROUP BY [Owner]
                        ORDER BY [Owner]",
                        startDate.ToShortDateString(), endDate.ToShortDateString(), clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        command.CommandTimeout = 0;
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ChargeBackOwner(reader[0].ToString(), Single.Parse(reader[1].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return result;
        }


        /// <summary>
        /// Get charge back data for an owner
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <param name="owner">owner of the jobs</param>
        /// <returns>A set of charge back values for the owner grouped by jobs</returns>
        public Collection<ChargeBackJob> GetChargeBackJobData(string clusterId, DateTime startDate, DateTime endDate, string owner)
        {

            string commandText;
            Collection<ChargeBackJob> result = new Collection<ChargeBackJob>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    commandText = string.Format(
                        @"SELECT T.[JobID], T.[JobName], SUM([Rate] * T.[Duration]) AS [Cost]
                        FROM [dbo].[Node] AS N
                        INNER JOIN (
                            SELECT JH.[JobID], JH.[JobName], AH.[NodeName], SUM(CAST(DATEDIFF(MILLISECOND, AH.[StartTime], AH.[EndTime]) AS REAL) / 3600000.0) AS [Duration], JH.[ClusterId]
		                    FROM (
			                    SELECT [JobID], [Name] AS [JobName], [RequeueID], [ClusterId]
			                    FROM [dbo].[JobHistory]
			                    WHERE [EventTime] >= '{0}' AND [EventTime] < '{1}' AND [ClusterId] = '{2}' AND [Owner] = '{3}' 
		                    ) AS JH
                            INNER JOIN [dbo].[AllocationHistory] AS AH
                                ON JH.[JobID] = AH.[JobID] 
                                    AND JH.[RequeueId] = AH.[RequeueID] AND AH.[EndTime] IS NOT NULL AND AH.[ClusterId] = JH.[ClusterId]
                            GROUP BY JH.[ClusterId], JH.[JobID], JH.[JobName], AH.[NodeName]
                        ) AS T
                            ON T.[NodeName] = N.[NodeName] AND T.[ClusterId] = N.[ClusterId]
                        INNER JOIN [ChargeRate] AS CR
                            ON N.[AzureInstanceSize] = CR.[NodeSize] AND CR.[Rate] IS NOT NULL
                        GROUP BY T.[JobID], T.[JobName]
                        ORDER BY T.[JobID]",
                        startDate.ToShortDateString(), endDate.ToShortDateString(), clusterId, owner);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ChargeBackJob(Convert.ToInt32(reader[0].ToString()), reader[1].ToString(),
                                    Convert.ToSingle(reader[2].ToString())));
                            }
                        }
                    }

                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get charge back data for a job
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <param name="owner">owner of the jobs</param>
        /// <returns>A set of charge back values and allocated nodes for the owner</returns>
        public Collection<ChargeBackJobDetailedData> GetChargeBackJobDetailedData(string clusterId, DateTime startDate, DateTime endDate, string owner)
        {
            Collection<ChargeBackJobDetailedData> result = new Collection<ChargeBackJobDetailedData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = string.Format(
                        @"SELECT T.[JobID], T.[JobName], N.[NodeName], N.[Role], N.[AzureInstanceSize] AS [NodeSize], T.[Duration], [Rate], (T.[Duration] * [Rate]) AS [Cost]
                        FROM [dbo].[Node] AS N
                        INNER JOIN (
                            SELECT JH.[JobID], JH.[Name] AS [JobName], AH.[NodeName], SUM(CAST(DATEDIFF(MILLISECOND, AH.[StartTime], AH.[EndTime]) AS REAL) / 3600000.0) AS [Duration], JH.[ClusterId]
		                    FROM (
			                    SELECT [Owner], [JobID], [RequeueID], [ClusterId]
			                    FROM [dbo].[JobHistory]
			                    WHERE [EventTime] >= '{0}' AND [EventTime] < '{1}' AND [ClusterId] = '{2}' AND [Owner] = '{3}' 
		                    ) AS JH
		                    INNER JOIN [dbo].[AllocationHistory] AS AH 
                                ON JH.[JobID] = AH.[JobID] 
                                    AND JH.[RequeueId] = AH.[RequeueID] AND AH.[EndTime] IS NOT NULL AND AH.[ClusterId] = JH.[ClusterId]
                            GROUP BY JH.[ClusterId], JH.[JobID], JH.[JobName], AH.[NodeName]
                            ) AS T
                            ON T.[NodeName] = N.[NodeName] AND T.[ClusterId] = N.[ClusterId]
                        INNER JOIN [ChargeRate] AS CR
                            ON N.[AzureInstanceSize] = CR.[NodeSize] AND CR.[Rate] IS NOT NULL
                        ORDER BY T.[JobID]",
                        startDate.ToShortDateString(), endDate.ToShortDateString(), clusterId, owner);


                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ChargeBackJobDetailedData(Convert.ToInt32(reader[0].ToString()),
                                     reader[1].ToString(), reader[2].ToString(), reader[3].ToString(),
                                     reader[4].ToString(), reader[5].ToString(), reader[6].ToString(), reader[7].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get charge back data for a job
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <param name="owner">owner of the jobs</param>
        /// <param name="jobID">ID of the job to query</param>
        /// <returns>A set of charge back values and allocated nodes for the owner and job</returns>
        public Collection<ChargeBackJobDetails> GetChargeBackJobDetails(string clusterId, DateTime startDate, DateTime endDate, string owner, int jobID)
        {
            Collection<ChargeBackJobDetails> result = new Collection<ChargeBackJobDetails>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = string.Format(
                        @"SELECT N.[NodeName], N.[Role], N.[AzureInstanceSize] AS [NodeSize], T.[Duration], [Rate], (T.[Duration] * [Rate]) AS [Cost]
                        FROM [dbo].[Node] AS N
                        INNER JOIN (
                            SELECT AH.[NodeName], SUM(CAST(DATEDIFF(MILLISECOND, AH.[StartTime], AH.[EndTime]) AS REAL) / 3600000.0) AS [Duration], JH.[ClusterId]
		                    FROM (
			                    SELECT [Owner], [JobID], [RequeueID], [ClusterId]
			                    FROM [dbo].[JobHistory]
			                    WHERE [EventTime] >= '{0}' AND [EventTime] < '{1}' AND [ClusterId] = '{2}' AND [Owner] = '{3}' 
                                    AND [JobID] = {4}
		                    ) AS JH
		                    INNER JOIN [dbo].[AllocationHistory] AS AH 
                                ON JH.[JobID] = AH.[JobID] 
                                    AND JH.[RequeueId] = AH.[RequeueID] AND AH.[EndTime] IS NOT NULL AND AH.[ClusterId] = JH.[ClusterId]
                            GROUP BY JH.[ClusterId], AH.[NodeName]
                        ) AS T
                            ON T.[NodeName] = N.[NodeName] AND T.[ClusterId] = N.[ClusterId]
                        INNER JOIN [ChargeRate] AS CR
                            ON N.[AzureInstanceSize] = CR.[NodeSize] AND CR.[Rate] IS NOT NULL
                        ORDER BY N.[NodeName]",
                        startDate.ToShortDateString(), endDate.ToShortDateString(), clusterId, owner, jobID);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ChargeBackJobDetails(reader[0].ToString(),
                                     reader[1].ToString(), reader[2].ToString(), reader[3].ToString(),
                                     reader[4].ToString(), reader[5].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Total time
        /// for All nodes
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Total_All(string clusterId, DateTime startDate, DateTime endDate)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT 'All' AS [Name], 'Total' AS [Type], [Date], AVG(CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreTotalTime] AS REAL)) AS [Utilization]
                        FROM [dbo].[DailyNodeStat]
                        WHERE [Date] BETWEEN '{0}' AND '{1}' 
                            AND [CoreTotalTime] > 0 AND [ClusterId] = '{2}'
                        GROUP BY [Date]",
                        startDate, endDate, clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Total time
        /// for nodes in a group
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Total_NodeGroup(string clusterId, DateTime startDate, DateTime endDate, string groupname)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT '{0}' AS [Name], 'Total' AS [Type], [Date], AVG(CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreTotalTime] AS REAL)) AS [Utilization]
                        FROM [dbo].[DailyNodeStat] AS DNS 
                        INNER JOIN [dbo].[NodeGroupMembership] AS NMS
                            ON DNS.[NodeName] = NMS.[NodeName]
                                AND DNS.[ClusterId] = NMS.[ClusterId]
                        WHERE [Date] BETWEEN '{1}' AND '{2}' 
                            AND [CoreTotalTime] > 0 AND DNS.[ClusterId] = '{3}'
                            AND NMS.[GroupName] = '{0}' 
                        GROUP BY [Date]",
                        groupname, startDate, endDate, clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Total time
        /// for a node
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Total_Node(string clusterId, DateTime startDate, DateTime endDate, string nodename)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT '{0}' AS [Name], 'Total' AS [Type], [Date], CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreTotalTime] AS REAL) AS [Utilization]
                        FROM [dbo].[DailyNodeStat]
                        WHERE [Date] BETWEEN '{1}' AND '{2}' 
                            AND [CoreTotalTime] > 0 AND [ClusterId] = '{3}'
                            AND [NodeName] = '{0}'",
                        nodename, startDate.ToString(), endDate.ToString(), clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Available time
        /// for All nodes
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Available_All(string clusterId, DateTime startDate, DateTime endDate)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT 'All' AS [Name], 'Available' AS [Type], [Date], AVG(CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreAvailableTime] AS REAL)) AS [Utilization]
                        FROM [dbo].[DailyNodeStat]
                        WHERE [Date] BETWEEN '{0}' AND '{1}' 
                            AND [CoreAvailableTime] > 0 AND [ClusterId] = '{2}'
                        GROUP BY [Date]",
                        startDate, endDate, clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Available time
        /// for nodes in a group
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Available_NodeGroup(string clusterId, DateTime startDate, DateTime endDate, string groupname)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT '{0}' AS [Name], 'Available' AS [Type], [Date], AVG(CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreAvailableTime] AS REAL)) AS [Utilization]
                        FROM [dbo].[DailyNodeStat] AS DNS 
                        INNER JOIN [dbo].[NodeGroupMembership] AS NMS
                            ON DNS.[NodeName] = NMS.[NodeName]
                                AND DNS.[ClusterId] = NMS.[ClusterId]
                        WHERE [Date] BETWEEN '{1}' AND '{2}' 
                            AND [CoreTotalTime] > 0 AND DNS.[ClusterId] = '{3}'
                            AND NMS.[GroupName] = '{0}' 
                        GROUP BY [Date]",
                        groupname, startDate, endDate, clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get cluster utilization based on Available time
        /// for a node
        /// </summary>
        /// <param name="startDate">start time to query</param>
        /// <param name="endDate">end time to query</param>
        /// <returns>a series of cluster utilization data</returns>
        public Collection<ClusterUtilizationData> GetClusterUtilization_Available_Node(string clusterId, DateTime startDate, DateTime endDate, string nodename)
        {
            Collection<ClusterUtilizationData> result = new Collection<ClusterUtilizationData>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = String.Format(
                        @"SELECT '{0}' AS [Name], 'Available' AS [Type], [Date], CAST([UtilizedTime] * 100 AS REAL)/CAST([CoreAvailableTime] AS REAL) AS [Utilization]
                        FROM [dbo].[DailyNodeStat]
                        WHERE [Date] BETWEEN '{1}' AND '{2}' 
                            AND [CoreAvailableTime] > 0 AND [ClusterId] = '{3}'
                            AND [NodeName] = '{0}'",
                        nodename, startDate.ToString(), endDate.ToString(), clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new ClusterUtilizationData(
                                    reader[0].ToString(), reader[1].ToString(), Convert.ToDateTime(reader[2].ToString()),
                                    Convert.ToDouble(reader[3].ToString())));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// Get the node-nodegroup mapping information
        /// </summary>
        /// <returns>A set of node-group-membership mappings</returns>
        public Collection<NodeGroupMembership> GetNodeGroupMemberships(string clusterId)
        {
            Collection<NodeGroupMembership> result = new Collection<NodeGroupMembership>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = string.Format(
                        @"SELECT [NodeName], [GroupName] 
                        FROM [dbo].[NodeGroupMembership]
                        WHERE [ClusterId]='{0}'",
                        clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new NodeGroupMembership(reader[0].ToString(),
                                     reader[1].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }

        }


        /// <summary>
        /// Get all HPC node groups
        /// </summary>
        /// <returns>A set of HPC node groups</returns>
        public Collection<NodeGroup> GetNodeGroups(string clusterId)
        {
            Collection<NodeGroup> result = new Collection<NodeGroup>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = string.Format(
                        @"SELECT [GroupName] FROM [dbo].[NodeGroup] WHERE [ClusterId]='{0}'",
                        clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new NodeGroup(reader[0].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }

        }


        /// <summary>
        /// Get all HPC nodes
        /// </summary>
        /// <returns>A set of HPC nodes</returns>
        public Collection<Node> GetNodes(string clusterId)
        {
            Collection<Node> result = new Collection<Node>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = string.Format(
                        @"SELECT [NodeName],[AzureInstanceSize] FROM [dbo].[Node] WHERE [ClusterId]='{0}'",
                        clusterId);

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new Node(reader[0].ToString(), reader[1].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public Collection<Cluster> GetClusters()
        {
            Collection<Cluster> result = new Collection<Cluster>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string commandText = @"SELECT [ClusterId], [ClusterName] FROM [dbo].[Cluster]";

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new Cluster(reader[0].ToString(), reader[1].ToString()));
                            }
                        }
                    }
                    connection.Close();
                }

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }












}
