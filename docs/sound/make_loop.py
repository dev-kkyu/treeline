"""음악을 이음매 없는 루프로 만든다.

효과음 API 에는 `loop` 파라미터가 있어서 생성기가 알아서 이어 주지만 **음악 API
에는 없다.** 그래서 길게 뽑은 다음 여기서 직접 잇는다.

방법은 꼬리를 머리에 겹치는 것이다. 길이 L 짜리에서 마지막 C 초를 떼어 첫 C 초
위에 크로스페이드로 얹으면, 결과는 L-C 초짜리이고 끝에서 처음으로 넘어갈 때
파형이 이미 이어져 있다. 자르거나 페이드로 숨기는 것과 달리 **음량이 일정하게
유지**되므로 반복해도 출렁이지 않는다.

크로스페이드는 등파워(equal power) 로 건다. 선형으로 하면 겹치는 구간에서 에너지가
떨어져 한 바퀴마다 소리가 움푹 들어간다.

    python docs/sound/make_loop.py Treeline/Assets/Game/Audio/Music/x.mp3
    python docs/sound/make_loop.py <파일> --crossfade 3.0
"""

import argparse
import pathlib
import subprocess
import sys

import numpy as np

SR = 44100


def probe_channels(path):
    out = subprocess.run(
        ["ffprobe", "-v", "quiet", "-select_streams", "a:0", "-show_entries",
         "stream=channels", "-of", "csv=p=0", str(path)],
        capture_output=True, text=True,
    ).stdout.strip()
    try:
        return max(1, int(out))
    except ValueError:
        return 2


def decode(path, channels):
    out = subprocess.run(
        ["ffmpeg", "-v", "quiet", "-i", str(path), "-f", "f32le",
         "-ac", str(channels), "-ar", str(SR), "-"],
        capture_output=True, check=True,
    ).stdout
    x = np.frombuffer(out, dtype="<f4").astype(np.float64)
    return x.reshape(-1, channels) if channels > 1 else x.reshape(-1, 1)


def make_loop(x, crossfade_sec):
    c = int(SR * crossfade_sec)
    if len(x) <= c * 2:
        sys.exit("크로스페이드 구간이 파일 길이에 비해 너무 길다.")

    head, tail = x[:c], x[-c:]
    t = np.linspace(0.0, 1.0, c, endpoint=False)[:, None]
    # 등파워. 선형으로 하면 겹치는 동안 에너지가 떨어진다
    blended = head * np.sqrt(t) + tail * np.sqrt(1.0 - t)

    out = x[:len(x) - c].copy()
    out[:c] = blended
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("path")
    ap.add_argument("--crossfade", type=float, default=4.0, help="초")
    ap.add_argument("--bitrate", default="192k")
    args = ap.parse_args()

    src = pathlib.Path(args.path)
    if not src.is_file():
        sys.exit(f"{src} 가 없다.")

    ch = probe_channels(src)
    x = decode(src, ch)
    before = len(x) / SR
    y = make_loop(x, args.crossfade)

    pcm = np.clip(y, -1.0, 1.0)
    pcm = (pcm * 32767.0).astype("<i2").tobytes()
    tmp = src.with_name(src.stem + ".loop" + src.suffix)
    subprocess.run(
        ["ffmpeg", "-v", "quiet", "-y", "-f", "s16le", "-ar", str(SR),
         "-ac", str(ch), "-i", "-", "-c:a", "libmp3lame", "-b:a", args.bitrate,
         str(tmp)],
        input=pcm, check=True,
    )
    tmp.replace(src)
    print(f"{src.name}  {before:.1f}s -> {len(y)/SR:.1f}s  "
          f"(크로스페이드 {args.crossfade:.1f}s)")
    print("check_loop.py 로 이음매를 확인한다.")


if __name__ == "__main__":
    main()
