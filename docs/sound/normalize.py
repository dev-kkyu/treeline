"""사운드 음량을 카테고리별로 맞춘다.

생성기는 요청할 때마다 음량이 제각각인 결과를 준다. 같은 발소리 3 종이 서로 다르게
들리면 번갈아 재생하는 의미가 없고, 장이 넘어갈 때 음악 크기가 튀면 플레이어가
설정에서 볼륨을 다시 만지게 된다.

**측정 방식이 길이에 따라 갈린다.** 이건 취향이 아니라 측정의 한계다.

- 긴 것(앰비언스 · 음악)은 **LUFS**(EBU R128). 사람이 느끼는 크기는 파형의
  최대값이 아니라 누적된 에너지라서 피크로 맞추면 잔잔한 눈보라와 날카로운
  문소리가 같은 숫자인데 전혀 다르게 들린다.
- 짧은 것(단발성)은 **RMS**. R128 은 3 초 이상을 전제로 게이트를 걸기 때문에,
  1 초짜리에 쓰면 게이트가 신호 대부분을 버려 값이 무너진다. 실제로 같은
  콘크리트 발소리 3 종이 -66 · -54 · -43 LUFS 로 나왔다. 셋은 비슷한 소리다.

피크는 어느 쪽이든 천장으로만 쓴다. 목표 음량에 닿으려고 클리핑하지는 않는다.

    python docs/sound/normalize.py --report
    python docs/sound/normalize.py --apply
"""

import argparse
import json
import math
import pathlib
import subprocess
import sys

import numpy as np

ROOT = "Treeline/Assets/Game/Audio"
SR = 44100

# 카테고리별 목표. 앰비언스가 가장 낮은 것은 계속 깔리는 바닥이기 때문이다.
# 그 위에 발소리와 음악이 얹혀야 한다.
#   lufs  : 긴 것의 목표 (LUFS)
#   rms   : 짧은 것의 목표 (dBFS RMS)
TARGETS = {
    "Ambience":   {"lufs": -23.0, "rms": -26.0},
    "Music":      {"lufs": -18.0, "rms": -20.0},
    "Footsteps":  {"lufs": -22.0, "rms": -22.0},
    "Structure":  {"lufs": -22.0, "rms": -22.0},
    "Electrical": {"lufs": -22.0, "rms": -22.0},
    "Interaction": {"lufs": -22.0, "rms": -22.0},
    "Devices":    {"lufs": -22.0, "rms": -22.0},
    # 적 소리는 조금 크게 둔다. 위치를 알려주는 것이 곧 게임 규칙이다.
    # 세 종이 한 폴더에 있으므로 항목도 하나다
    "Enemy":      {"lufs": -20.0, "rms": -20.0},
    # 메뉴 소리는 장면 위에 얹히는 것이 아니라 그 자체가 피드백이다. 다만
    # 눌릴 때마다 나므로 효과음보다 한 단계 낮게 둬야 거슬리지 않는다
    "UI":         {"lufs": -24.0, "rms": -24.0},
    # 무기 카테고리의 기본값은 **기계 조작음**(슬라이드 · 탄창 · 펌프) 기준이다.
    # 총성은 아래 FILE_TARGETS 가 따로 끌어올린다
    "Weapon":     {"lufs": -22.0, "rms": -24.0},
    # 주인공은 짧은 것(신음 · 사망)과 긴 것(지친 호흡 루프)이 갈린다. 루프는
    # 5 초를 넘어 LUFS 로 재므로 두 값이 그대로 두 목표가 된다
    "Protagonist": {"lufs": -26.0, "rms": -22.0},
}

# 같은 카테고리 안에서 목표가 다른 것.
#
# 총성과 탄창 딸깍은 한 폴더에 있어야 하지만(무기는 한 담당이다) 같은 음량이면
# 안 된다 — 카테고리로 맞추면 딸깍이 총성만큼 커진다. 폭발 · 제설포 · 눈사태도
# 같다. 장치 카테고리의 기준은 모터와 셔터인데, **그 장에서 가장 큰 사건**을
# 레버와 같은 크기로 두면 최후의 수단이 최후로 들리지 않는다.
#
# `max_gain` 은 MAX_GAIN_DB 를 이 파일에서만 푼다. 15 dB 상한은 잡음까지 끌어올리는
# 것을 막으려는 값인데, 총성은 생성기가 유난히 작게 주면서도 **잡음 여유가 44 dB**
# 라 상한이 이유 없이 물린다. 풀기 전에 여유를 재고 푼다.
FILE_TARGETS = {
    "pistol-fire":            {"lufs": -16.0, "rms": -16.0, "max_gain": 24.0},
    "shotgun-fire":           {"lufs": -14.0, "rms": -14.0},
    "bullet-impact-concrete": {"lufs": -22.0, "rms": -22.0},
    "bullet-impact-metal":    {"lufs": -22.0, "rms": -22.0},
    # 호흡 루프도 max_gain 을 푼다. 네 판을 뽑았고 가장 나은 것이 −29 LUFS 다
    "player-breath-exhausted-loop": {"lufs": -26.0, "rms": -26.0, "max_gain": 17.0},
    # 4 장 축전지실. 폭발과 그 직후만 장치 기준보다 크게 둔다
    "hydrogen-explosion":     {"lufs": -14.0, "rms": -14.0},
    "battery-arc":            {"lufs": -18.0, "rms": -18.0},
    "debris-fall":            {"lufs": -20.0, "rms": -20.0},
    # 귀울림은 폭발이 끝난 뒤 혼자 남는 소리다. 크면 귀가 아프고 작으면 없는
    # 것이 되므로 장치 기준보다 낮게 두고 재생 쪽에서 올린다
    "explosion-ear-ring":     {"lufs": -26.0, "rms": -26.0},
    # 5 장 제설포와 눈사태. 산이 무너지는 소리가 발전기와 같은 크기일 수 없다
    "avalanche-cannon":       {"lufs": -16.0, "rms": -16.0},
    "avalanche-slide":        {"lufs": -16.0, "rms": -16.0},
}

# Audio/ 밖에 있는 카테고리
CATEGORY_DIRS = {
    "Enemy": "Treeline/Assets/Game/Enemy/Audio",
    "Weapon": "Treeline/Assets/Game/Player/Audio/Weapons",
    "Protagonist": "Treeline/Assets/Game/Player/Audio/Body",
}

PEAK_CEILING_DB = -1.0   # 이 위로는 올리지 않는다
LONG_SEC = 5.0           # 이보다 길면 LUFS, 짧으면 RMS
MAX_GAIN_DB = 15.0       # 이 이상 올리면 잡음까지 끌어올린다
MP3_BITRATE = "192k"     # 원본이 128k 라 재인코딩 손실을 최소화한다


def probe(path):
    out = subprocess.run(
        ["ffprobe", "-v", "quiet", "-select_streams", "a:0", "-show_entries",
         "stream=channels,duration", "-of", "json", str(path)],
        capture_output=True, text=True,
    ).stdout
    s = json.loads(out)["streams"][0]
    return float(s.get("duration", 0)), int(s.get("channels", 1))


def decode(path, channels):
    out = subprocess.run(
        ["ffmpeg", "-v", "quiet", "-i", str(path), "-f", "f32le",
         "-ac", str(channels), "-ar", str(SR), "-"],
        capture_output=True, check=True,
    ).stdout
    x = np.frombuffer(out, dtype="<f4").astype(np.float64)
    return x.reshape(-1, channels) if channels > 1 else x.reshape(-1, 1)


def db(v):
    return 20 * math.log10(v) if v > 1e-9 else -99.0


def measure_lufs(path):
    res = subprocess.run(
        ["ffmpeg", "-v", "info", "-i", str(path), "-af",
         "loudnorm=I=-23:TP=-1:print_format=json", "-f", "null", "-"],
        capture_output=True, text=True,
    )
    err = res.stderr
    start = err.rfind("{")
    if start == -1:
        return None
    try:
        v = float(json.loads(err[start:err.rfind("}") + 1]).get("input_i"))
        return v if math.isfinite(v) and v > -70 else None
    except (ValueError, TypeError, KeyError):
        return None


def analyse(category, path):
    dur, ch = probe(path)
    x = decode(path, ch)
    peak = db(float(np.abs(x).max()))
    rms = db(float(np.sqrt(np.mean(x ** 2))))
    t = FILE_TARGETS.get(path.stem, TARGETS[category])

    lufs = measure_lufs(path) if dur >= LONG_SEC else None
    if lufs is not None:
        gain, basis, level = t["lufs"] - lufs, "LUFS", lufs
    else:
        gain, basis, level = t["rms"] - rms, "RMS", rms

    note, limit = "", False
    max_gain = t.get("max_gain", MAX_GAIN_DB)
    if gain > max_gain:
        gain, note = max_gain, "이득 상한"
    headroom = PEAK_CEILING_DB - peak
    if gain > headroom:
        # 순간음은 피크가 먼저 천장에 닿아 목표 음량까지 못 올라간다. 이득을
        # 깎으면 같은 발소리 3 종의 크기가 다시 어긋나므로, 목표대로 올리고
        # 리미터로 꼭지만 누른다. 차이가 작을 때는 리미터를 걸지 않는다.
        if gain - headroom >= 1.5:
            note, limit = "리미터", True
        else:
            gain, note = headroom, "피크 천장"
    return dict(dur=dur, ch=ch, peak=peak, rms=rms, level=level,
                basis=basis, gain=gain, note=note, limit=limit)


def apply_gain(path, gain_db, limit=False):
    suffix = path.suffix.lower()
    tmp = path.with_name(path.stem + ".norm" + suffix)
    af = f"volume={gain_db:.2f}dB"
    if limit:
        ceiling = 10 ** (PEAK_CEILING_DB / 20)
        af += (f",alimiter=level_in=1:level_out=1:limit={ceiling:.4f}"
               ":attack=1:release=40:level=disabled")
    cmd = ["ffmpeg", "-v", "quiet", "-y", "-i", str(path), "-af", af]
    if suffix == ".mp3":
        cmd += ["-c:a", "libmp3lame", "-b:a", MP3_BITRATE]
    else:
        cmd += ["-c:a", "pcm_s16le"]
    cmd.append(str(tmp))
    subprocess.run(cmd, check=True)
    tmp.replace(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--report", action="store_true")
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--category", action="append",
                    help="카테고리 지정. 여러 번 가능")
    # IMPORTANT: --category 만으로 --apply 하면 **그 폴더의 모든 파일**을 다시
    # 쓴다. 대부분은 이득이 0 이라 건너뛰지만, 남이 방금 다시 뽑아 아직 음량을
    # 안 맞춘 파일이 있으면 그것까지 같이 맞춰 버린다. 실제로 그렇게 남의
    # ice-melt-crack 을 11.6 dB 올렸다. 새로 뽑은 것만 맞출 때는 --only 로 좁힌다.
    ap.add_argument("--only", action="append",
                    help="파일 이름(확장자 없이) 지정. 여러 번 가능")
    args = ap.parse_args()
    if not (args.report or args.apply):
        args.report = True

    root = pathlib.Path(ROOT)
    spread = {}
    for category in TARGETS:
        if args.category and category not in args.category:
            continue
        d = (pathlib.Path(CATEGORY_DIRS[category]) if category in CATEGORY_DIRS
             else root / category)
        if not d.is_dir():
            continue
        files = sorted(list(d.glob("*.mp3")) + list(d.glob("*.wav")))
        if args.only:
            wanted = set(args.only)
            files = [f for f in files if f.stem in wanted]
        if not files:
            continue
        t = TARGETS[category]
        print(f"\n[{category}]  긴 것 {t['lufs']} LUFS · 짧은 것 {t['rms']} dBFS RMS")

        for path in files:
            a = analyse(category, path)
            if args.apply:
                if abs(a["gain"]) >= 0.1:
                    apply_gain(path, a["gain"], a["limit"])
                after = analyse(category, path)
                spread.setdefault(category, []).append(after["level"])
                print(f"  {path.stem:26} {a['gain']:+6.1f} dB  "
                      f"→ {after['level']:7.1f} {after['basis']}"
                      + (f"  ({a['note']})" if a["note"] else ""))
            else:
                spread.setdefault(category, []).append(a["level"])
                print(f"  {path.stem:26} {a['dur']:5.1f}s  "
                      f"{a['level']:7.1f} {a['basis']:4}  피크 {a['peak']:6.1f}"
                      f"  → {a['gain']:+6.1f} dB"
                      + (f"  ({a['note']})" if a["note"] else ""))

    print("\n=== 카테고리 안의 편차 (좁을수록 고름) ===")
    for category, vals in spread.items():
        print(f"  {category:12} 최소 {min(vals):7.1f}  최대 {max(vals):7.1f}"
              f"  폭 {max(vals) - min(vals):5.1f} dB")


if __name__ == "__main__":
    main()
