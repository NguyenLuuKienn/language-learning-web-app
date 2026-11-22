<#
.SYNOPSIS
    Automated setup script for GitHub Repository labels, milestones, and issues.
.DESCRIPTION
    Uses GitHub REST API to bootstrap project management tracking infrastructure.
.PARAMETER Repo
    Target GitHub repository in 'owner/repo' format.
.PARAMETER Token
    GitHub Personal Access Token (PAT) with repo scope.
.PARAMETER DryRun
    Simulates API operations without making remote modifications.
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
    Write-Error "Please provide a GitHub Token via -Token or the `$env:GITHUB_TOKEN environment variable."
    exit 1
}

$Repo = $Repo -replace '^https?://github\.com/', '' -replace '\.git/?$', '' -replace '/$', ''

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataFile = Join-Path $ScriptDir "issues-data.json"

if (-not (Test-Path $DataFile)) {
    Write-Error "Data file not found: $DataFile"
    exit 1
}

$Data = Get-Content $DataFile -Raw -Encoding UTF8 | ConvertFrom-Json

Write-Host "Setting up GitHub repository: $Repo" -ForegroundColor Cyan

$Headers = @{
    "Authorization" = "token $Token"
    "Accept"        = "application/vnd.github.v3+json"
    "User-Agent"    = "Antigravity-GitHub-Setup-Script"
}

Write-Host "Configuring labels..." -ForegroundColor Yellow
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
        Write-Host "  [OK] Created label: $($label.name)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 422) {
            $labelNameEscaped = [System.Uri]::EscapeDataString($label.name)
            $updateUri = "https://api.github.com/repos/$Repo/labels/$labelNameEscaped"
            try {
                $res = Invoke-RestMethod -Uri $updateUri -Method Patch -Headers $Headers -Body $body -ContentType "application/json"
                Write-Host "  [UPDATE] Updated label: $($label.name)" -ForegroundColor Blue
            }
            catch {
                Write-Warning "  [WARN] Skipped label $($label.name): $($_.Exception.Message)"
            }
        }
        else {
            Write-Warning "  [WARN] Failed to create label $($label.name): $($_.Exception.Message)"
        }
    }
}

Write-Host "Configuring milestones..." -ForegroundColor Yellow
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
        Write-Host "  [OK] Created milestone: $($ms.title) (ID: $($res.number))" -ForegroundColor Green
    }
    catch {
        Write-Host "  [INFO] Milestone $($ms.title) exists, resolving ID..." -ForegroundColor Yellow
        try {
            $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/milestones?state=all" -Method Get -Headers $Headers
            $found = $existing | Where-Object { $_.title -eq $ms.title }
            if ($found) {
                $MilestoneMap[$ms.title] = $found.number
                Write-Host "  [FOUND] Resolved milestone $($ms.title) with ID: $($found.number)" -ForegroundColor Blue
            }
        }
        catch {
            Write-Warning "  [WARN] Failed to query milestone: $($_.Exception.Message)"
        }
    }
}

Write-Host "Creating issues..." -ForegroundColor Yellow
$IssueIndex = 0

foreach ($issue in $Data.issues) {
    $IssueIndex++
    $msNumber = $null
    if ($MilestoneMap.ContainsKey($issue.milestone)) {
        $msNumber = $MilestoneMap[$issue.milestone]
    }

    $formattedTitle = "[#$($issue.id.ToString('D2'))] $($issue.title)"
    $issueBody = "**Workstream:** $($issue.workstream) | **Assignee:** $($issue.owner)`n`n$($issue.body)"

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
        Write-Host "  [OK] [$IssueIndex/62] Created issue #$($res.number): $formattedTitle" -ForegroundColor Green
        Start-Sleep -Milliseconds 400
    }
    catch {
        Write-Warning "  [WARN] Failed to create issue $($issue.id): $($_.Exception.Message)"
    }
}

Write-Host "Repository setup completed successfully." -ForegroundColor Green
