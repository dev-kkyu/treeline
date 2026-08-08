# -*- coding: utf-8 -*-
"""Treeline 적 3종 블록아웃 시안 — 4방향 정투영 렌더.

docs/enemy-sheets/*.png 의 미터 수치를 그대로 좌표로 쓴다. 형태를 확정하기 전의
시안이므로 디테일을 넣지 않고 덩어리와 실루엣만 맞춘다.

축 규약(시트와 동일) — Blender Z-up, 모델은 **-Y 를 본다.**
FBX 기본 내보내기(Up: Y, Forward: -Z)가 Blender -Y 를 Unity +Z 로 보내므로,
이렇게 모델링해 두면 변환이 한 번에 끝나고 상쇄 보정이 생기지 않는다.

    blender -b --python blockout.py -- <출력폴더>
"""
import bpy, bmesh, math, os, sys
from math import pi, cos, sin

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "."

# ---------------------------------------------------------------- 팔레트
def lin(hx):
    def c(v):
        v /= 255.0
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return (c(int(hx[0:2], 16)), c(int(hx[2:4], 16)), c(int(hx[4:6], 16)), 1.0)


PAL = {
    "flesh_light": lin("8C8279"),
    "flesh": lin("6E645C"),
    "flesh_dark": lin("4E463F"),
    "keratin": lin("B9AFA2"),
    "keratin_root": lin("2E2A27"),
    "mucosa": lin("7A3F3C"),
    "stage": lin("2A3038"),
}

MATS = {}


def mat(key):
    if key in MATS:
        return MATS[key]
    m = bpy.data.materials.new(key)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = PAL[key]
    bsdf.inputs["Roughness"].default_value = 0.7
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.0
    m.diffuse_color = PAL[key]          # Workbench 가 읽는 값
    MATS[key] = m
    return m


# ---------------------------------------------------------------- 벡터
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def mul(a, s): return (a[0] * s, a[1] * s, a[2] * s)
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2],
                         a[0] * b[1] - a[1] * b[0])


def norm(a):
    L = math.sqrt(a[0] ** 2 + a[1] ** 2 + a[2] ** 2) or 1.0
    return (a[0] / L, a[1] / L, a[2] / L)


# ---------------------------------------------------------------- 프리미티브
def dome_ends(pts, radii):
    """양 끝을 돔으로 닫는다. 평면 팬으로 닫으면 cavity 셰이딩이 뾰족한 주름을 만든다."""
    pts, radii = list(pts), list(radii)
    d0 = norm(sub(pts[1], pts[0]))
    r0 = (radii[0][0] + radii[0][1]) / 2.0
    pts = [add(pts[0], mul(d0, -0.60 * r0)), add(pts[0], mul(d0, -0.33 * r0))] + pts
    radii = [(radii[0][0] * 0.34, radii[0][1] * 0.34),
             (radii[0][0] * 0.78, radii[0][1] * 0.78)] + radii
    d1 = norm(sub(pts[-1], pts[-2]))
    r1 = (radii[-1][0] + radii[-1][1]) / 2.0
    pts = pts + [add(pts[-1], mul(d1, 0.33 * r1)), add(pts[-1], mul(d1, 0.60 * r1))]
    radii = radii + [(radii[-1][0] * 0.78, radii[-1][1] * 0.78),
                     (radii[-1][0] * 0.34, radii[-1][1] * 0.34)]
    return pts, radii


def tube(name, pts, radii, key="flesh", segs=12, smooth=True, dome=True):
    """관절 경로를 따라 타원 단면을 스윕한다. 몸통·팔다리·머리 전부 이걸로 만든다."""
    if dome:
        pts, radii = dome_ends(pts, radii)
    n = len(pts)
    verts, faces = [], []

    # 링마다 기준 벡터를 새로 고르면 인접 링에서 180° 뒤집혀 면이 꼬인다.
    # 첫 프레임만 정하고 그 뒤는 접선 변화만큼 회전시켜 이어받는다(평행이송).
    tang = []
    for i in range(n):
        if i == 0:
            d = sub(pts[1], pts[0])
        elif i == n - 1:
            d = sub(pts[-1], pts[-2])
        else:
            d = sub(pts[i + 1], pts[i - 1])
        tang.append(norm(d))
    d0 = tang[0]
    ref = (0.0, 0.0, 1.0) if abs(d0[2]) < 0.9 else (0.0, 1.0, 0.0)
    frames = [norm(cross(d0, ref))]
    for i in range(1, n):
        a, b = tang[i - 1], tang[i]
        ax = cross(a, b)
        s = math.sqrt(ax[0] ** 2 + ax[1] ** 2 + ax[2] ** 2)
        v = frames[-1]
        if s < 1e-7:
            frames.append(v)
            continue
        ax = (ax[0] / s, ax[1] / s, ax[2] / s)
        ang = math.atan2(s, a[0] * b[0] + a[1] * b[1] + a[2] * b[2])
        ca, sa = cos(ang), sin(ang)
        dt = ax[0] * v[0] + ax[1] * v[1] + ax[2] * v[2]
        rot = add(add(mul(v, ca), mul(cross(ax, v), sa)), mul(ax, dt * (1 - ca)))
        frames.append(norm(rot))

    for i, p in enumerate(pts):
        d = tang[i]
        side = frames[i]
        up2 = norm(cross(side, d))
        r1, r2 = radii[i]
        for k in range(segs):
            a = 2 * pi * k / segs
            verts.append(add(p, add(mul(side, r1 * cos(a)), mul(up2, r2 * sin(a)))))
    for i in range(n - 1):
        for k in range(segs):
            k2 = (k + 1) % segs
            faces.append([i * segs + k, i * segs + k2, (i + 1) * segs + k2, (i + 1) * segs + k])
    verts.append(pts[0]); c0 = len(verts) - 1
    verts.append(pts[-1]); c1 = len(verts) - 1
    for k in range(segs):
        k2 = (k + 1) % segs
        faces.append([c0, k2, k])
        faces.append([c1, (n - 1) * segs + k, (n - 1) * segs + k2])

    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.validate()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat(key))
    if smooth:
        for poly in ob.data.polygons:
            poly.use_smooth = True
    return ob


def blob(name, loc, scale, key="flesh", segs=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=segs // 2, location=loc)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mat(key))
    for poly in ob.data.polygons:
        poly.use_smooth = True
    return ob


def box(name, loc, scale, key="keratin", rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = tuple(s / 2.0 for s in scale)
    ob.data.materials.append(mat(key))
    return ob


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for blk in (bpy.data.meshes, bpy.data.objects, bpy.data.cameras, bpy.data.lights):
        for it in list(blk):
            if getattr(it, "users", 0) == 0:
                blk.remove(it)


def mirror_pts(pts):
    return [(-x, y, z) for x, y, z in pts]


# ================================================================ Crawler
# 전장 2.88 · 높이 0.98 · 보폭 1.52. 머리는 -Y 쪽(지면 0.3m), 척추는 +Y 로 끌린다.
def build_crawler():
    # 척추는 목에서 끊는다. 몸통과 머리를 한 튜브로 이으면 목이 안 생겨 머리가 묻힌다.
    S = [(0.0, 1.14, 0.04), (0.0, 0.94, 0.12), (0.0, 0.58, 0.27), (0.0, 0.18, 0.41),
         (0.0, -0.20, 0.46), (0.0, -0.55, 0.48), (0.0, -0.90, 0.46), (0.0, -1.12, 0.42),
         (0.0, -1.26, 0.36)]
    SR = [(0.02, 0.02), (0.05, 0.04), (0.09, 0.07), (0.17, 0.12), (0.28, 0.18),
          (0.31, 0.20), (0.26, 0.17), (0.16, 0.13), (0.10, 0.09)]
    tube("Crawler_Spine", S, SR, "flesh")

    # 머리 — 목 없이 앞아래로 뻗은 쐐기. 지면 0.3m 에 붙는다.
    tube("Crawler_Head",
         [(0.0, -1.24, 0.36), (0.0, -1.40, 0.33), (0.0, -1.58, 0.30),
          (0.0, -1.72, 0.27), (0.0, -1.79, 0.26)],
         [(0.10, 0.09), (0.16, 0.12), (0.17, 0.12), (0.12, 0.09), (0.05, 0.05)], "flesh")
    tube("Crawler_Jaw",
         [(0.0, -1.36, 0.23), (0.0, -1.56, 0.21), (0.0, -1.70, 0.23)],
         [(0.11, 0.055), (0.10, 0.05), (0.075, 0.05)], "flesh_dark", segs=8)

    # 등 융기 — 밝은 살. 손전등이 위에서 닿는 면이라 여기만 한 단 올린다.
    blob("Crawler_Ridge", (0.0, -0.42, 0.585), (0.20, 0.55, 0.075), "flesh_light")

    # 어깨 · 엉덩이 덩어리. 다리 뿌리에 덩어리가 없으면 관절 봉우리가 허공에서 솟는다.
    for sgn, tag in ((1, "R"), (-1, "L")):
        blob(f"Crawler_Shoulder_{tag}", (sgn * 0.15, -0.98, 0.50), (0.15, 0.20, 0.15), "flesh")
        blob(f"Crawler_Hip_{tag}", (sgn * 0.16, -0.30, 0.48), (0.17, 0.22, 0.16), "flesh")

    FRONT = [(0.22, -1.05, 0.56), (0.42, -0.93, 0.98), (0.62, -1.31, 0.30), (0.74, -1.53, 0.02)]
    FR = [(0.075, 0.075), (0.062, 0.062), (0.045, 0.045), (0.032, 0.032)]
    HIND = [(0.24, -0.35, 0.55), (0.46, -0.11, 0.88), (0.62, -0.03, 0.24), (0.72, 0.15, 0.02)]
    HR = [(0.085, 0.085), (0.062, 0.062), (0.045, 0.045), (0.032, 0.032)]
    for sgn, tag in ((1, "R"), (-1, "L")):
        pts = FRONT if sgn > 0 else mirror_pts(FRONT)
        tube(f"Crawler_ArmF_{tag}", pts, FR, "flesh")
        box(f"Crawler_ClawF_{tag}", (sgn * 0.76, -1.60, 0.04), (0.10, 0.22, 0.07), "keratin")
        pts = HIND if sgn > 0 else mirror_pts(HIND)
        tube(f"Crawler_ArmH_{tag}", pts, HR, "flesh")
        box(f"Crawler_ClawH_{tag}", (sgn * 0.73, 0.20, 0.04), (0.10, 0.22, 0.07), "keratin")
    return dict(name="crawler", res=(1040, 568), ortho=3.35, cz=0.52, ceiling=None)


# ================================================================ Colossus
# 신장 4.50 · 어깨폭 2.64 · 깊이 2.10. 최고점은 어깨 쌍봉, 머리는 그 사이 골.
def build_colossus():
    # 등이 굽어야 한다. 가슴은 앞(-Y)으로 밀고 윗등은 뒤로 넘겨 S 를 만든다.
    # 몸통 자체는 폭 1.7m 로 좁게 두고, 어깨폭 2.64m 는 쌍봉이 만든다.
    S = [(0.0, 0.10, 1.88), (0.0, 0.02, 2.45), (0.0, -0.08, 3.05), (0.0, 0.06, 3.66),
         (0.0, 0.26, 4.10), (0.0, 0.38, 4.32)]
    SR = [(0.72, 0.55), (0.84, 0.62), (0.92, 0.62), (0.86, 0.51), (0.70, 0.46), (0.38, 0.26)]
    tube("Col_Torso", S, SR, "flesh", segs=16)

    # 쌍봉이 실루엣의 최고점(4.50)이고 어깨 바깥선(±1.32)이다.
    for sgn, tag in ((1, "R"), (-1, "L")):
        blob(f"Col_Hump_{tag}", (sgn * 1.00, 0.18, 4.26), (0.32, 0.42, 0.24), "flesh_light")

    # 머리는 쌍봉보다 낮고(3.96 < 4.50) 앞으로 0.45m 나온다. 얼굴은 만들지 않는다.
    tube("Col_Neck", [(0.0, -0.20, 3.78), (0.0, -0.44, 3.70)],
         [(0.26, 0.24), (0.28, 0.26)], "flesh", segs=12)
    blob("Col_Head", (0.0, -0.60, 3.66), (0.33, 0.30, 0.30), "flesh")

    # IMPORTANT: 경로의 y 가 앞뒤로 왕복하면 튜브 단면이 뒤집혀 주름이 생긴다.
    # 팔은 한 방향으로만 흐르게 잡는다.
    ARM = [(1.02, 0.14, 4.00), (1.22, -0.10, 3.40), (1.38, -0.34, 2.70),
           (1.40, -0.46, 2.00), (1.36, -0.56, 1.30), (1.32, -0.66, 0.60),
           (1.30, -0.72, 0.34)]
    AR = [(0.24, 0.24), (0.235, 0.235), (0.225, 0.225), (0.21, 0.21), (0.195, 0.195),
          (0.175, 0.175), (0.16, 0.16)]
    LEG = [(0.52, 0.06, 1.95), (0.62, 0.20, 1.30), (0.68, 0.02, 0.60), (0.80, -0.08, 0.14)]
    LR = [(0.30, 0.30), (0.28, 0.28), (0.24, 0.24), (0.21, 0.21)]
    for sgn, tag in ((1, "R"), (-1, "L")):
        tube(f"Col_Arm_{tag}", ARM if sgn > 0 else mirror_pts(ARM), AR, "flesh", segs=12)
        box(f"Col_Hand_{tag}", (sgn * 1.32, -0.80, 0.20), (0.42, 0.78, 0.38), "keratin_root")
        tube(f"Col_Leg_{tag}", LEG if sgn > 0 else mirror_pts(LEG), LR, "flesh", segs=12)
        box(f"Col_Foot_{tag}", (sgn * 0.82, -0.26, 0.13), (0.66, 0.80, 0.26), "keratin_root")
    return dict(name="colossus", res=(740, 1036), ortho=3.9, cz=2.30, ceiling=None)


# ================================================================ Clinger
# 늘어진 2.51 · 몸통 0.85 × 0.48 · 갈고리 폭 1.10. 천장 3.40, 손끝 0.89.
def build_clinger():
    # 몸통은 목에서 끊고 머리는 갈라진 두 덩어리로 둔다 — 실루엣에서 갈라짐이 읽힌다.
    S = [(0.0, 0.0, 2.84), (0.0, 0.0, 2.72), (0.0, 0.0, 2.42), (0.0, 0.0, 2.12),
         (0.0, 0.0, 1.99)]
    SR = [(0.10, 0.09), (0.20, 0.18), (0.24, 0.22), (0.17, 0.16), (0.11, 0.10)]
    tube("Cli_Body", S, SR, "flesh", segs=14)
    for sgn, tag in ((1, "R"), (-1, "L")):
        blob(f"Cli_Head_{tag}", (sgn * 0.065, -0.01, 1.80), (0.085, 0.115, 0.135), "flesh")
    # 배가 밝은 면이다 — 아래에서 올려다보는 적이라 다른 둘과 반대로 잡는다.
    blob("Cli_Belly", (0.0, -0.13, 2.44), (0.11, 0.075, 0.22), "flesh_light")

    GRIP = [(0.40, 0.40, 3.38), (0.34, 0.34, 3.02), (0.14, 0.14, 2.82)]
    GR = [(0.05, 0.05), (0.045, 0.045), (0.04, 0.04)]
    for sx in (1, -1):
        for sy in (1, -1):
            pts = [(sx * x, sy * y, z) for x, y, z in GRIP]
            tube(f"Cli_Grip_{sx}{sy}", pts, GR, "flesh", segs=8)
            box(f"Cli_Hook_{sx}{sy}", (sx * 0.44, sy * 0.44, 3.36), (0.07, 0.07, 0.10), "keratin")

    ARM = [(0.19, 0.0, 2.18), (0.33, -0.10, 1.66), (0.23, 0.05, 1.16), (0.17, 0.0, 0.93)]
    AR = [(0.036, 0.036), (0.031, 0.031), (0.026, 0.026), (0.022, 0.022)]
    for sgn, tag in ((1, "R"), (-1, "L")):
        tube(f"Cli_Arm_{tag}", ARM if sgn > 0 else mirror_pts(ARM), AR, "flesh", segs=8)
        box(f"Cli_Claw_{tag}", (sgn * 0.17, -0.01, 0.90), (0.05, 0.05, 0.09), "keratin")
    return dict(name="clinger", res=(740, 1036), ortho=2.55, cz=2.10, ceiling=3.40)


# ---------------------------------------------------------------- 무대 · 렌더
def stage(cfg):
    """지면(또는 천장)을 얇은 판으로 둔다. 평면은 정투영 측면에서 안 보인다."""
    z = 0.0 if cfg["ceiling"] is None else cfg["ceiling"]
    sgn = -1 if cfg["ceiling"] is None else 1
    ob = box("Stage", (0, 0, z + sgn * 0.04), (60, 60, 0.08), "stage")
    return ob


VIEWS = [("01-front", (0, -1), (pi / 2, 0, 0)),
         ("02-right", (-1, 0), (pi / 2, 0, -pi / 2)),
         ("03-left", (1, 0), (pi / 2, 0, pi / 2)),
         ("04-back", (0, 1), (pi / 2, 0, pi))]


def bbox():
    """Stage 를 뺀 전체 바운딩박스. 프레이밍을 손으로 잡으면 매번 어긋난다."""
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for ob in bpy.context.scene.objects:
        if ob.type != "MESH" or ob.name == "Stage":
            continue
        for c in ob.bound_box:
            w = ob.matrix_world @ __import__("mathutils").Vector(c)
            for i in range(3):
                lo[i] = min(lo[i], w[i])
                hi[i] = max(hi[i], w[i])
    return lo, hi


def render(cfg):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x, sc.render.resolution_y = cfg["res"]
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"      # 알파 채널이 필요 없다
    sc.render.image_settings.compression = 100       # 기본 15% 라 파일이 커진다
    sc.render.film_transparent = False

    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_shadows = True
    sh.shadow_intensity = 0.45
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.curvature_ridge_factor = 1.0
    sh.curvature_valley_factor = 1.0
    sh.background_type = "VIEWPORT"
    sh.background_color = (0.030, 0.036, 0.046)
    sc.display.render_aa = "32"

    lo, hi = bbox()
    # 지면(천장)선이 프레임에 들어오게 한 뼘 넓힌다.
    if cfg["ceiling"] is None:
        lo[2] = min(lo[2], -0.06)
    else:
        hi[2] = max(hi[2], cfg["ceiling"] + 0.06)
    ctr = [(lo[i] + hi[i]) / 2.0 for i in range(3)]

    rx, ry = cfg["res"]
    aspect = rx / float(ry)
    need = 0.0
    for _tag, d, _rot in VIEWS:
        sx = 1 if d[0] == 0 else 0          # 화면 가로에 대응하는 월드 축
        vw = hi[sx] - lo[sx]
        vh = hi[2] - lo[2]
        need = max(need, max(vw, vh * aspect) if aspect >= 1 else max(vh, vw / aspect))
    ortho = need * 1.14

    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam

    folder = os.path.join(OUT, cfg["name"])
    os.makedirs(folder, exist_ok=True)
    D = 30.0
    for tag, d, rot in VIEWS:
        cam.location = (ctr[0] if d[0] == 0 else d[0] * D,
                        ctr[1] if d[1] == 0 else d[1] * D,
                        ctr[2])
        cam.rotation_euler = rot
        sc.render.filepath = os.path.join(folder, tag + ".png")
        bpy.ops.render.render(write_still=True)
    print(f"FIT {cfg['name']} ortho={ortho:.2f} center={[round(v,2) for v in ctr]} "
          f"bbox={[round(v,2) for v in lo]}~{[round(v,2) for v in hi]}")


for builder in (build_crawler, build_colossus, build_clinger):
    clear()
    MATS.clear()
    cfg = builder()
    stage(cfg)
    render(cfg)

print("DONE")
