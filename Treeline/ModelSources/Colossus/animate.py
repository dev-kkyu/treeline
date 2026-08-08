"""Build the Colossus actions on the rig, bake to deform bones, save the rig.

    blender Colossus_Rig_base.blend -b --python animate.py

Clip list, frame counts and event frames come from
docs/enemy-sheets/mockups/codex/colossus/final/animation/*.md
"""
import bpy
import math
import os
from mathutils import Vector, Matrix

SCRATCH = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"

arm = bpy.data.objects["Colossus_Armature"]
mesh = bpy.data.objects["Colossus_Mesh"]

BODY = "DEF_Pelvis"
MOTION = "Colossus_Motion"
LIMBS = ["FL", "FR", "HL", "HR"]
CTRL = {k: "CTRL_LimbIK." + k for k in LIMBS}
# the bone whose tail the IK drives, i.e. the thing that must stay planted
EFF = {"FL": "DEF_Wrist.L", "FR": "DEF_Wrist.R",
       "HL": "DEF_Foot.L", "HR": "DEF_Foot.R"}
# fingers and toes reach past the contact, so they have to be held flat while the
# sole above them rolls, exactly as a real toe joint does
DISTAL = {"FL": "DEF_Digits.L", "FR": "DEF_Digits.R",
          "HL": "DEF_Toes.L", "HR": "DEF_Toes.R"}

# 01-idle-weight.md wants torso roll spread over the three spine joints instead of
# hinging on one; these weights sum to 1 so the numbers below stay world angles.
SPINE_SPLIT = (("DEF_Spine01", 0.22), ("DEF_Spine02", 0.22),
               ("DEF_Spine03", 0.22), ("DEF_Chest", 0.34))
HUMPS = ("DEF_Hump.L", "DEF_Hump.R", "DEF_HumpTip.L", "DEF_HumpTip.R")

ROT_BONES = ([n for n, _ in SPINE_SPLIT] + list(HUMPS)
             + ["DEF_Pelvis", "DEF_HeadBase", "DEF_HeadTip", "DEF_Jaw"])
KEYED = list(CTRL.values()) + list(DISTAL.values()) + ROT_BONES + [MOTION]

for pb in arm.pose.bones:
    pb.rotation_mode = 'XYZ'

_fwd = {pb.name: pb.bone.matrix_local.to_3x3() for pb in arm.pose.bones}
_basis = {n: m.inverted() for n, m in _fwd.items()}
_world_delta = {}


def clamp(v, lo, hi):
    return lo if v < lo else (hi if v > hi else v)


def wloc(name, dx, dy, dz):
    """Set a pose bone's translation from a world-space delta."""
    _world_delta[name] = Vector((dx, dy, dz))
    arm.pose.bones[name].location = _basis[name] @ Vector((dx, dy, dz))


def world_rot(pitch=0.0, roll=0.0, yaw=0.0):
    return (Matrix.Rotation(math.radians(yaw), 3, 'Z')
            @ Matrix.Rotation(math.radians(roll), 3, 'Y')
            @ Matrix.Rotation(math.radians(pitch), 3, 'X'))


def wrot(name, pitch=0.0, roll=0.0, yaw=0.0):
    """Rotate a bone by world-frame angles.

    Every bone in this rig has a different roll - the arms are rolled to their
    own elbow plane - so a raw local euler means something different on each one.
    Pitch is about world +X (positive dips the head end), roll about world +Y
    (positive leans right), yaw about world +Z (positive turns right).
    """
    B = _fwd[name]
    arm.pose.bones[name].rotation_euler = (
        B.inverted() @ world_rot(pitch, roll, yaw) @ B).to_euler('XYZ')


# control head sits at the joint, tail at the ground contact
_CONTACT_OFF = {k: (arm.pose.bones[CTRL[k]].bone.tail_local
                    - arm.pose.bones[CTRL[k]].bone.head_local) for k in LIMBS}


# Where each limb hangs from, and how far that joint may be from it. The upper
# bound is the chain laid straight; the lower one is the fold limit, and on the
# arm it is the number that keeps catching this rig out - a 2.53 m upper arm and
# a 1.33 m forearm with the elbow capped at 95 degrees of fold cannot bring the
# wrist closer than 1.85 m, so anything asked for inside that just detaches.
ROOT_JOINT = {"FL": "DEF_UpperArm.L", "FR": "DEF_UpperArm.R",
              "HL": "DEF_Thigh.L", "HR": "DEF_Thigh.R"}
REACH = {"F": (1.90, 3.84), "H": (0.52, 1.405)}
_reach_pull = [0.0, ""]


def reach_guard(k, head_target):
    """Pull an out-of-range joint target back onto the limb's shell.

    Poses are written by eye from the sheets, and a pose that reads well on paper
    is regularly outside what the arm can do. Clamping here moves the hand a few
    centimetres; not clamping leaves the arm visibly unhooked from it.
    """
    root = arm.matrix_world @ arm.pose.bones[ROOT_JOINT[k]].head
    lo, hi = REACH[k[0]]
    v = head_target - root
    d = v.length
    if d < 1e-6:
        return head_target
    want = lo if d < lo else (hi if d > hi else d)
    if want != d and abs(want - d) > _reach_pull[0]:
        _reach_pull[0], _reach_pull[1] = abs(want - d), k
    return root + v * (want / d)


def plant(k, dy, dz, pitch=0.0, dx=0.0, yaw=0.0, roll=0.0, distal=True):
    """Put a limb's ground contact where the gait wants it, at the angle asked.

    The control turns about its head - the joint - so tilting it would drag the
    contact along. Cancelling that here means the caller only ever thinks about
    where the hand or foot touches, and the joint lands wherever it has to.

    The twist fades out as the contact comes down. A hand rolled over on the
    floor tips its own knuckles through it and drags the wrist 0.23 m sideways,
    which was enough to break both the planting and the reach.
    """
    roll = roll * clamp(abs(dz) / 0.30, 0.0, 1.0)
    off = _CONTACT_OFF[k]
    R = world_rot(pitch, roll, yaw)
    rest_head = arm.pose.bones[CTRL[k]].bone.head_local
    contact = contact_rest(k) + Vector((dx, dy, dz))
    head_target = reach_guard(k, contact - R @ off)
    d = head_target - rest_head
    wloc(CTRL[k], d.x, d.y, d.z)
    wrot(CTRL[k], pitch=pitch, roll=roll, yaw=yaw)
    if distal:
        wrot(DISTAL[k], pitch=-pitch)


def contact_rest(k):
    return arm.pose.bones[CTRL[k]].bone.tail_local.copy()


# The wring: the lower spine turns one way while the upper spine and chest turn
# the other, so the torso ends up fighting itself instead of pointing somewhere.
# It nets out near zero, which keeps it readable as a body rather than a turn.
SPINE_WRING = (0.55, 0.35, -0.40, -0.50)


def torso(pitch, roll, yaw, twist=0.0):
    for (name, k), w in zip(SPINE_SPLIT, SPINE_WRING):
        wrot(name, pitch * k, roll * k, yaw * k + twist * w)


def humps(now, lagged, follow=0.6, split=0.0):
    """00-rigging.md caps the humps at 4 deg and 01-idle-weight.md wants them to
    follow the chest at 0.6 and arrive late. They are children of the chest, so
    reaching a fraction of a lagged chest means cancelling the live one first."""
    local = [clamp(follow * l - n, -4.0, 4.0) for n, l in zip(now, lagged)]
    for h in HUMPS:
        # the two humps never agree with each other; one leads and one lags
        sgn = 1.0 if h.endswith(".R") else -1.0
        tip = 1.35 if "Tip" in h else 1.0
        wrot(h, local[0] * 0.5 + sgn * split * 0.35 * tip,
             local[1] * 0.5 + sgn * split * 0.55 * tip,
             local[2] * 0.5 + sgn * split * 0.30 * tip)


def head(chest_pitch, chest_roll, chest_yaw, counter=0.4, yaw_extra=0.0,
         pitch_extra=0.0, jaw=2.0, roll_extra=0.0):
    """There is no neck, so the head bone stays inside the tight limits in
    00-rigging.md and only partly cancels the chest."""
    wrot("DEF_HeadBase",
         clamp(-counter * chest_pitch + pitch_extra, -10.0, 10.0),
         clamp(-counter * chest_roll + roll_extra, -18.0, 18.0),
         clamp(-counter * chest_yaw + yaw_extra, -12.0, 12.0))
    wrot("DEF_Jaw", pitch=clamp(jaw, 0.0, 55.0))


def reset_pose():
    _world_delta.clear()
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_euler = (0, 0, 0)


def key(frame, names=None):
    for name in (names or KEYED):
        pb = arm.pose.bones[name]
        if name in CTRL.values() or name in (BODY, MOTION):
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


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


def ik_constraints():
    return [(pb, c) for pb in arm.pose.bones for c in pb.constraints
            if c.type in ('IK', 'COPY_ROTATION')]


def set_ik(value):
    for _, c in ik_constraints():
        c.influence = value


# ------------------------------------------------------------------- measuring

def eval_mesh_zmin():
    bpy.context.view_layer.update()
    ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    return min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)


_DOM = None


def dominant_group():
    global _DOM
    if _DOM is None:
        names = [g.name for g in mesh.vertex_groups]
        _DOM = []
        for vt in mesh.data.vertices:
            best, bw = "", 0.0
            for g in vt.groups:
                if g.weight > bw:
                    best, bw = names[g.group], g.weight
            _DOM.append(best)
    return _DOM


def worst_dip(act):
    """Deepest floor penetration and which body part causes it."""
    arm.animation_data.action = act
    dom = dominant_group()
    worst, where = 0.0, ""
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        co = [(mesh.matrix_world @ v.co) for v in ev.data.vertices]
        i = min(range(len(co)), key=lambda j: co[j].z)
        if co[i].z < worst:
            worst, where = co[i].z, "%s@%d" % (dom[i], f)
    return worst, where


def guard_action(act):
    """Per-frame reach clamp.

    Guarding the sparse key frames is not enough: two reachable keys can have
    every frame between them outside the shell, which is how an arm swinging up
    to the cable came off it in the middle of the raise. Same shape as the floor
    pass below, and for the same reason.
    """
    arm.animation_data.action = act
    worst, where = 0.0, ""
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        for k in LIMBS:
            pb = arm.pose.bones[CTRL[k]]
            now = arm.matrix_world @ pb.head
            fixed = reach_guard(k, now)
            pull = (fixed - now).length
            if pull > 1e-4:
                pb.location = pb.location + (_basis[CTRL[k]] @ (fixed - now))
                if pull > worst:
                    worst, where = pull, "%s@%d" % (k, f)
            pb.keyframe_insert("location", frame=f)
    return worst, where


def settle_action(act):
    """Per-frame floor clamp, run while the limbs are still on IK so that lifting
    the body re-folds them instead of floating the contacts."""
    arm.animation_data.action = act
    pb = arm.pose.bones[BODY]
    worst = 0.0
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        for _ in range(6):
            zmin = eval_mesh_zmin()
            worst = min(worst, zmin)
            # A planted sole is not perfectly flat, so a centimetre of the mesh
            # dips under load. Chasing that with the pelvis costs more than it
            # buys: it lifts the body out of IK reach and the contacts start to
            # slide. Only gross penetration is worth correcting.
            if zmin >= -0.025:
                break
            pb.location = pb.location + (_basis[BODY] @ Vector((0, 0, -zmin + 0.0005)))
        pb.keyframe_insert("location", frame=f)
    return worst


def sample_effectors(act):
    """World position of each planted point, per frame."""
    arm.animation_data.action = act
    out = []
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        out.append({k: (arm.matrix_world @ arm.pose.bones[EFF[k]].tail).copy()
                    for k in LIMBS})
    return out


def ik_error(act):
    """How far each contact misses the control it is supposed to sit on. Anything
    large means the chain ran out of reach and the contact is sliding."""
    arm.animation_data.action = act
    worst, where = 0.0, ""
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        for k in LIMBS:
            d = ((arm.matrix_world @ arm.pose.bones[EFF[k]].tail)
                 - (arm.matrix_world @ arm.pose.bones[CTRL[k]].tail)).length
            if d > worst:
                worst, where = d, "%s@%d" % (k, f)
    return worst, where


def stance_report(act, feet, duty, period, stride):
    """Two things 02-walk-knuckle.md insists on: never fewer than three contacts,
    and a planted contact that travels backward at one steady speed."""
    frames = sample_effectors(act)
    n = len(frames) - 1
    min_support, worst_frame = 4, 0
    flight = longest_flight = 0
    for i in range(n):
        t = i / float(period)
        support = sum(1 for k in LIMBS if ((t - feet[k][0]) % 1.0) < duty)
        flight = flight + 1 if support == 0 else 0
        longest_flight = max(longest_flight, flight)
        if support < min_support:
            min_support, worst_frame = support, i + 1
    per_frame = stride / (duty * period)
    worst_slip, slip_where = 0.0, ""
    for k in LIMBS:
        for i in range(n):
            t0, t1 = i / float(period), (i + 1) / float(period)
            if ((t0 - feet[k][0]) % 1.0) < duty and ((t1 - feet[k][0]) % 1.0) < duty:
                step = (frames[i + 1][k] - frames[i][k]).y
                if abs(step - per_frame) > worst_slip:
                    worst_slip, slip_where = abs(step - per_frame), "%s@%d" % (k, i + 1)
    print("STANCE %-24s min support %d (f%d)  longest flight %df  "
          "step error %.1f mm (%s, want %.1f mm/f)"
          % (act.name, min_support, worst_frame, longest_flight,
             worst_slip * 1000, slip_where, per_frame * 1000))
    return min_support


def loop_seam(act):
    """Pose gap and velocity gap where a looping action wraps."""
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
        worst, name = 0.0, ""
        for n in frames[0]:
            d1 = frames[j][n].to_translation() - frames[i][n].to_translation()
            d2 = frames[k][n].to_translation() - frames[j][n].to_translation()
            v = (d2 - d1).length
            if v > worst:
                worst, name = v, n
        return worst, name

    seam, seam_bone = accel(len(frames) - 2, 0, 1)
    # the last triple straddles the duplicate end frame, which is real interior
    # data; leaving it out let the seam look worse than a neighbour it matches
    inside = sorted(accel(i, i + 1, i + 2)[0] for i in range(len(frames) - 2))
    typical = inside[int(len(inside) * 0.9)] if inside else 0.0
    # A gait's touchdowns are its biggest accelerations by design, so the test is
    # whether the seam is worse than the worst thing already inside the clip -
    # not whether it stands out from the quiet frames. The 1.2 allows for the two
    # landings not being exactly equal: the legs match to 0.2%, but the free
    # shoulder leaves the IK solver a spare axis and the left forearm lands a few
    # percent harder than the right. Anything genuinely broken is multiples over.
    verdict = "ok" if seam <= max(inside[-1] * 1.20, typical * 1.35, 0.004) else "JUMP"
    print("SEAM %-24s pose %5.2f mm %5.2f deg (%s) | seam accel %6.2f mm  "
          "worst inside %6.2f mm  %s"
          % (act.name, pose_t * 1000, pose_r, who, seam * 1000,
             inside[-1] * 1000 if inside else 0.0, verdict))
    if verdict == "JUMP":
        print("      worst at %s (p90 inside %.2f mm)" % (seam_bone, typical * 1000))
    return pose_t, pose_r


# ---------------------------------------------------------------- curve shapes

def whip_coeffs(duty, throw=3.0):
    """Cubic swing: whip(0)=0, whip(1)=1, and whip'(1) set so that the limb's
    forward speed at touchdown already matches the backward speed of stance."""
    c = throw - 2.0 - (1.0 - duty) / duty
    return throw, 1.0 - throw - c, c


W_A, W_B, W_C = whip_coeffs(0.78)


def bump(s):
    """0 at both ends, 1 in the middle, flat where it joins."""
    return math.sin(math.pi * clamp(s, 0.0, 1.0)) ** 1.2


def smoothstep(s):
    s = clamp(s, 0.0, 1.0)
    return s * s * (3.0 - 2.0 * s)


def gait(u, duty, stride, lift, centre):
    """Contact offset (dy, dz) for cycle phase u.

    Stance runs backward at one constant speed, which is what keeps a planted
    contact from sliding once root motion is added. `centre` biases the whole
    swing backward: the rest pose already has the hands far out in front, so a
    stroke centred on rest would run out of forward reach long before it runs out
    of backward reach.
    """
    u = u % 1.0
    if u < duty:
        return centre + stride * (u / duty - 0.5), 0.0
    s = (u - duty) / (1.0 - duty)
    # Cubic through (0,0) and (1,1), thrown out fast and arriving at exactly the
    # speed stance carries on with. A plain s**0.5 lands the limb while it is
    # still racing forward, which on a body this heavy reads as a slap; it also
    # made the loop seam the largest acceleration in the clip.
    whip = W_A * s + W_B * s * s + W_C * s ** 3
    arc = math.sin(math.pi * (s ** 0.75)) ** 1.5  # eases off the floor and back on
    return centre + stride * (0.5 - whip), lift * arc


def slam(t, per_cycle):
    """Body height curve: fast drop on impact, slow climb back. Range -1..0."""
    s = (t * per_cycle) % 1.0
    if s < 0.14:
        return -((s / 0.14) ** 0.55)
    return -((1.0 - (s - 0.14) / 0.86) ** 2.2)


# --------------------------------------------------------------- 1 idle weight

IDLE_FRAMES = 72


def idle_torso(t):
    """Weight walks front-left -> centre -> right-rear, never side to side.

    Only whole harmonics of the 72-frame cycle appear here; a 1.5x term would put
    the clip halfway through that term at the seam, which is exactly the fault
    that made the Crawler head jump 25 degrees every loop.
    """
    sw = math.sin(2 * math.pi * t)
    return 1.2 * sw, -2.5 * sw, 0.0


def build_idle():
    act = new_action("Colossus_Idle_Weight")
    lag = 3.0 / IDLE_FRAMES
    for f in range(1, IDLE_FRAMES + 2):
        t = (f - 1) / float(IDLE_FRAMES)
        reset_pose()
        sw = math.sin(2 * math.pi * t)
        # 18 mm of vertical travel at most, and both terms are whole harmonics
        dz = -0.006 * (1 - math.cos(2 * math.pi * t)) - 0.003 * (1 - math.cos(4 * math.pi * t))
        wloc(BODY, -0.018 * sw, -0.020 * sw, dz)
        now = idle_torso(t)
        torso(*now)
        humps(now, idle_torso(t - lag))
        # the head keeps aiming at the floor and never turns: a head that looks
        # around reads as a creature that has already noticed you
        head(now[0], now[1], now[2], counter=0.4, yaw_extra=0.0,
             jaw=3.0 - 1.0 * math.cos(2 * math.pi * t))
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, IDLE_FRAMES + 1
    return act


# ----------------------------------------------------------------- locomotion

def build_locomotion(name, frames, duty, stride, feet, throw, sway_per_cycle,
                     body_sway, body_bob, dips_per_cycle, torso_fn, chest_lag_f,
                     hump_follow, head_counter, head_yaw_mix, jaw_lo, jaw_hi,
                     jaw_per_cycle, wring_amp, wring_per_cycle, split_amp):
    """feet: {limb: (phase, lift, centre, toe_up, heel_up)}

    Every periodic term here is a whole harmonic of `frames`. A term at 1.5x the
    gait sits halfway through its own cycle at the seam and snaps the pose every
    loop - the fault this pipeline already paid for once on the Crawler.
    """
    global W_A, W_B, W_C
    W_A, W_B, W_C = whip_coeffs(duty, throw)
    act = new_action(name)
    lag = chest_lag_f / float(frames)
    for f in range(1, frames + 2):
        t = (f - 1) / float(frames)
        reset_pose()
        wloc(BODY, body_sway * math.sin(2 * math.pi * sway_per_cycle * t + 1.26), 0.0,
             body_bob * slam(t, dips_per_cycle))
        now = torso_fn(t)
        wring = wring_amp * math.sin(2 * math.pi * wring_per_cycle * t + 0.9)
        torso(*now, twist=wring)
        humps(now, torso_fn(t - lag), hump_follow,
              split=split_amp * math.sin(2 * math.pi * wring_per_cycle * t + 2.4))
        bpy.context.view_layer.update()
        for k, (phase, lift, centre, toe_up, heel_up, twist_amp) in feet.items():
            dy, dz = gait(t - phase, duty, stride, lift, centre)
            # the limb rolls over while it is in the air and flattens out again
            # for the plant, so the contact stays honest and the swing does not
            plant(k, dy, dz, contact_tilt(t - phase, duty, toe_up, heel_up),
                  roll=twist_amp * (dz / lift))
        jaw_mid = 0.5 * (jaw_lo + jaw_hi)
        head(now[0], now[1], now[2], counter=head_counter,
             yaw_extra=head_yaw_mix * now[2],
             jaw=jaw_mid - 0.5 * (jaw_hi - jaw_lo)
             * math.cos(2 * math.pi * jaw_per_cycle * t),
             roll_extra=-0.8 * wring)
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, frames + 1
    return act


def contact_tilt(u, duty, toe_up, heel_up):
    """Sole angle through the cycle: flat as it lands, rolled up onto the front
    edge as it pushes off. Continuous across both joins, so the loop is safe.

    Only positive tilt is useful on this creature. Both contacts sit at the front
    of their patch - the toe joint and the knuckle - with sole behind them, so
    tipping the leading edge up drives the heel or the wrist straight into the
    floor instead; -6 degrees put the heel 80 mm under."""
    u = u % 1.0
    if u < duty:
        s = u / duty
        return -toe_up + (toe_up + heel_up) * s
    s = (u - duty) / (1.0 - duty)
    return heel_up - (toe_up + heel_up) * (s ** 0.6)


# --------------------------------------------------------------- 2 walk knuckle

WALK_FRAMES, WALK_DUTY, WALK_STRIDE = 54, 0.78, 1.25
# lateral sequence: hind, then the hand on the same side, then the other pair.
# phase, swing height, stroke centre, tilt at touchdown, tilt at push-off.
# The centres are biased backward because the rest pose already holds the hands
# far out in front, leaving much more room behind them than ahead.
WALK_FEET = {"HL": (0.00, 0.22, 0.22, 0.0, 24.0, -26.0),
             "FL": (0.25, 0.28, 0.36, 0.0, 20.0, -62.0),
             "HR": (0.50, 0.22, 0.22, 0.0, 24.0, 22.0),
             "FR": (0.75, 0.28, 0.36, 0.0, 20.0, 70.0)}


def walk_torso(t):
    return (3.0 * math.sin(4 * math.pi * t),          # pitch
            4.0 * math.sin(2 * math.pi * t + 1.26),   # roll, larger than pitch
            3.0 * math.sin(2 * math.pi * t + 0.60))   # yaw


def build_walk():
    return build_locomotion(
        "Colossus_Walk_Knuckle", WALK_FRAMES, WALK_DUTY, WALK_STRIDE, WALK_FEET,
        throw=3.0, sway_per_cycle=1, body_sway=0.030, body_bob=0.045, dips_per_cycle=4,
        torso_fn=walk_torso, chest_lag_f=2.5, hump_follow=0.6,
        head_counter=0.6, head_yaw_mix=-0.5, jaw_lo=2.0, jaw_hi=4.0,
        jaw_per_cycle=2, wring_amp=11.0, wring_per_cycle=1, split_amp=6.0)


# -------------------------------------------------------------------------- run

RUN_FRAMES, RUN_STRIDE = 36, 1.34
# 10-run.md asks for two flight phases of at most two frames. Its own 0.58 cannot
# produce one: two diagonal pairs half a cycle apart still overlap unless duty
# drops below 0.50. 0.45 leaves a 1.8-frame gap twice per cycle, and it is also
# what puts the ground speed on the 2.60 m/s the sheet wants without rescaling.
RUN_DUTY = 0.45
# arms lead - their swing arc is 1.6x the legs', per the sheet
RUN_FEET = {"FL": (0.00, 0.42, 0.52, 0.0, 20.0, -78.0),
            "HR": (0.00, 0.26, 0.34, 0.0, 32.0, 30.0),
            "FR": (0.50, 0.42, 0.52, 0.0, 20.0, 78.0),
            "HL": (0.50, 0.26, 0.34, 0.0, 32.0, -30.0)}


def run_torso(t):
    # Every term runs twice per cycle, matching the two diagonal contacts. Once
    # per cycle would make the two touchdowns different sizes, and since one of
    # them is the loop seam the clip would land harder there than anywhere else.
    return (7.0 * math.sin(4 * math.pi * t),
            6.0 * math.sin(4 * math.pi * t + 1.26),
            3.0 * math.sin(4 * math.pi * t + 0.60))


def build_run():
    return build_locomotion(
        "Colossus_Run", RUN_FRAMES, RUN_DUTY, RUN_STRIDE, RUN_FEET,
        throw=2.6, sway_per_cycle=2, body_sway=0.040, body_bob=0.160, dips_per_cycle=2,
        torso_fn=run_torso, chest_lag_f=2.0, hump_follow=0.5,
        head_counter=0.5, head_yaw_mix=-0.4, jaw_lo=8.0, jaw_hi=14.0,
        jaw_per_cycle=2, wring_amp=15.0, wring_per_cycle=2, split_amp=8.0)


# ------------------------------------------------------------------------ turn

TURN_FRAMES = 48
TURN_SWEEP = 90.0
# 09-turn.md gives each limb one lift and 22.5 degrees of catch-up per lift. That
# does not close: over a 90 degree turn a limb that lifts once has to make the
# whole quarter turn up in that one step, which puts the right hind foot on the
# left side of the body. Measured, the contact missed its target by 0.95 m and
# planted contacts drifted 911 mm.
#
# So each limb lifts four times instead, 22.5 degrees of catch-up per lift. Body
# drift away from neutral peaks near 19 degrees, which the limbs can reach, and
# every limb is back at neutral on the last frame so the clip still blends into
# Idle_Weight. Sixteen three-frame windows, one limb up at a time: three contacts
# always, the rule the sheet actually cares about.
TURN_STEPS = 4
TURN_CYCLE_R = ("HR", "FR", "HL", "FL")   # inner side first when turning right
TURN_LIFT = {"F": 0.18, "H": 0.14}


def mirror_limb(k):
    return k[0] + ("L" if k[1] == "R" else "R")


def turn_order(dirn):
    """(limb, first frame, last frame) for every lift in the clip."""
    seq = TURN_CYCLE_R if dirn > 0 else tuple(mirror_limb(k) for k in TURN_CYCLE_R)
    n = len(seq) * TURN_STEPS
    span = TURN_FRAMES // n
    return tuple((seq[i % len(seq)], i * span + 1, (i + 1) * span)
                 for i in range(n))


def turn_head_lead(t):
    return t / 0.20 if t < 0.20 else max(0.0, 1.0 - (t - 0.20) / 0.80)


def build_turn(name, dirn):
    """90 degrees in place, with the body itself never turning.

    The agent does the turning, at 55 deg/s; all this clip may do is pick the
    contacts up and set them down again underneath. So each contact holds a fixed
    world angle while its body-space angle runs backward at the agent's rate, and
    catches a whole quarter turn up during its one swing. At the last frame every
    limb has caught up exactly, which is why the pose matches frame 1 and blends
    back into Idle_Weight.
    """
    order = turn_order(dirn)
    act = new_action(name)
    rest = {k: contact_rest(k) for k in LIMBS}
    for f in range(1, TURN_FRAMES + 1):
        t = (f - 1) / float(TURN_FRAMES - 1)
        reset_pose()
        agent = TURN_SWEEP * t
        step = TURN_SWEEP / float(TURN_STEPS)
        lean = 0.0
        stepped = 0.0
        phis = {}
        for k in LIMBS:
            caught = sum(smoothstep((f - a) / float(b - a))
                         for j, a, b in order if j == k)
            stepped += caught / float(TURN_STEPS)
            phis[k] = dirn * (step * caught - agent)
        # The hips and shoulders lean into the turn by half of where the contacts
        # have got to. It costs a few degrees of transient over-rotation on top of
        # the agent's - back to zero by the last frame - and it buys the reach
        # that stops the contacts falling short.
        body_yaw = clamp(0.55 * sum(phis.values()) / len(phis), -10.0, 10.0)
        swings = {k: sum(bump((f - a) / float(b - a))
                         for j, a, b in order if j == k and a <= f <= b)
                  for k in LIMBS}
        lean = sum(swings[k] * (1.0 if k.endswith("R") else -1.0) for k in LIMBS)
        wrot(BODY, yaw=body_yaw)
        # weight goes to the side that still has its limb on the floor, and the
        # torso hitches with each transfer instead of sweeping evenly - an even
        # sweep reads as an office chair
        now = (0.0, -5.0 * lean, 2.5 * dirn * (stepped / len(LIMBS) - t))
        wring = -9.0 * dirn * math.sin(2 * math.pi * 2.0 * t)
        torso(*now, twist=wring)
        humps(now, now, 0.6, split=6.0 * math.sin(2 * math.pi * 3.0 * t))
        wloc(BODY, 0.0, 0.0, -0.020 * abs(lean))
        bpy.context.view_layer.update()
        for k in LIMBS:
            d = world_rot(yaw=phis[k]) @ rest[k] - rest[k]
            plant(k, d.y, TURN_LIFT[k[0]] * swings[k], dx=d.x, yaw=phis[k])
        # the head commits to the turn before the body does; it is the only
        # warning the player gets that this thing is coming round
        head(now[0], now[1], now[2], counter=0.5,
             yaw_extra=12.0 * dirn * turn_head_lead(t), jaw=3.0,
             roll_extra=-0.9 * wring)
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, TURN_FRAMES
    return act


def turn_report(act, dirn):
    """A planted contact has to hold still in the world while the agent turns
    under it. Undo the agent's rotation and look for drift."""
    order = turn_order(dirn)
    arm.animation_data.action = act
    n = TURN_FRAMES
    pos = {k: [] for k in LIMBS}
    for f in range(1, n + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        R = world_rot(yaw=dirn * TURN_SWEEP * (f - 1) / float(n - 1))
        for k in LIMBS:
            pos[k].append(R @ (arm.matrix_world @ arm.pose.bones[EFF[k]].tail))

    worst, where = 0.0, ""
    for k in LIMBS:
        swinging = set()
        for j, a, b in order:
            if j == k:
                swinging.update(range(a, b + 1))
        run = []
        for f in list(range(1, n + 1)) + [None]:
            if f is not None and f not in swinging:
                run.append(f)
                continue
            if len(run) > 1:
                base = pos[k][run[0] - 1]
                span = max((pos[k][i - 1] - base).length for i in run)
                if span > worst:
                    worst, where = span, "%s f%d-%d" % (k, run[0], run[-1])
            run = []
    support = min(4 - sum(1 for _, a, b in order if a <= f <= b)
                  for f in range(1, n + 1))
    print("TURN   %-24s min support %d  planted drift %.1f mm (%s)"
          % (act.name, support, worst * 1000, where))


# ------------------------------------------------------------- one-shot poses

# A pose is a dict. Everything left out is rest, so the sparse spec key frames
# read the way the documents write them.
#   body      (dx, dy, dz)   pelvis, world metres
#   body_rot  (pitch, roll, yaw)   pelvis, world degrees
#   motion    (dx, dy, dz)   the staging bone, for clips the agent is off for
#   torso     (pitch, roll, yaw)   spread over Spine01..Chest
#   <limb>    (dx, dy, dz, tilt, yaw)   ground contact, world metres/degrees
#   digits    {limb: degrees}   negative curls the fingers in
#   head      (counter, yaw_extra, pitch_extra, jaw)

HEAD_DEFAULT = (0.5, 0.0, 0.0, 2.0)
UPPER = [n for n, _ in SPINE_SPLIT] + list(HUMPS) + ["DEF_HeadBase", "DEF_HeadTip",
                                                    "DEF_Jaw"]


def apply_pose(p):
    reset_pose()
    wloc(BODY, *p.get("body", (0.0, 0.0, 0.0)))
    wrot(BODY, *p.get("body_rot", (0.0, 0.0, 0.0)))
    wloc(MOTION, *p.get("motion", (0.0, 0.0, 0.0)))
    t = p.get("torso", (0.0, 0.0, 0.0))
    torso(*t, twist=p.get("twist", 0.0))
    humps(t, p.get("hump", t), p.get("hump_follow", 0.6),
          split=p.get("hump_split", 0.0))
    # the reach guard reads the live shoulder and hip, so the body has to be
    # posed before any limb is placed
    bpy.context.view_layer.update()
    for k in LIMBS:
        v = p.get(k, (0.0, 0.0, 0.0, 0.0, 0.0))
        dx, dy, dz, tilt, yaw = v[:5]
        plant(k, dy, dz, tilt, dx=dx, yaw=yaw, roll=(v[5] if len(v) > 5 else 0.0))
    for k, deg in p.get("digits", {}).items():
        wrot(DISTAL[k], pitch=deg)
    counter, yaw_extra, pitch_extra, jaw = p.get("head", HEAD_DEFAULT)
    head(t[0], t[1], t[2], counter=counter, yaw_extra=yaw_extra,
         pitch_extra=pitch_extra, jaw=jaw, roll_extra=p.get("head_roll", 0.0))


def build_oneshot(name, frames, poses, upper_only=False):
    act = new_action(name)
    names = UPPER if upper_only else None
    for f in sorted(poses):
        apply_pose(poses[f])
        key(f, names)
    # plain bezier overshoots between sparse keys and pushes limbs through the
    # floor between them; clamped handles keep every curve inside its own keys
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        fc.update()
    act.frame_start, act.frame_end = 1, frames
    return act


# -------------------------------------------------- 3 grab miss / 12 grab kill

# Palms turned to face each other. At rest the hand lies back-down for knuckle
# walking, so this is a 100 degree twist at a wrist that has no business doing
# it - which is the point. Nothing on this animal moves the way a shoulder or a
# wrist is supposed to.
PALM_IN_R, PALM_IN_L = 100.0, -100.0

# 03 and 12 share frames 1..8 exactly: until the hands close the player cannot
# tell which one is coming, and getting out during those 0.27 s is the only
# defence this enemy allows.
LUNGE_OPENING = {
    1: {},
    8: {"body": (0.0, 0.07, -0.05), "torso": (3.0, -2.0, 4.0), "twist": -6.0,
        "FL": (0.35, 0.10, 0.30, 0.0, 0.0, -35.0),
        "FR": (-0.30, 0.06, 0.22, 0.0, 0.0, 30.0),
        "HL": (-0.10, 0.0, 0.0, 0.0, 0.0), "HR": (0.10, 0.0, 0.0, 0.0, 0.0),
        "head": (0.4, -6.0, 4.0, 8.0), "head_roll": 5.0},
}


def build_grab_miss():
    """The hands go wide, roll over so the palms face each other, then close on
    nothing. Renamed from Attack_Push: since the design change on 2026-08-09 this
    is not a shove that costs health, it is a grab that missed."""
    spread = {  # right arm leads by two frames; a symmetric pair reads as a machine
        14: {"body": (0.0, 0.06, -0.06), "torso": (5.0, -4.0, 7.0), "twist": -11.0,
             "FL": (1.05, 0.05, 1.35, 0.0, -22.0, PALM_IN_L),
             "FR": (-0.95, -0.10, 1.55, 0.0, 20.0, PALM_IN_R),
             "HL": (-0.12, 0.0, 0.0, 0.0, 0.0), "HR": (0.12, 0.0, 0.0, 0.0, 0.0),
             "digits": {"FL": 34.0, "FR": 30.0},   # fingers splayed back
             "head": (0.4, -10.0, 8.0, 10.0), "head_roll": 8.0},
        17: {"body": (0.0, -0.13, -0.02), "torso": (1.0, 3.0, -5.0), "twist": 9.0,
             "FL": (0.55, -0.85, 1.55, 0.0, -12.0, PALM_IN_L),
             "FR": (-0.70, -1.05, 1.60, 0.0, 10.0, PALM_IN_R),
             "HL": (-0.12, 0.0, 0.0, 0.0, 0.0), "HR": (0.12, 0.0, 0.0, 0.0, 0.0),
             "digits": {"FL": 20.0, "FR": 12.0},
             "head": (0.4, 6.0, 4.0, 14.0), "head_roll": -6.0},
    }
    # 20f: the two hands sweep in and clap shut on empty air in front of the face
    clamp_shut = {"body": (0.0, -0.16, 0.02), "torso": (-3.0, 1.0, -8.0), "twist": 13.0,
                  "FL": (1.55, -1.25, 1.50, 0.0, -6.0, PALM_IN_L),
                  "FR": (-1.60, -1.30, 1.52, 0.0, 4.0, PALM_IN_R),
                  "HL": (-0.12, 0.0, 0.0, 0.0, 0.0), "HR": (0.12, 0.0, 0.0, 0.0, 0.0),
                  # closing but not closed: the hand has to read as empty
                  "digits": {"FL": -34.0, "FR": -40.0},
                  "head": (0.4, 8.0, 0.0, 20.0), "head_roll": -9.0}
    poses = dict(LUNGE_OPENING)
    poses.update(spread)
    poses[20] = clamp_shut
    poses[21] = clamp_shut     # held two frames so 30 fps still reads the shape
    poses[24] = {"body": (0.0, -0.15, -0.04), "torso": (-5.0, -2.0, -6.0), "twist": 10.0,
                 "FL": (1.30, -1.00, 1.05, 0.0, -4.0, PALM_IN_L * 0.7),
                 "FR": (-1.35, -0.95, 0.95, 0.0, 4.0, PALM_IN_R * 0.7),
                 "HL": (-0.12, 0.0, 0.0, 0.0, 0.0), "HR": (0.12, 0.0, 0.0, 0.0, 0.0),
                 "digits": {"FL": -20.0, "FR": -26.0},
                 "head": (0.4, 4.0, -8.0, 12.0), "head_roll": -5.0}
    poses[28] = {"body": (0.0, -0.03, -0.06), "torso": (4.0, 2.0, 2.0), "twist": -4.0,
                 "FL": (0.10, -0.12, 0.0, 8.0, 0.0, -14.0),
                 "FR": (-0.08, -0.14, 0.0, 8.0, 0.0, 12.0),
                 "head": (0.4, 0.0, 4.0, 5.0), "head_roll": 3.0}
    poses[34] = {}
    return build_oneshot("Colossus_Grab_Miss", 34, poses)


def build_grab_execute():
    # Same wide, palms-in opening, but this time the hands meet round something.
    # The fingers interlace at 20f and stay laced until the jaw is done.
    laced = {"FL": 70.0, "FR": 70.0}
    poses = dict(LUNGE_OPENING)
    poses[14] = {"body": (0.0, 0.06, -0.06), "torso": (5.0, -4.0, 7.0), "twist": -11.0,
                 "FL": (1.05, 0.05, 1.35, 0.0, -22.0, PALM_IN_L),
                 "FR": (-0.95, -0.10, 1.55, 0.0, 20.0, PALM_IN_R),
                 "HL": (-0.12, 0.0, 0.0, 0.0, 0.0), "HR": (0.12, 0.0, 0.0, 0.0, 0.0),
                 "digits": {"FL": 34.0, "FR": 30.0},
                 "head": (0.4, -10.0, 8.0, 10.0), "head_roll": 8.0}
    # 20f: the hands overlap past each other and the fingers lace shut
    poses[20] = {"body": (0.0, -0.12, -0.02), "torso": (-2.0, 0.0, -6.0), "twist": 10.0,
                 "FL": (1.72, -1.20, 1.45, 0.0, -8.0, PALM_IN_L),
                 "FR": (-1.78, -1.24, 1.42, 0.0, 6.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, 6.0, 0.0, 16.0), "head_roll": -8.0}
    # 40f: stands the front of itself up and takes the player off the floor
    poses[40] = {"body": (0.0, 0.18, -0.06), "body_rot": (-18.0, 0.0, 6.0),
                 "torso": (-7.0, 3.0, -8.0), "twist": 12.0,
                 "FL": (1.66, -0.60, 2.14, 0.0, -8.0, PALM_IN_L),
                 "FR": (-1.72, -0.62, 2.10, 0.0, 6.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, 8.0, 0.0, 18.0), "head_roll": -10.0}
    # held, so the snap that follows has something to snap away from
    poses[48] = {"body": (0.0, 0.18, -0.06), "body_rot": (-19.0, 0.0, 7.0),
                 "torso": (-7.0, 3.0, -9.0), "twist": 13.0,
                 "FL": (1.66, -0.58, 2.16, 0.0, -8.0, PALM_IN_L),
                 "FR": (-1.72, -0.60, 2.12, 0.0, 6.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, 8.0, 0.0, 20.0), "head_roll": -10.0}
    # 48 -> 56 is 8 frames, 0.27 s: the laced hands whip the player into the
    # mouth in one movement instead of drawing him in
    poses[56] = {"body": (0.0, 0.20, -0.06), "body_rot": (-22.0, 0.0, -4.0),
                 "torso": (-9.0, -3.0, 6.0), "twist": -9.0,
                 "FL": (1.44, -0.30, 1.92, 0.0, 14.0, PALM_IN_L),
                 "FR": (-1.48, -0.30, 1.90, 0.0, -12.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, -4.0, 8.0, 26.0), "head_roll": 7.0}
    poses[64] = {"body": (0.0, 0.20, -0.06), "body_rot": (-23.0, 0.0, -3.0),
                 "torso": (-9.0, -2.0, 5.0), "twist": -8.0,
                 "FL": (1.46, -0.26, 1.88, 0.0, 14.0, PALM_IN_L),
                 "FR": (-1.50, -0.26, 1.86, 0.0, -12.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, -3.0, 8.0, 34.0), "head_roll": 6.0}
    poses[76] = {"body": (0.0, 0.20, -0.06), "body_rot": (-23.0, 0.0, -3.0),
                 "torso": (-9.0, -2.0, 5.0), "twist": -8.0,
                 "FL": (1.46, -0.24, 1.86, 0.0, 14.0, PALM_IN_L),
                 "FR": (-1.50, -0.24, 1.84, 0.0, -12.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, -3.0, 8.0, 55.0), "head_roll": 6.0}
    poses[84] = {"body": (0.0, 0.20, -0.06), "body_rot": (-23.0, 0.0, -2.0),
                 "torso": (-9.0, -1.0, 4.0), "twist": -6.0,
                 "FL": (1.42, -0.18, 1.85, 0.0, 12.0, PALM_IN_L),
                 "FR": (-1.46, -0.18, 1.83, 0.0, -10.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, -2.0, 8.0, 36.0), "head_roll": 5.0}
    poses[90] = {"body": (0.0, 0.20, -0.06), "body_rot": (-23.0, 0.0, 0.0),
                 "torso": (-9.0, 0.0, 0.0), "twist": 0.0,
                 "FL": (1.40, -0.16, 1.84, 0.0, 10.0, PALM_IN_L),
                 "FR": (-1.44, -0.16, 1.82, 0.0, -8.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.4, 0.0, 8.0, 4.0), "head_roll": 0.0}
    # the screen is already black by now; this is for anyone watching from across
    # the platform
    poses[98] = {"body": (0.0, 0.18, -0.06), "body_rot": (-20.0, 0.0, 20.0),
                 "torso": (-8.0, 5.0, 16.0), "twist": -14.0,
                 "FL": (1.34, -0.14, 1.78, 0.0, 20.0, PALM_IN_L),
                 "FR": (-1.38, -0.14, 1.76, 0.0, -18.0, PALM_IN_R),
                 "digits": laced,
                 "head": (0.3, 10.0, 6.0, 10.0), "head_roll": 12.0}
    poses[104] = {"body": (0.0, 0.18, -0.06), "body_rot": (-20.0, 0.0, -20.0),
                  "torso": (-8.0, -5.0, -16.0), "twist": 14.0,
                  "FL": (1.32, -0.14, 1.76, 0.0, -18.0, PALM_IN_L),
                  "FR": (-1.36, -0.14, 1.74, 0.0, 20.0, PALM_IN_R),
                  "digits": laced,
                  "head": (0.3, -10.0, 6.0, 10.0), "head_roll": -12.0}
    poses[120] = {"body": (0.0, 0.08, -0.05), "body_rot": (-8.0, 0.0, 4.0),
                  "torso": (-3.0, 2.0, 3.0), "twist": -4.0,
                  "FL": (0.55, -0.75, 0.85, 0.0, 6.0, -40.0),
                  "FR": (-0.60, -0.78, 0.82, 0.0, -6.0, 36.0),
                  "digits": {"FL": 12.0, "FR": 16.0},
                  "head": (0.4, 2.0, 2.0, 14.0), "head_roll": 4.0}
    poses[132] = {}
    return build_oneshot("Colossus_Grab_Execute", 132, poses)


# --------------------------------------------------------------------- 7 roar

def build_roar():
    # A neckless animal cannot throw its head back - HeadBase is capped at 10
    # degrees of pitch or the chest boundary tears. It stands the whole front of
    # itself up instead, and everything above the hips counter-rotates against
    # the head, so the shape reads as something coming apart rather than an
    # animal shouting.
    entry = {"head": (0.5, 0.0, 0.0, 2.0)}
    peak = {"body": (0.0, 0.12, -0.07), "body_rot": (-31.0, 4.0, 0.0),
            "torso": (-14.0, -6.0, 40.0), "twist": -26.0, "hump_split": 9.0,
            "FL": (0.75, 0.70, 1.35, 0.0, -26.0, -95.0),
            "FR": (-0.60, 0.80, 1.55, 0.0, 22.0, 88.0),
            "digits": {"FL": 40.0, "FR": 46.0},
            "head": (0.3, 12.0, 0.0, 55.0), "head_roll": -11.0}
    return build_oneshot("Colossus_Roar", 36, {
        1: entry,
        # 4f holds 1f exactly. A thing this size simply stopping is the whole
        # discovery beat, so nothing at all moves here.
        4: entry,
        # the head snaps round on its own, ahead of any of the body
        8: {"torso": (0.0, 0.0, 4.0), "twist": -9.0,
            "head": (0.5, 30.0, 0.0, 3.0), "head_roll": 9.0},
        # a wrong little jerk before the real move - the shape breaks first
        11: {"body": (0.0, 0.02, -0.02), "torso": (2.0, 5.0, 10.0), "twist": -14.0,
             "hump_split": 5.0,
             "FL": (0.10, 0.10, 0.05, 0.0, 0.0, -30.0),
             "FR": (-0.06, 0.12, 0.10, 0.0, 0.0, 26.0),
             "head": (0.5, 26.0, 4.0, 6.0), "head_roll": 12.0},
        14: {"body": (0.0, 0.09, -0.05), "body_rot": (-3.0, 2.0, 0.0),
             "torso": (-2.0, 3.0, 20.0), "twist": -18.0,
             "FL": (0.22, 0.16, 0.16, 0.0, -8.0, -55.0),
             "FR": (-0.16, 0.18, 0.22, 0.0, 6.0, 48.0),
             "head": (0.5, 22.0, 2.0, 8.0), "head_roll": 6.0},
        18: {"body": (0.0, 0.11, -0.06), "body_rot": (-21.0, 3.0, 0.0),
             "torso": (-9.0, -4.0, 32.0), "twist": -22.0, "hump_split": 7.0,
             "FL": (0.55, 0.50, 0.80, 0.0, -20.0, -80.0),
             "FR": (-0.44, 0.58, 1.05, 0.0, 16.0, 72.0),
             "digits": {"FL": 30.0, "FR": 36.0},
             "head": (0.4, 16.0, 0.0, 26.0), "head_roll": -7.0},
        22: peak,
        # the hold is not a freeze: the humps keep splitting while the jaw stays
        # open, so the silhouette is still changing under the sound
        28: {"body": (0.0, 0.12, -0.07), "body_rot": (-30.0, -4.0, 0.0),
             "torso": (-14.0, 6.0, 38.0), "twist": -24.0, "hump_split": -9.0,
             "FL": (0.80, 0.66, 1.30, 0.0, -24.0, -88.0),
             "FR": (-0.66, 0.76, 1.60, 0.0, 20.0, 95.0),
             "digits": {"FL": 44.0, "FR": 38.0},
             "head": (0.3, 10.0, 0.0, 55.0), "head_roll": 11.0},
        32: {"body": (0.0, 0.05, -0.06), "body_rot": (-8.0, 0.0, 0.0),
             "torso": (-4.0, 2.0, 22.0), "twist": -10.0,
             "FL": (0.10, 0.08, 0.0, 10.0, 0.0, -24.0),
             "FR": (-0.06, 0.06, 0.0, 10.0, 0.0, 20.0),
             "head": (0.5, 12.0, 0.0, 14.0), "head_roll": 4.0},
        36: {},
    })


# ------------------------------------------------------------------ 4 stagger

def build_stagger():
    # Mass being shoved, not pain. Nothing here flinches or cries out, because a
    # creature that reacts to being shot stops reading as one bullets do nothing
    # to. The wrongness is in the joints, not in the volume of the motion.
    return build_oneshot("Colossus_Stagger", 48, {
        1: {"torso": (1.0, 1.0, 0.0), "head": (0.6, 0.0, 0.0, 4.0)},
        5: {"body": (0.0, 0.10, -0.03), "torso": (-5.0, 8.0, -5.0), "twist": 8.0,
            "hump_split": 6.0,
            "HL": (0.0, 0.05, 0.0, 0.0, 0.0), "HR": (0.0, 0.06, 0.0, 0.0, 0.0),
            "FL": (0.06, 0.02, 0.0, 0.0, 0.0, -26.0),
            "FR": (-0.04, 0.03, 0.0, 0.0, 0.0, 18.0),
            "head": (0.7, 0.0, 0.0, 12.0), "head_roll": -7.0},
        12: {"body": (0.0, 0.08, -0.06), "torso": (-3.0, 6.0, -3.0), "twist": 5.0,
             "HL": (0.0, 0.08, 0.0, 0.0, 0.0), "HR": (0.0, 0.09, 0.0, 0.0, 0.0),
             "FL": (0.03, -0.06, 0.0, 0.0, 0.0, -14.0),
             "FR": (-0.02, -0.05, 0.0, 0.0, 0.0, 10.0),
             "head": (0.7, 0.0, 0.0, 8.0), "head_roll": -4.0},
        20: {"body": (0.0, 0.05, -0.07), "torso": (-3.0, 4.0, -2.0),
             "HL": (0.0, 0.08, 0.0, 0.0, 0.0), "HR": (0.0, 0.09, 0.0, 0.0, 0.0),
             "FL": (0.0, -0.06, 0.0, 0.0, 0.0, -8.0),
             "FR": (0.0, -0.05, 0.0, 0.0, 0.0, 6.0),
             "head": (0.7, 0.0, 0.0, 2.0)},
        30: {"body": (0.0, 0.05, -0.05), "torso": (-2.0, 5.0, 0.0),
             "hump_split": -7.0,
             "HL": (0.0, 0.07, 0.0, 0.0, 0.0), "HR": (0.0, 0.08, 0.0, 0.0, 0.0),
             "FL": (0.0, -0.05, 0.0, 0.0, 0.0, -6.0),
             "FR": (0.0, -0.04, 0.0, 0.0, 0.0, 4.0),
             "head": (0.7, 0.0, 0.0, 2.0)},
        40: {"body": (0.0, 0.02, -0.03), "torso": (-1.0, 2.0, 0.0),
             "HL": (0.0, 0.03, 0.0, 0.0, 0.0), "HR": (0.0, 0.04, 0.0, 0.0, 0.0),
             "head": (0.7, 0.0, 0.0, 2.0)},
        48: {},
    })


# ------------------------------------------------- 5 / 13 / 14 the cable clips

# One bundle, not two rails: both hands close on the same rope near the centre
# line. 05-grip-cable.md puts it 2.6 m over the head, which is 4.60 m up, and the
# arm cannot hold that. What the IK drives is the wrist joint, 0.46 m short of
# the hand's contact, and with a 2.53 m upper arm, a 1.33 m forearm and the elbow
# capped at 95 degrees of fold that joint never comes closer than 1.85 m to the
# shoulder. The shoulder is at 3.60 m and climbs to 4.20 m as the body hauls
# itself up, so 4.60 m leaves 1.68 m and the hands come off. 5.25 m is the lowest
# bar these arms can hang from.
GRIP_Z = 5.25
CABLE_X = 0.22       # half the gap between the two fists on one rope


def cable_grip(k, y, z=GRIP_Z - 0.08, roll=None, ahead=0.0):
    """Contact tuple for a hand closed round the bundle at world y."""
    rest = contact_rest(k)
    sign = 1.0 if k.endswith("R") else -1.0
    if roll is None:
        roll = PALM_IN_R if sign > 0 else PALM_IN_L
    return (sign * CABLE_X - rest.x, (y + ahead) - rest.y, z - rest.z, 0.0, 0.0, roll)


def build_grip_cable():
    hang = {"motion": (0.0, 0.0, 0.60), "body_rot": (-12.0, 3.0, 0.0),
            "torso": (-5.0, -4.0, 7.0), "twist": -8.0, "hump_split": 5.0,
            "FL": cable_grip("FL", -1.00), "FR": cable_grip("FR", -0.72),
            "HL": (0.10, 0.40, 0.90, 0.0, 0.0), "HR": (-0.14, 0.30, 0.98, 0.0, 0.0),
            "digits": {"FL": 70.0, "FR": 70.0},
            "head": (0.4, -6.0, 12.0, 8.0), "head_roll": 7.0}
    return build_oneshot("Colossus_Grip_Cable", 42, {
        1: {"head": (0.5, 0.0, 8.0, 4.0)},
        # the right arm goes up four frames before the left; both at once and the
        # weight disappears. The raise keeps the hand well forward - swung up
        # close to the body it passes through the shoulder's dead zone.
        8: {"torso": (0.0, 6.0, -6.0), "twist": 8.0,
            "FR": (-0.15, -0.35, 2.30, 0.0, 0.0, 60.0),
            "head": (0.5, -8.0, 9.0, 6.0), "head_roll": -6.0},
        11: {"torso": (0.0, 6.0, -8.0), "twist": 10.0,
             "FR": (-0.20, -0.60, 3.60, 0.0, 0.0, 85.0),
             "head": (0.5, -10.0, 9.0, 8.0), "head_roll": -8.0},
        14: {"torso": (-1.0, 5.0, -6.0), "twist": 8.0,
             "FR": cable_grip("FR", -0.72),
             "digits": {"FR": 70.0},
             "head": (0.5, -8.0, 10.0, 6.0), "head_roll": -6.0},
        # The hind feet come off almost as soon as the body rises.
        # 05-grip-cable.md has them pushing off the ground until 34f, but at rest
        # the hind leg is already within 54 mm of straight - 1.367 m of a 1.421 m
        # chain - so the body cannot climb more than that with them planted.
        20: {"motion": (0.0, 0.0, 0.15), "torso": (-2.0, -5.0, 6.0), "twist": -7.0,
             "FL": (0.15, -0.35, 2.40, 0.0, 0.0, -60.0),
             "FR": cable_grip("FR", -0.72),
             "HL": (0.0, 0.04, 0.12, 0.0, 0.0), "HR": (0.0, 0.05, 0.14, 0.0, 0.0),
             "digits": {"FR": 70.0},
             "head": (0.4, 8.0, 11.0, 6.0), "head_roll": 6.0},
        23: {"motion": (0.0, 0.0, 0.22), "torso": (-2.0, -4.0, 8.0), "twist": -9.0,
             "FL": (0.20, -0.60, 3.70, 0.0, 0.0, -85.0),
             "FR": cable_grip("FR", -0.72),
             "HL": (0.0, 0.06, 0.19, 0.0, 0.0), "HR": (0.0, 0.07, 0.22, 0.0, 0.0),
             "digits": {"FR": 70.0},
             "head": (0.4, 10.0, 11.0, 8.0), "head_roll": 8.0},
        26: {"motion": (0.0, 0.0, 0.30), "torso": (-3.0, 0.0, 4.0), "twist": -5.0,
             "FL": cable_grip("FL", -1.00), "FR": cable_grip("FR", -0.72),
             "HL": (0.0, 0.10, 0.27, 0.0, 0.0), "HR": (0.0, 0.11, 0.30, 0.0, 0.0),
             "digits": {"FL": 70.0, "FR": 70.0},
             "head": (0.4, 4.0, 11.0, 6.0), "head_roll": 4.0},
        34: {"motion": (0.0, 0.0, 0.50), "body_rot": (-7.0, 2.0, 0.0),
             "torso": (-4.0, -3.0, 6.0), "twist": -7.0,
             "FL": cable_grip("FL", -1.00), "FR": cable_grip("FR", -0.72),
             "HL": (0.06, 0.20, 0.55, 0.0, 0.0), "HR": (-0.08, 0.16, 0.60, 0.0, 0.0),
             "digits": {"FL": 70.0, "FR": 70.0},
             "head": (0.4, -4.0, 12.0, 7.0), "head_roll": 5.0},
        42: hang,
    })


CLIMB_FRAMES = 48
CLIMB_REACH = 0.85     # how far along the rope one pull takes the body


def build_cable_climb():
    """Hand over hand up the rope, in place: the grips travel backward past the
    body at a steady rate, exactly as the feet do on the ground, so the ending
    can move the creature at whatever speed it likes.

    The legs are dead weight here. They hang and twitch out of time with the
    arms, which is most of what makes it read as wrong rather than athletic.
    """
    act = new_action("Colossus_Cable_Climb")
    hands = {"FR": 0.0, "FL": 0.5}
    duty = 0.70
    for f in range(1, CLIMB_FRAMES + 2):
        t = (f - 1) / float(CLIMB_FRAMES)
        p = {}
        for k, phase in hands.items():
            u = (t - phase) % 1.0
            base = -0.72 if k == "FR" else -1.00
            if u < duty:
                y = base + CLIMB_REACH * (u / duty - 0.5)
                z = GRIP_Z - 0.08
                grip = 70.0
            else:
                s = (u - duty) / (1.0 - duty)
                y = base + CLIMB_REACH * (0.5 - smoothstep(s))
                z = GRIP_Z - 0.08 - 0.35 * math.sin(math.pi * s) ** 1.3
                grip = 70.0 - 62.0 * math.sin(math.pi * s) ** 0.7
            p[k] = cable_grip(k, y, z, roll=(PALM_IN_R if k == "FR" else PALM_IN_L))
            p.setdefault("digits", {})[k] = grip
        swing = math.sin(2 * math.pi * t)
        p["motion"] = (0.10 * swing, 0.0, 0.55 + 0.05 * math.cos(4 * math.pi * t))
        p["body_rot"] = (-12.0 + 3.0 * math.cos(4 * math.pi * t), 5.0 * swing, 0.0)
        p["torso"] = (-5.0, -6.0 * swing, 9.0 * swing)
        p["twist"] = -11.0 * swing
        p["hump_split"] = 6.0 * math.sin(4 * math.pi * t)
        # the legs run at half the arms' rate and never in step with them
        p["HL"] = (0.10 + 0.06 * swing, 0.40 + 0.10 * swing, 0.88, 0.0, 0.0)
        p["HR"] = (-0.14 - 0.05 * swing, 0.30 - 0.12 * swing, 0.98, 0.0, 0.0)
        p["head"] = (0.4, -7.0 * swing, 12.0, 6.0 + 4.0 * math.cos(2 * math.pi * t))
        p["head_roll"] = 8.0 * math.sin(4 * math.pi * t)
        apply_pose(p)
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, CLIMB_FRAMES + 1
    return act


FALL_FRAMES = 54


def build_cable_fall():
    """Loses the rope and drops. Root stays at zero; Colossus_Motion carries the
    fall, so whatever is driving the ending decides where it lands.

    The two hands do not fail together. The right tears off first and the whole
    mass swings from the left, which is the part that sells the weight.
    """
    def falling(depth, extra):
        p = {"motion": (0.0, 0.0, 0.60 - depth)}
        for k in LIMBS:
            base = extra.pop(k, None)
            if base is None:
                base = (0.0, 0.0, 0.0, 0.0, 0.0)
            p[k] = tuple(base[:2]) + (base[2] - depth,) + tuple(base[3:])
        p.update(extra)
        return p

    hang_l = cable_grip("FL", -1.00)
    hang_r = cable_grip("FR", -0.72)
    return build_oneshot("Colossus_Cable_Fall", FALL_FRAMES, {
        1: {"motion": (0.0, 0.0, 0.60), "body_rot": (-12.0, 3.0, 0.0),
            "torso": (-5.0, -4.0, 7.0), "twist": -8.0,
            "FL": hang_l, "FR": hang_r,
            "HL": (0.10, 0.40, 0.90, 0.0, 0.0), "HR": (-0.14, 0.30, 0.98, 0.0, 0.0),
            "digits": {"FL": 70.0, "FR": 70.0},
            "head": (0.4, -6.0, 12.0, 8.0), "head_roll": 7.0},
        # the right hand is already going: fingers peeling open on the rope
        6: {"motion": (0.0, 0.0, 0.52), "body_rot": (-14.0, 6.0, 0.0),
            "torso": (-6.0, -7.0, 10.0), "twist": -12.0, "hump_split": 6.0,
            "FL": hang_l, "FR": cable_grip("FR", -0.55, GRIP_Z - 0.10),
            "HL": (0.14, 0.46, 0.82, 0.0, 0.0), "HR": (-0.18, 0.22, 0.90, 0.0, 0.0),
            "digits": {"FL": 70.0, "FR": 34.0},
            "head": (0.4, -10.0, 12.0, 20.0), "head_roll": 11.0},
        # 10f: it comes off, and everything swings from the one arm left
        10: {"motion": (0.10, 0.0, 0.34), "body_rot": (-18.0, 14.0, -8.0),
             "torso": (-7.0, -12.0, 16.0), "twist": -19.0, "hump_split": 9.0,
             "FL": hang_l,
             "FR": (-1.30, 0.55, 1.90, 0.0, 0.0, 40.0),
             "HL": (0.30, 0.60, 0.55, 0.0, 0.0), "HR": (-0.34, 0.10, 0.70, 0.0, 0.0),
             "digits": {"FL": 70.0, "FR": -20.0},
             "head": (0.3, -14.0, 10.0, 34.0), "head_roll": 16.0},
        16: {"motion": (0.16, 0.0, 0.18), "body_rot": (-20.0, 18.0, -12.0),
             "torso": (-6.0, -14.0, 20.0), "twist": -22.0, "hump_split": 7.0,
             "FL": cable_grip("FL", -0.80, GRIP_Z - 0.12),
             "FR": (-1.60, 0.30, 1.30, 0.0, 0.0, 20.0),
             "HL": (0.40, 0.70, 0.30, 0.0, 0.0), "HR": (-0.44, 0.02, 0.45, 0.0, 0.0),
             "digits": {"FL": 44.0, "FR": -34.0},
             "head": (0.3, -12.0, 8.0, 40.0), "head_roll": 14.0},
        # 20f: the last hand goes. Nothing is holding it now.
        20: falling(0.10, {"body_rot": (-14.0, 10.0, -6.0),
                           "torso": (-2.0, -8.0, 12.0), "twist": -14.0,
                           "FL": (0.90, -0.10, 2.60, 0.0, 0.0, -30.0),
                           "FR": (-1.70, 0.20, 1.70, 0.0, 0.0, 10.0),
                           "HL": (0.45, 0.75, 1.00, 0.0, 0.0),
                           "HR": (-0.50, -0.05, 1.15, 0.0, 0.0),
                           "digits": {"FL": -30.0, "FR": -40.0},
                           "head": (0.2, -6.0, 6.0, 46.0), "head_roll": 9.0}),
        # from here the drop accelerates and the limbs trail upward, because
        # nothing is driving them any more
        28: falling(0.95, {"body_rot": (6.0, -6.0, 10.0),
                           "torso": (6.0, 6.0, -14.0), "twist": 16.0,
                           "hump_split": -8.0,
                           "FL": (0.60, 0.55, 3.10, 0.0, 0.0, -60.0),
                           "FR": (-0.75, 0.70, 3.30, 0.0, 0.0, 55.0),
                           "HL": (0.55, 0.95, 1.60, 0.0, 0.0),
                           "HR": (-0.60, 0.80, 1.75, 0.0, 0.0),
                           "digits": {"FL": -50.0, "FR": -55.0},
                           "head": (0.2, 8.0, -6.0, 52.0), "head_roll": -13.0}),
        38: falling(2.70, {"body_rot": (26.0, 8.0, -14.0),
                           "torso": (10.0, -8.0, 16.0), "twist": -18.0,
                           "hump_split": 8.0,
                           "FL": (0.35, 0.85, 3.60, 0.0, 0.0, -80.0),
                           "FR": (-0.50, 1.00, 3.85, 0.0, 0.0, 74.0),
                           "HL": (0.50, 1.10, 2.10, 0.0, 0.0),
                           "HR": (-0.56, 0.95, 2.30, 0.0, 0.0),
                           "digits": {"FL": -64.0, "FR": -68.0},
                           "head": (0.2, -10.0, -10.0, 48.0), "head_roll": 15.0}),
        46: falling(5.10, {"body_rot": (44.0, -10.0, 18.0),
                           "torso": (12.0, 9.0, -18.0), "twist": 20.0,
                           "hump_split": -9.0,
                           "FL": (0.20, 1.05, 4.00, 0.0, 0.0, -95.0),
                           "FR": (-0.35, 1.20, 4.25, 0.0, 0.0, 90.0),
                           "HL": (0.44, 1.25, 2.50, 0.0, 0.0),
                           "HR": (-0.50, 1.10, 2.70, 0.0, 0.0),
                           "digits": {"FL": -70.0, "FR": -70.0},
                           "head": (0.2, 12.0, -10.0, 44.0), "head_roll": -17.0}),
        # ends mid-fall, still turning: the level decides what it lands in
        54: falling(8.40, {"body_rot": (62.0, 12.0, -22.0),
                           "torso": (13.0, -9.0, 20.0), "twist": -21.0,
                           "hump_split": 9.0,
                           "FL": (0.10, 1.20, 4.30, 0.0, 0.0, -100.0),
                           "FR": (-0.25, 1.35, 4.55, 0.0, 0.0, 100.0),
                           "HL": (0.40, 1.35, 2.80, 0.0, 0.0),
                           "HR": (-0.46, 1.20, 3.00, 0.0, 0.0),
                           "digits": {"FL": -70.0, "FR": -70.0},
                           "head": (0.2, -12.0, -10.0, 40.0), "head_roll": 18.0}),
    })


# ------------------------------------------------------------------- 6 buried

def build_buried():
    # Not a death. The legs lock first and the arms hold out longest, because
    # this is an animal that carries itself on its arms.
    def sunk(depth, extra=None):
        p = {"motion": (0.0, 0.0, -depth),
             "FL": (0.0, 0.0, -depth * 0.9, 0.0, 0.0),
             "FR": (0.0, 0.0, -depth * 0.9, 0.0, 0.0),
             "HL": (0.0, 0.0, -depth, 0.0, 0.0),
             "HR": (0.0, 0.0, -depth, 0.0, 0.0)}
        p.update(extra or {})
        return p
    return build_oneshot("Colossus_Buried", 60, {
        1: {"head": (0.5, 0.0, 4.0, 6.0)},
        8: sunk(0.30, {"torso": (6.0, -3.0, 5.0), "twist": -7.0, "hump_split": 9.0,
                       "head": (0.4, 5.0, 2.0, 14.0), "head_roll": -8.0}),
        # 8..18 barely sinks at all: this is the part where it holds the slope up
        18: sunk(0.36, {"torso": (5.0, -4.0, 7.0), "twist": -9.0, "hump_split": 7.0,
                        "FL": (0.34, -0.14, -0.30, 0.0, 0.0, -48.0),
                        "FR": (-0.30, -0.10, -0.28, 0.0, 0.0, 40.0),
                        "digits": {"FL": 26.0, "FR": 30.0},
                        "head": (0.4, 8.0, 4.0, 24.0), "head_roll": -10.0}),
        30: sunk(0.90, {"torso": (2.0, 4.0, -6.0), "twist": 8.0, "hump_split": -6.0,
                        "FL": (0.38, -0.12, -0.52, 0.0, 0.0, -60.0),
                        "FR": (-0.34, -0.08, -0.50, 0.0, 0.0, 52.0),
                        "digits": {"FL": 18.0, "FR": 22.0},
                        "head": (0.4, -7.0, 6.0, 18.0), "head_roll": 9.0}),
        42: sunk(1.40, {"torso": (0.0, -3.0, 5.0), "twist": -6.0,
                        "FL": (0.40, -0.08, -0.92, 0.0, 0.0, -72.0),
                        "FR": (-0.36, -0.06, -0.90, 0.0, 0.0, 64.0),
                        "digits": {"FL": 10.0, "FR": 14.0},
                        "head": (0.4, 6.0, 8.0, 14.0), "head_roll": -7.0}),
        52: sunk(1.88, {"torso": (-2.0, 2.0, -3.0), "twist": 4.0,
                        "FL": (0.34, -0.06, -1.52, 0.0, 0.0, -80.0),
                        "FR": (-0.30, -0.05, -1.50, 0.0, 0.0, 72.0),
                        "head": (0.4, -4.0, 10.0, 9.0), "head_roll": 5.0}),
        # ends mid-movement, not locked: the ending camera rides DEF_HeadTip and
        # a hard stop there reads as death, which this is explicitly not
        60: sunk(1.90, {"torso": (-3.0, 3.0, -4.0), "twist": 5.0, "hump_split": 4.0,
                        "FL": (0.33, -0.05, -1.60, 0.0, 0.0, -84.0),
                        "FR": (-0.29, -0.04, -1.58, 0.0, 0.0, 76.0),
                        "head": (0.4, -5.0, 10.0, 8.0), "head_roll": 6.0}),
    })


# --------------------------------------------- 8 threat / 11 hit react (upper)

def build_threat():
    # Additive on top of locomotion: it never stops walking, so 1f and the last
    # frame have to be exactly rest or the chest drifts a little further over
    # every cycle.
    return build_oneshot("Colossus_Threat", 45, {
        1: {"head": (0.4, 0.0, 0.0, 0.0)},
        10: {"torso": (0.0, -4.0, 9.0), "twist": -12.0, "hump_split": 8.0,
             "head": (0.4, 5.0, 0.0, 8.0), "head_roll": -6.0},
        20: {"torso": (-6.0, -6.0, 12.0), "twist": -17.0, "hump_split": 11.0,
             "head": (0.4, 7.0, 0.0, 26.0), "head_roll": -10.0},
        26: {"torso": (-6.0, 5.0, 11.0), "twist": -15.0, "hump_split": -11.0,
             "head": (0.4, 6.0, 0.0, 26.0), "head_roll": 9.0},
        34: {"torso": (-2.0, 2.0, 5.0), "twist": -6.0, "hump_split": -4.0,
             "head": (0.4, 2.0, 0.0, 10.0), "head_roll": 4.0},
        45: {"head": (0.4, 0.0, 0.0, 0.0)},
    }, upper_only=True)


def build_hit_react():
    # A third of Stagger, and the head does not move at all - counter 1.0 keeps
    # it aimed at whoever fired. Knowing you hit it without it looking hurt is the
    # whole job, so the only thing that moves is the skin: the chest presses in
    # and the two humps twitch out of phase with each other.
    return build_oneshot("Colossus_Hit_React", 18, {
        1: {"head": (1.0, 0.0, 0.0, 0.0)},
        3: {"torso": (0.0, 2.5, 0.0), "hump_split": 5.0,
            "head": (1.0, 0.0, 0.0, 6.0)},
        6: {"torso": (0.0, 1.2, 0.0), "hump_split": -4.0,
            "head": (1.0, 0.0, 0.0, 5.0)},
        11: {"torso": (0.0, 0.7, 0.0), "hump_split": 2.0,
             "head": (1.0, 0.0, 0.0, 3.0)},
        18: {"head": (1.0, 0.0, 0.0, 0.0)},
    }, upper_only=True)


# -------------------------------------------------------------------- checks

def strip_to_upper(act):
    """Baking writes every bone, but an additive layer must not carry the pelvis
    or the limbs - a stray key there breaks the contacts of whatever is playing
    underneath. Blender 5 has no Bone.select to bake selectively, so the curves
    come out afterwards."""
    wanted = set(UPPER)
    dropped = 0
    if hasattr(act, "fcurves"):
        for fc in list(act.fcurves):
            if not any('"%s"' % b in fc.data_path for b in wanted):
                act.fcurves.remove(fc)
                dropped += 1
        return dropped
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                for fc in list(cb.fcurves):
                    if not any('"%s"' % b in fc.data_path for b in wanted):
                        cb.fcurves.remove(fc)
                        dropped += 1
    return dropped


def additive_rest_check(act):
    """Additive layers add the difference from frame 1, so frame 1 and the last
    frame both have to be rest exactly. Any drift here tilts the chest a little
    further on every cycle of whatever is playing underneath."""
    arm.animation_data.action = act
    reset_pose()
    worst_t = worst_r = 0.0
    who = ""
    for f in (int(act.frame_start), int(act.frame_end)):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        for b in arm.data.bones:
            if not b.use_deform:
                continue
            pb = arm.pose.bones[b.name]
            dt = (pb.matrix_basis.to_translation()).length
            dr = math.degrees(pb.matrix_basis.to_quaternion().angle)
            if dt > worst_t or dr > worst_r:
                who = "%s@%d" % (b.name, f)
            worst_t, worst_r = max(worst_t, dt), max(worst_r, dr)
    touched = sorted({n for n in (LIMBS + [BODY])
                      if any(fc.data_path.find("\"%s\"" % (CTRL.get(n, n))) >= 0
                             for fc in action_fcurves(act))})
    print("ADD    %-24s rest gap %.3f mm %.3f deg (%s)  lower-body curves %s"
          % (act.name, worst_t * 1000, worst_r, who, touched or "none"))


def bone_height(act, bone, frames):
    arm.animation_data.action = act
    out = []
    for f in frames:
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        out.append((f, (arm.matrix_world @ arm.pose.bones[bone].head).z))
    return out


def motion_drop(act):
    arm.animation_data.action = act
    worst = 0.0
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        bpy.context.view_layer.update()
        worst = min(worst, (arm.matrix_world @ arm.pose.bones[MOTION].head).z)
    return worst


# ----------------------------------------------------------------------- run

LOOPS = ("Colossus_Idle_Weight", "Colossus_Walk_Knuckle", "Colossus_Run",
         "Colossus_Cable_Climb")
ADDITIVE = ("Colossus_Threat", "Colossus_Hit_React")
# Buried sinks 1.9 m on purpose, so the floor test does not apply to it
# these four are off the ground on purpose
NO_FLOOR = ADDITIVE + ("Colossus_Buried", "Colossus_Grip_Cable",
                       "Colossus_Cable_Climb", "Colossus_Cable_Fall")


def main():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    _reach_pull[0] = 0.0
    actions = [build_idle(), build_walk(), build_run(),
               build_turn("Colossus_Turn_R", 1.0), build_turn("Colossus_Turn_L", -1.0),
               build_grab_miss(), build_grab_execute(), build_roar(),
               build_stagger(), build_grip_cable(), build_cable_climb(),
               build_cable_fall(), build_buried(),
               build_threat(), build_hit_react()]
    by_name = {a.name: a for a in actions}

    for act in actions:
        set_ik(1.0)
        print("IKERR0 %-24s %.4f m (%s)" % ((act.name,) + ik_error(act)))
        if act.name not in NO_FLOOR:
            print("DIP0   %-24s %7.4f  %s" % ((act.name,) + worst_dip(act)))
    for act in actions:
        set_ik(1.0)
        print("GUARD  %-24s pulled %.4f m (%s)" % ((act.name,) + guard_action(act)))
    for act in actions:
        if act.name in NO_FLOOR:
            continue
        set_ik(1.0)
        print("SETTLE %-24s worst dip %7.4f" % (act.name, settle_action(act)))
    for act in actions:
        set_ik(1.0)
        guard_action(act)
    for act in actions:
        set_ik(1.0)
        print("IKERR  %-24s %.4f m (%s)" % ((act.name,) + ik_error(act)))

    set_ik(1.0)
    stance_report(by_name["Colossus_Walk_Knuckle"], WALK_FEET, WALK_DUTY,
                  WALK_FRAMES, WALK_STRIDE)
    set_ik(1.0)
    stance_report(by_name["Colossus_Run"], RUN_FEET, RUN_DUTY, RUN_FRAMES, RUN_STRIDE)
    for name, dirn in (("Colossus_Turn_R", 1.0), ("Colossus_Turn_L", -1.0)):
        set_ik(1.0)
        turn_report(by_name[name], dirn)
    for act in actions:
        if act.name in LOOPS:
            set_ik(1.0)
            loop_seam(act)
    for name in ADDITIVE:
        set_ik(1.0)
        additive_rest_check(by_name[name])

    set_ik(1.0)
    print("ROAR   head z " + "  ".join(
        "f%d %.2f" % r for r in bone_height(by_name["Colossus_Roar"],
                                            "DEF_HeadBase", (1, 18, 22, 28, 36))))
    set_ik(1.0)
    print("BURIED motion floor %.2f m (limit 1.90)"
          % -motion_drop(by_name["Colossus_Buried"]))
    set_ik(1.0)
    print("GRIP   hand z " + "  ".join(
        "f%d %.2f" % r for r in bone_height(by_name["Colossus_Grip_Cable"],
                                            "DEF_Digits.R", (1, 14, 26, 42))))

    for tag, duty, frames, stride, want in (
            ("walk", WALK_DUTY, WALK_FRAMES, WALK_STRIDE, 1.15),
            ("run", RUN_DUTY, RUN_FRAMES, RUN_STRIDE, 2.60)):
        stance = duty * frames / 30.0
        print("SPEED  %-5s stance %.2fs  ground speed %.2f m/s  unity rate %.2fx"
              % (tag, stance, stride / stance, want / (stride / stance)))

    for act in actions:
        set_ik(1.0)
        arm.animation_data.action = act
        bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=int(act.frame_start), frame_end=int(act.frame_end),
                         only_selected=True, visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True, bake_types={'POSE'})
        dropped = strip_to_upper(act) if act.name in ADDITIVE else 0
        print("BAKED", act.name, int(act.frame_start), int(act.frame_end),
              ("upper only, %d curves dropped" % dropped) if dropped else "")

    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SCRATCH, "Colossus_Rig.blend"))
    print("ACTIONS", sorted(a.name for a in bpy.data.actions))


main()
