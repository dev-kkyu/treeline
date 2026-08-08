"""Export Crawler.glb (all six clips) for viewing in any glTF viewer.

    blender Crawler_Rig.blend -b --python export_glb.py

Lives outside Assets/ on purpose: Unity would import a .glb as a second model
and the project would carry two copies of the same creature.
"""
import bpy
import os

OUT = os.path.join(r"D:\새 폴더\놀이터\treeline", r"Treeline\ModelSources\Crawler", "Crawler.glb")

bpy.context.scene.render.fps = 30
bpy.context.scene.render.fps_base = 1.0

arm = bpy.data.objects["Crawler_Armature"]
mesh = bpy.data.objects["Crawler_Mesh"]

# textures must be resolvable for the GLB to embed them
for img in bpy.data.images:
    if img.size[0] and not img.packed_file:
        p = bpy.path.abspath(img.filepath)
        if os.path.exists(p):
            img.pack()
            print("PACK", img.name)
        else:
            print("MISSING", img.name, p)

bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm

bpy.ops.export_scene.gltf(
    filepath=OUT,
    export_format='GLB',
    use_selection=True,
    export_yup=True,
    export_apply=False,
    export_animations=True,
    export_animation_mode='ACTIONS',
    export_nla_strips=False,
    export_bake_animation=True,
    export_force_sampling=True,
    export_frame_range=False,
    export_anim_slide_to_zero=True,
    export_optimize_animation_size=False,
    export_skins=True,
    export_def_bones=False,
    export_morph=False,
    export_image_format='AUTO',
)
print("GLB", OUT, os.path.getsize(OUT))
