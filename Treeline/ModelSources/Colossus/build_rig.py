"""Colossus rig builder.

Runs headless:
    blender colossus.blend -b --python build_rig.py

Follows docs/enemy-sheets/mockups/codex/colossus/final/animation/00-rigging.md:
28 deform bones + Root/Motion, control rig is four IK targets only,
forward = -Y, Z up, 1 unit = 1 m.

Bone coordinates in the spec are given *before* the origin shift; DY below is
added to every one of them and to the mesh, so that the origin ends up at the
ground-level centre of the four contact patches.
"""
import bpy
import math
from mathutils import Vector

OUT_BLEND = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad\Colossus_Rig_base.blend"

# Measured contact centres: hands y -1.470, feet y +1.182. Midpoint -0.144.
# 00-rigging.md rounds this to -0.15; the measured value is used instead.
DY = 0.144

X = Vector((1, 0, 0))

# limb bones that must never take weight from the opposite side
LIMB = ("UpperArm", "ForeArm", "Wrist", "Digits", "Thigh", "Shin", "Foot", "Toes")


def v(x, y, z):
    return Vector((x, y + DY, z))


# name: (head, tail, parent, connected, deform)
SPINE = [
    ("Colossus_Root", (0, -DY, 0.0), (0, -DY - 0.60, 0.0), None, False, False),
    ("Colossus_Motion", (0, -DY, 0.0), (0, -DY - 0.45, 0.0), "Colossus_Root", False, False),

    ("DEF_Pelvis", (0.00, 1.10, 1.80), (0.00, 0.95, 2.30), "Colossus_Motion", False, True),
    ("DEF_Spine01", (0.00, 0.95, 2.30), (0.00, 0.72, 2.85), "DEF_Pelvis", True, True),
    ("DEF_Spine02", (0.00, 0.72, 2.85), (0.00, 0.50, 3.30), "DEF_Spine01", True, True),
    ("DEF_Spine03", (0.00, 0.50, 3.30), (0.00, 0.20, 3.62), "DEF_Spine02", True, True),
    # Chest has five children, so its tail is not implied by one of them.
    # Point it at the midpoint of the two hump roots.
    ("DEF_Chest", (0.00, 0.20, 3.62), (0.00, 0.30, 4.05), "DEF_Spine03", True, True),

    ("DEF_HeadBase", (0.00, -0.95, 2.35), (0.00, -1.60, 2.10), "DEF_Chest", False, True),
    ("DEF_HeadTip", (0.00, -1.60, 2.10), (0.00, -2.00, 1.95), "DEF_HeadBase", True, True),
    ("DEF_Jaw", (0.00, -1.15, 1.78), (0.00, -1.95, 1.62), "DEF_HeadBase", False, True),
]

# right-side values; mirrored for .L
HUMP = [
    ("DEF_Hump", (0.45, 0.30, 4.05), (0.45, 0.55, 4.45), "DEF_Chest", False),
    ("DEF_HumpTip", (0.45, 0.55, 4.45), (0.45, 0.72, 4.52), "DEF_Hump", True),
]
ARM = [
    ("DEF_UpperArm", (1.20, 0.40, 3.60), (2.15, -0.25, 1.35), "DEF_Chest", False),
    ("DEF_ForeArm", (2.15, -0.25, 1.35), (1.85, -1.00, 0.30), "DEF_UpperArm", True),
    ("DEF_Wrist", (1.85, -1.00, 0.30), (1.88, -1.40, 0.08), "DEF_ForeArm", True),
    ("DEF_Digits", (1.88, -1.40, 0.08), (1.90, -1.85, 0.02), "DEF_Wrist", True),
]
LEG = [
    ("DEF_Thigh", (0.75, 1.18, 1.72), (0.78, 1.06, 1.02), "DEF_Pelvis", False),
    ("DEF_Shin", (0.78, 1.06, 1.02), (0.75, 1.32, 0.36), "DEF_Thigh", True),
    ("DEF_Foot", (0.75, 1.32, 0.36), (0.78, 0.72, 0.03), "DEF_Shin", True),
    ("DEF_Toes", (0.78, 0.72, 0.03), (0.78, 0.58, 0.02), "DEF_Foot", True),
]

# Targets sit at the wrist tail and the foot tail, as 00-rigging.md says, which
# makes the IK chains three long: Wrist+ForeArm+UpperArm and Foot+Shin+Thigh.
# The spec also writes "chain 2", but its own reach numbers (chain length 2.05 m,
# horizontal travel +/-1.11 m) are the three-bone figures, and two bones leave the
# hind leg only +/-0.33 m - not enough for the 1.55 m stride 02-walk-knuckle.md
# asks for. Digits and Toes take the contact orientation by copying the target.
# Each control is laid exactly over the last bone of its limb - same head, tail
# and roll as DEF_Wrist / DEF_Foot - so it can both aim the IK and hand that bone
# a world-space rotation. Head at the joint, tail at the ground contact.
#
# This is the ordinary reverse-foot arrangement and it is here because nothing
# cheaper works. Letting the solver own the last bone (a three-long chain aimed
# at the contact) does reach far enough for the stride, but the solver picks
# ankle angles that drive the heel 72 mm through the floor. Freezing that bone
# instead keeps the sole flat and cuts the hind leg's backward reach to 0.27 m.
# Only an authored roll gives both: the animation code places the joint from the
# contact it wants and the angle it wants.
CTRLS = [
    ("CTRL_LimbIK.FR", (1.85, -1.00, 0.30), (1.88, -1.40, 0.08)),
    ("CTRL_LimbIK.FL", (-1.85, -1.00, 0.30), (-1.88, -1.40, 0.08)),
    ("CTRL_LimbIK.HR", (0.75, 1.32, 0.36), (0.78, 0.72, 0.03)),
    ("CTRL_LimbIK.HL", (-0.75, 1.32, 0.36), (-0.78, 0.72, 0.03)),
]

# Hinge limits, expressed in the chain-plane convention below: 0 is the rest
# angle and positive folds the joint further. Only the elbow and the knee are
# hinges; the shoulder and the hip stay free so IK can reach without a pole.
# The remaining ranges in 00-rigging.md are honoured by the animation code, not
# by constraints, so that baking stays predictable.
# The wrist and the ankle keep world X, which their own joint planes are already
# close to, and the ranges 00-rigging.md gives them.
# The elbow and knee are allowed to straighten roughly 30 degrees below rest.
# 00-rigging.md gives the elbow 0..95 - fold only - but the arm cannot then reach
# forward at all: the hand tops out 0.15 m ahead of rest, against the 0.70 m the
# walk stride needs. Rest already bows the elbow 40 degrees, so -30 still leaves
# it bowed outward, which is what the rule was protecting.
IK_LIMITS = {"DEF_ForeArm": (-30, 95), "DEF_Shin": (-25, 110),
             "DEF_Wrist": (-40, 35), "DEF_Foot": (-35, 55)}


def chain_axis(p0, p1, p2):
    """Normal of the plane the two bones span — the joint's real hinge axis.

    00-rigging.md asks for every local X to point at world +X so that the same
    sign reads the same on both sides. That holds for the legs, which lie almost
    in the sagittal plane, but the arms are splayed out to knuckle-walk and their
    elbow hinge sits about 50 degrees off world X. Rolling them to world X would
    leave the elbow unable to bend on its locked axis at all. Mirroring the
    positions first gives the left side (x, -y, -z) of the right side's axis,
    which is what keeps a positive rotation folding both elbows the same way.
    """
    u1 = (Vector(p1) - Vector(p0)).normalized()
    u2 = (Vector(p2) - Vector(p1)).normalized()
    return u1.cross(u2).normalized()


def roll_axes():
    out = {}
    for side, sgn in (("R", 1), ("L", -1)):
        m = (lambda t: t) if sgn > 0 else mirror
        a = chain_axis(m(ARM[0][1]), m(ARM[1][1]), m(ARM[2][1]))
        lg = chain_axis(m(LEG[0][1]), m(LEG[1][1]), m(LEG[2][1]))
        out["DEF_UpperArm." + side] = a
        out["DEF_ForeArm." + side] = a
        out["DEF_Thigh." + side] = lg
        out["DEF_Shin." + side] = lg
    return out


def mirror(t):
    return (-t[0], t[1], t[2])


def build_bone_table():
    table = [(n, v(*h), v(*t), p, c, d) for n, h, t, p, c, d in SPINE]
    for side, sgn in (("R", 1), ("L", -1)):
        for chain in (HUMP, ARM, LEG):
            for name, head, tail, parent, conn in chain:
                h = head if sgn > 0 else mirror(head)
                t = tail if sgn > 0 else mirror(tail)
                p = parent if parent.startswith(("DEF_Chest", "DEF_Pelvis")) else parent + "." + side
                table.append((name + "." + side, v(*h), v(*t), p, conn, True))
    for name, head, tail in CTRLS:
        table.append((name, v(*head), v(*tail), "Colossus_Root", False, False))
    return table


def report_contacts(mesh_ob, tag):
    co = [mesh_ob.matrix_world @ vt.co for vt in mesh_ob.data.vertices]
    zmin = min(c.z for c in co)
    low = [c for c in co if c.z < zmin + 0.08]
    for name, sel in (("hand.L", [c for c in low if c.x < -0.3 and c.y < 0]),
                      ("hand.R", [c for c in low if c.x > 0.3 and c.y < 0]),
                      ("foot.L", [c for c in low if c.x < 0 and c.y > 0]),
                      ("foot.R", [c for c in low if c.x > 0 and c.y > 0])):
        if sel:
            print("CONTACT %s %-7s n=%3d  lowest z %.4f  centre (%.3f, %.3f)"
                  % (tag, name, len(sel), min(c.z for c in sel),
                     sum(c.x for c in sel) / len(sel), sum(c.y for c in sel) / len(sel)))


def strip_cross_side(mesh_ob):
    """Automatic weights let an arm pick up weight from the opposite arm here,
    because the arms pass close to the body. 00-rigging.md forbids it."""
    groups = {g.index: g.name for g in mesh_ob.vertex_groups}
    removed = 0
    for vt in mesh_ob.data.vertices:
        side_here = "R" if vt.co.x > 0.05 else ("L" if vt.co.x < -0.05 else None)
        if side_here is None:
            continue
        wrong = ".L" if side_here == "R" else ".R"
        for g in list(vt.groups):
            name = groups[g.group]
            if name.endswith(wrong) and any(k in name for k in LIMB) and g.weight > 0:
                mesh_ob.vertex_groups[name].remove([vt.index])
                removed += 1
    print("SKIN: cross-side weights removed %d" % removed)


def main():
    # every clip in the spec is timed at 30 fps; Blender's scene default is 24
    # and the FBX carries that rate, which stretches each clip by 1.25x in Unity
    bpy.context.scene.render.fps = 30
    bpy.context.scene.render.fps_base = 1.0

    mesh_ob = bpy.data.objects["Mesh_0"]
    mesh_ob.name = "Colossus_Mesh"
    mesh_ob.data.name = "Colossus_Mesh"
    if mesh_ob.material_slots and mesh_ob.material_slots[0].material:
        mesh_ob.material_slots[0].material.name = "M_Colossus_FrostPressure"

    report_contacts(mesh_ob, "raw ")
    for vt in mesh_ob.data.vertices:
        vt.co.y += DY

    arm_data = bpy.data.armatures.new("Colossus_Armature")
    arm = bpy.data.objects.new("Colossus_Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    axes = roll_axes()
    for name, head, tail, parent, conn, deform in build_bone_table():
        b = eb.new(name)
        b.head = head
        b.tail = tail
        b.use_deform = deform
        d = (tail - head).normalized()
        roll_vec = axes.get(name, X).cross(d)
        if roll_vec.length < 1e-5:
            roll_vec = Vector((0, 0, 1))
        b.align_roll(roll_vec)
        if parent:
            b.parent = eb[parent]
            b.use_connect = conn
    bpy.ops.object.mode_set(mode='OBJECT')

    # skin
    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print("SKIN: automatic weights ok")

    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    bpy.context.view_layer.objects.active = mesh_ob
    strip_cross_side(mesh_ob)

    # 00-rigging.md: max 4 weights per vertex, drop anything under 0.01, normalize
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01, keep_single=True)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)

    worst = max(sum(1 for g in vt.groups if g.weight > 0.0001) for vt in mesh_ob.data.vertices)
    print("SKIN: max weights per vertex %d" % worst)
    used = set()
    for vt in mesh_ob.data.vertices:
        for g in vt.groups:
            if g.weight > 0.0001:
                used.add(g.group)
    empty = [g.name for g in mesh_ob.vertex_groups if g.index not in used]
    print("SKIN: groups=%d empty=%s" % (len(mesh_ob.vertex_groups), empty))

    # constraints
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    for side in ("R", "L"):
        for bone_name, target in (("DEF_ForeArm." + side, "CTRL_LimbIK.F" + side),
                                  ("DEF_Shin." + side, "CTRL_LimbIK.H" + side)):
            c = arm.pose.bones[bone_name].constraints.new('IK')
            c.target = arm
            c.subtarget = target
            c.chain_count = 2
            c.use_tail = True
        for bone_name, target in (("DEF_Wrist." + side, "CTRL_LimbIK.F" + side),
                                  ("DEF_Foot." + side, "CTRL_LimbIK.H" + side)):
            c = arm.pose.bones[bone_name].constraints.new('COPY_ROTATION')
            c.target = arm
            c.subtarget = target
            c.target_space = 'WORLD'
            c.owner_space = 'WORLD'

    # elbow and knee are hinges on the chain-plane axis, which is what stops the
    # IK from flipping without a pole target
    for pb in arm.pose.bones:
        base = pb.name.rsplit(".", 1)[0]
        if base in IK_LIMITS:
            pb.lock_ik_y = True
            pb.lock_ik_z = True
            pb.use_ik_limit_x = True
            pb.ik_min_x = math.radians(IK_LIMITS[base][0])
            pb.ik_max_x = math.radians(IK_LIMITS[base][1])
    bpy.ops.object.mode_set(mode='OBJECT')

    report_contacts(mesh_ob, "rest")
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("BONES", len(arm_data.bones), "deform",
          sum(1 for b in arm_data.bones if b.use_deform))


main()
