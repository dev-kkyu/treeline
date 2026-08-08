> 상태: 작성 · 최종 수정 2026-08-08

# Crawler 리그 원본

`Assets/` 밖에 있다. Unity 는 `.blend` 를 열 때 Blender 설치를 요구하는데, 팀원
중 누가 Blender 를 깔았는지 보장할 수 없어서 임포트 대상에서 빼 두었다. 게임이
쓰는 것은 `Assets/Game/Enemy/Models/Crawler.fbx` 이고 이 폴더는 그것을 다시
만들기 위한 재료다.

| 파일 | 무엇 |
| --- | --- |
| `Crawler_Rig.blend` | 22 deform bone + 발 IK 컨트롤 + 액션 6개. 텍스처 내장 |
| `build_rig.py` | 메시에서 본을 세우고 스키닝한다 |
| `animate.py` | 액션 6개를 만들고 컨트롤 리그를 deform bone 으로 굽는다 |
| `export.py` | 텍스처를 풀고 `Crawler.fbx` 를 내보낸다 |

만든 규격은 [`docs/enemy-sheets/mockups/codex/crawler/animation/`](../../../docs/enemy-sheets/mockups/codex/crawler/animation/)
의 명세를 따른다. 명세와 다르게 만든 부분과 그 이유는
[`docs/tech-design/decisions.md`](../../../docs/tech-design/decisions.md) 에 있다.

## 다시 만들기

원본 메시는 Meshy AI 출력물이다. 세 스크립트는 순서대로 돌려야 하며, 각 단계가
다음 단계의 입력을 만든다.

```bash
BLENDER="/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"

# 1. 리그 + 스키닝  (입력: Meshy 원본 .blend)
"$BLENDER" -b crawler_meshy.blend --python build_rig.py

# 2. 액션 6개 + 베이크  (입력: 1단계가 저장한 Crawler_Rig.blend)
"$BLENDER" -b Crawler_Rig_base.blend --python animate.py

# 3. FBX + 텍스처를 Assets/ 로 내보내기
"$BLENDER" -b Crawler_Rig.blend --python export.py
```

스크립트 상단의 경로 상수를 자기 환경에 맞게 고친다. 2단계는 프레임마다 메시를
평가해 바닥 관통을 재기 때문에 몇 분 걸린다.

## 손으로 만지려면

`Crawler_Rig.blend` 는 **베이크가 끝난 상태**다. IK 컨스트레인트가 제거되어
있어서 발 컨트롤(`CTRL_FootIK.*`)을 움직여도 다리가 따라오지 않는다. 리그를
다시 쓰려면 `build_rig.py` 를 돌려 컨스트레인트가 살아 있는 상태를 새로 만들고
거기서 작업한다.

애니메이션을 손으로 고칠 거면 `animate.py` 의 값을 고치는 쪽이 낫다. 접지와
바닥 관통 검사가 스크립트 안에 들어 있어서, 손으로 키를 옮기면 그 검사를 다시
받지 못한다.
