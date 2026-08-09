"""Clinger rig builder.

Runs headless:

    blender "C:\\Users\\gofla\\Downloads\\clinger.blend" -b --python build_rig.py

Follows docs/enemy-sheets/mockups/codex/clinger/final/animation/00-rigging.md:
24 deform + 2 non-deform bones, forward = -Y, Z up, 1 unit = 1 m.

Every bone position below was measured off the generated mesh (see the slice
dump in analyze.py), not read off the concept image. Where the mesh disagrees
with MODEL-SPEC.md the mesh wins and the doc carries the measured number.

Origin: the spec puts `Root` in ceiling-contact space, so the mesh is dropped
by its own height and z = 0 becomes the plane the eight hooks bite into. The
body then hangs at negative z, which is where a ceiling enemy actually lives.
Ground clips lift the whole body back over the origin through `GroundPivot`,
so on the floor the transform still sits at the feet where the agent wants it.
"""
import bpy
import math
from mathutils import Vector

OUT_BLEND = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad\Clinger_Rig.blend"

# hook tips measure z 2.510; drop that to zero so the ceiling plane is the origin
DZ = -2.510

X = Vector((1, 0, 0))
Y = Vector((0, 1, 0))

# Which world axis the roll is measured off. `align_roll(ref x dir)` leaves the
# bone's local X as `ref` projected perpendicular to the bone, and local X is
# the axis every `rot(bone, x=...)` and every IK limit here uses.
#   spine  -> X, so a positive x pitches the body forward and back
#   limbs  -> Y, so a positive x folds the joint in the limb's own plane
# Rolling the spine off Y instead (the obvious single rule) makes the torso bend
# sideways, which quietly throws the arms half a metre off centre.
ROLL_SPINE, ROLL_LIMB = X, Y


def v(x, y, z):
    return Vector((x, y, z + DZ))


def mirror(t):
    return (-t[0], t[1], t[2])


# name, head, tail, parent, connected, deform
SPINE = [
    ("Root", (0, 0, 2.510), (0, -0.30, 2.510), None, False, False),
    ("GroundPivot", (0, 0, 2.510), (0, -0.22, 2.510), "Root", False, False),

    # torso runs top (where the ceiling limbs root) down to the neck
    ("DEF_TorsoBase", (0, 0.13, 1.790), (0, 0.02, 1.400), "GroundPivot", False, True),
    ("DEF_TorsoMid", (0, 0.02, 1.400), (0, -0.06, 1.050), "DEF_TorsoBase", True, True),
    ("DEF_TorsoTip", (0, -0.06, 1.050), (0, -0.07, 0.960), "DEF_TorsoMid", True, True),
    ("DEF_Head", (0, -0.07, 0.960), (0, -0.09, 0.380), "DEF_TorsoTip", True, True),
    # hinge sits behind the mouth slit, which the render puts at z 0.49..0.61
    ("DEF_Jaw", (0, 0.02, 0.660), (0, -0.10, 0.410), "DEF_Head", False, True),

    # the swallow. Points out of the belly so a rotation pushes the front wall
    # of the torso forward instead of bending the spine.
    ("DEF_FoldBody", (0, -0.12, 1.210), (0, -0.34, 1.210), "DEF_TorsoMid", False, True),
]

# ceiling limbs, right-hand values. F = front pair (-Y), R = rear pair (+Y).
CEIL = {
    "F": [("DEF_CeilUpper", (0.100, 0.070, 1.720), (0.200, -0.045, 2.100), "DEF_TorsoBase", False),
          ("DEF_CeilLower", (0.200, -0.045, 2.100), (0.145, -0.033, 2.510), "DEF_CeilUpper", True)],
    "R": [("DEF_CeilUpper", (0.330, 0.270, 1.700), (0.435, 0.235, 2.100), "DEF_TorsoBase", False),
          ("DEF_CeilLower", (0.435, 0.235, 2.100), (0.428, 0.165, 2.510), "DEF_CeilUpper", True)],
}

# attack arms. They root at z 1.32, between TorsoMid and TorsoTip, so they hang
# off TorsoMid rather than TorsoBase - a torso that curls has to take the arms
# with it or the writhing reads as two separate creatures.
ARM = [
    ("DEF_AttackUpper", (0.340, 0.020, 1.330), (0.720, 0.020, 0.900), "DEF_TorsoMid", False),
    ("DEF_AttackFore", (0.720, 0.020, 0.900), (0.790, -0.060, 0.320), "DEF_AttackUpper", True),
    ("DEF_AttackHand", (0.790, -0.060, 0.320), (0.760, -0.140, 0.130), "DEF_AttackFore", True),
    ("DEF_DigitsGroup", (0.760, -0.140, 0.130), (0.710, -0.260, 0.020), "DEF_AttackHand", True),
]

FOLD_ARM = ("DEF_FoldArm", (0.300, 0.000, 1.360), (0.440, 0.000, 1.290), "DEF_TorsoMid", False)

# IK targets live at the tail of the bone that carries the constraint
CTRLS = [
    ("CTRL_CeilIK.RF", (0.145, -0.033, 2.510)),
    ("CTRL_CeilIK.LF", (-0.145, -0.033, 2.510)),
    ("CTRL_CeilIK.RR", (0.428, 0.165, 2.510)),
    ("CTRL_CeilIK.LR", (-0.428, 0.165, 2.510)),
    ("CTRL_AttackIK.R", (0.790, -0.060, 0.320)),
    ("CTRL_AttackIK.L", (-0.790, -0.060, 0.320)),
]


def bone_table():
    t = [(n, v(*h), v(*tl), p, c, d, ROLL_SPINE) for n, h, tl, p, c, d in SPINE]
    for side, sgn in (("R", 1), ("L", -1)):
        for tag in ("F", "R"):
            for name, head, tail, parent, conn in CEIL[tag]:
                h = head if sgn > 0 else mirror(head)
                tl = tail if sgn > 0 else mirror(tail)
                p = parent if parent.startswith("DEF_Torso") else parent + "." + side + tag
                t.append((name + "." + side + tag, v(*h), v(*tl), p, conn, True, ROLL_LIMB))
        for name, head, tail, parent, conn in ARM:
            h = head if sgn > 0 else mirror(head)
            tl = tail if sgn > 0 else mirror(tail)
            p = parent if parent.startswith("DEF_Torso") else parent + "." + side
            t.append((name + "." + side, v(*h), v(*tl), p, conn, True, ROLL_LIMB))
        name, head, tail, parent, conn = FOLD_ARM
        h = head if sgn > 0 else mirror(head)
        tl = tail if sgn > 0 else mirror(tail)
        t.append((name + "." + side, v(*h), v(*tl), parent, conn, True, ROLL_LIMB))
    for name, pos in CTRLS:
        p = Vector(pos)
        t.append((name, v(p.x, p.y, p.z), v(p.x, p.y, p.z + 0.12), "Root", False, False, ROLL_SPINE))
    return t


# IK chains: constrained bone -> (target, chain length, bend axis, min, max)
# Only the second bone of each pair is limited. Locking the root too would stop
# a shoulder swinging forward, which Scuttle needs.
# The ceiling limbs are limited to the *negative* half. Left to bend the other
# way they buckle with the elbow above the hook line, which puts 0.45 m of the
# creature inside the ceiling when it hauls itself up. Negative folds the elbows
# out and down into the Z-fold the concept sheet draws.
IK_CHAINS = [
    ("DEF_CeilLower.RF", "CTRL_CeilIK.RF", 2, (-135, -5)),
    ("DEF_CeilLower.LF", "CTRL_CeilIK.LF", 2, (-135, -5)),
    ("DEF_CeilLower.RR", "CTRL_CeilIK.RR", 2, (-135, -5)),
    ("DEF_CeilLower.LR", "CTRL_CeilIK.LR", 2, (-135, -5)),
    ("DEF_AttackFore.R", "CTRL_AttackIK.R", 2, (0, 125)),
    ("DEF_AttackFore.L", "CTRL_AttackIK.L", 2, (0, 125)),
]


def main():
    # 30 fps everywhere. Blender defaults to 24 and the FBX carries the rate,
    # which stretches every clip by 1.25x on the way into Unity.
    sc = bpy.context.scene
    sc.render.fps = 30
    sc.render.fps_base = 1.0

    mesh_ob = bpy.data.objects["Mesh_0"]
    mesh_ob.name = "Clinger_Mesh"
    mesh_ob.data.name = "Clinger_Mesh"
    if mesh_ob.material_slots and mesh_ob.material_slots[0].material:
        mesh_ob.material_slots[0].material.name = "M_Clinger"

    for vt in mesh_ob.data.vertices:
        vt.co.z += DZ

    arm_data = bpy.data.armatures.new("Clinger_Armature")
    arm = bpy.data.objects.new("Clinger_Armature", arm_data)
    sc.collection.objects.link(arm)

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    for name, head, tail, parent, conn, deform, ref in bone_table():
        b = eb.new(name)
        b.head = head
        b.tail = tail
        b.use_deform = deform
        # roll so local X is the main bend axis (see ROLL_SPINE / ROLL_LIMB)
        d = (tail - head).normalized()
        roll_vec = ref.cross(d)
        if roll_vec.length < 1e-5:
            roll_vec = Vector((0, 0, 1))
        b.align_roll(roll_vec)
        if parent:
            b.parent = eb[parent]
            b.use_connect = conn
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    bpy.context.view_layer.objects.active = mesh_ob
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01, keep_single=True)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
    worst = max(sum(1 for g in vt.groups if g.weight > 0.0001)
                for vt in mesh_ob.data.vertices)
    empty = [g.name for g in mesh_ob.vertex_groups
             if not any(g.index in [gr.group for gr in vt.groups]
                        for vt in mesh_ob.data.vertices)]
    print("SKIN: max weights/vertex %d  groups %d  empty %s"
          % (worst, len(mesh_ob.vertex_groups), empty))

    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    for bone, target, chain, (lo, hi) in IK_CHAINS:
        # `.L` frames are related to `.R` by a flip of local X, so a limit range
        # copied across unmirrored lets the two sides fold to different depths.
        if bone.rsplit(".", 1)[1].startswith("L"):
            lo, hi = -hi, -lo
        pb = arm.pose.bones[bone]
        c = pb.constraints.new('IK')
        c.target = arm
        c.subtarget = target
        c.chain_count = chain
        c.use_tail = True
        c.influence = 0.0          # every clip keys this on when it needs planting
        pb.lock_ik_y = True
        pb.lock_ik_z = True
        pb.use_ik_limit_x = True
        pb.ik_min_x = math.radians(lo)
        pb.ik_max_x = math.radians(hi)
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("BONES %d  deform %d"
          % (len(arm_data.bones), sum(1 for b in arm_data.bones if b.use_deform)))
    co = [mesh_ob.matrix_world @ vt.co for vt in mesh_ob.data.vertices]
    print("MESH  x %+.3f..%+.3f  y %+.3f..%+.3f  z %+.3f..%+.3f"
          % (min(c.x for c in co), max(c.x for c in co),
             min(c.y for c in co), max(c.y for c in co),
             min(c.z for c in co), max(c.z for c in co)))


main()
