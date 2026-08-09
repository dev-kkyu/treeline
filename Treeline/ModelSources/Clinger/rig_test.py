"""Acceptance numbers for the Clinger rig and its nine clips.

    blender <scratch>/Clinger_Anim.blend -b --python rig_test.py

Everything MODEL-SPEC.md and the animation docs claim as a measurement comes
from here. Run it before touching those numbers by hand.
"""
import bpy
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mathutils import Vector                                     # noqa: E402
import poses as P                                                # noqa: E402

arm, mesh = P.arm, P.mesh

CEILING = ("Clinger_Cling_Folded", "Clinger_Cling_Tremor",
           "Clinger_Unfold_Drop", "Clinger_Death_Fall")
LOOPS = ("Clinger_Cling_Folded", "Clinger_Scuttle")
CONTACTS = {"R": Vector(P.HAND_PLANT["R"]), "L": Vector(P.HAND_PLANT["L"]),
            "RR": Vector(P.HOOK_PLANT["RR"]), "LR": Vector(P.HOOK_PLANT["LR"])}

FAIL = []


def constraints(on):
    """animate.py leaves the IK muted after baking; the pose checks need it back."""
    for pb in arm.pose.bones:
        for c in pb.constraints:
            c.mute = not on


def check(ok, msg):
    print("%s %s" % ("  ok  " if ok else "  FAIL", msg))
    if not ok:
        FAIL.append(msg)


def mesh_stats():
    tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
    worst = max(sum(1 for g in v.groups if g.weight > 1e-4)
                for v in mesh.data.vertices)
    print("MESH   %d tris  %d verts  max %d weights/vertex  %d groups"
          % (tris, len(mesh.data.vertices), worst, len(mesh.vertex_groups)))
    check(worst <= 4, "max 4 weights per vertex")


def bones():
    total = len(arm.data.bones)
    deform = sum(1 for b in arm.data.bones if b.use_deform)
    print("BONES  %d total, %d deform, %d exported"
          % (total, deform, deform + 2))
    check(deform == 24, "24 deform bones")
    check(deform + 2 == 26, "26 exported bones")


def symmetry():
    """Mirror every .R bone against its .L twin in the rest pose and in the
    landed stance. A rig that is only symmetric at rest is not symmetric."""
    for tag, setup in (("rest", lambda: None), ("stance", P.stance)):
        P.reset()
        P.hooks_home()
        setup()
        worst, who = 0.0, ""
        for b in arm.data.bones:
            n = b.name
            if not n.startswith("DEF_"):
                continue
            ln = (n[:-1] + "L") if n.endswith(".R") else n.replace(".R", ".L")
            if ln == n or ln not in arm.pose.bones:
                continue
            r, l = P.bone_tail(n), P.bone_tail(ln)
            e = (Vector((-l.x, l.y, l.z)) - r).length
            if e > worst:
                worst, who = e, n
        print("SYMM   %-8s worst %.4f m at %s" % (tag, worst, who))
        check(worst <= 0.001, "%s symmetric within 1 mm" % tag)


def jaw_sweep():
    """How far the mouth actually opens.

    Measuring the mesh bounding box here is useless - the lowest point of the
    model is a claw and the jaw never touches it. What matters is how far the
    jaw's own vertices travel, and that nothing else in the head moves with them.
    """
    P.reset()
    P.hooks_home()
    shut = P.evaluated_points()
    for a in (20, 36, 48, 62, 75):
        P.rot(P.JAW, x=a)
        open_ = P.evaluated_points()
        travel = max((open_[i] - shut[i]).length for i in range(len(shut)))
        moved = sum(1 for i in range(len(shut))
                    if (open_[i] - shut[i]).length > 0.002)
        print("JAW    %3d deg  gape %.3f m  %d verts moved" % (a, travel, moved))
    P.rot(P.JAW, x=0.0)


def clip_report():
    print("%-26s %6s %8s %8s %8s %8s %8s" %
          ("CLIP", "frames", "zmin", "zmax", "height", "widest", "seam"))
    for act in sorted(bpy.data.actions, key=lambda a: a.name):
        arm.animation_data.action = act
        lo, hi = int(act.frame_start), int(act.frame_end)
        zl, zh, wide = 9e9, -9e9, 0.0
        for f in range(lo, hi + 1):
            bpy.context.scene.frame_set(f)
            pts = P.evaluated_points()
            zl = min(zl, min(p.z for p in pts))
            zh = max(zh, max(p.z for p in pts))
            wide = max(wide, max(p.x for p in pts) - min(p.x for p in pts))
        seam = "-"
        if act.name in LOOPS:
            def sample(f):
                bpy.context.scene.frame_set(f)
                bpy.context.view_layer.update()
                return {b.name: arm.pose.bones[b.name].matrix.copy()
                        for b in arm.data.bones if b.use_deform}
            a, b = sample(lo), sample(hi)
            dt = max((a[n].to_translation() - b[n].to_translation()).length for n in a)
            dr = max(math.degrees(a[n].to_quaternion().rotation_difference(
                b[n].to_quaternion()).angle) for n in a)
            seam = "%.4f" % dt
            check(dt <= 0.0005 and dr <= 0.05,
                  "%s loop seam closes (%.4f m / %.3f deg)" % (act.name, dt, dr))
        print("%-26s %3d..%-3d %+8.3f %+8.3f %8.3f %8.3f %8s"
              % (act.name, lo, hi, zl, zh, zh - zl, wide, seam))
        if act.name in CEILING:
            check(zh <= 0.035, "%s stays out of the ceiling (%+.3f)" % (act.name, zh))
        else:
            # 15 mm, not 2: `Land` drives a 160 mm impact squash and the mesh is
            # allowed to sink into the floor for the two frames that sells it
            check(zl >= -0.015, "%s stays out of the floor (%+.3f)" % (act.name, zl))


def stance_contacts():
    P.reset()
    P.hooks_home()
    P.stance()
    b = P.bbox()
    print("STANCE w %.3f  d %.3f  h %.3f   head %s"
          % (b[1] - b[0], b[3] - b[2], b[5] - b[4],
             tuple(round(v, 3) for v in P.bone_tail(P.HEAD))))
    # what matters is the mesh touching the floor at each contact, not the IK
    # target - `hand_clamp` moves the hand target on purpose
    pts = P.evaluated_points()
    for tag, xy in (("hand R", (0.62, -0.7)), ("hand L", (-0.62, -0.7)),
                    ("hook RR", (0.50, 0.42)), ("hook LR", (-0.50, 0.42))):
        near = [p.z for p in pts
                if (p.x - xy[0]) ** 2 + (p.y - xy[1]) ** 2 < 0.30 ** 2]
        gap = min(near) if near else 9.99
        print("       %-8s floor gap %+.4f m" % (tag, gap))
        check(abs(gap) <= 0.012, "%s touches the floor (%+.4f)" % (tag, gap))


def folded_bound():
    P.reset()
    P.hooks_home()
    P.set_ik(ceiling=1.0)
    P.cling(fold=1.0)
    b = P.bbox()
    print("FOLDED w %.3f  d %.3f  h %.3f   ceiling overshoot %+.3f"
          % (b[1] - b[0], b[3] - b[2], b[5] - b[4], b[5]))


def main():
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='POSE')
    mesh_stats()
    bones()
    constraints(True)
    symmetry()
    jaw_sweep()
    folded_bound()
    stance_contacts()
    constraints(False)
    P.reset()
    clip_report()
    print("\nRESULT %s%s" % ("PASS" if not FAIL else "FAIL",
                             "" if not FAIL else " (%d)" % len(FAIL)))
    for m in FAIL:
        print("  -", m)


main()
