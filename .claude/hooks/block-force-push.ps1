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

# Inspect each subcommand on its own. Judging the whole string at once made
# "git commit -F - && git push origin develop" look like a force push, because
# commit's -F matched the -f short flag rule.
$isForce = $false

foreach ($seg in ($cmd -split '(?:&&|\|\||;|\||\r?\n)')) {

    # A segment counts as a git command only when it starts with git, after any
    # env assignments or wrappers. Without this anchor a heredoc line that merely
    # mentions "git push --force" in prose would trip the check.
    if ($seg -notmatch '(?i)^\s*(?:\w+=\S+\s+)*(?:sudo\s+|timeout\s+\S+\s+|time\s+|nice\s+|nohup\s+)*git\b') { continue }
    if ($seg -notmatch '(?i)\bpush\b') { continue }

    # --force, --force-with-lease, --force-if-includes
    if ($seg -match '(?i)--force') { $isForce = $true; break }

    # short flag cluster containing f, e.g. -f / -uf
    if ($seg -match '(?i)(^|\s)-[a-z]*f([a-z]*)(\s|$)') { $isForce = $true; break }

    # forced refspec, e.g. git push origin +develop:develop
    if ($seg -match '\s\+[^\s:]+:') { $isForce = $true; break }
}

if ($isForce) {
    [Console]::Error.WriteLine(
        "BLOCKED: force push is not allowed for Claude in this repository " +
        "(see CLAUDE.md). Do not retry or work around this. " +
        "Tell the user to run the command themselves if they want it."
    )
    exit 2
}

exit 0
