"""Build the nine Clinger actions on the rig and bake them onto the deform bones.

    blender <scratch>/Clinger_Rig.blend -b --python animate.py

Clip list, frame counts and event frames come from
docs/enemy-sheets/mockups/codex/clinger/final/animation/*.md (2026-08-09 revision).

Every clip is authored as a list of `(frame, pose function)`. The pose functions
call into poses.py, which is the only place that knows the rig's axis and mirror
conventions. Looping clips get their seam frame by replaying the frame-1 pose, not
by re-deriving it - IK is history-dependent and re-entering the same numbers can
land on a different solution.
"""
import bpy
import math
import os
import sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import poses as P                                                # noqa: E402

SCRATCH = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"

arm, mesh = P.arm, P.mesh
bpy.context.scene.render.fps = 30
bpy.context.scene.render.fps_base = 1.0
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')

IK_BONES = [c[0] for c in (
    ("DEF_CeilLower.RF",), ("DEF_CeilLower.LF",), ("DEF_CeilLower.RR",),
    ("DEF_CeilLower.LR",), ("DEF_AttackFore.R",), ("DEF_AttackFore.L",))]


# ----------------------------------------------------------------- utilities

def ease(u, p=2.0):
    """0..1 -> 0..1, slow out of the pose and into the next one."""
    u = max(0.0, min(1.0, u))
    return u ** p / (u ** p + (1.0 - u) ** p)


def pulse(f, centre, width, power=2.0):
    """A single smooth bump, 1.0 at `centre`, 0 outside +-width."""
    d = abs(f - centre) / width
    return 0.0 if d >= 1.0 else math.cos(d * math.pi * 0.5) ** power


def new_action(name):
    if name in bpy.data.actions:
        bpy.data.actions.remove(bpy.data.actions[name])
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    return act


def key(frame):
    """One full-body key. 32 bones is cheap and never leaves a channel behind."""
    for pb in arm.pose.bones:
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_euler", frame=frame)
    for name in IK_BONES:
        for c in arm.pose.bones[name].constraints:
            if c.type == 'IK':
                c.keyframe_insert("influence", frame=frame)


def snapshot():
    return {pb.name: (pb.location.copy(), pb.rotation_euler.copy())
            for pb in arm.pose.bones}, \
           [next(c for c in arm.pose.bones[n].constraints if c.type == 'IK').influence
            for n in IK_BONES]


def replay(snap):
    pose, infl = snap
    for pb in arm.pose.bones:
        loc, rot = pose[pb.name]
        pb.location = loc
        pb.rotation_euler = rot
    for n, v in zip(IK_BONES, infl):
        next(c for c in arm.pose.bones[n].constraints if c.type == 'IK').influence = v


def build(name, frames, seam=None):
    """frames: [(frame, fn)]. `seam` frame replays the very first pose exactly."""
    act = new_action(name)
    lo = frames[0][0]
    hi = seam if seam is not None else frames[-1][0]
    first = None
    for f, fn in frames:
        P.reset()
        P.hooks_home()
        fn(f)
        if first is None:
            first = snapshot()
        key(f)
    if seam is not None:
        replay(first)
        key(seam)
    # Blender 5 keeps fcurves in layers/slots and `frame_range` reads 0..0 until
    # the manual range is set. The FBX exporter reads the same property, so an
    # unset range ships a one-frame clip.
    act.use_frame_range = True
    act.frame_start, act.frame_end = float(lo), float(hi)
    return act


def drop_constraint_curves(act):
    """Remove the IK influence curves once the constraints are baked away."""
    for fc in action_fcurves(act):
        if "constraints[" not in fc.data_path:
            continue
        if hasattr(act, "fcurves"):
            act.fcurves.remove(fc)
        else:
            for layer in act.layers:
                for strip in layer.strips:
                    for cb in strip.channelbags:
                        if fc in list(cb.fcurves):
                            cb.fcurves.remove(fc)


def action_fcurves(act):
    """Blender 5 moved fcurves into action layers/slots; 4.x kept act.fcurves."""
    if hasattr(act, "fcurves"):
        return list(act.fcurves)
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                out.extend(cb.fcurves)
    return out


# ------------------------------------------------------------ ceiling clips

SWALLOW = 90


def cling_folded(f):
    """01 - the swallow travels head-to-back while the load walks the diagonals.

    Two rhythms that never line up: the swallow is one pass over 90f, the
    diagonal load shift is one pass over 90f but a quarter cycle out of phase.
    """
    t = (f - 1) / float(SWALLOW)
    swell = pulse(f, 28, 30)
    # the three spine bones peak 7f apart, which is what makes it travel
    lead = pulse(f, 21, 26) * 16.0
    mid = pulse(f, 28, 26) * 13.0
    tail = pulse(f, 35, 26) * -19.0
    drop = 0.090 * pulse(f, 28, 34) - 0.055 * pulse(f, 52, 26)
    slide = 0.045 * math.sin(2 * math.pi * (t - 0.194))
    roll = 8.0 * math.sin(2 * math.pi * (t - 0.194))
    grip = 24.0 + 6.0 * math.sin(2 * math.pi * (2 * t + 0.15))

    P.set_ik(ceiling=1.0, hands=0.0)
    P.cling(fold=1.0, sag=drop, swell=swell, shift=(slide, 0.0))
    P.addrot(P.T2, x=lead)
    P.addrot(P.T1, x=mid)
    P.addrot(P.T0, x=tail, y=roll)
    P.rot(P.HEAD, x=-17.0 - 9.0 * pulse(f, 66, 20))
    P.rot(P.JAW, x=5.0 * pulse(f, 65, 6))
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=grip)
    P.ceiling_clamp()


# 02 - four convulsions against hooks that never let go
TREMOR = [(1, 0.0, 0.0, 0.0), (3, 0.060, -9.0, 14.0), (7, 0.030, -4.0, 14.0),
          (11, 0.040, 12.0, 9.0), (17, 0.075, 3.0, -18.0), (25, 0.024, -6.0, 6.0),
          (31, 0.010, 2.0, 3.0), (40, 0.0, 0.0, 0.0)]


def cling_tremor(f):
    drop = roll = base = 0.0
    for fr, d, r, b in TREMOR:
        w = pulse(f, fr, 9 if fr > 3 else 5)
        drop += d * w
        roll += r * w
        base += b * w
    P.set_ik(ceiling=1.0, hands=0.0)
    P.cling(fold=1.0, sag=drop, splay=0.55 * pulse(f, 9, 9),
            shift=(0.012 * roll / 12.0, 0.0))
    P.addrot(P.T0, x=base * 0.5, y=roll)
    P.addrot(P.T1, x=base)
    P.rot(P.JAW, x=30.0 * pulse(f, 19, 3))
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=24.0 - 56.0 * pulse(f, 17, 7))
    P.ceiling_clamp()


def unfold_drop(f):
    """03 - wind the spring, let go at 10f, overshoot at 18f."""
    if f <= 10:
        # 1..4 over-compress past the folded rest, 4..10 start to open
        fold = 1.0 + 0.18 * pulse(f, 4, 6)
        curl = 22.0 * pulse(f, 4, 6)
        splay = 0.35 * max(0.0, (f - 4) / 6.0)
        P.set_ik(ceiling=1.0, hands=0.0)
        P.cling(fold=fold, sag=-0.070 * pulse(f, 4, 6), curl=curl, splay=splay)
        P.rot(P.JAW, x=12.0 * max(0.0, (f - 4) / 6.0))
        P.ceiling_clamp()
        return
    # released. u 0 at f10, 1 at f18, then a small recoil to f24
    u = ease(min(1.0, (f - 10) / 8.0))
    recoil = max(0.0, (f - 18) / 6.0)
    P.set_ik(ceiling=0.0, hands=0.0)
    P.hang(arch=26.0 * u * (1.0 - 0.35 * recoil),
           gape=48.0 * u - 22.0 * recoil,
           reach=1.25 * u - 0.19 * recoil,
           spread=min(1.0, (f - 10) / 3.0) * (1.0 - 0.25 * recoil))
    # the arms lag one side behind the other on the way out
    P.saddrot("DEF_AttackUpper", "L", x=-9.0 * pulse(f, 15, 6))
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=-30.0 * u + 14.0 * recoil)
    P.ceiling_clamp()


# --------------------------------------------------------------- ground clips

GROUND_BACK = 0.94          # anchor height of the settled stance
HAND = P.HAND_PLANT
HOOK = P.HOOK_PLANT


def ground(f, back=None, lift=0.0, roll=0.0, yaw=0.0, curl=0.0, push=0.0,
           hands=None, hooks=None, front=1.0, twitch=(0.0, 0.0), hump=0.5,
           jaw=0.0, wrist=None, ik=(1.0, 1.0), settle=True):
    """Every landed pose goes through here so the four contacts stay honest."""
    az = GROUND_BACK if back is None else back
    P.ground_body(anchor=(0.0, 0.46 + push, az + lift), roll=roll, yaw=yaw, curl=curl)
    P.fold_front_limbs(front, twitch, hump)
    P.plant_ground(hands, hooks, wrist)
    P.set_ik(front=0.0, rear=ik[0], hands=ik[1])
    P.rot(P.JAW, x=jaw)
    if settle:
        P.hand_clamp(P.HAND_PLANT if hands is None else hands)
        P.settle()
        P.hand_clamp(P.HAND_PLANT if hands is None else hands)


def land(f):
    """04 - slam, rebound past the rest height, two aftershocks."""
    if f <= 2:
        u = (f - 1) / 1.0
        P.set_ik(ceiling=0.0, hands=0.0)
        P.hang(arch=8.0, gape=30.0, reach=0.9, spread=0.7)
        P.ground_body(anchor=(0.0, 0.46, GROUND_BACK + 0.42 - 0.10 * u))
        return
    # 3..5 compress, 9 rebound, 12..15 settle, 18/21 aftershocks
    lift = (-0.32 * pulse(f, 5, 4)
            + 0.08 * pulse(f, 9, 5)
            - 0.024 * pulse(f, 18, 4)
            - 0.010 * pulse(f, 21, 3))
    ground(f,
           lift=lift,
           roll=9.0 * pulse(f, 9, 6) - 3.0 * pulse(f, 18, 4),
           yaw=11.0 * pulse(f, 18, 4) - 5.0 * pulse(f, 21, 3),
           curl=-18.0 * pulse(f, 5, 4),
           jaw=44.0 * pulse(f, 5, 4) + 8.0 * pulse(f, 21, 4),
           front=min(1.0, max(0.0, (f - 12) / 8.0)),
           hump=0.5,
           ik=(min(1.0, max(0.0, (f - 3) / 2.0)), min(1.0, max(0.0, (f - 2) / 1.0))))
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s,
               x=-10.0 - 26.0 * pulse(f, 5, 4) + 28.0 * pulse(f, 9, 6))


# gait: (phase, plant, stride, lift). Forward is -Y, so a planted contact
# slides +Y while it carries the body.
#
# The strides are the most the rig can actually reach - swept in reach.py, the
# hands stay exact from y -0.85 to -0.13 and the rear hooks from +0.25 to +0.65.
# Asking for more just parks the IK short of its target and the foot floats.
#
# Zero-slip speed is stride / (duty * cycle), NOT stride / cycle: a foot only has
# to keep up with the body while it is carrying it. Hands 0.65 m over a 0.42 duty
# of a 16f cycle is 2.90 m/s, which is what MODEL-SPEC now lists as the agent
# speed. The old 4.20 m/s was inherited from Crawler and would have dragged every
# foot at seven times its own stride.
SCUTTLE_N = 16
GAIT = {
    "handL": (0.00, Vector((-0.62, -0.55, 0.20)), 0.65, 0.220),
    "hookRR": (0.04, Vector((0.50, 0.42, 0.00)), 0.40, 0.080),
    "handR": (0.54, Vector((0.62, -0.55, 0.20)), 0.65, 0.220),
    "hookLR": (0.46, Vector((-0.50, 0.42, 0.00)), 0.40, 0.080),
}
# 0.42, so each diagonal is down for 42% of the cycle and 16% of it has nothing
# down at all. That flight phase is what buys the stride - without it this rig
# tops out near 1.1 m/s.
DUTY = 0.42


def _contact(f, phase, plant, stride, lift):
    t = ((f - 1) / float(SCUTTLE_N) + phase) % 1.0
    if t < DUTY:
        u = t / DUTY
        return plant + Vector((0.0, stride * (u - 0.5), 0.0)), True
    u = (t - DUTY) / (1.0 - DUTY)
    return plant + Vector((0.0, stride * (0.5 - u), lift * math.sin(math.pi * u))), False


def scuttle(f):
    """05 - the long front arms haul, the short rear limbs are dragged after."""
    c = {k: _contact(f, *v) for k, v in GAIT.items()}
    t = (f - 1) / float(SCUTTLE_N)
    ground(f,
           lift=-0.110 * (0.5 - 0.5 * math.cos(4 * math.pi * t)),
           roll=11.0 * math.sin(2 * math.pi * t),
           yaw=8.0 * math.sin(2 * math.pi * (t + 0.22)),
           curl=6.0 * math.sin(4 * math.pi * t),
           jaw=12.0 + 5.0 * math.sin(4 * math.pi * (t + 0.1)),
           hands={"L": c["handL"][0], "R": c["handR"][0]},
           hooks={"RR": c["hookRR"][0], "LR": c["hookLR"][0]},
           twitch=(14.0 * math.sin(2 * math.pi * (t + 0.30)),
                   10.0 * math.sin(2 * math.pi * (t + 0.70))),
           settle=True)
    for s, tag in (("R", "handR"), ("L", "handL")):
        P.srot("DEF_DigitsGroup", s, x=18.0 if c[tag][1] else -12.0)
    # the head keeps looking where the body is going, at 60% of the yaw
    P.rot(P.HEAD, z=-0.60 * 8.0 * math.sin(2 * math.pi * (t + 0.22)))


def attack(f):
    """06 - gather, telegraph wide at 9f, throw at 13f, land past its own feet."""
    gather = pulse(f, 5, 5)
    tele = pulse(f, 9, 5)
    strike = pulse(f, 14, 5)
    over = pulse(f, 17, 5)
    push = 0.120 * gather + 0.060 * tele - 0.260 * strike - 0.180 * over
    spread = 0.200 * gather + 0.260 * tele - 0.230 * strike - 0.140 * over
    fwd = -0.30 * strike - 0.22 * over
    hands = {s: Vector(HAND[s]) + Vector((spread * (1 if s == "R" else -1),
                                          fwd, 0.10 * (tele + strike)))
             for s in P.SIDES}
    # the rear hooks let go between 11f and 16f - the throw
    airborne = pulse(f, 13, 4)
    ground(f,
           back=GROUND_BACK,
           lift=0.22 * tele - 0.23 * over + 0.05 * strike,
           push=push,
           curl=14.0 * gather - 22.0 * tele - 8.0 * over,
           roll=7.0 * pulse(f, 20, 5),
           jaw=10.0 * gather + 30.0 * tele + 62.0 * strike + 16.0 * pulse(f, 20, 6),
           hands=hands,
           hump=0.5 + 0.5 * tele,
           ik=(1.0 - airborne, 1.0),
           settle=True)
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=-10.0 + 30.0 * tele + 44.0 * strike)
    P.addrot(P.HEAD, x=-8.0 * pulse(f, 3, 4) - 10.0 * tele)


def hit(f):
    """07 - the struck arm folds, the shock crosses the body, one late twitch."""
    a = pulse(f, 3, 3)
    b = pulse(f, 5, 4)
    c = pulse(f, 7, 4)
    d = pulse(f, 12, 3)
    ground(f,
           lift=-0.090 * b - 0.020 * d,
           roll=11.0 * (a + b) - 7.0 * c,
           yaw=-16.0 * a * 0.6,
           curl=-10.0 * b,
           jaw=26.0 * b + 5.0 * d,
           settle=True)
    # base pose is a left-side hit; export mirrors it for HitSide
    P.saddrot("DEF_AttackUpper", "L", x=24.0 * a + 6.0 * d)
    P.saddrot("DEF_AttackFore", "L", x=32.0 * a + 8.0 * d)
    P.saddrot("DEF_AttackUpper", "R", x=9.0 * b)
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=-10.0 + 40.0 * b)
    P.addrot(P.HEAD, x=-16.0 * a + 8.0 * c)


def death_fall(f):
    """08a - ceiling. Four hooks let go one at a time, then nothing holds it."""
    order = (("LF", 5), ("RR", 7), ("RF", 9), ("LR", 11))
    seize = pulse(f, 3, 3)
    limp = max(0.0, min(1.0, (f - 11) / 6.0))
    P.set_ik(hands=0.0)
    for t, fr in order:
        next(c for c in arm.pose.bones["DEF_CeilLower." + t].constraints
             if c.type == 'IK').influence = 1.0 if f < fr else 0.0
    P.cling(fold=1.0 - 0.55 * limp, sag=0.10 * limp, curl=-14.0 * limp,
            splay=0.30 * limp, wrap=1.0 - 0.4 * limp)
    P.addrot(P.T0, y=34.0 * min(1.0, max(0.0, (f - 5) / 8.0)))
    P.addrot(P.T1, x=3.0 * seize - 18.0 * limp)
    P.rot(P.JAW, x=18.0 * seize + 24.0 * limp)
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=24.0 + 16.0 * seize - 30.0 * limp)
    P.ceiling_clamp()


def death(f):
    """08b - ground. Rigor first, then it gives out and rolls onto its side."""
    rigor = pulse(f, 7, 8)
    collapse = ease(max(0.0, min(1.0, (f - 12) / 14.0)))
    twitch = pulse(f, 31, 4)
    ground(f,
           lift=0.24 * rigor - 0.39 * collapse,
           roll=78.0 * collapse,
           curl=30.0 * rigor - 18.0 * collapse,
           jaw=34.0 * rigor + 10.0 * twitch,
           front=1.0 - 0.5 * collapse,
           hump=0.5 + 0.5 * rigor,
           ik=(max(0.0, 1.0 - (f - 12) / 2.0), max(0.0, 1.0 - (f - 12) / 2.0)),
           settle=True)
    for s in P.SIDES:
        P.srot("DEF_DigitsGroup", s, x=-10.0 + 50.0 * rigor - 20.0 * collapse
               + (30.0 * twitch if s == "R" else 0.0))
        P.saddrot("DEF_AttackUpper", s, x=-26.0 * collapse)
        P.saddrot("DEF_AttackFore", s, x=40.0 * collapse)
    P.addrot(P.HEAD, x=10.0 * rigor - 6.0 * collapse)


# ------------------------------------------------------------------- measure

def loop_seam(act):
    arm.animation_data.action = act
    first, last = int(act.frame_start), int(act.frame_end)

    def sample(f):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        return {b.name: arm.pose.bones[b.name].matrix.copy()
                for b in arm.data.bones if b.use_deform}

    a, b = sample(first), sample(last)
    dt = max((a[n].to_translation() - b[n].to_translation()).length for n in a)
    dr = max(math.degrees(a[n].to_quaternion().rotation_difference(
        b[n].to_quaternion()).angle) for n in a)
    return dt, dr


def measure(act, floor=False):
    arm.animation_data.action = act
    lo, hi, worst = 9e9, -9e9, 9e9
    wide = 0.0
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        pts = P.evaluated_points()
        zl = min(p.z for p in pts)
        zh = max(p.z for p in pts)
        lo, hi = min(lo, zl), max(hi, zh)
        wide = max(wide, max(p.x for p in pts) - min(p.x for p in pts))
        worst = min(worst, zl)
    return lo, hi, wide, worst


# ---------------------------------------------------------------------- main

def frames(lo, hi, step=1):
    return list(range(lo, hi + 1, step))


def main():
    jobs = [
        ("Clinger_Cling_Folded", [(f, cling_folded) for f in frames(1, 90)], 91, True),
        ("Clinger_Cling_Tremor", [(f, cling_tremor) for f in frames(1, 40)], None, False),
        ("Clinger_Unfold_Drop", [(f, unfold_drop) for f in frames(1, 24)], None, False),
        ("Clinger_Land", [(f, land) for f in frames(1, 24)], None, False),
        ("Clinger_Scuttle", [(f, scuttle) for f in frames(1, SCUTTLE_N)], SCUTTLE_N + 1, True),
        ("Clinger_Attack_Underjaw", [(f, attack) for f in frames(1, 26)], None, False),
        ("Clinger_Hit", [(f, hit) for f in frames(1, 14)], None, False),
        ("Clinger_Death_Fall", [(f, death_fall) for f in frames(1, 18)], None, False),
        ("Clinger_Death", [(f, death) for f in frames(1, 36)], None, False),
    ]
    acts = []
    for name, fr, seam, loop in jobs:
        act = build(name, fr, seam)
        acts.append((act, loop))
        print("BUILT  %-26s %d..%d" % (name, act.frame_start, act.frame_end))

    for act, loop in acts:
        lo, hi, wide, worst = measure(act)
        line = ("MEAS   %-26s z %+.3f..%+.3f  h %.3f  wmax %.3f"
                % (act.name, lo, hi, hi - lo, wide))
        if loop:
            dt, dr = loop_seam(act)
            line += "  seam %.4f m / %.3f deg" % (dt, dr)
        print(line)

    bpy.ops.pose.select_all(action='SELECT')
    for act, _ in acts:
        arm.animation_data.action = act
        bpy.ops.nla.bake(frame_start=int(act.frame_start), frame_end=int(act.frame_end),
                         only_selected=True, visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True, bake_types={'POSE'})
        # the influence curves point at constraints that are about to go
        drop_constraint_curves(act)
        print("BAKED  %-26s %d..%d" % (act.name, act.frame_start, act.frame_end))

    # Mute rather than delete. The pose is baked onto the bones so a live
    # constraint would double-transform, but rig_test.py still needs the IK to
    # re-pose the rig, and deleting it would mean rebuilding to check anything.
    for pb in arm.pose.bones:
        for c in pb.constraints:
            c.mute = True
            c.influence = 0.0
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SCRATCH, "Clinger_Anim.blend"))
    print("ACTIONS", sorted(a.name for a in bpy.data.actions))


main()
