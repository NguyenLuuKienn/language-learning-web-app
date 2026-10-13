<#
.SYNOPSIS
    Script tu dong tao Labels, Milestones va 62 Issues len GitHub Repository.

.DESCRIPTION
    Su dung GitHub REST API de khoi tao toan bo ha tang quan ly du an phuc vu mon hoc PTPMMNM.
    Chi can cung cap Ten Repo (owner/repo) va GitHub Personal Access Token (PAT).

.PARAMETER Repo
    Ten repository tren GitHub theo dinh dang 'owner/repo' (Vi du: 'my-org/language-learning-web-app')

.PARAMETER Token
    GitHub Personal Access Token (PAT) co quyen 'repo' hoac 'issues:write'.

.PARAMETER DryRun
    Neu bat co nay, script chi in ra cac buoc thuc hien ma khong goi API thuc te.

.EXAMPLE
    .\scripts\github-setup.ps1 -Repo "your-username/language-learning-app" -Token "ghp_xxxxxxxxxxxx"
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Repo,

    [Parameter(Mandatory = $false)]
    [string]$Token = $env:GITHUB_TOKEN,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not $Token -and -not $DryRun) {
    Write-Error "Vui long cung cap GitHub Token qua tham so -Token hoac bien moi truong `$env:GITHUB_TOKEN"
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataFile = Join-Path $ScriptDir "issues-data.json"

if (-not (Test-Path $DataFile)) {
    Write-Error "Khong tim thay tep du lieu $DataFile"
    exit 1
}

$Data = Get-Content $DataFile -Raw -Encoding UTF8 | ConvertFrom-Json

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " BAT DAU THIET LAP GITHUB CHO REPOSITORY: $Repo" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$Headers = @{
    "Authorization" = "token $Token"
    "Accept"        = "application/vnd.github.v3+json"
    "User-Agent"    = "Antigravity-GitHub-Setup-Script"
}

# 1. TAO HOAC CAP NHAT LABELS
Write-Host ""
Write-Host "[1/3] Dang xu ly danh sach Labels..." -ForegroundColor Yellow
foreach ($label in $Data.labels) {
    if ($DryRun) {
        Write-Host "  [DryRun] Label: $($label.name) (#$($label.color))" -ForegroundColor Gray
        continue
    }

    $uri = "https://api.github.com/repos/$Repo/labels"
    $body = @{
        name        = $label.name
        color       = $label.color
        description = $label.description
    } | ConvertTo-Json

    try {
        $res = Invoke-RestMethod -Uri $uri -Method Post -Headers $Headers -Body $body -ContentType "application/json"
        Write-Host "  [OK] Tao thanh cong label: $($label.name)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 422) {
            $labelNameEscaped = [System.Uri]::EscapeDataString($label.name)
            $updateUri = "https://api.github.com/repos/$Repo/labels/$labelNameEscaped"
            try {
                $res = Invoke-RestMethod -Uri $updateUri -Method Patch -Headers $Headers -Body $body -ContentType "application/json"
                Write-Host "  [UPDATE] Cap nhat label: $($label.name)" -ForegroundColor Blue
            }
            catch {
                Write-Warning "  [WARN] Bo qua label $($label.name): $($_.Exception.Message)"
            }
        }
        else {
            Write-Warning "  [WARN] Loi khi tao label $($label.name): $($_.Exception.Message)"
        }
    }
}

# 2. TAO MILESTONES
Write-Host ""
Write-Host "[2/3] Dang xu ly Milestones..." -ForegroundColor Yellow
$MilestoneMap = @{}

foreach ($ms in $Data.milestones) {
    if ($DryRun) {
        Write-Host "  [DryRun] Milestone: $($ms.title)" -ForegroundColor Gray
        $MilestoneMap[$ms.title] = 1
        continue
    }

    $uri = "https://api.github.com/repos/$Repo/milestones"
    $body = @{
        title       = $ms.title
        description = $ms.description
        due_on      = $ms.due_on
    } | ConvertTo-Json

    try {
        $res = Invoke-RestMethod -Uri $uri -Method Post -Headers $Headers -Body $body -ContentType "application/json"
        $MilestoneMap[$ms.title] = $res.number
        Write-Host "  [OK] Tao thanh cong milestone: $($ms.title) (ID: $($res.number))" -ForegroundColor Green
    }
    catch {
        Write-Host "  [INFO] Milestone $($ms.title) co the da ton tai, dang tim ID..." -ForegroundColor Yellow
        try {
            $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/milestones?state=all" -Method Get -Headers $Headers
            $found = $existing | Where-Object { $_.title -eq $ms.title }
            if ($found) {
                $MilestoneMap[$ms.title] = $found.number
                Write-Host "  [FOUND] Tim thay milestone $($ms.title) voi ID: $($found.number)" -ForegroundColor Blue
            }
        }
        catch {
            Write-Warning "  [WARN] Khong the truy van milestone: $($_.Exception.Message)"
        }
    }
}

# 3. TAO 62 ISSUES
Write-Host ""
Write-Host "[3/3] Dang tien hanh tao 62 Issues..." -ForegroundColor Yellow
$IssueIndex = 0

foreach ($issue in $Data.issues) {
    $IssueIndex++
    $msNumber = $null
    if ($MilestoneMap.ContainsKey($issue.milestone)) {
        $msNumber = $MilestoneMap[$issue.milestone]
    }

    $formattedTitle = "[#$($issue.id.ToString('D2'))] $($issue.title)"
    $issueBody = "**Workstream:** $($issue.workstream) | **Phu trach:** $($issue.owner)`n`n$($issue.body)"

    if ($DryRun) {
        Write-Host "  [DryRun] [$IssueIndex/62] $formattedTitle (Milestone: $($issue.milestone))" -ForegroundColor Gray
        continue
    }

    $uri = "https://api.github.com/repos/$Repo/issues"
    $payload = @{
        title     = $formattedTitle
        body      = $issueBody
        labels    = $issue.labels
    }
    if ($msNumber) {
        $payload["milestone"] = $msNumber
    }

    $bodyJson = $payload | ConvertTo-Json -Depth 5

    try {
        $res = Invoke-RestMethod -Uri $uri -Method Post -Headers $Headers -Body $bodyJson -ContentType "application/json; charset=utf-8"
        Write-Host "  [OK] [$IssueIndex/62] Da tao Issue #$($res.number): $formattedTitle" -ForegroundColor Green
        Start-Sleep -Milliseconds 400
    }
    catch {
        Write-Warning "  [WARN] Loi khi tao Issue $($issue.id): $($_.Exception.Message)"
    }
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " HOAN TAT THIET LAP REPOSITORY THANH CONG!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
