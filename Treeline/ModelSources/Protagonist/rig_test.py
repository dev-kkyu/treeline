"""Protagonist RIG_TEST - the approval checks in 00-rigging.md, measured and
rendered.

    blender Protagonist_Rig.blend -b --python rig_test.py

Writes PNGs to OUT_DIR and prints numbers. Nothing here is exported; this only
decides whether the rig is good enough to start building clips on.

The weapon-grip checks in 00-rigging.md are not here. They belong to the
first-person arms rig, which is a separate file with the weapon bones in it -
this body is only ever seen as a shadow and in the ending long shot. What is
checked instead is everything the three body clips lean on: reach, fold
direction, and whether a positive rotation closes a finger or opens it.
"""
import bpy
import math
import os
from mathutils import Vector, Quaternion, Matrix, kdtree

OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\rigtest_protagonist")

arm = bpy.data.objects["Protagonist_Armature"]
mesh_ob = bpy.data.objects["Protagonist_Mesh"]

# pose bone location and rotation are expressed in the bone's own rest frame, so
# anything phrased in world terms has to be converted first
_basis = {pb.name: pb.bone.matrix_local.to_3x3().inverted() for pb in arm.pose.bones}


def wloc(name, dx, dy, dz):
    arm.pose.bones[name].location = _basis[name] @ Vector((dx, dy, dz))


def wrot(name, axis, deg):
    pb = arm.pose.bones[name]
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = Quaternion((_basis[name] @ Vector(axis)).normalized(),
                                        math.radians(deg))


def reset():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def head(name):
    return arm.matrix_world @ arm.pose.bones[name].head


def tail(name):
    return arm.matrix_world @ arm.pose.bones[name].tail


def bone_dir(name):
    return (tail(name) - head(name)).normalized()


def signed_bend(upper, lower, axis):
    """Fold angle about the joint's hinge axis. Negative means it bent the wrong
    way, which is the only thing that counts as a flip; a deep fold does not."""
    u1, u2 = bone_dir(upper), bone_dir(lower)
    return math.degrees(math.atan2(u1.cross(u2).dot(axis), u1.dot(u2)))


def deformed():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(dg)
    me = ev.to_mesh()
    co = [mesh_ob.matrix_world @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return co


def dominant_groups():
    names = [g.name for g in mesh_ob.vertex_groups]
    out = []
    for vt in mesh_ob.data.vertices:
        best, bw = None, 0.0
        for g in vt.groups:
            if g.weight > bw:
                best, bw = names[g.group], g.weight
        out.append(best)
    return out


DOM = dominant_groups()


def min_gap(co, set_a, set_b):
    """Closest approach between two body parts. Falls below ~0 when they meet."""
    ia = [i for i, d in enumerate(DOM) if d in set_a]
    ib = [i for i, d in enumerate(DOM) if d in set_b]
    if not ia or not ib:
        return None
    tree = kdtree.KDTree(len(ib))
    for n, i in enumerate(ib):
        tree.insert(co[i], n)
    tree.balance()
    return min(tree.find(co[i])[2] for i in ia)


TORSO = ("Hips", "Spine", "Chest", "UpperChest")
ARM_R = ("UpperArm.R", "LowerArm.R")
PALM_R = ("Hand.R",)
FINGERS_R = tuple("%s%d.R" % (f, i) for f in ("Index", "Middle", "Ring", "Little")
                  for i in (1, 2, 3))


# ---------------------------------------------------------------- rendering

def setup_render():
    os.makedirs(OUT_DIR, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'
    sc.display.shading.color_type = 'SINGLE'
    sc.display.shading.single_color = (0.62, 0.63, 0.66)
    sc.display.shading.show_cavity = True
    sc.render.resolution_x = 720
    sc.render.resolution_y = 720
    sc.world = sc.world or bpy.data.worlds.new("W")
    sc.world.color = (0.09, 0.10, 0.12)

    target = bpy.data.objects.new("CamTarget", None)
    sc.collection.objects.link(target)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    sc.collection.objects.link(cam)
    c = cam.constraints.new('TRACK_TO')
    c.target = target
    c.track_axis = 'TRACK_NEGATIVE_Z'
    c.up_axis = 'UP_Y'
    sc.camera = cam
    return cam, target


CAM, TARGET = setup_render()

VIEWS = {
    "front":    ((-0.95, -4.20, 1.55), (0, 0, 0.95), 50),
    "side":     ((4.10, -0.60, 1.45), (0, 0, 0.95), 50),
    "legs":     ((-0.70, -2.40, 0.90), (0, -0.02, 0.52), 55),
    "hand":     ((1.02, -0.38, 1.60), (0.84, 0.05, 1.42), 70),
    "shoulder": ((0.85, -1.15, 2.00), (0.22, 0.03, 1.45), 60),
}


def shot(name, view):
    pos, tgt, lens = VIEWS[view]
    CAM.location = pos
    TARGET.location = tgt
    CAM.data.lens = lens
    bpy.context.scene.render.filepath = os.path.join(OUT_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("SHOT", name)


# ------------------------------------------------------------------- checks

print("=" * 68)

# 1. rest: the numbers MODEL-SPEC.md fixes, and the reference every gap is read
# against later
reset()
rest_co = deformed()
rest = {
    "height": max(c.z for c in rest_co),
    "foot.L": min(c.z for c in rest_co if c.x < 0),
    "foot.R": min(c.z for c in rest_co if c.x > 0),
    "eye": head("Eye.R").z,
    "span": max(c.x for c in rest_co) - min(c.x for c in rest_co),
}
print("TEST1 rest  height %.4f  eye %.4f  soles L %.4f R %.4f  span %.4f"
      % (rest["height"], rest["eye"], rest["foot.L"], rest["foot.R"], rest["span"]))
print("TEST1 spec  height 1.8200 +/-0.01   eye 1.6500 +/-0.01   soles 0.0000")
rest_gap_arm = min_gap(rest_co, ARM_R, TORSO)
rest_gap_fing = min_gap(rest_co, FINGERS_R, PALM_R)
mid_tip_rest = tail("Middle3.R")
print("TEST1 gaps  arm-torso %.4f  fingers-palm %.4f  middle fingertip z %.4f"
      % (rest_gap_arm, rest_gap_fing, mid_tip_rest.z))
shot("01_rest_front", "front")
shot("01_rest_side", "side")
shot("01_rest_hand", "hand")

# 2. pull each effector towards the body: the fold has to deepen, never reverse.
# The move has to shorten the shoulder-to-hand distance. A T-pose arm is already
# within 3 mm of full extension, so lifting the hand straight up asks for a point
# 56 mm out of reach and the elbow answers by straightening - which reads as a
# flip but is only the target being unreachable.
LIMBS = (("arm.R", "CTRL_HandIK.R", "UpperArm.R", "LowerArm.R", "Hand.R",
          (-0.20, -0.22, 0.10)),
         ("arm.L", "CTRL_HandIK.L", "UpperArm.L", "LowerArm.L", "Hand.L",
          (0.20, -0.22, 0.10)),
         ("leg.R", "CTRL_FootIK.R", "UpperLeg.R", "LowerLeg.R", "Foot.R",
          (0, -0.10, 0.30)),
         ("leg.L", "CTRL_FootIK.L", "UpperLeg.L", "LowerLeg.L", "Foot.L",
          (0, -0.10, 0.30)))
for tag, ctrl, upper, lower, eff, d in LIMBS:
    reset()
    axis = bone_dir(upper).cross(bone_dir(lower)).normalized()
    rest_bend = signed_bend(upper, lower, axis)
    p0 = head(eff)
    wloc(ctrl, *d)
    bpy.context.view_layer.update()
    bend = signed_bend(upper, lower, axis)
    err = (head(eff) - (p0 + Vector(d))).length
    print("TEST2 %s  IK error %5.2f mm  fold %5.1f -> %5.1f deg  %s"
          % (tag, err * 1000, rest_bend, bend, "FLIP" if bend < 0.5 else "ok"))
    if tag == "arm.R":
        shot("02_reach_arm", "front")
    elif tag == "leg.R":
        shot("02_reach_leg", "legs")

# 3. stride: swing one foot 0.30 m forward and 0.30 m back, foot 0.12 m off the
# floor. A flat 0.35 m swing leaves the leg 3 mm short of straight and the fold
# angle stops meaning anything.
for dy, name in ((-0.30, "forward"), (0.30, "back")):
    reset()
    axis = bone_dir("UpperLeg.R").cross(bone_dir("LowerLeg.R")).normalized()
    wloc("CTRL_FootIK.R", 0, dy, 0.12)
    bpy.context.view_layer.update()
    knee = head("LowerLeg.R")
    ankle = head("Foot.R")
    print("TEST3 stride %-7s  ankle y %+.3f  knee (%+.3f %+.3f %+.3f)  fold %5.1f deg  %s"
          % (name, ankle.y, knee.x, knee.y, knee.z,
             signed_bend("UpperLeg.R", "LowerLeg.R", axis),
             "FLIP" if signed_bend("UpperLeg.R", "LowerLeg.R", axis) < 0.5 else "ok"))
    shot("03_stride_" + name, "side")

# 4. crouch: drop the hips onto planted feet
reset()
axis_l = bone_dir("UpperLeg.R").cross(bone_dir("LowerLeg.R")).normalized()
hips0 = head("Hips").z
wloc("Hips", 0, 0, -0.35)
bpy.context.view_layer.update()
co = deformed()
print("TEST4 crouch  hips %.3f -> %.3f  knee fold %5.1f deg  soles %.4f / %.4f"
      % (hips0, head("Hips").z, signed_bend("UpperLeg.R", "LowerLeg.R", axis_l),
         min(c.z for c in co if c.x > 0), min(c.z for c in co if c.x < 0)))
shot("04_crouch_side", "side")
shot("04_crouch_front", "front")

# 5. arm raised 120 deg: 00-rigging.md wants the shoulder checked there
reset()
sh = head("UpperArm.R")
off = head("Hand.R") - sh
for deg in (60, 120):
    reset()
    # +X tilting towards +Z is a negative turn about world Y
    tgt = sh + (Matrix.Rotation(math.radians(-deg), 3, 'Y') @ off)
    d = tgt - head("Hand.R")
    wloc("CTRL_HandIK.R", d.x, d.y, d.z)
    bpy.context.view_layer.update()
    co = deformed()
    gap = min_gap(co, ARM_R, TORSO)
    err = (head("Hand.R") - tgt).length
    print("TEST5 arm %3d deg  IK error %5.2f mm  arm-torso gap %.4f (rest %.4f)  %s"
          % (deg, err * 1000, gap, rest_gap_arm, "TOUCH" if gap < 0.005 else "ok"))
    shot("05_raise%d_shoulder" % deg, "shoulder")

# 6. finger roll: a positive turn about a finger's own X has to close the hand.
# This is the one thing in the rig that a wrong roll silently inverts.
for deg in (45, -45):
    reset()
    for f in ("Index", "Middle", "Ring", "Little"):
        for i in (1, 2, 3):
            arm.pose.bones["%s%d.R" % (f, i)].rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()
    co = deformed()
    tip = tail("Middle3.R")
    print("TEST6 fingers %+d  fingertip z %.4f (rest %.4f, %+5.1f mm)  "
          "fingers-palm %.4f (rest %.4f)  %s"
          % (deg, tip.z, mid_tip_rest.z, (tip.z - mid_tip_rest.z) * 1000,
             min_gap(co, FINGERS_R, PALM_R), rest_gap_fing,
             "closes" if tip.z < mid_tip_rest.z - 0.01 else "OPENS"))
    shot("06_fingers%+d_hand" % deg, "hand")

# 7. thumb: it folds across the palm, so its own test is which way the tip moves
thumb_rest = tail("Thumb3.R")
for deg in (50, -50):
    reset()
    for i in (1, 2, 3):
        arm.pose.bones["Thumb%d.R" % i].rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()
    tip = tail("Thumb3.R")
    d = tip - thumb_rest
    print("TEST7 thumb %+d  tip moved (%+.1f %+.1f %+.1f) mm  %s"
          % (deg, d.x * 1000, d.y * 1000, d.z * 1000,
             "towards the palm" if d.y > 0.005 and d.z < 0 else "away"))
    shot("07_thumb%+d_hand" % deg, "hand")

reset()
print("=" * 68)
