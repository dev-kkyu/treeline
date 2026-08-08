"""ElevenLabs 로 게임 사운드를 배치 생성해 Unity 폴더에 바로 떨군다.

프롬프트를 코드에 두는 이유는 재생성 때문이다. 마음에 안 드는 것만 --only 로
다시 뽑을 수 있고, 나중에 톤을 바꾸고 싶으면 프롬프트만 고쳐 전부 다시 돌린다.

루프 프롬프트에 "constant level, no fade" 를 넣은 것은 실측 결과다. 이 문구가
없으면 앞뒤가 잦아들어 루프를 돌 때마다 음량이 출렁인다. check_loop.py 로 확인한다.

키는 ELEVENLABS_API_KEY 환경변수, 없으면 --key-file 의 파일에서 읽는다.
키 파일은 저장소 루트의 elevenlabs.local.key 이고 gitignore 로 제외된다.

    python docs/sound/gen_sfx.py
    python docs/sound/gen_sfx.py --only heater-hum-loop --only vent-fan-loop
    python docs/sound/gen_sfx.py --category Ambience
    python docs/sound/gen_sfx.py --dry-run
"""

import argparse
import json
import os
import pathlib
import sys
import urllib.error
import urllib.request

ENDPOINT = "https://api.elevenlabs.io/v1/sound-generation"
CREDITS_PER_SECOND = 40
OUT_ROOT = "Treeline/Assets/Game/Audio"

# 적 · 무기 소리는 Audio/ 가 아니라 담당 폴더로 간다. architecture.md 의 규칙이다.
CATEGORY_DIRS = {
    "Crawler": "Treeline/Assets/Game/Enemy/Audio",
    "Clinger": "Treeline/Assets/Game/Enemy/Audio",
    "Colossus": "Treeline/Assets/Game/Enemy/Audio",
}


def dest_dir(out_root, category):
    override = CATEGORY_DIRS.get(category)
    return pathlib.Path(override) if override else pathlib.Path(out_root) / category

# 루프에 공통으로 붙는 꼬리표. 페이드를 막는다.
STEADY = "constant level throughout, no fade in, no fade out, no music"

# (카테고리, 이름, 프롬프트, 초, 루프)
PROMPTS = [
    # --- 지속 배경음. 장면에 깔린다 -------------------------------------
    ("Ambience", "blizzard-outside-loop",
     "Howling blizzard wind sweeping across an exposed mountain ridge, "
     "dense snow, deep roaring gusts, " + STEADY, 15.0, True),
    ("Ambience", "blizzard-muffled-loop",
     "Blizzard wind heard from inside a concrete building, muffled through "
     "thick walls, low rumble with distant whistling, " + STEADY, 15.0, True),
    ("Ambience", "room-tone-loop",
     "Air handling unit noise filling a concrete corridor, steady broadband "
     "rush of forced air with a low rumble underneath, recorded close to the "
     "vent, " + STEADY, 15.0, True),
    ("Ambience", "heater-hum-loop",
     "Old industrial heater humming and vibrating, steady low mechanical "
     "drone with a faint metallic rattle, " + STEADY, 15.0, True),
    ("Ambience", "vent-fan-loop",
     "Large ventilation fan running steadily inside a metal duct, deep "
     "resonant whirring with airflow, " + STEADY, 15.0, True),
    ("Ambience", "generator-rumble-loop",
     "Diesel generator running in an adjacent room, low chugging rumble "
     "through a wall, " + STEADY, 15.0, True),
    ("Ambience", "fluorescent-buzz-loop",
     "Fluorescent tube light buzzing loudly right overhead, strong electrical "
     "hum and ballast rattle, close microphone, " + STEADY, 12.0, True),
    ("Ambience", "radio-static-loop",
     "Dead analog shortwave radio static hiss with faint intermittent "
     "squelch pops, no voices, " + STEADY, 12.0, True),

    # --- 발소리. 표면별로 나눈다 ------------------------------------------
    ("Footsteps", "footstep-metal-grate-01",
     "Single heavy boot stamping on a cold metal grating floor, loud sharp "
     "clang, short reverb tail, close microphone", 1.5, False),
    ("Footsteps", "footstep-metal-grate-02",
     "Single boot scuffing then landing on metal grating, gritty scrape into "
     "a dull metallic ring", 1.5, False),
    ("Footsteps", "footstep-metal-grate-03",
     "Single boot step on loose metal grating, panel rattles slightly under "
     "the weight", 1.5, False),
    ("Footsteps", "footstep-concrete-01",
     "Foley recording of one footstep on a concrete floor, shoe hitting the "
     "microphone level, loud dry percussive hit, no reverb tail", 1.5, False),
    ("Footsteps", "footstep-concrete-02",
     "Single boot step grinding on gritty concrete, loud sand and grit "
     "crunching under the sole, close microphone", 1.5, False),
    ("Footsteps", "footstep-concrete-03",
     "Single deliberate boot step on concrete, firm heel strike then toe, "
     "tight dry recording, close microphone", 1.5, False),
    ("Footsteps", "footstep-snow-01",
     "Single boot step into deep fresh snow, dry squeaky crunch", 1.5, False),
    ("Footsteps", "footstep-snow-02",
     "Single boot step onto packed icy snow, brittle crust breaking", 1.5,
     False),
    ("Footsteps", "footstep-snow-03",
     "Single boot pulling out of deep snow then pressing down again, soft "
     "muffled crunch", 1.5, False),
    ("Footsteps", "footstep-wood-01",
     "Single boot step on an old wooden floorboard, hollow knock with a "
     "faint creak", 1.5, False),
    ("Footsteps", "footstep-wood-02",
     "Single boot step on a loose wooden plank, long groaning creak under "
     "weight", 1.5, False),
    ("Footsteps", "footstep-wood-03",
     "Single boot step on dry wood, sharp hollow knock, close microphone", 1.5, False),

    # --- 건물. 문 · 계단 · 창 --------------------------------------------
    ("Structure", "door-steel-open",
     "Heavy steel door slowly creaking open on rusted hinges, long metallic "
     "groan", 4.0, False),
    ("Structure", "door-steel-close",
     "Heavy steel door swinging shut and slamming into its frame, loud "
     "metallic boom with latch clack", 3.0, False),
    ("Structure", "door-handle-rattle",
     "Someone rattling a locked steel door handle twice, metal clatter, door "
     "does not open", 2.0, False),
    ("Structure", "metal-stair-creak",
     "Weight shifting on an old metal staircase, structural creak and faint "
     "bolt groan", 3.0, False),
    ("Structure", "window-wind-rattle",
     "Strong wind gust violently rattling a loose window pane in its frame, "
     "glass buzzing hard, wind whistling through the gap, close microphone", 4.0, False),
    ("Structure", "pipe-clank",
     "Water pipe hammering inside a wall, two loud sharp metallic clanks "
     "ringing hollow, close microphone", 2.0, False),
    ("Structure", "roof-snow-shift",
     "Mass of packed snow sliding off a metal roof and thudding onto the "
     "ground outside", 4.0, False),

    # --- 전기. 정전과 복구 -------------------------------------------------
    ("Electrical", "light-switch-click",
     "Single industrial light switch flipped, crisp plastic and metal click",
     1.5, False),
    ("Electrical", "breaker-throw",
     "Heavy electrical breaker lever thrown in a metal panel, solid chunky "
     "clunk", 2.0, False),
    ("Electrical", "power-down",
     "Building power cutting out, fluorescent lights and fans winding down "
     "into total silence", 4.0, False),
    ("Electrical", "power-up",
     "Building power coming back on, breaker thunk then fluorescent lights "
     "and fans spinning up to a steady hum", 4.0, False),
    ("Electrical", "fluorescent-flicker",
     "Fluorescent tube struggling to start, rapid stuttering electrical "
     "flicker and ticking", 3.0, False),

    # --- 상호작용. Lever · Pickup · Carriable 에 대응한다 -------------------
    ("Interaction", "lever-pull",
     "Large industrial lever being pulled down through its travel, metal "
     "handle grinding then locking into place with a solid clack, close "
     "microphone", 2.0, False),
    ("Interaction", "lever-locked",
     "Industrial lever jammed, metal straining against a stop, refuses to "
     "move, close microphone", 1.5, False),
    ("Interaction", "valve-turn",
     "Stiff iron valve wheel turning several rotations, metal squeal and "
     "grinding threads, close microphone", 3.0, False),
    ("Interaction", "pickup-item",
     "Picking up a metal object from a shelf, brief scrape then a soft "
     "handling clink, close microphone", 1.5, False),
    ("Interaction", "pickup-ammo",
     "Handful of pistol cartridges picked up, brass casings rattling "
     "together, close microphone", 1.5, False),
    ("Interaction", "pickup-battery",
     "Battery cell taken from a rack and seated with a plastic and metal "
     "click, close microphone", 1.5, False),
    ("Interaction", "carriable-lift",
     "Heavy steel jerry can lifted off a concrete floor, liquid sloshing "
     "inside, strained metal handle creak, close microphone", 2.0, False),
    ("Interaction", "carriable-drop",
     "Heavy steel jerry can set down hard on concrete, dull metallic boom "
     "with liquid sloshing after, close microphone", 2.0, False),
    ("Interaction", "interact-denied",
     "Short dull thud of a locked mechanism refusing to move, single dead "
     "clunk, no reverb, close microphone", 1.0, False),

    # --- 장치. 즉시 발동 / 시간 발동 / 지속 상태 세 패턴에 대응한다 -------
    ("Devices", "shutter-close",
     "Heavy fire shutter released and rolling down its rails fast, chain "
     "clatter, ending in a loud slam against the floor", 4.0, False),
    ("Devices", "fuel-ignite",
     "Pool of spilled diesel catching fire, sudden whoomph of ignition then "
     "a roaring wall of flame", 4.0, False),
    ("Devices", "hydrogen-explosion",
     "Hydrogen gas explosion in a confined concrete room, sharp cracking "
     "blast then debris and ringing metal", 4.0, False),
    ("Devices", "avalanche-cannon",
     "Gas avalanche cannon firing, compressed gas discharge with a deep "
     "hollow boom rolling across a mountain slope", 3.0, False),
    ("Devices", "avalanche-slide",
     "Massive snow slab breaking loose and sliding down a mountainside, "
     "deep rushing rumble building then fading", 5.0, False),
    ("Devices", "device-start",
     "Industrial machine starting up, relay clunk then a motor winding up "
     "to speed, close microphone", 2.5, False),
    ("Devices", "device-complete",
     "Industrial process finishing, motor winding down and a single bell "
     "chime confirming completion, close microphone", 2.0, False),
    ("Devices", "ice-melt-crack",
     "Thick ice on metal cracking and releasing as it thaws, sharp splitting "
     "cracks with water dripping, close microphone", 3.0, False),
    ("Devices", "generator-start",
     "Large diesel generator cranking, catching, and settling into a steady "
     "idle, recorded in the engine room", 5.0, False),
    ("Devices", "snowblower-start",
     "Rotary snow blower engine cranking and its auger beginning to spin up, "
     "heavy machinery", 3.0, False),
    ("Devices", "snowblower-loop",
     "Rotary snow blower running at full speed, engine roar with the auger "
     "chewing through packed snow, " + STEADY, 12.0, True),
    ("Devices", "searchlight-swivel-loop",
     "Large searchlight motor slewing the lamp housing, steady electric "
     "servo whine with gear train, " + STEADY, 10.0, True),

    # --- 적 소리 -----------------------------------------------------------
    # 스펙(docs/enemy-sheets/)이 세 종의 표면을 「검은 회청색 균열 피부 + 흰 서리
    # 흉터」로 고정하고 「젖은 값은 아래쪽 입과 관절 최심부에만 3% 미만」 이라고
    # 못박았다. Colossus 는 「실제 석재·금속 재질 금지」 다. 그래서 소리도 몸통은
    # 마른 서릿결이 갈라지는 쪽으로 가고, 젖은 소리는 입에서만 낸다.
    # 셋 다 무안이라 「노려본다」 류의 신호가 없고 예고는 전부 호흡으로 만든다.

    # Crawler — 바닥 쐐기. AC_Crawler 클립 6종 + 상태 전환 4종
    ("Crawler", "crawler-idle-loop",
     "Low steady breathing of a large four legged animal crouched flat "
     "against a cold floor, air drawn through a lipless underslung jaw, dry "
     "frost cracked hide creaking faintly as the ribs move, close "
     "microphone, " + STEADY, 8.0, True),
    ("Crawler", "crawler-skitter-loop",
     "Four legged creature moving fast and low across concrete, hard keratin "
     "claws ticking and scraping in a two beat diagonal rhythm, dry hide "
     "creaking, close microphone, " + STEADY, 6.0, True),
    ("Crawler", "crawler-sprint-loop",
     "Four legged creature sprinting hard on concrete, heavy rapid claw "
     "strikes with compressed breath forced out on every stride, dry frozen "
     "hide flexing, close microphone, " + STEADY, 6.0, True),
    # 04-attack-underbite.md — 8f 예고, 13f 히트. 둘로 나눈다
    ("Crawler", "crawler-attack-tell",
     "Large animal freezing then drawing a sharp hissing breath through an "
     "opening jaw, low warning rasp building, close microphone, no music",
     1.0, False),
    ("Crawler", "crawler-attack",
     "Very loud close up bite impact, wet crunch of flesh and cartilage at "
     "full volume, aggressive animal bark on top, dry studio foley, no "
     "music", 1.5, False),
    ("Crawler", "crawler-hit",
     "Bullet striking a large animal, dull wet impact under dry cracked "
     "hide and a short choked grunt, close microphone, no music", 1.0, False),
    ("Crawler", "crawler-death",
     "Large four legged animal collapsing, air escaping a ruined throat in a "
     "long rattling hiss as the limbs fold under the body, dry hide scraping "
     "the floor, close microphone, no music", 3.0, False),
    ("Crawler", "crawler-notice",
     "Animal breathing stops abruptly, then a rising dry rasp through a jaw "
     "opening wide, ending in one sharp bark, close microphone, no music",
     2.0, False),
    ("Crawler", "crawler-vocal-01",
     "Distant creature call down a long concrete corridor, low dry rasping "
     "moan rising into a thin whine, echoing, no music", 2.5, False),
    ("Crawler", "crawler-vocal-02",
     "Distant creature call in a concrete corridor, dry clicking chatter "
     "from a jaw followed by a short guttural growl, echoing, no music",
     2.5, False),

    # Clinger — 천장의 것. 각질 갈고리 8개로 붙어 있다 떨어진다
    ("Clinger", "clinger-cling-loop",
     "Continuous scraping and creaking of a heavy body hanging from a "
     "concrete ceiling, keratin hooks grinding slowly against the surface "
     "without pause, steady shallow breathing underneath, never silent, "
     "close microphone, " + STEADY, 8.0, True),
    ("Clinger", "clinger-tremor-loop",
     "Dense unbroken rattling texture, dozens of small dry horn plates "
     "vibrating against stone at once, like a hard rain of clicks, "
     "continuous from start to finish, close microphone, " + STEADY, 6.0, True),
    # 03-unfold-drop.md — 10f AE_DropRelease
    ("Clinger", "clinger-drop-release",
     "Eight keratin hooks tearing free from a concrete ceiling at once, "
     "sharp dry crack and scrape followed by the rush of a heavy body "
     "falling, close microphone, no music", 1.5, False),
    # 04-land.md — 15f AE_EnableGroundMode. 1.42 m 에서 네 점 착지
    ("Clinger", "clinger-land",
     "Heavy creature landing on four points on a concrete floor from above, "
     "hard compressed thud with hide slapping and a forced grunt, close "
     "microphone, no music", 2.0, False),
    ("Clinger", "clinger-scuttle-loop",
     "Unbroken run cycle of a heavy four limbed animal on concrete, rapid "
     "overlapping hand and claw impacts with scrapes filling every gap, "
     "dense continuous rhythm from start to finish, close microphone, "
     + STEADY, 6.0, True),
    # 06-attack-underjaw.md — 9f 예고, 13f 히트
    ("Clinger", "clinger-attack-tell",
     "Creature drawing back with a dry rasping inhale, jaw unlatching with a "
     "wet click, close microphone, no music", 1.0, False),
    ("Clinger", "clinger-attack",
     "Underslung jaw striking upward together with both hands, wet snap and "
     "a heavy grabbing impact, close microphone, no music", 1.5, False),
    ("Clinger", "clinger-hit",
     "Bullet impact on a thick limb, dry cracking hide and a short high "
     "shriek, close microphone, no music", 1.0, False),
    ("Clinger", "clinger-death",
     "Creature losing its grip and collapsing sideways onto concrete, hooks "
     "scraping loose, heavy body impact then a long draining hiss, close "
     "microphone, no music", 3.0, False),

    # Colossus — 죽지 않는 것. Death 클립이 없으므로 사망 소리도 없다
    ("Colossus", "colossus-idle-loop",
     "Enormous animal standing still and breathing very slowly, deep "
     "subsonic inhale and exhale, huge frost cracked hide creaking under its "
     "own weight, close microphone, " + STEADY, 10.0, True),
    ("Colossus", "colossus-walk-loop",
     "Enormous four point knuckle walker moving slowly, massive hands and "
     "feet pressing into packed snow, deep slow rhythm with hide groaning, "
     + STEADY, 8.0, True),
    ("Colossus", "colossus-step-01",
     "Single enormous hand slamming down onto frozen ground, deep low impact "
     "with debris and a heavy weight shift, close microphone, no music",
     2.0, False),
    ("Colossus", "colossus-step-02",
     "Single enormous foot planting on packed snow, deep crushing impact "
     "with a long low rumble after, close microphone, no music", 2.0, False),
    # MODEL-SPEC 5. 애니메이션 — Push 20f 에 전방 밀기
    ("Colossus", "colossus-push",
     "Enormous creature shoving forward with both arms, huge mass "
     "accelerating into a deep grinding impact with forced breath, no music",
     3.0, False),
    ("Colossus", "colossus-grip-cable",
     "Three huge fingers closing around a steel cable, metal creaking and "
     "singing under an enormous load, no music", 3.0, False),
    ("Colossus", "colossus-stagger",
     "Enormous creature knocked back a step, heavy stumbling weight shift "
     "with a deep angry rumble, no music", 3.0, False),
    ("Colossus", "colossus-buried",
     "Enormous body buried under collapsed snow, muffled deep breathing and "
     "slow shifting still audible beneath the mass, no music", 5.0, False),
    # 1장 창밖. 실물 없이 실루엣만 지나간다
    ("Colossus", "colossus-distant-loop",
     "Enormous unseen animal moving far away through a blizzard, deep low "
     "rumble and slow heavy impacts muffled by wind and distance, "
     + STEADY, 10.0, True),
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

    # NAME=값 형태로 적어둔 경우도 받아준다
    if not key.startswith("sk_") and "=" in key:
        key = key.split("=", 1)[1]
    return key.strip().strip("\"'")


def generate(key, item, out_root, fmt, overwrite):
    category, name, text, seconds, loop = item
    dest = dest_dir(out_root, category) / f"{name}.mp3"
    # 단발성은 trim_silence.py 를 거치며 wav 가 된다. 그것도 있는 것으로 친다
    if not overwrite:
        for existing in (dest, dest.with_suffix(".wav")):
            if existing.exists():
                print(f"  건너뜀 (이미 있음) {category}/{existing.name}")
                return 0

    body = {
        "text": text,
        "duration_seconds": seconds,
        "prompt_influence": 0.5,
        "output_format": fmt,
    }
    if loop:
        body["loop"] = True
        body["model_id"] = "eleven_text_to_sound_v2"

    req = urllib.request.Request(
        ENDPOINT,
        data=json.dumps(body).encode("utf-8"),
        headers={"xi-api-key": key, "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as res:
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
    print(f"  {category}/{dest.name:32} {len(audio)/1024:6.0f} KB  ~{cost} 크레딧")
    return cost


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=OUT_ROOT)
    ap.add_argument("--key-file", default="elevenlabs.local.key")
    ap.add_argument("--format", default="mp3_44100_128",
                    help="Starter 는 128 까지. 192 는 Creator 이상")
    ap.add_argument("--only", action="append", help="이름 지정. 여러 번 가능")
    ap.add_argument("--category", action="append", help="카테고리 지정")
    ap.add_argument("--overwrite", action="store_true",
                    help="이미 있는 파일도 다시 생성한다")
    ap.add_argument("--dry-run", action="store_true",
                    help="호출하지 않고 목록과 예상 크레딧만 본다")
    args = ap.parse_args()

    todo = PROMPTS
    if args.category:
        todo = [p for p in todo if p[0] in args.category]
    if args.only:
        todo = [p for p in todo if p[1] in args.only]
    if not todo:
        sys.exit("생성할 항목이 없다.")

    planned = sum(int(p[3] * CREDITS_PER_SECOND) for p in todo)
    total_sec = sum(p[3] for p in todo)
    print(f"{len(todo)}개 · {total_sec:.0f}초 · 예상 약 {planned} 크레딧")

    if args.dry_run:
        for category, name, _, seconds, loop in todo:
            tag = "loop" if loop else "    "
            print(f"  {tag} {category}/{name}.mp3  {seconds}s")
        return

    key = load_key(args.key_file)
    spent = 0
    for item in todo:
        spent += generate(key, item, args.out, args.format, args.overwrite)
    print(f"완료. 약 {spent} 크레딧 사용")


if __name__ == "__main__":
    main()
