---
paths:
  - "**/*.ps1"
---

# PowerShell 스크립트 작성 규칙

## 실행 정책

IMPORTANT: `.ps1`을 호출할 때는 반드시 `-ExecutionPolicy Bypass`를 붙일 것.

```
powershell -NoProfile -ExecutionPolicy Bypass -File path/to/script.ps1
```

없으면 실행 정책에 막혀 스크립트가 **시작조차 못 한다**. 에러는 stderr로만
나가므로 stdout을 보는 쪽(상태줄, 파이프 등)에서는 출력이 조용히 비어 보일 뿐
원인이 드러나지 않는다.

## 소스 인코딩

IMPORTANT: `.ps1` 소스에는 ASCII 문자만 쓸 것.

Windows PowerShell 5.1은 BOM 없는 `.ps1`을 시스템 ANSI 코드페이지(여기선 CP949)로
읽는다. 파일에 비ASCII 문자를 직접 넣으면 깨진다 (`·` → `쨌`).

필요하면 코드로 생성한다:

```powershell
$dot   = [char]0x00B7   # ·
$block = [char]0x2593   # ▓
```

## 입출력 인코딩

IMPORTANT: 소스 인코딩과는 **별개 문제**다. 소스가 ASCII뿐이어도 런타임에
주고받는 데이터가 깨진다.

PowerShell은 stdin을 콘솔 코드페이지(여기선 CP949)로 디코딩하고, 네이티브
명령의 출력도 같은 방식으로 읽는다. Claude Code의 훅·상태줄은 UTF-8 JSON을
보내고 `git`도 UTF-8을 출력하므로, 그대로 두면 한글이 전부 깨진다.

특히 **stdin으로 받는 JSON에 한글이 섞이면 `ConvertFrom-Json`이 파싱 자체에
실패한다.** 깨진 바이트가 JSON 구조를 무너뜨리기 때문이다. 글자만 깨지는 게
아니라 스크립트가 통째로 동작하지 않으므로 증상만 봐서는 원인을 찾기 어렵다.

stdin으로 JSON을 받는 스크립트는 맨 위에 다음을 넣는다:

```powershell
try { [Console]::InputEncoding  = New-Object System.Text.UTF8Encoding($false) } catch { }
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$reader = New-Object System.IO.StreamReader(
    [Console]::OpenStandardInput(),
    (New-Object System.Text.UTF8Encoding($false)))
$json = $reader.ReadToEnd() | ConvertFrom-Json
```

`$input | Out-String`으로 읽으면 안 된다. 이미 콘솔 코드페이지로 디코딩된
뒤라서 되돌릴 수 없다.

파일에 쓸 때도 인코딩을 명시한다. `Set-Content`의 기본값은 ANSI다:

```powershell
[System.IO.File]::AppendAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
```
