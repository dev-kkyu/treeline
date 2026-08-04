[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$json = $input | Out-String | ConvertFrom-Json

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
$cModelLbl = C 67    # muted steel   - "model:" label
$cModel    = C 110   # soft blue     - model name
$cCtxLbl   = C 73    # muted teal    - "context:" label
$cEmpty    = C 237   # dark grey     - unused bar
$cCostLbl  = C 137   # muted tan     - "usage:" label
$cCost     = C 179   # muted gold    - cost

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
$parts += "${cModelLbl}model:$reset $cModel$model$reset"
$parts += "${cCtxLbl}context:$reset $bar $cBar$pctInt%$reset"
$parts += "${cCostLbl}usage:$reset $cCost`$$('{0:N2}' -f $cost)$reset"

Write-Host ($parts -join $sep)
