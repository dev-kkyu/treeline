# stdin carries UTF-8 JSON and git prints UTF-8. Without this the console
# codepage (CP949 here) mangles non-ASCII, and a Korean path in the JSON
# breaks ConvertFrom-Json outright - the whole status line then renders empty.
try { [Console]::InputEncoding  = New-Object System.Text.UTF8Encoding($false) } catch { }
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$reader = New-Object System.IO.StreamReader(
    [Console]::OpenStandardInput(),
    (New-Object System.Text.UTF8Encoding($false)))
$json = $reader.ReadToEnd() | ConvertFrom-Json

$model = $json.model.display_name
$pct   = $json.context_window.used_percentage
$cost  = $json.cost.total_cost_usd
$cwd   = $json.workspace.current_dir
if (-not $cwd) { $cwd = $json.cwd }

if ($null -eq $pct)  { $pct = 0 }
if ($null -eq $cost) { $cost = 0 }

$pctInt = [int][Math]::Floor($pct)

# --- palette (256-color) ---
$e     = [char]27
$reset = "$e[0m"
function C($n) { "$e[38;5;${n}m" }

$cSep      = C 239   # dark grey     - separators
$cDirUp    = C 65    # dark sage     - parent dir
$cDir      = C 151   # pale sage     - working dir
$cBranch   = C 176   # soft violet   - branch
$cModel    = C 110   # soft blue     - model name
$cEmpty    = C 237   # dark grey     - unused bar
$cBracket  = C 73    # muted teal    - gauge frame
$cCostLbl  = C 137   # muted tan     - "usage:" label
$cCost     = C 179   # muted gold    - cost
$cLimLbl   = C 132   # dusty rose    - "limit:" label

# bar color shifts with usage: sage -> amber -> rose
if     ($pctInt -ge 80) { $cBar = C 174 }
elseif ($pctInt -ge 50) { $cBar = C 180 }
else                    { $cBar = C 108 }

# --- git branch ---
$branch = $null
try {
    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null | Select-Object -First 1)
} catch { }

# --- context bar ---
$filled = [int][Math]::Round($pctInt / 10)
if ($filled -gt 10) { $filled = 10 }
if ($filled -lt 0)  { $filled = 0 }
$bar = "$cBar" + ([string][char]0x2593 * $filled) + "$cEmpty" + ([string][char]0x2591 * (10 - $filled)) + $reset

# --- 5-hour rate limit ---
# Only present for Claude.ai subscribers, and only after the first API response
# of the session. Absent otherwise, so the whole segment is dropped silently.
# The reset countdown is noise until the limit is close, so it appears above 80%.
$limitText = $null
$rl = $json.rate_limits.five_hour
if ($rl -and $null -ne $rl.used_percentage) {
    $lim = [int][Math]::Floor([double]$rl.used_percentage)

    if     ($lim -ge 80) { $cLim = C 174 }
    elseif ($lim -ge 50) { $cLim = C 180 }
    else                 { $cLim = C 108 }

    $tail = ''
    if ($lim -gt 80 -and $rl.resets_at) {
        try {
            $resetsAt = [DateTimeOffset]::FromUnixTimeSeconds([long]$rl.resets_at).LocalDateTime
            $left     = $resetsAt - (Get-Date)
            if ($left.TotalSeconds -gt 0) {
                if ($left.TotalHours -ge 1) {
                    $tail = ' ({0}h{1:00}m)' -f [int]$left.TotalHours, $left.Minutes
                } else {
                    $tail = ' ({0}m)' -f [int]$left.TotalMinutes
                }
            }
        } catch { }
    }

    $limitText = "${cLimLbl}limit:$reset $cLim" + "5h $lim%$tail" + $reset
}

$sep = "$cSep | $reset"

$parts = @()
if ($cwd) {
    # last two path segments: "parent/leaf"
    $seg = @($cwd -split '[\\/]' | Where-Object { $_ -ne '' })
    if ($seg.Count -ge 2) {
        $dirText = "$cDirUp$($seg[-2])/$reset$cDir$($seg[-1])$reset"
    } else {
        $dirText = "$cDir$($seg[-1])$reset"
    }
    $parts += $dirText
}
if ($branch) { $parts += "$cBranch$branch$reset" }
$parts += "$cModel$model$reset"
$parts += "$cBracket[$reset$bar$cBracket]$reset $cBar$pctInt%$reset"
$parts += "${cCostLbl}usage:$reset $cCost`$$('{0:N2}' -f $cost)$reset"
if ($limitText) { $parts += $limitText }

Write-Host ($parts -join $sep)
