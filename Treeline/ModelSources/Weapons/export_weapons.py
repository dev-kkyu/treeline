"""Standalone weapon props for the level, out of the same originals the
viewmodel uses.

    blender -b --python prep_weapons.py            # rebuilds Weapons.blend
    blender <scratch>/Weapons.blend -b --python export_weapons.py

Writes into `Assets/Game/Player/Models/`:

    Pistol.fbx        frame + slide + magazine, assembled
    Shotgun.fbx       receiver + pump + tube fill, assembled
    PistolMag.fbx     a loose magazine
    ShotgunShell.fbx  a loose shell

These are the *pickups* - what lies on a table in chapter 3 - as opposed to the
rigged pieces inside `Protagonist_Arms.fbx`, which are split so the slide and
the pump can move. Both come from `prep_weapons.py`, so a gun on the floor and
the same gun in your hands are the same mesh at the same scale with the same
texture. Building the props from the raw Meshy files instead would have put a
different size and a different atlas on the table.

IMPORTANT: the origin is not re-centred. `09-weapons.md` put it on the grip web
for the weapons, the floorplate for the magazine and the brass base for the
shell, and those are the points the hands are posed against - so a pickup's
origin is already "where it is held", which is what an equip has to line up.
"""
import bpy
import bmesh
import os
import sys
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
UNITY = os.path.abspath(os.path.join(HERE, "..", "..",
                                     "Assets", "Game", "Player", "Models"))

# The magazine sits in the well at this offset from the grip web - the same
# number build_arms.py parks the Magazine bone at, so the seated round in the
# prop is where the rig's magazine is.
MAG_SEAT = Vector((0, -0.020, -0.083))

# piece -> the material name Unity already has a .mat for. Meshy names them
# after its own export, and the arms rig renames them on the way in; the props
# have to agree or the two end up on different materials.
MATERIAL = {
    "Pistol": "M_Pistol", "Slide": "M_Pistol", "PistolMag": "M_PistolMag",
    "Shotgun": "M_Shotgun", "Pump": "M_Shotgun", "TubeFill": "M_TubeFill",
    "Shell": "M_Shell",
}

# fbx name -> (pieces, {piece: offset})
PICKUPS = [
    ("Pistol", ["Pistol", "Slide", "PistolMag"], {"PistolMag": MAG_SEAT}),
    ("Shotgun", ["Shotgun", "Pump", "TubeFill"], {}),
    ("PistolMag", ["PistolMag"], {}),
    ("ShotgunShell", ["Shell"], {}),
]


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def copy_piece(name, offset):
    src = bpy.data.objects[name]
    ob = src.copy()
    ob.data = src.data.copy()
    ob.name = "_" + name
    bpy.context.scene.collection.objects.link(ob)
    if offset is not None and offset.length > 0:
        for v in ob.data.vertices:
            v.co += offset
    for m in ob.data.materials:
        if m is not None:
            m.name = MATERIAL[name]
    return ob


def join(name, pieces, offsets):
    """One object per pickup: a prop with three renderers is three draw calls
    and three things for a designer to get wrong."""
    made = [copy_piece(p, offsets.get(p)) for p in pieces]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in made:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = made[0]
    if len(made) > 1:
        bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    ob.data.name = name
    return ob


def main():
    os.makedirs(UNITY, exist_ok=True)
    for name, pieces, offsets in PICKUPS:
        for o in [o for o in bpy.data.objects if o.name.startswith("_")]:
            bpy.data.objects.remove(o, do_unlink=True)
        ob = join(name, pieces, offsets)
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        path = os.path.join(UNITY, name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=path,
            use_selection=True,
            # UNITS, not NONE. Both come out the right size, but NONE writes
            # the file in centimetres and leaves Unity to undo it with a 100x
            # scale on the prefab root - which then shows up in every physics
            # and lightmap question the level asks later.
            apply_scale_options='FBX_SCALE_UNITS',
            object_types={'MESH'},
            mesh_smooth_type='FACE',
            add_leaf_bones=False,
            bake_anim=False,
            axis_forward='-Z',
            axis_up='Y',
            path_mode='STRIP',
            embed_textures=False,
        )
        co = [v.co for v in ob.data.vertices]
        mats = [m.name for m in ob.data.materials if m]
        print("PICKUP %-13s tris %5d  mats %-28s  "
              "x %+.3f..%+.3f y %+.3f..%+.3f z %+.3f..%+.3f  %.2f MB"
              % (name, tri_count(ob), ",".join(mats),
                 min(c.x for c in co), max(c.x for c in co),
                 min(c.y for c in co), max(c.y for c in co),
                 min(c.z for c in co), max(c.z for c in co),
                 os.path.getsize(path) / 1048576.0))
        bpy.data.objects.remove(ob, do_unlink=True)


if __name__ == "__main__":
    main()
