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

스크립트가 비ASCII를 출력한다면 맨 위에 다음을 추가한다:

```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```
