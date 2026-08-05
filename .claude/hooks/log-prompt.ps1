# UserPromptSubmit hook: append each prompt to a per-author log.
#
# IMPORTANT: this hook must print NOTHING to stdout. On UserPromptSubmit,
# stdout is injected into Claude's context, so any stray output would be
# prepended to every prompt. Errors are swallowed on purpose: a logging
# failure must never block the user's work.
#
# Source is ASCII-only (see .claude/rules/powershell.md).

$ErrorActionPreference = 'SilentlyContinue'

# stdin carries UTF-8 JSON and git prints UTF-8; without this the console
# codepage (CP949 here) mangles every non-ASCII character.
try { [Console]::InputEncoding  = New-Object System.Text.UTF8Encoding($false) } catch { }
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

try {
    $stdin  = [Console]::OpenStandardInput()
    $reader = New-Object System.IO.StreamReader($stdin, (New-Object System.Text.UTF8Encoding($false)))
    $raw    = $reader.ReadToEnd()
    $json   = $raw | ConvertFrom-Json

    $prompt = $json.prompt
    if ([string]::IsNullOrWhiteSpace($prompt)) { exit 0 }

    # opt-out marker: prompts containing [nolog] are never written
    if ($prompt -match '(?i)\[nolog\]') { exit 0 }

    # ---- repo root -------------------------------------------------------
    $root = (& git rev-parse --show-toplevel 2>$null | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($root)) { $root = $json.cwd }
    if ([string]::IsNullOrWhiteSpace($root)) { exit 0 }

    # ---- author ----------------------------------------------------------
    $author = (& git config user.name 2>$null | Select-Object -First 1)
    if ([string]::IsNullOrWhiteSpace($author)) { $author = $env:USERNAME }
    if ([string]::IsNullOrWhiteSpace($author)) { $author = 'unknown' }
    foreach ($ch in [System.IO.Path]::GetInvalidFileNameChars()) {
        $author = $author.Replace($ch, '_')
    }

    # ---- redaction -------------------------------------------------------
    # Best effort only. Pattern matching cannot catch every secret.
    $rules = @(
        @{ p = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'; r = '[EMAIL]' },
        @{ p = '\b01[016-9][-. ]?\d{3,4}[-. ]?\d{4}\b';          r = '[PHONE]' },
        @{ p = '\b\d{6}[-. ]?[1-4]\d{6}\b';                      r = '[RRN]' },
        @{ p = '\b(?:\d{4}[-. ]?){3}\d{4}\b';                    r = '[CARD]' },
        @{ p = 'sk-[A-Za-z0-9_-]{16,}';                          r = '[API_KEY]' },
        @{ p = 'ghp_[A-Za-z0-9]{20,}';                           r = '[GITHUB_TOKEN]' },
        @{ p = 'github_pat_[A-Za-z0-9_]{20,}';                   r = '[GITHUB_TOKEN]' },
        @{ p = 'AKIA[0-9A-Z]{16}';                               r = '[AWS_KEY]' },
        @{ p = 'AIza[0-9A-Za-z_-]{35}';                          r = '[GOOGLE_KEY]' },
        @{ p = 'xox[baprs]-[A-Za-z0-9-]{10,}';                   r = '[SLACK_TOKEN]' },
        @{ p = '(?i)\b(bearer)\s+[A-Za-z0-9._-]{20,}';           r = '$1 [TOKEN]' },
        @{ p = '(?i)\b(api[_-]?key|token|secret|password|passwd|pwd)\b(\s*[:=]\s*)\S+'; r = '$1$2[REDACTED]' }
    )
    foreach ($rule in $rules) { $prompt = $prompt -replace $rule.p, $rule.r }

    # ---- write -----------------------------------------------------------
    $dir = Join-Path $root 'docs/ai/prompts'
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $file  = Join-Path $dir ($author + '.md')
    $stamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $sid   = "$($json.session_id)"
    if ($sid.Length -gt 8) { $sid = $sid.Substring(0, 8) }

    $entry = "`n## $stamp - session $sid`n$prompt`n"

    if (-not (Test-Path $file)) {
        $header = "# AI prompt log - $author`n`n" +
                  "This file is appended automatically by .claude/hooks/log-prompt.ps1.`n" +
                  "Do not edit by hand. Add ``[nolog]`` to a prompt to skip logging it.`n"
        [System.IO.File]::WriteAllText($file, $header, (New-Object System.Text.UTF8Encoding($false)))
    }
    [System.IO.File]::AppendAllText($file, $entry, (New-Object System.Text.UTF8Encoding($false)))
} catch {
    # never surface logging failures
}

exit 0
