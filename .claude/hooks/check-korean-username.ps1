# PreToolUse hook: require a Korean git user.name before committing.
#
# The AI usage log (docs/ai-log/) is keyed by git user.name, so a romanized or
# default name would split one person's records across two files.
#
# exit 2 = block the tool call (stderr is shown to Claude)
# Source is ASCII-only (see .claude/rules/powershell.md). The Hangul range is
# tested by char code rather than a literal or an escape, so nothing in this
# file depends on how PowerShell decodes it. The message is addressed to
# Claude, not to the user, so English is fine.

$ErrorActionPreference = 'SilentlyContinue'

# git prints UTF-8; without this a Korean name arrives mangled and never matches.
try { [Console]::InputEncoding  = New-Object System.Text.UTF8Encoding($false) } catch { }
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

# drain stdin so the caller never blocks
try {
    $reader = New-Object System.IO.StreamReader(
        [Console]::OpenStandardInput(),
        (New-Object System.Text.UTF8Encoding($false)))
    $null = $reader.ReadToEnd()
} catch { }

$name = (& git config user.name 2>$null | Select-Object -First 1)

# Hangul syllables occupy U+AC00 - U+D7A3
$hasHangul = $false
if (-not [string]::IsNullOrWhiteSpace($name)) {
    foreach ($ch in $name.ToCharArray()) {
        $code = [int]$ch
        if ($code -ge 0xAC00 -and $code -le 0xD7A3) { $hasHangul = $true; break }
    }
}

if ($hasHangul) { exit 0 }

if ([string]::IsNullOrWhiteSpace($name)) { $current = '(not set)' } else { $current = $name }

[Console]::Error.WriteLine(@"
BLOCKED: git user.name is not a Korean name (current: $current).

This repository keys the AI usage log (docs/ai-log/) by git user.name, so a
romanized or unset name would split one person's records across two files.

Do NOT pick a name yourself and do NOT work around this check. Ask the user
which Korean name to use, then run:

    git config user.name "<Korean name>"

Omit --global on purpose: this must apply to this repository only, leaving the
user's identity in other repositories untouched. Retry the commit afterwards.
"@)
exit 2
