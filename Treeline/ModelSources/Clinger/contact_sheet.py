"""Tile keyframes of every clip into one image per clip.

    blender <scratch>/Clinger_Anim.blend -b --python contact_sheet.py

Numbers pass while the picture is wrong: a clip can measure clean and still have
the head inside the torso. This is the check that catches that.
"""
import bpy, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mathutils import Vector
import poses as P

OUT = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad"
CELL = 300

SHEETS = [
    ("Clinger_Cling_Folded", [1, 14, 28, 40, 52, 66, 78, 91], (0, 0, -0.8), 2.6, False),
    ("Clinger_Cling_Tremor", [1, 3, 7, 11, 17, 19, 25, 40], (0, 0, -0.9), 2.8, False),
    ("Clinger_Unfold_Drop", [1, 4, 8, 10, 13, 16, 18, 24], (0, 0, -1.3), 3.4, False),
    ("Clinger_Land", [1, 3, 5, 9, 12, 15, 18, 24], (0, 0, 0.8), 3.2, True),
    ("Clinger_Scuttle", [1, 4, 7, 10, 13, 16, 19, 25], (0, 0, 0.7), 3.0, True),
    ("Clinger_Attack_Underjaw", [1, 5, 9, 11, 13, 16, 20, 26], (0, -0.2, 0.7), 3.4, True),
    ("Clinger_Hit", [1, 3, 5, 7, 9, 11, 12, 14], (0, 0, 0.7), 3.0, True),
    ("Clinger_Death_Fall", [1, 3, 5, 7, 9, 11, 14, 18], (0, 0, -1.1), 3.2, False),
    ("Clinger_Death", [1, 4, 7, 12, 18, 24, 31, 36], (0, 0, 0.6), 3.2, True),
]

sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x = sc.render.resolution_y = CELL
sc.render.image_settings.file_format = 'PNG'
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.10, 0.11, 0.12, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.5
cd = bpy.data.cameras.new("C"); cam = bpy.data.objects.new("C", cd)
sc.collection.objects.link(cam); sc.camera = cam; cd.type = 'ORTHO'
ld = bpy.data.lights.new("L", 'SUN'); ld.energy = 3.5
lo = bpy.data.objects.new("L", ld); sc.collection.objects.link(lo)
lo.rotation_euler = (math.radians(58), 0, math.radians(30))
gm = bpy.data.meshes.new("floor")
gm.from_pydata([(-6,-6,0),(6,-6,0),(6,6,0),(-6,6,0)], [], [(0,1,2,3)])
fl = bpy.data.objects.new("floor", gm); sc.collection.objects.link(fl)
fm = bpy.data.materials.new("fm"); fm.diffuse_color = (0.24,0.25,0.26,1)
fl.data.materials.append(fm)


def tile(paths, cols, path):
    """Stitch the cells into one strip so a sheet costs one look, not eight."""
    imgs = [bpy.data.images.load(p) for p in paths]
    rows = (len(imgs) + cols - 1) // cols
    out = bpy.data.images.new("sheet", CELL * cols, CELL * rows)
    buf = [0.0] * (CELL * cols * CELL * rows * 4)
    for i, im in enumerate(imgs):
        px = list(im.pixels)
        cx, cy = (i % cols) * CELL, (rows - 1 - i // cols) * CELL
        for y in range(CELL):
            src = y * CELL * 4
            dst = ((cy + y) * CELL * cols + cx) * 4
            buf[dst:dst + CELL * 4] = px[src:src + CELL * 4]
    out.pixels = buf
    out.filepath_raw = path
    out.file_format = 'PNG'
    out.save()


for name, frames, ctr, scale, floor in SHEETS:
    act = bpy.data.actions[name]
    P.arm.animation_data.action = act
    fl.hide_render = not floor
    cd.ortho_scale = scale
    cam.location = Vector(ctr) + Vector((-5.0, -5.0, 3.2))
    cam.rotation_euler = (math.radians(64), 0, math.radians(-45))
    paths = []
    for f in frames:
        sc.frame_set(f)
        sc.render.filepath = os.path.join(OUT, "_cell_%02d.png" % f)
        bpy.ops.render.render(write_still=True)
        paths.append(sc.render.filepath)
    tile(paths, 4, os.path.join(OUT, "sheet_%s.png" % name))
    print("SHEET", name, frames)
