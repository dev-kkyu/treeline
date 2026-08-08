"""ElevenLabs 로 장면별 배경음악을 만든다.

프롬프트는 `docs/game-design/treeline.md` 의 「여섯 장소」를 그대로 따른다. 장마다
플레이어가 하는 일이 다르고 그것이 곧 음악의 성격이다 — 2장과 6장은 무력한
구간이라 움직임이 거의 없고, 3~5 장은 싸우므로 추진력이 있다.

효과음과 미터가 다르다. 음악은 **분당 900 크레딧**(초당 15)으로 훨씬 비싸다.
그래서 --dry-run 으로 예산을 먼저 본다.

전부 instrumental 이다. 가사가 붙으면 공포가 아니라 뮤직비디오가 된다.

    python docs/sound/gen_music.py --dry-run
    python docs/sound/gen_music.py
    python docs/sound/gen_music.py --only ch4-nest --overwrite
"""

import argparse
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request

ENDPOINT = "https://api.elevenlabs.io/v1/music"
CREDITS_PER_SECOND = 15          # 분당 900
OUT_DIR = "Treeline/Assets/Game/Audio/Music"

# 모든 프롬프트에 붙는 꼬리표. 목소리와 드럼이 끼면 공포 톤이 깨진다
INSTRUMENTAL = "fully instrumental, no vocals, no singing, no lyrics"

# (이름, 프롬프트, 초)
TRACKS = [
    ("title-theme",
     "Slow ominous orchestral drone for a horror game title screen. Low "
     "sustained strings, distant metallic resonance, a few sparse piano "
     "notes. Cold and vast, a mountain above the treeline. " + INSTRUMENTAL,
     60),

    # 1장 관제동 — 아무도 없다. 위협이 아직 없고 부재만 있다
    ("ch1-control-room",
     "Very sparse ambient horror underscore. Quiet sustained low strings, "
     "faint electrical hum, an occasional single piano note with long decay. "
     "The feeling of a room someone left minutes ago. Almost no melody, "
     "no percussion. " + INSTRUMENTAL,
     90),

    # 2장 숙소동 — 무기가 없다. 숨고 버틴다
    ("ch2-hide",
     "Tense minimal horror underscore for hiding in the dark. Slow pulsing "
     "low bass like a heartbeat, one thin high string harmonic holding "
     "tension, barely any movement. Claustrophobic corridor. " + INSTRUMENTAL,
     90),

    # 3장 연료 갱도 — 처음 쏜다. 지하, 좁고 리듬이 생긴다
    ("ch3-tunnel-combat",
     "Aggressive industrial horror combat music. Driving low percussion, "
     "dissonant brass stabs, rhythmic metallic clangs. Underground pipe "
     "tunnel, urgent and cornered. " + INSTRUMENTAL,
     60),

    # 4장 발전동 — 둥지. 유기물로 덮인 공간
    ("ch4-nest",
     "Wet organic horror underscore. Detuned strings sliding microtonally, "
     "slow breathing low drones, unsettling fleshy texture. The feeling of "
     "standing inside something alive. " + INSTRUMENTAL,
     90),

    # 4장 조명이 켜지는 순간. 어둠이 걷히며 오히려 더 무서워진다
    ("power-restored",
     "Ominous revelation cue. Low brass and strings swelling upward into a "
     "bright dissonant sustained chord, then holding. The moment the lights "
     "come on and reveal what was always there. " + INSTRUMENTAL,
     30),

    # 5장 외부 철탑 — 눈보라. 멈추면 죽는다. 공포의 색이 뒤집힌다
    ("ch5-blizzard",
     "Driving cold horror chase music. Howling high-register string noise, "
     "propulsive low ostinato, relentless forward motion through a whiteout "
     "blizzard. Bright and harsh rather than dark. " + INSTRUMENTAL,
     60),

    # 6장 삭도 승강장 — 죽지 않는 것. 다시 무력해진다
    ("ch6-colossus",
     "Overwhelming dread underscore for an unkillable giant. Massive slow "
     "low brass, deep sub bass pulses, distant groaning metal under strain. "
     "Crushing scale, the sense of something that cannot be fought. "
     + INSTRUMENTAL,
     75),

    # 발각 순간. 짧고 날카롭게 끊는다
    ("chase-stinger",
     "Short horror chase stinger. Sudden dissonant string burst into fast "
     "panicked tremolo, sharp attack, cuts off abruptly. " + INSTRUMENTAL,
     20),

    # 엔딩. 탈출했지만 그것은 죽지 않았다
    ("ending-descent",
     "Cold melancholic outro. Slow descending strings under a fragile solo "
     "cello, relief undercut by unease, thin mountain wind quality. "
     + INSTRUMENTAL,
     60),
]


def load_key(key_file):
    raw = os.environ.get("ELEVENLABS_API_KEY")
    if not raw:
        path = pathlib.Path(key_file).expanduser()
        if not path.is_file():
            sys.exit(
                f"API 키가 없다. ELEVENLABS_API_KEY 를 설정하거나 "
                f"{path} 에 키를 넣어라."
            )
        raw = path.read_text(encoding="utf-8")

    lines = [ln.strip() for ln in raw.splitlines()]
    lines = [ln for ln in lines if ln and not ln.startswith("#")]
    if not lines:
        sys.exit(f"{key_file} 이 비어 있다.")
    key = lines[0]
    if not key.startswith("sk_") and "=" in key:
        key = key.split("=", 1)[1]
    return key.strip().strip("\"'")


def compose(key, name, prompt, seconds, out_dir, fmt, overwrite):
    dest = pathlib.Path(out_dir) / f"{name}.mp3"
    if dest.exists() and not overwrite:
        print(f"  건너뜀 (이미 있음) {dest.name}")
        return 0

    body = {
        "prompt": prompt,
        "music_length_ms": int(seconds * 1000),
        "output_format": fmt,
        "model_id": "music_v2",
    }
    req = urllib.request.Request(
        ENDPOINT,
        data=json.dumps(body).encode("utf-8"),
        headers={"xi-api-key": key, "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=600) as res:
            audio = res.read()
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:500]
        print(f"  [{e.code}] {name} 실패: {detail}")
        return 0
    except urllib.error.URLError as e:
        print(f"  [연결 실패] {name}: {e.reason}")
        return 0

    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(audio)
    cost = int(seconds * CREDITS_PER_SECOND)
    print(f"  {dest.name:24} {len(audio)/1024:6.0f} KB  {seconds}s  ~{cost} 크레딧")
    return cost


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=OUT_DIR)
    ap.add_argument("--key-file", default="elevenlabs.local.key")
    ap.add_argument("--format", default="mp3_44100_128",
                    help="Starter 는 128 까지. 192 는 Creator 이상")
    ap.add_argument("--only", action="append", help="이름 지정. 여러 번 가능")
    ap.add_argument("--overwrite", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    todo = TRACKS
    if args.only:
        todo = [t for t in todo if t[0] in args.only]
    if not todo:
        sys.exit("생성할 항목이 없다.")

    total_sec = sum(t[2] for t in todo)
    planned = int(total_sec * CREDITS_PER_SECOND)
    print(f"{len(todo)}개 · {total_sec}초({total_sec/60:.1f}분) · "
          f"예상 약 {planned} 크레딧")

    if args.dry_run:
        for name, _, seconds in todo:
            print(f"  {name:24} {seconds:4d}s  ~{seconds*CREDITS_PER_SECOND} 크레딧")
        return

    key = load_key(args.key_file)
    spent = 0
    for name, prompt, seconds in todo:
        spent += compose(key, name, prompt, seconds, args.out, args.format,
                         args.overwrite)
    print(f"완료. 약 {spent} 크레딧 사용")


if __name__ == "__main__":
    main()
