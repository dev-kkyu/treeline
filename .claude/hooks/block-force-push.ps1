# PreToolUse hook: block force push.
# stdin: { "tool_name": "...", "tool_input": { "command": "..." } }
# exit 2 = block the tool call (stderr is shown to Claude)

$raw = $input | Out-String

try {
    $cmd = ($raw | ConvertFrom-Json).tool_input.command
} catch {
    exit 0   # unparseable input: do not interfere
}

if ([string]::IsNullOrWhiteSpace($cmd)) { exit 0 }

# only inspect git push commands
if ($cmd -notmatch '(?i)\bgit\b[^&|;]*\bpush\b') { exit 0 }

# --force, --force-with-lease, --force-if-includes
$isForce = $cmd -match '(?i)--force'

# short flag cluster containing f, e.g. -f / -uf
if ($cmd -match '(?i)(^|\s)-[a-z]*f([a-z]*)(\s|$)') { $isForce = $true }

# forced refspec, e.g. git push origin +develop:develop
if ($cmd -match '\s\+[^\s:]+:') { $isForce = $true }

if ($isForce) {
    [Console]::Error.WriteLine(
        "BLOCKED: force push is not allowed for Claude in this repository " +
        "(see CLAUDE.md). Do not retry or work around this. " +
        "Tell the user to run the command themselves if they want it."
    )
    exit 2
}

exit 0
