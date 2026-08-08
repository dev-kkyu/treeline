# -*- coding: utf-8 -*-
"""Treeline 적 3종 모델링 시트 작도.

정투영 3면도 + 역광 실루엣 검수 시트를 SVG 로 그려 HTML 로 감싼다.
헤드리스 Chrome 이 PNG 로 굽는다.

이름과 경로는 docs/tech-design/architecture.md 의 계약을 따른다 —
기는 것 Crawler · 큰 것 Colossus · 매달린 것 Clinger.
"""
import os, sys, math

OUT = sys.argv[1]

BG, PANEL, PANEL_E = "#0d1116", "#141a21", "#243040"
GRID, GRID_M = "#1c2531", "#2b3a4a"
INK, INK_2 = "#dae4ef", "#5d7086"
FILL, FILL_2 = "#212c39", "#19212a"
ACC, ACC_2 = "#e0603f", "#4d9be0"
HUMAN, TXT, TXT_DIM = "#37475a", "#c6d2df", "#7c8b9c"
LITE = "#e8ecf1"
OK_G = "#63c184"

FONT = "'Malgun Gothic','Segoe UI',sans-serif"
MONO = "'Consolas','Courier New',monospace"

W, M = 1800, 46
R = W - M                       # 1754
PY, PH = 104, 500               # 1행
GY = PY + 430                   # 지면
BY, BH = 620, 300               # 2행
Y3, H3 = 936, 380               # 3행
Y4, H4 = 1332, 344              # 4행 — 머티리얼 · 리그 · 콜라이더
SHEET_H = 1706

# ---------------------------------------------------------------- 공용 팔레트
# 세 적이 같은 팔레트를 쓴다. 구분은 색이 아니라 형태다 — 역광에서 색은 안 보인다.
PAL = [
    ("#8C8279", "살 · 밝은", "등 · 어깨 · 관절 융기. 손전등이 먼저 닿는 면"),
    ("#6E645C", "살 · 기본", "몸통 대부분"),
    ("#4E463F", "살 · 음영", "틈새 · 배 · 겨드랑 · 관절 안쪽"),
    ("#B9AFA2", "각질 · 팁", "발톱 · 이빨 · 갈고리 끝. 어둠에서 유일하게 밝다"),
    ("#2E2A27", "각질 · 뿌리", "발톱 · 이빨 뿌리 · 발바닥"),
    ("#7A3F3C", "점막", "구강 · 갈라진 머리 안쪽. 붉은색은 여기만 쓴다"),
    ("#3A4A46", "유기물 데코", "4장 발전동 벽 · 천장. 적이 아니지만 팔레트를 맞춘다"),
]

SURFACE = [("살", "0.0", "0.30", "1.0"), ("각질", "0.0", "0.45", "0.8"),
           ("점막", "0.0", "0.65", "0.6")]


# ---------------------------------------------------------------- 기본 작도
def smooth(pts, close=True):
    n = len(pts)
    if n < 3:
        return "M " + " L ".join(f"{x:.1f} {y:.1f}" for x, y in pts)
    d = [f"M {pts[0][0]:.1f} {pts[0][1]:.1f}"]
    for i in range(n if close else n - 1):
        p0 = pts[(i - 1) % n] if close else pts[max(i - 1, 0)]
        p1, p2 = pts[i], pts[(i + 1) % n] if close else pts[min(i + 1, n - 1)]
        p3 = pts[(i + 2) % n] if close else pts[min(i + 2, n - 1)]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6.0, p1[1] + (p2[1] - p0[1]) / 6.0)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6.0, p2[1] - (p3[1] - p1[1]) / 6.0)
        d.append(f"C {c1[0]:.1f} {c1[1]:.1f} {c2[0]:.1f} {c2[1]:.1f} {p2[0]:.1f} {p2[1]:.1f}")
    return " ".join(d) + (" Z" if close else "")


class View:
    def __init__(self, cx, gy, s):
        self.cx, self.gy, self.s = cx, gy, s

    def p(self, x, z):
        return (self.cx + x * self.s, self.gy - z * self.s)

    def pts(self, ms):
        return [self.p(x, z) for x, z in ms]


def body(v, ms, sil=False, far=False, w=2.4):
    fill = "#000" if sil else (FILL_2 if far else FILL)
    ink = "#000" if sil else (INK_2 if far else INK)
    return (f'<path d="{smooth(v.pts(ms))}" fill="{fill}" stroke="{ink}" stroke-width="{w}" '
            f'stroke-linejoin="round"/>')


def limb(v, ms, wm, sil=False, far=False):
    """관절 부위. 몸통과 같게 어두운 채움 + 밝은 외곽선으로 보이도록 두 번 긋는다."""
    pt = " ".join(f"{x:.1f},{y:.1f}" for x, y in v.pts(ms))
    sw = wm * v.s
    if sil:
        return (f'<polyline points="{pt}" fill="none" stroke="#000" stroke-width="{sw+3:.1f}" '
                f'stroke-linecap="round" stroke-linejoin="round"/>')
    ink = INK_2 if far else INK
    fill = FILL_2 if far else FILL
    return (f'<polyline points="{pt}" fill="none" stroke="{ink}" stroke-width="{sw+4.4:.1f}" '
            f'stroke-linecap="round" stroke-linejoin="round"/>'
            f'<polyline points="{pt}" fill="none" stroke="{fill}" stroke-width="{sw:.1f}" '
            f'stroke-linecap="round" stroke-linejoin="round"/>')


def taper(v, ms, w0, w1, sil=False):
    P = v.pts(ms)
    n = len(P)
    up, dn = [], []
    for i, (x, y) in enumerate(P):
        hw = (w0 + (w1 - w0) * (i / (n - 1.0))) * v.s / 2.0
        if i == 0:
            dx, dy = P[1][0] - x, P[1][1] - y
        elif i == n - 1:
            dx, dy = x - P[-2][0], y - P[-2][1]
        else:
            dx, dy = P[i + 1][0] - P[i - 1][0], P[i + 1][1] - P[i - 1][1]
        L = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / L * hw, dx / L * hw
        up.append((x + nx, y + ny))
        dn.append((x - nx, y - ny))
    fill = "#000" if sil else FILL
    ink = "#000" if sil else INK
    return (f'<path d="{smooth(up + dn[::-1])}" fill="{fill}" stroke="{ink}" '
            f'stroke-width="2.2" stroke-linejoin="round"/>')


def mir(pts, sgn):
    return [(sgn * x, z) for x, z in pts]


def swapxy(pts):
    return [(y, x) for x, y in pts]


def flipy(pts):
    return [(x, -y) for x, y in pts]


# ---------------------------------------------------------------- 패널
def panel(x, y, w, h, title, sub=""):
    s = (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="4" fill="{PANEL}" '
         f'stroke="{PANEL_E}" stroke-width="1.5"/>'
         f'<text x="{x+16}" y="{y+27}" font-family="{FONT}" font-size="17" font-weight="700" '
         f'fill="{TXT}" letter-spacing="1.2">{title}</text>')
    if sub:
        s += (f'<text x="{x+w-16}" y="{y+27}" text-anchor="end" font-family="{MONO}" '
              f'font-size="12.5" fill="{TXT_DIM}">{sub}</text>')
    return s + (f'<line x1="{x}" y1="{y+40}" x2="{x+w}" y2="{y+40}" stroke="{PANEL_E}" '
                f'stroke-width="1.5"/>')


def lite(x, y, w, h, title, sub=""):
    s = (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="4" fill="{LITE}" '
         f'stroke="{PANEL_E}" stroke-width="1.5"/>'
         f'<text x="{x+16}" y="{y+28}" font-family="{FONT}" font-size="16.5" font-weight="700" '
         f'fill="#1a2027">{title}</text>')
    if sub:
        s += (f'<text x="{x+w-16}" y="{y+28}" text-anchor="end" font-family="{FONT}" '
              f'font-size="13" fill="#5a6672">{sub}</text>')
    return s + (f'<line x1="{x}" y1="{y+42}" x2="{x+w}" y2="{y+42}" stroke="#c6cfd8" '
                f'stroke-width="1.5"/>')


_N = [0]


def clip(x, y, w, h, top=41):
    _N[0] += 1
    t = f"cl{_N[0]}"
    return (f'<clipPath id="{t}"><rect x="{x+1}" y="{y+top}" width="{w-2}" height="{h-top-1}"/>'
            f'</clipPath><g clip-path="url(#{t})">')


def grid(x, y, w, h, v, step=0.5, ceiling=None, ground=True):
    g = [clip(x, y, w, h)]
    k = -90
    while k <= 90:
        m = k * step
        px, _ = v.p(m, 0)
        if x < px < x + w:
            big = abs(m - round(m)) < 1e-6
            g.append(f'<line x1="{px:.1f}" y1="{y+41}" x2="{px:.1f}" y2="{y+h-1}" '
                     f'stroke="{GRID_M if big else GRID}" stroke-width="{1.2 if big else 1}"/>')
        _, py = v.p(0, m)
        if y + 41 < py < y + h:
            big = abs(m - round(m)) < 1e-6
            g.append(f'<line x1="{x+1}" y1="{py:.1f}" x2="{x+w-1}" y2="{py:.1f}" '
                     f'stroke="{GRID_M if big else GRID}" stroke-width="{1.2 if big else 1}"/>')
            if big and abs(m) > 0.01:
                g.append(f'<text x="{x+w-8}" y="{py-5:.1f}" text-anchor="end" font-family="{MONO}" '
                         f'font-size="11" fill="{TXT_DIM}" opacity="0.7">{m:.0f}m</text>')
        k += 1
    if ceiling is not None:
        _, cy = v.p(0, ceiling)
        g.append(f'<rect x="{x+1}" y="{cy-10:.1f}" width="{w-2}" height="10" fill="{ACC}" '
                 f'opacity="0.15"/>'
                 f'<line x1="{x+1}" y1="{cy:.1f}" x2="{x+w-1}" y2="{cy:.1f}" stroke="{ACC}" '
                 f'stroke-width="2.5" opacity="0.8"/>'
                 f'<text x="{x+w-12}" y="{cy-13:.1f}" text-anchor="end" font-family="{MONO}" '
                 f'font-size="12" fill="{ACC}">천장 {ceiling:.1f}m</text>')
    if ground:
        _, gy = v.p(0, 0)
        g.append(f'<line x1="{x+1}" y1="{gy:.1f}" x2="{x+w-1}" y2="{gy:.1f}" stroke="#78889a" '
                 f'stroke-width="2.5"/>')
    return "".join(g) + "</g>"


def dim_h(v, x0, x1, z, label, up=True):
    a, ay = v.p(x0, z)
    b, _ = v.p(x1, z)
    t = f'<g stroke="{ACC}" stroke-width="1.4" fill="{ACC}">'
    t += f'<line x1="{a:.1f}" y1="{ay:.1f}" x2="{b:.1f}" y2="{ay:.1f}"/>'
    for px in (a, b):
        t += f'<line x1="{px:.1f}" y1="{ay-5:.1f}" x2="{px:.1f}" y2="{ay+5:.1f}"/>'
    return t + (f'<text x="{(a+b)/2:.1f}" y="{ay-9 if up else ay+18:.1f}" text-anchor="middle" '
                f'font-family="{MONO}" font-size="13" stroke="none">{label}</text></g>')


def dim_v(v, z0, z1, x, label, left=True):
    ax, a = v.p(x, z0)
    _, b = v.p(x, z1)
    t = f'<g stroke="{ACC}" stroke-width="1.4" fill="{ACC}">'
    t += f'<line x1="{ax:.1f}" y1="{a:.1f}" x2="{ax:.1f}" y2="{b:.1f}"/>'
    for py in (a, b):
        t += f'<line x1="{ax-5:.1f}" y1="{py:.1f}" x2="{ax+5:.1f}" y2="{py:.1f}"/>'
    return t + (f'<text x="{ax + (-9 if left else 9):.1f}" y="{(a+b)/2+5:.1f}" '
                f'text-anchor="{"end" if left else "start"}" font-family="{MONO}" font-size="13" '
                f'stroke="none">{label}</text></g>')


def marker(v, mx, mz, n):
    x, y = v.p(mx, mz)
    return (f'<g><circle cx="{x:.1f}" cy="{y:.1f}" r="10.5" fill="{ACC}"/>'
            f'<text x="{x:.1f}" y="{y+4.5:.1f}" text-anchor="middle" font-family="{MONO}" '
            f'font-size="12.5" font-weight="700" fill="#0d1116">{n}</text></g>')


def legend(x, y, items, size=12.5, lh=17):
    o = []
    for i, s in enumerate(items):
        yy = y + i * lh
        o.append(f'<circle cx="{x+7}" cy="{yy-4}" r="7.6" fill="none" stroke="{ACC}" '
                 f'stroke-width="1.3"/>'
                 f'<text x="{x+7}" y="{yy}" text-anchor="middle" font-family="{MONO}" '
                 f'font-size="10.5" fill="{ACC}">{i+1}</text>'
                 f'<text x="{x+21}" y="{yy}" font-family="{FONT}" font-size="{size}" '
                 f'fill="{TXT_DIM}">{s}</text>')
    return "".join(o)


def note(x, y, lines, color=TXT_DIM, size=13, lh=21, weight="400"):
    return "".join(
        f'<text x="{x}" y="{y + i*lh}" font-family="{FONT}" font-size="{size}" '
        f'font-weight="{weight}" fill="{color}" xml:space="preserve">{s}</text>'
        for i, s in enumerate(lines))


def scalebar(x, y, s, meters=0.5, color=TXT_DIM):
    w = s * meters
    return (f'<g stroke="{color}" stroke-width="2"><line x1="{x}" y1="{y}" x2="{x+w:.1f}" y2="{y}"/>'
            f'<line x1="{x}" y1="{y-5}" x2="{x}" y2="{y+5}"/>'
            f'<line x1="{x+w:.1f}" y1="{y-5}" x2="{x+w:.1f}" y2="{y+5}"/>'
            f'<text x="{x+w+8:.1f}" y="{y+5}" font-family="{MONO}" font-size="12" fill="{color}" '
            f'stroke="none">{meters:g}m</text></g>')


def up_arrow(v, x, z0, z1, label="+Z 전방"):
    ax, a = v.p(x, z0)
    _, b = v.p(x, z1)
    return (f'<g stroke="{ACC_2}" fill="{ACC_2}" stroke-width="1.8">'
            f'<line x1="{ax:.1f}" y1="{a:.1f}" x2="{ax:.1f}" y2="{b+9:.1f}"/>'
            f'<path d="M {ax:.1f} {b:.1f} L {ax-5.5:.1f} {b+11:.1f} L {ax+5.5:.1f} {b+11:.1f} Z" '
            f'stroke="none"/>'
            f'<text x="{ax+10:.1f}" y="{(a+b)/2+4:.1f}" font-family="{MONO}" font-size="12.5" '
            f'stroke="none">{label}</text></g>')


# ---------------------------------------------------------------- 사람 1.75m
HS = [(-0.10, 0.02), (0.13, 0.02), (0.11, 0.42), (0.13, 0.86), (0.16, 1.20), (0.14, 1.42),
      (0.10, 1.52), (0.13, 1.62), (0.08, 1.73), (-0.06, 1.73), (-0.11, 1.62), (-0.09, 1.50),
      (-0.16, 1.36), (-0.17, 1.10), (-0.13, 0.70), (-0.15, 0.40)]
HF = [(-0.06, 0.02), (0.06, 0.02), (0.06, 0.44), (0.09, 0.86), (0.22, 0.92), (0.26, 1.30),
      (0.22, 1.44), (0.10, 1.50), (0.09, 1.62), (0.05, 1.74), (-0.05, 1.74), (-0.09, 1.62),
      (-0.10, 1.50), (-0.22, 1.44), (-0.26, 1.30), (-0.22, 0.92), (-0.09, 0.86), (-0.06, 0.44)]


def human(v, x=0.0, front=False, color=HUMAN, label=True):
    pts = [(px + x, pz) for px, pz in (HF if front else HS)]
    s = f'<path d="{smooth(v.pts(pts))}" fill="{color}" stroke="none"/>'
    if label:
        tx, ty = v.p(x, 1.83)
        s += (f'<text x="{tx:.1f}" y="{ty:.1f}" text-anchor="middle" font-family="{MONO}" '
              f'font-size="12" fill="{color}">1.75m</text>')
    return s


# ================================================================ Crawler
C_BODY = [(0.16, 0.50), (0.20, 0.34), (0.45, 0.27), (0.85, 0.29), (1.10, 0.36), (1.23, 0.48),
          (1.06, 0.63), (0.70, 0.67), (0.36, 0.61)]
C_HEAD = [(1.20, 0.34), (1.24, 0.52), (1.46, 0.51), (1.70, 0.39), (1.74, 0.26), (1.54, 0.17),
          (1.30, 0.21)]
C_TAIL = [(0.22, 0.46), (-0.18, 0.41), (-0.58, 0.27), (-0.94, 0.12), (-1.14, 0.04)]
C_FL = [(1.05, 0.56), (0.93, 0.98), (1.31, 0.30), (1.53, 0.02)]
C_FL2 = [(0.99, 0.53), (0.85, 0.90), (1.17, 0.28), (1.35, 0.02)]
C_HL = [(0.35, 0.55), (0.11, 0.88), (0.03, 0.24), (-0.15, 0.02)]
C_HL2 = [(0.30, 0.52), (0.05, 0.80), (-0.03, 0.24), (-0.21, 0.02)]

CF_BODY = [(-0.29, 0.29), (-0.31, 0.52), (-0.18, 0.64), (0.0, 0.67), (0.18, 0.64), (0.31, 0.52),
           (0.29, 0.29), (0.0, 0.25)]
CF_HEAD = [(-0.21, 0.17), (-0.23, 0.39), (0.0, 0.48), (0.23, 0.39), (0.21, 0.17), (0.0, 0.11)]
CF_LEG = [(0.27, 0.56), (0.63, 0.97), (0.71, 0.30), (0.76, 0.02)]
CF_LEG2 = [(0.22, 0.52), (0.49, 0.85), (0.55, 0.28), (0.59, 0.02)]

CT_BODY = swapxy([(0.16, 0.10), (0.18, -0.11), (0.45, -0.26), (0.90, -0.28), (1.16, -0.20),
                  (1.26, 0.0), (1.16, 0.20), (0.90, 0.28), (0.45, 0.26), (0.18, 0.11)])
CT_HEAD = swapxy([(1.22, -0.18), (1.52, -0.17), (1.74, 0.0), (1.52, 0.17), (1.22, 0.18)])
CT_TAIL = swapxy([(0.22, 0.0), (-0.26, 0.03), (-0.70, -0.04), (-1.14, 0.0)])
CT_FL = swapxy([(1.04, 0.20), (1.22, 0.55), (1.50, 0.66)])
CT_HL = swapxy([(0.34, 0.20), (0.16, 0.56), (-0.12, 0.68)])


def crawler_side(v, sil=False):
    o = [limb(v, C_HL2, 0.085, sil, far=True), limb(v, C_FL2, 0.085, sil, far=True),
         taper(v, C_TAIL, 0.17, 0.03, sil), limb(v, C_HL, 0.10, sil),
         body(v, C_BODY, sil), limb(v, C_FL, 0.10, sil), body(v, C_HEAD, sil)]
    if not sil:
        o.append(f'<path d="{smooth(v.pts([(1.30,0.26),(1.54,0.235),(1.70,0.30)]), False)}" '
                 f'fill="none" stroke="{INK_2}" stroke-width="1.6"/>'
                 f'<path d="{smooth(v.pts([(0.40,0.62),(0.70,0.68),(1.02,0.64)]), False)}" '
                 f'fill="none" stroke="{INK_2}" stroke-width="1.3" stroke-dasharray="4 4"/>')
    return "".join(o)


def crawler_front(v, sil=False):
    o = [limb(v, mir(CF_LEG2, s), 0.085, sil, far=True) for s in (-1, 1)]
    o += [limb(v, mir(CF_LEG, s), 0.10, sil) for s in (-1, 1)]
    o += [body(v, CF_BODY, sil), body(v, CF_HEAD, sil)]
    return "".join(o)


def crawler_top(v, sil=False):
    o = [taper(v, CT_TAIL, 0.16, 0.03, sil)]
    for s in (-1, 1):
        o.append(limb(v, mir(CT_FL, s), 0.10, sil))
        o.append(limb(v, mir(CT_HL, s), 0.10, sil))
    o += [body(v, CT_BODY, sil), body(v, CT_HEAD, sil)]
    return "".join(o)


# ================================================================ Clinger
# 늘어진 자세는 천장이 높은 4장 발전동 기준이다. 2.6m 복도에 이대로 매달면
# 플레이어가 몸통에 정면으로 박으므로, 좁은 곳에서는 접은 대기 자세를 쓴다.
CEIL = 3.40
ZOFF = 0.80


def off(pts, dz=ZOFF):
    return [(x, z + dz) for x, z in pts]


G_BODY = off([(0.0, 2.02), (0.18, 1.96), (0.25, 1.62), (0.16, 1.28), (0.0, 1.17), (-0.15, 1.28),
              (-0.24, 1.60), (-0.20, 1.95)])
G_HEAD = off([(0.0, 1.19), (0.14, 1.10), (0.15, 0.95), (0.0, 0.84), (-0.15, 0.95), (-0.14, 1.10)])
G_GRIP_A = off([(0.08, 2.58), (0.34, 2.44), (0.33, 2.16), (0.12, 1.99)])
G_GRIP_B = off([(-0.06, 2.58), (-0.30, 2.42), (-0.31, 2.14), (-0.10, 1.99)])
G_ARM_A = off([(0.19, 1.36), (0.33, 0.86), (0.23, 0.36), (0.17, 0.13)])
G_ARM_B = off([(-0.17, 1.33), (-0.33, 0.80), (-0.22, 0.30), (-0.13, 0.09)])

GF_BODY = off([(0.0, 2.02), (0.21, 1.94), (0.24, 1.60), (0.15, 1.27), (0.0, 1.17), (-0.15, 1.27),
               (-0.24, 1.60), (-0.21, 1.94)])
GF_GRIP = off([(0.17, 2.58), (0.44, 2.42), (0.46, 2.12), (0.15, 1.99)])
GF_ARM = off([(0.20, 1.36), (0.38, 0.84), (0.26, 0.34), (0.19, 0.11)])

GT_BODY = [(0.0, -0.26), (0.20, -0.16), (0.24, 0.16), (0.0, 0.26), (-0.24, 0.16), (-0.20, -0.16)]
GT_GRIP = [(0.08, -0.14), (0.36, 0.06), (0.52, 0.32)]

L_TORSO = [(-0.16, 0.86), (0.10, 0.84), (0.24, 1.10), (0.20, 1.34), (0.02, 1.42), (-0.18, 1.32),
           (-0.24, 1.06)]
L_HEAD = [(0.22, 1.30), (0.40, 1.30), (0.46, 1.16), (0.34, 1.06), (0.20, 1.12)]
L_LEG = [(-0.04, 0.88), (0.28, 0.54), (0.06, 0.22), (0.16, 0.02)]
L_LEG2 = [(-0.10, 0.85), (-0.34, 0.52), (-0.20, 0.20), (-0.32, 0.02)]
L_ARM = [(0.06, 1.34), (0.46, 0.86), (0.58, 0.34), (0.62, 0.04)]
L_ARM2 = [(-0.06, 1.30), (-0.36, 0.84), (-0.30, 0.32), (-0.24, 0.04)]


def clinger_side(v, sil=False):
    return "".join([limb(v, G_GRIP_B, 0.075, sil, far=True), limb(v, G_ARM_B, 0.055, sil, far=True),
                    limb(v, G_GRIP_A, 0.085, sil), limb(v, G_ARM_A, 0.06, sil),
                    body(v, G_BODY, sil), body(v, G_HEAD, sil)])


def clinger_front(v, sil=False):
    o = []
    for s in (-1, 1):
        o.append(limb(v, mir(GF_GRIP, s), 0.085, sil))
        o.append(limb(v, mir(GF_ARM, s), 0.06, sil))
    o += [body(v, GF_BODY, sil), body(v, G_HEAD, sil)]
    return "".join(o)


def clinger_top(v, sil=False):
    o = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(limb(v, [(sx * x, sy * y) for x, y in GT_GRIP], 0.085, sil))
    return "".join(o) + body(v, GT_BODY, sil)


def clinger_land(v, sil=False):
    return "".join([limb(v, L_LEG2, 0.10, sil, far=True), limb(v, L_ARM2, 0.065, sil, far=True),
                    limb(v, L_LEG, 0.11, sil), body(v, L_TORSO, sil), limb(v, L_ARM, 0.07, sil),
                    body(v, L_HEAD, sil)])


# ================================================================ Colossus
K_TORSO = [(-0.12, 1.95), (0.42, 2.10), (0.58, 2.85), (0.48, 3.60), (0.16, 4.22), (-0.36, 4.48),
           (-0.76, 4.10), (-0.88, 3.05), (-0.78, 2.12)]
K_HEAD = [(0.28, 3.42), (0.44, 3.74), (0.72, 3.76), (0.86, 3.52), (0.76, 3.24), (0.44, 3.20)]
K_LEG = [(0.06, 2.05), (0.30, 1.18), (0.06, 0.52), (0.24, 0.10)]
K_LEG2 = [(-0.30, 2.02), (-0.48, 1.16), (-0.30, 0.50), (-0.46, 0.10)]
K_FOOT = [(-0.14, 0.02), (0.62, 0.02), (0.56, 0.26), (-0.10, 0.24)]
K_FOOT2 = [(-0.86, 0.02), (-0.16, 0.02), (-0.20, 0.24), (-0.82, 0.24)]
K_ARM = [(0.06, 4.05), (0.78, 2.62), (0.58, 1.12), (0.74, 0.34)]
K_ARM2 = [(-0.24, 3.98), (-0.64, 2.60), (-0.46, 1.10), (-0.34, 0.32)]
K_HAND = [(0.46, 0.04), (1.22, 0.10), (1.16, 0.44), (0.52, 0.38)]

KF_TORSO = [(0.0, 1.88), (0.78, 2.00), (1.08, 3.20), (1.32, 4.12), (0.96, 4.50), (0.36, 4.20),
            (0.0, 4.08), (-0.36, 4.20), (-0.96, 4.50), (-1.32, 4.12), (-1.08, 3.20), (-0.78, 2.00)]
KF_HEAD = [(0.0, 3.42), (0.29, 3.54), (0.32, 3.92), (0.0, 4.06), (-0.32, 3.92), (-0.29, 3.54)]
KF_LEG = [(0.56, 2.00), (0.66, 1.10), (0.74, 0.42), (0.86, 0.10)]
KF_FOOT = [(0.52, 0.02), (1.20, 0.02), (1.16, 0.28), (0.56, 0.28)]
KF_ARM = [(1.14, 4.18), (1.46, 2.70), (1.30, 1.20), (1.36, 0.34)]
KF_HAND = [(1.06, 0.04), (1.72, 0.10), (1.66, 0.44), (1.10, 0.38)]

KT_TORSO = flipy([(0.0, -0.72), (0.72, -0.62), (1.30, -0.18), (1.32, 0.34), (0.66, 0.72),
                  (0.0, 0.80), (-0.66, 0.72), (-1.32, 0.34), (-1.30, -0.18), (-0.72, -0.62)])
KT_HEAD = flipy([(0.0, -0.62), (0.30, -0.72), (0.34, -1.06), (0.0, -1.18), (-0.34, -1.06),
                 (-0.30, -0.72)])
KT_ARM = flipy([(1.16, 0.10), (1.60, -0.30), (1.56, -0.86)])


def col_side(v, sil=False):
    return "".join([limb(v, K_LEG2, 0.44, sil, far=True), body(v, K_FOOT2, sil, far=True),
                    limb(v, K_ARM2, 0.36, sil, far=True), limb(v, K_LEG, 0.48, sil),
                    body(v, K_FOOT, sil), body(v, K_TORSO, sil), body(v, K_HEAD, sil),
                    limb(v, K_ARM, 0.40, sil), body(v, K_HAND, sil)])


def col_front(v, sil=False):
    o = []
    for s in (-1, 1):
        o.append(limb(v, mir(KF_LEG, s), 0.50, sil))
        o.append(body(v, mir(KF_FOOT, s), sil))
    o += [body(v, KF_TORSO, sil), body(v, KF_HEAD, sil)]
    for s in (-1, 1):
        o.append(limb(v, mir(KF_ARM, s), 0.42, sil))
        o.append(body(v, mir(KF_HAND, s), sil))
    return "".join(o)


def col_top(v, sil=False):
    o = [limb(v, mir(KT_ARM, s), 0.42, sil) for s in (-1, 1)]
    return "".join(o) + body(v, KT_TORSO, sil) + body(v, KT_HEAD, sil)


# ---------------------------------------------------------------- 시트 골격
def head_band(title_kr, code, line, right1, right2):
    return (f'<rect x="0" y="0" width="{W}" height="86" fill="#111821"/>'
            f'<rect x="0" y="84" width="{W}" height="2" fill="{ACC}"/>'
            f'<text x="{M}" y="43" font-family="{FONT}" font-size="30" font-weight="800" '
            f'fill="{TXT}" letter-spacing="1">{title_kr}'
            f'<tspan font-family="{MONO}" font-size="19" font-weight="400" fill="{ACC}">'
            f' / {code}</tspan></text>'
            f'<text x="{M}" y="69" font-family="{FONT}" font-size="14" fill="{TXT_DIM}">{line}</text>'
            f'<text x="{W-M}" y="33" text-anchor="end" font-family="{MONO}" font-size="13" '
            f'fill="{TXT_DIM}">TREELINE · 적 모델링 시트 · rev 1 · 2026-08-08 · B 김도한</text>'
            f'<text x="{W-M}" y="56" text-anchor="end" font-family="{FONT}" font-size="14" '
            f'fill="{ACC_2}">{right1}</text>'
            f'<text x="{W-M}" y="76" text-anchor="end" font-family="{MONO}" font-size="12" '
            f'fill="{TXT_DIM}">{right2}</text>')


def chip(x, y, w, h, hex_, stroke="#000", op=1.0):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="2.5" fill="{hex_}" '
            f'stroke="{stroke}" stroke-width="1" opacity="{op}"/>')


def tbl(x, y, cols, rows, lh=22, size=12.5, head_color=ACC_2, body_color=TXT):
    """cols = [(dx, 제목, anchor)]  ·  rows = [[셀, ...]]"""
    o = []
    for dx, label, anc in cols:
        o.append(f'<text x="{x+dx}" y="{y}" text-anchor="{anc}" font-family="{FONT}" '
                 f'font-size="11.5" font-weight="700" fill="{head_color}" '
                 f'letter-spacing="0.4">{label}</text>')
    o.append(f'<line x1="{x}" y1="{y+7}" x2="{x+cols[-1][0]+30}" y2="{y+7}" '
             f'stroke="{PANEL_E}" stroke-width="1"/>')
    for i, row in enumerate(rows):
        yy = y + 26 + i * lh
        for (dx, _, anc), cell in zip(cols, row):
            bold = cell.startswith("*")
            txt = cell[1:] if bold else cell
            o.append(f'<text x="{x+dx}" y="{yy}" text-anchor="{anc}" font-family="{FONT}" '
                     f'font-size="{size}" font-weight="{"700" if bold else "400"}" '
                     f'fill="{body_color}">{txt}</text>')
    return "".join(o)


def row4(mat_rows, mat_note, bones, clips, phys, rig_sub="", mat_w=660, rig_w=480):
    """4행 — 머티리얼 · 리그·애니메이션 · 콜라이더·에이전트."""
    o = []
    x1 = M
    o.append(spec(x1, Y4, mat_w, H4, mat_rows,
                  "머티리얼  MATERIAL", "공용 팔레트 → material-guide.png"))
    sy = Y4 + H4 - 86
    o.append(f'<text x="{x1+18}" y="{sy-14}" font-family="{FONT}" font-size="12" '
             f'font-weight="700" fill="{ACC_2}">공용 팔레트 — 적 모델은 유기물 데코를 뺀 '
             f'<tspan fill="{TXT}">6색</tspan>만 쓴다</text>')
    cw = (mat_w - 36 - 6 * 7) / 7.0
    for i, (hx, _nm, _u) in enumerate(PAL):
        cx = x1 + 18 + i * (cw + 7)
        unused = (i == 6)
        o.append(chip(cx, sy, cw, 28, hx, "#0d1116", 0.3 if unused else 1.0))
        o.append(f'<text x="{cx+cw/2:.1f}" y="{sy+42}" text-anchor="middle" '
                 f'font-family="{MONO}" font-size="9.5" fill="{TXT_DIM}" '
                 f'opacity="{0.45 if unused else 1.0}">{hx[1:]}</text>')
    o.append(note(x1 + 18, sy + 62, [mat_note], TXT_DIM, 12.5, 17))

    x2 = x1 + mat_w + 16
    o.append(panel(x2, Y4, rig_w, H4, "리그 · 애니메이션",
                   rig_sub or f"30fps · {len(clips)}클립 · ○ 루프"))
    o.append(note(x2 + 18, Y4 + 64, bones, TXT_DIM, 11.8, 16))
    lh = 20 if len(clips) <= 6 else 18
    o.append(tbl(x2 + 18, Y4 + 68 + len(bones) * 16 + 12,
                 [(0, "클립", "start"), (rig_w - 196, "프레임", "end"),
                  (rig_w - 138, "초", "end"), (rig_w - 88, "루프", "middle"),
                  (rig_w - 30, "히트", "end")],
                 clips, lh=lh, size=12))

    x3 = x2 + rig_w + 16
    o.append(spec(x3, Y4, R - x3, H4, phys, "콜라이더 · 에이전트"))
    return "".join(o)


def spec(x, y, w, h, rows, title, sub=""):
    o = [panel(x, y, w, h, title, sub)]
    cy = y + 68
    for label, val in rows:
        if label == "--":
            o.append(f'<line x1="{x+18}" y1="{cy-15}" x2="{x+w-18}" y2="{cy-15}" '
                     f'stroke="{PANEL_E}" stroke-width="1"/>')
            cy += 8
            continue
        o.append(f'<text x="{x+18}" y="{cy}" font-family="{FONT}" font-size="13.5" '
                 f'font-weight="700" fill="{ACC_2}">{label}</text>'
                 f'<text x="{x+152}" y="{cy}" font-family="{FONT}" font-size="13.5" '
                 f'fill="{TXT}">{val}</text>')
        cy += 25
    return "".join(o)


def write(name, inner, h=SHEET_H):
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{h}" '
           f'viewBox="0 0 {W} {h}"><rect width="{W}" height="{h}" fill="{BG}"/>{inner}</svg>')
    with open(os.path.join(OUT, name + ".html"), "w", encoding="utf-8") as f:
        f.write("<!doctype html><meta charset='utf-8'><style>"
                f"html,body{{margin:0;padding:0;background:{BG};width:{W}px;height:{h}px;"
                "overflow:hidden}svg{display:block}</style>" + svg)
    print(f"{name}\t{W}x{h}")


AXIS_ROWS = [
    ("업 축", "Blender Z-up → Unity Y-up 변환은 FBX 내보내기에서 <tspan font-weight='700'>한 번만</tspan>"),
    ("전방 축", "모델 로컬 <tspan font-weight='700'>+Z</tspan> 가 전방. 평면도에서 머리가 +Z 를 본다"),
    ("금지", "리그 -90° / Root 본 +90° 처럼 서로 상쇄되는 보정을 넣지 않는다"),
    ("이유", "상쇄 구조는 정지 화면에서 정상으로 보이고 애니메이션이 돌면 눕는다."),
    ("", "R&D 에서 실제로 이 함정을 밟았고 끝내 못 고쳤다"),
    ("Avatar", "임포터 avatarSetup = CreateFromThisModel (Generic)"),
    ("원점", "발밑 바닥 · 좌우 중앙. 스케일 1.0, 미터 단위로 내보낸다"),
]


# ================================================================ 1 · Crawler
def sheet_crawler():
    o = [head_band("기는 것", "CRAWLER",
                   "네발로 낮게 깔려 빠르다. 수평 실루엣 — 낮은 몸통 위로 솟은 관절 봉우리 둘",
                   "등장 2 · 3장 · 권총 1~2발 · 빠르고 약하다 · 1단계",
                   "단위 m · 격자 0.5m · 정투영")]

    px, pw = M, 700
    v = View(px + 304, GY, 145)
    o += [panel(px, PY, pw, PH, "측면  SIDE"), grid(px, PY, pw, PH, v)]
    o.append(clip(px, PY, pw, PH))
    o.append(human(v, x=-1.85))
    o.append(crawler_side(v))
    o.append(dim_h(v, -1.14, 1.74, 1.30, "전장 2.88"))
    o.append(dim_v(v, 0, 0.98, 2.08, "0.98", left=False))
    o.append(dim_v(v, 0, 0.67, -1.32, "0.67", left=False))
    o.append(marker(v, 0.93, 1.03, 1))
    o.append(marker(v, 1.80, 0.28, 2))
    o.append(marker(v, -1.08, 0.06, 3))
    o.append("</g>")
    o.append(legend(px + 20, GY + 26, [
        "어깨·엉덩이 관절이 등 윤곽(0.67m)보다 높이 솟는다 — 실루엣의 핵",
        "머리는 목 없이 앞으로 뻗어 지면 0.3m 에 붙는다",
        "척추가 뒤로 1.0m 끌린다. 진행 방향이 실루엣만으로 읽힌다"]))
    o.append(scalebar(px + pw - 120, PY + 62, 145, 0.5))

    px2, pw2 = px + pw + 16, 480
    v2 = View(px2 + 290, GY, 150)
    o += [panel(px2, PY, pw2, PH, "정면  FRONT"), grid(px2, PY, pw2, PH, v2)]
    o.append(clip(px2, PY, pw2, PH))
    o.append(human(v2, x=-1.10, front=True))
    o.append(crawler_front(v2))
    o.append(dim_h(v2, -0.76, 0.76, 1.28, "보폭 1.52"))
    o.append(dim_h(v2, -0.31, 0.31, -0.24, "몸통폭 0.62", up=False))
    o.append("</g>")

    px3, pw3 = px2 + pw2 + 16, R - (px2 + pw2 + 16)
    v3 = View(px3 + 248, PY + 286, 130)
    o += [panel(px3, PY, pw3, PH, "평면  TOP", "전방 = 화면 위"),
          grid(px3, PY, pw3, PH, v3, ground=False)]
    o.append(clip(px3, PY, pw3, PH))
    o.append(crawler_top(v3))
    o.append(dim_h(v3, -0.70, 0.70, -1.45, "1.40", up=False))
    o.append(dim_v(v3, -1.14, 1.74, -1.05, "2.88"))
    o.append(up_arrow(v3, 1.02, 1.15, 1.80))
    o.append("</g>")

    o.append(lite(px, BY, pw, BH, "역광 검수  BACKLIT SILHOUETTE",
                  "순검정 · 측면과 정면 둘 다 판별되어야 한다"))
    o.append(clip(px, BY, pw, BH, 43))
    g2 = BY + BH - 46
    o.append(f'<line x1="{px+1}" y1="{g2}" x2="{px+pw-1}" y2="{g2}" stroke="#a8b3bf" stroke-width="1.6"/>')
    o.append(crawler_side(View(px + 232, g2, 118), sil=True))
    o.append(crawler_front(View(px + 566, g2, 118), sil=True))
    for cxx, lb in ((px + 232, "SIDE"), (px + 566, "FRONT")):
        o.append(f'<text x="{cxx}" y="{g2+22}" text-anchor="middle" font-family="{MONO}" '
                 f'font-size="12" fill="#5a6672">{lb}</text>')
    o.append(f'<text x="{px+18}" y="{BY+70}" font-family="{FONT}" font-size="13.5" '
             f'fill="#40505f">이 두 그림이 적 3종을 나눈 근거다. 모델이 나온 뒤 같은 판을 '
             f'다시 만들어 대조한다.</text>')
    o.append("</g>")

    sx, sw = px + pw + 16, R - (px + pw + 16)
    o.append(spec(sx, BY, sw, BH, [
        ("치수", "전장 2.88m (몸통+머리 1.90 / 척추 0.98) · 높이 0.98m · 보폭 1.52m"),
        ("자세", "상시 사족 보행. 직립 자세를 만들지 않는다"),
        ("--", ""),
        ("트라이앵글", "4,000 tris 상한 (LOD0) · LOD1 1,600 · LOD2 600 · 전환 12m / 28m"),
        ("본", "24개 — Root · Pelvis · Spine 3 · Neck · Head · Jaw · Tail 4 · 다리 4×3"),
        ("스키닝", "부위별 강체 웨이트(본 하나에 1.0). 자동 웨이트 금지"),
        ("텍스처", "1024² 아틀라스 1장 · URP Lit · Albedo + Normal 만"),
        ("--", ""),
        ("애니메이션", "Idle · Prowl(배회) · Chase(추격) · Attack · Hit · Death — 6종"),
        ("Animator", "Speed(float) · Attack(trigger) · Hit(trigger) · Dead(bool)"),
        ("루트", "applyRootMotion = false. 이동은 NavMeshAgent 가 담당한다"),
    ], "제작 규격  SPEC", "Assets/Game/Enemy/Crawler.prefab"))

    o.append(panel(px, Y3, pw, H3, "실루엣 설계 근거",
                   "기획서 「셋이 역광에서 검은 실루엣만 보여도 구분되는지」"))
    o.append(note(px + 18, Y3 + 70, [
        f"세 적은 <tspan font-weight='700' fill='{ACC}'>화면을 채우는 방향</tspan>으로 갈린다. "
        f"기는 것은 <tspan font-weight='700'>수평</tspan>이다.",
        "",
        "· 전장 : 높이 = 2.9 : 1.0. 어떤 거리에서도 가로로 긴 덩어리로 읽힌다",
        "· 몸통 윤곽 0.67m 위로 관절이 0.98m 까지 솟는다. 낮은 가로 막대 위의",
        "&#160;&#160;뾰족한 봉우리 둘 — 이 조합은 나머지 둘에 없다",
        "· 머리가 지면 0.3m 에 붙어 서 있는 형태와 절대 겹치지 않는다",
        "· 끌리는 척추가 뒤로 빠져 진행 방향을 알려준다",
        "",
        "복도에서 정면으로 마주치면 보폭 1.52m 가 통로 폭을 거의 채운다.",
        "「비켜 갈 수 없다」가 규칙 설명 없이 형태로 전달된다.",
        "",
        f"<tspan font-weight='700' fill='{ACC_2}'>먼저 만드는 것이 이 한 종이다.</tspan> "
        f"시트 → 생성 → 리깅 → 엔진 반영을",
        "끝까지 한 바퀴 돌려 파이프라인을 검증한 뒤 나머지 둘로 간다.",
    ], TXT_DIM, 13.5, 21))

    o.append(spec(sx, Y3, sw, H3, AXIS_ROWS + [
        ("--", ""),
        ("파이프라인", "① 이 시트 → ② MeshyAI (image-to-3D 참조로 시트를 넣는다) →"),
        ("", "③ Blender MCP 로 리깅·애니메이션 → ④ Unity MCP 로 프리팹·Animator"),
        ("기록", "생성 도구와 라이선스는 그 자리에서 docs/assets.md 에 적는다"),
    ], "축 규약  AXIS CONVENTION", "파이프라인 맨 앞에서 못 박고 뒤로는 손대지 않는다"))

    o.append(row4(
        [("슬롯", "1개 — M_Crawler_Body. 각질·점막은 같은 아틀라스의 다른 UV 영역이다"),
         ("셰이더", "URP Lit · Metallic Workflow · Opaque · Alpha Clipping 끔"),
         ("맵", "Albedo 1024² + Normal 1024². Metallic/Smoothness 맵은 쓰지 않는다"),
         ("표면값", "Metallic 0.0 · Smoothness 0.30 · Normal 1.0 · Env Reflections 끔"),
         ("--", ""),
         ("텍셀 밀도", "약 250 px/m. 전장 2.88m 에 1024² 한 장이면 충분하다"),
         ("아틀라스", "몸통·머리 좌상 512² · 다리·척추 우상 512² · 각질 좌하 512×256")],
        "밝은 살(8C8279)은 등과 관절 융기에만. 손전등이 위에서 내려오므로 그 두 면이 형태를 만든다.",
        ["Root · Pelvis · Spine01~03 · Neck · Head · Jaw",
         "Tail01~04 — 끌리는 척추",
         "L/R Femur · Tibia · Foot  (뒷다리)",
         "L/R Humerus · Radius · Hand  (앞다리)",
         "합 24개. 손가락 본은 만들지 않는다"],
        [["*Idle", "40", "1.33", "○", "—"], ["Prowl", "32", "1.07", "○", "—"],
         ["Chase", "20", "0.67", "○", "—"], ["Attack", "24", "0.80", "—", "8"],
         ["Hit", "12", "0.40", "—", "—"], ["Death", "30", "1.00", "—", "—"]],
        [("Capsule", "h 0.9 · r 0.4 · center (0, 0.45, 0)"),
         ("Agent", "r 0.4 · h 0.9 · speed 5.5 · 가속 20 · 정지 1.2"),
         ("--", ""),
         ("체력", "2 — 권총 1~2발"),
         ("시야", "18m · 120° · 눈높이 0.5m · 0.15s 간격"),
         ("공격", "사거리 1.6m · 재사용 1.2s · 피해 1"),
         ("--", ""),
         ("실측", "Crawler.cs · CrawlerSetup.cs 의 커밋값이다. 이 셋 중 유일하게"),
         ("", "코드가 있으므로 모델을 여기에 맞춘다"),
         ("주의", "다리는 콜라이더에 넣지 않는다. 눈높이 0.5m 는 모델 머리(0.3m)보다"),
         ("", "높은데 문턱 때문이니 모델을 맞추지 않는다")]))
    return "".join(o)


# ================================================================ 2 · Colossus
def sheet_colossus():
    o = [head_band("큰 것", "COLOSSUS",
                   "화면을 채운다. 느리다. 수직 거대 덩어리 — 어깨가 최고점이고 머리가 보이지 않는다",
                   "등장 1 · 6장 · 죽지 않는다 · 밀어내거나 도망친다 · 2단계",
                   "단위 m · 격자 1.0m · 정투영")]

    px, pw = M, 560
    v = View(px + 250, GY, 78)
    o += [panel(px, PY, pw, PH, "측면  SIDE"), grid(px, PY, pw, PH, v, step=1.0)]
    o.append(clip(px, PY, pw, PH))
    o.append(human(v, x=-2.60))
    o.append(col_side(v))
    o.append(dim_v(v, 0, 4.48, 1.72, "4.50", left=False))
    o.append(dim_h(v, -0.88, 1.22, 4.72, "깊이 2.10"))
    o.append(marker(v, -0.58, 4.30, 1))
    o.append(marker(v, 0.94, 3.52, 2))
    o.append(marker(v, 0.86, 1.10, 3))
    o.append("</g>")
    o.append(legend(px + 20, GY + 26, [
        "최고점은 어깨다. 머리가 어깨선 위로 올라오면 안 된다",
        "머리는 어깨보다 1m 가까이 낮고 쌍봉 사이 골에 묻힌다",
        "팔이 무릎을 지나 바닥에 닿는다"]))
    o.append(scalebar(px + pw - 110, PY + 62, 78, 1.0))

    px2, pw2 = px + pw + 16, 500
    v2 = View(px2 + 250, GY, 78)
    o += [panel(px2, PY, pw2, PH, "정면  FRONT", "6장 조우 구도"),
          grid(px2, PY, pw2, PH, v2, step=1.0)]
    o.append(clip(px2, PY, pw2, PH))
    o.append(human(v2, x=-2.55, front=True))
    o.append(col_front(v2))
    o.append(dim_h(v2, -1.32, 1.32, 4.72, "어깨 2.64"))
    o.append(dim_h(v2, -1.72, 1.72, -0.46, "총폭 3.44", up=False))
    o.append(marker(v2, 0.0, 4.10, 4))
    o.append("</g>")
    o.append(legend(px2 + 20, GY + 26, ["쌍봉 사이의 골이 머리 자리다"]))

    px3, pw3 = px2 + pw2 + 16, 320
    v3 = View(px3 + 160, PY + 300, 86)
    o += [panel(px3, PY, pw3, PH, "평면  TOP", "전방 = 화면 위"),
          grid(px3, PY, pw3, PH, v3, step=1.0, ground=False)]
    o.append(clip(px3, PY, pw3, PH))
    o.append(col_top(v3))
    o.append(dim_h(v3, -1.32, 1.32, -1.18, "2.64", up=False))
    o.append(up_arrow(v3, 0.0, 1.38, 1.98))
    o.append("</g>")

    px4, pw4 = px3 + pw3 + 16, R - (px3 + pw3 + 16)
    o.append(panel(px4, PY, pw4, PH, "1장 창밖", "실루엣만"))
    o.append(clip(px4, PY, pw4, PH))
    o.append(f'<rect x="{px4+1}" y="{PY+41}" width="{pw4-2}" height="{PH-42}" fill="#4b5764"/>')
    o.append(f'<g opacity="0.92">{col_front(View(px4 + pw4/2, PY + 400, 62), sil=True)}</g>')
    o.append(f'<rect x="{px4+1}" y="{PY+41}" width="{pw4-2}" height="{PH-42}" fill="#9aa6b3" '
             f'opacity="0.34"/>')
    o.append(f'<rect x="{px4+1}" y="{PY+PH-62}" width="{pw4-2}" height="61" fill="#141a21" '
             f'opacity="0.9"/>')
    o.append("</g>")
    o.append(note(px4 + 16, PY + PH - 38, [
        "1장에서는 이 정도만 보여준다. 여기서",
        "형태가 읽혀야 6장에서 「그것」이 된다."], TXT_DIM, 12.5, 18))

    lw = 1080
    o.append(lite(px, BY, lw, BH, "역광 검수  BACKLIT SILHOUETTE", "순검정"))
    o.append(clip(px, BY, lw, BH, 43))
    g2 = BY + BH - 42
    o.append(f'<line x1="{px+1}" y1="{g2}" x2="{px+lw-1}" y2="{g2}" stroke="#a8b3bf" stroke-width="1.6"/>')
    o.append(human(View(px + 66, g2, 43), front=True, color="#8b96a3", label=False))
    o.append(col_side(View(px + 176, g2, 43), sil=True))
    o.append(col_front(View(px + 396, g2, 43), sil=True))
    for cxx, lb in ((px + 66, "사람"), (px + 176, "SIDE"), (px + 396, "FRONT")):
        o.append(f'<text x="{cxx}" y="{g2+20}" text-anchor="middle" font-family="{MONO}" '
                 f'font-size="12" fill="#5a6672">{lb}</text>')
    o.append(f'<text x="{px+580}" y="{BY+76}" font-family="{FONT}" font-size="14.5" '
             f'font-weight="700" fill="#1a2027">머리를 지운다</text>')
    o.append(f'<g>{note(px + 580, BY + 104, [])}</g>')
    o.append("".join(
        f'<text x="{px+580}" y="{BY+104+i*21}" font-family="{FONT}" font-size="13" fill="#40505f">'
        f'{s}</text>' for i, s in enumerate([
            "머리가 어깨선 위로 올라오면 사람 형태로 읽히고, 그 순간 덜",
            "무서워진다. 어깨 쌍봉을 최고점으로 두고 머리를 그 사이 골에",
            "묻어 어디를 보는지 알 수 없게 만든다. 「죽지 않는 상대」라는",
            "규칙이 설명 없이 형태로 먼저 전달된다.",
            "",
            "사람(1.75m) 옆에 두면 2.6배다. 이 대비가 실루엣의 전부이므로",
            "신장을 줄이면 다른 두 종과 구분이 흐려진다.",
        ])))
    o.append("</g>")

    sx = px + lw + 16
    o.append(spec(sx, BY, R - sx, BH, [
        ("신장", "4.50m — 사람의 2.6배"),
        ("어깨폭", "2.64m · 총폭 3.44m"),
        ("깊이", "2.10m"),
        ("--", ""),
        ("tris", "9,000 상한 (근접 노출)"),
        ("본", "26개"),
        ("텍스처", "2048² 1장 — 화면을 채우므로 예외"),
        ("--", ""),
        ("이동", "NavMeshAgent 1.2 m/s 이하."),
        ("", "플레이어보다 반드시 느리다"),
    ], "규격"))

    o.append(spec(px, Y3, 900, H3, [
        ("죽지 않는다", "Death 클립을 만들지 않는다. 손상 모델도 없다"),
        ("대신", "Stagger(밀려남) · Buried(눈에 묻힘) 두 개로 끝낸다"),
        ("이유", "「죽지 않는 것이 있다」가 1장부터 6장까지 안 깨져야 무섭다"),
        ("--", ""),
        ("애니메이션", "Idle · Walk(느림) · Reach(붙잡기) · Slam · Stagger · Buried — 6종"),
        ("Animator", "Speed · Attack · Stagger(trigger) · Buried(bool). "
                     "<tspan font-weight='700'>Dead 없음</tspan>"),
        ("--", ""),
        ("통로 제약", "총폭 3.44m. 이 적이 지나갈 공간은 4m 이상이어야 한다."),
        ("", "6장을 만드는 A(이정범) 에게 먼저 넘겨야 할 수치다"),
        ("1장 등장", "실루엣만. 모델은 같은 것을 쓰고 안개와 거리로 가린다"),
        ("스케일", "4.5m 를 Transform 스케일로 키우지 않는다. 모델링 단계에서 실측"),
        ("", "크기로 만든다. 스케일을 쓰면 NavMeshAgent 반경과 물리가 어긋난다"),
    ], "제작 규격  SPEC", "Assets/Game/Enemy/Colossus.prefab"))

    o.append(spec(px + 916, Y3, R - (px + 916), H3, AXIS_ROWS + [
        ("--", ""),
        ("순서", "Crawler(1단계) → <tspan font-weight='700'>Colossus(2단계)</tspan> → Clinger(3단계)"),
        ("검수", "세 적을 같은 배율 · 순검정으로 놓고 셋이 다르면 통과다."),
        ("", "silhouette-check.png 가 그 검수판이다"),
    ], "축 규약 · 진행", "기는 것 시트와 동일하다"))

    o.append(row4(
        [("슬롯", "2개 — M_Colossus_Body · M_Colossus_Wet. 근접 노출이라 예외로 나눈다"),
         ("Wet", "구강 · 가슴 틈새 · 팔 안쪽. Smoothness 0.65 · Normal 0.6"),
         ("셰이더", "URP Lit · Metallic Workflow · Opaque"),
         ("맵", "Albedo 2048² + Normal 2048². 셋 중 여기만 2048 이다"),
         ("표면값", "Body — Metallic 0.0 · Smoothness 0.30 · Normal 1.0"),
         ("--", ""),
         ("그림자", "Cast Shadows 필수. 1장 창밖 실루엣이 그림자로 읽힌다"),
         ("텍셀 밀도", "약 220 px/m. 신장 4.5m 에 2048² 한 장")],
        "쌍봉 어깨 윗면만 밝은 살로 올린다. 최고점이 먼저 밝아야 「어깨가 머리보다 높다」가 읽힌다.",
        ["Root · Pelvis · Spine01~03 · Chest · Neck · Head",
         "L/R ShoulderHump — 실루엣 쌍봉 전용",
         "L/R Clavicle · Humerus · Radius · Hand · HandTip",
         "L/R Femur · Tibia · Foot",
         "합 26개. 얼굴 본은 없다"],
        [["*Idle", "60", "2.00", "○", "—"], ["Walk", "52", "1.73", "○", "—"],
         ["Reach", "36", "1.20", "—", "20"], ["Slam", "40", "1.33", "—", "22"],
         ["Stagger", "30", "1.00", "—", "—"], ["Buried", "45", "1.50", "—", "—"]],
        [("Capsule", "h 4.2 · r 1.1 · center (0, 2.1, 0)"),
         ("Agent", "r 1.3 · h 4.4 · speed 1.2 · 가속 4 · 정지 2.2"),
         ("--", ""),
         ("체력", "없음 — 죽지 않는다. Death 클립도 없다"),
         ("피격", "Stagger 만. 누적 피해로 밀려나고 다시 온다"),
         ("통로", "Agent r 1.3 이라 2.6m 를 지나지만 렌더 총폭이 3.44m 다."),
         ("", "레벨은 4m 이상으로 잡는다"),
         ("--", ""),
         ("제안값", "이 적은 아직 코드가 없다. Crawler 실측을 기준으로 비례해"),
         ("", "잡은 값이므로 구현할 때 다시 본다")],
        rig_sub="30fps · 6클립 · Death 없음"))
    return "".join(o)


# ================================================================ 3 · Clinger
def sheet_clinger():
    o = [head_band("매달린 것", "CLINGER",
                   "천장에 붙어 있다 떨어진다. 수직 실루엣 — 짧은 몸통에 지나치게 긴 팔이 늘어진다",
                   "등장 4 · 5장 · 산탄총 · 장치 · 여럿이 온다 · 3단계",
                   "단위 m · 격자 0.5m · 정투영")]

    px, pw = M, 560
    v = View(px + 250, GY, 100)
    o += [panel(px, PY, pw, PH, "측면  SIDE", "늘어진 자세 · 천장 3.4m (4장 발전동)"),
          grid(px, PY, pw, PH, v, ceiling=CEIL)]
    o.append(clip(px, PY, pw, PH))
    o.append(human(v, x=-1.66))
    o.append(clinger_side(v))
    o.append(dim_v(v, 0.89, 3.40, 0.90, "늘어진 길이 2.51", left=False))
    o.append(dim_v(v, 1.97, 2.82, -0.64, "0.85"))
    o.append(dim_v(v, 0, 0.89, 1.86, "여유 0.89", left=False))
    o.append(marker(v, 0.40, 1.42, 1))
    o.append(marker(v, 0.44, 3.12, 2))
    o.append("</g>")
    o.append(legend(px + 20, GY + 26, [
        "팔이 몸통 길이의 1.5배. 짧으면 기는 것과 겹친다",
        "천장 부착용 갈고리 사지 4개. 낙하할 때 접힌다",
        "2.6m 복도에서는 접어 천장 아래 0.7m 안에 넣는다"]))

    px2, pw2 = px + pw + 16, 400
    v2 = View(px2 + 200, GY, 100)
    o += [panel(px2, PY, pw2, PH, "정면  FRONT"),
          grid(px2, PY, pw2, PH, v2, ceiling=CEIL)]
    o.append(clip(px2, PY, pw2, PH))
    o.append(clinger_front(v2))
    o.append(dim_h(v2, -0.46, 0.46, 3.54, "갈고리 1.10"))
    o.append(dim_h(v2, -0.24, 0.24, -0.24, "몸통폭 0.48", up=False))
    o.append("</g>")

    px3, pw3 = px2 + pw2 + 16, 320
    v3 = View(px3 + 160, PY + 300, 170)
    o += [panel(px3, PY, pw3, PH, "평면  TOP", "천장 접지"),
          grid(px3, PY, pw3, PH, v3, ground=False)]
    o.append(clip(px3, PY, pw3, PH))
    o.append(clinger_top(v3))
    o.append(dim_h(v3, -0.52, 0.52, -0.62, "1.04", up=False))
    o.append(up_arrow(v3, 0.0, 0.46, 0.86))
    o.append("</g>")

    px4, pw4 = px3 + pw3 + 16, R - (px3 + pw3 + 16)
    v4 = View(px4 + 200, GY, 148)
    o += [panel(px4, PY, pw4, PH, "착지 자세  LANDED", "떨어진 직후 · 4장"),
          grid(px4, PY, pw4, PH, v4)]
    o.append(clip(px4, PY, pw4, PH))
    o.append(human(v4, x=-1.05))
    o.append(clinger_land(v4))
    o.append(dim_v(v4, 0, 1.42, 0.92, "1.42", left=False))
    o.append(marker(v4, 0.68, 0.06, 1))
    o.append("</g>")
    o.append(legend(px4 + 20, GY + 26, [
        "손이 바닥에 닿는다. 상체는 서 있어 네발이 아니다"]))

    lw = 1240
    o.append(lite(px, BY, lw, BH, "역광 검수  BACKLIT SILHOUETTE",
                  "매달림 두 컷은 천장 기준 · 착지는 지면 기준"))
    o.append(clip(px, BY, lw, BH, 43))
    g2 = BY + BH - 42
    S = 76
    cy = BY + 58
    gh = cy + CEIL * S            # 매달린 자세는 지면이 판 밖이다
    o.append(f'<line x1="{px+1}" y1="{cy:.1f}" x2="{px+520}" y2="{cy:.1f}" stroke="#9aa5b1" '
             f'stroke-width="1.5" stroke-dasharray="9 6"/>'
             f'<text x="{px+14}" y="{cy+16:.1f}" font-family="{MONO}" font-size="11.5" '
             f'fill="#8d98a4">천장 3.4m</text>')
    o.append(f'<line x1="{px+520}" y1="{g2}" x2="{px+740}" y2="{g2}" stroke="#a8b3bf" stroke-width="1.6"/>')
    for cxx, fn, gg, lb in ((px + 200, clinger_side, gh, "매달림 · SIDE"),
                            (px + 380, clinger_front, gh, "매달림 · FRONT"),
                            (px + 630, clinger_land, g2, "착지  LANDED")):
        o.append(fn(View(cxx, gg, S), sil=True))
        o.append(f'<text x="{cxx}" y="{g2+20}" text-anchor="middle" font-family="{MONO}" '
                 f'font-size="12" fill="#5a6672">{lb}</text>')
    o.append(f'<text x="{px+800}" y="{BY+76}" font-family="{FONT}" font-size="14.5" '
             f'font-weight="700" fill="#1a2027">기는 것과 겹치지 않는 이유</text>')
    o.append("".join(
        f'<text x="{px+800}" y="{BY+104+i*21}" font-family="{FONT}" font-size="13" fill="#40505f">'
        f'{s}</text>' for i, s in enumerate([
            "· 세로 : 가로 = 2.7 : 1.0 — 기는 것과 정확히 반대다",
            "· 몸통 덩어리가 전체 길이의 1/3 뿐이고 나머지는 얇은 선이다",
            "· 매달렸을 땐 화면 위쪽 절반에만 존재한다. 위를 봐야 보인다",
            "· 착지해도 상체가 서 있고 손이 바닥에 닿는다. 네발이 아니다",
            "",
            "IMPORTANT: 팔을 짧게 만들면 착지 자세가 기는 것과 닮아진다.",
            "이 시트에서 가장 지켜야 하는 수치가 팔 길이다.",
        ])))
    o.append("</g>")

    sx = px + lw + 16
    o.append(spec(sx, BY, R - sx, BH, [
        ("늘어진 길이", "2.51m"),
        ("몸통", "0.85 × 0.48m"),
        ("착지 신장", "1.42m"),
        ("갈고리 폭", "1.10m"),
        ("필요 천장", "3.4m 이상 (발전동)"),
        ("--", ""),
        ("tris", "3,500 상한"),
        ("본", "20개"),
        ("텍스처", "1024² 1장"),
        ("--", ""),
        ("얼굴", "만들지 않는다"),
    ], "규격"))

    o.append(spec(px, Y3, 900, H3, [
        ("실루엣 규칙", "위를 봐야 보인다. 천장선 아래 0.3m 안에서 몸통이 시작한다"),
        ("팔 길이", "몸통 대비 1.5배. 가장 중요한 수치다"),
        ("갈고리 사지", "4개. 천장 부착점을 만들고 낙하할 때 접힌다"),
        ("얼굴", "만들지 않는다. 머리는 갈라진 덩어리로 끝낸다"),
        ("--", ""),
        ("자세 두 개", "늘어진 자세는 천장 3.4m 이상(발전동)에서만 쓴다. 2.6m 복도에서는"),
        ("", "<tspan font-weight='700'>접은 대기 자세</tspan>로 천장 아래 0.7m 안에 넣는다. 안 접으면 플레이어가"),
        ("", "몸통에 박아 낙하 연출이 성립하지 않는다. 리그 접힘 범위를 이 둘로 정한다"),
        ("--", ""),
        ("애니메이션", "Cling(접은 대기) · Sway · Drop · Land · Chase · Attack · Hit ·"),
        ("", "Death — 8종. Cling / Drop / Land 가 이 적의 정체성이다"),
        ("Animator", "Speed · Attack · Hit · Dead + Drop(trigger) · Clinging(bool)"),
        ("낙하", "Drop 은 애니메이션만. 실제 낙하는 코드가 y 를 내리고 착지에서 Agent 를 켠다"),
        ("배치", "4 · 5장 담당에게 프리팹으로 넘긴다. 남의 씬에 직접 놓지 않는다"),
    ], "제작 규격  SPEC", "Assets/Game/Enemy/Clinger.prefab"))

    o.append(spec(px + 916, Y3, R - (px + 916), H3, AXIS_ROWS + [
        ("--", ""),
        ("매달림", "거꾸로 매달렸다고 모델을 뒤집어 내보내지 않는다. 매달린 상태에서도"),
        ("", "로컬 +Z 전방 · +Y 위이고, 회전은 코드가 준다"),
        ("순서", "Crawler(1단계) → Colossus(2단계) → <tspan font-weight='700'>Clinger(3단계)</tspan>"),
    ], "축 규약 · 진행", "기는 것 시트와 동일하다"))

    o.append(row4(
        [("슬롯", "1개 — M_Clinger_Body. 갈고리 끝만 각질 UV 로 뺀다"),
         ("셰이더", "URP Lit · Metallic Workflow · Opaque"),
         ("맵", "Albedo 1024² + Normal 1024²"),
         ("표면값", "Metallic 0.0 · Smoothness 0.35 · Normal Scale 1.0"),
         ("젖은 느낌", "머티리얼을 늘리지 않고 Smoothness 를 0.30 → 0.35 로만 올린다"),
         ("--", ""),
         ("천장 대비", "천장은 어둡다. 몸통을 살 기본보다 어둡게 잡으면 올려다봐도"),
         ("", "안 보인다. 갈고리 끝(B9AFA2)이 유일한 단서다")],
        "위에서 보는 적이라 밝은 면이 아래를 향한다. 배와 팔 안쪽에 밝은 살을 쓴다 — 다른 둘과 반대다.",
        ["Root · Pelvis · Spine01~02 · Neck · Head",
         "Grip01~04 각 2본 — 천장 갈고리 · 접힘 축",
         "L/R Humerus · Radius · Hand · 합 20개"],
        [["*Cling", "60", "2.00", "○", "—"], ["Sway", "90", "3.00", "○", "—"],
         ["Drop", "18", "0.60", "—", "—"], ["Land", "20", "0.67", "—", "—"],
         ["Chase", "24", "0.80", "○", "—"], ["Attack", "22", "0.73", "—", "9"],
         ["Hit", "12", "0.40", "—", "—"], ["Death", "28", "0.93", "—", "—"]],
        [("매달림", "SphereCollider r 0.32 · 몸통 중심. Agent 는 꺼둔다"),
         ("착지", "CapsuleCollider h 1.3 · r 0.35 · center (0, 0.65, 0)"),
         ("전환", "콜라이더 두 개를 두고 Land 에서 켜고 끈다"),
         ("--", ""),
         ("Agent", "r 0.35 · h 1.4 · speed 3.2 · 가속 14 · 정지 1.2"),
         ("Agent 켜기", "Land 마지막 프레임. 그 전에 켜면 공중에서 경로를 잡는다"),
         ("체력", "1~2 — 산탄총 한 발"),
         ("--", ""),
         ("제안값", "이 적도 코드가 없다. Crawler 실측 기준으로 비례해 잡았다")]))
    return "".join(o)


# ================================================================ 4 · 실루엣 검수
def sheet_silhouette():
    Hh = 1200
    o = [head_band("역광 실루엣 검수", "SILHOUETTE CHECK",
                   "기획서 — 「역광에서 검은 실루엣만 보여도 구분되는지를 기준으로 검수한다. "
                   "이것이 안 되면 적 3종을 나눈 의미가 없어진다」",
                   "동일 배율 · 순검정 · 사람 1.75m 기준",
                   "Crawler · Colossus · Clinger 대조판")]

    px, py, pw, ph = M, 104, R - M, 556
    o.append(lite(px, py, pw, ph, "A · 동일 배율 나란히", "세로 격자 1m"))
    o.append(clip(px, py, pw, ph, 43))
    S = 90.0
    gy = py + ph - 62
    cy = gy - CEIL * S
    o.append(f'<line x1="{px+1}" y1="{cy:.1f}" x2="{px+1300}" y2="{cy:.1f}" stroke="#9aa5b1" '
             f'stroke-width="1.5" stroke-dasharray="9 6"/>'
             f'<text x="{px+14}" y="{cy-9:.1f}" font-family="{MONO}" font-size="12" '
             f'fill="#7b8794">천장 3.4m (4장 발전동)</text>')
    for k in range(1, 5):
        yy = gy - k * S
        if yy > py + 52:
            o.append(f'<line x1="{px+1}" y1="{yy:.1f}" x2="{px+pw-1}" y2="{yy:.1f}" '
                     f'stroke="#d5dce4" stroke-width="1"/>'
                     f'<text x="{px+pw-12}" y="{yy-5:.1f}" text-anchor="end" font-family="{MONO}" '
                     f'font-size="11" fill="#9aa5b1">{k}m</text>')
    o.append(f'<line x1="{px+1}" y1="{gy:.1f}" x2="{px+pw-1}" y2="{gy:.1f}" stroke="#6b7683" '
             f'stroke-width="2.5"/>')
    o.append(human(View(px + 120, gy, S), front=True, color="#8b96a3", label=False))
    o.append(crawler_side(View(px + 420, gy, S), sil=True))
    o.append(clinger_side(View(px + 840, gy, S), sil=True))
    o.append(clinger_land(View(px + 1060, gy, S), sil=True))
    o.append(col_side(View(px + 1430, gy, S), sil=True))
    for cxx, lb, sub in ((px + 120, "사람", "1.75m"),
                         (px + 420, "기는 것 CRAWLER", "수평 · 2.88 × 0.98m"),
                         (px + 950, "매달린 것 CLINGER", "얇은 수직 · 2.51m 늘어짐 / 착지 1.42m"),
                         (px + 1430, "큰 것 COLOSSUS", "두꺼운 수직 · 4.50m")):
        o.append(f'<text x="{cxx}" y="{gy+26:.1f}" text-anchor="middle" font-family="{FONT}" '
                 f'font-size="15" font-weight="700" fill="#1a2027">{lb}</text>'
                 f'<text x="{cxx}" y="{gy+44:.1f}" text-anchor="middle" font-family="{MONO}" '
                 f'font-size="12" fill="#5a6672">{sub}</text>')
    o.append("</g>")

    by, bh, bw = py + ph + 16, 226, 840
    o.append(lite(px, by, bw, bh, "B · 원거리 축소 (33%)", "복도 끝 크기"))
    o.append(clip(px, by, bw, bh, 43))
    s2, g2 = 30.0, by + bh - 34
    o.append(f'<line x1="{px+1}" y1="{g2}" x2="{px+bw-1}" y2="{g2}" stroke="#a8b3bf" stroke-width="1.5"/>')
    o.append(human(View(px + 80, g2, s2), front=True, color="#8b96a3", label=False))
    o.append(crawler_side(View(px + 230, g2, s2), sil=True))
    o.append(clinger_land(View(px + 430, g2, s2), sil=True))
    o.append(col_side(View(px + 650, g2, s2), sil=True))
    o.append("</g>")

    cx0 = px + bw + 16
    cw = R - cx0
    o.append(lite(cx0, by, cw, bh, "C · 눈보라 흐림", "5장 시야 조건"))
    o.append('<filter id="blur"><feGaussianBlur stdDeviation="4"/></filter>')
    o.append(clip(cx0, by, cw, bh, 43))
    o.append(f'<rect x="{cx0+1}" y="{by+43}" width="{cw-2}" height="{bh-44}" fill="#d2d9e1"/>')
    g3 = by + bh - 34
    o.append('<g filter="url(#blur)" opacity="0.92">')
    o.append(crawler_side(View(cx0 + 180, g3, s2), sil=True))
    o.append(clinger_land(View(cx0 + 390, g3, s2), sil=True))
    o.append(col_side(View(cx0 + 630, g3, s2), sil=True))
    o.append("</g></g>")

    y3, h3 = by + bh + 16, 208
    o.append(panel(px, y3, 1080, h3, "판정  VERDICT", "A · B · C 세 조건 모두"))
    o.append(f'<text x="{px+18}" y="{y3+72}" font-family="{FONT}" font-size="15" '
             f'font-weight="700" fill="{OK_G}">통과 — 셋이 서로 다른 방향으로 화면을 채운다</text>')
    o.append(note(px + 18, y3 + 100, [
        "기는 것 = 가로로 긴 낮은 덩어리 + 등 위로 솟은 관절 봉우리 둘",
        "매달린 것 = 몸통이 전체의 1/3 뿐인 얇은 세로선. 매달렸을 땐 화면 위쪽 절반에만 존재한다",
        "큰 것 = 사람 2.6배 높이의 두꺼운 세로 덩어리. 최고점이 어깨고 머리가 없다",
        "33% 로 줄이고 흐리게 해도 세 방향(가로 / 얇은 세로 / 두꺼운 세로)이 유지된다",
    ], TXT_DIM, 13.5, 21))

    o.append(panel(px + 1096, y3, R - (px + 1096), h3, "모델이 나온 뒤 다시 검수한다"))
    o.append(note(px + 1114, y3 + 70, [
        "1. 세 프리팹을 한 씬에 같은 배율로 놓는다",
        "2. Unlit 검정 머티리얼 · 흰 배경 · 정투영 카메라",
        "3. 이 시트와 나란히 비교한다",
        "",
        "모델링 중에 실루엣이 서로 닮아가면 그 자리에서 되돌린다.",
        "리깅까지 간 뒤에는 되돌리기 어렵다.",
    ], TXT_DIM, 13, 21))
    return "".join(o), Hh


# ================================================================ 5 · 머티리얼
def sheet_material():
    Hh = 1276
    o = [head_band("머티리얼 규격", "MATERIAL GUIDE",
                   "적 3종이 같은 팔레트를 쓴다. 구분은 색이 아니라 형태다 — 역광에서 색은 보이지 않는다",
                   "URP Lit · Albedo + Normal 만 · WebGL",
                   "Crawler · Colossus · Clinger 공용")]

    # ── 팔레트
    px, py, pw, ph = M, 104, 1080, 470
    o.append(panel(px, py, pw, ph, "공용 팔레트  PALETTE", "sRGB hex · 채도를 낮게 유지한다"))
    o.append(tbl(px + 18, py + 66, [(76, "이름 · hex", "start"),
                                    (250, "쓰는 곳", "start")], [], lh=0))
    for i, (hx, nm, use) in enumerate(PAL):
        yy = py + 80 + i * 47
        # 명도는 sRGB 휘도. 살 3단이 이 값으로 갈린다.
        lum = (0.2126 * int(hx[1:3], 16) + 0.7152 * int(hx[3:5], 16)
               + 0.0722 * int(hx[5:7], 16)) / 255.0
        o.append(chip(px + 18, yy, 54, 34, hx, "#0d1116"))
        o.append(f'<text x="{px+100}" y="{yy+15}" font-family="{FONT}" font-size="13.5" '
                 f'font-weight="700" fill="{TXT}">{nm}</text>'
                 f'<text x="{px+100}" y="{yy+31}" font-family="{MONO}" font-size="12" '
                 f'fill="{TXT_DIM}">{hx} · 명도 {lum:.2f}</text>'
                 f'<text x="{px+268}" y="{yy+23}" font-family="{FONT}" font-size="13" '
                 f'fill="{TXT_DIM}">{use}</text>')
    o.append(note(px + 18, py + ph - 40, [
        "IMPORTANT: 세 적에게 서로 다른 색을 주지 않는다. 어두운 게임이라 색으로 준 차이는 전달되지 않고, "
        "구분은 형태가 한다."], ACC, 12.5, 17))
    o.append(note(px + 18, py + ph - 19, [
        "점막 · 유기물 데코는 살 음영과 명도가 거의 같다. 가까이서 채도로만 갈리는 색이라 형태 판별에는 "
        "쓰지 않는다 — 살 3단만 명도로 갈린다."], TXT_DIM, 12.5, 17))

    # ── 표면값 · URP Lit
    px2, pw2 = px + pw + 16, R - (px + pw + 16)
    o.append(panel(px2, py, pw2, ph, "표면값 · URP Lit"))
    o.append(tbl(px2 + 18, py + 66,
                 [(0, "부위", "start"), (200, "Metallic", "end"),
                  (300, "Smoothness", "end"), (392, "Normal", "end")],
                 [[f"*{a}", b, c, d] for a, b, c, d in SURFACE], lh=24, size=13))
    ry = py + 176
    o.append(f'<line x1="{px2+18}" y1="{ry-24}" x2="{px2+pw2-18}" y2="{ry-24}" '
             f'stroke="{PANEL_E}" stroke-width="1"/>')
    for i, (k, v) in enumerate([
            ("Workflow Mode", "Metallic"),
            ("Surface Type", "Opaque · Render Face Front"),
            ("Alpha Clipping", "끈다"),
            ("Specular Highlights", "켠다"),
            ("Environment Reflections", "<tspan font-weight='700'>끈다</tspan>"),
            ("Cast / Receive Shadows", "둘 다 켠다"),
            ("Metallic·Smoothness 맵", "쓰지 않는다 — 슬라이더 상수"),
    ]):
        yy = ry + i * 25
        o.append(f'<text x="{px2+18}" y="{yy}" font-family="{FONT}" font-size="13" '
                 f'fill="{ACC_2}">{k}</text>'
                 f'<text x="{px2+pw2-18}" y="{yy}" text-anchor="end" font-family="{FONT}" '
                 f'font-size="13" fill="{TXT}">{v}</text>')
    o.append(note(px2 + 18, ry + 176, [
        "Env Reflections 를 끄는 이유는 두 가지다. 어두운 실내에서",
        "큐브맵 반사가 유기물에 얹히면 플라스틱처럼 보이고, WebGL",
        "에서 공짜가 아니다.",
        "",
        "맵을 두 장(Albedo · Normal)으로 끝내면 적 하나당 텍스처가",
        "둘이다. 빌드 용량이 곧 첫 로딩 시간이다.",
    ], TXT_DIM, 12.5, 19))

    # ── 아틀라스
    by, bh = 590, 330
    aw = 700
    o.append(panel(px, by, aw, bh, "아틀라스 배치  ATLAS", "1024² · Colossus 만 2048²"))
    ax, ay, asz = px + 26, by + 62, 236
    o.append(f'<rect x="{ax}" y="{ay}" width="{asz}" height="{asz}" fill="{FILL_2}" '
             f'stroke="{INK_2}" stroke-width="1.5"/>')
    half = asz / 2
    regions = [(0, 0, half, half, "몸통 · 머리", "512²"),
               (half, 0, half, half, "팔 · 다리 · 척추", "512²"),
               (0, half, half, half / 2, "각질 (공용 타일)", "512×256"),
               (half, half, half / 2, half / 2, "점막", "256²"),
               (half + half / 2, half, half / 2, half / 2, "여백", ""),
               (0, half + half / 2, asz, half / 2, "예비 — 비워둔다", "1024×256")]
    for rx, ry2, rw, rh, lb, sz in regions:
        o.append(f'<rect x="{ax+rx}" y="{ay+ry2}" width="{rw}" height="{rh}" fill="none" '
                 f'stroke="{INK_2}" stroke-width="1" stroke-dasharray="4 3"/>')
        o.append(f'<text x="{ax+rx+rw/2:.1f}" y="{ay+ry2+rh/2-2:.1f}" text-anchor="middle" '
                 f'font-family="{FONT}" font-size="11.5" fill="{TXT}">{lb}</text>')
        if sz:
            o.append(f'<text x="{ax+rx+rw/2:.1f}" y="{ay+ry2+rh/2+14:.1f}" text-anchor="middle" '
                     f'font-family="{MONO}" font-size="10" fill="{TXT_DIM}">{sz}</text>')
    o.append(f'<text x="{ax}" y="{ay-8}" font-family="{MONO}" font-size="11" '
             f'fill="{TXT_DIM}">0,0</text>'
             f'<text x="{ax+asz}" y="{ay+asz+16}" text-anchor="end" font-family="{MONO}" '
             f'font-size="11" fill="{TXT_DIM}">1024,1024</text>')
    o.append(note(ax + asz + 30, ay + 18, [
        "각질 타일은 셋이 <tspan font-weight='700'>같은 영역을 공유</tspan>한다.",
        "발톱 · 이빨 · 갈고리 끝이 전부 같은",
        "재질이므로 세 번 그리지 않는다.",
        "",
        "UV 는 겹치지 않게 편다. 미러 UV 를",
        "쓰면 노멀맵이 한쪽에서 뒤집힌다.",
        "",
        "텍셀 밀도 약 250 px/m 을 맞춘다.",
        "Colossus 만 2048² 에 220 px/m.",
    ], TXT_DIM, 12.5, 19))

    # ── 임포트 설정
    ix = px + aw + 16
    o.append(spec(ix, by, R - ix, bh, [
        ("Max Size", "1024 (Colossus 2048). 원본이 더 커도 여기서 자른다"),
        ("Compression", "Normal Quality · <tspan font-weight='700'>Use Crunch Compression 켠다</tspan>"),
        ("", "다운로드 용량이 줄고 런타임 메모리는 그대로다"),
        ("Mip Maps", "Generate 켠다. 멀리 있는 적이 지글거린다"),
        ("Streaming", "끈다. WebGL 에서 이득이 없다"),
        ("--", ""),
        ("Normal 맵", "Texture Type = Normal map · sRGB 해제. 반드시 확인한다"),
        ("Albedo", "sRGB 켠다 (기본값)"),
        ("--", ""),
        ("확인", "임포트 설정은 .meta 에 들어가고 .meta 는 커밋된다. 셋이 같은"),
        ("", "설정을 쓰게 되므로 첫 적에서 한 번 정하면 뒤는 복사면 된다"),
    ], "임포트 설정  IMPORT", "WebGL 빌드 용량이 곧 첫 로딩 시간이다"))

    # ── 어두운 씬 검수
    cy2, ch = 936, 300
    o.append(panel(px, cy2, R - px, ch, "어두운 씬 검수  VALUE SEPARATION",
                   "손전등 원뿔 안에서 살 3단이 갈리는지"))
    gx, gw = px + 30, R - px - 60
    # 손전등 감쇠는 검정을 덮어 만든다. 흰색을 multiply 하면 아무 일도 일어나지 않는다.
    o.append('<defs><linearGradient id="fall" x1="0" y1="0" x2="1" y2="0">'
             '<stop offset="0" stop-color="#000" stop-opacity="0"/>'
             '<stop offset="0.30" stop-color="#000" stop-opacity="0.28"/>'
             '<stop offset="0.55" stop-color="#000" stop-opacity="0.62"/>'
             '<stop offset="0.78" stop-color="#000" stop-opacity="0.86"/>'
             '<stop offset="1" stop-color="#000" stop-opacity="0.97"/>'
             '</linearGradient></defs>')
    bar_y = cy2 + 78
    for i, (hx, nm, _u) in enumerate(PAL[:3]):
        yy = bar_y + i * 42
        lum = (0.2126 * int(hx[1:3], 16) + 0.7152 * int(hx[3:5], 16)
               + 0.0722 * int(hx[5:7], 16)) / 255.0
        o.append(f'<rect x="{gx}" y="{yy}" width="{gw}" height="34" fill="{hx}"/>')
        o.append(f'<rect x="{gx}" y="{yy}" width="{gw}" height="34" fill="url(#fall)"/>')
        o.append(f'<text x="{gx-12}" y="{yy+16}" text-anchor="end" font-family="{MONO}" '
                 f'font-size="11" fill="{TXT_DIM}">{hx[1:]}</text>'
                 f'<text x="{gx-12}" y="{yy+30}" text-anchor="end" font-family="{MONO}" '
                 f'font-size="11" fill="{ACC}">{lum:.2f}</text>')
    for frac, lb in ((0.06, "손전등 중심 ~2m"), (0.44, "원뿔 경계 ~6m"), (0.76, "원뿔 밖")):
        xx = gx + gw * frac
        o.append(f'<line x1="{xx:.1f}" y1="{bar_y-10}" x2="{xx:.1f}" y2="{bar_y+3*42-4}" '
                 f'stroke="{ACC}" stroke-width="1" stroke-dasharray="3 3" opacity="0.75"/>'
                 f'<text x="{xx+6:.1f}" y="{bar_y-14}" font-family="{MONO}" font-size="11" '
                 f'fill="{ACC}">{lb}</text>')
    o.append(note(px + 30, cy2 + ch - 62, [
        "살 3단의 명도 차를 <tspan font-weight='700' fill='" + ACC + "'>0.10 이상</tspan> 벌린다. "
        "위 팔레트는 0.52 → 0.40 → 0.28 로 <tspan font-weight='700'>단 차 0.12</tspan> 라 규칙을 만족한다. "
        "좁히면 손전등이 닿는 동안에도",
        "형태가 뭉개져 실루엣만 남고, 그러면 적 3종을 나눈 것 말고는 아무 정보도 전달되지 않는다. "
        "원뿔 경계까지 세 단이 갈려 보이면 통과다.",
        "젖음 · 피 · 눈은 머티리얼에 넣지 않는다. 데칼과 파티클로 얹는다 — 기획의 「시뮬레이션하지 않는다」와 같은 이유다.",
    ], TXT_DIM, 13, 21))
    return "".join(o), Hh


write("crawler-sheet", sheet_crawler())
write("colossus-sheet", sheet_colossus())
write("clinger-sheet", sheet_clinger())
s, h = sheet_silhouette()
write("silhouette-check", s, h)
s, h = sheet_material()
write("material-guide", s, h)
