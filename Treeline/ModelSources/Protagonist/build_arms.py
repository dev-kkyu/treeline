"""First-person arms rig: cut the arms out of the body mesh and give them their
own armature with the weapon bones in it.

    blender Protagonist_Rig.blend -b --python build_arms.py -- [Weapons.blend]

Follows 00-rigging.md's "1인칭 팔" section and 09-weapons.md. 46 deform bones:
UpperChest, both arms down to the fingers (39), a forearm twist each side (2),
plus WeaponRoot / Slide / Magazine / Pump / Shell (5). Generic rig, root is
ViewModelRoot.

Two things make this a separate file from the body rather than a second export
of it:

  - the origin moves to the camera. ViewModelRoot sits where CameraPivot is in
    Player.prefab, 1.65 m up, so a hand 0.4 m in front of the face is at y -0.4
    and nothing downstream has to subtract eye height.
  - the weapon is a bone, not a socket on the hand. 09-weapons.md has the
    reason: bolt it to Hand.R and the gun's motion is the wrist's motion, and
    the reload lays the gun over further than a wrist can go.

The rest pose is the body's T-pose, with the weapon parked at the right hand's
grip web. Rest is not a grip - the hand does not close on the gun here. The grips get built in the pose library, and sharing the body's rest is
what lets this file reuse the body's bone table, rolls and pre-bent elbow.

Wants Weapons.blend from prep_weapons.py; pass its path after -- if it is not
in the scratchpad. Both that file and protagonist.blend come from Meshy output
that lives outside the repository, so the committed artefact is the .blend this
script writes, not its inputs.
"""
import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_rig as B

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_BLEND = os.path.join(HERE, "Protagonist_Arms.blend")
SCRATCH = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
WEAPONS = argv[0] if argv else os.path.join(SCRATCH, "Weapons.blend")

# CameraPivot in Player.prefab. Not the eye bone at y -0.075 - the Unity camera
# sits on the head's own axis, and everything the clips are authored against is
# measured from the camera.
CAMERA = Vector((0.0, 0.0, 1.65))

# How much longer the viewmodel forearm is than the body's. Zero, and it stays
# zero unless something measured says otherwise.
#
# This exists because the shotgun looked unreachable and lengthening the arm is
# the usual viewmodel answer. It was not unreachable: the search was comparing
# the wrist against the grip web, two points 56 mm apart by construction, so
# every stance in the sweep reported the same ~53 mm miss. With that fixed, 274
# of 486 stances hold the gun two-handed at this arm length. The knob is left
# here, off, because it is the right knob if a later weapon really does not fit.
ARM_EXTENSION = 0.0

# grip web of the right hand: the notch between thumb and index, read off the
# rest hand. Both weapons have their origin on their own grip web already
# (prep_weapons.py), so one point parks both. Negative x because the model faces
# -y, so the character's right is -x - see build_rig.py's side loop.
WEB = Vector((-0.785, 0.026, 1.402))

# weapon bones, as offsets from WEB. Straight off the piece bounds printed by
# prep_weapons.py: the slide spans y +0.008..-0.177 at z 0.039, the pump
# y -0.347..-0.572 at z 0.035, a shell is 68 mm long.
WEAPON_BONES = [
    ("WeaponRoot", (0, 0, 0), (0, -0.100, 0), "ViewModelRoot"),
    ("Slide",      (0, 0.008, 0.039), (0, -0.177, 0.039), "WeaponRoot"),
    ("Magazine",   (0, -0.020, -0.083), (0, -0.020, 0.022), "WeaponRoot"),
    ("Pump",       (0, -0.347, 0.035), (0, -0.572, 0.035), "WeaponRoot"),
    ("Shell",      (0, -0.020, -0.055), (0, -0.088, -0.055), "WeaponRoot"),
]

# object in Weapons.blend -> (bone it rides, where its own origin goes)
WEAPON_MESHES = [
    ("Pistol",    "WeaponRoot", (0, 0, 0),             "M_Pistol"),
    ("Slide",     "Slide",      (0, 0, 0),             "M_Pistol"),
    ("PistolMag", "Magazine",   (0, -0.020, -0.083),   "M_PistolMag"),
    ("Shotgun",   "WeaponRoot", (0, 0, 0),             "M_Shotgun"),
    ("Pump",      "Pump",       (0, 0, 0),             "M_Shotgun"),
    ("TubeFill",  "WeaponRoot", (0, 0, 0),             "M_TubeFill"),
    ("Shell",     "Shell",      (0, -0.020, -0.055),   "M_Shell"),
]

FINGER_NAMES = ["%s%d" % (f, i) for f, _, _ in B.FINGERS for i in (1, 2, 3)]
FINGER_NAMES += ["Thumb%d" % i for i in (1, 2, 3)]
ARM_BASES = [n for n, _, _, _, _ in B.ARM] + FINGER_NAMES
ARM_GROUPS = {n + "." + s for n in ARM_BASES for s in ("L", "R")}


# ------------------------------------------------------------------ mesh work

# Inboard limit for the cut. The shoulder joint is at x 0.155, so 0.13 keeps the
# whole deltoid and nothing of the neck.
CUT_X = 0.13


def stretch_forearms(table, mesh_ob, ext):
    """Push the wrist and everything past it out along the forearm.

    The mesh is ramped rather than translated: zero at the elbow, full at the
    wrist, constant beyond. That lengthens the sleeve and carries the hand out
    rigidly, so no hand or finger proportion changes and every grip solved
    against those proportions stays valid.
    """
    if ext <= 0:
        return table
    by = {t[0]: t for t in table}
    moved = {}
    for side in ("R", "L"):
        elbow = by["LowerArm." + side][1]
        wrist = by["LowerArm." + side][2]
        axis = (wrist - elbow).normalized()
        span = (wrist - elbow).length
        moved[side] = (elbow, axis, span)

        out = []
        for name, h, t, p, c, d in table:
            if not name.endswith("." + side):
                out.append((name, h, t, p, c, d))
                continue
            base = name.rsplit(".", 1)[0]
            past = base in ("Hand", "CTRL_HandIK") or base[:-1] in (
                "Thumb", "Index", "Middle", "Ring", "Little")
            if past:
                out.append((name, h + axis * ext, t + axis * ext, p, c, d))
            elif base == "LowerArm":
                out.append((name, h, t + axis * ext, p, c, d))
            else:
                out.append((name, h, t, p, c, d))
        table = out

        for vt in mesh_ob.data.vertices:
            if (vt.co.x < 0) != (side == "R"):
                continue
            u = (vt.co - elbow).dot(axis) / span
            if u <= 0:
                continue
            vt.co += axis * (ext * min(u, 1.0))
    print("STRETCH forearms +%.0f mm" % (ext * 1000))
    return table


def cut_arms(body, keep_ratio):
    """Duplicate the body mesh and drop everything the arms do not own.

    Selecting by weight rather than by a cutting plane: the sleeve, the glove
    and the shoulder cap are all one continuous surface here, and any plane
    through it either eats the deltoid or leaves a wedge of chest hanging.

    The weight test alone is not enough. Shoulder.L/R start at x 0.025, right
    beside the neck, so automatic weights hand them the collar and part of the
    hood - 397 vertices of it, reaching 27 mm above the camera. Anything inboard
    of CUT_X goes whatever its weights say.
    """
    me = body.data.copy()
    ob = bpy.data.objects.new("Arms_Mesh", me)
    bpy.context.scene.collection.objects.link(ob)
    for g in body.vertex_groups:
        ob.vertex_groups.new(name=g.name)

    names = [g.name for g in ob.vertex_groups]
    keep = set()
    for vt in me.vertices:
        if abs(vt.co.x) < CUT_X:
            continue
        w = sum(g.weight for g in vt.groups if names[g.group] in ARM_GROUPS)
        if w >= keep_ratio:
            keep.add(vt.index)

    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    drop = [v for v in bm.verts if v.index not in keep]
    bmesh.ops.delete(bm, geom=drop, context='VERTS')

    # A per-vertex test cuts through the jacket's strap and pocket detail and
    # leaves single triangles floating beside the shoulder. Keep the two big
    # islands - one arm each - and drop the rest.
    islands, seen = [], set()
    for v in bm.verts:
        if v in seen:
            continue
        stack, comp = [v], []
        seen.add(v)
        while stack:
            cur = stack.pop()
            comp.append(cur)
            for e in cur.link_edges:
                nxt = e.other_vert(cur)
                if nxt not in seen:
                    seen.add(nxt)
                    stack.append(nxt)
        islands.append(comp)
    islands.sort(key=len, reverse=True)
    if len(islands) > 2:
        loose = [v for comp in islands[2:] for v in comp]
        print("CUT    %d islands, dropped %d loose verts from %d slivers"
              % (len(islands), len(loose), len(islands) - 2))
        bmesh.ops.delete(bm, geom=loose, context='VERTS')

    bm.to_mesh(me)
    bm.free()
    return ob


def cap_holes(ob):
    """Close the sleeve openings the cut leaves.

    New faces get a flat UV taken from the rim they close, otherwise they land
    on whatever happens to be at (0,0) in the atlas - which on this texture is
    a bright patch and reads as a hole in the arm.
    """
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    uv = bm.loops.layers.uv.active

    # A vertex where more than two boundary edges meet is a figure-eight: two
    # rims touching at a point, and nothing will fill that as one face. The left
    # sleeve came out with exactly one such vertex and stayed open. Drop those
    # vertices - it costs a triangle at a seam nobody sees, and the rim becomes
    # a plain cycle.
    pinched = 0
    while True:
        val = {}
        for e in bm.edges:
            if e.is_boundary:
                for v in e.verts:
                    val[v] = val.get(v, 0) + 1
        bad = [v for v, n in val.items() if n != 2]
        if not bad:
            break
        pinched += len(bad)
        bmesh.ops.delete(bm, geom=bad, context='VERTS')
    if pinched:
        print("CAP    removed %d pinch vertices" % pinched)

    boundary = [e for e in bm.edges if e.is_boundary]
    if not boundary:
        bm.free()
        print("CAP    nothing open")
        return 0

    # one loop at a time. holes_fill over both openings at once closed the right
    # sleeve and left the left one open, and an open sleeve is invisible from
    # every angle a test renders until the camera is inside the arm.
    groups, seen = [], set()
    for e in boundary:
        if e in seen:
            continue
        stack, comp = [e], []
        seen.add(e)
        while stack:
            cur = stack.pop()
            comp.append(cur)
            for v in cur.verts:
                for nxt in v.link_edges:
                    if nxt.is_boundary and nxt not in seen:
                        seen.add(nxt)
                        stack.append(nxt)
        groups.append(comp)

    before = set(bm.faces)
    for comp in groups:
        bmesh.ops.contextual_create(bm, geom=comp)
    new = [f for f in bm.faces if f not in before]
    if uv:
        for f in new:
            rim = [l[uv].uv.copy() for l in f.loops]
            mid = sum(rim, Vector((0, 0))) / len(rim)
            for l in f.loops:
                l[uv].uv = mid
    bmesh.ops.triangulate(bm, faces=new)
    bm.to_mesh(ob.data)
    left = sum(1 for e in bm.edges if e.is_boundary)
    bm.free()
    print("CAP    %d openings, %d boundary edges -> %d faces, %d left open"
          % (len(groups), len(boundary), len(new), left))
    assert left == 0, "an opening did not close"
    return len(new)


# The hands and the outer half of each forearm - everything the viewmodel camera
# is within half a metre of. LowerArmTwist owns exactly that outer half already,
# because its weights ramp from nothing at the elbow to all of it at the wrist.
HAND_GROUPS = {n + "." + s for n in ["Hand", "LowerArmTwist"] + FINGER_NAMES
               for s in ("L", "R")}


def smooth_hands(ob, groups, smooth=0.5):
    """Subdivide the hands, and only the hands.

    2,520 triangles is plenty for a sleeve seen from the inside and far too few
    for a knuckle the camera sits 0.2 m from - the fingers read as flat plates
    with visible corners along the silhouette. Subdividing the whole mesh would
    triple the count to fix geometry nobody gets close to, so the selection is
    every face whose vertices all belong mostly to a hand or finger group.

    `smooth` is what makes this worth doing: a plain split adds vertices in the
    same planes and changes nothing about the silhouette.
    """
    idx = {g.index for g in ob.vertex_groups if g.name in groups}
    keep = {vt.index for vt in ob.data.vertices
            if sum(g.weight for g in vt.groups if g.group in idx) > 0.5}
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bm.verts.ensure_lookup_table()
    faces = [f for f in bm.faces if all(v.index in keep for v in f.verts)]
    if not faces:
        bm.free()
        print("SMOOTH nothing selected")
        return
    before = len(bm.faces)
    edges = list({e for f in faces for e in f.edges})
    bmesh.ops.subdivide_edges(bm, edges=edges, cuts=1, smooth=smooth,
                              use_grid_fill=True)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    after = len(bm.faces)
    bm.to_mesh(ob.data)
    bm.free()
    print("SMOOTH hands %d faces subdivided, mesh %d -> %d tris"
          % (len(faces), before, after))


def spread_twist(mesh_ob, table):
    """Hand the outer half of each forearm's weight to its twist bone.

    A linear ramp - nothing at the elbow, all of it at the wrist - is what turns
    the bone's single roll into a gradient along the sleeve. The bone copies the
    hand's roll whole; the gradient lives here, in the weights.
    """
    by = {t[0]: t for t in table}
    for side in ("R", "L"):
        src = mesh_ob.vertex_groups.get("LowerArm." + side)
        if src is None:
            continue
        dst = (mesh_ob.vertex_groups.get("LowerArmTwist." + side)
               or mesh_ob.vertex_groups.new(name="LowerArmTwist." + side))
        elbow = by["LowerArm." + side][1]
        axis = (by["LowerArm." + side][2] - elbow)
        span = axis.length
        axis = axis.normalized()
        moved = 0
        for vt in mesh_ob.data.vertices:
            w = next((g.weight for g in vt.groups if g.group == src.index), 0.0)
            if w <= 0.0:
                continue
            u = min(1.0, max(0.0, (vt.co - elbow).dot(axis) / span))
            if u <= 0.0:
                continue
            dst.add([vt.index], w * u, 'REPLACE')
            src.add([vt.index], w * (1.0 - u), 'REPLACE')
            moved += 1
        print("TWIST  %s  %d verts share the roll" % (side, moved))


def prune_groups(ob, keep_names):
    for g in [g for g in ob.vertex_groups if g.name not in keep_names]:
        ob.vertex_groups.remove(g)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',
                                              lock_active=False)


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


# ------------------------------------------------------------------ armature

def arms_bone_table():
    """The body's bone table, minus the spine below UpperChest and the legs,
    plus the weapon chain. Reusing B's tables keeps the two rest poses identical
    - which is the whole point of cutting the mesh out of the body."""
    # written in body space like everything else here; the loop below subtracts
    # CAMERA from every bone, which is what lands this one on the origin
    table = [("ViewModelRoot", CAMERA, CAMERA + Vector((0, -0.15, 0)),
              None, False, False)]
    body, axes = B.build_bone_table()
    by_name = {t[0]: t for t in body}

    uc = by_name["UpperChest"]
    table.append(("UpperChest", uc[1], uc[2], "ViewModelRoot", False, True))
    for side in ("R", "L"):
        for base in ARM_BASES:
            name = base + "." + side
            n, h, t, p, c, _ = by_name[name]
            table.append((n, h, t, p, c, True))
        # Forearm twist. The hand takes the whole roll relative to the
        # forearm, so with the wrist turned 45 degrees onto a grip the sleeve
        # shears in one loop of vertices and the glove creases into facets. A
        # twist bone from mid-forearm to the wrist, copying the hand's roll,
        # spreads that turn along the sleeve instead.
        #
        # It does NOT reduce the wrist angle. Pronation does not change the
        # angle between forearm and hand - it is the skinning this fixes.
        elbow = by_name["LowerArm." + side][1]
        wrist = by_name["LowerArm." + side][2]
        table.append(("LowerArmTwist." + side, elbow.lerp(wrist, 0.5), wrist,
                      "LowerArm." + side, False, True))
        axes["LowerArmTwist." + side] = axes["LowerArm." + side]

        ctrl = "CTRL_HandIK." + side
        n, h, t, _, c, _ = by_name[ctrl]
        table.append((n, h, t, "ViewModelRoot", False, False))

        # elbow pole. The body rig does without one - its three clips never care
        # where the elbow ends up - but a viewmodel does: with the hands brought
        # to a low ready and no pole, the solver splays both elbows sideways and
        # the forearms cross the whole frame. Behind the elbow, along the
        # direction rest is already bent.
        elbow = by_name["LowerArm." + side][1]
        table.append(("CTRL_ElbowPole." + side, elbow + Vector((0, 0.30, 0)),
                      elbow + Vector((0, 0.38, 0)), "ViewModelRoot", False, False))

    for name, h, t, parent in WEAPON_BONES:
        table.append((name, WEB + Vector(h), WEB + Vector(t), parent, False, True))
    return table, axes


def solve_pole_angles(arm_ob, iks):
    """Find the pole_angle that leaves rest untouched.

    Attaching a pole target rotates the whole chain about the shoulder-wrist
    line by an arbitrary amount, and the correction depends on the bone's roll,
    which here comes from the chain plane rather than a world axis. Rather than
    derive it, sweep it: rest is the answer we already have, so pick the angle
    that reproduces it.
    """
    for side, c in iks.items():
        rest = arm_ob.data.bones["LowerArm." + side].head_local
        best = (None, 1e9)
        for step, span, centre in ((2.0, 180.0, 0.0), (0.05, 2.0, None)):
            centre = best[0] if centre is None else centre
            a = centre - span
            while a <= centre + span:
                c.pole_angle = math.radians(a)
                bpy.context.view_layer.update()
                d = (arm_ob.pose.bones["LowerArm." + side].head - rest).length
                if d < best[1]:
                    best = (a, d)
                a += step
        c.pole_angle = math.radians(best[0])
        bpy.context.view_layer.update()
        print("POLE   %-3s angle %+7.2f deg  rest elbow moves %.3f mm"
              % (side, best[0], best[1] * 1000))
        assert best[1] < 0.001, "pole target moves the rest elbow"


def main():
    sc = bpy.context.scene
    sc.render.fps = 30
    sc.render.fps_base = 1.0

    body = bpy.data.objects["Protagonist_Mesh"]
    body_arm = bpy.data.objects["Protagonist_Armature"]

    arms = cut_arms(body, keep_ratio=0.5)
    print("ARMS   %d verts, %d tris of the body's %d"
          % (len(arms.data.vertices), tri_count(arms), tri_count(body)))
    cap_holes(arms)

    table, axes = arms_bone_table()
    table = stretch_forearms(table, arms, ARM_EXTENSION)
    keep = {t[0] for t in table if t[5]}
    prune_groups(arms, keep)
    spread_twist(arms, table)
    # after spread_twist, because the selection uses LowerArmTwist's ramp to
    # find the outer half of the forearm and that group does not exist before it
    smooth_hands(arms, HAND_GROUPS)

    arm_data = bpy.data.armatures.new("Arms_Armature")
    arm_ob = bpy.data.objects.new("Arms_Armature", arm_data)
    sc.collection.objects.link(arm_ob)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm_data.edit_bones
    for name, h, t, parent, conn, deform in table:
        b = eb.new(name)
        b.head = h - CAMERA
        b.tail = t - CAMERA
        b.use_deform = deform
        d = (t - h).normalized()
        roll_vec = axes.get(name, B.X).cross(d)
        if roll_vec.length < 1e-5:
            roll_vec = Vector((0, 0, 1))
        b.align_roll(roll_vec)
        if parent:
            b.parent = eb[parent]
            b.use_connect = conn
    bpy.ops.object.mode_set(mode='OBJECT')

    n_def = sum(1 for b in arm_data.bones if b.use_deform)
    print("BONES  %d  deform %d" % (len(arm_data.bones), n_def))
    assert n_def == 46, "expected 46 deform bones, built %d" % n_def

    for vt in arms.data.vertices:
        vt.co -= CAMERA

    # weapons: each piece rides one bone rigidly, so it gets one group at 1.0
    with bpy.data.libraries.load(WEAPONS) as (src, dst):
        want = [n for n, _, _, _ in WEAPON_MESHES]
        dst.objects = [n for n in src.objects if n in want]
    loaded = {o.name: o for o in dst.objects if o}
    parts = []
    for name, bone, off, mat in WEAPON_MESHES:
        ob = loaded[name]
        sc.collection.objects.link(ob)
        shift = WEB + Vector(off) - CAMERA
        for vt in ob.data.vertices:
            vt.co += shift
        ob.location = (0, 0, 0)
        ob.name = "W_" + name
        ob.data.name = "W_" + name
        if ob.data.materials and ob.data.materials[0]:
            ob.data.materials[0].name = mat
        vg = ob.vertex_groups.new(name=bone)
        vg.add(range(len(ob.data.vertices)), 1.0, 'REPLACE')
        m = ob.modifiers.new("Armature", 'ARMATURE')
        m.object = arm_ob
        ob.parent = arm_ob
        parts.append(ob)
        print("PART   %-12s -> %-10s tris %5d  mat %s"
              % (ob.name, bone, tri_count(ob), mat))

    # arms keep the weights they were cut with, so bind without recomputing
    m = arms.modifiers.new("Armature", 'ARMATURE')
    m.object = arm_ob
    arms.parent = arm_ob

    bpy.ops.object.select_all(action='DESELECT')
    arms.select_set(True)
    bpy.context.view_layer.objects.active = arms
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_clean(group_select_mode='ALL', limit=0.01,
                                      keep_single=True)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',
                                              lock_active=False)
    used = set()
    for vt in arms.data.vertices:
        for g in vt.groups:
            if g.weight > 0.0001:
                used.add(g.group)
    empty = sorted(g.name for g in arms.vertex_groups if g.index not in used)
    print("SKIN   groups %d  empty %d %s" % (len(arms.vertex_groups), len(empty), empty))

    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode='POSE')
    iks = {}
    for side in ("R", "L"):
        c = arm_ob.pose.bones["LowerArm." + side].constraints.new('IK')
        c.target = arm_ob
        c.subtarget = "CTRL_HandIK." + side
        c.chain_count = 2
        c.use_tail = True
        c.pole_target = arm_ob
        c.pole_subtarget = "CTRL_ElbowPole." + side
        iks[side] = c
        c = arm_ob.pose.bones["Hand." + side].constraints.new('COPY_ROTATION')
        c.target = arm_ob
        c.subtarget = "CTRL_HandIK." + side
        c.target_space = 'WORLD'
        c.owner_space = 'WORLD'

        # roll only, and in local space, so the twist bone follows how far the
        # hand has turned about the forearm and nothing else
        c = arm_ob.pose.bones["LowerArmTwist." + side].constraints.new(
            'COPY_ROTATION')
        c.target = arm_ob
        c.subtarget = "Hand." + side
        c.use_x = c.use_z = False
        c.target_space = 'LOCAL'
        c.owner_space = 'LOCAL'
    for pb in arm_ob.pose.bones:
        if pb.name.rsplit(".", 1)[0] == "LowerArm":
            pb.lock_ik_y = True
            pb.lock_ik_z = True
            pb.use_ik_limit_x = True
            pb.ik_min_x = math.radians(B.IK_LIMITS["LowerArm"][0])
            pb.ik_max_x = math.radians(B.IK_LIMITS["LowerArm"][1])
    bpy.ops.object.mode_set(mode='OBJECT')
    solve_pole_angles(arm_ob, iks)

    # the body is only here as the source; do not carry it into the arms file
    for ob in (body, body_arm):
        bpy.data.objects.remove(ob, do_unlink=True)

    total = tri_count(arms) + sum(tri_count(p) for p in parts)
    print("TRIS   arms %d, weapons %d, total %d  (02-spec budget: arms 6500/8000)"
          % (tri_count(arms), total - tri_count(arms), total))
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)


if __name__ == "__main__":
    main()
