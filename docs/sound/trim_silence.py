"""단발성 효과음 앞뒤의 무음을 잘라낸다.

ElevenLabs 는 요청한 길이를 꽉 채워 돌려주므로, 발소리처럼 짧은 소리는 앞에
무음이 붙어 나온다. 그대로 재생하면 트리거와 소리 사이에 지연이 생긴다.
코드에서 오프셋을 주는 방법도 있지만 파일에서 잘라내는 쪽이 확실하다.

앞은 10ms 만 남기고 자른다. 0 으로 딱 붙이면 첫 파형이 잘려 딸깍거린다.
뒤는 잔향이 죽는 지점까지 남긴다.

루프(-loop)는 건드리지 않는다. 길이가 바뀌면 이음매가 깨진다.

    python docs/sound/trim_silence.py --report
    python docs/sound/trim_silence.py --apply
"""

import argparse
import pathlib
import subprocess
import sys

import numpy as np

SR = 44100
ROOT = "Treeline/Assets/Game/Audio"
ONE_SHOT_DIRS = ["Footsteps", "Structure", "Electrical", "Interaction",
                 "Devices"]

HEAD_PAD_MS = 10        # 앞에 남길 여유
TAIL_PAD_MS = 60        # 뒤에 남길 잔향 여유
MUSIC_TAIL_PAD_MS = 400  # 음악은 잔향이 길어 더 남긴다
THRESHOLD = 0.02      # 피크 대비 이 비율을 넘으면 소리가 시작된 것으로 본다


def decode(path, channels):
    out = subprocess.run(
        ["ffmpeg", "-v", "quiet", "-i", str(path),
         "-f", "f32le", "-ac", str(channels), "-ar", str(SR), "-"],
        capture_output=True, check=True,
    ).stdout
    x = np.frombuffer(out, dtype="<f4")
    return x.reshape(-1, channels) if channels > 1 else x.reshape(-1, 1)


def channels_of(path):
    out = subprocess.run(
        ["ffprobe", "-v", "quiet", "-select_streams", "a:0",
         "-show_entries", "stream=channels", "-of", "csv=p=0", str(path)],
        capture_output=True, text=True,
    ).stdout.strip()
    try:
        return max(1, int(out))
    except ValueError:
        return 1


def bounds(x):
    """소리가 실제로 존재하는 구간의 [시작, 끝) 샘플 인덱스."""
    env = np.abs(x).max(axis=1)
    peak = env.max()
    if peak <= 0:
        return 0, len(env)
    loud = np.flatnonzero(env >= peak * THRESHOLD)
    if len(loud) == 0:
        return 0, len(env)
    return int(loud[0]), int(loud[-1]) + 1


def targets():
    root = pathlib.Path(ROOT)
    files = []
    for d in ONE_SHOT_DIRS:
        files += sorted((root / d).glob("*.mp3"))
        files += sorted((root / d).glob("*.wav"))
    return [f for f in files if "-loop" not in f.stem]


def trim_music(apply_it):
    """음악은 mp3 구간 복사로 자른다.

    효과음처럼 wav 로 풀면 90 초짜리 하나가 15MB 라 저장소가 감당이 안 되고,
    mp3 로 다시 인코딩하면 손실이 한 세대 쌓인다. ffmpeg 의 스트림 복사는
    프레임 경계에서 자르므로 26ms 안팎의 오차만 있고 재인코딩이 없다.

    앞뒤를 모두 자른다. 앞은 큐가 제때 터지게 하려고, 뒤는 루프에 구멍이
    생기지 않게 하려고다. 잔향이 잘리지 않도록 뒤 여유는 넉넉히 둔다.
    """
    root = pathlib.Path(ROOT) / "Music"
    files = sorted(root.glob("*.mp3"))
    if not files:
        sys.exit(f"{root} 에 mp3 가 없다.")

    for path in files:
        x = decode(path, 2)
        start, end = bounds(x)
        head_ms = start / SR * 1000
        tail_ms = (len(x) - end) / SR * 1000

        if not apply_it:
            print(f"  {path.stem:22} 앞 {head_ms:6.0f}ms  뒤 {tail_ms:6.0f}ms")
            continue

        ss = max(0.0, start / SR - HEAD_PAD_MS / 1000)
        to = min(len(x) / SR, end / SR + MUSIC_TAIL_PAD_MS / 1000)
        tmp = path.with_suffix(".trim.mp3")
        subprocess.run(
            ["ffmpeg", "-v", "quiet", "-y", "-ss", f"{ss:.3f}", "-to",
             f"{to:.3f}", "-i", str(path), "-c", "copy", str(tmp)],
            check=True,
        )
        tmp.replace(path)
        print(f"  {path.stem:22} 앞 {ss*1000:5.0f}ms · 뒤 "
              f"{(len(x)/SR - to)*1000:6.0f}ms 잘라냄  "
              f"{len(x)/SR:.1f}s -> {to - ss:.1f}s")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--report", action="store_true", help="측정만 한다")
    ap.add_argument("--apply", action="store_true", help="실제로 자른다")
    ap.add_argument("--music", action="store_true",
                    help="Music/ 을 대상으로 한다. mp3 구간 복사로 자른다")
    args = ap.parse_args()
    if not (args.report or args.apply):
        args.report = True

    if args.music:
        trim_music(args.apply)
        return

    files = targets()
    if not files:
        sys.exit(f"{ROOT} 아래에 단발성 파일이 없다.")

    total_saved = 0.0
    for path in files:
        ch = channels_of(path)
        x = decode(path, ch)
        start, end = bounds(x)
        head_ms = start / SR * 1000
        tail_ms = (len(x) - end) / SR * 1000

        if not args.apply:
            flag = "  <-- 지연 큼" if head_ms >= 50 else ""
            print(f"  {path.name:34} 앞 무음 {head_ms:6.0f}ms  "
                  f"뒤 여백 {tail_ms:6.0f}ms{flag}")
            total_saved += head_ms
            continue

        s = max(0, start - int(SR * HEAD_PAD_MS / 1000))
        e = min(len(x), end + int(SR * TAIL_PAD_MS / 1000))
        y = x[s:e]

        # wav 로 저장한다. mp3 로 다시 인코딩하면 손실이 한 세대 더 쌓인다
        dest = path.with_suffix(".wav")
        pcm = np.clip(y, -1.0, 1.0)
        pcm = (pcm * 32767.0).astype("<i2").tobytes()
        subprocess.run(
            ["ffmpeg", "-v", "quiet", "-y", "-f", "s16le", "-ar", str(SR),
             "-ac", str(ch), "-i", "-", str(dest)],
            input=pcm, check=True,
        )
        cut = s / SR * 1000
        total_saved += cut
        print(f"  {path.name:34} 앞 {cut:5.0f}ms 잘라냄  "
              f"{len(x)/SR:.2f}s -> {len(y)/SR:.2f}s  -> {dest.name}")

    print(f"\n{len(files)}개 · 앞 무음 평균 {total_saved/len(files):.0f}ms")
    if args.apply:
        print("원본 mp3 는 남아 있다. Unity 에서 정리한 뒤 지운다.")


if __name__ == "__main__":
    main()
