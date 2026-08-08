"""First-person arm clips. The two reloads first - they are the hard ones.

    blender Protagonist_Arms.blend -b --python animate_arms.py

Writes Protagonist_Arms_Anim.blend with the actions on the rig, baked onto the
deform bones, and renders a frame strip of each to OUT_DIR.

06-pistol.md and 07-shotgun.md fix the frame counts and the event frames; this
file fills in the beats between them. Reloads come before the other fifteen
clips because everything hard is in them: the weapon leaves the idle pose and
comes back to it exactly, a part leaves the gun and a new one arrives, and the
left hand goes somewhere the right hand cannot follow. If these two work the
rest is bookkeeping.

Nothing here places a hand by hand. A grip is a pose name out of hand_poses.py
plus whichever weapon bone it is attached to, and the hand follows that bone
wherever the clip takes it - which is the whole reason the pose library stores
a frame rather than a wrist position.
"""
import bpy
import math
import os
import sys
from mathutils import Vector, Matrix, Quaternion

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hand_poses as HP

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_BLEND = os.path.join(HERE, "Protagonist_Arms_Anim.blend")
OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\clips")

arm = bpy.data.objects["Arms_Armature"]
arms_mesh = bpy.data.objects["Arms_Mesh"]

WRIST_LIMIT = 35.0      # 06-pistol.md
NEAR_CLIP = 0.03        # 00-rigging.md

# Idle stances. The pistol sits where the grips were solved; the shotgun where
# 07-shotgun.md puts it, because this character's arms cannot reach the pump
# from anywhere else.
PISTOL_IDLE = dict(loc=Vector((0.10, -0.35, -0.12)),
                   muzzle=Vector((0.0, -0.9925, -0.1219)),   # 7 deg below level
                   roll=0.0)
# found by find_stance.py: of 486 candidates 274 are holdable two-handed, and
# this one shows the most gun with the most wrist to spare - 83% of the shotgun
# and all of the pump inside an 80 degree frame at 19.7 degrees of wrist.
# Scoring on visibility alone first picked a stance sitting at 34.7 of the 35
# limit, which had nowhere to go the moment the clip rolled the gun.
SHOTGUN_IDLE = dict(loc=Vector((0.12, -0.22, -0.18)),
                    muzzle=Vector((-0.50, -1.00, -0.40)),
                    roll=25.0)

POLE = {"R": Vector((0.22, 0.30, -0.70)), "L": Vector((-0.22, 0.30, -0.70))}

# far enough below the camera to be outside a 90 degree frame at arm's length
OFFSCREEN = Vector((-0.16, -0.18, -0.62))

CTRLS = ["CTRL_HandIK.R", "CTRL_HandIK.L", "CTRL_ElbowPole.R", "CTRL_ElbowPole.L"]
PARTS = ["WeaponRoot", "Slide", "Magazine", "Pump", "Shell"]
FINGERS = ["%s%d.%s" % (d, i, s) for s in ("R", "L")
           for d in HP.DIGITS for i in (1, 2, 3)]
KEYED = CTRLS + PARTS + FINGERS


# ------------------------------------------------------------------ posing

def rest_head(name):
    return arm.data.bones[name].head_local


def delta(name):
    """World transform this bone's pose applies to whatever rides on it."""
    pb = arm.pose.bones[name]
    return pb.matrix @ arm.data.bones[name].matrix_local.inverted()


def reset_pose():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = ('QUATERNION' if pb.name in CTRLS or pb.name in PARTS
                            else 'XYZ')
        if pb.rotation_mode == 'QUATERNION':
            pb.rotation_quaternion = (1, 0, 0, 0)
        else:
            pb.rotation_euler = (0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def wloc(name, world_delta):
    m = arm.data.bones[name].matrix_local.to_3x3()
    arm.pose.bones[name].location = m.inverted() @ Vector(world_delta)


def wrot(name, rot3):
    m = arm.data.bones[name].matrix_local.to_3x3()
    pb = arm.pose.bones[name]
    q = (m.inverted() @ rot3 @ m).to_quaternion()
    if pb.rotation_mode == 'QUATERNION':
        pb.rotation_quaternion = q
    else:
        pb.rotation_euler = q.to_euler('XYZ')


def aim_basis(muzzle, up):
    """Rest weapon axes are side +X, muzzle -Y, up +Z."""
    m = Vector(muzzle).normalized()
    u = Vector(up)
    u = (u - u.dot(m) * m).normalized()
    return Matrix((u.cross(m), m, u)).transposed()


REST_BASIS = aim_basis(Vector((0, -1, 0)), Vector((0, 0, 1)))


def up_from_roll(muzzle, roll):
    return Matrix.Rotation(math.radians(roll), 3,
                           Vector(muzzle).normalized()) @ Vector((0, 0, 1))


def set_weapon(loc, muzzle, roll=0.0, up=None):
    up = up if up is not None else up_from_roll(muzzle, roll)
    rot = aim_basis(muzzle, up) @ REST_BASIS.inverted()
    wrot("WeaponRoot", rot)
    head = rest_head("WeaponRoot")
    wloc("WeaponRoot", Vector(loc) - head)
    bpy.context.view_layer.update()


def part_frame(name):
    """The frame a pose bone's `location` actually acts in, measured.

    A pose bone's location is expressed in its own rest axes and applied under
    whatever its parent is doing. Slide, Magazine, Pump and Shell all hang off
    WeaponRoot, so once the gun is rolled 30 degrees, converting a world offset
    through the bone's rest matrix - which is what wloc does - puts the part
    somewhere else entirely. The first run dropped the magazine upwards.

    Rather than compose the chain by hand, push one unit along each axis and see
    where the head goes.
    """
    pb = arm.pose.bones[name]
    save = pb.location.copy()
    pb.location = (0, 0, 0)
    bpy.context.view_layer.update()
    origin = pb.head.copy()
    cols = []
    for i in range(3):
        v = [0.0, 0.0, 0.0]
        v[i] = 1.0
        pb.location = v
        bpy.context.view_layer.update()
        cols.append(pb.head - origin)
    pb.location = save
    bpy.context.view_layer.update()
    return Matrix(cols).transposed(), origin


def part_rot_frame(name):
    """World rotation this bone has with no rotation of its own.

    Same trap as part_frame: a child of a posed WeaponRoot cannot be given a
    world rotation through its rest matrix. Read the frame instead of composing
    it - set the basis to identity and see which way the bone points.
    """
    pb = arm.pose.bones[name]
    save = pb.rotation_quaternion.copy()
    pb.rotation_quaternion = (1, 0, 0, 0)
    bpy.context.view_layer.update()
    m = pb.matrix.to_3x3().normalized()
    pb.rotation_quaternion = save
    bpy.context.view_layer.update()
    return m


def set_part_rot(name, world_rot):
    m = part_rot_frame(name)
    arm.pose.bones[name].rotation_quaternion =         (m.inverted() @ world_rot).to_quaternion()
    bpy.context.view_layer.update()


def align_shell(u):
    """Turn the shell from hand-held to gun-aligned as it travels to the port.

    Shell hangs off WeaponRoot, so in the pouch it inherits the gun's 97 degree
    roll and the left hand gets asked to hold a cartridge lying the way the gun
    lies. That put the wrist at 77.6 degrees. A shell in a pouch is oriented by
    the hand that picks it up and only lines up with the barrel on the way in.
    u 0 is rest orientation, u 1 is whatever the gun is doing.
    """
    gun = part_rot_frame("Shell").to_quaternion()
    rest = arm.data.bones["Shell"].matrix_local.to_3x3().normalized().to_quaternion()
    set_part_rot("Shell", rest.slerp(gun, u).to_matrix())


def place_part(name, world_pos):
    F, origin = part_frame(name)
    arm.pose.bones[name].location = F.inverted() @ (Vector(world_pos) - origin)
    bpy.context.view_layer.update()


def set_part(name, offset):
    """Offset in the weapon's own frame, carried wherever the gun is."""
    D = delta("WeaponRoot")
    place_part(name, D @ (rest_head(name) + Vector(offset)))


def set_fingers(side, pose):
    for d in HP.DIGITS:
        a = HP.angles(pose, d)
        for i in (1, 2, 3):
            pb = arm.pose.bones["%s%d.%s" % (d, i, side)]
            pb.rotation_mode = 'XYZ'
            pb.rotation_euler = (math.radians(a[i - 1]), 0, 0)


def rest_frame(side):
    b = arm.data.bones["Hand." + side]
    fwd = (b.tail_local - b.head_local).normalized()
    palm = Vector((0, 0, -1))
    palm = (palm - palm.dot(fwd) * fwd).normalized()
    return Matrix((fwd, fwd.cross(palm), -palm)).transposed()


WEB_REST = {s: (arm.data.bones["Thumb1." + s].head_local
                + arm.data.bones["Index1." + s].head_local) * 0.5
            for s in ("R", "L")}


def _ortho(pal, fwd):
    f = Vector(fwd).normalized()
    p = Vector(pal)
    return (p - p.dot(f) * f).normalized()


def _apply_hand(side, grip_world, fwd, pal, twist):
    target = Matrix((fwd, fwd.cross(pal), -pal)).transposed()
    if twist:
        target = Matrix.Rotation(math.radians(twist), 3, pal) @ target
    rot = target @ rest_frame(side).inverted()
    ctrl = "CTRL_HandIK." + side
    m = arm.data.bones[ctrl].matrix_local.to_3x3()
    pb = arm.pose.bones[ctrl]
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = (m.inverted() @ rot @ m).to_quaternion()
    wrist = arm.data.bones["Hand." + side].head_local
    want = Vector(grip_world) - rot @ (WEB_REST[side] - wrist)
    pb.location = m.inverted() @ (want - wrist)
    bpy.context.view_layer.update()
    return want


def set_hand(side, grip_world, forward, palm, twist=None, span=180.0):
    """Place the hand, choosing the twist that keeps the wrist straightest.

    The twist is never written down in a clip. Authoring it by eye put the
    wrist 70 degrees over on the pistol reload and 113 over on the shotgun,
    while every number that was being checked looked fine. `twist` here is a
    centre to search around, not a value to use: a grip keeps its character by
    staying near the angle the pose library solved, a free hand can go anywhere.
    """
    fwd = Vector(forward).normalized()
    pal = Vector(palm)
    pal = (pal - pal.dot(fwd) * fwd).normalized()
    centre = 0.0 if twist is None else twist

    def score(t):
        want = _apply_hand(side, grip_world, fwd, pal, t)
        return wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                            - want).length

    lo, hi = centre - span, centre + span
    step = 5.0
    best = min([lo + i * step for i in range(int((hi - lo) / step) + 1)], key=score)
    best = min([best + i for i in range(-4, 5)], key=score)
    _apply_hand(side, grip_world, fwd, pal, best)
    return best




def grip(pose, drift=None, span=25.0):
    """Put the hand on whatever its pose is attached to, wherever that is now.

    This is what the pose library is for: FRAMES stores the grip in the bone's
    own space, so the hand tracks the magazine into the gun without a single
    wrist position being written down anywhere.
    """
    side, bone, g, fwd, palm, twist, off = HP.FRAMES[pose]
    D = delta(bone)
    R = D.to_3x3()
    grip_w = D @ (rest_head(bone) + Vector(g))
    palm_w = R @ Vector(palm)
    if drift:
        grip_w = grip_w + Vector(drift)
    # a grip stays within 25 degrees of the twist the pose library solved, so
    # the fingers keep wrapping the way they were solved to wrap.
    #
    # Travel segments are keyed every frame by the callers below, not at their
    # endpoints. Holding one twist across a segment was tried and is worse: the
    # optimum genuinely moves as the magazine comes up, and pinning it put the
    # pistol wrist at 61 degrees where re-solving gave 29.
    set_hand(side, grip_w - palm_w.normalized() * off, R @ Vector(fwd), palm_w,
             twist, span=span)
    set_fingers(side, pose)


def grip_around(pose, drift=None):
    """Grip a cylinder, choosing where around it the hand sits.

    For a shell or a pump the free parameter is not the twist about the palm
    normal - it is the angle around the cylinder's own axis. Every angle wraps
    the fingers equally well and they put the wrist in very different places.
    Pinning the hand to the one angle the pose library happened to solve left
    the left wrist at 82 degrees at the loading port, because the shell is
    gun-aligned there and the gun is rolled over.
    """
    side, bone, g, fwd, palm, twist, off = HP.FRAMES[pose]
    D = delta(bone)
    R = D.to_3x3()
    axis = (R @ Vector((0, -1, 0))).normalized()      # the cylinder's long axis

    def frame(theta):
        Rt = Matrix.Rotation(math.radians(theta), 3, axis)
        gw = D @ (rest_head(bone) + Rt @ Vector(g))
        if drift:
            gw = gw + Vector(drift)
        pw = Rt @ (R @ Vector(palm))
        fw = Rt @ (R @ Vector(fwd))
        return gw - pw.normalized() * off, fw, pw

    def score(theta):
        t, fw, pw = frame(theta)
        want = _apply_hand(side, t, Vector(fw).normalized(), _ortho(pw, fw), twist)
        return wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                            - want).length

    best = min(range(-180, 181, 10), key=score)
    best = min([best + i * 2 for i in range(-5, 6)], key=score)
    t, fw, pw = frame(best)
    set_hand(side, t, fw, pw, twist, span=20.0)
    set_fingers(side, pose)
    return best


def free_hand(side, pos, fwd, palm, pose):
    set_hand(side, Vector(pos), fwd, palm)
    set_fingers(side, pose)


def poles(r=None, l=None):
    for side, p in (("R", r), ("L", l)):
        wloc("CTRL_ElbowPole." + side,
             (Vector(p) if p else POLE[side]) - rest_head("CTRL_ElbowPole." + side))
    bpy.context.view_layer.update()


# ----------------------------------------------------------------- actions

def action_fcurves(act):
    """Blender 5 moved fcurves into action layers/slots; 4.x kept act.fcurves."""
    if hasattr(act, "fcurves") and len(act.fcurves):
        return list(act.fcurves)
    out = []
    for layer in getattr(act, "layers", []):
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


_last_quat = {}


def key(frame):
    """Key everything, keeping quaternions on one hemisphere.

    q and -q are the same orientation, but the curve between them is not: if
    two consecutive keys land on opposite sides, the hand takes the long way
    round between them. The shotgun reload read 34.7 degrees of wrist at every
    beat and 132 degrees five frames in, which is what that looks like.
    """
    for name in KEYED:
        pb = arm.pose.bones[name]
        if name in CTRLS or name in PARTS:
            pb.keyframe_insert("location", frame=frame)
        if pb.rotation_mode == 'QUATERNION':
            prev = _last_quat.get(name)
            if prev is not None and pb.rotation_quaternion.dot(prev) < 0:
                pb.rotation_quaternion = -pb.rotation_quaternion
            _last_quat[name] = pb.rotation_quaternion.copy()
            pb.keyframe_insert("rotation_quaternion", frame=frame)
        else:
            pb.keyframe_insert("rotation_euler", frame=frame)


# --------------------------------------------------------------- the clips

def pistol_idle():
    set_weapon(**PISTOL_IDLE)
    set_part("Slide", (0, 0, 0))
    set_part("Magazine", (0, 0, 0))
    poles()
    grip("PistolGrip")
    grip("PistolSupport")


def build_pistol_reload():
    """54 frames. 06-pistol.md: 8 release, 16 out, 30 new one approaching,
    39 MagazineIn, 48 slide, 54 ReloadComplete.

    The slide runs back over the first four frames rather than starting back.
    09-weapons.md settled on one reload with the slide locked, but frame 1 has
    to equal PistolIdle frame 1 or the clip pops on entry - and frame 54 has to
    equal it as well. Racking it inside the clip satisfies both ends.
    """
    _last_quat.clear()
    act = new_action("Arms_PistolReload")
    L = PISTOL_IDLE

    def gun(f, lift=0.0, roll=0.0, inboard=0.0, pitch=0.0):
        loc = L["loc"] + Vector((-inboard, 0, lift))
        m = L["muzzle"].copy()
        m.z += pitch
        set_weapon(loc, m, L["roll"] + roll)

    # 1 - idle
    reset_pose()
    pistol_idle()
    key(1)

    # 4 - presented: rolled inboard so the well faces the left hand, lifted 6 cm.
    # The slide finishes running back here.
    gun(4, lift=0.06, roll=-30, inboard=0.05)
    set_part("Slide", (0, 0.008, 0))
    set_part("Magazine", (0, 0, 0))
    grip("PistolGrip")
    grip("PistolSupport")
    key(4)

    # 8 - magazine catch. Left hand comes off the gun.
    free_hand("L", Vector((-0.02, -0.30, -0.20)), Vector((0, -0.7, -0.7)),
              Vector((1, 0, 0)), "Open")
    key(8)

    # 10 - magazine drops free
    set_part("Magazine", (0, 0, -0.03))
    free_hand("L", Vector((-0.05, -0.28, -0.28)), Vector((0, -0.6, -0.8)),
              Vector((1, 0, 0)), "Open")
    key(10)

    # 16 - clear of the gun, still falling
    set_part("Magazine", (0, 0, -0.22))
    free_hand("L", Vector((-0.12, -0.24, -0.44)), Vector((0, -0.5, -0.87)),
              Vector((1, 0, 0)), "Hand_Relax")
    key(16)

    # 20 - out of frame. The magazine is moved to the pouch on the same frame it
    # leaves the view, which is how one magazine plays both the old and the new.
    place_part("Magazine", OFFSCREEN)
    free_hand("L", Vector((-0.16, -0.20, -0.56)), Vector((0, -0.5, -0.87)),
              Vector((1, 0, 0)), "Open")
    key(20)

    # 26..42 - the magazine is in the hand and comes up into the well. Keyed
    # every frame: the hand's roll changes a lot over this stretch, and keying
    # only the beats let the wrist swing to 90 degrees between two keys that
    # both measured 30.
    WAY = [(26, OFFSCREEN), (30, Vector((-0.02, -0.30, -0.30))),
           (36, None), (39, None), (42, None)]
    for f in range(26, 43):
        if f <= 30:
            u = (f - 26) / 4.0
            place_part("Magazine", OFFSCREEN.lerp(
                Vector((-0.02, -0.30, -0.30)), u))
        elif f <= 39:
            u = (f - 30) / 9.0
            seated = delta("WeaponRoot") @ rest_head("Magazine")
            place_part("Magazine",
                       Vector((-0.02, -0.30, -0.30)).lerp(seated, u))
        else:
            set_part("Magazine", (0, 0, 0))
        drift = (0, 0, -0.02 * math.sin(math.pi * (f - 39) / 3.0)) if f > 39 else None
        grip("MagHold", drift=drift)
        key(f)

    # 45 - hand off the magazine, moving to the slide
    free_hand("L", Vector((0.02, -0.36, -0.02)), Vector((0, -0.8, -0.6)),
              Vector((1, 0, 0)), "Point")
    key(45)

    # 48 - slide released and run forward. 06-pistol.md calls this the check.
    set_part("Slide", (0, 0, 0))
    free_hand("L", Vector((0.02, -0.34, -0.01)), Vector((0, -0.8, -0.6)),
              Vector((1, 0, 0)), "Point")
    key(48)

    # 51 - back towards the support grip
    gun(51, lift=0.02, roll=-10, inboard=0.02)
    grip("PistolGrip")
    free_hand("L", Vector((-0.04, -0.33, -0.14)), Vector((0, -0.85, -0.52)),
              Vector((1, 0, 0)), "Hand_Relax")
    key(51)

    # 54 - exactly the idle pose again. ReloadComplete.
    reset_pose()
    pistol_idle()
    key(54)

    act.frame_start, act.frame_end = 1, 54
    return act


def shotgun_idle():
    set_weapon(**SHOTGUN_IDLE)
    set_part("Pump", (0, 0, 0))
    set_part("Shell", (0, 0, 0))
    poles()
    grip("ShotgunGrip")
    grip("PumpHold")


def build_shotgun_reload():
    """78 frames. 07-shotgun.md: 0 roll in, 12 port exposed, shells at
    18/32/46/60, 68 TubesClosed, 78 ReloadComplete. No pump cycle - that lives
    in FirePump, and the code refills the whole tube at 2.6 s either way.
    """
    _last_quat.clear()
    act = new_action("Arms_ShotgunReload")
    S = SHOTGUN_IDLE

    def gun(roll=0.0, lift=0.0, inboard=0.0):
        set_weapon(S["loc"] + Vector((-inboard, 0, lift)), S["muzzle"],
                   S["roll"] + roll)

    # the loading port is under the receiver; rolling the gun inboard turns it
    # towards the left hand instead of the floor
    PORT = Vector((0, -0.020, -0.055))
    POUCH = Vector((-0.20, -0.16, -0.50))

    reset_pose()
    shotgun_idle()
    key(1)

    # 12 - rolled inboard, port exposed, left hand off the pump
    # the stance is already rolled 45 degrees, which is most of what exposing
    # the port needs. Another 20 on top put the right wrist at 50 degrees.
    gun(roll=6, lift=0.02, inboard=0.012)
    set_part("Shell", (0, 0, 0))
    grip("ShotgunGrip")
    free_hand("L", POUCH + Vector((0, 0.04, 0.06)), Vector((0, -0.5, -0.87)),
              Vector((1, 0, 0)), "Open")
    key(12)

    # four shells, 14 frames apart, each one out of the pouch and into the port
    for k, seat in enumerate((18, 32, 46, 60)):
        # out of the pouch and into the port, every frame. Two keys and an
        # interpolation put 90 degrees of wrist in the middle of this.
        port_w = delta("WeaponRoot") @ (rest_head("Shell") + PORT)
        for f in range(seat - 10, seat + 1):
            u = (f - (seat - 10)) / 10.0
            place_part("Shell", POUCH.lerp(port_w, u))
            # only most of the way to gun-aligned while the hand still has it.
            # A shell is thumbed into the port at a slight angle; forcing it
            # parallel to the barrel while the fingers are still wrapped left
            # the left wrist at 47 degrees with nowhere to go.
            align_shell(min(u, 0.7))
            grip_around("ShellHold")
            key(f)

        # gone into the tube; hand drops back for the next one
        align_shell(1.0)
        set_part("Shell", PORT + Vector((0, -0.09, 0.01)))
        free_hand("L", POUCH + Vector((0.04, -0.02, 0.10)), Vector((0, -0.5, -0.87)),
                  Vector((1, 0, 0)), "Hand_Relax")
        key(seat + 4)

    # 68 - tube closed, hand coming back to the pump
    align_shell(1.0)
    place_part("Shell", OFFSCREEN)
    gun(roll=3, lift=0.02, inboard=0.015)
    grip("ShotgunGrip")
    free_hand("L", Vector((-0.12, -0.42, -0.34)), Vector((1, -0.2, 0)),
              Vector((0, 0, 1)), "Hand_Relax")
    key(68)

    # 78 - idle again. ReloadComplete.
    reset_pose()
    shotgun_idle()
    key(78)

    act.frame_start, act.frame_end = 1, 78
    return act


# Frames Unity has to fire on. 06/07 name these; the numbers are theirs.
EVENTS = {
    "Arms_PistolReload": [(8, "MagRelease"), (16, "MagOut"), (39, "MagazineIn"),
                          (48, "SlideRelease"), (54, "ReloadComplete")],
    "Arms_ShotgunReload": [(18, "ShellIn"), (32, "ShellIn"), (46, "ShellIn"),
                           (60, "ShellIn"), (68, "TubesClosed"),
                           (78, "ReloadComplete")],
}


# --------------------------------------------------------------- measuring

def at(act, frame):
    arm.animation_data.action = act
    bpy.context.scene.frame_set(int(frame))
    bpy.context.view_layer.update()


def pose_vector(act, frame):
    at(act, frame)
    out = []
    for b in arm.data.bones:
        if b.use_deform:
            out.append(arm.pose.bones[b.name].matrix.copy())
    return out


def pose_gap(act, a, b):
    pa, pb = pose_vector(act, a), pose_vector(act, b)
    worst, where = 0.0, ""
    for m1, m2, bone in zip(pa, pb, [x for x in arm.data.bones if x.use_deform]):
        d = (m1.to_translation() - m2.to_translation()).length
        if d > worst:
            worst, where = d, bone.name
    return worst, where


def wrist_bend(side):
    def d(n):
        pb = arm.pose.bones[n + "." + side]
        return (pb.tail - pb.head).normalized()
    return math.degrees(d("LowerArm").angle(d("Hand")))


def scan(act):
    """Worst wrist bend and closest approach to the camera over the whole clip."""
    dg = bpy.context.evaluated_depsgraph_get()
    worst_w, worst_f, worst_s, near, near_f = 0.0, 0, "", 1e9, 0
    f = int(act.frame_start)
    while f <= int(act.frame_end):
        at(act, f)
        for side in ("R", "L"):
            w = wrist_bend(side)
            if w > worst_w:
                worst_w, worst_f, worst_s = w, f, side
        ev = arms_mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        me = ev.to_mesh()
        for v in me.vertices:
            if v.co.y < 0 and v.co.length < near:
                near, near_f = v.co.length, f
        ev.to_mesh_clear()
        f += 1
    return worst_w, worst_f, worst_s, near, near_f


def part_track(act, bone, frames):
    out = []
    for f in frames:
        at(act, f)
        out.append((f, arm.pose.bones[bone].head.copy()))
    return out


# ---------------------------------------------------------------- rendering

def setup_render():
    os.makedirs(OUT_DIR, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'
    sc.display.shading.color_type = 'TEXTURE'
    sc.display.shading.show_cavity = True
    sc.render.resolution_x, sc.render.resolution_y = 480, 270
    sc.world = sc.world or bpy.data.worlds.new("W")
    sc.world.color = (0.09, 0.10, 0.12)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.sensor_fit = 'VERTICAL'
    cam.data.angle_y = math.radians(80)
    cam.data.clip_start = NEAR_CLIP
    cam.location = (0, 0, 0)
    cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    return cam


def strip(act, frames, show):
    for ob in bpy.data.objects:
        if ob.type == 'MESH':
            ob.hide_render = ob.name not in show
    for f in frames:
        at(act, f)
        bpy.context.scene.render.filepath = os.path.join(
            OUT_DIR, "%s_%03d.png" % (act.name, f))
        bpy.ops.render.render(write_still=True)
    print("SHOT %s %s" % (act.name, list(frames)))


# --------------------------------------------------------------------- run

def main():
    sc = bpy.context.scene
    sc.render.fps, sc.render.fps_base = 30, 1.0
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    pistol = build_pistol_reload()
    shotgun = build_shotgun_reload()

    setup_render()
    PISTOL_SHOW = {"Arms_Mesh", "W_Pistol", "W_Slide", "W_PistolMag"}
    SHOTGUN_SHOW = {"Arms_Mesh", "W_Shotgun", "W_Pump", "W_TubeFill", "W_Shell"}

    print("=" * 72)
    for act, show, ends in ((pistol, PISTOL_SHOW, (1, 54)),
                            (shotgun, SHOTGUN_SHOW, (1, 78))):
        gap, where = pose_gap(act, *ends)
        print("LOOP  %-20s f%d vs f%d  %.3f mm (%s)"
              % (act.name, ends[0], ends[1], gap * 1000, where))
        w, wf, ws, near, nf = scan(act)
        print("SCAN  %-20s worst wrist %5.1f on %s at f%d (limit %.0f)  "
              "nearest %.4f m at f%d (near clip %.2f)"
              % (act.name, w, ws, wf, WRIST_LIMIT, near, nf, NEAR_CLIP))
        for f, name in EVENTS[act.name]:
            print("EVENT %-20s f%-3d %s" % (act.name, f, name))

    print("MAG   " + "  ".join(
        "f%d z%+.3f" % (f, p.z) for f, p in
        part_track(pistol, "Magazine", (1, 10, 16, 20, 26, 36, 39, 54))))
    print("SLIDE " + "  ".join(
        "f%d y%+.4f" % (f, p.y) for f, p in
        part_track(pistol, "Slide", (1, 4, 20, 45, 48, 54))))
    print("SHELL " + "  ".join(
        "f%d z%+.3f" % (f, p.z) for f, p in
        part_track(shotgun, "Shell", (1, 8, 18, 22, 32, 60, 68, 78))))

    strip(pistol, (1, 4, 10, 16, 26, 36, 39, 45, 48, 54), PISTOL_SHOW)
    strip(shotgun, (1, 12, 18, 25, 32, 46, 60, 68, 78), SHOTGUN_SHOW)

    for act in (pistol, shotgun):
        arm.animation_data.action = act
        bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=int(act.frame_start),
                         frame_end=int(act.frame_end), only_selected=True,
                         visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True,
                         bake_types={'POSE'})
        print("BAKED %-20s %d curves" % (act.name, len(action_fcurves(act))))

    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("=" * 72)


if __name__ == "__main__":
    main()
