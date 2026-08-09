"""Export Clinger.fbx + textures into the Unity project.

    blender <scratch>/Clinger_Anim.blend -b --python export.py

Follows the Crawler export so the two enemies land in the same place with the
same importer assumptions. Textures come out of the blend because Meshy packs
them into the file and they exist nowhere else.
"""
import bpy
import os

PROJ = r"D:\새 폴더\놀이터\treeline"
MODELS = os.path.join(PROJ, r"Treeline\Assets\Game\Enemy\Models")
TEXTURES = os.path.join(PROJ, r"Treeline\Assets\Game\Enemy\Textures")
SOURCE = os.path.join(PROJ, r"Treeline\ModelSources\Clinger")
for d in (MODELS, TEXTURES, SOURCE):
    os.makedirs(d, exist_ok=True)

bpy.context.scene.render.fps = 30
bpy.context.scene.render.fps_base = 1.0

arm = bpy.data.objects["Clinger_Armature"]
mesh = bpy.data.objects["Clinger_Mesh"]
mat = mesh.material_slots[0].material

SOCKET_NAME = {
    "Base Color": "BaseColor", "Normal": "Normal", "Roughness": "Roughness",
    "Metallic": "Metallic", "Emission Color": "Emission",
    "Specular IOR Level": "Specular",
}
img_role = {}
bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)


def trace(socket, depth=0):
    if not socket.is_linked or depth > 4:
        return None
    node = socket.links[0].from_node
    if node.type == 'TEX_IMAGE':
        return node
    for inp in node.inputs:
        found = trace(inp, depth + 1)
        if found:
            return found
    return None


if bsdf:
    for name, tag in SOCKET_NAME.items():
        if name in bsdf.inputs:
            node = trace(bsdf.inputs[name])
            if node and node.image:
                img_role.setdefault(node.image.name, tag)

for img in bpy.data.images:
    if img.size[0] == 0:
        continue
    tag = img_role.get(img.name, img.name)
    path = os.path.join(TEXTURES, "Clinger_%s.png" % tag)
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    img.filepath = path
    img.source = 'FILE'
    if img.packed_file:
        img.unpack(method='REMOVE')
    print("TEX %-28s -> %s  %dx%d" % (img.name, os.path.basename(path), *img.size))

mat.name = "M_Clinger"

bpy.ops.object.select_all(action='DESELECT')
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm

fbx = os.path.join(MODELS, "Clinger.fbx")
bpy.ops.export_scene.fbx(
    filepath=fbx,
    use_selection=True,
    object_types={'ARMATURE', 'MESH'},
    apply_unit_scale=True,
    global_scale=1.0,
    apply_scale_options='FBX_SCALE_NONE',
    axis_forward='-Z',
    axis_up='Y',
    use_space_transform=True,
    bake_space_transform=False,
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    add_leaf_bones=False,
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    use_armature_deform_only=True,
    armature_nodetype='NULL',
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    # 0 keeps every baked key. Simplifying is how a 45 mm swallow becomes a
    # 12 mm one and the idle goes back to looking like a prop.
    bake_anim_simplify_factor=0.0,
    path_mode='STRIP',
)
print("FBX %s  %.2f MB" % (fbx, os.path.getsize(fbx) / 1048576.0))

for a in sorted(bpy.data.actions, key=lambda a: a.name):
    print("ACTION %-28s frames %d..%d" % (a.name, a.frame_start, a.frame_end))

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE, "Clinger_Rig.blend"))
print("SOURCE", os.path.join(SOURCE, "Clinger_Rig.blend"))
