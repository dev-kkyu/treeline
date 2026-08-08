"""Bring the four Meshy weapon models to game scale, orient them, split the
moving parts, and set each origin where the rig needs it.

    blender -b --python prep_weapons.py

Writes Weapons.blend with one object per piece. Numbers come from
docs/character-sheets/mockups/codex/final/animation/09-weapons.md

Convention after this script: muzzle points -Y, up is +Z, and the origin of a
weapon is its grip web - the notch between thumb and index finger. Everything
downstream poses hands against that point, so it has to be the same idea on
both weapons.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix

RAW = r"C:\Users\gofla\Downloads"
SCRATCH = (r"C:\Users\gofla\AppData\Local\Temp\claude"
           r"\D-----------treeline\333f0c0c-3b0b-4771-9a80-63bba730d450\scratchpad")
OUT = os.path.join(SCRATCH, "Weapons.blend")

# file, name, target length on the longest axis, triangle target
PARTS = [
    ("Meshy_AI_A_compact_modern_serv_0808201926_texture.blend", "Pistol", 0.190, 8752),
    ("Meshy_AI_A_single_stack_pistol_0808201932_texture.blend", "PistolMag", 0.105, 1200),
    ("Meshy_AI_A_short_pump_action_1_0808201939_texture.blend", "Shotgun", 0.760, 9000),
    ("Meshy_AI_A_single_12_gauge_sho_0808201943_texture.blend", "Shell", 0.068, 600),
]
# the magazine comes out almost square in section and will not fit a 34.7 mm
# grip, so its two short axes get squashed on top of the uniform scale
MAG_SQUASH = {1: 0.922, 2: 0.611}

# raw orientation, read off the slice measurements:
#   Pistol / Shotgun  long axis X, muzzle at -X, up already +Z
#   PistolMag         long axis X, floorplate at -X
#   Shell             long axis Z, brass base at -Z
ORIENT = {
    "Pistol": Matrix.Rotation(math.radians(90), 4, 'Z'),
    "Shotgun": Matrix.Rotation(math.radians(90), 4, 'Z'),
    "PistolMag": Matrix.Rotation(math.radians(-90), 4, 'Y'),
    "Shell": Matrix.Rotation(math.radians(90), 4, 'X'),
}

# after orientation: -Y is forward. Cut boxes are in that oriented space.
# Pistol slide: everything above the frame seam and forward of the backstrap.
SLIDE_BOX = (Vector((-0.030, -0.100, 0.030)), Vector((0.030, 0.090, 0.080)))
# Shotgun pump: the ribbed sleeve under the barrel. Taking the magazine tube
# with it is fine - TubeFill below covers the gap it leaves.
PUMP_BOX = (Vector((-0.030, -0.310, -0.012)), Vector((0.030, -0.085, 0.058)))

# grip web - the notch between thumb and index finger, in oriented space
GRIP_WEB = {
    "Pistol": Vector((0.0, 0.082, 0.012)),
    "Shotgun": Vector((0.0, 0.262, -0.005)),
}


def bounds(ob):
    co = [ob.matrix_world @ v.co for v in ob.data.vertices]
    return (Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co))),
            Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co))))


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def apply_transform(ob):
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def decimate(ob, target):
    have = tri_count(ob)
    if have <= target * 1.05:
        return have
    m = ob.modifiers.new("dec", 'DECIMATE')
    m.ratio = float(target) / have
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)
    return tri_count(ob)


def load_piece(fname, name, target_len, tris):
    path = os.path.join(RAW, fname)
    with bpy.data.libraries.load(path) as (src, dst):
        dst.objects = list(src.objects)
    ob = None
    for o in dst.objects:
        if o and o.type == 'MESH':
            bpy.context.scene.collection.objects.link(o)
            ob = o
            break
    ob.name = name
    ob.data.name = name

    lo, hi = bounds(ob)
    size = hi - lo
    order = sorted(range(3), key=lambda i: -size[i])
    k = target_len / size[order[0]]
    factor = [k, k, k]
    if name == "PistolMag":
        for rank, extra in MAG_SQUASH.items():
            factor[order[rank]] = k * extra
    ob.scale = factor
    apply_transform(ob)
    got = decimate(ob, tris)

    ob.matrix_world = ORIENT[name] @ ob.matrix_world
    apply_transform(ob)
    lo, hi = bounds(ob)
    print("PIECE %-10s  x %.4f  y %.4f  z %.4f  tris %d"
          % (name, hi.x - lo.x, hi.y - lo.y, hi.z - lo.z, got))
    return ob


def split_box(ob, name, box, planes=()):
    """Move every face fully inside the box into a new object.

    The box test alone leaves a torn edge: a face straddling the boundary stays
    behind, so the seam comes out as sawteeth. Cutting the mesh on the boundary
    plane first gives a straight edge on both pieces.
    """
    lo, hi = box
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    for co, no in planes:
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
                               plane_co=co, plane_no=no,
                               clear_inner=False, clear_outer=False)
    bm.faces.ensure_lookup_table()
    eps = 1e-5
    lo = lo - Vector((eps, eps, eps))
    hi = hi + Vector((eps, eps, eps))
    inside = [f for f in bm.faces
              if all(lo.x <= v.co.x <= hi.x and lo.y <= v.co.y <= hi.y
                     and lo.z <= v.co.z <= hi.z for v in f.verts)]
    if not inside:
        bm.free()
        print("SPLIT %-10s NOTHING inside the box" % name)
        return None
    part = bmesh.new()
    uv_src = bm.loops.layers.uv.active
    uv_dst = part.loops.layers.uv.new(uv_src.name) if uv_src else None
    vmap = {}
    for f in inside:
        vs = []
        for v in f.verts:
            if v not in vmap:
                vmap[v] = part.verts.new(v.co)
            vs.append(vmap[v])
        try:
            nf = part.faces.new(vs)
        except ValueError:
            continue
        if uv_dst:
            for lsrc, ldst in zip(f.loops, nf.loops):
                ldst[uv_dst].uv = lsrc[uv_src].uv
    me2 = bpy.data.meshes.new(name)
    part.to_mesh(me2)
    part.free()
    ob2 = bpy.data.objects.new(name, me2)
    bpy.context.scene.collection.objects.link(ob2)
    for m in ob.data.materials:
        ob2.data.materials.append(m)
    bmesh.ops.delete(bm, geom=inside, context='FACES')
    bm.to_mesh(me)
    bm.free()
    print("SPLIT %-10s took %d tris, %d left on %s"
          % (name, tri_count(ob2), tri_count(ob), ob.name))
    return ob2


def set_origin(ob, pivot):
    for v in ob.data.vertices:
        v.co -= pivot
    ob.location = (0, 0, 0)


def tube_fill(ob, y0, y1, centre_z, radius):
    """A plain cylinder that keeps the magazine tube from showing a hole when
    the pump slides back."""
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius,
                                        depth=(y1 - y0), location=(0, 0, 0))
    fill = bpy.context.active_object
    fill.name = "TubeFill"
    fill.rotation_euler = (math.radians(90), 0, 0)
    fill.location = (0, (y0 + y1) * 0.5, centre_z)
    apply_transform(fill)
    # its own flat dark material: the cylinder has primitive UVs and would
    # otherwise sample a random patch of the shotgun atlas
    mat = bpy.data.materials.new("M_TubeFill")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.045, 0.048, 0.052, 1.0)
        bsdf.inputs["Metallic"].default_value = 0.7
        bsdf.inputs["Roughness"].default_value = 0.45
    fill.data.materials.append(mat)
    print("FILL   TubeFill  y %.3f..%.3f  r %.3f  tris %d"
          % (y0, y1, radius, tri_count(fill)))
    return fill


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    made = {}
    for fname, name, length, tris in PARTS:
        made[name] = load_piece(fname, name, length, tris)

    made["Slide"] = split_box(made["Pistol"], "Slide", SLIDE_BOX,
                              planes=[(Vector((0, 0, 0.030)), Vector((0, 0, 1)))])
    made["Pump"] = split_box(made["Shotgun"], "Pump", PUMP_BOX,
                             planes=[(Vector((0, -0.310, 0)), Vector((0, 1, 0))),
                                     (Vector((0, -0.085, 0)), Vector((0, 1, 0)))])
    made["TubeFill"] = tube_fill(made["Shotgun"], -0.315, -0.085, 0.030, 0.011)

    # both weapons and their moving parts share one origin, so a part keeps its
    # place when the parent moves
    for weapon, parts in (("Pistol", ("Slide",)), ("Shotgun", ("Pump", "TubeFill"))):
        web = GRIP_WEB[weapon]
        for n in (weapon,) + parts:
            if made.get(n):
                set_origin(made[n], web)
    # loose props stand on their own origin: the magazine on its floorplate,
    # the shell on its brass base
    for n, pick in (("PistolMag", lambda lo, hi: Vector((0, 0, lo.z))),
                    ("Shell", lambda lo, hi: Vector((0, hi.y, 0)))):
        lo, hi = bounds(made[n])
        set_origin(made[n], pick(lo, hi))

    for n, ob in sorted(made.items()):
        if not ob:
            continue
        lo, hi = bounds(ob)
        print("FINAL %-10s  x %+.3f..%+.3f  y %+.3f..%+.3f  z %+.3f..%+.3f  tris %d"
              % (n, lo.x, hi.x, lo.y, hi.y, lo.z, hi.z, tri_count(ob)))

    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    print("SAVED", OUT)


main()
