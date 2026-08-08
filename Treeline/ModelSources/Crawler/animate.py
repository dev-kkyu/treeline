"""Build the six Crawler actions on the rig, bake to deform bones, export FBX.

    blender Crawler_Rig.blend -b --python animate.py

Clip list / frame counts / event frames come from
docs/enemy-sheets/mockups/codex/crawler/animation/*.md
"""
import bpy
import math
import os
from mathutils import Vector

SCRATCH = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"
EXPORT_DIR = os.environ.get("CRAWLER_EXPORT_DIR", SCRATCH)

arm = bpy.data.objects["Crawler_Armature"]
mesh = bpy.data.objects["Crawler_Mesh"]

FEET = ["CTRL_FootIK.FL", "CTRL_FootIK.FR", "CTRL_FootIK.HL", "CTRL_FootIK.HR"]
BODY = "DEF_BodyRoot"
ROT_BONES = ["DEF_BodyRoot", "DEF_SpineFront", "DEF_HeadPlate", "DEF_Mouth",
             "DEF_Tail01", "DEF_Tail02", "DEF_Tail03", "DEF_Tail04"]
LEG_BONES = [n + s for s in (".L", ".R") for n in
             ("DEF_FrontUpper", "DEF_FrontLower", "DEF_FrontFoot",
              "DEF_HindThigh", "DEF_HindShin", "DEF_HindAnkle", "DEF_HindFoot")]


def ik_constraints():
    out = []
    for pb in arm.pose.bones:
        for c in pb.constraints:
            if c.type in ('IK', 'COPY_ROTATION'):
                out.append(c)
    return out


def set_ik(value):
    for c in ik_constraints():
        c.influence = value


def key_ik(frame):
    for pb in arm.pose.bones:
        for i, c in enumerate(pb.constraints):
            if c.type in ('IK', 'COPY_ROTATION'):
                c.keyframe_insert("influence", frame=frame)


def wobble(seed, t, a=1.0):
    """Deterministic smooth noise in roughly -1..1.

    Irrational frequency ratios so the three terms never line up into a beat --
    a repeating wobble reads as a machine, not a body losing control.
    """
    p = seed * 1.7137
    return a * (0.55 * math.sin(2 * math.pi * (3.0 * t + p))
                + 0.30 * math.sin(2 * math.pi * (7.13 * t + p * 2.3))
                + 0.15 * math.sin(2 * math.pi * (11.71 * t + p * 3.7)))

for pb in arm.pose.bones:
    pb.rotation_mode = 'XYZ'

_basis = {pb.name: pb.bone.matrix_local.to_3x3().inverted() for pb in arm.pose.bones}


_world_delta = {}


def wloc(name, dx, dy, dz):
    """Set a pose bone's translation from a world-space delta."""
    _world_delta[name] = Vector((dx, dy, dz))
    arm.pose.bones[name].location = _basis[name] @ Vector((dx, dy, dz))


def ground_settle(limit=0.0005):
    """Drop *or* lift the body until the mesh just touches the floor.

    ground_clamp only ever lifts, which is right while the feet carry the body.
    Once the creature rolls over there is nothing holding it up, so the body has
    to fall onto whatever surface is now facing down.
    """
    zmin = 0.0
    for _ in range(4):
        bpy.context.view_layer.update()
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        zmin = min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)
        if abs(zmin) <= limit:
            return zmin
        d = _world_delta.get(BODY, Vector((0, 0, 0)))
        wloc(BODY, d.x, d.y, d.z - zmin)
    return zmin


def ground_clamp(limit=0.0005):
    """Lift the body until nothing is below the floor.

    The generated mesh clears the ground by only 65 mm under the head, so poses
    taken straight from the spec's angles push the jaw through the floor. The
    feet are on IK, so lifting the body re-folds the legs instead of dragging
    them up.
    """
    for _ in range(6):
        bpy.context.view_layer.update()
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        zmin = min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)
        if zmin >= -limit:
            return zmin
        d = _world_delta.get(BODY, Vector((0, 0, 0)))
        wloc(BODY, d.x, d.y, d.z - zmin + limit)
    return zmin


def rot(name, rx=0.0, ry=0.0, rz=0.0):
    arm.pose.bones[name].rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))


def reset_pose():
    _world_delta.clear()
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_euler = (0, 0, 0)


def key(frame, names=None):
    for name in (names or (FEET + ROT_BONES)):
        pb = arm.pose.bones[name]
        if name in FEET or name == BODY:
            pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_euler", frame=frame)


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


def settle_action(act):
    """Per-frame ground clamp.

    The sparse spec key frames are clamped as they are authored, but the frames
    between them still dip: body height and the nose-up pitch that pays for it
    interpolate at different rates. Run before baking, while the feet are still
    on IK, so lifting the body re-folds the legs instead of floating the feet.
    """
    arm.animation_data.action = act
    pb = arm.pose.bones[BODY]
    worst = 0.0
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        for _ in range(6):
            bpy.context.view_layer.update()
            ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            zmin = min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)
            worst = min(worst, zmin)
            if zmin >= -0.0005:
                break
            pb.location = pb.location + (_basis[BODY] @ Vector((0, 0, -zmin + 0.0005)))
        pb.keyframe_insert("location", frame=f)
    return worst


def loop_seam(act):
    """Measure how badly a looping action jumps where it wraps.

    Two numbers matter. The pose gap is frame N+1 against frame 1 -- if that is
    non-zero the clip teleports. The velocity gap compares the step across the
    seam with the step just after it; a matching pose with a mismatched slope
    still reads as a stutter every cycle.
    """
    arm.animation_data.action = act
    first, last = int(act.frame_start), int(act.frame_end)

    def sample(f):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        return {b.name: arm.pose.bones[b.name].matrix.copy()
                for b in arm.data.bones if b.use_deform}

    frames = [sample(f) for f in range(first, last + 1)]

    pose_t = pose_r = 0.0
    who = ""
    for n in frames[0]:
        dt = (frames[0][n].to_translation() - frames[-1][n].to_translation()).length
        dr = math.degrees(frames[0][n].to_quaternion().rotation_difference(
            frames[-1][n].to_quaternion()).angle)
        if dt > pose_t or dr > pose_r:
            who = n
        pose_t, pose_r = max(pose_t, dt), max(pose_r, dr)

    def accel(i, j, k):
        """How much the per-frame step changes between i->j and j->k."""
        worst, name = 0.0, ""
        for n in frames[0]:
            d1 = frames[j][n].to_translation() - frames[i][n].to_translation()
            d2 = frames[k][n].to_translation() - frames[j][n].to_translation()
            v = (d2 - d1).length
            if v > worst:
                worst, name = v, n
        return worst, name

    # the seam is frame last == frame first, so the step into it comes from
    # last-1 and the step out of it goes to first+1
    seam, seam_bone = accel(len(frames) - 2, 0, 1)
    inside = sorted(accel(i, i + 1, i + 2)[0] for i in range(len(frames) - 3))
    typical = inside[int(len(inside) * 0.9)] if inside else 0.0
    verdict = "ok" if seam <= max(typical * 1.35, 0.004) else "JUMP"
    print("SEAM %-26s pose %5.2f mm %5.2f deg (%s) | seam accel %6.2f mm  "
          "worst inside %6.2f mm  %s"
          % (act.name, pose_t * 1000, pose_r, who, seam * 1000,
             inside[-1] * 1000 if inside else 0.0, verdict))
    if verdict == "JUMP":
        print("      worst at %s (p90 inside %.2f mm)" % (seam_bone, typical * 1000))
    return pose_t, pose_r


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


# ---------------------------------------------------------------- gait helper

def gait(u, duty, stride, lift):
    """Foot offset (dy, dz) for cycle phase u in [0,1).

    Stance runs backward (+y) at constant speed so a planted foot never slides.
    The swing is deliberately not a sine: the foot whips forward in the first
    third and then hangs, so the leg reads as thrown rather than swung. A smooth
    sine here is the single thing that made the whole creature look like a dog.
    """
    u = u % 1.0
    if u < duty:
        s = u / duty
        return stride * (s - 0.5), 0.0
    s = (u - duty) / (1.0 - duty)
    whip = s ** 0.5                      # forward travel front-loaded
    arc = math.sin(math.pi * (s ** 0.75))  # peak early, then drop onto the floor
    return stride * (0.5 - whip), lift * arc


def drift(t, phase):
    """Wandering value in roughly -1..1 that is exactly 1-periodic in t.

    The head sweep used to run at 1.5x the gait so it never fell into step with
    the legs. That does not survive a loop: after one cycle the term is halfway
    through its own, so the clip snapped back at the seam. Stacking the 1st and
    3rd harmonic keeps the motion from reading as a simple rhythm while landing
    on the same value and the same slope at both ends of the cycle.
    """
    return (0.62 * math.sin(2 * math.pi * t + phase)
            + 0.38 * math.sin(6 * math.pi * t + phase * 1.7))


def slam(t, per_cycle=2):
    """Body height curve: fast drop on impact, slow climb back. Range -1..0."""
    s = (t * per_cycle) % 1.0
    if s < 0.14:
        return -((s / 0.14) ** 0.55)
    r = (s - 0.14) / 0.86
    return -((1.0 - r) ** 2.2)


def build_locomotion(name, frames, feet, duty, stride_f, stride_h, lift_f, lift_h,
                     body_drop, bob, pitch, lurch, yaw, tail_swing, neck_counter,
                     mouth_open, head_sweep):
    """feet: {bone: (phase, stride_mult, lift_mult, duty_mult)}

    The per-foot multipliers break left/right symmetry. A trot with matched
    limbs reads as a healthy animal no matter how large the motion is.
    """
    act = new_action(name)
    period = frames  # frame 1 and frame frames+1 hold the same pose
    for f in range(1, frames + 2):
        t = (f - 1) / period
        reset_pose()
        for foot, cfg in feet.items():
            ph, sm, lm, dm = cfg
            stride = (stride_f if ".F" in foot else stride_h) * sm
            lift = (lift_f if ".F" in foot else lift_h) * lm
            dy, dz = gait(t - ph, min(duty * dm, 0.9), stride, lift)
            wloc(foot, 0.0, dy, dz)
            # keep the sole roughly flat: tip the foot up as it leaves the ground
            arm.pose.bones[foot].rotation_euler = (math.radians(-24.0 * (dz / max(lift, 1e-6))), 0, 0)

        impact = slam(t)                       # -1 at touchdown, 0 at full extension
        wloc(BODY, lurch * 0.5 * math.sin(2 * math.pi * t + 1.1),
             0.02 * slam(t, 1), -body_drop + bob * impact)
        body_pitch = pitch * impact
        body_yaw = yaw * drift(t, 0.0)
        rot(BODY, body_pitch, lurch * 22 * math.sin(2 * math.pi * t), body_yaw)
        # the neck now only partly cancels the body pitch; a perfectly levelled
        # head is what made it look like it was politely watching the floor
        rot("DEF_SpineFront", -body_pitch * neck_counter, lurch * 14,
            -body_yaw * 0.4 + head_sweep * 0.5 * drift(t, 2.0))
        rot("DEF_HeadPlate", -pitch * 0.35 * impact, 0,
            head_sweep * drift(t, 2.6))
        rot("DEF_Mouth", mouth_open + mouth_open * 0.45 * impact, 0, 0)
        for i in range(4):
            lag = 0.13 * (i + 1)
            amp = tail_swing * (0.5 + 0.5 * i)
            rot("DEF_Tail0%d" % (i + 1),
                -3.0 - 2.0 * (i + 1) * math.sin(2 * math.pi * (t - lag)), 0,
                amp * math.sin(2 * math.pi * (t - lag)))
        ground_clamp()
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, frames + 1
    return act


def build_oneshot(name, frames, poses):
    """poses: {frame: {bone: dict(dy=,dz=,rx=,ry=,rz=)}}"""
    act = new_action(name)
    for f in sorted(poses):
        reset_pose()
        for bone, p in poses[f].items():
            if "dy" in p or "dz" in p or "dx" in p:
                wloc(bone, p.get("dx", 0.0), p.get("dy", 0.0), p.get("dz", 0.0))
            if "rx" in p or "ry" in p or "rz" in p:
                rot(bone, p.get("rx", 0.0), p.get("ry", 0.0), p.get("rz", 0.0))
        ground_clamp()
        key(f)
    # plain bezier overshoots between the spec's sparse key frames and dips the
    # jaw through the floor; clamped handles keep every curve inside its keys
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        fc.update()
    act.frame_start, act.frame_end = 1, frames
    return act


# ------------------------------------------------------------------- 1 idle

def build_idle():
    act = new_action("Crawler_Idle_Low")
    for f in range(1, 47):
        t = (f - 1) / 45.0
        reset_pose()
        # idle is the one clip that must stay nearly still, so the threat comes
        # from the head hunting sideways and the jaw hanging open, not from size
        sweep = math.sin(2 * math.pi * t)
        hunt = math.sin(6 * math.pi * t + 0.7)
        wloc(BODY, 0.012 * sweep, 0.0, -0.012 * abs(hunt))
        rot(BODY, 0, 2.5 * sweep, 3.0 * sweep)
        rot("DEF_SpineFront", 1.5 * hunt, 3.0 * sweep, -7.0 * sweep)
        rot("DEF_HeadPlate", 2.0 * hunt, 0, -11.0 * math.sin(2 * math.pi * t + 1.3))
        rot("DEF_Mouth", 14.0 + 5.0 * hunt, 0, 0)
        for i in range(4):
            lag = 0.09 * (i + 1)
            rot("DEF_Tail0%d" % (i + 1), -2.0, 0,
                (5 + 4 * i) * math.sin(2 * math.pi * (t - lag)))
        ground_clamp()
        key(f)
    act.frame_start, act.frame_end = 1, 46
    return act


# ------------------------------------------------------------------ 4 attack
F, H = "CTRL_FootIK.F", "CTRL_FootIK.H"


def build_attack():
    # The old version eased from anticipation into the strike at roughly one
    # speed, which is why it read as a lunge and not a snap. Now 4..9 holds a
    # coiled pose almost still and 9..13 covers the whole distance.
    coil = {
        BODY: dict(dy=0.11, dz=-0.02, rx=-3, ry=4),
        "DEF_Mouth": dict(rx=34), "DEF_HeadPlate": dict(rx=3, rz=-9),
        "DEF_SpineFront": dict(rx=1, ry=6, rz=7),
        F + "L": dict(dx=-0.09, dy=0.05), F + "R": dict(dx=0.09, dy=0.05),
        "DEF_Tail01": dict(rx=-14, rz=18), "DEF_Tail02": dict(rx=-14, rz=22),
        "DEF_Tail03": dict(rx=-10, rz=20), "DEF_Tail04": dict(rz=14),
    }
    strike = {
        BODY: dict(dy=-0.17, dz=0.03, rx=-2, ry=-3),
        "DEF_Mouth": dict(rx=72), "DEF_HeadPlate": dict(rx=-11, rz=5),
        "DEF_SpineFront": dict(rx=-7, ry=-4, rz=-5),
        H + "L": dict(dz=0.06, dy=0.04), H + "R": dict(dz=0.06, dy=0.04),
        "DEF_Tail01": dict(rx=6, rz=-16), "DEF_Tail02": dict(rx=8, rz=-22),
        "DEF_Tail03": dict(rx=6, rz=-18), "DEF_Tail04": dict(rz=-12),
    }
    return build_oneshot("Crawler_Attack_Underbite", 22, {
        1: {BODY: dict(dz=-0.02), "DEF_Mouth": dict(rx=6)},
        4: {BODY: dict(dy=0.09, dz=-0.02, rx=-2, ry=3), "DEF_Mouth": dict(rx=22),
            F + "L": dict(dx=-0.08, dy=0.04), F + "R": dict(dx=0.08, dy=0.04),
            "DEF_HeadPlate": dict(rx=3, rz=-6), "DEF_SpineFront": dict(ry=4, rz=5),
            "DEF_Tail01": dict(rx=-12, rz=14), "DEF_Tail02": dict(rx=-12, rz=18),
            "DEF_Tail03": dict(rx=-9, rz=16)},
        7: coil,
        9: coil,
        11: {BODY: dict(dy=-0.06, dz=0.01), "DEF_Mouth": dict(rx=58),
             H + "L": dict(dz=0.04), H + "R": dict(dz=0.04),
             "DEF_HeadPlate": dict(rx=-6, rz=-2), "DEF_SpineFront": dict(rx=-4),
             "DEF_Tail01": dict(rx=-2, rz=4), "DEF_Tail02": dict(rz=2)},
        13: strike,
        14: strike,
        16: {BODY: dict(dy=-0.07, dz=-0.04, ry=-2), "DEF_Mouth": dict(rx=20),
             "DEF_HeadPlate": dict(rx=-2, rz=3), "DEF_SpineFront": dict(rx=-2),
             "DEF_Tail01": dict(rz=-8), "DEF_Tail02": dict(rz=-10)},
        18: {BODY: dict(dy=-0.02, dz=-0.01), "DEF_Mouth": dict(rx=11),
             "DEF_Tail01": dict(rz=4), "DEF_Tail02": dict(rz=6)},
        22: {BODY: dict(), "DEF_Mouth": dict(rx=7)},
    })


# --------------------------------------------------------------------- 5 hit
def build_hit():
    # 0.4 s is not much room, so all of it goes into one violent whip: the head
    # snaps away from the impact and comes back aimed at whatever shot it.
    return build_oneshot("Crawler_Hit", 12, {
        1: {BODY: dict(), "DEF_Mouth": dict(rx=8)},
        3: {BODY: dict(dx=-0.09, dz=-0.04, ry=-13, rz=6),
            "DEF_SpineFront": dict(ry=14, rz=17), "DEF_Mouth": dict(rx=34),
            "DEF_HeadPlate": dict(rz=22, ry=-9),
            F + "R": dict(dx=-0.05), H + "R": dict(dx=-0.04)},
        5: {BODY: dict(dx=-0.13, dz=-0.06, ry=-17, rz=9),
            "DEF_SpineFront": dict(ry=17, rz=21), "DEF_Mouth": dict(rx=40),
            "DEF_HeadPlate": dict(rz=26, ry=-12),
            F + "L": dict(dx=-0.07, dz=0.03), H + "L": dict(dx=-0.06),
            "DEF_Tail01": dict(rz=-24), "DEF_Tail02": dict(rz=-30),
            "DEF_Tail03": dict(rz=-26), "DEF_Tail04": dict(rz=-18)},
        8: {BODY: dict(dx=-0.03, dz=-0.015, ry=-4, rz=-3),
            "DEF_SpineFront": dict(ry=4, rz=-6), "DEF_Mouth": dict(rx=22),
            "DEF_HeadPlate": dict(rz=-8), "DEF_Tail01": dict(rz=14),
            "DEF_Tail02": dict(rz=20), "DEF_Tail03": dict(rz=16)},
        12: {BODY: dict(), "DEF_Mouth": dict(rx=9)},
    })


# ------------------------------------------------------------------- 6 death
def build_death():
    """30 f. The creature rolls onto its back while the whole body writhes.

    Two things make this clip different from the others. It rolls past the point
    where the legs can hold it, so the IK is faded out at 6..10 f and the limbs
    finish on FK -- otherwise the feet stay welded to the floor and the legs tear
    off the body. And with nothing holding it up, the body has to be settled onto
    the floor every frame rather than only pushed out of it.
    """
    act = new_action("Crawler_Death")
    ROLL_END = -184.0
    FREEZE = 26

    for f in range(1, 31):
        fr = min(f, FREEZE)               # 26..30 hold one pose exactly
        t = (fr - 1) / 29.0
        reset_pose()

        # roll: slow tip, then it goes over all at once, then a small rock back
        r = max(0.0, min(1.0, (fr - 3) / 17.0))
        roll = ROLL_END * (r ** 1.6) * (1.0 + 0.06 * math.sin(math.pi * r) )
        if fr > 20:
            roll = ROLL_END + 7.0 * math.exp(-(fr - 20) / 2.2) * math.cos((fr - 20) * 1.9)

        # how hard it is still fighting: peaks at 8 f, gone by 26 f
        if fr < 6:
            fight = fr / 6.0
        else:
            fight = max(0.0, 1.0 - ((fr - 6) / 19.0) ** 1.4)

        ik = 1.0 if fr <= 6 else max(0.0, 1.0 - (fr - 6) / 4.0)
        set_ik(ik)

        wloc(BODY, 0.10 * fight * wobble(1, t) - 0.04 * r,
             0.06 * fight * wobble(2, t), 0.0)
        rot(BODY,
            -6.0 * r + 9.0 * fight * wobble(3, t),
            roll,
            11.0 * fight * wobble(4, t))

        # spine and head twist against the roll, then give up
        rot("DEF_SpineFront", 7.0 * fight * wobble(5, t) + 4.0 * r,
            -14.0 * fight * wobble(6, t), 17.0 * fight * wobble(7, t) - 6.0 * r)
        rot("DEF_HeadPlate", 9.0 * fight * wobble(8, t), 0.0,
            21.0 * fight * wobble(9, t) - 5.0 * r)
        rot("DEF_Mouth", 26.0 + 34.0 * fight * abs(wobble(10, t)))

        # The roll axis runs along the body and tips upward toward the tail, so
        # 180 deg of roll throws the 1.5 m tail into the air. The droop has to
        # flip with the roll to keep pointing at the floor in world space.
        droop = 17.0 * r * (-math.cos(math.radians(roll)))
        for i in range(4):
            lag = 0.05 * (i + 1)
            amp = (7.0 + 6.0 * i)
            rot("DEF_Tail0%d" % (i + 1),
                droop + 0.5 * amp * fight * wobble(20 + i, t - lag), 0.0,
                amp * fight * wobble(30 + i, t - lag) - 5.0 * r * (i + 1) * 0.4)

        # once the IK lets go the legs curl in on themselves, still twitching
        curl = max(0.0, min(1.0, (fr - 6) / 8.0))
        for side, sgn in ((".L", 1.0), (".R", -1.0)):
            rot("DEF_FrontUpper" + side, 26 * curl + 16 * fight * wobble(41 * sgn, t), 0,
                13 * curl * sgn)
            rot("DEF_FrontLower" + side, 62 * curl + 22 * fight * wobble(43 * sgn, t))
            rot("DEF_FrontFoot" + side, -34 * curl + 14 * fight * wobble(45 * sgn, t))
            rot("DEF_HindThigh" + side, -24 * curl + 15 * fight * wobble(47 * sgn, t), 0,
                -11 * curl * sgn)
            rot("DEF_HindShin" + side, -58 * curl + 24 * fight * wobble(49 * sgn, t))
            rot("DEF_HindAnkle" + side, 42 * curl + 18 * fight * wobble(51 * sgn, t))
            rot("DEF_HindFoot" + side, -20 * curl + 10 * fight * wobble(53 * sgn, t))

        ground_settle()
        key(f, FEET + ROT_BONES + LEG_BONES)
        key_ik(f)

    set_ik(1.0)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, 30
    return act


def main():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    actions = []
    actions.append(build_idle())

    # phase / stride / lift / duty multipliers per foot. The right side leads and
    # the left hind drags, so no two limbs repeat the same arc.
    SK = dict(frames=24, duty=0.52, stride_f=0.44, stride_h=0.38,
              lift_f=0.13, lift_h=0.17, body_drop=0.015, bob=0.055,
              pitch=5.0, lurch=0.055, yaw=7.0, tail_swing=26.0,
              neck_counter=0.45, mouth_open=9.0, head_sweep=13.0)
    actions.append(build_locomotion(
        "Crawler_Skitter",
        feet={"CTRL_FootIK.FL": (0.00, 1.00, 1.00, 1.00),
              "CTRL_FootIK.HR": (0.07, 1.15, 0.85, 0.92),
              "CTRL_FootIK.FR": (0.47, 0.78, 1.30, 1.10),
              "CTRL_FootIK.HL": (0.61, 0.88, 1.15, 0.84)}, **SK))

    SP = dict(frames=18, duty=0.30, stride_f=0.50, stride_h=0.44,
              lift_f=0.19, lift_h=0.23, body_drop=0.0, bob=0.085,
              pitch=8.0, lurch=0.045, yaw=5.0, tail_swing=20.0,
              neck_counter=0.35, mouth_open=12.0, head_sweep=9.0)
    actions.append(build_locomotion(
        "Crawler_Sprint",
        feet={"CTRL_FootIK.HR": (0.00, 1.10, 1.00, 1.00),
              "CTRL_FootIK.HL": (0.09, 0.86, 1.25, 0.88),
              "CTRL_FootIK.FR": (0.40, 1.00, 1.15, 1.05),
              "CTRL_FootIK.FL": (0.50, 0.82, 0.90, 0.90)}, **SP))

    actions.append(build_attack())
    actions.append(build_hit())
    actions.append(build_death())

    for tag, cfg, fps in (("Skitter", SK, 30.0), ("Sprint", SP, 30.0)):
        stance = cfg["duty"] * cfg["frames"] / fps
        print("SPEED %-8s stance %.3fs front %.2f m/s  hind %.2f m/s" % (
            tag, stance, cfg["stride_f"] / stance, cfg["stride_h"] / stance))

    # bake control rig down onto the deform bones
    for act in actions:
        # only Death animates constraint influence; without this reset the value
        # it left behind leaks into whichever action is evaluated next
        set_ik(1.0)
        print("SETTLE %-26s worst dip %7.4f" % (act.name, settle_action(act)))

    for act in actions:
        if act.name in ("Crawler_Idle_Low", "Crawler_Skitter", "Crawler_Sprint"):
            set_ik(1.0)
            loop_seam(act)

    bpy.ops.pose.select_all(action='SELECT')
    for act in actions:
        set_ik(1.0)
        arm.animation_data.action = act
        bpy.ops.nla.bake(frame_start=int(act.frame_start), frame_end=int(act.frame_end),
                         only_selected=True, visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True, bake_types={'POSE'})
        print("BAKED", act.name, int(act.frame_start), int(act.frame_end))

    # constraints have been baked in; leaving them on would double-transform
    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SCRATCH, "Crawler_Rig.blend"))
    print("ACTIONS", [a.name for a in bpy.data.actions])


main()
