"""Full-body clips for the protagonist.

    blender Protagonist_Rig.blend -b --python animate_body.py

Writes Protagonist_Body_Anim.blend with the actions baked onto the deform
bones, and renders a frame strip of each.

README.md cut the body down to three clips - shadow Idle, shadow Walk, and the
ending's cable-car ride - because the player's body is never on screen. That was
true when it was written. The execution changed it: `12-grab-execute.md` locks
the camera to `GrabAnchor` between the Colossus's hands for 4.4 seconds, and for
all of that time the player can look down at a body that does not exist. So
there is a fourth clip here, `Body_Grabbed`.

IMPORTANT: `Body_Grabbed` does not co-animate with the Colossus. It cannot - the
two rigs live in different files and the monster's hands are already baked. What
makes that unnecessary is that `GrabAnchor` is a single transform on the
monster's hand, so parenting the body to it puts the body wherever the hand is,
for free. This clip therefore owes only the part the anchor cannot give: the
struggle, and the moment it stops.

The rig is 00-rigging.md's: IK on both forearms and both shins with the last
bone of each limb copying its control's world rotation, and no elbow or knee
pole - the body rig does without one because its clips never care which way the
elbow points. Feet are placed by their IK targets, which is what keeps them on
the floor while the hips move.
"""
import bpy
import math
import os
import sys
from mathutils import Vector, Matrix, Euler

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_arms as BA          # for leave_openable only

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_BLEND = os.path.join(HERE, "Protagonist_Body_Anim.blend")
OUT_DIR = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450"
           r"\scratchpad\body")

arm = bpy.data.objects["Protagonist_Armature"]
mesh = bpy.data.objects["Protagonist_Mesh"]

WALK_SPEED = 2.8         # PlayerMotor.walkSpeed, m/s
FPS = 30

CTRLS = ["CTRL_HandIK.R", "CTRL_HandIK.L", "CTRL_FootIK.R", "CTRL_FootIK.L"]
SPINE = ["Hips", "Spine", "Chest", "UpperChest", "Neck", "Head"]
ARMS = [n + "." + s for n in ("Shoulder", "UpperArm") for s in ("R", "L")]
LEGS = [n + "." + s for n in ("UpperLeg",) for s in ("R", "L")]
KEYED = CTRLS + SPINE + ARMS + LEGS


# ------------------------------------------------------------------ posing

def rest(name):
    return arm.data.bones[name].head_local


def reset_pose():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def wloc(name, world_delta):
    m = arm.data.bones[name].matrix_local.to_3x3()
    arm.pose.bones[name].location = m.inverted() @ Vector(world_delta)


def wrot(name, rot3):
    m = arm.data.bones[name].matrix_local.to_3x3()
    arm.pose.bones[name].rotation_quaternion = (
        m.inverted() @ rot3 @ m).to_quaternion()


def rot(x=0.0, y=0.0, z=0.0):
    """A world rotation from degrees about the world axes, x first."""
    return Euler((math.radians(x), math.radians(y), math.radians(z)),
                 'XYZ').to_matrix()


def place(name, world_pos, rot3=None):
    wloc(name, Vector(world_pos) - rest(name))
    if rot3 is not None:
        wrot(name, rot3)


def spine(bend=0.0, twist=0.0, lean=0.0, spread=None):
    """Share a bend down the chain, so nothing hinges at one joint.

    Real spines curve. Putting the whole of a lean into Hips gives a plank that
    pivots at the waist, which reads as a mannequin on a stick.
    """
    spread = spread or (0.15, 0.25, 0.35, 0.25)
    for name, w in zip(("Hips", "Spine", "Chest", "UpperChest"), spread):
        wrot(name, rot(x=bend * w, y=twist * w, z=lean * w))


_last_quat = {}


def key(frame):
    for name in KEYED:
        pb = arm.pose.bones[name]
        pb.keyframe_insert("location", frame=frame)
        prev = _last_quat.get(name)
        if prev is not None and pb.rotation_quaternion.dot(prev) < 0:
            pb.rotation_quaternion = -pb.rotation_quaternion
        _last_quat[name] = pb.rotation_quaternion.copy()
        pb.keyframe_insert("rotation_quaternion", frame=frame)


def snapshot():
    return {n: (arm.pose.bones[n].location.copy(),
                arm.pose.bones[n].rotation_quaternion.copy()) for n in KEYED}


def replay(snap, frame):
    for n, (loc, q) in snap.items():
        arm.pose.bones[n].location = loc
        arm.pose.bones[n].rotation_quaternion = q
    bpy.context.view_layer.update()
    key(frame)


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    _last_quat.clear()
    return act


def action_fcurves(act):
    if hasattr(act, "fcurves") and len(act.fcurves):
        return list(act.fcurves)
    out = []
    for layer in getattr(act, "layers", []):
        for strip in layer.strips:
            for cb in strip.channelbags:
                out.extend(cb.fcurves)
    return out


# ------------------------------------------------------- stance and gaits

HIPS0 = rest("Hips")
FOOT0 = {s: rest("Foot." + s) for s in ("R", "L")}
HAND0 = {s: rest("CTRL_HandIK." + s) for s in ("R", "L")}
# Feet come in under the body from the T-pose's 0.15 m stance to a standing one.
STANCE = 0.105


def stand(hip_lift=0.0, hip_shift=0.0, arm_in=(0.0, 0.0)):
    """The pose everything else is a departure from.

    The T-pose has the arms out sideways and the feet wide; nothing here is that
    shape. Bringing them in once, in one place, keeps every clip below from
    repeating the same twenty numbers.
    """
    for i, side in enumerate(("R", "L")):
        sgn = -1.0 if side == "R" else 1.0
        place("CTRL_FootIK." + side,
              Vector((sgn * STANCE, FOOT0[side].y, FOOT0[side].z)),
              rot(x=-2.0))
        # arms down by the sides: the shoulder drops and the elbow follows the
        # hand IK, so only the hand target and a little shoulder droop are set
        place("CTRL_HandIK." + side,
              Vector((sgn * 0.215, 0.045 + arm_in[i], HIPS0.z - 0.075)),
              rot(x=8.0, y=sgn * 74.0))
        wrot("Shoulder." + side, rot(y=sgn * -6.0))
    wloc("Hips", Vector((hip_shift, 0, hip_lift)))
    bpy.context.view_layer.update()


def build_idle():
    """90f loop. A shadow that never moves reads as a prop, so it breathes and
    shifts its weight once - slowly enough that the loop point does not show."""
    act = new_action("Body_Idle")
    reset_pose()
    first = None
    for f in range(1, 90, 6):
        t = 2 * math.pi * (f - 1) / 90.0
        stand(hip_lift=0.006 * math.sin(2 * t), hip_shift=0.012 * math.sin(t),
              arm_in=(0.010 * math.sin(t), -0.010 * math.sin(t)))
        spine(bend=1.2 * math.sin(2 * t), lean=-1.5 * math.sin(t))
        wrot("Head", rot(z=-2.0 * math.sin(t), x=1.0))
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 90)
    act.frame_start, act.frame_end = 1, 90
    return act


def build_walk():
    """30f loop. PlayerMotor.walkSpeed is 2.8 m/s, so one cycle covers 2.8 m.

    That is two 1.40 m steps a second - which is a jog, not a stroll, and the
    clip has to look like one or the feet will skate. Root motion is off, so the
    only thing the stride length buys is that the planted foot stays put
    relative to the hips for the half cycle it is on the ground.
    """
    act = new_action("Body_Walk")
    reset_pose()
    stride = WALK_SPEED * 30.0 / FPS / 2.0      # per step
    first = None
    for f in range(1, 30, 2):
        u = (f - 1) / 30.0
        for side in ("R", "L"):
            sgn = -1.0 if side == "R" else 1.0
            p = (u + (0.0 if side == "R" else 0.5)) % 1.0
            if p < 0.5:                      # stance: planted, sliding back
                y = stride * (0.5 - p * 2.0) * 0.5
                z = 0.0
                pitch = -14.0 * (0.5 - p) * 2
            else:                            # swing: through the air, forward
                q = (p - 0.5) * 2.0
                y = stride * (q - 0.5) * 0.5
                z = 0.085 * math.sin(math.pi * q)
                pitch = 16.0 * math.sin(math.pi * q) - 8.0
            place("CTRL_FootIK." + side,
                  Vector((sgn * STANCE * 0.8, FOOT0[side].y - y,
                          FOOT0[side].z + z)), rot(x=pitch))
            # arms swing opposite the leg on the same side
            swing = -math.sin(2 * math.pi * p)
            place("CTRL_HandIK." + side,
                  Vector((sgn * 0.215, 0.045 - 0.16 * swing,
                          HIPS0.z - 0.075 + 0.03 * abs(swing))),
                  rot(x=8.0 + 22.0 * swing, y=sgn * 74.0))
            wrot("Shoulder." + side, rot(y=sgn * -6.0, x=6.0 * swing))
        bob = 0.022 * math.cos(4 * math.pi * u)
        wloc("Hips", Vector((0.018 * math.sin(2 * math.pi * u), 0, bob)))
        spine(bend=4.0, twist=6.0 * math.sin(2 * math.pi * u),
              lean=2.0 * math.sin(2 * math.pi * u))
        wrot("Head", rot(y=-6.0 * math.sin(2 * math.pi * u)))
        bpy.context.view_layer.update()
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 30)
    act.frame_start, act.frame_end = 1, 30
    return act


def build_cablecar():
    """60f loop. The ending, seen from far away as the cable car pulls out.

    Standing, one hand up on the car's rail, riding the sway. Nothing here is
    read closely - what it has to sell at that distance is that the shape is a
    person standing rather than a statue being carried.
    """
    act = new_action("Body_CableCarRide")
    reset_pose()
    first = None
    for f in range(1, 60, 4):
        t = 2 * math.pi * (f - 1) / 60.0
        sway = math.sin(t)
        stand(hip_lift=0.008 * math.sin(2 * t), hip_shift=0.022 * sway)
        # the left hand goes up to a rail overhead; the right stays down
        # overhead, not chest height: the shoulder is already at 1.45 m and the
        # arm is 0.60 m, so a target 0.52 m above the hips is inside the reach
        # and the elbow just folds - the first pass read as both arms hanging.
        place("CTRL_HandIK.L",
              Vector((0.150, 0.02, HIPS0.z + 0.95 + 0.01 * sway)),
              rot(x=-8.0, y=100.0))
        wrot("Shoulder.L", rot(y=34.0))
        spine(bend=-2.0 + 1.0 * math.sin(2 * t), lean=-3.5 * sway,
              twist=2.0 * sway)
        wrot("Head", rot(z=-4.0 * sway, x=-3.0))
        bpy.context.view_layer.update()
        key(f)
        if first is None:
            first = snapshot()
    replay(first, 60)
    act.frame_start, act.frame_end = 1, 60
    return act


def build_grabbed():
    """132f. Frame for frame with `Colossus_Grab_Execute` (12-grab-execute.md).

    Every beat below is that document's, so the two clips can be started on the
    same frame and left alone:

        8   caught        20  wrapped        40  lifted clear of the ground
        48  held still    56  snatched in    64  face to face
        76  jaw opens     90  KILL           104 shaken       120 released

    The body is parented to `GrabAnchor`, so none of the carrying is in here -
    what is in here is the struggling, and the frame it stops. That is the whole
    reason this clip is authorable at all without the monster in the file.
    """
    act = new_action("Body_Grabbed")
    reset_pose()

    def beat(f):
        """How hard the body is fighting, 0..1, and how dead it is, 0..1."""
        if f <= 8:
            return 0.15 * (f / 8.0), 0.0
        if f <= 40:
            return 0.15 + 0.85 * min(1.0, (f - 8) / 22.0), 0.0
        if f <= 56:
            return 1.0, 0.0
        if f <= 90:
            return max(0.35, 1.0 - (f - 56) / 40.0), 0.0
        return 0.0, min(1.0, (f - 90) / 14.0)

    for f in range(1, 133, 2):
        fight, dead = beat(f)
        # two kicks a second while there is anything left to kick with
        k = math.sin(2 * math.pi * f / 15.0) * fight
        j = math.sin(2 * math.pi * f / 11.0 + 1.1) * fight
        limp = 1.0 - dead
        for side in ("R", "L"):
            sgn = -1.0 if side == "R" else 1.0
            s = k if side == "R" else -k
            place("CTRL_FootIK." + side,
                  Vector((sgn * (STANCE + 0.06 * abs(s)),
                          FOOT0[side].y - 0.26 * fight - 0.16 * s,
                          FOOT0[side].z + (0.10 + 0.14 * s) * fight
                          - 0.10 * dead)),
                  rot(x=-30.0 * s - 12.0 * fight + 25.0 * dead))
            place("CTRL_HandIK." + side,
                  Vector((sgn * (0.215 + 0.10 * fight),
                          0.045 - 0.20 * fight * j,
                          HIPS0.z - 0.075 + (0.34 + 0.10 * j) * fight
                          - 0.12 * dead)),
                  rot(x=8.0 - 40.0 * fight, y=sgn * 74.0))
            wrot("Shoulder." + side, rot(y=sgn * -6.0 - sgn * 16.0 * fight))
        # arched back while fighting, hanging forward once it is over
        spine(bend=-16.0 * fight * limp + 26.0 * dead,
              twist=9.0 * j * limp, lean=6.0 * k * limp)
        wrot("Head", rot(x=-10.0 * fight * limp + 34.0 * dead,
                         z=-8.0 * j * limp))
        wloc("Hips", Vector((0.02 * k, -0.03 * fight, -0.04 * dead)))
        bpy.context.view_layer.update()
        key(f)
    act.frame_start, act.frame_end = 1, 132
    return act


EVENTS = {
    "Body_Grabbed": [(8, "GrabLock"), (40, "LiftClear"), (56, "Snatch"),
                     (90, "GrabKill"), (120, "Release")],
}


# --------------------------------------------------------------- measuring

def at(act, frame):
    arm.animation_data.action = act
    bpy.context.scene.frame_set(int(frame))
    bpy.context.view_layer.update()


def pose_gap(act, a, b):
    def snap(f):
        at(act, f)
        return [arm.pose.bones[x.name].matrix.to_translation()
                for x in arm.data.bones if x.use_deform]
    pa, pb = snap(a), snap(b)
    worst, where = 0.0, ""
    for m1, m2, bone in zip(pa, pb, [x for x in arm.data.bones if x.use_deform]):
        d = (m1 - m2).length
        if d > worst:
            worst, where = d, bone.name
    return worst, where


def knee_bend(side):
    u = arm.pose.bones["UpperLeg." + side]
    l = arm.pose.bones["LowerLeg." + side]
    return math.degrees((u.tail - u.head).angle(l.tail - l.head))


def scan(act, grounded=True):
    """Worst knee fold, lowest mesh point, and how far a planted foot drifts."""
    worst_k, kf, low, lowf = 0.0, 0, 1e9, 0
    f = int(act.frame_start)
    while f <= int(act.frame_end):
        at(act, f)
        for side in ("R", "L"):
            b = knee_bend(side)
            if b > worst_k:
                worst_k, kf = b, f
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        me = ev.to_mesh()
        z = min(v.co.z for v in me.vertices)
        if z < low:
            low, lowf = z, f
        ev.to_mesh_clear()
        f += 1
    return worst_k, kf, low, lowf


# ---------------------------------------------------------------- rendering

def setup_render():
    os.makedirs(OUT_DIR, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.light = 'STUDIO'
    sc.display.shading.color_type = 'TEXTURE'
    sc.display.shading.show_cavity = True
    sc.render.resolution_x, sc.render.resolution_y = 480, 760
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
    cam.data.ortho_scale = 2.1
    return cam, tgt


def strip(act, frames, cam, tgt, d=(-0.9, -1.0, 0.25)):
    tgt.location = (0, 0, 0.95)
    cam.location = Vector(tgt.location) + Vector(d).normalized() * 6.0
    for f in frames:
        at(act, f)
        bpy.context.scene.render.filepath = os.path.join(
            OUT_DIR, "%s_%03d.png" % (act.name, f))
        bpy.ops.render.render(write_still=True)
    print("SHOT  %-22s %s" % (act.name, list(frames)))


# --------------------------------------------------------------------- run

JOBS = [
    (build_idle,     (1, 90),  (1, 23, 45, 68, 90)),
    (build_walk,     (1, 30),  (1, 5, 9, 13, 17, 21, 25, 29)),
    (build_cablecar, (1, 60),  (1, 15, 30, 45, 60)),
    (build_grabbed,  None,     (1, 9, 21, 41, 57, 65, 91, 105, 121, 131)),
]


def main():
    sc = bpy.context.scene
    sc.render.fps, sc.render.fps_base = 30, 1.0
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    built = []
    for builder, ends, frames in JOBS:
        act = builder()
        built.append((act, ends, frames))
        print("BUILD %-22s %d..%d" % (act.name, act.frame_start, act.frame_end))

    cam, tgt = setup_render()
    print("=" * 72)
    for act, ends, frames in built:
        if ends:
            gap, where = pose_gap(act, *ends)
            print("LOOP  %-22s f%d vs f%d  %.3f mm (%s)"
                  % (act.name, ends[0], ends[1], gap * 1000, where))
        k, kf, low, lowf = scan(act)
        print("SCAN  %-22s worst knee %5.1f at f%-3d  lowest point %+.4f m "
              "at f%-3d" % (act.name, k, kf, low, lowf))
        for f, name in EVENTS.get(act.name, []):
            print("EVENT %-22s f%-3d %s" % (act.name, f, name))

    for act, _e, frames in built:
        strip(act, frames, cam, tgt)

    for act, _e, _f in built:
        arm.animation_data.action = act
        bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=int(act.frame_start),
                         frame_end=int(act.frame_end), only_selected=True,
                         visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True,
                         bake_types={'POSE'})
        print("BAKED %-22s %4d curves" % (act.name, len(action_fcurves(act))))

    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')

    BA.leave_openable()
    arm.animation_data.action = built[0][0]
    sc.frame_start, sc.frame_end, sc.frame_current = 1, 90, 1
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
    print("SAVED", OUT_BLEND)
    print("=" * 72)


if __name__ == "__main__":
    main()
