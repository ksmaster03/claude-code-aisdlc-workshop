# OpsTrack lab: pre-course machine check (Windows PowerShell 5.1+)
# Usage: powershell -ExecutionPolicy Bypass -File check-machine.ps1 [-RepoPath C:\work\opstrack]
param([string]$RepoPath = "")

$results = @()
function Check($name, [scriptblock]$test, $fix) {
    try {
        $out = & $test
        if ($out) { $script:results += [pscustomobject]@{ Item = $name; Status = "OK"; Detail = "$out"; Fix = "" } }
        else { $script:results += [pscustomobject]@{ Item = $name; Status = "MISSING"; Detail = ""; Fix = $fix } }
    } catch {
        $script:results += [pscustomobject]@{ Item = $name; Status = "MISSING"; Detail = $_.Exception.Message; Fix = $fix }
    }
}

Check "Git" { (git --version) 2>$null } "Install Git for Windows: https://git-scm.com/download/win"
Check ".NET SDK 8+" {
    $sdks = (dotnet --list-sdks) 2>$null
    $ok = $sdks | Where-Object { $_ -match '^(\d+)\.' -and [int]$Matches[1] -ge 8 }
    if ($ok) { ($ok | Select-Object -Last 1) }
} "Install .NET 8 SDK or newer: https://dotnet.microsoft.com/download"
Check "Node.js 20+" {
    $v = (node -v) 2>$null
    if ($v -match '^v(\d+)' -and [int]$Matches[1] -ge 20) { $v }
} "Install Node.js 22 LTS: https://nodejs.org"
Check "Claude Code CLI" { (claude --version) 2>$null } "PowerShell: irm https://claude.ai/install.ps1 | iex   (or: winget install Anthropic.ClaudeCode)"
Check "VS Code" { (code --version | Select-Object -First 1) 2>$null } "Install VS Code, then the extension 'Claude Code' (anthropic.claude-code)"
Check "VS Code: Claude Code extension" {
    $ext = (code --list-extensions) 2>$null
    if ($ext -contains "anthropic.claude-code") { "anthropic.claude-code" }
} "In VS Code: Extensions > search 'Claude Code' by Anthropic > Install"
Check "Reach api.anthropic.com" {
    # any HTTP answer (even 401/404) proves the network path works; only a connection error fails
    try { Invoke-WebRequest -Uri "https://api.anthropic.com" -Method Head -UseBasicParsing -TimeoutSec 10 | Out-Null; "reachable" }
    catch [System.Net.WebException] { if ($_.Exception.Response) { "reachable (HTTP answer)" } else { throw } }
} "Ask IT to allow claude.ai and api.anthropic.com (or set HTTPS_PROXY)"
Check "Reach nuget.org" {
    $r = Invoke-WebRequest -Uri "https://api.nuget.org/v3/index.json" -UseBasicParsing -TimeoutSec 10
    if ($r.StatusCode -eq 200) { "reachable" }
} "Ask IT to allow nuget.org or configure the internal NuGet mirror"
Check "Smart App Control not enforcing" {
    # 1 = enforcing: freshly built .NET DLLs can be blocked at random (0x800711C7)
    $v = (Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -ErrorAction SilentlyContinue).VerifiedAndReputablePolicyState
    if ($v -ne 1) { "state=$v (OK)" }
} "Smart App Control is enforcing and may block DLLs you build (0x800711C7). Do NOT switch it off yourself (it cannot be switched back on): ask IT, or use WSL2 for the lab"
Check "Free disk on current drive >= 3 GB" {
    $d = (Get-Location).Drive
    $free = [math]::Round($d.Free / 1GB, 1)
    if ($free -ge 3) { "$free GB free" }
} "Free up at least 3 GB"

if ($RepoPath -ne "") {
    Push-Location $RepoPath
    Check "OpsTrack build" {
        dotnet build OpsTrack.sln -nologo -v q | Out-Null
        if ($LASTEXITCODE -eq 0) { "build succeeded" }
    } "Run 'dotnet build OpsTrack.sln' and send the error to the trainer"
    Check "OpsTrack tests" {
        $t = dotnet test OpsTrack.sln -nologo --no-build 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0) { ($t -split "`n" | Where-Object { $_ -match 'Passed!|Total tests|passed' } | Select-Object -Last 1).Trim() }
    } "Run 'dotnet test OpsTrack.sln' and send the output to the trainer"
    Pop-Location
}

$results | Format-Table -AutoSize -Wrap
$missing = @($results | Where-Object { $_.Status -ne "OK" }).Count
if ($missing -eq 0) { Write-Host "All checks passed. See you in class." -ForegroundColor Green }
else { Write-Host "$missing item(s) need attention. Fix them or send this screen to the trainer." -ForegroundColor Yellow }
