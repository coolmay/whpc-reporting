param (
    [string] $server = "ftk1wj2yff.database.windows.net", 
    [string] $database = "HPCReporting",
    [string] $loginId = "dbadmin",
    [string] $password = "!!123abc",
	[string] $hpcdb = ".\COMPUTECLUSTER",
    [datetime] $starttime, 
    [datetime] $endtime
)

# This script must run in HPC powershell or with Microsoft.HPC snap-in is added
Add-PSSnapIn Microsoft.HPC

# $server - database server address
# $database - database name
# $loginId - database login id
# $password - database login password
# $hpcdb - local hpc database
# $starttime - start time to collect the metric value history records
# $endtime - end time to collect the metric value history records

function DeleteFile([string] $filepath){
    if (Test-Path $filepath)
    {
	    del $filepath
    }
}

function FormatFile([string] $filepath){
    $tmp = gc $filepath | ForEach-Object {$_ -replace "`"",""}
    $tmp | sc $filepath
}

function UploadData([string] $tablename, [string] $filepath, [int] $startRow=1){
    bcp $tablename in $filepath -c -t '|' -r '\n' -F $startRow -S $server -d $database -U $loginId -P $password 
}

function UploadAndUpdateData([string] $tablename, [string] $filepath, [int] $startRow=1){
    sqlcmd -S $server -d $database -U $loginId -P $password -t 15 -Q "DELETE FROM $tablename WHERE [ClusterId] = '$hpcid'"
    bcp $tablename in $filepath -c -t '|' -r '\n' -F $startRow -S $server -d $database -U $loginId -P $password 
}

function MigrateData([string] $tablename, [string] $filter){
    $viewname = $tablename + "View"
    $tempfile = ".\" + $tablename.ToLower() + ".csv"

    # Delete temp file
    DeleteFile $tempfile

    # Collect data
    bcp "SELECT '' AS Id, '$hpcid' AS ClusterId, * FROM HpcReporting.HpcReportingView.$viewname WHERE $filter >= '$starttime' AND $filter < '$endtime'" queryout $tempfile  -c -t '|' -r '\n' -S $hpcdb -T

	if (Test-Path $tempfile)
    {
        # Upload data
        UploadData "dbo.$tablename" $tempfile 1

        # Delete temp file
        DeleteFile $tempfile

        Write-Host $tablename data have been uploaded.
    }
    else
    {
        Write-Host Failed to upload $tablename data.
    }
}


# defaultly we get only yesterday's data
$today = Get-Date;
if ($starttime -eq $null)
{
    $starttime = $today.Date.AddDays(-1); # yesterday's 00:00
}
if ($endtime -eq $null)
{
    $endtime = $today.Date; # today's 00:00
}


#######################################
# Get cluster id
#######################################
$hpcid = hostname


#######################################
# Insert Metric data
#######################################
$tempfile = ".\metricvaluehistorystage1.csv"

# Delete temp files
DeleteFile $tempfile

# Collect HPCCpuUsage
Get-HpcMetricValueHistory -StartDate $starttime -EndDate $endtime -MetricName HPCCpuUsage | Select-Object Id,@{Name="ClusterId"; Expression= {$hpcid}},NodeName,Metric,Counter,Time,Value | Export-Csv $tempfile -Delimiter '|'

if (Test-Path $tempfile)
{
    FormatFile $tempfile

    # Upload data
    UploadData "dbo.MetricValueHistory" $tempfile 2
    
    # Delete temp file
    DeleteFile $tempfile

    Write-Host HPCCpuUsage data have been uploaded.
}
else
{
    Write-Host Failed to upload HPCCpuUsage data.
}

$tempfile = ".\metricvaluehistorystage2.csv"

# Delete temp files
DeleteFile $tempfile

# Collect HPCPhysicalMem
Get-HpcMetricValueHistory -StartDate $starttime -EndDate $endtime -MetricName HPCPhysicalMem | Select-Object Id,@{Name="ClusterId"; Expression= {$hpcid}},NodeName,Metric,Counter,Time,Value | Export-Csv $tempfile -Delimiter '|'

if (Test-Path $tempfile)
{
    FormatFile $tempfile

    # Upload data
    UploadData "dbo.MetricValueHistory" $tempfile 2
    
    # Delete temp file
    DeleteFile $tempfile

    Write-Host HPCPhysicalMem data have been uploaded.
}
else
{
    Write-Host Failed to upload HPCPhysicalMem data.
}

$tempfile = ".\metricvaluehistorystage3.csv"

# Delete temp files
DeleteFile $tempfile

# Collect HPCNetwork
Get-HpcMetricValueHistory -StartDate $starttime -EndDate $endtime -MetricName HPCNetwork | Select-Object Id,@{Name="ClusterId"; Expression= {$hpcid}},NodeName,Metric,Counter,Time,Value | Export-Csv $tempfile -Delimiter '|'

if (Test-Path $tempfile)
{
    FormatFile $tempfile

    # Upload data
    UploadData "dbo.MetricValueHistory" $tempfile 2
    
    # Delete temp file
    DeleteFile $tempfile

    Write-Host HPCNetwork data have been uploaded.
}
else
{
    Write-Host Failed to upload HPCNetwork data.
}

Write-Host Metric data have been uploaded.


#######################################
# Insert AllocationHistory data
#######################################
MigrateData "AllocationHistory" "EndTime"


#######################################
# Insert DailyNodeStat data
#######################################
MigrateData "DailyNodeStat" "Date"


#######################################
# Insert JobHistory data
#######################################
MigrateData "JobHistory" "EventTime"


#######################################
# Insert NodeEventHistory data
#######################################
MigrateData "NodeEventHistory" "EventTime"
