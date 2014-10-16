param (
    [string] $server = "ftk1wj2yff.database.windows.net", 
    [string] $database = "HPCReporting",
    [string] $loginId = "dbadmin",
    [string] $password = "!!123abc",
    [string] $sqlscript = ".\InitializeDatabase.sql"
)

# $server - database server address
# $database - database name
# $loginId - database login id
# $password - database login password
# $sqlscript - initialization sql script filepath

# Convert the input path to the absolute path
$sqlscript=convert-path $sqlscript

if (Test-Path $sqlscript) {
    # Run sql script
    cmd /c sqlcmd -S $server -d $database -U $loginId -P $password -t 15 -i $sqlscript
}

Write-Host Database initialization is completed.

