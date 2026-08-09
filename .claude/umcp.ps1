# Call the Unity MCP server directly over HTTP.
#
# Use this when mcp__UnityMCP__* tools are missing from the session. MCP attaches
# only at session start, but the server itself keeps running inside the editor,
# so JSON-RPC over plain HTTP reaches it just fine.
#
# See docs/ai/setup.md for the why. Source is ASCII only on purpose -- see
# .claude/rules/powershell.md.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File .claude/umcp.ps1 -List
#   ... -Tool read_console -ArgsJson '{"action":"get"}'
#   ... -Tool execute_code -ArgsFile scratch\pose.cs
#
# -ArgsFile sends the file's contents as execute_code's `code` argument, which
# avoids escaping a whole C# body through JSON by hand.
param(
    [string]$Tool,
    [string]$ArgsJson = '{}',
    [string]$ArgsFile,
    [string]$Url = 'http://127.0.0.1:8080/mcp',
    [switch]$List,
    [int]$TimeoutSec = 240
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$Headers = @{ Accept = 'application/json, text/event-stream' }

function Read-Sse($content) {
    # Responses come back as SSE. Pull the one data line out; if it is plain
    # JSON already, hand it back untouched.
    foreach ($line in ($content -split "`n")) {
        if ($line.StartsWith('data: ')) { return $line.Substring(6).Trim() }
    }
    return $content
}

function Invoke-Rpc($body, $sid) {
    $h = $Headers.Clone()
    if ($sid) { $h['Mcp-Session-Id'] = $sid }
    $r = Invoke-WebRequest -Uri $Url -Method Post -ContentType 'application/json' `
        -Headers $h -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) `
        -UseBasicParsing -TimeoutSec $TimeoutSec
    return @{ Sid = $r.Headers['Mcp-Session-Id']; Json = (Read-Sse $r.Content) }
}

# Handshake. The session id from initialize must ride along on every later call.
$init = Invoke-Rpc '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"umcp","version":"1"}}}' $null
$sid = $init.Sid
if ($sid -is [array]) { $sid = $sid[0] }
Invoke-Rpc '{"jsonrpc":"2.0","method":"notifications/initialized"}' $sid | Out-Null

if ($List) {
    $res = Invoke-Rpc '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' $sid
    $obj = $res.Json | ConvertFrom-Json
    $obj.result.tools | ForEach-Object { "{0,-26} {1}" -f $_.name, ($_.description -split "`n")[0] }
    exit 0
}

if (-not $Tool) { throw 'Pass -Tool <name>, or -List to see what is available.' }

if ($ArgsFile) {
    $code = [System.IO.File]::ReadAllText($ArgsFile, (New-Object System.Text.UTF8Encoding($false)))
    # execute_code requires `action`; leaving it out fails validation server-side.
    $argObj = @{ action = 'execute'; code = $code }
} else {
    $argObj = $ArgsJson | ConvertFrom-Json
}

$payload = @{
    jsonrpc = '2.0'; id = 3; method = 'tools/call'
    params  = @{ name = $Tool; arguments = $argObj }
} | ConvertTo-Json -Depth 30 -Compress

$res = Invoke-Rpc $payload $sid
$obj = $res.Json | ConvertFrom-Json
if ($obj.error) { 'RPC ERROR: ' + ($obj.error | ConvertTo-Json -Depth 10); exit 1 }
foreach ($c in $obj.result.content) { if ($c.type -eq 'text') { $c.text } }
if ($obj.result.isError) { exit 1 }
