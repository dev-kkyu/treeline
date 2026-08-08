"""Colossus RIG_TEST — the six checks in 00-rigging.md, measured and rendered.

    blender Colossus_Rig_base.blend -b --python rig_test.py

Writes PNGs to OUT_DIR and prints numbers. Nothing here is exported; this only
decides whether the rig is good enough to start building clips on.
"""
import bpy
import math
import os
from mathutils import Vector, Quaternion, kdtree

OUT_DIR = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad\rigtest"

arm = bpy.data.objects["Colossus_Armature"]
mesh_ob = bpy.data.objects["Colossus_Mesh"]

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

TORSO = ("DEF_Chest", "DEF_Spine01", "DEF_Spine02", "DEF_Spine03", "DEF_Pelvis")
ARM_R = ("DEF_UpperArm.R", "DEF_ForeArm.R")
HAND_R = ("DEF_Wrist.R",)


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


def deformed():
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh_ob.evaluated_get(dg)
    me = ev.to_mesh()
    co = [mesh_ob.matrix_world @ v.co for v in me.vertices]
    ev.to_mesh_clear()
    return co


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


def reset():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def bone_dir(name):
    pb = arm.pose.bones[name]
    return ((arm.matrix_world @ pb.tail) - (arm.matrix_world @ pb.head)).normalized()


def signed_bend(upper, lower, axis):
    """Fold angle about the joint's hinge axis. Negative means it bent the wrong
    way, which is the only thing that counts as a flip; a deep fold does not."""
    u1, u2 = bone_dir(upper), bone_dir(lower)
    return math.degrees(math.atan2(u1.cross(u2).dot(axis), u1.dot(u2)))


def contacts(co):
    zmin = min(c.z for c in co)
    low = [c for c in co if c.z < zmin + 0.08]
    out = {}
    for name, sel in (("hand.L", [c for c in low if c.x < -0.3 and c.y < 0]),
                      ("hand.R", [c for c in low if c.x > 0.3 and c.y < 0]),
                      ("foot.L", [c for c in low if c.x < 0 and c.y > 0]),
                      ("foot.R", [c for c in low if c.x > 0 and c.y > 0])):
        out[name] = min((c.z for c in sel), default=None)
    return out


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
    sc.render.film_transparent = False
    sc.world = sc.world or bpy.data.worlds.new("W")
    sc.world.color = (0.09, 0.10, 0.12)

    target = bpy.data.objects.new("CamTarget", None)
    sc.collection.objects.link(target)
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    c = cam.constraints.new('TRACK_TO')
    c.target = target
    c.track_axis = 'TRACK_NEGATIVE_Z'
    c.up_axis = 'UP_Y'
    sc.camera = cam
    return cam, target


CAM, TARGET = setup_render()

VIEWS = {
    "front":  ((-6.6, -9.0, 4.2), (0, 0, 2.10), 50),
    "side":   ((9.6, -1.2, 3.0), (0, -0.3, 2.10), 50),
    "top":    ((0.0, -6.4, 7.4), (0, -0.5, 2.30), 50),
    "hand":   ((3.5, -3.4, 1.5), (1.85, -1.25, 0.35), 60),
    # close on the two places the spec calls dangerous: the neckless head/chest
    # boundary and the shoulder the arm brushes past
    "head":     ((-2.9, -4.4, 2.9), (0.0, -1.15, 2.05), 62),
    "headtop":  ((0.0, -3.0, 5.4), (0.0, -1.05, 2.20), 55),
    "shoulder": ((4.6, -3.2, 4.6), (1.35, 0.05, 3.10), 62),
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

print("=" * 64)

# 6 first: rest is the reference every other number is read against
reset()
rest_co = deformed()
rest_contacts = contacts(rest_co)
lo = min(v for v in rest_contacts.values())
hi = max(v for v in rest_contacts.values())
print("TEST6 rest contacts %s  spread %.1f mm"
      % ({k: round(v * 1000, 1) for k, v in rest_contacts.items()}, (hi - lo) * 1000))
rest_gap_arm = min_gap(rest_co, ARM_R, TORSO)
rest_gap_jaw = min_gap(rest_co, ("DEF_Jaw",), TORSO)
rest_gap_dig = min_gap(rest_co, ("DEF_Digits.R",), HAND_R)
rest_jaw_z = min(rest_co[i].z for i, d in enumerate(DOM) if d == "DEF_Jaw")
print("TEST_REF gaps  arm-torso %.3f  jaw-torso %.3f  digits-palm %.3f  jaw z %.3f"
      % (rest_gap_arm, rest_gap_jaw, rest_gap_dig, rest_jaw_z))
shot("06_rest_front", "front")
shot("06_rest_side", "side")
shot("06_rest_head", "head")
shot("06_rest_shoulder", "shoulder")
shot("06_rest_headtop", "headtop")

# 1. lift each contact 0.40 m
for tag, ctrl, upper, lower, eff in (
        ("FL", "CTRL_LimbIK.FL", "DEF_UpperArm.L", "DEF_ForeArm.L", "DEF_Wrist.L"),
        ("FR", "CTRL_LimbIK.FR", "DEF_UpperArm.R", "DEF_ForeArm.R", "DEF_Wrist.R"),
        ("HL", "CTRL_LimbIK.HL", "DEF_Thigh.L", "DEF_Shin.L", "DEF_Foot.L"),
        ("HR", "CTRL_LimbIK.HR", "DEF_Thigh.R", "DEF_Shin.R", "DEF_Foot.R")):
    reset()
    axis = bone_dir(upper).cross(bone_dir(lower)).normalized()
    rest_bend = signed_bend(upper, lower, axis)
    tip0 = (arm.matrix_world @ arm.pose.bones[eff].tail).z
    wloc(ctrl, 0, 0, 0.40)
    bpy.context.view_layer.update()
    bend = signed_bend(upper, lower, axis)
    tip1 = (arm.matrix_world @ arm.pose.bones[eff].tail).z
    print("TEST1 %s  effector z %.3f -> %.3f  fold %.1f -> %.1f deg  %s"
          % (tag, tip0, tip1, rest_bend, bend,
             "FLIP" if bend < 1.0 else "ok"))
    shot("01_lift_" + tag, "front")

# 2. drop the body 0.25 m onto planted limbs
reset()
chest0 = (arm.matrix_world @ arm.pose.bones["DEF_Chest"].head).z
wloc("Colossus_Motion", 0, 0, -0.25)
bpy.context.view_layer.update()
chest1 = (arm.matrix_world @ arm.pose.bones["DEF_Chest"].head).z
co = deformed()
gap = min_gap(co, ARM_R, TORSO)
print("TEST2 crouch  chest z %.3f -> %.3f  arm-torso gap %.3f (rest %.3f)  %s"
      % (chest0, chest1, gap, rest_gap_arm, "TOUCH" if gap < 0.01 else "ok"))
shot("02_crouch_front", "front")
shot("02_crouch_side", "side")
shot("02_crouch_shoulder", "shoulder")

# 3. jaw to the rig limit
for deg in (45, 55):
    reset()
    arm.pose.bones["DEF_Jaw"].rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()
    co = deformed()
    gap = min_gap(co, ("DEF_Jaw",), TORSO)
    jaw_z = min(co[i].z for i, d in enumerate(DOM) if d == "DEF_Jaw")
    print("TEST3 jaw %d  jaw-torso gap %.3f (rest %.3f)  lowest jaw z %.3f  %s"
          % (deg, gap, rest_gap_jaw, jaw_z, "TOUCH" if gap < 0.01 else "ok"))
    shot("03_jaw%d_side" % deg, "side")
    shot("03_jaw%d_head" % deg, "head")

# 4. head yaw 12
reset()
pivot = arm.matrix_world @ arm.pose.bones["DEF_HeadBase"].head
tip_0 = (arm.matrix_world @ arm.pose.bones["DEF_HeadTip"].tail) - pivot
wrot("DEF_HeadBase", (0, 0, 1), 12)
bpy.context.view_layer.update()
tip_r = (arm.matrix_world @ arm.pose.bones["DEF_HeadTip"].tail) - pivot
yaw = math.degrees(math.atan2(tip_r.x, -tip_r.y) - math.atan2(tip_0.x, -tip_0.y))
co = deformed()
gap = min_gap(co, ("DEF_HeadBase", "DEF_HeadTip"), TORSO)
print("TEST4 head yaw  tip swept %.1f deg  head-torso gap %.3f (rest %.3f)"
      % (yaw, gap, min_gap(rest_co, ("DEF_HeadBase", "DEF_HeadTip"), TORSO)))
shot("04_headyaw_top", "top")
shot("04_headyaw_headtop", "headtop")
shot("04_headyaw_head", "head")

# 5. digits curled. Which sign grips is a property of the bone roll, so try both.
wrist_head = arm.matrix_world @ arm.pose.bones["DEF_Wrist.R"].head
dig_idx = [i for i, d in enumerate(DOM) if d == "DEF_Digits.R"]
tips = sorted(dig_idx, key=lambda i: -(rest_co[i] - wrist_head).length)[:24]


def tip_centre(co):
    return sum((co[i] for i in tips), Vector()) / len(tips)


print("TEST5 rest fingertip %s  reach %.3f"
      % (tuple(round(c, 3) for c in tip_centre(rest_co)),
         (tip_centre(rest_co) - wrist_head).length))
for deg in (70, -70):
    reset()
    # Digits copies the control's rotation, so the control is what to turn
    for side in ("L", "R"):
        arm.pose.bones["CTRL_LimbIK.F" + side].rotation_euler = (math.radians(deg), 0, 0)
    bpy.context.view_layer.update()
    co = deformed()
    gap = min_gap(co, ("DEF_Digits.R",), HAND_R)
    tc = tip_centre(co)
    print("TEST5 digits %+d  fingertip %s  reach %.3f  digits-palm gap %.3f (rest %.3f)  %s"
          % (deg, tuple(round(c, 3) for c in tc), (tc - wrist_head).length,
             gap, rest_gap_dig, "TOUCH" if gap < 0.005 else "ok"))
    shot("05_digits%+d_hand" % deg, "hand")

reset()
print("=" * 64)
