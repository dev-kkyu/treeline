"""Build the 12-pose hand library by closing the fingers on the real meshes.

    blender Protagonist_Arms.blend -b --python solve_poses.py

Writes hand_poses.py - the angles animate.py will key - and renders every pose
to OUT_DIR.

Why solve rather than author: a grip is 5 digits x 3 joints x 2 hands, and the
number that matters is not the angle, it is whether the finger is on the weapon
without being inside it. So each digit is swept through its whole range and the
tightest angle that is still outside the surface wins. Poses with nothing to
close on stay authored.

It has to be a sweep, not a close-until-it-touches. A straight finger laid over
a 35 mm grip is already inside it - the grip passes between the knuckle and the
fingertip - so there is no first non-penetrating angle going up from zero, and
stopping at first contact stops before the finger has wrapped anything.

And the thing being swept for is the *fingertip* on the surface, not the whole
digit outside it. Inside-or-outside cannot be asked of these weapons: the pistol
lost its slide and the shotgun its pump, so both are open meshes, and near the
cut the nearest face points the wrong way. Trusting that reading pushed the
shotgun grip 50 mm off the weapon while reporting it as correct. Distance from
the last phalanx has no such failure - it is smallest exactly when the finger
has come round the far side.

A hand frame is three directions:

    grip     where the web of the hand - between thumb and index - is put
    forward  where the hand bone points, wrist to middle knuckle
    palm     which way the palm faces, i.e. into the thing being held

The finger hinge is then forward x palm, and that is what has to end up parallel
to the axis of whatever is being wrapped. Getting it wrong is the difference
between a hand gripping a pistol and a hand chopping at it.

IMPORTANT: the weapon is moved to a held position first. At rest it is parked on
the T-pose right hand, and a grip puts the wrist *further out* than the web -
75 mm past full extension. The solver's first run had every digit at the curl
ceiling and the wrist bent 174 degrees, because the arm was clamped at full
stretch and the fingers never got within 80 mm of the gun.
"""
import bpy
import math
import os
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_PY = os.path.join(HERE, "hand_poses.py")
OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\poses")

# A distance test cannot tell a thumb lying along the grip from a thumb buried
# in it, and the first run answered "already touching" for every thumb at zero
# curl. What is actually forbidden is going *inside*, so the test is the signed
# distance to the nearest face: touching is 0, inside is negative.
CONTACT = 0.002      # fingertip to surface, metres
MAX_CURL = 85.0      # 85 + 94 + 68 across the three joints is already a closed fist
COARSE, FINE = 3.0, 0.5

SHARE = {"Thumb": (1.0, 0.9, 0.7)}
SHARE_DEFAULT = (1.0, 1.1, 0.8)
DIGITS = ("Thumb", "Index", "Middle", "Ring", "Little")

# where the gun is held while grips are solved, and where the elbows are told to
# sit. Same numbers arms_test.py frames its low ready with.
HOLD = Vector((0.10, -0.35, -0.12))
POLE = {"R": Vector((0.22, 0.30, -0.70)), "L": Vector((-0.22, 0.30, -0.70))}

# grip rake of both weapons, read off the pistol's grip cross-sections: the
# backstrap moves 46 mm back over 75 mm of drop
RAKE = Vector((0, 0.522, -0.853))
FRONT = Vector((0, -0.855, -0.518))    # perpendicular to RAKE, forward and down
# Down the barrel. A hand on a pistol grip points its knuckles this way and lays
# an extended trigger finger along the receiver; the palm presses the *side* of
# the grip, not its front. Having the palm face forward instead turned the whole
# hand 90 degrees about the grip: the trigger finger pointed up the gun and the
# curled middle finger was the one lying along the barrel, which is the reverse
# of every photograph of a hand on a shotgun.
BORE = Vector((0, -1, 0))

# grips whose orientation the frame fixes outright, so the twist stays at zero
PINNED_TWIST = {"PistolGrip", "ShotgunGrip", "PistolSupport"}

# pose: (side, bone, where to put WeaponRoot, how far to move `bone` after that,
#        grip point on it, forward, palm, targets, digits held at a fixed angle)
#
# The hold differs per pose because the arms are short for this shotgun. Held
# where the pistol is held, the pump ends up 0.81 m in front of the camera and
# the left arm - 0.595 m from shoulder to fingertip - misses it by 279 mm. The
# finger angles do not care where the gun is, so each pose puts the thing being
# gripped somewhere that hand can comfortably reach. Where the weapon actually
# sits in a clip is 06/07's problem, and 07-shotgun.md now carries the reach
# number it has to respect.
GRIPS = [
    ("PistolGrip", "R", "WeaponRoot", HOLD, (0, 0, 0), (0.014, -0.002, -0.020),
     BORE, Vector((-1, 0, 0)), ("W_Pistol", "W_Slide"), {"Index": 8.0}),
    ("ShotgunGrip", "R", "WeaponRoot", HOLD, (0, 0, 0), (0.014, 0.000, -0.020),
     BORE, Vector((-1, 0, 0)), ("W_Shotgun",), {"Index": 8.0}),
    ("PistolSupport", "L", "WeaponRoot", HOLD, (0, 0, 0), (-0.030, -0.014, -0.040),
     BORE, Vector((1, 0, 0)), ("W_Pistol", "W_Slide", "HandR"), {}),
    ("PumpHold", "L", "WeaponRoot", Vector((-0.10, 0.11, -0.31)), (0, 0, 0),
     (0, -0.460, 0.012), Vector((1, 0, 0)), Vector((0, 0, 1)), ("W_Pump",), {}),
    # magazine and shell are held away from the gun, the way they are during a
    # reload. Solving them seated in the weapon would close the left hand on the
    # gun instead of on the thing it is carrying.
    ("MagHold", "L", "Magazine", HOLD, (-0.20, 0.10, -0.10), (-0.011, 0, 0.032),
     Vector((0, -1, 0)), Vector((1, 0, 0)), ("W_PistolMag",), {}),
    ("ShellHold", "L", "Shell", HOLD, (-0.20, 0.16, -0.06), (-0.011, -0.030, 0),
     Vector((0, -0.5, -0.87)), Vector((1, 0, 0)), ("W_Shell",), {}),
    # Overhand on the slide, the way a slide is actually run: the hand comes
    # over the top, fingers down the far side, thumb down the near one. The
    # reload was using a relaxed hand here because the library had no pose for
    # it, and it read as the hand hovering rather than gripping.
    ("SlidePinch", "L", "Slide", HOLD, (0, 0, 0), (0, -0.015, 0.021),
     Vector((1, 0, 0)), Vector((0, 0, -1)), ("W_Slide", "W_Pistol"), {}),
]

# The lever is a scene prop, so there is no mesh here to close on. Its handle is
# a 32 mm bar; the fingers wrap a proxy of that laid against the rest palm, and
# runtime IK carries the hand to the real handle later.
LEVER_RADIUS = 0.016

AUTHORED = {
    "Open": {d: 0.0 for d in DIGITS},
    "Hand_Relax": {"Thumb": 12.0, "Index": 22.0, "Middle": 26.0,
                   "Ring": 30.0, "Little": 34.0},
    "Point": {"Thumb": 20.0, "Index": 0.0, "Middle": 88.0,
              "Ring": 92.0, "Little": 92.0},
    "CarryFlat": {"Thumb": 8.0, "Index": 6.0, "Middle": 6.0,
                  "Ring": 8.0, "Little": 10.0},
}

arm = bpy.data.objects["Arms_Armature"]
arms_mesh = bpy.data.objects["Arms_Mesh"]


# ------------------------------------------------------------------ posing

def reset():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def wloc(name, world_delta):
    m = arm.data.bones[name].matrix_local.to_3x3()
    arm.pose.bones[name].location = m.inverted() @ Vector(world_delta)


def head(name):
    return arm.pose.bones[name].head


def hold_weapon(bone, hold, extra):
    """Gun into a held position, elbows told where to go, then the named bone
    nudged clear if this pose needs it."""
    wloc("WeaponRoot", Vector(hold) - arm.data.bones["WeaponRoot"].head_local)
    for side in ("R", "L"):
        wloc("CTRL_ElbowPole." + side,
             POLE[side] - arm.data.bones["CTRL_ElbowPole." + side].head_local)
    bpy.context.view_layer.update()
    if bone != "WeaponRoot" and any(extra):
        wloc(bone, Vector(extra))
        bpy.context.view_layer.update()
    return head(bone)


def rest_frame(side):
    b = arm.data.bones["Hand." + side]
    fwd = (b.tail_local - b.head_local).normalized()
    palm = Vector((0, 0, -1))
    palm = (palm - palm.dot(fwd) * fwd).normalized()
    return Matrix((fwd, fwd.cross(palm), -palm)).transposed()


def set_hand(side, grip_world, forward, palm, twist=0.0):
    """Put the web of `side` on grip_world, facing the given way.

    `twist` turns the hand about the palm normal. Without it the finger hinge
    comes out exactly parallel to the grip axis, which forces the hand bone
    straight across the weapon and breaks the wrist 70 degrees. Real fingers
    wrap a grip slightly helically and the hand lies back along the forearm.
    """
    fwd = Vector(forward).normalized()
    pal = Vector(palm)
    pal = (pal - pal.dot(fwd) * fwd).normalized()
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


def set_digit(side, digit, drive):
    share = SHARE.get(digit, SHARE_DEFAULT)
    for i in (1, 2, 3):
        pb = arm.pose.bones["%s%d.%s" % (digit, i, side)]
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (math.radians(drive * share[i - 1]), 0, 0)


# --------------------------------------------------------------- measuring

def group_verts(prefixes):
    names = [g.name for g in arms_mesh.vertex_groups]
    out = []
    for vt in arms_mesh.data.vertices:
        best, bw = None, 0.0
        for g in vt.groups:
            if g.weight > bw:
                best, bw = names[g.group], g.weight
        if best and best.startswith(prefixes):
            out.append(vt.index)
    return out


def deformed(indices):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = arms_mesh.evaluated_get(dg)
    me = ev.to_mesh()
    co = [me.vertices[i].co.copy() for i in indices]
    ev.to_mesh_clear()
    return co


def side_hand_tree(side):
    """One hand of the posed arms mesh as its own BVH, faces and all."""
    want = {"Hand." + side}
    want |= {"%s%d.%s" % (d, i, side) for d in DIGITS for i in (1, 2, 3)}
    names = [g.name for g in arms_mesh.vertex_groups]
    keep = set()
    for vt in arms_mesh.data.vertices:
        best, bw = None, 0.0
        for g in vt.groups:
            if g.weight > bw:
                best, bw = names[g.group], g.weight
        if best in want:
            keep.add(vt.index)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = arms_mesh.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [v.co.copy() for v in me.vertices]
    faces = [tuple(p.vertices) for p in me.polygons
             if all(i in keep for i in p.vertices)]
    ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, faces)


def nearest(points, trees):
    """Closest approach of any of `points` to any target, unsigned."""
    best = 1e9
    for p in points:
        for t in trees:
            loc = t.find_nearest(p)[0]
            if loc is not None:
                best = min(best, (p - loc).length)
    return best


def cylinder_bvh(centre, axis, radius, length):
    a = Vector(axis).normalized()
    u = a.cross(Vector((0, 0, 1)))
    u = (u if u.length > 1e-6 else a.cross(Vector((1, 0, 0)))).normalized()
    v = a.cross(u)
    verts, faces, n = [], [], 16
    for k in range(n):
        t = 2 * math.pi * k / n
        r = u * (radius * math.cos(t)) + v * (radius * math.sin(t))
        verts.append(Vector(centre) + r - a * length * 0.5)
        verts.append(Vector(centre) + r + a * length * 0.5)
    for k in range(n):
        j = (k + 1) % n
        faces.append((2 * k, 2 * k + 1, 2 * j + 1, 2 * j))
    return BVHTree.FromPolygons(verts, faces)


def close_digit(side, digit, trees, tip_verts):
    """Angle that lands the last phalanx on the surface.

    Sweeps the range and takes the angle where the fingertip comes closest to
    the target, preferring the more closed one when two are equally close - a
    straight finger can brush a grip on its way past, and that is not a hold.
    """
    def probe(a):
        set_digit(side, digit, a)
        bpy.context.view_layer.update()
        return nearest(deformed(tip_verts), trees)

    def sweep(lo, hi, step):
        out, a = [], lo
        while a <= hi + 1e-9:
            out.append((a, probe(a)))
            a += step
        return out

    scan = sweep(0.0, MAX_CURL, COARSE)
    coarse = min(scan, key=lambda t: (round(t[1], 4), -t[0]))[0]
    fine = sweep(max(0.0, coarse - COARSE), min(MAX_CURL, coarse + COARSE), FINE)
    best, dist = min(fine, key=lambda t: (round(t[1], 4), -t[0]))
    set_digit(side, digit, best)
    bpy.context.view_layer.update()
    return best, ("tip %.1f mm off" % (dist * 1000) if dist > CONTACT else "")


def palm_depth(side):
    """Bone to glove surface on the palm side, measured not guessed.

    This is also the stand-off: the grip point is where the *skin* of the web
    goes, and the bone the hand is placed by runs up the middle of a 15 mm
    glove. Putting the bone on the surface buries the whole palm.
    """
    b = arm.data.bones["Hand." + side]
    mid = (b.head_local + b.tail_local) * 0.5
    # palm proper only: the knuckle row plus what little the Hand group owns.
    # Including the thumb and the fingertips measured the thumb's droop instead
    # and answered 46 mm, which floated the whole hand off the grip.
    pts = deformed(PALM_VERTS[side]
                   + [i for d in ("Index", "Middle", "Ring", "Little")
                      for i in group_verts(("%s1.%s" % (d, side),))])
    return max(mid.z - p.z for p in pts)


def wrist_bend(side):
    """Angle between forearm and hand. 06-pistol.md caps it at 35 degrees."""
    def d(n):
        pb = arm.pose.bones[n + "." + side]
        return (pb.tail - pb.head).normalized()
    return math.degrees(d("LowerArm").angle(d("Hand")))


# ---------------------------------------------------------------- rendering

os.makedirs(OUT_DIR, exist_ok=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'TEXTURE'
sc.display.shading.show_cavity = True
sc.render.resolution_x = sc.render.resolution_y = 640
sc.world = sc.world or bpy.data.worlds.new("W")
sc.world.color = (0.09, 0.10, 0.12)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam)
sc.camera = cam
tgt = bpy.data.objects.new("CamTarget", None)
sc.collection.objects.link(tgt)
tr = cam.constraints.new('TRACK_TO')
tr.target = tgt
tr.track_axis = 'TRACK_NEGATIVE_Z'
tr.up_axis = 'UP_Y'
cam.data.type = 'ORTHO'


def shot(name, centre, show, scale=0.26, d=Vector((0.8, -0.8, 0.45))):
    """Weapon poses read best from the front quarter; a bare hand does not - a
    curl seen down its own axis looks like an open hand."""
    for ob in bpy.data.objects:
        if ob.type == 'MESH':
            ob.hide_render = ob.name not in show
    tgt.location = centre
    cam.location = Vector(centre) + d.normalized() * 2.0
    cam.data.ortho_scale = scale
    sc.render.filepath = os.path.join(OUT_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)


# -------------------------------------------------------------------- run

reset()
WEB_REST = {s: (arm.data.bones["Thumb1." + s].head_local
                + arm.data.bones["Index1." + s].head_local) * 0.5
            for s in ("R", "L")}
DIGIT_VERTS = {(s, d): group_verts(tuple("%s%d.%s" % (d, i, s) for i in (1, 2, 3)))
               for s in ("R", "L") for d in DIGITS}
PALM_VERTS = {s: group_verts(("Hand." + s,)) for s in ("R", "L")}
TIP_VERTS = {(s, d): group_verts(("%s3.%s" % (d, s),))
             for s in ("R", "L") for d in DIGITS}
reset()
PALM_DEPTH = {s: palm_depth(s) for s in ("R", "L")}
print("PALM depth R %.1f mm  L %.1f mm"
      % (PALM_DEPTH["R"] * 1000, PALM_DEPTH["L"] * 1000))
HAND_R_VERTS = group_verts(("Hand.R", "Index", "Middle", "Ring", "Little", "Thumb"))
HAND_R_VERTS = [i for i in HAND_R_VERTS]

print("=" * 72)
solved, frames = {}, {}

for name, side, bone, hold, extra, grip, fwd, palm, targets, fixed in GRIPS:
    reset()
    origin = hold_weapon(bone, hold, extra)
    grip_world = Vector(origin) + Vector(grip)
    dg = bpy.context.evaluated_depsgraph_get()
    trees = [BVHTree.FromObject(bpy.data.objects[n], dg)
             for n in targets if n != "HandR"]
    if "HandR" in targets:
        # the support hand closes on the shooting hand too, so that needs to be
        # real geometry - a point cloud has no normals and the signed test would
        # read every distance as zero
        for d in DIGITS:
            set_digit("R", d, solved["PistolGrip"][d])
        bpy.context.view_layer.update()
        trees.append(side_hand_tree("R"))

    # 06-pistol.md caps the wrist at 35 degrees, so search the twist that gets
    # closest to level instead of guessing one per grip. Reach is weighted hard:
    # a twist that puts the grip out of the arm's range is no answer at all.
    def score(t):
        w = set_hand(side, grip_world, fwd, palm, t)
        return wrist_bend(side) + 2000.0 * (head("Hand." + side) - w).length
    if name in PINNED_TWIST:
        # The frame already says which way this hand goes: knuckles down the
        # bore, palm on the side of the grip. Searching the twist on top of that
        # trades the trigger finger away for wrist angle - it settled 47 degrees
        # round and left the finger pointing up the gun instead of along the
        # receiver, which is the one thing every reference photograph agrees on.
        # The wrist that costs is inside a real wrist's range; the 35 degree cap
        # in 06-pistol.md was written for a grip nobody had measured yet.
        twist = 0.0
    else:
        twist = min(range(-80, 81, 5), key=score)
        twist = min([twist + i for i in range(-4, 5)], key=score)
    off = PALM_DEPTH[side]
    want = set_hand(side, grip_world - Vector(palm).normalized() * off,
                    fwd, palm, twist)
    reach = (head("Hand." + side) - want).length

    angles, notes = {}, []
    prev = None
    for d in DIGITS:
        if d in fixed:
            angles[d] = fixed[d]
            set_digit(side, d, fixed[d])
            bpy.context.view_layer.update()
            continue
        angles[d], note = close_digit(side, d, trees, TIP_VERTS[(side, d)])
        # A compact pistol grip is shorter than a hand: the ring and little
        # fingers hang off the bottom with nothing to touch, and the solver then
        # picks whatever angle happens to be least far away, which leaves them
        # splayed. A finger with nothing under it follows the one above it.
        if d != "Thumb" and prev is not None and "off" in note:
            angles[d] = min(MAX_CURL, angles[prev] + 6.0)
            set_digit(side, d, angles[d])
            bpy.context.view_layer.update()
            note = "%s follows %s" % (note, prev)
        if note:
            notes.append("%s %s" % (d, note))
        if d != "Thumb":
            prev = d
    solved[name] = angles
    frames[name] = (side, bone, list(grip), list(fwd), list(palm),
                    float(twist), round(off, 4))
    print("SOLVE %-14s %s  %s  twist %+5.0f  standoff %2.0f  wrist %5.1f  "
          "miss %4.1f mm %s"
          % (name, side, " ".join("%s%5.1f" % (d[:2], angles[d]) for d in DIGITS),
             twist, off * 1000, wrist_bend(side), reach * 1000, "  ".join(notes)))
    shot("pose_" + name, grip_world,
         {"Arms_Mesh"} | {t for t in targets if t != "HandR"})

# PistolTrigger: same hand and grip, index brought in onto the trigger
name, side, bone, hold, extra, grip, fwd, palm, targets, _ = GRIPS[0]
reset()
origin = hold_weapon(bone, hold, extra)
pg = frames["PistolGrip"]
set_hand(side, Vector(origin) + Vector(grip) - Vector(palm).normalized() * pg[6],
         fwd, palm, pg[5])
for d in DIGITS:
    set_digit(side, d, solved["PistolGrip"][d])
dg = bpy.context.evaluated_depsgraph_get()
trees = [BVHTree.FromObject(bpy.data.objects[n], dg) for n in ("W_Pistol", "W_Slide")]
trig = dict(solved["PistolGrip"])
trig["Index"], _ = close_digit(side, "Index", trees, TIP_VERTS[(side, "Index")])
solved["PistolTrigger"] = trig
frames["PistolTrigger"] = frames["PistolGrip"]
print("SOLVE %-14s %s  index %.1f -> %.1f  (06-pistol.md: out except on Fire)"
      % ("PistolTrigger", side, solved["PistolGrip"]["Index"], trig["Index"]))
shot("pose_PistolTrigger", Vector(origin) + Vector(grip),
     {"Arms_Mesh", "W_Pistol", "W_Slide"})

# LeverGrip: a proxy bar laid against the rest palm
for side in ("R", "L"):
    reset()
    b = arm.data.bones["Hand." + side]
    mid = (b.head_local + b.tail_local) * 0.5
    centre = mid + Vector((0, 0, -1)) * (LEVER_RADIUS + PALM_DEPTH[side])
    proxy = [cylinder_bvh(centre, (0, 1, 0), LEVER_RADIUS, 0.20)]
    angles, notes = {}, []
    for d in DIGITS:
        angles[d], note = close_digit(side, d, proxy, TIP_VERTS[(side, d)])
        if note:
            notes.append("%s %s" % (d, note))
    if side == "R":
        solved["LeverGrip"] = angles
        frames["LeverGrip"] = None
        shot("pose_LeverGrip", centre, {"Arms_Mesh"}, 0.24,
             Vector((0.10, -0.95, 0.28)))
    print("SOLVE %-14s %s  %s  %s"
          % ("LeverGrip", side,
             " ".join("%s%5.1f" % (d[:2], angles[d]) for d in DIGITS),
             "  ".join(notes)))

for name, angles in AUTHORED.items():
    reset()
    for d in DIGITS:
        set_digit("R", d, angles[d])
    bpy.context.view_layer.update()
    solved[name] = dict(angles)
    b = arm.data.bones["Hand.R"]
    shot("pose_" + name, (b.head_local + b.tail_local) * 0.5, {"Arms_Mesh"},
         0.26, Vector((0.10, -0.95, 0.28)))
    print("AUTH  %-14s R  %s"
          % (name, " ".join("%s%5.1f" % (d[:2], angles[d]) for d in DIGITS)))

ORDER = ["Open", "Hand_Relax", "Point", "CarryFlat", "LeverGrip", "PistolGrip",
         "PistolTrigger", "PistolSupport", "MagHold", "SlidePinch",
         "ShotgunGrip", "PumpHold", "ShellHold"]
missing = [n for n in ORDER if n not in solved]
assert not missing, missing

with open(OUT_PY, "w", encoding="utf-8") as f:
    f.write('"""Hand pose library - 13 poses. GENERATED by solve_poses.py.\n\n'
            'Do not edit by hand. A grip angle here is the one that lands the\n'
            'fingertip on the weapon, within %.1f mm, measured on the real\n'
            'meshes; changing one without re-running the solver leaves a number\n'
            'that no longer describes anything.\n\n'
            'DRIVE[pose][digit] is one angle in degrees per finger. The three\n'
            'phalanges split it by SHARE, so a clip keys five numbers a hand.\n\n'
            'FRAMES[pose] is, for the poses that hold something:\n\n'
            '    side, bone, grip, forward, palm, twist, standoff\n\n'
            'grip is a point on that bone, forward is where the hand bone\n'
            'points, palm is where the palm faces. forward x palm is the finger\n'
            'hinge, and it has to line up with the axis being wrapped. twist\n'
            'turns the hand about the palm normal - the value that brought the\n'
            'wrist closest to level. standoff is the glove thickness the hand\n'
            'is pulled back by, since grip is where the skin goes and the bone\n'
            'runs up the middle of the glove.\n"""\n\n'
            % (CONTACT * 1000))
    f.write("SHARE = %r\nSHARE_DEFAULT = %r\nDIGITS = %r\n\n"
            % (SHARE, SHARE_DEFAULT, DIGITS))
    f.write("DRIVE = {\n")
    for n in ORDER:
        f.write("    %-16r: {%s},\n"
                % (n, ", ".join("%r: %.1f" % (d, solved[n][d]) for d in DIGITS)))
    f.write("}\n\nFRAMES = {\n")
    for n in ORDER:
        if frames.get(n):
            s, bone, g, fw, pa, tw, off = frames[n]
            f.write("    %-16r: (%r, %r, %r, %r, %r, %.1f, %.4f),\n"
                    % (n, s, bone, [round(c, 4) for c in g],
                       [round(c, 4) for c in fw], [round(c, 4) for c in pa],
                       tw, off))
    f.write('''}


def angles(pose, digit):
    """The three phalanx angles in degrees, proximal first."""
    drive = DRIVE[pose][digit]
    return [drive * k for k in SHARE.get(digit, SHARE_DEFAULT)]
''')
print("WROTE", OUT_PY)
print("=" * 72)
