# Treeline 파티클 VFX 키트

> 상태: 데스크톱 WebGL용 Unity ParticleSystem 프리팹 제작 · 2026-08-08

확정 기획의 「불 · 눈 · 연기는 시뮬레이션하지 않고 파티클과 데칼로 만든다」를
바로 구현할 수 있도록 공용 텍스처와 프리팹으로 묶었다. 모바일 대응용 저사양
변형은 만들지 않았지만, 심사자가 브라우저에서 실행하므로 반투명 오버드로우와
동시 파티클 수는 제한한다.

![VFX 키트 프리뷰](previews/vfx-kit-preview-1920x1080.jpg)

## 폴더

```text
docs/vfx-assets/
├─ masters/chroma/   Codex 내장 이미지 생성 원본
├─ masters/alpha/    크로마 제거 고해상도 마스터
├─ runtime/textures/ Unity 복사용 9개 PNG
├─ previews/         공유용 연락판
├─ tools/            런타임 텍스처와 프리뷰 재생성
└─ generation-prompts.md

Assets/Game/VFX/
├─ Textures/         파티클 텍스처
├─ Materials/        URP Particles/Unlit 머티리얼
└─ Prefabs/          장면에 끌어다 놓는 ParticleSystem 프리팹
```

## 프리팹과 기획 연결

| 프리팹 | 쓰는 장면 | 담당자가 할 일 |
| --- | --- | --- |
| `PS_IndoorFrostDust` | 1·2·3·4·6장 실내 냉기 | 방 크기에 맞게 Box Shape만 조절 |
| `PS_Blizzard` | 5장 외부 철탑, 6장 승강장 | 플레이어 주변에 배치하고 바람 방향 회전 |
| `PS_MuzzleFlash` | 3장 권총, 5장 산탄총 | 무기 총구 Transform 아래에 두고 발사 때 `Play()` |
| `PS_FuelFire` | 3장 고인 연료 인화 | 불붙은 구간 폭에 맞게 Scale 조절 |
| `PS_ElectricalSparks` | 4장 차단기·전력 복구 | 사건 위치에서 한 번 `Play()` |
| `PS_ExplosionBurst` | 4장 수소, 6장 눈사태 제설포 | 폭발 위치에서 한 번 `Play()` + 별도 카메라 흔들림 |

씬 소유자가 프리팹 내부 수치를 복제해 바꾸지 않는다. 공통 수정을 해야 하면 원본
프리팹을 고치고, 장별 차이는 인스턴스의 Shape 크기와 Transform Scale로 해결한다.

## 텍스처

| 파일 | 크기 | 용도 |
| --- | ---: | --- |
| `ice-burst.png` | 256×256 | 폭발 순간의 얼음 파열 중심 |
| `fuel-flame.png` | 256×256 | 연료 화염 혀 |
| `muzzle-flash.png` | 256×256 | 권총·산탄총 단발 섬광 |
| `snow-streak.png` | 256×64 | 바람에 눕는 눈보라 |
| `soft-disc.png` | 256×256 | 서리 먼지와 폭발광 공용 마스크 |
| `spark-streak.png` | 256×64 | 전기 스파크 |
| `ember.png` | 64×64 | 불씨 |
| `smoke-puff.png` | 256×256 | 연료·폭발 연기 |
| `debris-shard.png` | 128×128 | 폭발 파편 |

세 회화 텍스처는 Codex 내장 이미지 생성 뒤 로컬 크로마 제거를 거쳤다. 나머지는
[`tools/build_assets.py`](tools/build_assets.py)가 결정적으로 생성하므로 수정 후 같은
결과를 다시 만들 수 있다.

## WebGL 운용 기준

- VFX Graph가 아니라 Unity 기본 `ParticleSystem`만 사용한다.
- 눈보라는 플레이어를 따라가는 로컬 볼륨 하나로 쓰고 맵 전체에 깔지 않는다.
- 기본 최대치는 눈보라 900, 실내 먼지 300, 나머지 순간 효과 120 이하로 둔다.
- Collision, Trails, Particle Light는 기본으로 끈다. 폭발 피해는 VFX가 아니라 기존
  게임 로직이 판정한다.
- 투명 파티클이 화면 전체를 네 겹 이상 덮지 않게 한다. 불과 연기는 서로 다른
  파티클이어도 같은 좁은 영역에서만 재생한다.
- 1920×1080 WebGL Development Build의 Profiler와 브라우저 Release Build를 둘 다
  확인한다. 에디터 FPS만으로 완료 처리하지 않는다.

## 재생성

```powershell
python docs/vfx-assets/tools/build_assets.py
```

스크립트는 `masters/alpha/`를 건드리지 않고 `runtime/textures/`와 프리뷰를 다시
만든다. 생성 이미지 원본을 교체할 때는
[`generation-prompts.md`](generation-prompts.md)의 프롬프트를 사용한다.
