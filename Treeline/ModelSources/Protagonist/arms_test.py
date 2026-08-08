"""ARMS_TEST - the first-person checks in 00-rigging.md, measured and rendered.

    blender Protagonist_Arms.blend -b --python arms_test.py

Two things this rig can get wrong that the body rig could not:

  - the cut. A wedge of chest left behind, or a hole where the sleeve was
    closed, only shows from outside, so the seam gets its own shot.
  - the frame. Near clip is 0.03 m and the FOV is not settled, so the arms are
    posed into a low ready and looked at through a Unity-shaped camera. The
    pose here is rough on purpose - it is a framing test, not a grip. Grips
    come from the pose library.
"""
import bpy
import math
import os
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree

OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\armstest")

NEAR_CLIP = 0.03
FOVS = (70, 80, 90)

arm = bpy.data.objects["Arms_Armature"]
arms_mesh = bpy.data.objects["Arms_Mesh"]
PISTOL = ("W_Pistol", "W_Slide", "W_PistolMag")
SHOTGUN = ("W_Shotgun", "W_Pump", "W_TubeFill", "W_Shell")

_basis = {pb.name: pb.bone.matrix_local.to_3x3().inverted() for pb in arm.pose.bones}


def wloc(name, v):
    arm.pose.bones[name].location = _basis[name] @ Vector(v)


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


def evaluated(names):
    dg = bpy.context.evaluated_depsgraph_get()
    out = []
    for n in names:
        ob = bpy.data.objects[n]
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        out += [ob.matrix_world @ v.co for v in me.vertices]
        ev.to_mesh_clear()
    return out


def hits_centre(names):
    """Does anything in `names` sit on the crosshair? One ray straight down the
    camera axis answers it, and that is exactly the question 00-rigging.md asks."""
    dg = bpy.context.evaluated_depsgraph_get()
    for n in names:
        ob = bpy.data.objects[n]
        bvh = BVHTree.FromObject(ob, dg)
        hit = bvh.ray_cast(Vector((0, 0, 0)), Vector((0, -1, 0)))
        if hit[0] is not None:
            return (hit[0] - Vector((0, 0, 0))).length
    return None


# ---------------------------------------------------------------- rendering

os.makedirs(OUT_DIR, exist_ok=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'TEXTURE'
sc.display.shading.show_cavity = True
sc.world = sc.world or bpy.data.worlds.new("W")
sc.world.color = (0.09, 0.10, 0.12)

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam)
sc.camera = cam
target = bpy.data.objects.new("CamTarget", None)
sc.collection.objects.link(target)
track = cam.constraints.new('TRACK_TO')
track.target = target
track.track_axis = 'TRACK_NEGATIVE_Z'
track.up_axis = 'UP_Y'


def show(names):
    for ob in bpy.data.objects:
        if ob.type == 'MESH':
            ob.hide_render = ob.name not in names


def shot_outside(name, pos, tgt, scale, res=(760, 760)):
    track.mute = False
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = scale
    cam.location = pos
    target.location = tgt
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = os.path.join(OUT_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("SHOT", name)


def shot_eye(name, fov):
    """A Unity camera: at ViewModelRoot, looking -Y, vertical FOV, 16:9."""
    track.mute = True
    cam.data.type = 'PERSP'
    cam.data.sensor_fit = 'VERTICAL'
    cam.data.angle_y = math.radians(fov)
    cam.data.clip_start = NEAR_CLIP
    cam.location = (0, 0, 0)
    # X+90 alone aims a Blender camera at +Y - behind the player. The Z turn is
    # what makes it look where the model faces.
    cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    sc.render.resolution_x, sc.render.resolution_y = 960, 540
    sc.render.filepath = os.path.join(OUT_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("SHOT", name)


# ------------------------------------------------------------------- checks

print("=" * 68)
reset()

print("TEST1 bones %d  deform %d  groups on the arms mesh %d"
      % (len(arm.data.bones), sum(1 for b in arm.data.bones if b.use_deform),
         len(arms_mesh.vertex_groups)))
print("TEST1 ViewModelRoot %s  shoulder.R %s  WeaponRoot %s"
      % tuple(tuple(round(c, 3) for c in head(n))
              for n in ("ViewModelRoot", "Shoulder.R", "WeaponRoot")))

# 2. the cut. Rest pose, arms only, from outside.
show({"Arms_Mesh"})
shot_outside("01_cut_front", (0, -2.0, -0.10), (0, 0, -0.10), 1.9)
shot_outside("01_cut_back", (0, 2.0, -0.10), (0, 0, -0.10), 1.9)
shot_outside("01_cut_seam", (0.55, 0.55, 0.20), (0.20, 0.02, -0.20), 0.42)

co = evaluated(["Arms_Mesh"])
print("TEST2 cut  verts %d  x %+.3f..%+.3f  y %+.3f..%+.3f  z %+.3f..%+.3f"
      % (len(co), min(c.x for c in co), max(c.x for c in co),
         min(c.y for c in co), max(c.y for c in co),
         min(c.z for c in co), max(c.z for c in co)))
inboard = [c for c in co if abs(c.x) < 0.14]
print("TEST2 chest left behind (|x| < 0.14): %d verts%s"
      % (len(inboard),
         "" if not inboard else "  z up to %+.3f" % max(c.z for c in inboard)))

# 3. weapons at rest, parked on the right hand's grip web
for tag, names in (("pistol", PISTOL), ("shotgun", SHOTGUN)):
    show(set(names) | {"Arms_Mesh"})
    shot_outside("02_park_" + tag, (0.30, -1.2, 0.10), (0.79, 0.00, -0.25), 0.95)
    w = evaluated(names)
    print("TEST3 %-8s parked  y %+.3f..%+.3f  z %+.3f..%+.3f  tris on %d meshes"
          % (tag, min(c.y for c in w), max(c.y for c in w),
             min(c.z for c in w), max(c.z for c in w), len(names)))

# 4. low ready: both hands and the gun brought in front of the camera. Rough by
# design - what is being read is the frame, not the grip.
GUN = Vector((0.10, -0.35, -0.12))
for tag, names in (("pistol", PISTOL), ("shotgun", SHOTGUN)):
    reset()
    wloc("WeaponRoot", GUN - head("WeaponRoot"))
    wloc("CTRL_HandIK.R", Vector((0.10, -0.30, -0.13)) - head("CTRL_HandIK.R"))
    wloc("CTRL_HandIK.L", Vector((-0.02, -0.33, -0.14)) - head("CTRL_HandIK.L"))
    for side, sgn in (("R", 1), ("L", -1)):
        wrot("CTRL_HandIK." + side, (0, 0, 1), -90)
        # elbows down by the ribs, not out at shoulder height
        wloc("CTRL_ElbowPole." + side,
             Vector((sgn * 0.22, 0.30, -0.70)) - head("CTRL_ElbowPole." + side))
    bpy.context.view_layer.update()

    show(set(names) | {"Arms_Mesh"})
    seen = evaluated(["Arms_Mesh"] + list(names))
    front = [c for c in seen if c.y < 0]
    nearest = min((c.length for c in front), default=None)
    clipped = [c for c in front if c.length < NEAR_CLIP]
    centre = hits_centre(names)
    print("TEST4 %-8s nearest %.4f m  inside near clip %d verts  "
          "elbow.R %s  crosshair %s"
          % (tag, nearest, len(clipped),
             tuple(round(c, 3) for c in head("LowerArm.R")),
             "BLOCKED at %.3f m" % centre if centre else "clear"))
    for fov in FOVS:
        shot_eye("03_ready_%s_fov%d" % (tag, fov), fov)

reset()
show({o.name for o in bpy.data.objects if o.type == 'MESH'})
print("=" * 68)
