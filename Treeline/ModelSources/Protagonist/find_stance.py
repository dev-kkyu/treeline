"""Find a shotgun idle stance that is both reachable and on screen.

    blender Protagonist_Arms.blend -b --python find_stance.py

07-shotgun.md's stance was derived from reach alone - the left hand has to get
to a pump 0.46 m ahead of the grip on a 0.595 m arm - and it put the gun almost
entirely below the frame. Reach is necessary, not sufficient. This sweeps grip
position, aim and roll, keeps only the candidates both hands can hold with a
wrist inside 35 degrees, and among those takes the one that shows the most gun.

Prints a stance; animate_arms.py carries the numbers.
"""
import bpy
import math
import os
import sys
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import hand_poses as HP

FOV_Y, ASPECT = math.radians(80), 16.0 / 9.0
WRIST_LIMIT = 35.0

arm = bpy.data.objects["Arms_Armature"]
shotgun = bpy.data.objects["W_Shotgun"]
pump = bpy.data.objects["W_Pump"]

POLE = {"R": Vector((0.22, 0.30, -0.70)), "L": Vector((-0.22, 0.30, -0.70))}


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


def set_weapon(loc, muzzle, up):
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


PUMP_GRIP = None


def grip(pose, twist_span=25.0, grip_override=None):
    side, bone, g, fwd, pal, twist, off = HP.FRAMES[pose]
    if grip_override is not None:
        g = grip_override
    D = delta(bone)
    R = D.to_3x3()
    gw = D @ (rest_head(bone) + Vector(g))
    pw = R @ Vector(pal)
    target = gw - pw.normalized() * off

    def score(t):
        want = place(side, target, R @ Vector(fwd), pw, t)
        return wrist_bend(side) + 2000.0 * (arm.pose.bones["Hand." + side].head
                                            - want).length
    best = min([twist + i for i in range(int(-twist_span), int(twist_span) + 1, 5)],
               key=score)
    want = place(side, target, R @ Vector(fwd), pw, best)
    # against the WRIST target place() returns, not the grip point. The web sits
    # about 56 mm out from the wrist, and comparing the wrist to the web reported
    # a constant ~53 mm miss at every stance - which read as "unreachable".
    return (arm.pose.bones["Hand." + side].head - want).length, wrist_bend(side)


def wrist_bend(side):
    def d(n):
        pb = arm.pose.bones[n + "." + side]
        return (pb.tail - pb.head).normalized()
    return math.degrees(d("LowerArm").angle(d("Hand")))


def on_screen(ob):
    """Fraction of the object in front of the camera and inside the frame."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    me = ev.to_mesh()
    ty = math.tan(FOV_Y * 0.5)
    tx = ty * ASPECT
    inside = 0
    tot = len(me.vertices)
    for v in me.vertices:
        p = v.co
        if p.y >= -0.03:
            continue
        d = -p.y
        if abs(p.x) <= tx * d and abs(p.z) <= ty * d:
            inside += 1
    ev.to_mesh_clear()
    return inside / float(tot)


def try_stance(loc, muzzle, roll):
    up = Matrix.Rotation(math.radians(roll), 3, Vector(muzzle).normalized()) @ \
        Vector((0, 0, 1))
    reset()
    for s in ("R", "L"):
        wloc("CTRL_ElbowPole." + s, POLE[s] - rest_head("CTRL_ElbowPole." + s))
    set_weapon(loc, muzzle, up)
    missR, wR = grip("ShotgunGrip")
    missL, wL = grip("PumpHold", grip_override=PUMP_GRIP)
    vis = on_screen(shotgun)
    pvis = on_screen(pump)
    ok = missR < 0.002 and missL < 0.002 and wR <= WRIST_LIMIT and wL <= WRIST_LIMIT
    return ok, vis, pvis, max(wR, wL), max(missR, missL), missR, missL, wR, wL


def main():
    global PUMP_GRIP
    for back in (0.0, 0.05, 0.09, 0.113):
        PUMP_GRIP = (0, -0.460 + back, 0.012)
        ok, vis, pvis, w, miss = try_stance(Vector((0.16, -0.06, -0.36)),
                                            Vector((-0.55, -0.80, -0.24)), 0)[:5]
        print("PUMPBACK %.3f  miss %6.2f mm  wrist %5.1f  vis %2.0f%%"
              % (back, miss * 1000, w, vis * 100))
    PUMP_GRIP = (0, -0.347, 0.012)
    ok, vis, pvis, w, miss = try_stance(Vector((0.16, -0.06, -0.36)),
                                        Vector((-0.55, -0.80, -0.24)), 0)[:5]
    print("REF07 ok=%s vis %.0f%% pump %.0f%% wrist %.1f miss %.2f mm"
          % (ok, vis * 100, pvis * 100, w, miss * 1000))
    best = None
    bestR = bestL = 1e9
    bestSum = (1e9, 0, 0, 0, 0, None, None, 0)
    tried = ok_n = 0
    for gx in (0.06, 0.12, 0.18):
        for gy in (-0.30, -0.22, -0.14):
            for gz in (-0.34, -0.26, -0.18):
                for mx in (-0.70, -0.50, -0.30):
                    for mz in (-0.40, -0.22, -0.05):
                        m = Vector((mx, -1.0, mz))
                        for roll in (25, 45):
                            tried += 1
                            ok, vis, pvis, w, miss, mR, mL, wR, wL = try_stance(
                                Vector((gx, gy, gz)), m, roll)
                            bestR = min(bestR, mR); bestL = min(bestL, mL)
                            if mR + mL < bestSum[0]:
                                bestSum = (mR + mL, mR, mL, wR, wL,
                                           (gx, gy, gz), tuple(m), roll)
                            if miss > 0.002:
                                continue
                            ok_n += 1
                            # prefer slack, not merely passing: a stance that
                            # sits at 34.7 of 35 has nowhere to go once the
                            # clip rolls the gun, and the reload went over on
                            # the third frame.
                            s = vis + 0.5 * pvis - 0.03 * max(0.0, w - 22.0)
                            if best is None or s > best[0]:
                                best = (s, (gx, gy, gz), tuple(m), roll, vis,
                                        pvis, w, miss)
    print("TRIED %d, reachable %d" % (tried, ok_n))
    print("BESTMISS  right alone %.1f mm   left alone %.1f mm" % (bestR * 1000, bestL * 1000))
    print("BESTBOTH  R %.1f mm  L %.1f mm  wrists %.1f / %.1f  loc %s muzzle %s roll %s"
          % (bestSum[1] * 1000, bestSum[2] * 1000, bestSum[3], bestSum[4],
             bestSum[5], bestSum[6], bestSum[7]))
    if best is None:
        print("NONE reachable")
        return
    s, loc, m, roll, vis, pvis, w, miss = best
    print("BEST  loc %s  muzzle %s  roll %d" % (loc, tuple(round(c, 3) for c in m), roll))
    print("      shotgun on screen %.0f%%  pump %.0f%%  worst wrist %.1f  miss %.2f mm"
          % (vis * 100, pvis * 100, w, miss * 1000))


main()
