"""Ship the rigs to Unity.

    blender Protagonist_Arms_Anim.blend -b --python export_fbx.py
    blender Protagonist_Body_Anim.blend -b --python export_fbx.py

Writes into the Unity project, following architecture.md's layout - the
protagonist and the weapons are A's, so they go under `Assets/Game/Player/`:

    Models/    Protagonist_Arms.fbx · Protagonist_Body.fbx
    Textures/  <Name>_BaseColor.png · <Name>_Normal.png

The textures come out of the blend rather than being pointed at: Meshy packed
them into the file and they exist nowhere else. Only base colour and normal -
decisions.md drops metallic/roughness/emission and covers them with a flat
metallic 0 · smoothness 0.25 in the material, because the generated values vary
per asset and make the set look like it came from six different games.

The FBX carries no textures itself (`path_mode='STRIP'`). Embedding 2048 maps
would double a file that is already committed, and Unity binds the materials by
name through the importer anyway.
"""
import bpy
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
UNITY = os.path.abspath(os.path.join(HERE, "..", "..",
                                     "Assets", "Game", "Player"))
MODELS = os.path.join(UNITY, "Models")
TEXTURES = os.path.join(UNITY, "Textures")

# armature name -> (fbx name, which objects go with it)
TARGETS = {
    "Arms_Armature": "Protagonist_Arms",
    "Protagonist_Armature": "Protagonist_Body",
}

# material -> the name its textures are written under. Several materials share
# an atlas, so the file name follows the material, not the image datablock.
TEX_NAME = {
    "M_Protagonist": "Protagonist",
    "M_Pistol": "Pistol",
    "M_PistolMag": "PistolMag",
    "M_Shotgun": "Shotgun",
    "M_TubeFill": "TubeFill",
    "M_Shell": "Shell",
}


def feeding(mat, socket):
    """The image datablock feeding a Principled socket, through a Normal Map."""
    if not mat.use_nodes:
        return None
    bsdf = next((n for n in mat.node_tree.nodes
                 if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None or socket not in bsdf.inputs:
        return None
    link = bsdf.inputs[socket].links
    if not link:
        return None
    node = link[0].from_node
    if node.type == 'NORMAL_MAP':
        c = node.inputs["Color"].links
        node = c[0].from_node if c else None
    return node.image if node is not None and node.type == 'TEX_IMAGE' else None


def save(image, path):
    if image is None:
        return None
    if os.path.exists(path):
        return path
    old = image.filepath_raw, image.file_format
    image.filepath_raw = path
    image.file_format = 'PNG'
    image.save()
    image.filepath_raw, image.file_format = old
    return path


def main():
    arm = next((o for o in bpy.data.objects
                if o.type == 'ARMATURE' and o.name in TARGETS), None)
    assert arm is not None, "no known armature in this file"
    name = TARGETS[arm.name]
    os.makedirs(MODELS, exist_ok=True)
    os.makedirs(TEXTURES, exist_ok=True)

    meshes = [o for o in bpy.data.objects
              if o.type == 'MESH' and o.find_armature() is arm]
    print("EXPORT %s  armature %s  meshes %s"
          % (name, arm.name, [o.name for o in meshes]))

    seen = set()
    for ob in meshes:
        for slot in ob.material_slots:
            mat = slot.material
            if mat is None or mat.name in seen:
                continue
            seen.add(mat.name)
            tag = TEX_NAME.get(mat.name, mat.name.replace("M_", ""))
            base = save(feeding(mat, "Base Color"),
                        os.path.join(TEXTURES, tag + "_BaseColor.png"))
            norm = save(feeding(mat, "Normal"),
                        os.path.join(TEXTURES, tag + "_Normal.png"))
            print("TEX    %-16s base %-6s normal %-6s"
                  % (mat.name, "ok" if base else "-", "ok" if norm else "-"))

    bpy.ops.object.select_all(action='DESELECT')
    arm.select_set(True)
    for ob in meshes:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = arm

    path = os.path.join(MODELS, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options='FBX_SCALE_NONE',
        object_types={'ARMATURE', 'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        # keeps the control bones out: they do not deform and have no deforming
        # children, while Root / ViewModelRoot do and so survive
        use_armature_deform_only=True,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        # 0 means keep every baked key. The clips are already one key a frame and
        # simplifying them is how a 25 mm slide stroke becomes a 9 mm one.
        bake_anim_simplify_factor=0.0,
        axis_forward='-Z',
        axis_up='Y',
        path_mode='STRIP',
        embed_textures=False,
    )
    size = os.path.getsize(path)
    acts = sorted(a.name for a in bpy.data.actions)
    print("WROTE  %s  %.1f MB  %d actions" % (path, size / 1048576.0, len(acts)))
    for a in acts:
        print("TAKE   %s|%s" % (arm.name, a))


if __name__ == "__main__":
    main()
