"""Shared pose vocabulary for the Clinger clips.

Imported by animate.py and preview.py. Nothing here writes keyframes - each
function just puts the rig in a pose so the caller can key it.

Bend convention, measured on the built rig rather than assumed: every bone's
local X is the anatomical fold and points at world +Y, local Z swings the limb
sideways, local Y twists.

IMPORTANT: the left side is NOT the usual Blender mirror. build_rig.py rolls
every bone off the same world Y reference, which leaves `.L` frames related to
`.R` by a flip of X *and* Y, not of Y and Z. Mirroring a pose therefore negates
rx and ry and leaves rz alone - the opposite of the convention most rigs use.
`srot` is the only place that knows this; nothing else should hand-write a side
sign. (Verified: rx -30 / rz +30 on `.L` reproduce rx +30 / rz +30 on `.R` to
0.0000 m.)
"""
import bpy
import math
from mathutils import Vector

arm = bpy.data.objects["Clinger_Armature"]
mesh = bpy.data.objects["Clinger_Mesh"]

for _pb in arm.pose.bones:
    _pb.rotation_mode = 'XYZ'

_basis = {pb.name: pb.bone.matrix_local.to_3x3().inverted() for pb in arm.pose.bones}

T0, T1, T2 = "DEF_TorsoBase", "DEF_TorsoMid", "DEF_TorsoTip"
HEAD, JAW, BELLY = "DEF_Head", "DEF_Jaw", "DEF_FoldBody"
PIVOT = "GroundPivot"
CEIL_TAGS = ("RF", "LF", "RR", "LR")
FRONT_TAGS = ("RF", "LF")
REAR_TAGS = ("RR", "LR")
SIDES = ("R", "L")

IK_CEIL = {t: "CTRL_CeilIK." + t for t in CEIL_TAGS}
IK_HAND = {s: "CTRL_AttackIK." + s for s in SIDES}

# Landed stance, tuned against the rig and then measured back (see rig_test.py).
# The arms are 1.55 m long, far longer than the 0.82 m ceiling limbs, so the
# stance tents them over the back like a spider and lets the short rear limbs
# take the hind contacts.
GROUND_PITCH = -52.0
GROUND_ANCHOR = Vector((0.0, 0.46, 0.94))       # world position of TorsoBase head
# the hand IK drives the *wrist*, so it is planted 0.20 m up and the palm is
# rolled down onto the floor underneath it
HAND_PLANT = {"R": Vector((0.62, -0.55, 0.20)), "L": Vector((-0.62, -0.55, 0.20))}
HAND_ROLL = (-30.0, -10.0)                      # wrist, digits
# the rear limbs are only 0.82 m, so this is as far back as they can plant while
# the back stays at 0.98 m. Anything higher and the hooks stop touching the floor.
HOOK_PLANT = {"RR": Vector((0.50, 0.42, 0.0)), "LR": Vector((-0.50, 0.42, 0.0))}


# --------------------------------------------------------------- primitives

def rot(name, x=0.0, y=0.0, z=0.0):
    arm.pose.bones[name].rotation_euler = (math.radians(x), math.radians(y),
                                           math.radians(z))


def srot(base, side, x=0.0, y=0.0, z=0.0):
    """Rotate a sided bone with right-hand numbers; mirrors for `.L`/`.LF`/`.LR`."""
    if side.startswith("L"):
        x, y = -x, -y
    rot(base + "." + side, x, y, z)


def addrot(name, x=0.0, y=0.0, z=0.0):
    e = arm.pose.bones[name].rotation_euler
    arm.pose.bones[name].rotation_euler = (e.x + math.radians(x),
                                           e.y + math.radians(y),
                                           e.z + math.radians(z))


def saddrot(base, side, x=0.0, y=0.0, z=0.0):
    if side.startswith("L"):
        x, y = -x, -y
    addrot(base + "." + side, x, y, z)


def wloc(name, x, y, z):
    """Translate a pose bone by a world-space delta."""
    arm.pose.bones[name].location = _basis[name] @ Vector((x, y, z))


def addwloc(name, x, y, z):
    arm.pose.bones[name].location += _basis[name] @ Vector((x, y, z))


def update():
    bpy.context.view_layer.update()


def bone_head(name):
    update()
    return arm.matrix_world @ arm.pose.bones[name].head


def bone_tail(name):
    update()
    return arm.matrix_world @ arm.pose.bones[name].tail


def evaluated_points():
    update()
    ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    return [mesh.matrix_world @ v.co for v in ev.data.vertices]


def bbox():
    p = evaluated_points()
    return (min(v.x for v in p), max(v.x for v in p),
            min(v.y for v in p), max(v.y for v in p),
            min(v.z for v in p), max(v.z for v in p))


def reset():
    for pb in arm.pose.bones:
        pb.location = (0.0, 0.0, 0.0)
        pb.rotation_euler = (0.0, 0.0, 0.0)
    set_ik(ceiling=0.0, hands=0.0)


# ------------------------------------------------------------- IK influence

def _ik(bone):
    return next(c for c in arm.pose.bones[bone].constraints if c.type == 'IK')


def set_ik(ceiling=None, hands=None, rear=None, front=None):
    if ceiling is not None:
        for t in CEIL_TAGS:
            _ik("DEF_CeilLower." + t).influence = ceiling
    if front is not None:
        for t in FRONT_TAGS:
            _ik("DEF_CeilLower." + t).influence = front
    if rear is not None:
        for t in REAR_TAGS:
            _ik("DEF_CeilLower." + t).influence = rear
    if hands is not None:
        for s in SIDES:
            _ik("DEF_AttackFore." + s).influence = hands


def ik_target(name, pos):
    rest = arm.data.bones[name].head_local
    arm.pose.bones[name].location = _basis[name] @ (Vector(pos) - rest)


def hooks_home():
    """Every IK target back where it was built - the ceiling plane, and the
    hands at their hanging rest."""
    for t in CEIL_TAGS:
        arm.pose.bones[IK_CEIL[t]].location = (0.0, 0.0, 0.0)
    for s in SIDES:
        arm.pose.bones[IK_HAND[s]].location = (0.0, 0.0, 0.0)


# --------------------------------------------------------- ceiling posture

# how far the body can ride up toward the ceiling before the 0.82 m limbs
# straighten out and the hooks start to drag
CLING_LIFT = 0.42


def cling(fold=1.0, sag=0.0, curl=0.0, swell=0.0, splay=0.0, wrap=1.0,
          shift=(0.0, 0.0)):
    """Hanging from all four hooks.

    The fold is not keyed onto the limb bones - the hooks are pinned by IK, so
    hauling the body up toward the ceiling is what makes the limbs Z-fold, the
    same way it works on the animal.

    fold   0 = hanging at full stretch, 1 = hauled up tight under the ceiling
    sag    extra metres the mass drops away from the ceiling
    curl   degrees of spine curl on top of the fold, at TorsoMid
    swell  belly bulge, 0..1
    splay  how far the wrapped arms come off the body, 0..1
    wrap   0 = arms hang free, 1 = arms wrapped around the torso
    shift  (x, y) metres the mass slides under the fixed hooks
    """
    rot(T0, x=-13 * fold - curl * 0.30)
    rot(T1, x=-22 * fold - curl * 0.45)
    rot(T2, x=-11 * fold - curl * 0.25)
    rot(HEAD, x=-17 * fold)
    rot(BELLY, x=-38 * swell)
    for s in SIDES:
        srot("DEF_AttackUpper", s, x=-52 * wrap + 44 * splay, z=24 * wrap - 28 * splay)
        srot("DEF_AttackFore", s, x=100 * wrap - 68 * splay)
        srot("DEF_AttackHand", s, x=30 * wrap, z=-16 * wrap)
        srot("DEF_DigitsGroup", s, x=24 * wrap)
        srot("DEF_FoldArm", s, x=-12 * wrap)
    wloc(T0, shift[0], shift[1], CLING_LIFT * fold - sag)


def hang(drop=0.0, arch=0.0, gape=0.0, reach=0.0, spread=0.0):
    """Fully unfurled and head-down - the shape Unfold_Drop ends on.

    arch    degrees the spine bends backward past straight
    gape    jaw degrees
    reach   attack arms swing forward and out, 0..1 (can go past 1 to overshoot)
    spread  ceiling limbs open away from the body, 0..1
    """
    rot(T0, x=arch * 0.30)
    rot(T1, x=arch * 0.45)
    rot(T2, x=arch * 0.25)
    rot(HEAD, x=arch * 0.30)
    rot(JAW, x=gape)
    for t in CEIL_TAGS:
        srot("DEF_CeilUpper", t, x=-6 - 16 * spread, z=(6 if t in REAR_TAGS else -6) * (1 + spread))
        srot("DEF_CeilLower", t, x=8 + 20 * spread)
    for s in SIDES:
        srot("DEF_AttackUpper", s, x=-26 * reach, z=-20 * reach)
        srot("DEF_AttackFore", s, x=20 * reach)
        srot("DEF_AttackHand", s, x=-16 * reach)
        srot("DEF_DigitsGroup", s, x=-24 * reach)
    wloc(T0, 0.0, 0.0, -drop)


# ---------------------------------------------------------- ground posture

def ground_body(pitch=None, anchor=None, roll=0.0, yaw=0.0, curl=0.0, lean=0.0):
    """Swing the body down into the landed stance.

    The swing happens at GroundPivot and would throw the body two metres in
    front of the origin, so a translation puts TorsoBase back on `anchor`. That
    keeps the transform at the feet, which is where the agent wants it.
    """
    p = GROUND_PITCH if pitch is None else pitch
    a = Vector(GROUND_ANCHOR if anchor is None else anchor)
    rot(PIVOT, x=p, y=roll, z=yaw)
    wloc(PIVOT, 0.0, 0.0, 0.0)
    d = a - bone_head(T0)
    wloc(PIVOT, d.x, d.y, d.z)
    rot(T0, x=curl * 0.30 + lean * 0.5)
    rot(T1, x=curl * 0.45)
    rot(T2, x=curl * 0.25)


def fold_front_limbs(amount=1.0, twitch=(0.0, 0.0), hump=0.0):
    """LF/RF stow along the back. They must not read as two more legs."""
    for i, t in enumerate(FRONT_TAGS):
        srot("DEF_CeilUpper", t, x=-104 * amount + 26 * hump + twitch[i],
             z=-16 * amount)
        srot("DEF_CeilLower", t, x=126 * amount - 20 * hump - twitch[i] * 1.6)


def plant_ground(hands=None, hooks=None, roll=None):
    h = HAND_PLANT if hands is None else hands
    k = HOOK_PLANT if hooks is None else hooks
    wrist, digits = HAND_ROLL if roll is None else roll
    for s in SIDES:
        ik_target(IK_HAND[s], h[s])
        srot("DEF_AttackHand", s, x=wrist)
        srot("DEF_DigitsGroup", s, x=digits)
    for t in REAR_TAGS:
        ik_target(IK_CEIL[t], k[t])


def settle(limit=0.002, tries=8):
    """Lift the body until nothing is under the floor.

    The head hangs off the low end of the torso and the concept angles push the
    jaw straight through the ground, so the stance is finished by measuring the
    mesh rather than by trusting the numbers.
    """
    zmin = 0.0
    for _ in range(tries):
        zmin = min(p.z for p in evaluated_points())
        if zmin >= -limit:
            return zmin
        addwloc(PIVOT, 0.0, 0.0, -zmin + limit)
    return zmin


def contact_clamp(hands=None, hooks=None, limit=0.001, tries=8):
    """Lift each planted contact until its claws stop cutting into the floor.

    `settle` moves the body, which cannot help here: the contact is pinned by IK,
    so body roll swings the claws below the floor while the contact point stays
    put. The fix has to move the contact. Both pairs need it - the rear hooks dig
    in during the landing impact exactly the way the hands do.

    Points are assigned to a contact by which side and end of the body they are
    on, which is enough: nothing else on this animal reaches |x| > 0.35 and the
    floor at the same time.
    """
    h = dict(HAND_PLANT if hands is None else hands)
    k = dict(HOOK_PLANT if hooks is None else hooks)
    cur = {}
    for s in SIDES:
        cur["hand" + s] = Vector(h[s])
    for t in REAR_TAGS:
        cur["hook" + t] = Vector(k[t])
    for _ in range(tries):
        dip = dict((n, 0.0) for n in cur)
        for p in evaluated_points():
            if p.z >= -limit or abs(p.x) < 0.35:
                continue
            side = "R" if p.x > 0.0 else "L"
            n = ("hand" + side) if p.y < 0.0 else ("hook" + side + "R")
            if n in dip:
                dip[n] = min(dip[n], p.z)
        if not any(v < 0.0 for v in dip.values()):
            break
        for n, d in dip.items():
            if d >= 0.0:
                continue
            cur[n] = cur[n] + Vector((0.0, 0.0, -d + limit))
            if n.startswith("hand"):
                ik_target(IK_HAND[n[4:]], cur[n])
            else:
                ik_target(IK_CEIL[n[4:]], cur[n])
    return cur


def ceiling_clamp(limit=0.004, tries=8):
    """Push the body down until nothing pokes through the ceiling.

    The hooks bite in, so a few millimetres above z = 0 is correct; half a metre
    is the body hanging *inside* the slab. Needed once the hook IK is released
    and nothing is holding the limbs down any more.
    """
    zmax = 0.0
    for _ in range(tries):
        zmax = max(p.z for p in evaluated_points())
        if zmax <= limit:
            return zmax
        addwloc(T0, 0.0, 0.0, -zmax + limit)
    return zmax


def stance(hands=None, hooks=None, roll=None, front=1.0, twitch=(0.0, 0.0),
           hump=0.5, do_settle=True, **kw):
    """The whole landed pose in one call."""
    ground_body(**kw)
    fold_front_limbs(front, twitch, hump)
    plant_ground(hands, hooks, roll)
    set_ik(front=0.0, rear=1.0, hands=1.0)
    if do_settle:
        # the claws go under the floor before anything else does, and lifting the
        # body cannot fix a contact that IK is holding still - clamp the hand first
        # order matters: clamp the claws up first, then settle. The other way
        # round, settle lifts the whole body to clear a claw and the short rear
        # limbs stop reaching the floor 24 mm later.
        contact_clamp(hands, hooks)
        settle()
        contact_clamp(hands, hooks)
