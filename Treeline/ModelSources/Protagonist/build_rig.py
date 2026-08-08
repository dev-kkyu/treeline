"""Protagonist rig builder.

Runs headless:
    blender protagonist.blend -b --python build_rig.py

Follows docs/character-sheets/mockups/codex/final/animation/00-rigging.md:
56 export bones (Root plus 55 Humanoid deform bones), Blender Z-up, 1 unit = 1 m,
model forward -Y, feet already on z = 0 so no origin shift is needed.

Every coordinate below was measured off the mesh, not taken from the spec table:
the generated model is 1.8200 m tall with a 0.52 m shoulder-to-wrist, which is
shorter than MODEL-SPEC.md's 0.61 m. See MODEL-SPEC.md section 1-1 - the model
is right and the spec value was optimistic, so the bones follow the mesh.

The control rig is four IK targets, one per limb, laid exactly over the last
bone of that limb. Everything else in the chain is keyed FK by animate.py. This
is the same arrangement the Colossus rig uses and it is here for the same
reason: extra control bones would all be baked away on export anyway, and a
reverse-foot control is the one thing that cannot be recovered afterwards.
"""
import bpy
import math
import os
from mathutils import Vector
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_BLEND = os.path.join(HERE, "Protagonist_Rig.blend")

X = Vector((1, 0, 0))
Y = Vector((0, 1, 0))
# the thumb folds across the palm, not along it, so its hinge is not world Y
THUMB_AXIS = Vector((0.0, 0.40, 0.92)).normalized()

# ---------------------------------------------------------------- bone table
# (name, head, tail, parent, connected)
# Centre chain. Root is the only non-deform bone in the export set.
SPINE = [
    ("Hips",       (0, 0.005, 0.995), (0, 0.000, 1.085), "Root", False),
    ("Spine",      (0, 0.000, 1.085), (0, -0.005, 1.190), "Hips", True),
    ("Chest",      (0, -0.005, 1.190), (0, -0.005, 1.310), "Spine", True),
    ("UpperChest", (0, -0.005, 1.310), (0, 0.010, 1.520), "Chest", True),
    ("Neck",       (0, 0.010, 1.520), (0, 0.015, 1.625), "UpperChest", True),
    ("Head",       (0, 0.015, 1.625), (0, 0.015, 1.790), "Neck", True),
    ("Jaw",        (0, -0.010, 1.645), (0, -0.095, 1.612), "Head", False),
]

# Right-side values; the left side is these mirrored in x.
# Shoulder joint x 0.155 sits inboard of the deltoid surface at 0.22. Elbow x
# 0.470 is where the sleeve is narrowest between 0.42 and 0.52. Wrist x 0.750 is
# the narrow band before the palm widens again.
ARM = [
    ("Shoulder", (0.025, -0.020, 1.480), (0.155, 0.070, 1.452), "UpperChest", False),
    ("UpperArm", (0.155, 0.070, 1.452), (0.470, 0.086, 1.442), "Shoulder", True),
    ("LowerArm", (0.470, 0.086, 1.442), (0.750, 0.066, 1.420), "UpperArm", True),
    ("Hand",     (0.750, 0.066, 1.420), (0.830, 0.058, 1.412), "LowerArm", True),
]

# Fingertips and finger axes come from cutting the hand mesh at x 0.875 and
# letting connectivity separate it: four components at y 0.015 / 0.054 / 0.097 /
# 0.132 with tips at x 0.899 / 0.916 / 0.907 / 0.884. The thumb never separates,
# so it was read off the palm-side render instead. Phalanx split is the usual
# 45 / 30 / 25 of the finger's length.
#
# A finger is not parallel to the palm: the knuckle sits about 17 mm higher than
# the fingertip, because the hand is thick at the knuckle line and thin at the
# end. Running each finger flat at its tip height put every knuckle outside the
# mesh. The knuckle points below are slab centroids at x 0.820 and x 0.830.
FINGERS = [
    # name, knuckle, tip
    ("Index",  (0.826, 0.026, 1.418), (0.899, 0.020, 1.414)),
    ("Middle", (0.828, 0.054, 1.428), (0.916, 0.056, 1.411)),
    ("Ring",   (0.826, 0.090, 1.426), (0.907, 0.095, 1.408)),
    ("Little", (0.822, 0.122, 1.424), (0.884, 0.130, 1.408)),
]
THUMB = [(0.745, 0.030, 1.400), (0.776, 0.010, 1.390),
         (0.798, -0.001, 1.383), (0.816, -0.008, 1.378)]

# Hip joint z 0.930 is 0.51 x stature; knee 0.500 is 0.285 x stature. Both land
# inside the mesh, checked by build_rig's inside() report. The knee is pushed
# 20 mm forward of the hip-ankle line and the elbow 16 mm back of the
# shoulder-wrist line, which is what gives the solver a side to fold towards.
LEG = [
    ("UpperLeg", (0.095, 0.010, 0.930), (0.125, 0.000, 0.500), "Hips", False),
    ("LowerLeg", (0.125, 0.000, 0.500), (0.150, 0.030, 0.090), "UpperLeg", True),
    ("Foot",     (0.150, 0.030, 0.090), (0.163, -0.095, 0.032), "LowerLeg", True),
    ("Toes",     (0.163, -0.095, 0.032), (0.170, -0.158, 0.026), "Foot", True),
]
EYE = ("Eye", (0.032, -0.075, 1.660), (0.032, -0.115, 1.660), "Head", False)

CENTRE_PARENTS = {"Root", "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head"}

# Rest already folds the elbow 5 degrees and the knee 3 degrees in the direction
# the joint is meant to close, so a positive rotation about the chain-plane axis
# always means "fold further". Letting either straighten past a few degrees below
# rest is what lets an IK chain flip through.
IK_LIMITS = {"LowerArm": (-8, 145), "LowerLeg": (-5, 145)}


def mirror(t):
    return (-t[0], t[1], t[2])


def mirror_axis(v):
    return Vector((v.x, -v.y, -v.z))


def chain_axis(p0, p1, p2):
    """Normal of the plane two bones span - the joint's real hinge axis.

    Taking it from the geometry rather than from a world axis is what keeps the
    elbow and the knee bending on the axis they are already bent on, so the sign
    of a fold reads the same on both sides.
    """
    u1 = (Vector(p1) - Vector(p0)).normalized()
    u2 = (Vector(p2) - Vector(p1)).normalized()
    return u1.cross(u2).normalized()


def finger_bones(name, knuckle, tip):
    """Three phalanges along the knuckle-to-tip line, split 45 / 30 / 25."""
    a, b = Vector(knuckle), Vector(tip)
    pts = [a.lerp(b, t) for t in (0.0, 0.45, 0.75, 1.0)]
    return [("%s%d" % (name, i + 1), pts[i], pts[i + 1],
             "Hand" if i == 0 else "%s%d" % (name, i), i > 0)
            for i in range(3)]


def build_bone_table():
    """Returns (name, head, tail, parent, connected, deform) and the roll axes."""
    table = [("Root", Vector((0, 0, 0)), Vector((0, -0.30, 0)), None, False, False)]
    axes = {}
    for name, h, t, p, c in SPINE:
        table.append((name, Vector(h), Vector(t), p, c, True))

    for side, sgn in (("R", 1), ("L", -1)):
        m = (lambda t: t) if sgn > 0 else mirror
        arm_ax = chain_axis(m(ARM[1][1]), m(ARM[2][1]), m(ARM[3][1]))
        leg_ax = chain_axis(m(LEG[0][1]), m(LEG[1][1]), m(LEG[2][1]))
        fin_ax = Y if sgn > 0 else mirror_axis(Y)
        thu_ax = THUMB_AXIS if sgn > 0 else mirror_axis(THUMB_AXIS)

        chains = list(ARM) + list(LEG) + [EYE]
        for name, h, t, p, c in chains:
            parent = p if p in CENTRE_PARENTS else p + "." + side
            table.append((name + "." + side, Vector(m(h)), Vector(m(t)),
                          parent, c, True))
        for base, ax in (("UpperArm", arm_ax), ("LowerArm", arm_ax),
                         ("Hand", arm_ax), ("UpperLeg", leg_ax),
                         ("LowerLeg", leg_ax)):
            axes[base + "." + side] = ax

        for fname, knuckle, tip in FINGERS:
            for name, h, t, p, c in finger_bones(fname, knuckle, tip):
                table.append((name + "." + side, Vector(m(h)), Vector(m(t)),
                              p + "." + side, c, True))
                axes[name + "." + side] = fin_ax
        for i in range(3):
            name = "Thumb%d" % (i + 1)
            parent = "Hand." + side if i == 0 else "Thumb%d.%s" % (i, side)
            table.append((name + "." + side, Vector(m(THUMB[i])),
                          Vector(m(THUMB[i + 1])), parent, i > 0, True))
            axes[name + "." + side] = thu_ax

        # control: laid exactly over the last bone of each limb, so the same
        # control both aims the IK chain and hands that bone a world rotation.
        # Same roll too - COPY_ROTATION is in world space, so a control whose
        # roll differs from its bone snaps that bone the moment the rig loads.
        for ctrl, src in (("CTRL_HandIK", ARM[3]), ("CTRL_FootIK", LEG[2])):
            name = ctrl + "." + side
            table.append((name, Vector(m(src[1])), Vector(m(src[2])),
                          "Root", False, False))
            axes[name] = axes.get(src[0] + "." + side, X)
    return table, axes


# -------------------------------------------------------------------- skinning

def strip_cross_side(mesh_ob, limbs):
    """Automatic weights let the left leg pick up weight from the right one where
    the thighs almost touch. 00-rigging.md forbids cross-side weights."""
    groups = {g.index: g.name for g in mesh_ob.vertex_groups}
    removed = 0
    for vt in mesh_ob.data.vertices:
        side = "R" if vt.co.x > 0.03 else ("L" if vt.co.x < -0.03 else None)
        if side is None:
            continue
        wrong = ".L" if side == "R" else ".R"
        for g in list(vt.groups):
            name = groups[g.group]
            if name.endswith(wrong) and any(k in name for k in limbs) and g.weight > 0:
                mesh_ob.vertex_groups[name].remove([vt.index])
                removed += 1
    print("SKIN cross-side weights removed %d" % removed)


# Five directions, majority vote: one ray can graze an edge and miscount.
PARITY_DIRS = [Vector(d).normalized() for d in
               ((1, 0.13, 0.07), (-1, 0.05, -0.11), (0.09, 1, 0.06),
                (0.03, -1, 0.12), (0.11, 0.04, 1))]


def report_inside(mesh_ob, arm_ob):
    """Every joint has to sit inside the mesh or the skin will pull the wrong way.

    Counting ray crossings rather than reading closest_point_on_mesh's normal:
    the generated boots have a few inward-facing faces, and the normal test calls
    the toe joint 14 mm outside the mesh when it is 20 mm inside it.
    """
    bvh = BVHTree.FromObject(mesh_ob, bpy.context.evaluated_depsgraph_get())

    def inside(p):
        votes = 0
        for d in PARITY_DIRS:
            n, o = 0, p.copy()
            while True:
                hit = bvh.ray_cast(o, d)
                if hit[0] is None:
                    break
                n += 1
                o = hit[0] + d * 1e-5
            votes += n % 2
        return votes >= 3

    joints = [b for b in arm_ob.data.bones if b.use_deform]
    out = []
    for b in joints:
        p = b.head_local
        if not inside(p):
            _, loc, _, _ = mesh_ob.closest_point_on_mesh(p)
            out.append(((p - loc).length, b.name))
    out.sort(reverse=True)
    print("INSIDE %d/%d joints outside the mesh" % (len(out), len(joints)))
    for dist, name in out[:8]:
        print("   %-16s %.4f m out" % (name, dist))


def main():
    # every clip is timed at 30 fps; Blender defaults to 24 and the FBX carries
    # that rate, which stretches each clip by 1.25x in Unity
    sc = bpy.context.scene
    sc.render.fps = 30
    sc.render.fps_base = 1.0

    mesh_ob = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    mesh_ob.name = "Protagonist_Mesh"
    mesh_ob.data.name = "Protagonist_Mesh"
    if mesh_ob.material_slots and mesh_ob.material_slots[0].material:
        mesh_ob.material_slots[0].material.name = "M_Protagonist"

    co = [mesh_ob.matrix_world @ v.co for v in mesh_ob.data.vertices]
    print("MESH %s verts %d  height %.4f  span %.4f"
          % (mesh_ob.name, len(co), max(c.z for c in co),
             max(c.x for c in co) - min(c.x for c in co)))

    table, axes = build_bone_table()

    arm_data = bpy.data.armatures.new("Protagonist_Armature")
    arm_ob = bpy.data.objects.new("Protagonist_Armature", arm_data)
    sc.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    for name, head, tail, parent, conn, deform in table:
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

    n_def = sum(1 for b in arm_data.bones if b.use_deform)
    print("BONES %d  deform %d  control %d"
          % (len(arm_data.bones), n_def,
             sum(1 for b in arm_data.bones if b.name.startswith("CTRL_"))))
    assert n_def == 55, "00-rigging.md asks for 55 deform bones, built %d" % n_def

    report_inside(mesh_ob, arm_ob)

    # skin
    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    arm_ob.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print("SKIN automatic weights ok")

    bpy.ops.object.select_all(action='DESELECT')
    mesh_ob.select_set(True)
    bpy.context.view_layer.objects.active = mesh_ob
    strip_cross_side(mesh_ob, ("UpperArm", "LowerArm", "Hand", "Shoulder",
                               "UpperLeg", "LowerLeg", "Foot", "Toes",
                               "Thumb", "Index", "Middle", "Ring", "Little"))

    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01,
                                      keep_single=True)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',
                                              lock_active=False)

    worst = max(sum(1 for g in vt.groups if g.weight > 0.0001)
                for vt in mesh_ob.data.vertices)
    used = set()
    for vt in mesh_ob.data.vertices:
        for g in vt.groups:
            if g.weight > 0.0001:
                used.add(g.group)
    empty = sorted(g.name for g in mesh_ob.vertex_groups if g.index not in used)
    print("SKIN max weights per vertex %d  groups %d  empty %d %s"
          % (worst, len(mesh_ob.vertex_groups), len(empty), empty))

    # constraints: two-bone IK on the forearm and the shin, and the last bone of
    # each limb copies the control's world rotation
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode='POSE')
    for side in ("R", "L"):
        for bone, ctrl in (("LowerArm." + side, "CTRL_HandIK." + side),
                           ("LowerLeg." + side, "CTRL_FootIK." + side)):
            c = arm_ob.pose.bones[bone].constraints.new('IK')
            c.target = arm_ob
            c.subtarget = ctrl
            c.chain_count = 2
            c.use_tail = True
        for bone, ctrl in (("Hand." + side, "CTRL_HandIK." + side),
                           ("Foot." + side, "CTRL_FootIK." + side)):
            c = arm_ob.pose.bones[bone].constraints.new('COPY_ROTATION')
            c.target = arm_ob
            c.subtarget = ctrl
            c.target_space = 'WORLD'
            c.owner_space = 'WORLD'

    for pb in arm_ob.pose.bones:
        base = pb.name.rsplit(".", 1)[0]
        if base in IK_LIMITS:
            pb.lock_ik_y = True
            pb.lock_ik_z = True
            pb.use_ik_limit_x = True
            pb.ik_min_x = math.radians(IK_LIMITS[base][0])
            pb.ik_max_x = math.radians(IK_LIMITS[base][1])
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)


# build_arms.py imports the bone tables from here, so nothing may run on import
if __name__ == "__main__":
    main()
