"""루프 사운드가 실제로 이음매 없이 이어지는지 파형으로 검사한다.

귀로 판단하기 전에 객관적 근거를 본다. 보는 것은 두 가지다.

1. 페이드 — 앞뒤 50ms 가 전체보다 조용하면 페이드인/아웃이 걸린 것이다.
   루프를 돌리면 매 바퀴마다 소리가 꺼졌다 켜져서 반드시 티가 난다.
2. 이음매 단차 — 끝 샘플에서 첫 샘플로 넘어갈 때의 진폭 점프를 파일 내부의
   평균 점프와 비교한다. 훨씬 크면 그 지점에서 "툭" 소리가 난다.

    python docs/sound/check_loop.py docs/sound/test
"""

import pathlib
import subprocess
import sys

import numpy as np

SR = 44100
EDGE_MS = 50


def decode(path):
    out = subprocess.run(
        ["ffmpeg", "-v", "quiet", "-i", str(path),
         "-f", "f32le", "-ac", "1", "-ar", str(SR), "-"],
        capture_output=True, check=True,
    ).stdout
    return np.frombuffer(out, dtype="<f4")


def rms(x):
    return float(np.sqrt(np.mean(x.astype(np.float64) ** 2))) if len(x) else 0.0


def check(path):
    x = decode(path)
    if len(x) < SR // 4:
        return f"{path.name}: 너무 짧다"

    edge = int(SR * EDGE_MS / 1000)
    head, tail, whole = rms(x[:edge]), rms(x[-edge:]), rms(x)

    # 페이드 판정: 가장자리가 전체의 절반도 안 되면 페이드로 본다
    def ratio(v):
        return v / whole if whole else 0.0
    fade_in, fade_out = ratio(head), ratio(tail)

    # 이음매 단차: 끝→처음 점프를 내부 점프 분포와 비교
    internal = float(np.mean(np.abs(np.diff(x))))
    seam = abs(float(x[0]) - float(x[-1]))
    seam_ratio = seam / internal if internal else 0.0

    verdict = []
    if fade_in < 0.5 or fade_out < 0.5:
        verdict.append("페이드 있음")
    if seam_ratio > 20:
        verdict.append("이음매 단차 큼")
    label = " · ".join(verdict) if verdict else "이음매 양호"

    return (f"{path.name:24} 길이 {len(x)/SR:5.1f}s  "
            f"앞 {fade_in:4.2f} 뒤 {fade_out:4.2f} (1.0 이 이상적)  "
            f"단차 x{seam_ratio:6.1f} (20 이하면 양호)  → {label}")


def main():
    root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "docs/sound/test")
    files = sorted(root.glob("*.mp3"))
    if not files:
        sys.exit(f"{root} 에 mp3 가 없다.")
    print("[루프용]")
    for p in files:
        if "loop" in p.stem:
            print("  " + check(p))
    print("[단발성. 페이드가 있어도 문제 없다]")
    for p in files:
        if "loop" not in p.stem:
            print("  " + check(p))


if __name__ == "__main__":
    main()
