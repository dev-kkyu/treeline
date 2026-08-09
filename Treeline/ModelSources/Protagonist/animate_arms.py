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
import build_arms as BA          # for leave_openable only

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_BLEND = os.path.join(HERE, "Protagonist_Arms_Anim.blend")
OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\clips")

arm = bpy.data.objects["Arms_Armature"]
arms_mesh = bpy.data.objects["Arms_Mesh"]

# 06-pistol.md wrote 35 before anyone had measured a grip. A hand actually on a
# pistol is in ulnar deviation plus a little extension at once, and this measure
# is the unsigned total of both - the anatomical ranges it comes out of are 30
# ulnar and 70 extension, so 55 is still well inside one wrist. What the cap is
# for is catching a solver that has quietly folded the hand back on the forearm,
# and it still does that.
WRIST_LIMIT = 55.0
# Forearm pronation, which nothing was watching: a hand can sit at 21 degrees of
# bend and still be turned 102 degrees about the forearm, and that is what wrung
# the sleeve into a sweet wrapper in the render. A forearm pronates about 75
# degrees from neutral, so 70 is an allowance rather than a target - a shooting
# grip is genuinely pronated and spending none of it would be the wrong answer.
ROLL_LIMIT = 70.0
# Player.prefab's ViewModelCamera, not the world camera: the arms are on their
# own layer with their own lens - 50 degrees vertical and a 0.01 near plane. The
# stances were being framed through the world camera's 80 and 0.03, which is why
# everything read small and far away.
FOV_Y = 50.0
NEAR_CLIP = 0.01

# Idle stances. The pistol sits where the grips were solved; the shotgun where
# 07-shotgun.md puts it, because this character's arms cannot reach the pump
# from anywhere else. Negative x is the shooting side - the model faces -y, so
# right = forward x up = -x.
# Both come out of find_stance.py, which now gates on forearm roll as well as
# wrist bend. That is what moved them out to the shooting side: held near the
# centreline the arm could only reach the grip by wringing the forearm 120
# degrees, and no elbow angle bought that back. Out here the pistol holds at 25
# degrees of wrist and the shotgun at 3.
PISTOL_IDLE = dict(loc=Vector((-0.14, -0.24, -0.10)),
                   muzzle=Vector((0.20, -1.00, 0.06)),
                   roll=0.0)
# Levelled, not tipped over: the reference holds a pump gun flat across the
# body, and the roll belongs to the reload beat that exposes the port.
SHOTGUN_IDLE = dict(loc=Vector((-0.22, -0.14, -0.12)),
                    muzzle=Vector((0.60, -1.00, -0.02)),
                    roll=0.0)

POLE = {"R": Vector((-0.22, 0.30, -0.70)), "L": Vector((0.22, 0.30, -0.70))}

# Far enough below the camera to be out of frame, and no further: the pouch is
# where the left hand picks a magazine up, and 0.62 m down was 0.3 m below
# anything the arm could hold comfortably - the wrist read 69 the frame the hand
# arrived. Through the 50 degree viewmodel lens, 0.36 m down at 0.26 m out is
# already well past the bottom edge.
OFFSCREEN = Vector((0.14, -0.26, -0.36))

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
    # Same trap as the elbow, one level up. A free hand searches the whole
    # circle and two twists 180 degrees apart bend the wrist by the same
    # amount, so a resting hand flipped over between two frames of a breathing
    # loop and dragged the elbow 160 mm with it.
    last = _last_twist.get(side)

    def score(t):
        want = _apply_hand(side, grip_world, fwd, pal, t)
        c = wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                         - want).length
        if last is not None:
            c += TWIST_CONTINUITY * abs(t - last)
        return c

    lo, hi = centre - span, centre + span
    step = 5.0
    best = min([lo + i * step for i in range(int((hi - lo) / step) + 1)], key=score)
    best = min([best + i for i in range(-4, 5)], key=score)
    _last_twist[side] = best
    want = _apply_hand(side, grip_world, fwd, pal, best)
    # how far short of the target the arm stopped. The IK target is not a
    # promise - past full stretch the hand simply does not arrive - so anything
    # choosing between hand placements has to be told.
    return best, (arm.pose.bones["Hand." + side].head - want).length




def grip(pose, drift=None, span=25.0, along=None):
    """Put the hand on whatever its pose is attached to, wherever that is now.

    `along` can force the extended fingers down a direction, but the weapon
    grips do not need it any more: the pose library's frames now put the palm on
    the side of the grip with the knuckles forward, so a straight trigger finger
    lands 6 degrees off the bore on its own. Forcing the twist on top of that
    cost 20 degrees of wrist and bought nothing.

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
    target = grip_w - palm_w.normalized() * off
    fw, pw = R @ Vector(fwd), palm_w
    if span == 0.0:
        # Use the library's twist exactly. It was solved on the real weapon with
        # both things right at once - wrist 21 degrees and the trigger finger
        # down the bore - and re-searching it here only ever traded one of those
        # away for the other, because the twist is a single degree of freedom
        # and they pull on opposite ends of it. The stance difference is the
        # elbow's problem, not the hand's.
        _apply_hand(side, target, (R @ Vector(fwd)).normalized(),
                    _ortho(palm_w, R @ Vector(fwd)), twist)
    elif along is None:
        set_hand(side, target, fw, pw, twist, span=span)
    else:
        # Both at once. Scoring the wrist alone rolled the hand back round the
        # grip and left the trigger finger 66 degrees off the bore; scoring the
        # bore alone straightened the finger and cost 20 degrees of wrist. A
        # degree of either is worth about the same to look at.
        aim = Vector(along).normalized()
        fwn, pwn = fw.normalized(), _ortho(pw, fw)

        def score(t):
            want = _apply_hand(side, target, fwn, pwn, t)
            hb = arm.pose.bones["Hand." + side]
            # the wrist is a hard limit and the bore is a preference, so the
            # bore is worth less per degree
            return (wrist_bend(side)
                    + 0.45 * math.degrees(
                        (hb.tail - hb.head).normalized().angle(aim))
                    + 2000.0 * (hb.head - want).length)
        best = min([twist + i for i in range(-40, 41, 5)], key=score)
        best = min([best + i for i in range(-4, 5)], key=score)
        _apply_hand(side, target, fwn, pwn, best)
    set_fingers(side, pose)
    fit_elbow(side)


def barrel():
    """Which way the muzzle points right now, in world space."""
    pb = arm.pose.bones["WeaponRoot"]
    return (pb.tail - pb.head).normalized()


def grip_around(pose, drift=None, prefer=None, theta=None):
    """Grip a cylinder, choosing where around it the hand sits.

    For a shell or a pump the free parameter is not the twist about the palm
    normal - it is the angle around the cylinder's own axis. Every angle wraps
    the fingers equally well and they put the wrist in very different places.
    Pinning the hand to the one angle the pose library happened to solve left
    the left wrist at 82 degrees at the loading port, because the shell is
    gun-aligned there and the gun is rolled over.

    `prefer` is a world direction the hand should sit on relative to the grip,
    and it exists because the wrist alone does not care which side of a cylinder
    it is on while the camera very much does: racking the slide, the cheapest
    angle put the whole left hand between the eye and the pistol and the frame
    was a fist. It is a bias, not a constraint - a blown wrist still outbids it.
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
        s = wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                         - want).length
        if prefer is not None:
            s -= 300.0 * (arm.pose.bones["Hand." + side].head - t).dot(prefer)
        return s

    def settle(theta):
        t, fw, pw = frame(theta)
        _, miss = set_hand(side, t, fw, pw, twist, span=20.0)
        set_fingers(side, pose)
        fit_elbow(side)
        # reach first: a hand that did not get to the pump is worse than any
        # wrist angle, and pose_violation cannot see it - it only knows about
        # the arm, not about what the arm was asked to hold
        return (pose_violation(side) + miss / MISS_NORM, wrist_bend(side))

    # Two passes, because the wrist alone does not see the pronation and this
    # angle sets it: which side of the pump the hand comes round decides how far
    # the forearm has to turn, and the cheapest wrist was leaving it at 94
    # degrees with no elbow angle able to buy it back. The wrist-only sweep is
    # kept as the shortlist - it is 40 times cheaper than fitting an elbow - and
    # the shortlist is then judged on the arm it actually produces.
    # `theta` pins the answer instead of searching it. A hand does not slide
    # round the sleeve while the action is worked, and re-solving every frame
    # moved it 7 mm one frame and 14 the next.
    if theta is None:
        coarse = sorted(range(-180, 181, 10), key=score)[:5]
        theta = min(coarse, key=settle)
        theta = min([theta + i * 3 for i in range(-3, 4)], key=settle)
    settle(theta)
    return theta


def free_hand(side, pos, fwd, palm, pose):
    set_hand(side, Vector(pos), fwd, palm)
    set_fingers(side, pose)
    # the grips fit the elbow and the free hand did not, so the left arm went
    # to the pouch and back with whatever roll the last grip had left in it -
    # 146 degrees of it, three frames after a keyed pose that measured 20
    fit_elbow(side)


# Everything the elbow has to respect, with the size of "too far" for each. They
# are all limits, none of them is a goal, and that is the whole design: an elbow
# angle is judged by how far it breaks a limit as a *fraction* of that limit, so
# nothing trades against anything until something is actually broken. Weighted
# sums were tried three times and each time whichever term had the steepest
# gradient took the whole budget - the roll went to zero and the wrist paid 89
# degrees, then the elbow dropped and the wrist paid 97.
ROLL_LIMIT_NORM = ROLL_LIMIT          # pronation, degrees
ELBOW_DROP = 0.12                     # metres the elbow hangs below the shoulder
DROP_NORM = 0.30
# The camera is inside the head, so an arm segment passing near the origin is an
# arm through the skull. The left upper arm was ending up 3 mm from it during the
# pump cycle - the near plane hides that in the viewmodel, but it is the same
# wrong pose and it shows anywhere else.
HEAD_CLEAR = 0.16
# The elbow must stay behind the hand or the forearm swings round in front of
# the eye and the frame is a sleeve.
AHEAD_NORM = 0.05
# Neighbouring frames must not pick opposite branches. Several elbow angles can
# be equally acceptable and 100 degrees apart, so a frame flips and the two keys
# either side interpolate the elbow straight through the chest.
CONTINUITY = 0.2
# Per degree of twist, and deliberately small: it breaks a tie between two
# equally straight wrists without ever outbidding a real difference.
TWIST_CONTINUITY = 0.05
_last_twist = {}
# How far short of a grip counts as a whole limit broken. The IK stops where the
# arm runs out, so this is the one condition the arm itself cannot report.
MISS_NORM = 0.002
_last_elbow = {}


def _seg_dist(a, b):
    """Distance from the camera at the origin to the segment ab."""
    d = b - a
    n = d.length_squared
    if n < 1e-9:
        return a.length
    t = max(0.0, min(1.0, -a.dot(d) / n))
    return (a + d * t).length


def pose_violation(side):
    """How far this arm breaks its limits, each as a fraction of its own."""
    sh = arm.pose.bones["UpperArm." + side].head
    el = arm.pose.bones["LowerArm." + side].head
    hd = arm.pose.bones["Hand." + side].head
    clear = min(_seg_dist(sh, el), _seg_dist(el, hd))
    return round(max(0.0, wrist_bend(side) - WRIST_LIMIT) / WRIST_LIMIT
                 + max(0.0, forearm_roll(side) - ROLL_LIMIT) / ROLL_LIMIT_NORM
                 + max(0.0, (el.z - sh.z) + ELBOW_DROP) / DROP_NORM
                 + max(0.0, HEAD_CLEAR - clear) / HEAD_CLEAR
                 + max(0.0, hd.y - el.y) / AHEAD_NORM, 4)


def fit_elbow(side, span=170, step=5):
    """Swing the elbow round the shoulder-wrist axis until the arm sits right.

    The elbow was pinned, so every degree the grip asked of the arm had to come
    out of the wrist. It is a free degree of freedom: the hand stays exactly
    where the IK target is no matter where the elbow goes, and the forearm
    rotates under it. The reference photograph has the forearm continuing the
    line of the grip - that is this, not a different hand pose.

    Three things are conditions and one is the goal, and mixing them up is what
    every earlier version of this function did:

      pronation   the forearm turns almost one for one with this swing while the
                  wrist bend moves half as fast, so any weighted sum drives the
                  roll to zero and lets the wrist pay - measured, 89 degrees of
                  wrist to save 43 of roll. A forearm has a pronation budget
                  (ROLL_LIMIT), not a pronation objective.
      elbow down  a shooting stance has both elbows below the shoulder. Nothing
                  in the wrist or the roll says so, and the search was laying
                  the whole upper arm out sideways - the right elbow 16 mm under
                  the shoulder and 315 mm outboard of it.
      elbow back  it must also stay behind the hand, or the forearm swings round
                  in front of the eye and the frame is a sleeve.
      wrist       what is left over is spent here, and only here.

    So: gate, then minimise. Weighting these against each other cannot work -
    they are not the same kind of quantity, and whichever one has the steepest
    gradient wins regardless of which one matters. The weighted sum survives
    only as the fallback for when nothing passes, where it decides which
    condition to break least.
    """
    sh = arm.pose.bones["UpperArm." + side].head.copy()
    wr = arm.pose.bones["Hand." + side].head.copy()
    axis = (wr - sh)
    if axis.length < 1e-5:
        return 0.0
    axis = axis.normalized()
    base = POLE[side]

    def measure(a):
        p = sh + Matrix.Rotation(math.radians(a), 3, axis) @ (base - sh)
        wloc("CTRL_ElbowPole." + side, p - rest_head("CTRL_ElbowPole." + side))
        bpy.context.view_layer.update()
        return (a, pose_violation(side), wrist_bend(side))

    last = _last_elbow.get(side)

    def rank(r):
        c = r[2]
        if last is not None:
            c += CONTINUITY * abs(r[0] - last)
        return (r[1], c)

    rows = [measure(a) for a in range(-span, span + 1, step)]
    best = min(rows, key=rank)[0]
    measure(best)
    _last_elbow[side] = best
    return best


def snapshot():
    """Every value key() would write, so a loop can be closed by replaying it.

    Re-posing the last frame from the same numbers as the first does not close
    a loop: the searches behind it - twist, elbow angle - are biased towards the
    previous frame by design, and after ninety frames of drift they land
    somewhere else. Replaying the pose itself has no such freedom.
    """
    out = {}
    for name in KEYED:
        pb = arm.pose.bones[name]
        out[name] = (pb.location.copy(), pb.rotation_mode,
                     pb.rotation_quaternion.copy(), pb.rotation_euler.copy())
    return out


def replay(snap, frame):
    for name, (loc, mode, q, e) in snap.items():
        pb = arm.pose.bones[name]
        pb.location = loc
        pb.rotation_mode = mode
        pb.rotation_quaternion = q
        pb.rotation_euler = e
    bpy.context.view_layer.update()
    key(frame)


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
    grip("PistolGrip", span=0.0)
    grip("PistolSupport")


def build_pistol_reload():
    """54 frames. 06-pistol.md: 8 release, 16 out, 30 new one approaching,
    39 MagazineIn, 48 slide, 54 ReloadComplete.

    The slide is racked *after* the magazine goes in, not before it. That is the
    order the gun is actually run - new magazine, then chamber a round - and it
    puts the one moment of violence in the clip at the end where it reads as the
    reload finishing. Frame 1 and frame 54 both have the slide forward, which is
    what lets either end match PistolIdle exactly.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_PistolReload")
    L = PISTOL_IDLE
    # 09-weapons.md read 8 mm off the frame's horizontal seam, which is the
    # visible join, not the stroke. A slide has to travel past the magazine to
    # pick a round up, and at 8 mm the rack read as a twitch. 40 mm was tried
    # and is too far for this model: its frame tang ends only 5 mm behind the
    # slide, so the slide hung 35 mm out the back of the gun.
    SLIDE_TRAVEL = 0.025

    def gun(f, lift=0.0, roll=0.0, inboard=0.0, pitch=0.0, push=0.0):
        # inboard is towards the body's midline, and the gun is at negative x
        loc = L["loc"] + Vector((inboard, -push, lift))
        m = L["muzzle"].copy()
        m.z += pitch
        set_weapon(loc, m, L["roll"] + roll)

    # 1 - idle
    reset_pose()
    pistol_idle()
    key(1)

    # 4 - presented: rolled inboard so the well faces the left hand, lifted
    for f in range(2, 5):
        u = (f - 1) / 3.0
        gun(f, lift=0.03 * u, roll=12 * u, inboard=0.03 * u, push=0.06 * u)
        set_part("Slide", (0, 0, 0))
        set_part("Magazine", (0, 0, 0))
        grip("PistolGrip", span=0.0)
        grip("PistolSupport")
        key(f)

    # 8 - magazine catch. Left hand leaves the slide and comes off the gun.
    free_hand("L", Vector((0.02, -0.30, -0.20)), Vector((0, -0.7, -0.7)),
              Vector((-1, 0, 0)), "Open")
    key(8)

    # 10 - magazine drops free
    set_part("Magazine", (0, 0, -0.03))
    free_hand("L", Vector((0.05, -0.28, -0.28)), Vector((0, -0.6, -0.8)),
              Vector((-1, 0, 0)), "Open")
    key(10)

    # 16 - clear of the gun, still falling
    set_part("Magazine", (0, 0, -0.22))
    free_hand("L", Vector((0.10, -0.26, -0.32)), Vector((0, -0.5, -0.87)),
              Vector((-1, 0, 0)), "Hand_Relax")
    key(16)

    # 20 - out of frame. The magazine is moved to the pouch on the same frame it
    # leaves the view, which is how one magazine plays both the old and the new.
    place_part("Magazine", OFFSCREEN)
    free_hand("L", Vector((0.13, -0.26, -0.36)), Vector((0, -0.5, -0.87)),
              Vector((-1, 0, 0)), "Open")
    key(20)

    # 26..42 - the magazine is in the hand and comes up into the well. Keyed
    # every frame: the hand's roll changes a lot over this stretch, and keying
    # only the beats let the wrist swing to 90 degrees between two keys that
    # both measured 30.
    CARRY = Vector((0.02, -0.30, -0.30))
    for f in range(26, 43):
        if f <= 30:
            u = (f - 26) / 4.0
            place_part("Magazine", OFFSCREEN.lerp(CARRY, u))
        elif f <= 39:
            u = (f - 30) / 9.0
            seated = delta("WeaponRoot") @ rest_head("Magazine")
            place_part("Magazine", CARRY.lerp(seated, u))
        else:
            set_part("Magazine", (0, 0, 0))
        drift = (0, 0, -0.02 * math.sin(math.pi * (f - 39) / 3.0)) if f > 39 else None
        # a magazine is held like a stick, so its free angle is round its own
        # long axis too
        grip_around("MagHold", drift=drift)
        key(f)

    # 43..50 - the left hand takes the slide overhand, runs it back 8 mm and
    # lets it go. Keyed every frame: the hand rolls a long way getting onto the
    # slide, and this is the beat the whole clip is read by.
    for f in range(43, 51):
        gun(f, lift=0.03, roll=10, inboard=0.03, push=0.14)
        if f <= 45:
            back = 0.0
        elif f <= 47:
            back = (f - 45) / 2.0            # pulled back
        else:
            back = max(0.0, 1.0 - (f - 47) / 1.5)   # released, runs forward
        set_part("Slide", (0, SLIDE_TRAVEL * back, 0))
        grip("PistolGrip", span=0.0)
        # around the slide, not about the palm: an overhand slide grip can sit
        # anywhere on the circle round it, and the library solved only one of
        # those, with the gun somewhere else. Pinned there the wrist hit 99.
        # Overhand means over the top and away from the eye, which is also the
        # only quarter of that circle the hand does not fill the frame from.
        grip_around("SlidePinch", prefer=Vector((-0.4, -0.5, 0.75)).normalized())
        key(f)

    # 51..53 - hand back towards the support grip, every frame. The gun is
    # coming down out of the rack pose and the left hand is coming off the
    # slide, which is exactly the kind of stretch two keys get wrong.
    for f in range(51, 54):
        v = (53 - f) / 2.0
        gun(f, lift=0.01 * v, roll=4 * v, inboard=0.01 * v, push=0.02 * v)
        set_part("Slide", (0, 0, 0))
        grip("PistolGrip", span=0.0)
        grip("PistolSupport")
        key(f)

    # 54 - exactly the idle pose again. ReloadComplete.
    reset_pose()
    pistol_idle()
    key(54)

    act.frame_start, act.frame_end = 1, 54
    return act


def shotgun_idle(theta=None):
    set_weapon(**SHOTGUN_IDLE)
    set_part("Pump", (0, 0, 0))
    set_part("Shell", (0, 0, 0))
    poles()
    grip("ShotgunGrip", span=0.0)
    return grip_around("PumpHold", theta=theta)


def build_shotgun_reload():
    """78 frames. 07-shotgun.md: 0 roll in, 12 port exposed, shells at
    18/32/46/60, 68 TubesClosed, 78 ReloadComplete.

    A pump cycle closes it out. The spec did not ask for one and the code
    refills the tube either way, but a tube-fed gun that was run dry has an
    empty chamber when the last shell goes in the magazine, and coming back to
    idle without working the action leaves it looking loaded when it is not.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_ShotgunReload")
    S = SHOTGUN_IDLE

    def gun(roll=0.0, lift=0.0, inboard=0.0, push=0.0):
        set_weapon(S["loc"] + Vector((inboard, -push, lift)), S["muzzle"],
                   S["roll"] + roll)

    # The loading port is under the receiver, so at 6 degrees of roll the shell
    # went in below the bottom of the frame and the clip read as a gun sitting
    # still for two seconds. Positive roll about the muzzle brings the underside
    # round towards the camera - the sign is measurable, not a guess: the gun is
    # at negative x and the camera at the origin, so it sees the +x face, and
    # rolling +z towards -x is what turns -z (the port) towards +x.
    ROLL_OUT = 20.0
    # ...and pushing it out to arm's length is what actually put the port on
    # screen. At the idle distance the receiver is 0.16 m from the eye and its
    # underside is 47 degrees below the axis - outside a 25 degree half-frame at
    # any roll at all, so the shells were going in below the bottom edge. 0.16 m
    # further out and 60 mm up brings the whole gun into frame and costs 47
    # degrees of wrist instead of the 113 that rolling alone did.
    PUSH_OUT, LIFT_OUT, IN_OUT = 0.16, 0.06, 0.03
    # The Shell bone rests in the loading port - build_arms.py measured it onto
    # the receiver floor - so the offset from it is zero.
    PORT = Vector((0, 0, 0))
    POUCH = Vector((0.20, -0.16, -0.50))

    reset_pose()
    shotgun_idle()
    key(1)

    # 2..12 - rolling in and the left hand leaving the pump for the pouch,
    # keyed every other frame. Two keys across this put 41 degrees of wrist in
    # the middle of a move that measured 20 at both ends.
    pump_home = arm.pose.bones["Hand.L"].head.copy()
    pouch_hand = POUCH + Vector((0, 0.04, 0.06))
    for f in range(2, 13):
        u = (f - 1) / 11.0
        # the gun does not start moving until the left hand is off it. Ramping
        # both together pulled the pump away from a hand that was still holding
        # it, and the left elbow came up 103 mm above the shoulder chasing it.
        w = max(0.0, (u - 0.35) / 0.65)
        gun(roll=ROLL_OUT * w, lift=LIFT_OUT * w, inboard=IN_OUT * w,
            push=PUSH_OUT * w)
        set_part("Shell", (0, 0, 0))
        grip("ShotgunGrip", span=0.0)
        if u < 0.35:
            grip_around("PumpHold")
        else:
            v = (u - 0.35) / 0.65
            free_hand("L", pump_home.lerp(pouch_hand, v),
                      Vector((0, -0.5, -0.87)), Vector((-1, 0, 0)),
                      "Hand_Relax" if v < 0.6 else "Open")
        key(f)

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

        # gone into the tube; hand drops back for the next one, or onto the
        # pump if that was the last shell
        align_shell(1.0)
        set_part("Shell", PORT + Vector((0, -0.09, 0.01)))
        back = POUCH + Vector((-0.04, -0.02, 0.10))
        if seat == 60:
            # the run home to the pump, keyed every frame - two keys across it
            # left 40 degrees of wrist in the middle. The gun comes back in over
            # the same frames: 0.16 m out is where the port is visible and it is
            # also 0.16 m further than the left arm can reach the pump, so
            # holding the reload position until f68 put that wrist at 86.
            here = arm.pose.bones["Hand.L"].head.copy()
            for f in range(seat + 1, 68):
                v = (f - seat) / 7.0
                v = v * v * (3 - 2 * v)      # ease out of the grip, not off it
                gun(roll=ROLL_OUT * (1 - v), lift=LIFT_OUT * (1 - v),
                    inboard=IN_OUT * (1 - v), push=PUSH_OUT * (1 - v))
                grip("ShotgunGrip", span=0.0)
                if v < 0.95:
                    free_hand("L", here.lerp(pump_home, v),
                              Vector((0, -0.5, -0.87)), Vector((-1, 0, 0)),
                              "Hand_Relax")
                else:
                    # on the pump for the last frame of the run rather than
                    # arriving at f68 out of nowhere. A free hand and a grip pick
                    # different elbows, and the jump between them left one frame
                    # with the left elbow up at shoulder height. Only the last
                    # frame: earlier than that the gun is still out at the reload
                    # position and gripping the pump there raises the elbow for
                    # the same reason.
                    grip_around("PumpHold")
                key(f)
        else:
            free_hand("L", back, Vector((0, -0.5, -0.87)),
                      Vector((-1, 0, 0)), "Hand_Relax")
            key(seat + 4)

    # 68 - tube closed, hand back on the pump. TubesClosed.
    align_shell(1.0)
    place_part("Shell", OFFSCREEN)
    gun()
    grip("ShotgunGrip", span=0.0)
    theta = grip_around("PumpHold")
    key(68)

    # 69..76 - work the action. Back over four frames, forward over four; the
    # forward stroke is the one that has to snap, so it gets fewer.
    for f in range(69, 77):
        u = (f - 68) / 4.0 if f <= 72 else max(0.0, 1.0 - (f - 72) / 4.0)
        gun()
        off = Vector((0, 0.070 * u, 0))
        set_part("Pump", off)
        grip("ShotgunGrip", span=0.0)
        grip_around("PumpHold", theta=theta)
        key(f)

    # 77 - keyed too, so the action closing and the return to idle do not get
    # one interpolated frame between them. That frame read 89 degrees of
    # pronation with 70 at the keys on either side.
    gun()
    grip("ShotgunGrip", span=0.0)
    grip_around("PumpHold", theta=theta)
    key(77)

    # 78 - idle again. ReloadComplete.
    reset_pose()
    shotgun_idle()
    key(78)

    act.frame_start, act.frame_end = 1, 78
    return act


def build_shotgun_firepump():
    """30 frames. 07-shotgun.md: 1 Fire, 3 recoil, 10 pump unlock, 15 back,
    16 Eject, 23 forward and loaded, 30 Idle.

    The left hand rides the pump by being given the same offset the pump gets.
    PumpHold's frame hangs off WeaponRoot rather than off Pump, so the hand does
    not follow it for free the way the magazine hand does - passing the travel
    as a drift is the cheap way and keeps the pose library untouched.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_ShotgunFirePump")
    S = SHOTGUN_IDLE
    TRAVEL = 0.070          # 09-weapons.md

    def gun(roll=0.0, lift=0.0, pitch=0.0, inboard=0.0, push=0.0):
        m = S["muzzle"].copy()
        m.z += pitch
        set_weapon(S["loc"] + Vector((inboard, -push, lift)), m, S["roll"] + roll)

    def hands(pump_u):
        off = Vector((0, TRAVEL * pump_u, 0))
        set_part("Pump", off)
        grip("ShotgunGrip", span=0.0)
        # the hand does not travel round the sleeve while the action is worked
        # PumpHold hangs off the Pump bone, so the hand rides the sleeve without
        # the travel being passed in. Around the barrel axis, not about the
        # palm: the pump is a cylinder and that is where its free angle lives.
        grip_around("PumpHold", theta=theta[0])

    reset_pose()
    theta = [shotgun_idle()]
    key(1)

    # 07-shotgun.md gives the camera 3.4 deg of recoil and asks the arms for 45%
    # of it, since PlayerLook is already kicking the view. That is the beat at
    # frame 3 in the table below.

    # Every frame from here, not just the beats. The pump stroke moves the left
    # hand 70 mm and turns it a long way doing it, and two keys across that left
    # the forearm 114 degrees pronated in the middle of a stroke that measured
    # under 70 at both ends. Interpolating an elbow is not the same as fitting
    # one.
    # Recoil at 1.5x what 07-shotgun.md's "45% of the camera's 3.4 degrees"
    # worked out to. That share was picked before either kick existed; with both
    # of them running the arms were the quiet one.
    BEATS = [(1, 0.0, 0.0, 0.0), (3, 0.165, 0.015, 0.0), (6, 0.075, 0.008, 0.0),
             (10, 0.0, 0.0, 0.0), (15, 0.0, 0.0, 1.0), (18, 0.0, 0.0, 1.0),
             (23, 0.0, 0.0, 0.0), (26, 0.0, 0.0, 0.0), (30, 0.0, 0.0, 0.0)]
    for f in range(2, 30):
        for a, b in zip(BEATS, BEATS[1:]):
            if a[0] <= f <= b[0]:
                t = (f - a[0]) / float(b[0] - a[0])
                pitch, lift, u = (a[i] + (b[i] - a[i]) * t for i in (1, 2, 3))
                break
        # the gun goes out as the pump comes back. A real pump stays put and the
        # hand travels, but the hand travelling 70 mm towards a shoulder that is
        # already at the limit of its reach ran the left elbow up above it - and
        # a shooter does push the gun out against the stroke.
        gun(pitch=pitch, lift=lift, push=0.05 * u)
        hands(u)
        key(f)

    reset_pose()
    shotgun_idle()
    key(30)
    act.frame_start, act.frame_end = 1, 30
    return act


# ------------------------------------------------- the other fourteen clips
#
# The two reloads and the pump cycle carried every hard problem: a part leaving
# the gun, a hand going where the other cannot follow, a wrist and an elbow
# fighting over the same degree of freedom. What is left is bookkeeping on top
# of the same three calls - set_weapon, grip, free_hand - and the same measure
# afterwards.

# Where a bare hand sits with nothing in it. Not authored: through the 50 degree
# viewmodel lens the bottom of the picture at 0.34 m out is 0.16 m down, so this
# is that edge plus a centimetre, which puts the knuckles just inside frame and
# the forearms out of it.
BARE = {"R": Vector((-0.17, -0.34, -0.17)), "L": Vector((0.17, -0.34, -0.17))}
BARE_FWD = Vector((0, -0.86, 0.51))
BARE_PALM = {"R": Vector((1, 0, -0.35)), "L": Vector((-1, 0, -0.35))}

# Carried things ride in front of the chest, low enough to see over. 05-carry.md
# asks for the centre 0.52 m under the eye; that is a two-handed hold at the
# bottom of the frame with the elbows in.
# Carried things ride in front of the chest, low enough to see over, and where
# the hands can be *seen*. 05-carry.md's "centre 0.52 m under the eye" was
# written for the full-body spec: through the 50 degree viewmodel lens at 0.44 m
# out that is 2.8 frame-heights down, and the first attempt rendered an empty
# picture. The bottom edge is 0.21 m down there, so this sits just inside it.
#
# The hands are a hold, not a grip: the palms turn inward as much as up, because
# flat-up is full supination - 88 degrees of it, measured - and a box is carried
# by its sides anyway.
CARRY = {"R": Vector((-0.20, -0.44, -0.17)), "L": Vector((0.20, -0.44, -0.17))}
CARRY_FWD = Vector((0, -1.0, 0.18))
CARRY_PALM = {"R": Vector((0.85, 0, 0.53)), "L": Vector((-0.85, 0, 0.53))}
CARRY_FLOOR = -0.52      # where the hands are when the prop is on the ground

# The lever handle a clip is authored against. Runtime IK moves the hands to the
# real one; this is only so the pose is a pull and not a mime - but it does have
# to stay in frame, or nobody can check it. At 0.4 m out the picture is 0.19 m
# tall, so a 55 degree arc on a 0.26 m arm drops the hands clean out the bottom.
# The arm is 0.13 m and the handle starts near the middle of the frame.
LEVER = Vector((0.0, -0.42, -0.02))
LEVER_ARC = 55.0         # 04-interaction.md
HAND_SHARE = 0.45
# How much of the lever's swing the hand takes. Not all of it: a hand that
# followed the bar rigidly ended the pull pointing backwards and down, which is
# 161 degrees of forearm and a wrist that is not a wrist. The rest of the arc is
# the bar turning inside the fingers, which is what actually happens.


def breathe(f, period, amp=0.006):
    """Vertical drift of a resting hand. Sine, so the loop closes on itself."""
    return Vector((0, 0, amp * math.sin(2 * math.pi * f / period)))


def bare_hands(pose="Hand_Relax", offset=None, period=None, f=0):
    for side in ("R", "L"):
        p = BARE[side].copy()
        if offset:
            p = p + Vector(offset[side] if isinstance(offset, dict) else offset)
        if period:
            p = p + breathe(f, period)
        free_hand(side, p, BARE_FWD, BARE_PALM[side], pose)


IK_CURVE = {}


def ik_weight(v, frame):
    """The curve Unity reads to know when the hand belongs to the prop.

    Baked as an object custom property so it travels with the action and can be
    read off the blend. Whether the FBX carries it is the exporter's problem -
    what matters is that the numbers are not invented twice.
    """
    IK_CURVE[int(frame)] = float(v)
    arm["IKWeight"] = float(v)
    arm.keyframe_insert('["IKWeight"]', frame=frame)


# --------------------------------------------------------------- bare hands

def build_idle():
    """90f loop. Hands at rest, breathing. Chapters 1 and 2 are all of this."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_Idle")
    reset_pose()
    first = None
    for f in range(1, 90, 10):
        bare_hands(period=90.0, f=f - 1)
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 90)
    act.frame_start, act.frame_end = 1, 90
    return act


def build_interact():
    """18f. 04-interaction.md: 8 오른손 접촉, 9 InteractCommit, 18 복귀.

    One hand only. The left stays where it was, because a game that makes both
    hands leave the frame to press a button reads as the player putting
    something down every time they open a door.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_Interact")
    reset_pose()
    reach = Vector((-0.10, -0.44, -0.11))
    for f in range(1, 19):
        u = min(1.0, f / 8.0) if f <= 8 else max(0.0, 1.0 - (f - 8) / 10.0)
        u = u * u * (3 - 2 * u)
        free_hand("L", BARE["L"], BARE_FWD, BARE_PALM["L"], "Hand_Relax")
        free_hand("R", BARE["R"].lerp(reach, u), BARE_FWD.lerp(
            Vector((0, -1, 0.1)), u), BARE_PALM["R"],
            "Point" if u > 0.35 else "Hand_Relax")
        key(f)
    act.frame_start, act.frame_end = 1, 18
    return act


def build_lever_pull():
    """36f. 04-interaction.md: 8 양손 고정, 10~24 -55° 추종, 24 InteractCommit.

    The handle is a bar running left-right, so the finger hinge has to run that
    way too - forward x palm along x. Authoring the hand as though it were
    reaching down at the bar instead put the wrist at 139 degrees: a hand on a
    bar points *across* it, it does not point at it.

    The bar here is a proxy at a fixed place. Runtime IK drags the hands to
    whatever the scene's lever actually is (README's two-IK split), so all this
    clip owes is the shape of a pull - both hands on, arms extending as it comes
    down, elbows following.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_LeverPull")
    reset_pose()
    pivot = LEVER + Vector((0, 0.13, 0.03))     # hinge behind the handle
    # Overhand: hand points down the bar's far side, palm down onto it. The
    # first version had the palm facing up and forward - a grip from underneath,
    # which is how you press a lever up, not how you pull one down.
    FWD, PALM = Vector((0, -1, 0)), Vector((0, 0, -1))

    for f in range(1, 37):
        if f <= 8:
            u, deg = (f / 8.0) ** 2, 0.0
        elif f <= 24:
            u, deg = 1.0, LEVER_ARC * (f - 8) / 16.0
        else:
            u, deg = max(0.0, 1.0 - (f - 24) / 12.0), LEVER_ARC
        R = Matrix.Rotation(math.radians(deg), 3, Vector((1, 0, 0)))
        # the hand's own turn, applied *after* the blend from resting to holding
        # so the two vectors being mixed stay near perpendicular. Mixing them
        # after the swing put nearly opposite directions on either end of the
        # lerp and the hand tumbled through the degenerate middle.
        H = Matrix.Rotation(math.radians(deg * HAND_SHARE), 3, Vector((1, 0, 0)))
        bar = pivot + R @ (LEVER - pivot)
        for side, dx in (("R", -0.055), ("L", 0.055)):
            free_hand(side, BARE[side].lerp(bar + Vector((dx, 0, 0)), u),
                      H @ BARE_FWD.lerp(FWD, u), H @ BARE_PALM[side].lerp(PALM, u),
                      "LeverGrip" if u > 0.5 else "Hand_Relax")
        ik_weight(1.0 if u > 0.5 else 0.0, f)
        key(f)
    act.frame_start, act.frame_end = 1, 36
    return act


# ------------------------------------------------------------------- carry

def carry_hands(u, lift=0.0, pose="CarryFlat"):
    for side in ("R", "L"):
        low = Vector((CARRY[side].x, CARRY[side].y, CARRY_FLOOR))
        p = low.lerp(CARRY[side] + Vector((0, 0, lift)), u)
        free_hand(side, p, BARE_FWD.lerp(CARRY_FWD, u),
                  BARE_PALM[side].lerp(CARRY_PALM[side], u), pose)


def build_carry_lift():
    """24f. 05-carry.md: 6 양손 고정, 8 AttachProp, 18 가슴, 24 Idle."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_CarryLift")
    reset_pose()
    for f in range(1, 25):
        u = min(1.0, max(0.0, (f - 6) / 12.0))
        u = u * u * (3 - 2 * u)
        carry_hands(u)
        ik_weight(1.0 if f >= 6 else 0.0, f)
        key(f)
    act.frame_start, act.frame_end = 1, 24
    return act


def build_carry_idle():
    """60f loop. 05-carry.md: 호흡 5 mm 이하."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_CarryIdle")
    reset_pose()
    first = None
    for f in range(1, 60, 10):
        carry_hands(1.0, lift=0.004 * math.sin(2 * math.pi * (f - 1) / 60.0))
        ik_weight(1.0, f)
        key(f)
        if first is None:
            first = snapshot()
    ik_weight(1.0, 60)
    replay(first, 60)
    act.frame_start, act.frame_end = 1, 60
    return act


def build_carry_drop():
    """18f. 05-carry.md: 12 바닥 직전, 13 DetachProp, 18 손 해제."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_CarryDrop")
    reset_pose()
    for f in range(1, 19):
        u = 1.0 - min(1.0, max(0.0, (f - 1) / 12.0))
        u = u * u * (3 - 2 * u)
        if f <= 13:
            carry_hands(u)
        else:
            v = (f - 13) / 5.0
            for side in ("R", "L"):
                low = Vector((CARRY[side].x, CARRY[side].y, CARRY_FLOOR))
                free_hand(side, low.lerp(BARE[side], v), BARE_FWD,
                          BARE_PALM[side], "Open" if v < 0.6 else "Hand_Relax")
        ik_weight(1.0 if f <= 13 else 0.0, f)
        key(f)
    act.frame_start, act.frame_end = 1, 18
    return act


# ------------------------------------------------------------------ pistol

def build_pistol_idle():
    """60f loop. The stance find_stance.py settled on, breathing."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_PistolIdle")
    reset_pose()
    L = PISTOL_IDLE
    first = None
    for f in range(1, 60, 10):
        s = math.sin(2 * math.pi * (f - 1) / 60.0)
        set_weapon(L["loc"] + Vector((0, 0, 0.005 * s)), L["muzzle"],
                   L["roll"] + 0.6 * s)
        set_part("Slide", (0, 0, 0))
        set_part("Magazine", (0, 0, 0))
        poles()
        grip("PistolGrip", span=0.0)
        grip("PistolSupport")
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 60)
    act.frame_start, act.frame_end = 1, 60
    return act


def equip(name, frames, ready, idle_fn, drop=0.30, roll=35.0):
    """A weapon coming up into frame from below, for both equips.

    The gun starts below the bottom edge and rolled over - a gun is carried
    muzzle-down and levels off as it comes up, and doing both at once is what
    stops the move reading as a lift on a string.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action(name)
    reset_pose()
    for f in range(1, frames + 1):
        u = min(1.0, (f - 1) / float(ready - 1))
        u = u * u * (3 - 2 * u)
        idle_fn(1.0 - u, drop, roll)
        key(f)
    act.frame_start, act.frame_end = 1, frames
    return act


def pistol_stance(back, drop, roll):
    L = PISTOL_IDLE
    set_weapon(L["loc"] + Vector((0, 0.06 * back, -drop * back)), L["muzzle"],
               L["roll"] + roll * back)
    set_part("Slide", (0, 0, 0))
    set_part("Magazine", (0, 0, 0))
    poles()
    grip("PistolGrip", span=0.0)
    if back < 0.5:
        grip("PistolSupport")
    else:
        free_hand("L", BARE["L"], BARE_FWD, BARE_PALM["L"], "Hand_Relax")


def build_pistol_fire():
    """8f. 06-pistol.md: 2 shots/s, camera recoil is PlayerLook's job.

    The slide runs its full stroke inside eight frames - back by 3, forward by
    6 - because that is the one thing on the gun that says it fired. The trigger
    finger is only in on frame 1; the pose library keeps it out everywhere else
    for exactly this reason.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_PistolFire")
    L = PISTOL_IDLE
    reset_pose()
    for f in range(1, 9):
        kick = math.sin(math.pi * min(1.0, (f - 1) / 5.0))
        back = min(1.0, (f - 1) / 2.0) if f <= 3 else max(0.0, 1.0 - (f - 3) / 3.0)
        m = L["muzzle"].copy()
        m.z += 0.16 * kick
        set_weapon(L["loc"] + Vector((0, 0.012 * kick, 0.006 * kick)), m,
                   L["roll"])
        set_part("Slide", (0, 0.025 * back, 0))
        set_part("Magazine", (0, 0, 0))
        poles()
        grip("PistolTrigger" if f == 1 else "PistolGrip", span=0.0)
        grip("PistolSupport")
        key(f)
    act.frame_start, act.frame_end = 1, 8
    return act


# ----------------------------------------------------------------- shotgun

def shotgun_stance(back, drop, roll):
    S = SHOTGUN_IDLE
    set_weapon(S["loc"] + Vector((0, 0.06 * back, -drop * back)), S["muzzle"],
               S["roll"] + roll * back)
    set_part("Pump", (0, 0, 0))
    set_part("Shell", (0, 0, 0))
    poles()
    grip("ShotgunGrip", span=0.0)
    if back < 0.5:
        grip_around("PumpHold")
    else:
        free_hand("L", BARE["L"], BARE_FWD, BARE_PALM["L"], "Hand_Relax")


def build_shotgun_idle():
    """60f loop."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_ShotgunIdle")
    reset_pose()
    S = SHOTGUN_IDLE
    theta = None
    first = None
    for f in range(1, 60, 10):
        s = math.sin(2 * math.pi * (f - 1) / 60.0)
        set_weapon(S["loc"] + Vector((0, 0, 0.005 * s)), S["muzzle"],
                   S["roll"] + 0.6 * s)
        set_part("Pump", (0, 0, 0))
        set_part("Shell", (0, 0, 0))
        poles()
        grip("ShotgunGrip", span=0.0)
        theta = grip_around("PumpHold", theta=theta)
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 60)
    act.frame_start, act.frame_end = 1, 60
    return act


# ------------------------------------------------------------ hit and death

def build_hit():
    """12f additive. 08-damage-death.md: 2 왼쪽 갈비 충격, 6 최대, 12 80% 복원.

    Additive, so frame 1 has to be the pose it will be added to - Unity takes
    the first frame as the reference and layers the difference. Everything here
    is therefore a departure from the bare idle and a return most of the way
    back to it.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_Hit")
    reset_pose()
    for f in range(1, 13):
        if f <= 6:
            u = ((f - 1) / 5.0) ** 0.6
        else:
            u = 0.2 + 0.8 * max(0.0, 1.0 - (f - 6) / 6.0)
            u = min(u, 1.0)
        push = Vector((0.05, 0.03, -0.05)) * u
        bare_hands("Hand_Relax", offset={"R": push, "L": push})
        key(f)
    act.frame_start, act.frame_end = 1, 12
    return act


def build_death():
    """36f. Hands fall out of frame and stay there; input is off by then."""
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_Death")
    reset_pose()
    for f in list(range(1, 37, 3)) + [36]:
        u = min(1.0, (f - 1) / 26.0)
        u = u * u                      # falls, not lowered
        drop = Vector((0.02 * u, 0.10 * u, -0.42 * u))
        bare_hands("Open" if u > 0.3 else "Hand_Relax",
                   offset={"R": drop, "L": drop})
        key(f)
    act.frame_start, act.frame_end = 1, 36
    return act


def build_sway():
    """30f additive loop. One set of arm swing for the whole game.

    README's reason for a single additive clip instead of per-weapon walk
    cycles: four weapon states times four gaits is sixteen clips of the same
    figure-eight, and PlayerLook's sway is already doing most of it.
    """
    _last_quat.clear()
    _last_elbow.clear()
    _last_twist.clear()
    act = new_action("Arms_Sway")
    reset_pose()
    first = None
    for f in range(1, 30, 3):
        t = 2 * math.pi * ((f - 1) % 30) / 30.0
        # figure of eight: the hand goes round twice vertically for once across
        swing = Vector((0.010 * math.sin(t), 0, 0.006 * math.sin(2 * t)))
        bare_hands("Hand_Relax", offset={"R": swing, "L": -swing})
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 30)
    act.frame_start, act.frame_end = 1, 30
    return act


def build_pistol_equip():
    return equip("Arms_PistolEquip", 15, 13, pistol_stance)


def build_shotgun_equip():
    return equip("Arms_ShotgunEquip", 18, 16, shotgun_stance)


# Frames Unity has to fire on. 06/07 name these; the numbers are theirs.
EVENTS = {
    "Arms_PistolReload": [(8, "MagRelease"), (16, "MagOut"), (39, "MagazineIn"),
                          (48, "SlideRelease"), (54, "ReloadComplete")],
    "Arms_ShotgunReload": [(18, "ShellIn"), (32, "ShellIn"), (46, "ShellIn"),
                           (60, "ShellIn"), (68, "TubesClosed"),
                           (72, "PumpBack"), (76, "PumpClosed"),
                           (78, "ReloadComplete")],
    "Arms_ShotgunFirePump": [(1, "Fire"), (16, "Eject"), (23, "PumpClosed")],
    "Arms_Interact": [(9, "InteractCommit")],
    "Arms_LeverPull": [(8, "HandsOn"), (24, "InteractCommit")],
    "Arms_CarryLift": [(8, "AttachProp")],
    "Arms_CarryDrop": [(13, "DetachProp")],
    "Arms_PistolEquip": [(13, "WeaponReady")],
    "Arms_ShotgunEquip": [(16, "WeaponReady")],
    "Arms_PistolFire": [(1, "Fire")],
    "Arms_Death": [(1, "Died")],
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


def forearm_roll(side):
    """How far the hand is turned about the forearm's own axis, in degrees.

    A different thing from wrist_bend, and the one that was going unwatched: the
    wrist can point dead straight down the forearm while the hand is rolled 100
    degrees about it. LowerArmTwist spreads that turn along the sleeve rather
    than creasing it into one loop of vertices, but it can only spread what is
    there - past about 60 degrees the glove folds however it is spread.

    Swing-twist, not the angle between two side vectors: with the wrist bent a
    long way, any two perpendicular axes drift apart from the bend alone and the
    measure reads roll that is not there. The twist part of a quaternion about
    the forearm's own axis has no such cross-talk, and it is also exactly the
    quantity LowerArmTwist's local-Y COPY_ROTATION picks up.
    """
    rest = (arm.data.bones["LowerArm." + side].matrix_local.to_3x3().inverted()
            @ arm.data.bones["Hand." + side].matrix_local.to_3x3())
    now = (arm.pose.bones["LowerArm." + side].matrix.to_3x3().normalized()
           .inverted()
           @ arm.pose.bones["Hand." + side].matrix.to_3x3().normalized())
    q = (rest.inverted() @ now).to_quaternion()
    # a bone's own axis is its local Y
    tw = math.degrees(2.0 * math.atan2(abs(q.y), abs(q.w)))
    return min(tw, 360.0 - tw)


def scan(act):
    """Worst wrist bend, worst forearm roll, and closest approach to the camera."""
    dg = bpy.context.evaluated_depsgraph_get()
    worst_w, worst_f, worst_s, near, near_f = 0.0, 0, "", 1e9, 0
    worst_r, roll_f, roll_s = 0.0, 0, ""
    f = int(act.frame_start)
    while f <= int(act.frame_end):
        at(act, f)
        for side in ("R", "L"):
            w = wrist_bend(side)
            if w > worst_w:
                worst_w, worst_f, worst_s = w, f, side
            r = forearm_roll(side)
            if r > worst_r:
                worst_r, roll_f, roll_s = r, f, side
        ev = arms_mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        me = ev.to_mesh()
        for v in me.vertices:
            if v.co.y < 0 and v.co.length < near:
                near, near_f = v.co.length, f
        ev.to_mesh_clear()
        f += 1
    return worst_w, worst_f, worst_s, near, near_f, worst_r, roll_f, roll_s


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
    sc.render.resolution_x, sc.render.resolution_y = 960, 540
    sc.world = sc.world or bpy.data.worlds.new("W")
    sc.world.color = (0.09, 0.10, 0.12)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.sensor_fit = 'VERTICAL'
    cam.data.angle_y = math.radians(FOV_Y)
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

BARE_SHOW = {"Arms_Mesh"}
PISTOL_SHOW = {"Arms_Mesh", "W_Pistol", "W_Slide", "W_PistolMag"}
SHOTGUN_SHOW = {"Arms_Mesh", "W_Shotgun", "W_Pump", "W_TubeFill", "W_Shell"}

# builder, what to render it with, the two frames a loop has to match, and the
# frames worth looking at. `None` for the loop pair means the clip is one-shot.
JOBS = [
    (build_idle,            BARE_SHOW,    (1, 90), (1, 23, 45, 68, 90)),
    (build_interact,        BARE_SHOW,    None,    (1, 5, 8, 12, 18)),
    (build_lever_pull,      BARE_SHOW,    None,    (1, 8, 16, 24, 30, 36)),
    (build_carry_lift,      BARE_SHOW,    None,    (1, 6, 12, 18, 24)),
    (build_carry_idle,      BARE_SHOW,    (1, 60), (1, 20, 40, 60)),
    (build_carry_drop,      BARE_SHOW,    None,    (1, 6, 13, 18)),
    (build_pistol_equip,    PISTOL_SHOW,  None,    (1, 5, 9, 13, 15)),
    (build_pistol_idle,     PISTOL_SHOW,  (1, 60), (1, 20, 40, 60)),
    (build_pistol_fire,     PISTOL_SHOW,  None,    (1, 2, 3, 5, 8)),
    (build_pistol_reload,   PISTOL_SHOW,  (1, 54), (1, 4, 10, 16, 26, 36, 39, 45, 47, 54)),
    (build_shotgun_equip,   SHOTGUN_SHOW, None,    (1, 6, 11, 16, 18)),
    (build_shotgun_idle,    SHOTGUN_SHOW, (1, 60), (1, 20, 40, 60)),
    (build_shotgun_firepump, SHOTGUN_SHOW, (1, 30), (1, 3, 10, 15, 18, 23, 30)),
    (build_shotgun_reload,  SHOTGUN_SHOW, (1, 78), (1, 12, 18, 25, 32, 46, 60, 68, 78)),
    (build_hit,             BARE_SHOW,    None,    (1, 3, 6, 9, 12)),
    (build_death,           BARE_SHOW,    None,    (1, 9, 18, 27, 36)),
    (build_sway,            BARE_SHOW,    (1, 30), (1, 8, 15, 23, 30)),
]


def main():
    sc = bpy.context.scene
    sc.render.fps, sc.render.fps_base = 30, 1.0
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    arm["IKWeight"] = 0.0

    built = []
    for builder, show, ends, frames in JOBS:
        IK_CURVE.clear()
        act = builder()
        built.append((act, show, ends, frames, dict(IK_CURVE)))
        print("BUILD %-22s %d..%d" % (act.name, act.frame_start, act.frame_end))

    setup_render()
    print("=" * 72)
    for act, show, ends, frames, _ik in built:
        if ends:
            gap, where = pose_gap(act, *ends)
            print("LOOP  %-22s f%d vs f%d  %.3f mm (%s)"
                  % (act.name, ends[0], ends[1], gap * 1000, where))
        w, wf, ws, near, nf, r, rf, rs = scan(act)
        print("SCAN  %-22s worst wrist %5.1f on %s at f%-3d (limit %.0f)  "
              "worst roll %5.1f on %s at f%-3d (limit %.0f)  "
              "nearest %.4f m at f%-3d (near clip %.2f)"
              % (act.name, w, ws, wf, WRIST_LIMIT, r, rs, rf, ROLL_LIMIT,
                 near, nf, NEAR_CLIP))
        for f, name in EVENTS.get(act.name, []):
            print("EVENT %-22s f%-3d %s" % (act.name, f, name))

    by = {a.name: a for a, _s, _e, _f, _i in built}
    print("MAG   " + "  ".join(
        "f%d z%+.3f" % (f, p.z) for f, p in
        part_track(by["Arms_PistolReload"], "Magazine",
                   (1, 10, 16, 20, 26, 36, 39, 54))))
    print("SLIDE " + "  ".join(
        "f%d y%+.4f" % (f, p.y) for f, p in
        part_track(by["Arms_PistolReload"], "Slide", (1, 4, 20, 45, 47, 54))))
    print("SHELL " + "  ".join(
        "f%d z%+.3f" % (f, p.z) for f, p in
        part_track(by["Arms_ShotgunReload"], "Shell",
                   (1, 8, 18, 22, 32, 60, 68, 78))))

    for act, show, _e, frames, _i in built:
        strip(act, frames, show)

    for act, _s, _e, _f, ik in built:
        arm.animation_data.action = act
        bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=int(act.frame_start),
                         frame_end=int(act.frame_end), only_selected=True,
                         visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True,
                         bake_types={'POSE'})
        # the bake rewrites the action, so the IK weight goes back in after it
        for f, v in sorted(ik.items()):
            arm["IKWeight"] = v
            arm.keyframe_insert('["IKWeight"]', frame=f)
        print("BAKED %-22s %4d curves%s"
              % (act.name, len(action_fcurves(act)),
                 "  + IKWeight %d keys" % len(ik) if ik else ""))

    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')

    # leave the file openable: the strips above hide whichever weapon they are
    # not shooting and leave the scene on Workbench, so saving straight after
    # them ships a blend where half the meshes never render and the rest look
    # untextured.
    BA.leave_openable()
    arm.animation_data.action = by["Arms_Idle"]
    sc.frame_start, sc.frame_end, sc.frame_current = 1, 90, 1
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("=" * 72)


if __name__ == "__main__":
    main()
