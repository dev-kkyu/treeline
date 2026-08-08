"""Build the six Crawler actions on the rig, bake to deform bones, export FBX.

    blender Crawler_Rig.blend -b --python animate.py

Clip list / frame counts / event frames come from
docs/enemy-sheets/mockups/codex/crawler/animation/*.md
"""
import bpy
import math
import os
from mathutils import Vector

SCRATCH = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"
EXPORT_DIR = os.environ.get("CRAWLER_EXPORT_DIR", SCRATCH)

arm = bpy.data.objects["Crawler_Armature"]
mesh = bpy.data.objects["Crawler_Mesh"]

FEET = ["CTRL_FootIK.FL", "CTRL_FootIK.FR", "CTRL_FootIK.HL", "CTRL_FootIK.HR"]
BODY = "DEF_BodyRoot"
ROT_BONES = ["DEF_BodyRoot", "DEF_SpineFront", "DEF_HeadPlate", "DEF_Mouth",
             "DEF_Tail01", "DEF_Tail02", "DEF_Tail03", "DEF_Tail04"]

for pb in arm.pose.bones:
    pb.rotation_mode = 'XYZ'

_basis = {pb.name: pb.bone.matrix_local.to_3x3().inverted() for pb in arm.pose.bones}


_world_delta = {}


def wloc(name, dx, dy, dz):
    """Set a pose bone's translation from a world-space delta."""
    _world_delta[name] = Vector((dx, dy, dz))
    arm.pose.bones[name].location = _basis[name] @ Vector((dx, dy, dz))


def ground_clamp(limit=0.0005):
    """Lift the body until nothing is below the floor.

    The generated mesh clears the ground by only 65 mm under the head, so poses
    taken straight from the spec's angles push the jaw through the floor. The
    feet are on IK, so lifting the body re-folds the legs instead of dragging
    them up.
    """
    for _ in range(6):
        bpy.context.view_layer.update()
        ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        zmin = min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)
        if zmin >= -limit:
            return zmin
        d = _world_delta.get(BODY, Vector((0, 0, 0)))
        wloc(BODY, d.x, d.y, d.z - zmin + limit)
    return zmin


def rot(name, rx=0.0, ry=0.0, rz=0.0):
    arm.pose.bones[name].rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))


def reset_pose():
    _world_delta.clear()
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_euler = (0, 0, 0)


def key(frame, names=None):
    for name in (names or (FEET + ROT_BONES)):
        pb = arm.pose.bones[name]
        if name in FEET or name == BODY:
            pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_euler", frame=frame)


def action_fcurves(act):
    """Blender 5 moved fcurves into action layers/slots; 4.x kept act.fcurves."""
    if hasattr(act, "fcurves"):
        return list(act.fcurves)
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags:
                out.extend(cb.fcurves)
    return out


def settle_action(act):
    """Per-frame ground clamp.

    The sparse spec key frames are clamped as they are authored, but the frames
    between them still dip: body height and the nose-up pitch that pays for it
    interpolate at different rates. Run before baking, while the feet are still
    on IK, so lifting the body re-folds the legs instead of floating the feet.
    """
    arm.animation_data.action = act
    pb = arm.pose.bones[BODY]
    worst = 0.0
    for f in range(int(act.frame_start), int(act.frame_end) + 1):
        bpy.context.scene.frame_set(f)
        for _ in range(6):
            bpy.context.view_layer.update()
            ev = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            zmin = min((mesh.matrix_world @ v.co).z for v in ev.data.vertices)
            worst = min(worst, zmin)
            if zmin >= -0.0005:
                break
            pb.location = pb.location + (_basis[BODY] @ Vector((0, 0, -zmin + 0.0005)))
        pb.keyframe_insert("location", frame=f)
    return worst


def new_action(name):
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


# ---------------------------------------------------------------- gait helper

def gait(u, duty, stride, lift):
    """Foot offset (dy, dz) for cycle phase u in [0,1).

    Stance runs backward (+y) at constant speed so a planted foot never slides;
    swing carries it forward again on a sine arc.
    """
    u = u % 1.0
    if u < duty:
        s = u / duty
        return stride * (s - 0.5), 0.0
    s = (u - duty) / (1.0 - duty)
    return stride * (0.5 - s), lift * math.sin(math.pi * s)


def build_locomotion(name, frames, phases, duty, stride_f, stride_h, lift_f, lift_h,
                     body_drop, bob, pitch, roll, tail_swing, head_counter):
    act = new_action(name)
    period = frames  # frame 1 and frame frames+1 hold the same pose
    for f in range(1, frames + 2):
        t = (f - 1) / period
        reset_pose()
        for foot, ph in phases.items():
            stride = stride_f if ".F" in foot else stride_h
            lift = lift_f if ".F" in foot else lift_h
            dy, dz = gait(t - ph, duty, stride, lift)
            wloc(foot, 0.0, dy, dz)
            # keep the sole roughly flat: tip the foot up as it leaves the ground
            arm.pose.bones[foot].rotation_euler = (math.radians(-18.0 * (dz / max(lift, 1e-6))), 0, 0)
        bz = -body_drop + bob * math.sin(4 * math.pi * t)
        by = 0.012 * math.sin(2 * math.pi * t)
        wloc(BODY, 0.0, by, bz)
        body_pitch = pitch * math.sin(4 * math.pi * t)
        rot(BODY, body_pitch, 0, 0)
        # the neck cancels the body pitch so the head keeps aiming at the floor
        # instead of swinging its 1.5 m lever arm through it
        rot("DEF_SpineFront", -body_pitch, 0, roll * math.sin(2 * math.pi * t))
        rot("DEF_HeadPlate", -head_counter * 0.3 * math.sin(4 * math.pi * t), 0,
            -roll * 0.6 * math.sin(2 * math.pi * t))
        rot("DEF_Mouth", 1.2 + 1.0 * math.sin(4 * math.pi * t), 0, 0)
        for i in range(4):
            lag = 0.10 * (i + 1)
            rot("DEF_Tail0%d" % (i + 1),
                -1.5 - 1.0 * (i + 1) * math.sin(2 * math.pi * (t - lag)), 0,
                tail_swing * (i + 1) * 0.55 * math.sin(2 * math.pi * (t - lag)))
        ground_clamp()
        key(f)
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    act.frame_start, act.frame_end = 1, frames + 1
    return act


def build_oneshot(name, frames, poses):
    """poses: {frame: {bone: dict(dy=,dz=,rx=,ry=,rz=)}}"""
    act = new_action(name)
    for f in sorted(poses):
        reset_pose()
        for bone, p in poses[f].items():
            if "dy" in p or "dz" in p or "dx" in p:
                wloc(bone, p.get("dx", 0.0), p.get("dy", 0.0), p.get("dz", 0.0))
            if "rx" in p or "ry" in p or "rz" in p:
                rot(bone, p.get("rx", 0.0), p.get("ry", 0.0), p.get("rz", 0.0))
        ground_clamp()
        key(f)
    # plain bezier overshoots between the spec's sparse key frames and dips the
    # jaw through the floor; clamped handles keep every curve inside its keys
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        fc.update()
    act.frame_start, act.frame_end = 1, frames
    return act


# ------------------------------------------------------------------- 1 idle

def build_idle():
    act = new_action("Crawler_Idle_Low")
    for f in range(1, 47):
        t = (f - 1) / 45.0
        reset_pose()
        wloc(BODY, 0.0, 0.0, -0.004 * math.sin(2 * math.pi * t))
        rot("DEF_SpineFront", 0, 0, 1.5 * math.sin(2 * math.pi * t))
        rot("DEF_HeadPlate", 0.6 * math.sin(2 * math.pi * t), 0,
            -0.9 * math.sin(2 * math.pi * t))
        rot("DEF_Mouth", 0.5 + 0.5 * math.sin(2 * math.pi * t), 0, 0)
        for i in range(4):
            lag = 0.06 * (i + 1)
            rot("DEF_Tail0%d" % (i + 1), 0, 0,
                (2 + i) * 0.5 * math.sin(2 * math.pi * (t - lag)))
        ground_clamp()
        key(f)
    act.frame_start, act.frame_end = 1, 46
    return act


# ------------------------------------------------------------------ 4 attack
F, H = "CTRL_FootIK.F", "CTRL_FootIK.H"


def build_attack():
    return build_oneshot("Crawler_Attack_Underbite", 22, {
        1: {BODY: dict(dz=-0.02), "DEF_Mouth": dict(rx=1)},
        4: {BODY: dict(dy=0.035, dz=-0.015), "DEF_Mouth": dict(rx=5),
            F + "L": dict(dx=-0.04, dy=0.02), F + "R": dict(dx=0.04, dy=0.02),
            "DEF_HeadPlate": dict(rx=2), "DEF_Tail01": dict(rx=-6),
            "DEF_Tail02": dict(rx=-6), "DEF_Tail03": dict(rx=-5)},
        # spec asks for 6 deg of head-plate dip here; this mesh only clears the
        # floor by 65 mm so the dip is split 3/1 between plate and neck
        8: {BODY: dict(dy=0.045, dz=-0.02), "DEF_Mouth": dict(rx=12),
            F + "L": dict(dx=-0.04, dy=0.02), F + "R": dict(dx=0.04, dy=0.02),
            "DEF_HeadPlate": dict(rx=3), "DEF_SpineFront": dict(rx=1),
            "DEF_Tail01": dict(rx=-8), "DEF_Tail02": dict(rx=-8), "DEF_Tail03": dict(rx=-6)},
        11: {BODY: dict(dy=-0.055, dz=0.01), "DEF_Mouth": dict(rx=28),
             H + "L": dict(dz=0.02), H + "R": dict(dz=0.02),
             "DEF_HeadPlate": dict(rx=-2), "DEF_SpineFront": dict(rx=-3),
             "DEF_Tail01": dict(rx=-2)},
        13: {BODY: dict(dy=-0.080, dz=0.02), "DEF_Mouth": dict(rx=38),
             H + "L": dict(dz=0.035), H + "R": dict(dz=0.035),
             "DEF_HeadPlate": dict(rx=-8), "DEF_SpineFront": dict(rx=-6),
             "DEF_Tail01": dict(rx=2), "DEF_Tail02": dict(rx=3)},
        14: {BODY: dict(dy=-0.080, dz=0.02), "DEF_Mouth": dict(rx=38),
             H + "L": dict(dz=0.035), H + "R": dict(dz=0.035),
             "DEF_HeadPlate": dict(rx=-8), "DEF_SpineFront": dict(rx=-6),
             "DEF_Tail01": dict(rx=2), "DEF_Tail02": dict(rx=3)},
        15: {BODY: dict(dy=-0.05, dz=-0.03), "DEF_Mouth": dict(rx=18),
             "DEF_HeadPlate": dict(rx=-3), "DEF_SpineFront": dict(rx=-2)},
        18: {BODY: dict(dy=-0.01, dz=-0.01), "DEF_Mouth": dict(rx=4)},
        22: {BODY: dict(), "DEF_Mouth": dict(rx=1)},
    })


# --------------------------------------------------------------------- 5 hit
def build_hit():
    return build_oneshot("Crawler_Hit", 12, {
        1: {BODY: dict(), "DEF_Mouth": dict(rx=1)},
        3: {BODY: dict(dx=-0.035, dz=-0.02), "DEF_SpineFront": dict(ry=5),
            "DEF_Mouth": dict(rx=4), "DEF_HeadPlate": dict(rz=4),
            F + "R": dict(dx=-0.02), H + "R": dict(dx=-0.02)},
        5: {BODY: dict(dx=-0.055, dz=-0.03), "DEF_SpineFront": dict(ry=5, rz=-3),
            "DEF_Mouth": dict(rx=4), "DEF_HeadPlate": dict(rz=6, ry=-3),
            F + "L": dict(dx=-0.03, dz=0.01), H + "L": dict(dx=-0.025),
            "DEF_Tail01": dict(rz=-8), "DEF_Tail02": dict(rz=-10),
            "DEF_Tail03": dict(rz=-8)},
        8: {BODY: dict(dx=-0.016, dz=-0.008), "DEF_SpineFront": dict(ry=1.5),
            "DEF_HeadPlate": dict(rz=1.5), "DEF_Tail01": dict(rz=4),
            "DEF_Tail02": dict(rz=6), "DEF_Tail03": dict(rz=5)},
        12: {BODY: dict(), "DEF_Mouth": dict(rx=1)},
    })


# ------------------------------------------------------------------- 6 death
def build_death():
    # The hips are the tallest part (0.78 m) and the jaw already sits 65 mm off
    # the floor, so the collapse is authored as "hips fall, head stays down":
    # every drop of the body is paired with a nose-up pitch on DEF_BodyRoot.
    settled = {
        BODY: dict(dz=-0.24, dx=-0.03, rx=-8, ry=6),
        "DEF_SpineFront": dict(rx=5, rz=-5), "DEF_HeadPlate": dict(rx=2, rz=-3),
        "DEF_Mouth": dict(rx=6),
        F + "L": dict(dx=0.13, dy=0.20), F + "R": dict(dx=-0.12, dy=0.18),
        H + "L": dict(dx=0.12, dy=-0.17), H + "R": dict(dx=-0.10, dy=-0.19),
        "DEF_Tail01": dict(rz=-4), "DEF_Tail02": dict(rz=-8),
        "DEF_Tail03": dict(rz=-6), "DEF_Tail04": dict(rz=4),
    }
    return build_oneshot("Crawler_Death", 30, {
        1: {BODY: dict(), "DEF_Mouth": dict(rx=3)},
        5: {BODY: dict(dz=-0.05, dx=-0.01, rx=-2), "DEF_Mouth": dict(rx=5),
            F + "L": dict(dy=0.05, dz=0.01), H + "R": dict(dy=-0.04),
            "DEF_SpineFront": dict(rx=1)},
        10: {BODY: dict(dz=-0.13, dx=-0.02, rx=-5, ry=2), "DEF_Mouth": dict(rx=7),
             F + "L": dict(dx=0.10, dy=0.14), F + "R": dict(dx=-0.09, dy=0.12),
             H + "R": dict(dy=-0.06), "DEF_SpineFront": dict(rx=4, rz=-3),
             "DEF_HeadPlate": dict(rx=3), "DEF_Tail01": dict(rz=10),
             "DEF_Tail02": dict(rz=12)},
        16: {BODY: dict(dz=-0.20, dx=-0.03, rx=-7, ry=5), "DEF_Mouth": dict(rx=8),
             F + "L": dict(dx=0.12, dy=0.18), F + "R": dict(dx=-0.11, dy=0.16),
             H + "L": dict(dx=0.10, dy=-0.14), H + "R": dict(dx=-0.08, dy=-0.16),
             "DEF_SpineFront": dict(rx=6, rz=-5), "DEF_HeadPlate": dict(rx=2, rz=-3),
             "DEF_Tail01": dict(rz=6), "DEF_Tail02": dict(rz=-6), "DEF_Tail03": dict(rz=-10)},
        23: settled,
        26: settled,
        30: settled,
    })


def main():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')

    actions = []
    actions.append(build_idle())

    SK = dict(frames=24, duty=0.55, stride_f=0.40, stride_h=0.34,
              lift_f=0.06, lift_h=0.09, body_drop=0.020, bob=0.015,
              pitch=1.5, roll=2.5, tail_swing=4.0, head_counter=1.8)
    actions.append(build_locomotion(
        "Crawler_Skitter",
        phases={"CTRL_FootIK.FL": 0.0, "CTRL_FootIK.HR": 0.0,
                "CTRL_FootIK.FR": 0.5, "CTRL_FootIK.HL": 0.5}, **SK))

    SP = dict(frames=18, duty=0.34, stride_f=0.46, stride_h=0.40,
              lift_f=0.10, lift_h=0.13, body_drop=0.0, bob=0.025,
              pitch=3.0, roll=1.5, tail_swing=3.0, head_counter=3.0)
    actions.append(build_locomotion(
        "Crawler_Sprint",
        phases={"CTRL_FootIK.HR": 0.0, "CTRL_FootIK.HL": 0.055,
                "CTRL_FootIK.FR": 0.42, "CTRL_FootIK.FL": 0.475}, **SP))

    actions.append(build_attack())
    actions.append(build_hit())
    actions.append(build_death())

    for tag, cfg, fps in (("Skitter", SK, 30.0), ("Sprint", SP, 30.0)):
        stance = cfg["duty"] * cfg["frames"] / fps
        print("SPEED %-8s stance %.3fs front %.2f m/s  hind %.2f m/s" % (
            tag, stance, cfg["stride_f"] / stance, cfg["stride_h"] / stance))

    # bake control rig down onto the deform bones
    for act in actions:
        print("SETTLE %-26s worst dip %7.4f" % (act.name, settle_action(act)))

    bpy.ops.pose.select_all(action='SELECT')
    for act in actions:
        arm.animation_data.action = act
        bpy.ops.nla.bake(frame_start=int(act.frame_start), frame_end=int(act.frame_end),
                         only_selected=True, visual_keying=True, clear_constraints=False,
                         clear_parents=False, use_current_action=True, bake_types={'POSE'})
        print("BAKED", act.name, int(act.frame_start), int(act.frame_end))

    # constraints have been baked in; leaving them on would double-transform
    for pb in arm.pose.bones:
        for c in list(pb.constraints):
            pb.constraints.remove(c)
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SCRATCH, "Crawler_Rig.blend"))
    print("ACTIONS", [a.name for a in bpy.data.actions])


main()
