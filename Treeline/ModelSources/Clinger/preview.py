"""Render the anchor poses so they can be judged as pictures, not numbers.

    blender <scratch>/Clinger_Rig.blend -b --python preview.py
"""
import bpy, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mathutils import Vector
import poses as P

OUT = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"

sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x = 460
sc.render.resolution_y = 460
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.10, 0.11, 0.12, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.5
cd = bpy.data.cameras.new("C"); cam = bpy.data.objects.new("C", cd)
sc.collection.objects.link(cam); sc.camera = cam
cd.type = 'ORTHO'
ld = bpy.data.lights.new("L", 'SUN'); ld.energy = 3.5
lo = bpy.data.objects.new("L", ld); sc.collection.objects.link(lo)
lo.rotation_euler = (math.radians(58), 0, math.radians(35))
gm = bpy.data.meshes.new("floor")
gm.from_pydata([(-5,-5,0),(5,-5,0),(5,5,0),(-5,5,0)], [], [(0,1,2,3)])
fl = bpy.data.objects.new("floor", gm); sc.collection.objects.link(fl)
fm = bpy.data.materials.new("fm"); fm.diffuse_color = (0.22,0.23,0.24,1)
fl.data.materials.append(fm)

def shot(tag, ctr, scale, views=("front","left","iso")):
    fl.hide_render = ctr[2] < 0
    cd.ortho_scale = scale
    for name in views:
        if name == "front":
            cam.location = Vector(ctr) + Vector((0,-8,0)); cam.rotation_euler = (math.radians(90),0,0)
        elif name == "left":
            cam.location = Vector(ctr) + Vector((-8,0,0)); cam.rotation_euler = (math.radians(90),0,math.radians(-90))
        else:
            cam.location = Vector(ctr) + Vector((-5.0,-5.0,3.4))
            cam.rotation_euler = (math.radians(64),0,math.radians(-45))
        sc.render.filepath = os.path.join(OUT, "pose_%s_%s.png" % (tag, name))
        bpy.ops.render.render(write_still=True)
    b = P.bbox()
    print("POSE %-10s x %+.2f..%+.2f  y %+.2f..%+.2f  z %+.2f..%+.2f  (w %.2f d %.2f h %.2f)"
          % (tag, b[0],b[1],b[2],b[3],b[4],b[5], b[1]-b[0], b[3]-b[2], b[5]-b[4]))

P.reset(); P.hooks_home(); P.set_ik(ceiling=1.0); P.cling(fold=1.0)
shot("folded", (0,0,-0.85), 2.6)
for f in (0.6, 0.8, 1.0, 1.2):
    P.reset(); P.hooks_home(); P.set_ik(ceiling=1.0); P.cling(fold=f)
    b = P.bbox()
    print("  fold %.1f  top %+.3f  bottom %+.3f  h %.2f w %.2f" % (f, b[5], b[4], b[5]-b[4], b[1]-b[0]))

P.reset(); P.hooks_home(); P.set_ik(ceiling=1.0); P.cling(fold=0.0, wrap=0.0)
shot("cling0", (0,0,-1.3), 3.4, ("front",))

P.reset(); P.hooks_home(); P.set_ik(ceiling=1.0); P.hang(reach=0.45, spread=0.5, gape=26)
shot("hang", (0,0,-1.3), 3.4, ("front","left"))

P.reset(); P.hooks_home(); P.stance()
shot("stance", (0,0.0,0.75), 3.0)
for n in ("DEF_DigitsGroup.R","DEF_CeilLower.RR","DEF_Head","DEF_AttackFore.R","DEF_AttackUpper.R"):
    print("  %-22s tail %s" % (n, tuple(round(v,3) for v in P.bone_tail(n))))
