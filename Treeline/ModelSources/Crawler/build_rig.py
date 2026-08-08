"""Crawler rig builder.

Runs headless:
    blender crawler.blend -b --python build_rig.py

Follows docs/enemy-sheets/mockups/codex/crawler/animation/00-rigging.md:
22 deform bones, control rig is IK targets only, forward = -Y, Z up, 1 unit = 1 m.
Bone positions were measured from the mesh (see analyze*.py output).
"""
import bpy
import math
from mathutils import Vector

OUT_BLEND = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad\Crawler_Rig.blend"

# Origin shift: 00-rigging.md wants the root at the ground-level centre of the four
# foot contacts. Measured contacts sit at y -0.80 (front) and -0.14 (hind).
DY = 0.47

X = Vector((1, 0, 0))


def v(x, y, z):
    return Vector((x, y + DY, z))


# name: (head, tail, parent, connected, deform)
BONES = [
    ("Crawler_Root", v(0, -0.47, 0.0), v(0, -0.77, 0.0), None, False, False),

    ("DEF_BodyRoot", v(0, -0.20, 0.64), v(0, -0.55, 0.56), "Crawler_Root", False, True),
    ("DEF_SpineFront", v(0, -0.55, 0.56), v(0, -1.05, 0.46), "DEF_BodyRoot", True, True),
    ("DEF_HeadPlate", v(0, -1.05, 0.46), v(0, -1.50, 0.27), "DEF_SpineFront", True, True),
    ("DEF_Mouth", v(0, -1.15, 0.19), v(0, -1.57, 0.14), "DEF_HeadPlate", False, True),

    ("DEF_Tail01", v(0, 0.05, 0.69), v(0, 0.43, 0.63), "DEF_BodyRoot", False, True),
    ("DEF_Tail02", v(0, 0.43, 0.63), v(0, 0.81, 0.56), "DEF_Tail01", True, True),
    ("DEF_Tail03", v(0, 0.81, 0.56), v(0, 1.19, 0.51), "DEF_Tail02", True, True),
    ("DEF_Tail04", v(0, 1.19, 0.51), v(0, 1.61, 0.53), "DEF_Tail03", True, True),
]

# limbs, right side values; mirrored for .L
FRONT = [
    ("DEF_FrontUpper", (0.21, -0.70, 0.44), (0.31, -0.57, 0.28), "DEF_BodyRoot", False),
    ("DEF_FrontLower", (0.31, -0.57, 0.28), (0.35, -0.64, 0.17), "DEF_FrontUpper", True),
    ("DEF_FrontFoot", (0.35, -0.64, 0.17), (0.43, -0.89, 0.02), "DEF_FrontLower", True),
]
HIND = [
    ("DEF_HindThigh", (0.20, -0.17, 0.58), (0.27, -0.13, 0.40), "DEF_BodyRoot", False),
    ("DEF_HindShin", (0.27, -0.13, 0.40), (0.33, -0.03, 0.22), "DEF_HindThigh", True),
    ("DEF_HindAnkle", (0.33, -0.03, 0.22), (0.40, -0.06, 0.05), "DEF_HindShin", True),
    ("DEF_HindFoot", (0.40, -0.06, 0.05), (0.43, -0.21, 0.015), "DEF_HindAnkle", True),
]

# IK control targets sit at the tail of the bone that carries the IK constraint.
CTRLS = [
    ("CTRL_FootIK.FR", (0.35, -0.64, 0.17)),
    ("CTRL_FootIK.FL", (-0.35, -0.64, 0.17)),
    ("CTRL_FootIK.HR", (0.40, -0.06, 0.05)),
    ("CTRL_FootIK.HL", (-0.40, -0.06, 0.05)),
]


def mirror(t):
    return (-t[0], t[1], t[2])


def build_bone_table():
    table = list(BONES)
    for side, sgn in (("R", 1), ("L", -1)):
        for chain in (FRONT, HIND):
            for name, head, tail, parent, conn in chain:
                h = head if sgn > 0 else mirror(head)
                t = tail if sgn > 0 else mirror(tail)
                p = parent if parent == "DEF_BodyRoot" else parent + "." + side
                table.append((name + "." + side, v(*h), v(*t), p, conn, True))
    for name, pos in CTRLS:
        p = Vector(pos)
        table.append((name, v(p.x, p.y, p.z), v(p.x, p.y, p.z + 0.12), "Crawler_Root", False, False))
    return table


def main():
    # every clip in the spec is timed at 30 fps; Blender's scene default is 24
    # and the FBX carries that rate, which stretches each clip by 1.25x in Unity
    bpy.context.scene.render.fps = 30
    bpy.context.scene.render.fps_base = 1.0

    mesh_ob = bpy.data.objects["Mesh_0"]
    mesh_ob.name = "Crawler_Mesh"
    mesh_ob.data.name = "Crawler_Mesh"
    if mesh_ob.material_slots and mesh_ob.material_slots[0].material:
        mesh_ob.material_slots[0].material.name = "M_Crawler"

    # shift mesh so the four foot contacts straddle the origin
    for vt in mesh_ob.data.vertices:
        vt.co.y += DY

    arm_data = bpy.data.armatures.new("Crawler_Armature")
    arm = bpy.data.objects.new("Crawler_Armature", arm_data)
    bpy.context.scene.collection.objects.link(arm)

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    for name, head, tail, parent, conn, deform in build_bone_table():
        b = eb.new(name)
        b.head = head
        b.tail = tail
        b.use_deform = deform
        d = (tail - head).normalized()
        roll_vec = X.cross(d)
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
    try:
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        print("SKIN: automatic weights ok")
    except RuntimeError as e:
        print("SKIN: auto failed (%s), falling back to envelope" % e)
        bpy.ops.object.parent_set(type='ARMATURE_ENVELOPE')

    # 00-rigging.md: max 4 weights per vertex, drop anything under 0.01, normalize
    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    bpy.context.view_layer.objects.active = mesh_ob
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01, keep_single=True)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
    worst = max(sum(1 for g in vt.groups if g.weight > 0.0001) for vt in mesh_ob.data.vertices)
    print("SKIN: max weights per vertex %d" % worst)

    empty = [g.name for g in mesh_ob.vertex_groups
             if not any(g.index in [gr.group for gr in vt.groups] for vt in mesh_ob.data.vertices)]
    print("SKIN: groups=%d empty=%s" % (len(mesh_ob.vertex_groups), empty))

    # constraints
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    for side, tag in (("R", "R"), ("L", "L")):
        for bone_name, target, chain in (
                ("DEF_FrontLower." + side, "CTRL_FootIK.F" + tag, 2),
                ("DEF_HindAnkle." + side, "CTRL_FootIK.H" + tag, 3)):
            pb = arm.pose.bones[bone_name]
            c = pb.constraints.new('IK')
            c.target = arm
            c.subtarget = target
            c.chain_count = chain
            c.use_tail = True
        for foot, target in (("DEF_FrontFoot." + side, "CTRL_FootIK.F" + tag),
                             ("DEF_HindFoot." + side, "CTRL_FootIK.H" + tag)):
            pb = arm.pose.bones[foot]
            c = pb.constraints.new('COPY_ROTATION')
            c.target = arm
            c.subtarget = target
            c.target_space = 'LOCAL_WITH_PARENT'
            c.owner_space = 'LOCAL_WITH_PARENT'

    # keep the leg chains planar so IK cannot flip without pole targets
    limits = {
        "DEF_FrontUpper": (-60, 60), "DEF_FrontLower": (0, 115),
        "DEF_HindThigh": (-55, 55), "DEF_HindShin": (-130, 10), "DEF_HindAnkle": (-45, 65),
    }
    for pb in arm.pose.bones:
        base = pb.name.rsplit(".", 1)[0]
        if base in limits:
            pb.lock_ik_y = True
            pb.lock_ik_z = True
            pb.use_ik_limit_x = True
            pb.ik_min_x = math.radians(limits[base][0])
            pb.ik_max_x = math.radians(limits[base][1])
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("BONES", len(arm_data.bones), "deform",
          sum(1 for b in arm_data.bones if b.use_deform))


main()
