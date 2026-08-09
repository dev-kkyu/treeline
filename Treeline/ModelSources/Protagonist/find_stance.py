"""Find weapon stances that hold like the reference photographs.

    blender Protagonist_Arms.blend -b --python find_stance.py

The first version scored reach only and put the shotgun 19% on screen. The
second added visibility and got it into frame, but still far away, tipped over,
with the forearms filling the shot - nothing like a viewmodel. Reach is a gate,
not a goal.

What a held weapon has to satisfy, from the references:

  size      the gun reads as the subject, not as something the hands are near
  place     it sits where the eye expects it - low centre for a pistol, across
            the lower frame for a long gun
  level     the top of the weapon faces up. A gun rolled over to show its port
            is a reload beat, not an idle
  along     the barrel runs away from the camera, so the sights line up

All four are measurable, so none of them is authored. Reach and wrist gate a
candidate; those four score it.
"""
import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hand_poses as HP
# for fit_elbow only. The elbow is a free degree of freedom and the wrist gate is
# meaningless without it: pinning the elbow charged every stance for an angle the
# arm did not have to hold.
import animate_arms as A

FOV_Y, ASPECT = math.radians(A.FOV_Y), 16.0 / 9.0
WRIST_GATE = 48.0        # slack under animate_arms.py's 55, which the clips eat into
MISS_GATE = 0.002

arm = bpy.data.objects["Arms_Armature"]
POLE = {"R": Vector((-0.22, 0.30, -0.70)), "L": Vector((0.22, 0.30, -0.70))}

# coverage is the share of the frame the weapon's silhouette should fill;
# centre is where it sits in normalised screen space and z up, -1..1. Screen
# right is -x in world: the camera looks down -y with +z up, so its right-hand
# vector is -x, and the shooting side sits at negative x for the same reason.
WEAPONS = {
    "pistol": dict(
        meshes=("W_Pistol", "W_Slide"), grips=("PistolGrip", "PistolSupport"),
        coverage=0.11, centre=(0.48, -0.52),
        loc=((-0.18, -0.14, -0.10), (-0.28, -0.24, -0.20),
             (-0.14, -0.10, -0.06)),
        muzzle=((0.10, 0.20, 0.32), (0.02, 0.06, 0.10)),
        rolls=(0, 10)),
    "shotgun": dict(
        meshes=("W_Shotgun", "W_Pump"), grips=("ShotgunGrip", "PumpHold"),
        coverage=0.29, centre=(0.13, -0.33),
        loc=((-0.26, -0.22, -0.18), (-0.18, -0.14, -0.10),
             (-0.16, -0.12, -0.08)),
        muzzle=((0.45, 0.60, 0.75), (-0.08, -0.02, 0.04)),
        rolls=(-12, 0)),
}


def rest_head(n):
    return arm.data.bones[n].head_local


def delta(n):
    pb = arm.pose.bones[n]
    return pb.matrix @ arm.data.bones[n].matrix_local.inverted()


def reset():
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def wloc(n, d):
    m = arm.data.bones[n].matrix_local.to_3x3()
    arm.pose.bones[n].location = m.inverted() @ Vector(d)


def wrot(n, r):
    m = arm.data.bones[n].matrix_local.to_3x3()
    pb = arm.pose.bones[n]
    pb.rotation_mode = 'QUATERNION'
    pb.rotation_quaternion = (m.inverted() @ r @ m).to_quaternion()


def aim_basis(muzzle, up):
    m = Vector(muzzle).normalized()
    u = Vector(up)
    u = (u - u.dot(m) * m).normalized()
    return Matrix((u.cross(m), m, u)).transposed()


REST_BASIS = aim_basis(Vector((0, -1, 0)), Vector((0, 0, 1)))


def set_weapon(loc, muzzle, roll):
    up = Matrix.Rotation(math.radians(roll), 3,
                         Vector(muzzle).normalized()) @ Vector((0, 0, 1))
    wrot("WeaponRoot", aim_basis(muzzle, up) @ REST_BASIS.inverted())
    wloc("WeaponRoot", Vector(loc) - rest_head("WeaponRoot"))
    bpy.context.view_layer.update()


def rest_frame(side):
    b = arm.data.bones["Hand." + side]
    fwd = (b.tail_local - b.head_local).normalized()
    palm = Vector((0, 0, -1))
    palm = (palm - palm.dot(fwd) * fwd).normalized()
    return Matrix((fwd, fwd.cross(palm), -palm)).transposed()


WEB = {s: (arm.data.bones["Thumb1." + s].head_local
           + arm.data.bones["Index1." + s].head_local) * 0.5 for s in ("R", "L")}


def place(side, grip_w, fwd, pal, twist):
    fwd = Vector(fwd).normalized()
    pal = Vector(pal)
    pal = (pal - pal.dot(fwd) * fwd).normalized()
    t = Matrix((fwd, fwd.cross(pal), -pal)).transposed()
    if twist:
        t = Matrix.Rotation(math.radians(twist), 3, pal) @ t
    rot = t @ rest_frame(side).inverted()
    c = "CTRL_HandIK." + side
    m = arm.data.bones[c].matrix_local.to_3x3()
    pb = arm.pose.bones[c]
    pb.rotation_quaternion = (m.inverted() @ rot @ m).to_quaternion()
    wrist = arm.data.bones["Hand." + side].head_local
    want = Vector(grip_w) - rot @ (WEB[side] - wrist)
    pb.location = m.inverted() @ (want - wrist)
    bpy.context.view_layer.update()
    return want


def wrist_bend(side):
    def d(n):
        pb = arm.pose.bones[n + "." + side]
        return (pb.tail - pb.head).normalized()
    return math.degrees(d("LowerArm").angle(d("Hand")))


def set_fingers(side, pose):
    for d in HP.DIGITS:
        a = HP.angles(pose, d)
        for i in (1, 2, 3):
            pb = arm.pose.bones["%s%d.%s" % (d, i, side)]
            pb.rotation_mode = 'XYZ'
            pb.rotation_euler = (math.radians(a[i - 1]), 0, 0)


def grip(pose):
    side, bone, g, fwd, pal, twist, off = HP.FRAMES[pose]
    D = delta(bone)
    R = D.to_3x3()
    target = D @ (rest_head(bone) + Vector(g)) - (R @ Vector(pal)).normalized() * off

    def score(t):
        want = place(side, target, R @ Vector(fwd), R @ Vector(pal), t)
        return wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                            - want).length
    best = min(range(int(twist) - 25, int(twist) + 26, 5), key=score)
    want = place(side, target, R @ Vector(fwd), R @ Vector(pal), best)
    # the fingers matter for anything that gets looked at: without them the
    # renders showed two open palms and no grip at all
    set_fingers(side, pose)
    A.fit_elbow(side)
    bpy.context.view_layer.update()
    # measured against the wrist target place() returns, not the grip point: the
    # web sits 56 mm out from the wrist and comparing the two reported a
    # constant 53 mm miss at every stance, which read as unreachable
    return ((arm.pose.bones["Hand." + side].head - want).length,
            wrist_bend(side), A.forearm_roll(side))


TY = math.tan(FOV_Y * 0.5)
TX = TY * ASPECT


def screen(meshes):
    """Share of the weapon in frame, where it sits, and how much frame it fills."""
    dg = bpy.context.evaluated_depsgraph_get()
    xs, zs, inside, tot = [], [], 0, 0
    for name in meshes:
        ev = bpy.data.objects[name].evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            tot += 1
            p = v.co
            if p.y >= -0.03:
                continue
            d = -p.y
            # screen right is -x: the camera looks down -y with +z up, so its
            # right-hand vector is forward x up = -x. Reporting raw world x here
            # meant the shotgun's target centre and its search grid were on
            # opposite sides of the frame, and the pump left the picture.
            sx, sz = -p.x / (TX * d), p.z / (TY * d)
            if abs(sx) <= 1 and abs(sz) <= 1:
                inside += 1
                xs.append(sx)
                zs.append(sz)
        ev.to_mesh_clear()
    if not xs:
        return 0.0, 0.0, 0.0, 0.0
    span = (max(xs) - min(xs)) * (max(zs) - min(zs)) * 0.25
    return (inside / float(tot), sum(xs) / len(xs), sum(zs) / len(zs),
            min(span, 1.0))


def evaluate(cfg, loc, muzzle, roll):
    reset()
    for s in ("R", "L"):
        wloc("CTRL_ElbowPole." + s, POLE[s] - rest_head("CTRL_ElbowPole." + s))
    set_weapon(loc, muzzle, roll)
    misses, wrists, rolls = [], [], []
    for g in cfg["grips"]:
        m, w, r = grip(g)
        misses.append(m)
        wrists.append(w)
        rolls.append(r)
    vis, sx, sz, span = screen(cfg["meshes"])
    return max(misses), max(wrists), max(rolls), vis, sx, sz, span


def search(name, cfg):
    best, gated = None, 0
    for gx in cfg["loc"][0]:
        for gy in cfg["loc"][1]:
            for gz in cfg["loc"][2]:
                for mx in cfg["muzzle"][0]:
                    for mz in cfg["muzzle"][1]:
                        for roll in cfg["rolls"]:
                            m = Vector((mx, -1.0, mz))
                            miss, w, rl, vis, sx, sz, span = evaluate(
                                cfg, Vector((gx, gy, gz)), m, roll)
                            if (miss > MISS_GATE or w > WRIST_GATE
                                    or rl > A.ROLL_LIMIT or vis < 0.6):
                                continue
                            gated += 1
                            cx, cz = cfg["centre"]
                            s = -(3.0 * abs(span - cfg["coverage"])
                                  + 1.2 * abs(sx - cx) + 1.2 * abs(sz - cz)
                                  + 0.02 * abs(roll)
                                  + 0.02 * max(0.0, w - 20.0)
                                  - 0.4 * vis)
                            if best is None or s > best[0]:
                                best = (s, (gx, gy, gz), (mx, -1.0, mz), roll,
                                        vis, sx, sz, span, w, rl, miss)
    print("SEARCH %-8s passed the gate %d" % (name, gated))
    if best is None:
        print("       nothing passed")
        return
    s, loc, mu, roll, vis, sx, sz, span, w, rl, miss = best
    print("BEST   %-8s loc %s  muzzle %s  roll %d"
          % (name, tuple(round(c, 3) for c in loc),
             tuple(round(c, 2) for c in mu), roll))
    print("       in frame %.0f%%  fills %.0f%%  centre (%+.2f %+.2f)  "
          "wrist %.1f  roll %.1f  miss %.2f mm"
          % (vis * 100, span * 100, sx, sz, w, rl, miss * 1000))


for _name, _cfg in WEAPONS.items():
    search(_name, _cfg)
