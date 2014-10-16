param (
    [string] $server = "ftk1wj2yff.database.windows.net", 
    [string] $database = "HPCReporting",
    [string] $loginId = "dbadmin",
    [string] $password = "!!123abc",
	[string] $hpcdb = ".\COMPUTECLUSTER",
    [string] $hpcname = ""
)

# This script must run in HPC powershell or with Microsoft.HPC snap-in is added
Add-PSSnapIn Microsoft.HPC

# $server - database server address
# $database - database name
# $loginId - database login id
# $password - database login password
# $hpcdb - local hpc database

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

function UploadAndUpdateData([string] $tablename, [string] $filepath, [int] $startRow=1){
    sqlcmd -S $server -d $database -U $loginId -P $password -t 15 -Q "DELETE FROM $tablename WHERE [ClusterId] = '$hpcid'"
    bcp $tablename in $filepath -c -t '|' -r '\n' -F $startRow -S $server -d $database -U $loginId -P $password 
}

function MigrateData([string] $tablename){
    $viewname = $tablename + "View"
    $tempfile = ".\" + $tablename.ToLower() + ".csv"

    # Delete temp file
    DeleteFile $tempfile

    # Collect data
    bcp "SELECT '' AS Id, '$hpcid' AS ClusterId, * FROM HpcReporting.HpcReportingView.$viewname" queryout $tempfile  -c -t '|' -r '\n' -S $hpcdb -T

	if (Test-Path $tempfile)
    {
        # Upload data
        UploadAndUpdateData "dbo.$tablename" $tempfile 1

        # Delete temp file
        DeleteFile $tempfile

        Write-Host $tablename data have been uploaded.
    }
    else
    {
        Write-Host Failed to upload $tablename data.
    }
}

#######################################
# Get cluster id
#######################################
$hpcid = hostname


#######################################
# Insert Cluster data
#######################################
if($hpcname -eq "" -or $hpcname -eq $null){
    $hpcname = $hpcid
}

sqlcmd -S $server -d $database -U $loginId -P $password -t 15 -Q "DELETE FROM dbo.Cluster WHERE [ClusterId] = '$hpcid'"
sqlcmd -S $server -d $database -U $loginId -P $password -t 15 -Q "INSERT INTO dbo.Cluster ([ClusterId], [ClusterName]) VALUES ('$hpcid', '$hpcname')"


#######################################
# Insert Network data
#######################################
$tempfile = ".\network.csv"

# Delete temp file
DeleteFile $tempfile

# Collect data
Get-HpcNetworkInterface | Where-Object {$_.type -eq "enterprise" -or $_.type -eq "application" -or $_.type -eq "Private"} | Select-Object Id,@{Name="ClusterId"; Expression= {$hpcid}},Name,Type,IpAddress,SubnetMask | Export-Csv $tempfile -Delimiter '|'

if (Test-Path $tempfile)
{
    FormatFile $tempfile

    # Upload data
    UploadAndUpdateData "dbo.Network" $tempfile 2
    
    # Delete temp file
    DeleteFile $tempfile

    Write-Host Network data have been uploaded.
}
else
{
    Write-Host Failed to upload Network data.
}


#######################################
# Insert Node data
#######################################
# MigrateData "Node"
$tempfile = ".\node.csv"

# Delete temp file
DeleteFile $tempfile

# Collect data
Get-HpcNode | Select-Object Id,@{Name="ClusterId"; Expression= {$hpcid}},NetBiosName,Processors,ProcessorCores,Sockets,Memory,Location,NodeRole,SubscribedCores,SubscribedSockets,AzureInstanceSize | Export-Csv $tempfile -Delimiter '|'

if (Test-Path $tempfile)
{
    FormatFile $tempfile

    # Upload data
    UploadAndUpdateData "dbo.Node" $tempfile 2
    
    # Delete temp file
    DeleteFile $tempfile

    Write-Host Node data have been uploaded.
}
else
{
    Write-Host Failed to upload Node data.
}


#######################################
# Insert Group data
#######################################
MigrateData "NodeGroup"


#######################################
# Insert NodeGroupMemberShip data
#######################################
MigrateData "NodeGroupMemberShip"


#######################################
# Insert CustomProperty data
#######################################
MigrateData "CustomProperty"


