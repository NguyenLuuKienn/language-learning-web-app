param([string]$Database = 'language_learning_db')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$settings = Get-Content (Join-Path $repoRoot 'backend/src/LanguageLearning.Api/appsettings.Development.json') -Raw | ConvertFrom-Json
$connection = [string]$settings.ConnectionStrings.DefaultConnection
function ConnectionValue([string]$name) {
    return [regex]::Match($connection, '(?i)(?:^|;)\s*' + $name + '\s*=\s*([^;]+)').Groups[1].Value
}
$server = ConnectionValue '(?:Server|Host)'
$port = ConnectionValue 'Port'
$configuredDatabase = ConnectionValue 'Database'
$userName = ConnectionValue '(?:User Id|Uid|User)'
$password = ConnectionValue '(?:Password|Pwd)'
if ($server -notin @('localhost', '127.0.0.1') -or $configuredDatabase -ne $Database -or !$userName) {
    throw 'This verifier accepts only the configured local Development database.'
}
if (!$port) { $port = '3306' }
$mysqlBin = 'C:\Program Files\MySQL\MySQL Server 8.0\bin'
$mysql = Join-Path $mysqlBin 'mysql.exe'
$dump = Join-Path $mysqlBin 'mysqldump.exe'
if (!(Test-Path $mysql) -or !(Test-Path $dump)) { throw 'MySQL CLI tools are missing.' }
$clone = 'language_learning_verify_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$backup = Join-Path $env:TEMP ($clone + '.sql')
$stdout = Join-Path $env:TEMP ($clone + '.log')
$stderr = Join-Path $env:TEMP ($clone + '.err')
$cloneCreated = $false
$appProcess = $null
$env:MYSQL_PWD = $password
try {
    & $dump --host=127.0.0.1 --port=$port --user=$userName --ssl-mode=DISABLED `
        --single-transaction --no-tablespaces --set-gtid-purged=OFF --result-file=$backup $Database
    if ($LASTEXITCODE -ne 0) { throw 'Database backup failed.' }
    & $mysql --host=127.0.0.1 --port=$port --user=$userName --ssl-mode=DISABLED `
        --execute="CREATE DATABASE $clone CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci"
    if ($LASTEXITCODE -ne 0) { throw 'Temporary database creation failed.' }
    $cloneCreated = $true
    $sourcePath = $backup.Replace('\', '/')
    & $mysql --host=127.0.0.1 --port=$port --user=$userName --ssl-mode=DISABLED `
        --database=$clone --execute="source $sourcePath"
    if ($LASTEXITCODE -ne 0) { throw 'Temporary database restore failed.' }

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__DefaultConnection = [regex]::Replace($connection,
        '(?i)Database\s*=\s*[^;]+', 'Database=' + $clone) + ';SslMode=None'
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $apiPort = $listener.LocalEndpoint.Port
    $listener.Stop()
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$apiPort"
    $apiDll = (Resolve-Path (Join-Path $repoRoot 'backend/src/LanguageLearning.Api/bin/Release/net8.0/LanguageLearning.Api.dll')).Path
    $appProcess = Start-Process -FilePath 'dotnet' -ArgumentList @($apiDll) -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $ready = $false
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        if ($appProcess.HasExited) { break }
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:$apiPort/api/v1/languages" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
        Start-Sleep -Seconds 1
    }
    if (!$ready) {
        $failure = Select-String -LiteralPath $stderr, $stdout -Pattern 'MySqlException|Unhandled exception|Database initialization failed' `
            -ErrorAction SilentlyContinue | Select-Object -First 3 | ForEach-Object { $_.Line }
        throw ('API did not start on the clone. ' + ($failure -join ' '))
    }
    $found = @(& $mysql --host=127.0.0.1 --port=$port --user=$userName --ssl-mode=DISABLED --database=$clone `
        --batch --skip-column-names --execute="SELECT table_name FROM information_schema.tables WHERE table_schema='$clone' AND table_name IN ('learning_sessions','learning_evidences','lesson_step_progresses','card_lesson_sources','skill_practice_events') ORDER BY table_name")
    if ($LASTEXITCODE -ne 0 -or $found.Count -ne 5) { throw ('New journey tables missing: ' + ($found -join ', ')) }
    Write-Output 'Local MySQL clone: API startup succeeded.'
    Write-Output ('Journey tables verified: ' + ($found -join ', '))
} finally {
    if ($appProcess -and !$appProcess.HasExited) { Stop-Process -Id $appProcess.Id -Force }
    if ($cloneCreated -and $clone -match '^language_learning_verify_[0-9a-f]{8}$') {
        & $mysql --host=127.0.0.1 --port=$port --user=$userName --ssl-mode=DISABLED `
            --execute="DROP DATABASE IF EXISTS $clone" | Out-Null
        Write-Output 'Temporary MySQL clone removed.'
    }
    Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    foreach ($path in @($backup, $stdout, $stderr)) {
        if ($path -and (Test-Path -LiteralPath $path)) { Remove-Item -LiteralPath $path -Force }
    }
}
