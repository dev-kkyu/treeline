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
    "Music": "Treeline/Assets/Game/Audio/Music",
    "Crawler": "Treeline/Assets/Game/Enemy/Audio",
    "Clinger": "Treeline/Assets/Game/Enemy/Audio",
    "Colossus": "Treeline/Assets/Game/Enemy/Audio",
    # 무기 · 손전등은 architecture.md 가 Player/ 를 담당 폴더로 정했다
    "Weapon": "Treeline/Assets/Game/Player/Audio/Weapons",
    "Protagonist": "Treeline/Assets/Game/Player/Audio/Body",
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

    # --- 메뉴. 타이틀 · 엔딩 화면의 버튼 ----------------------------------
    # 메뉴는 중계소 제어반의 연장이다. 밝고 둥근 UI 효과음을 쓰면 화면 안의
    # 금속 패널과 따로 놀아서, 실제 토글 스위치를 누른 소리로 지시한다.
    ("UI", "ui-click",
     "One single crisp click of a small metal toggle switch on an industrial "
     "control panel, one press only, dry tight snap with no reverb tail and "
     "nothing before or after it, close microphone, no music", 0.5, False),

    # --- 무기 -------------------------------------------------------------
    # 09-weapons.md 의 실측대로 지시한다. 권총은 190 mm 짜리 슬라이드 · 해머 ·
    # 8발 탄창, 산탄총은 개머리판이 없는 760 mm 짧은 진입용에 22.8 × 68 mm 탄
    # 4발이다. 「군용」 이나 「전술」 로 지시하면 정비공의 총이 아니게 된다.
    #
    # IMPORTANT: 총성에 공간을 굽지 않는다. 「concrete corridor」 를 넣으면 5 · 6장
    # 야외에서 실내 반향이 따라다닌다. 마른 근접 녹음으로 받고 공간은 씬이 준다.
    #
    # 기계 조작음(슬라이드 · 탄창 · 펌프)은 발사음과 **같은 카테고리에 두고 음량
    # 목표만 다르게** 준다. normalize.py 의 FILE_TARGETS 가 총성만 끌어올린다 —
    # 한 카테고리로 맞추면 탄창 딸깍이 총성만큼 커진다.

    # IMPORTANT: 총성은 두 번 틀렸다. 판정은 **상승 시간**과 **60 Hz 아래가 먹는
    # 에너지**로 본다. 같은 배치의 산탄총이 8 ms · HPF 후 손실 2 dB 로 제대로
    # 나왔으므로 기준선이 있다.
    #   1판 「tight dry crack」 → 상승 130 ms · 200 Hz 아래 0.4 %. 터지는 소리가
    #      아니라 부풀어 오르는 소리고, 얇다
    #   2판 「deep low end punching underneath」 → 초저역 덩어리가 왔다. 60 Hz
    #      HPF 를 걸면 RMS 가 13 dB 빠진다. **들리지 않는 대역이 헤드룸을 다 먹어**
    #      normalize 가 이득 상한에 걸리고도 목표보다 14 dB 아래에 남는다
    # 「deep」 · 「low end」 를 쓰지 않고 저역을 **화약이 터지는 몸통**으로 지시한다.
    ("Weapon", "pistol-fire",
     "One gunshot from a 9 mm pistol, recorded a arm's length away indoors. "
     "The powder goes off in an instant — a loud flat percussive report with "
     "real body to it, immediately followed by the slide cycling and the "
     "empty casing flicking out. Dry and close with a very short tail.",
     1.5, False),
    # Loaded <= 0 에서 방아쇠를 당긴 경우. 「총성 아님」 을 못 박아야 총이 나온다
    ("Weapon", "pistol-dry",
     "Trigger pulled on an empty pistol: hammer falls on nothing, one dead "
     "metallic click, small and hollow. No gunshot, no bang, no discharge, "
     "close microphone, no music", 1.0, False),
    # 06-pistol.md f8 탄창 멈치 → f10~20 낙하
    ("Weapon", "pistol-mag-out",
     "Pistol magazine catch pressed and the empty magazine dropping free of "
     "the grip, small spring loaded click then the magazine sliding out of "
     "the well, close microphone, no music", 1.2, False),
    # f39 MagazineIn — f42 손바닥 타격까지 한 소리로 받는다
    ("Weapon", "pistol-mag-in",
     "Fresh magazine seated into a pistol grip and struck home with the palm, "
     "metal on metal insertion then a solid seating clack, close microphone, "
     "no music", 1.2, False),
    # f43~50 슬라이드 25 mm 왕복
    ("Weapon", "pistol-slide",
     "Pistol slide pulled back and released, hard metallic rasp of the "
     "recoil spring then the slide snapping forward and chambering a round, "
     "close microphone, no music", 1.2, False),
    ("Weapon", "pistol-equip",
     "Pistol drawn from a canvas holster and brought up in both hands, fabric "
     "scrape then the weight settling with a light metallic tap, close "
     "microphone, no music", 1.2, False),

    ("Weapon", "shotgun-fire",
     "Single shot from a short pump action shotgun with no stock, enormous "
     "flat blast much deeper and wider than a pistol, dry close recording "
     "with a very short tail, no music", 2.0, False),
    ("Weapon", "shotgun-dry",
     "Trigger pulled on an empty pump action shotgun, one dull mechanical "
     "click of the hammer with nothing following. No gunshot, no bang, no "
     "discharge, close microphone, no music", 1.0, False),

    # 펌프 왕복. 2026-08-09 에 둘 다 다시 뽑았다 — 1판은 후퇴가 무게중심
    # 1493 Hz 로 둔탁했다. 펌프는 **강철이 강철 위를 긁는** 소리이고 거기서
    # 오는 것은 중역의 쿵이 아니라 금속의 거친 결이다. 07-shotgun.md 의 실측
    # 65 mm 행정과 「10 pump unlock · 15 후퇴 · 16 Eject · 23 전진·장전」 을
    # 각각 앞뒤 두 소리로 나눠 담는다.
    # 2판은 무게중심이 1493 → 4194 Hz 로 제대로 왔지만 **0 dBFS 에 218 샘플이
    # 붙어** 클리핑했다. 프롬프트로 음량을 조절할 수는 없으므로 극단적인 낱말
    # (hauled · hard)만 덜고 다시 뽑는다. 판정은 채널별 피크로 하며, 다운믹스로
    # 재면 1.0 을 넘는 값이 나와도 실제 클리핑이 아니다.
    ("Weapon", "shotgun-pump-back",
     "The forend of a pump action shotgun pulled all the way to the rear in "
     "one firm stroke: the action bars unlock with a clack, steel slides "
     "along steel with an audible rasp the whole length of the travel, the "
     "spent plastic hull is thrown clear of the ejection port, and the stroke "
     "comes to rest against the rear limit. Close microphone, dry, no music",
     1.2, False),
    ("Weapon", "shotgun-pump-forward",
     "The forend of a pump action shotgun driven forward hard: steel rasping "
     "along steel again, a fresh shell picked up off the lifter and stuffed "
     "into the chamber, ending in the breech locking shut with one heavy "
     "solid clunk. Close microphone, dry, no music", 1.2, False),

    # f18 · 32 · 46 · 60 — 네 번 반복 재생되므로 특징적인 꼬리를 남기지 않는다
    ("Weapon", "shotgun-shell-insert",
     "One shotgun shell pushed into the loading port with the thumb, plastic "
     "hull scraping on steel then a short click as the lifter takes it, close "
     "microphone, no music", 1.0, False),
    ("Weapon", "shotgun-equip",
     "Short pump action shotgun taken up in both hands, heavy weight shifting "
     "against a thick winter coat with a metallic knock, close microphone, "
     "no music", 1.5, False),

    # 벽에 맞는 소리. 적에 맞는 소리는 이미 적 폴더에 있다(crawler-hit 등)
    #
    # IMPORTANT: 「no deep boom, no rumble」 을 넣은 판이 **피크 0.017 로 거의
    # 무음**으로 왔다. 무엇을 빼라는 지시가 소리 자체를 지웠다. 뺄 것을 적지 않고
    # 남길 것만 적는다. (적 소리의 「소가 아니다」 는 통했다 — 차이는 대상이
    # 있는지다. 정체를 좁히는 부정은 되고, 대역을 빼라는 부정은 안 된다.)
    ("Weapon", "bullet-impact-concrete",
     "A bullet slamming into a concrete wall at full speed, loud and close: "
     "the hard crack of the surface breaking with dust and sharp stone chips "
     "spraying off right after it, close microphone", 1.0, False),
    ("Weapon", "bullet-impact-metal",
     "Bullet striking heavy steel plate, hard bright metallic spang with a "
     "short ringing decay, close microphone, no music", 1.0, False),
    # 권총 탄피. 8발이 이어서 나가면 발사음 뒤에 하나씩 떨어진다
    ("Weapon", "shell-casing-drop",
     "One empty brass pistol casing landing on a concrete floor, bright thin "
     "tinkle bouncing twice, close microphone, no music", 1.0, False),

    # --- 주인공 -----------------------------------------------------------
    # 정비공이고 군인이 아니다. 숨과 힘쓰는 소리로만 표현하고 대사는 없다.
    # 성별은 스펙에 없으므로 지시하지 않는다 — 1.82 m · 76 kg 의 체격에 맞춰
    # 낮은 음역으로만 잡는다. 두꺼운 방한복과 장비가 같이 움직인다.

    ("Protagonist", "player-hurt-01",
     "One short involuntary grunt of pain from an adult person in a heavy "
     "winter coat, air forced out through the teeth, thick fabric and gear "
     "jolting at the same time. Low pitched, no words, close microphone, "
     "no music", 1.0, False),
    ("Protagonist", "player-hurt-02",
     "One sharp pained gasp from an adult person taking a heavy hit, breath "
     "caught and cut short, padded clothing crushing. Low pitched, no words, "
     "close microphone, no music", 1.0, False),
    ("Protagonist", "player-death",
     "An adult person collapsing onto a cold floor: the last breath emptying "
     "out in a long uneven exhale, heavy winter clothing and gear settling, "
     "then nothing. Low pitched, no words, close microphone, no music",
     3.0, False),
    # PlayerMotor.Jumped
    ("Protagonist", "player-jump",
     "Short effort breath of an adult person pushing off the ground, gear and "
     "webbing jolting upward with it, no words, close microphone, no music",
     1.0, False),
    ("Protagonist", "player-land",
     "An adult person landing on both boots, knees absorbing the drop, heavy "
     "clothing and slung equipment thudding and rattling down, short breath "
     "out, close microphone, no music", 1.2, False),
    # IsExhausted. 8 초 루프라 normalize 가 LUFS 로 잡는다.
    # 세 판이 −44 · −51 · −62 LUFS 로 계속 작아졌다. 「exhausted」 · 「panting」
    # 같은 상태어를 쓰는 동안은 조용한 호흡이 온다. 적 소리에서 통했던 방식대로
    # **공기가 지나가는 관**으로 지시하니 −29 LUFS 가 왔다
    ("Protagonist", "player-breath-exhausted-loop",
     "Air being dragged in and shoved back out through a wide open human "
     "mouth and a dry throat, fast and without pause, close enough that the "
     "airflow scrapes and rasps on every pass. Low pitched, wordless, "
     + STEADY, 8.0, True),
    # Carriable 을 들 때. carriable-lift 는 통 쪽 소리이고 이쪽은 사람 쪽이다
    ("Protagonist", "player-exert",
     "Adult person straining to lift something heavy, breath held then "
     "released through the teeth, coat and gear pulling tight. Low pitched, "
     "no words, close microphone, no music", 1.5, False),
    # Body_Grabbed 132f. 8 포획 · 40 들어올림 · 90 치명 중 사람 쪽 소리
    ("Protagonist", "player-struggle",
     "An adult person caught in a crushing grip and fighting to get out: "
     "boots kicking at nothing, thick clothing wrenching, breath forced out "
     "in short panicked bursts with no words. Low pitched, close microphone, "
     "no music", 3.0, False),

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

    # 4 장 축전지실. 폭발은 소리 하나로 끝나지 않는다 — 기획의 「문 옆 계기가
    # 빨갛다 · 여기서 쏘면 터진다 · 폭발이 무리를 쓸어버린다 · 대신 축전지가
    # 날아가 조명 일부가 안 들어온다」가 네 개의 다른 소리다.
    #
    # 위 hydrogen-explosion 은 터지는 순간만 갖고 있고, 그 앞의 경고와 뒤의
    # 결과가 없다. 특히 **경고**가 없으면 규칙을 죽어보고 배우게 된다 —
    # 계기 색은 보고 있어야 보이지만 소리는 방에 들어서면 들린다.
    ("Devices", "hydrogen-hiss-loop",
     "Gas leaking steadily out of a cracked pipe fitting into a small "
     "concrete room, thin continuous high pitched hiss with a faint metallic "
     "resonance around it, recorded close to the leak, " + STEADY, 12.0, True),
    # 폭발 직후. 무리를 쓸어버린 뒤 방이 무너지는 소리
    ("Devices", "debris-fall",
     "Aftermath of a blast in a sealed concrete room: chunks of concrete and "
     "torn sheet metal raining down onto a hard floor, the heavy pieces "
     "landing first and then smaller grit and dust settling, close microphone",
     3.0, False),
    # 축전지가 날아간다. 조명 일부가 안 들어오는 이유를 소리가 준다
    ("Devices", "battery-arc",
     "A bank of lead acid batteries shorting out: a loud snapping electrical "
     "arc, heavy current buzzing hard through metal terminals, spitting and "
     "crackling, close microphone", 2.5, False),
    # 폭발의 사람 쪽 꼬리. 기획이 「폭발은 카메라를 흔들고 화면을 덮는 방식으로
    # 만든다」고 했고, 그 소리 쪽 짝이다. 장치 폴더에 두는 것은 이것을 재생하는
    # 쪽이 폭발 장치이기 때문이다. STEADY 를 붙이지 않는다 — 이것만은 잦아든다
    ("Devices", "explosion-ear-ring",
     "The high thin ringing left in a person's ears right after a very loud "
     "blast in an enclosed space: one sustained metallic tone hanging in near "
     "silence, everything else muffled and far away, slowly dying down",
     4.0, False),
    # 5 장 제설포와 그 결과. 2026-08-09 에 둘 다 다시 뽑았다.
    #   제설포 1판 — 무게중심 2762 Hz · 피크 0.284. 「deep hollow boom」 을 넣었는데
    #     중역에 몰려 나왔다. 산을 흔드는 소리가 아니라 문 닫는 소리에 가깝다
    #   눈사태 1판 — 무게중심 4499 Hz · 60 Hz HPF 손실 8 dB. 밝은 쉭 소리에
    #     들리지 않는 초저역이 얹힌 것이고, 그 초저역이 헤드룸을 먹는다
    # 둘 다 「deep」 · 「low」 를 쓰지 않고 **무엇이 그 소리를 내는지**로 지시한다 —
    # 제설포는 강철관 안에서 터지는 압축가스, 눈사태는 사면째로 움직이는 질량이다.
    ("Devices", "avalanche-cannon",
     "A gas avalanche cannon going off on an exposed mountain slope. A large "
     "charge of compressed gas detonates inside a heavy steel barrel and the "
     "muzzle blast slams out across open ground, the steel tube ringing hard "
     "with it, and the report rolls away over the valley. Enormous, blunt, "
     "recorded from twenty metres away outdoors", 3.0, False),
    # 2판은 60 Hz 아래 쓰레기가 사라졌지만(HPF 손실 8.0 → 0.0 dB) 무게가 안 왔다 —
    # 200 Hz 아래가 4.5 % 뿐인 밝은 쉭 소리다. 「무게」 를 형용사로 부탁하면
    # 초저역이 오고, 「질량」 으로 부탁하면 바람 소리가 온다. 3판은 **저역을 내는
    # 물체**를 넣는다 — 눈 속에 섞여 구르는 얼음덩이와 바위다. 물체가 있으면
    # 생성기가 그것을 때리는 소리로 저역을 만든다.
    ("Devices", "avalanche-slide",
     "A whole snow slope collapsing and running down over broken ground, "
     "recorded from close by. Blocks of ice and boulders the size of cars are "
     "rolling inside the moving snow, slamming into each other and into the "
     "rock face with heavy dull impacts the whole way down, while the snow "
     "itself grinds and roars around them. It builds, fills everything, then "
     "runs out", 6.0, False),
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
    # 발전기가 내려가는 소리. 기동만 있고 이것이 없었다(2026-08-09 확인).
    # Electrical/power-down 과 다른 소리다 — 저쪽은 건물의 조명과 팬이 죽는
    # 소리이고 이것은 **기관이 멈추는** 소리다. 4 장에서 발전기를 세우거나
    # 연료가 끊길 때 쓴다
    ("Devices", "generator-stop",
     "Large diesel generator shutting down in its engine room: the fuel cuts "
     "and the engine loses speed, the firing beats slowing and getting further "
     "apart until the last one, then the flywheel spinning down alone and "
     "stopping with a metallic settle", 5.0, False),
    ("Devices", "snowblower-start",
     "Rotary snow blower engine cranking and its auger beginning to spin up, "
     "heavy machinery", 3.0, False),
    ("Devices", "snowblower-loop",
     "Rotary snow blower running at full speed, engine roar with the auger "
     "chewing through packed snow, " + STEADY, 12.0, True),
    # 6 장 삭도. 그동안 발전기 소리를 빌려 썼다
    ("Devices", "cablecar-start",
     "Large industrial winch starting up: brake releasing with a hard clunk, "
     "heavy electric motor whining up to speed under load, steel haul rope "
     "creaking as it takes tension, close microphone", 4.0, False),
    ("Devices", "cablecar-loop",
     "Aerial cable car winch running steadily under load, deep electric motor "
     "hum with a steel haul rope singing over rollers and a slow rhythmic "
     "sheave clatter, " + STEADY, 12.0, True),

    ("Devices", "searchlight-swivel-loop",
     "Large searchlight motor slewing the lamp housing, steady electric "
     "servo whine with gear train, " + STEADY, 10.0, True),

    # --- 음악이지만 효과음 API 로 만드는 것 -------------------------------
    # 금속 질감이라 악기 생성보다 이쪽이 맞고, loop 파라미터가 있어 이음매를
    # 생성기가 처리한다. 다만 생성 결과가 매번 원본보다 밝게 나오므로 뽑은 뒤
    # 아래 두 단계를 반드시 거친다.
    #   1. ffmpeg treble=g=-9:f=2000:width_type=q:w=0.6
    #      목표는 무게중심 1393 Hz 다. 그냥 쓰면 3000 Hz 근처로 쇳소리만 난다.
    #   2. 루프 안 음량 기울기 펴기(원형 이동 RMS 의 역수, ±3 dB 로 세 번).
    #      원본은 9.5 dB 차이라 한 바퀴마다 부풀었다 꺼진다.
    #   3. normalize.py 로 -18 LUFS
    ("Music", "chase-dread-loop",
     "Grinding scraped metal resonance and harsh inharmonic ringing, sheets "
     "of cold steel groaning and singing under stress, sitting on top of a "
     "heavy low drone that holds underneath. Dense and metallic, unresolved, "
     "oppressive, airless. Already at full intensity with no build up and no "
     "development, the same state held all the way through. " + STEADY
     + " No music, no instruments, no melody.", 22.0, True),

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
    # IMPORTANT: 여기에 moan · call · bellow · cry 를 쓰면 **소 울음이 온다.**
    # 전부 생성기에게 포유류 발성 신호다. 실제로 그렇게 나와서 다시 뽑았고,
    # 재보니 스펙트럴 플랫니스가 0.0001 이었다 — 음정이 완벽하게 또렷하다는 뜻,
    # 즉 짐승 울음이다. 「울음」 을 뜻하는 단어 대신 **소리가 나는 기관**으로
    # 지시한다. 형제들의 범위는 플랫니스 0.03~0.39 · 무게중심 39~5800 Hz 다.
    ("Crawler", "crawler-vocal-01",
     "A predatory thing forcing air out through a lipless underslung jaw in a "
     "concrete corridor. Dry rasping hiss tearing into a high strangled "
     "screech that splinters and breaks apart, wet at the very end. "
     "Reptilian and insectile, ragged, no clear pitch. Not a cow, not cattle, "
     "not a mammal, no lowing, no mooing, no bellow, no human voice, no music.",
     2.5, False),
    # snarl 도 포유류 발성이다. 이것 하나 남겨뒀다가 플랫니스가 0.046 에 머물러
    # 다시 뽑았다. 목으로 내는 소리를 아예 지우고 턱과 뼈판이 부딪히는 물리적인
    # 소리로만 지시하니 0.157 이 됐다.
    ("Crawler", "crawler-vocal-02",
     "A predatory thing working its jaw fast in a long concrete corridor: hard "
     "bone plates clacking together in rapid irregular bursts, dry rattling "
     "and scraping of keratin, ending in one short sharp hiss of air forced "
     "out. Brittle, insectile, mechanical, no voice at all, no pitch. "
     "Not a cow, not cattle, not a mammal, no growl, no snarl, no moan, "
     "no lowing, no human voice, no music.", 2.5, False),

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
    # 5 장은 실내가 아니라 눈보라 속이다. 같은 걸음이 눈에서는 전혀 다르게 난다 —
    # 딱딱한 충격이 사라지고 **묻히는 소리와 파헤치는 소리**만 남는다.
    #
    # IMPORTANT: 05-scuttle.md 가 이 걸음의 정체를 「앞팔(1.55 m)이 몸을 던지고
    # 짧은 뒷사지(0.82 m)가 못 따라가 바닥을 긁으며 끌려온다」로 못박았다. 뒤
    # 갈고리가 1.5 m/s 만큼 미끄러지는 것은 버그가 아니라 이 걸음이다. 눈에서는
    # 그 끌림이 가장 잘 들리는 성분이므로 **앞발이 박히는 소리와 뒷발이 눈을
    # 파헤치는 소리를 따로** 지시한다. 그냥 「눈 위 발소리」로 지시하면 사람
    # 발소리 넷이 온다.
    ("Clinger", "clinger-scuttle-snow-loop",
     "A heavy four limbed thing moving fast over deep snow, unbroken. Two "
     "long front limbs punch down and sink in with a muffled compressed crunch "
     "that throws the body forward, and two much shorter back limbs never keep "
     "up — they plough and drag through the snow between every impact, so the "
     "scraping never stops. Lopsided and lurching, dense and continuous, "
     "recorded close outdoors, " + STEADY, 6.0, True),
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
    # IMPORTANT: 거대함을 저역으로만 만들면 안 들린다. 무게중심이 88 Hz 였고
     # step-01 은 29 Hz 라 노트북 스피커에서 사라졌다. 귀에 남는 것은 서브가
     # 아니라 충격체와 부스러기다. 언 땅이 갈라지는 소리를 반드시 넣는다.
    ("Colossus", "colossus-walk-loop",
     "Enormous four point knuckle walker crossing frozen ground at a slow "
     "pace. Every contact is a huge blunt thud you feel in the chest, the "
     "body of the impact heavy and round, with the ice crust splitting and a "
     "little grit scattering right after it. The thud dominates; the cracking "
     "is only a detail on top. Recorded close, " + STEADY, 8.0, True),
    ("Colossus", "colossus-step-01",
     "One enormous hand slamming down onto frozen ground. The ice crust "
     "splits with a hard cracking report, packed grit crunches under the "
     "palm, and loose debris scatters afterward. Blunt and heavy but bright "
     "enough to hear clearly, close microphone, no music", 2.0, False),
    ("Colossus", "colossus-step-02",
     "One enormous foot planting into packed snow over frozen ground. Snow "
     "compresses with a dense crunch, the crust breaks underneath, and grit "
     "shifts as the weight settles. Blunt and heavy but bright enough to hear "
     "clearly, close microphone, no music", 2.0, False),
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
    # 6 장 발각 큐. 기획의 「멈춰 서서 돌아보고 포효한 뒤 온다」 그 2 초다.
    # roar 를 쓰되 lion · bear 를 막지 않으면 사자가 온다. 울음 계열 금지어는
    # crawler-vocal 에서 배운 그대로다.
    ("Colossus", "colossus-roar",
     "An enormous predatory thing stopping, turning, and forcing a roar out "
     "through a lipless underslung jaw. Cavernous and low with dry cracked "
     "hide flexing under it, rising into a harsh rasping blast that carries "
     "across a valley, ending abruptly. Vast, inhuman, no clear pitch. "
     "Not a lion, not a bear, not a mammal, no human voice, no music.",
     3.0, False),

    # 즉사 장면. 기획의 「양손으로 붙들려 목이 뜯긴다」 다. Colossus 만 한다.
    # 붙잡는 순간과 끝나는 순간을 나눈다 — 한 덩어리면 타이밍을 못 맞추고
    # 「그 자리에서 끝난다」 의 갑작스러움도 안 산다.
    ("Colossus", "colossus-grab",
     "Two enormous hands slamming shut around a person: heavy impact on a "
     "padded winter coat, fabric and gear crushing, the breath knocked out in "
     "a short choked gasp, feet leaving the ground. Close microphone, "
     "no music", 1.5, False),
    ("Colossus", "colossus-kill",
     "A short brutal end: wet tearing of flesh and cartilage in one motion, "
     "a cut off gasp, then a body dropping onto frozen ground and one deep "
     "settling weight. Ends abruptly, nothing after. Close microphone, "
     "no music", 2.5, False),

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
