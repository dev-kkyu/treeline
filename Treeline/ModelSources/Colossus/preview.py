"""Render every action in the baked rig to a PNG sequence for eyeballing.

    blender Colossus_Rig.blend -b --python preview.py

Turn them into GIFs afterwards with ffmpeg; nothing here goes into the game.
"""
import bpy
import os
import sys

OUT_DIR = r"C:\Users\gofla\AppData\Local\Temp\claude\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad\preview"

VIEWS = {
    "q": ((-7.4, -10.2, 4.6), (0, 0, 2.10), 50),    # three-quarter front
    "s": ((10.8, -0.6, 3.2), (0, -0.2, 2.10), 50),  # side, shows the gait
    "f": ((0.0, -12.0, 3.0), (0, 0, 2.10), 50),     # head on, silhouette width
}

only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

arm = bpy.data.objects["Colossus_Armature"]
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'SINGLE'
sc.display.shading.single_color = (0.62, 0.63, 0.66)
sc.display.shading.show_cavity = True
sc.render.resolution_x = sc.render.resolution_y = 560
sc.render.image_settings.file_format = 'PNG'
sc.world = sc.world or bpy.data.worlds.new("W")
sc.world.color = (0.09, 0.10, 0.12)

target = bpy.data.objects.new("CamTarget", None)
sc.collection.objects.link(target)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
sc.collection.objects.link(cam)
c = cam.constraints.new('TRACK_TO')
c.target = target
c.track_axis = 'TRACK_NEGATIVE_Z'
c.up_axis = 'UP_Y'
sc.camera = cam

# a floor, so a foot going through it is obvious
bpy.ops.mesh.primitive_plane_add(size=24, location=(0, 0, 0))

for act in bpy.data.actions:
    if only and act.name not in only:
        continue
    # additive clips only key the upper body, so without this the legs and arms
    # keep whatever the previous action left them in
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0, 0, 0)
    arm.animation_data.action = act
    for tag, (pos, tgt, lens) in VIEWS.items():
        cam.location = pos
        target.location = tgt
        cam.data.lens = lens
        d = os.path.join(OUT_DIR, act.name + "_" + tag)
        os.makedirs(d, exist_ok=True)
        for i, f in enumerate(range(int(act.frame_start), int(act.frame_end) + 1)):
            sc.frame_set(f)
            sc.render.filepath = os.path.join(d, "%03d.png" % i)
            bpy.ops.render.render(write_still=True)
        print("SEQ", d, i + 1)
