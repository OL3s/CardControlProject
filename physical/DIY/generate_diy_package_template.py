#!/usr/bin/env python3
"""Generate 1:1 transparent DIY cardboard package templates.

Output structure relative to this script:
    png/*.png
    svg/*.svg
    data/diy_package_measurements.txt

Run:
    python3 generate_diy_package_template.py

The SVG files are authored directly in millimetres. The PNG exports are created
from those SVG files at 300 DPI for print previews. Print at 100% scale.
"""

from __future__ import annotations

from pathlib import Path
import shutil
import subprocess
from xml.sax.saxutils import escape


ROOT = Path(__file__).resolve().parent
PNG_DIR = ROOT / "png"
SVG_DIR = ROOT / "svg"
DATA_DIR = ROOT / "data"

for directory in (PNG_DIR, SVG_DIR, DATA_DIR):
    directory.mkdir(parents=True, exist_ok=True)


# Mirrored from physical/mesurement/generate_measurement_package.py. The DIY
# sheet is a cardboard tuck/lock prototype, not the magnetic production box.
SPEC = {
    "base_outer": (97.0, 72.0, 43.0),
    "closed_total": (97.0, 72.0, 45.0),
    "tray_outer": (92.0, 67.0, 21.5),
    "base_inner": (93.0, 68.0, 41.0),
    "board": 2.0,
    "lid_panel": (97.0, 72.0, 2.0),
    "front_flap": (97.0, 20.0, 2.0),
    "magnet_edge_offset": 18.0,
}

A4 = (210.0, 297.0)
A3 = (297.0, 420.0)
DPI = 300
MM_PER_INCH = 25.4

CUT = "#101010"
FOLD = "#101010"
GRID = "#2f8fcf"
DIM = "#333333"

NET = {
    "bottom": (SPEC["base_outer"][0], SPEC["base_outer"][1]),
    "wall_h": SPEC["base_outer"][2],
    "lid": (SPEC["lid_panel"][0], SPEC["lid_panel"][1]),
    "insert_tab": (87.0, SPEC["front_flap"][1]),
    "front_inner_flap": (87.0, 16.0),
    "lid_side_flap": (20.0, 54.0),
    "lock_tab_depth": 8.0,
    "lock_tab_h": 16.0,
    "slot": (1.8, 22.0),
    "slot_x_from_side": SPEC["magnet_edge_offset"],
}


class Svg:
    def __init__(self, width: float, height: float) -> None:
        self.width = width
        self.height = height
        self.elements: list[str] = []

    def sy(self, y: float) -> float:
        return self.height - y

    def point(self, x: float, y: float) -> str:
        return f"{x:.3f},{self.sy(y):.3f}"

    def line(
        self,
        points: list[tuple[float, float]],
        *,
        stroke: str = CUT,
        width: float = 0.35,
        dash: str | None = None,
        alpha: float = 1.0,
        z_note: str = "",
    ) -> None:
        dash_attr = f' stroke-dasharray="{dash}"' if dash else ""
        opacity_attr = f' opacity="{alpha:.3f}"' if alpha < 1.0 else ""
        points_attr = " ".join(self.point(x, y) for x, y in points)
        self.elements.append(
            f'<polyline points="{points_attr}" fill="none" stroke="{stroke}" '
            f'stroke-width="{width:.3f}" stroke-linecap="round" '
            f'stroke-linejoin="round"{dash_attr}{opacity_attr}/>{z_note}'
        )

    def rect(
        self,
        x: float,
        y: float,
        w: float,
        h: float,
        *,
        stroke: str = CUT,
        width: float = 0.35,
        fill: str = "none",
        dash: str | None = None,
        alpha: float = 1.0,
        rx: float = 0.0,
    ) -> None:
        dash_attr = f' stroke-dasharray="{dash}"' if dash else ""
        opacity_attr = f' opacity="{alpha:.3f}"' if alpha < 1.0 else ""
        rx_attr = f' rx="{rx:.3f}" ry="{rx:.3f}"' if rx else ""
        self.elements.append(
            f'<rect x="{x:.3f}" y="{self.sy(y + h):.3f}" width="{w:.3f}" height="{h:.3f}" '
            f'fill="{fill}" stroke="{stroke}" stroke-width="{width:.3f}" '
            f'stroke-linecap="round" stroke-linejoin="round"{dash_attr}{opacity_attr}{rx_attr}/>'
        )

    def path(
        self,
        commands: list[tuple],
        *,
        stroke: str = CUT,
        width: float = 0.35,
        fill: str = "none",
    ) -> None:
        parts: list[str] = []
        for command in commands:
            op = command[0]
            if op in {"M", "L"}:
                _, x, y = command
                parts.append(f"{op} {x:.3f},{self.sy(y):.3f}")
            elif op == "Q":
                _, cx, cy, x, y = command
                parts.append(f"Q {cx:.3f},{self.sy(cy):.3f} {x:.3f},{self.sy(y):.3f}")
            elif op == "Z":
                parts.append("Z")
            else:
                raise ValueError(f"Unsupported SVG command: {op}")
        self.elements.append(
            f'<path d="{" ".join(parts)}" fill="{fill}" stroke="{stroke}" '
            f'stroke-width="{width:.3f}" stroke-linecap="round" stroke-linejoin="round"/>'
        )

    def text(
        self,
        x: float,
        y: float,
        text: str,
        *,
        size: float = 2.5,
        anchor: str = "middle",
        weight: str = "400",
        color: str = "#111111",
        rotate: float | None = None,
        line_gap: float = 1.25,
    ) -> None:
        lines = text.split("\n")
        y_svg = self.sy(y)
        transform = ""
        if rotate is not None:
            transform = f' transform="rotate({rotate:.3f} {x:.3f} {y_svg:.3f})"'
        tspans = []
        start_dy = -((len(lines) - 1) * size * line_gap) / 2
        for index, line in enumerate(lines):
            dy = start_dy if index == 0 else size * line_gap
            tspans.append(
                f'<tspan x="{x:.3f}" dy="{dy:.3f}">{escape(line)}</tspan>'
            )
        self.elements.append(
            f'<text x="{x:.3f}" y="{y_svg:.3f}" text-anchor="{anchor}" '
            f'font-family="Arial, Helvetica, sans-serif" font-size="{size:.3f}" '
            f'font-weight="{weight}" fill="{color}" dominant-baseline="middle"{transform}>'
            f'{"".join(tspans)}</text>'
        )

    def to_string(self) -> str:
        return f'''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="{self.width:.3f}mm" height="{self.height:.3f}mm" viewBox="0 0 {self.width:.3f} {self.height:.3f}" version="1.1">
  <title>Element War DIY package template</title>
  <desc>Transparent 1:1 millimetre grid foldable cardboard box net. Solid lines are cuts; dashed lines are folds.</desc>
  {chr(10).join(self.elements)}
</svg>
'''


def net_size() -> tuple[float, float]:
    bottom_w, bottom_d = NET["bottom"]
    wall_h = NET["wall_h"]
    front_flap_d = NET["front_inner_flap"][1]
    lid_d = NET["lid"][1]
    insert_d = NET["insert_tab"][1]
    width = NET["lock_tab_depth"] + wall_h + bottom_w + wall_h + NET["lock_tab_depth"]
    height = front_flap_d + wall_h + bottom_d + wall_h + lid_d + insert_d
    return width, height


def cut_line(svg: Svg, points: list[tuple[float, float]], width: float = 0.34) -> None:
    svg.line(points, stroke=CUT, width=width)


def fold_line(svg: Svg, points: list[tuple[float, float]], width: float = 0.28) -> None:
    svg.line(points, stroke=FOLD, width=width, dash="3.2,2.4")


def draw_grid(svg: Svg) -> None:
    for x in range(int(svg.width) + 1):
        if x % 10 == 0:
            alpha = 0.33
            width = 0.125
        elif x % 5 == 0:
            alpha = 0.23
            width = 0.095
        else:
            alpha = 0.13
            width = 0.065
        svg.line([(float(x), 0.0), (float(x), svg.height)], stroke=GRID, width=width, alpha=alpha)

    for y in range(int(svg.height) + 1):
        if y % 10 == 0:
            alpha = 0.33
            width = 0.125
        elif y % 5 == 0:
            alpha = 0.23
            width = 0.095
        else:
            alpha = 0.13
            width = 0.065
        svg.line([(0.0, float(y)), (svg.width, float(y))], stroke=GRID, width=width, alpha=alpha)

    for x in range(10, int(svg.width), 10):
        svg.text(float(x), svg.height - 2.4, str(x), size=1.25, color=GRID)
    for y in range(10, int(svg.height), 10):
        svg.text(1.6, float(y), str(y), size=1.25, anchor="start", color=GRID)


def tab_path(svg: Svg, x0: float, x1: float, y: float, depth: float, upward: bool) -> None:
    radius = 5.0
    y_outer = y + depth if upward else y - depth
    direction = 1.0 if upward else -1.0
    svg.path(
        [
            ("M", x0, y),
            ("L", x0, y_outer - direction * radius),
            ("Q", x0, y_outer, x0 + radius, y_outer),
            ("L", x1 - radius, y_outer),
            ("Q", x1, y_outer, x1, y_outer - direction * radius),
            ("L", x1, y),
        ],
        width=0.34,
    )


def front_inner_flap_path(svg: Svg, x0: float, x1: float, y: float, depth: float) -> None:
    inset = 3.0
    svg.path(
        [
            ("M", x0, y),
            ("L", x0 + inset, y - depth),
            ("L", x1 - inset, y - depth),
            ("L", x1, y),
        ],
        width=0.34,
    )


def side_panel_path(svg: Svg, attach_x: float, outer_x: float, y0: float, y1: float, mirror: bool) -> None:
    tab_h = NET["lock_tab_h"]
    tab_d = NET["lock_tab_depth"]
    centers = (y0 + 16.0, y1 - 16.0)
    direction = -1.0 if mirror else 1.0

    points: list[tuple[float, float]] = [(attach_x, y0), (outer_x, y0)]
    for center_y in centers:
        tab_y0 = center_y - tab_h / 2
        tab_y1 = center_y + tab_h / 2
        points.extend(
            [
                (outer_x, tab_y0),
                (outer_x + direction * (tab_d - 2.0), tab_y0 + 1.5),
                (outer_x + direction * tab_d, tab_y0 + 4.0),
                (outer_x + direction * tab_d, tab_y1 - 4.0),
                (outer_x + direction * (tab_d - 2.0), tab_y1 - 1.5),
                (outer_x, tab_y1),
            ]
        )
    points.extend([(outer_x, y1), (attach_x, y1)])
    cut_line(svg, points)


def rounded_slot(svg: Svg, cx: float, cy: float, width: float, height: float) -> None:
    svg.rect(cx - width / 2, cy - height / 2, width, height, width=0.30, rx=width / 2)


def panel_label(svg: Svg, x: float, y: float, w: float, h: float, title: str, size: str) -> None:
    svg.text(x + w / 2, y + h / 2 + 3.3, title, size=3.0)
    svg.text(x + w / 2, y + h / 2 - 4.0, size, size=2.05)


def arrow_h(svg: Svg, x: float, y: float, direction: float) -> None:
    cut_line(svg, [(x, y), (x + direction * 2.0, y + 1.2)], width=0.22)
    cut_line(svg, [(x, y), (x + direction * 2.0, y - 1.2)], width=0.22)


def arrow_v(svg: Svg, x: float, y: float, direction: float) -> None:
    cut_line(svg, [(x, y), (x - 1.2, y + direction * 2.0)], width=0.22)
    cut_line(svg, [(x, y), (x + 1.2, y + direction * 2.0)], width=0.22)


def dim_h(svg: Svg, x0: float, x1: float, y: float, text: str) -> None:
    svg.line([(x0, y), (x1, y)], stroke=DIM, width=0.20)
    arrow_h(svg, x0, y, 1.0)
    arrow_h(svg, x1, y, -1.0)
    svg.text((x0 + x1) / 2, y + 2.4, text, size=1.9, color=DIM)


def dim_v(svg: Svg, y0: float, y1: float, x: float, text: str) -> None:
    svg.line([(x, y0), (x, y1)], stroke=DIM, width=0.20)
    arrow_v(svg, x, y0, 1.0)
    arrow_v(svg, x, y1, -1.0)
    svg.text(x + 2.4, (y0 + y1) / 2, text, size=1.9, anchor="middle", color=DIM, rotate=-90)


def draw_template(svg: Svg) -> None:
    bottom_w, bottom_d = NET["bottom"]
    wall_h = NET["wall_h"]
    lid_w, lid_d = NET["lid"]
    insert_w, insert_d = NET["insert_tab"]
    front_flap_w, front_flap_d = NET["front_inner_flap"]
    lock_depth = NET["lock_tab_depth"]
    total_w, total_h = net_size()

    x0 = (svg.width - total_w) / 2 + lock_depth + wall_h
    x1 = x0 + bottom_w
    y0 = (svg.height - total_h) / 2 + front_flap_d + wall_h
    y1 = y0 + bottom_d
    front_y = y0 - wall_h
    back_y = y1
    lid_y = back_y + wall_h
    insert_y = lid_y + lid_d
    front_inner_y = front_y - front_flap_d

    left_outer_x = x0 - wall_h
    right_outer_x = x1 + wall_h

    side_panel_path(svg, x0, left_outer_x, y0, y1, mirror=True)
    side_panel_path(svg, x1, right_outer_x, y0, y1, mirror=False)

    cut_line(svg, [(x0, front_y), (x0, y0)])
    cut_line(svg, [(x1, front_y), (x1, y0)])
    cut_line(svg, [(x0, back_y), (x0, lid_y)])
    cut_line(svg, [(x1, back_y), (x1, lid_y)])

    front_flap_x0 = x0 + (bottom_w - front_flap_w) / 2
    front_flap_x1 = front_flap_x0 + front_flap_w
    cut_line(svg, [(x0, front_y), (front_flap_x0, front_y)])
    front_inner_flap_path(svg, front_flap_x0, front_flap_x1, front_y, front_flap_d)
    cut_line(svg, [(front_flap_x1, front_y), (x1, front_y)])

    side_flap_w, side_flap_h = NET["lid_side_flap"]
    side_flap_y0 = lid_y + (lid_d - side_flap_h) / 2
    side_flap_y1 = side_flap_y0 + side_flap_h
    cut_line(svg, [(x0, lid_y), (x0, side_flap_y0)])
    cut_line(svg, [(x0, side_flap_y1), (x0, insert_y)])
    cut_line(svg, [(x1, lid_y), (x1, side_flap_y0)])
    cut_line(svg, [(x1, side_flap_y1), (x1, insert_y)])
    cut_line(svg, [(x0, side_flap_y0), (x0 - side_flap_w, side_flap_y0 + 5.0), (x0 - side_flap_w, side_flap_y1 - 5.0), (x0, side_flap_y1)])
    cut_line(svg, [(x1, side_flap_y0), (x1 + side_flap_w, side_flap_y0 + 5.0), (x1 + side_flap_w, side_flap_y1 - 5.0), (x1, side_flap_y1)])

    insert_x0 = x0 + (bottom_w - insert_w) / 2
    insert_x1 = insert_x0 + insert_w
    cut_line(svg, [(x0, insert_y), (insert_x0, insert_y)])
    tab_path(svg, insert_x0, insert_x1, insert_y, insert_d, upward=True)
    cut_line(svg, [(insert_x1, insert_y), (x1, insert_y)])

    fold_line(svg, [(x0, y0), (x1, y0)])
    fold_line(svg, [(x0, y1), (x1, y1)])
    fold_line(svg, [(x0, y0), (x0, y1)])
    fold_line(svg, [(x1, y0), (x1, y1)])
    fold_line(svg, [(x0, lid_y), (x1, lid_y)])
    fold_line(svg, [(front_flap_x0, front_y), (front_flap_x1, front_y)])
    fold_line(svg, [(insert_x0, insert_y), (insert_x1, insert_y)])
    fold_line(svg, [(x0, side_flap_y0), (x0, side_flap_y1)])
    fold_line(svg, [(x1, side_flap_y0), (x1, side_flap_y1)])

    slot_w, slot_h = NET["slot"]
    slot_offset = NET["slot_x_from_side"]
    for panel_y in (front_y, back_y):
        for slot_x in (x0 + slot_offset, x1 - slot_offset):
            rounded_slot(svg, slot_x, panel_y + wall_h / 2, slot_w, slot_h)

    panel_label(svg, x0, y0, bottom_w, bottom_d, "Bunn", "97 x 72 mm")
    panel_label(svg, x0, front_y, bottom_w, wall_h, "Frontvegg", "97 x 43 mm")
    panel_label(svg, x0, back_y, bottom_w, wall_h, "Bakvegg", "97 x 43 mm")
    panel_label(svg, x0, lid_y, lid_w, lid_d, "Lokk", "97 x 72 mm")
    panel_label(svg, left_outer_x, y0, wall_h, bottom_d, "Venstre\nsidevegg", "43 x 72 mm")
    panel_label(svg, x1, y0, wall_h, bottom_d, "Hoyre\nsidevegg", "43 x 72 mm")
    panel_label(svg, insert_x0, insert_y, insert_w, insert_d, "Innstikksflik", "87 x 20 mm")
    panel_label(svg, front_flap_x0, front_inner_y, front_flap_w, front_flap_d, "Innvendig\nfrontflik", "87 x 16 mm")

    dim_h(svg, x0, x1, y0 + 6.0, "97 mm")
    dim_v(svg, y0, y1, x1 - 6.0, "72 mm")
    dim_v(svg, front_y, y0, x0 + 6.0, "43 mm")
    dim_v(svg, back_y, lid_y, x1 - 6.0, "43 mm")
    dim_v(svg, lid_y, insert_y, x0 + 6.0, "72 mm")


def draw_legend(svg: Svg, page_name: str) -> None:
    net_w, net_h = net_size()
    svg.text(
        5,
        svg.height - 5,
        f"DIY package net - {page_name}, print at 100%; grid = 1 mm",
        size=2.35,
        anchor="start",
        weight="700",
    )
    svg.text(
        5,
        svg.height - 9,
        f"Outer box target 97 x 72 x 43 mm; net footprint {net_w:.0f} x {net_h:.0f} mm",
        size=1.85,
        anchor="start",
    )

    legend_x = svg.width - 57
    cut_line(svg, [(legend_x, 9), (legend_x + 13, 9)], width=0.34)
    svg.text(legend_x + 16, 9, "solid = cut", size=2.0, anchor="start")
    fold_line(svg, [(legend_x, 5), (legend_x + 13, 5)], width=0.28)
    svg.text(legend_x + 16, 5, "dashed = fold", size=2.0, anchor="start")

    svg.line([(8, 8), (58, 8)], stroke=CUT, width=0.25)
    svg.line([(8, 6.5), (8, 9.5)], stroke=CUT, width=0.20)
    svg.line([(58, 6.5), (58, 9.5)], stroke=CUT, width=0.20)
    svg.text(33, 11.0, "50 mm scale check", size=1.9)


def write_measurements() -> None:
    bottom_w, bottom_d = NET["bottom"]
    wall_h = NET["wall_h"]
    lid_w, lid_d = NET["lid"]
    insert_w, insert_d = NET["insert_tab"]
    front_w, front_d = NET["front_inner_flap"]
    slot_w, slot_h = NET["slot"]
    net_w, net_h = net_size()
    text = f"""ELEMENT WAR - DIY CARDBOARD PACKAGE TEMPLATE
All dimensions are in millimetres. Print PNG/SVG at 100% scale.

Source dimensions from physical/mesurement diagrams:
Outer box target: {SPEC['base_outer'][0]:.1f} x {SPEC['base_outer'][1]:.1f} x {SPEC['base_outer'][2]:.1f}
Closed production target with rigid lid: {SPEC['closed_total'][0]:.1f} x {SPEC['closed_total'][1]:.1f} x {SPEC['closed_total'][2]:.1f}
Internal tray reference: {SPEC['tray_outer'][0]:.1f} x {SPEC['tray_outer'][1]:.1f} x {SPEC['tray_outer'][2]:.1f}

DIY tuck/lock net panels:
Bottom: {bottom_w:.1f} x {bottom_d:.1f}
Front wall: {bottom_w:.1f} x {wall_h:.1f}
Back wall: {bottom_w:.1f} x {wall_h:.1f}
Left side wall: {wall_h:.1f} x {bottom_d:.1f}
Right side wall: {wall_h:.1f} x {bottom_d:.1f}
Lid: {lid_w:.1f} x {lid_d:.1f}
Lid insert tab: {insert_w:.1f} x {insert_d:.1f}
Inner front flap: {front_w:.1f} x {front_d:.1f}

Locking details:
Lock tab protrusion: {NET['lock_tab_depth']:.1f}
Lock tab height: {NET['lock_tab_h']:.1f}
Lock slot: {slot_w:.1f} x {slot_h:.1f}
Slot centre from left/right panel edge: {NET['slot_x_from_side']:.1f}

Canvas and fit:
A4 transparent sheet: {A4[0]:.1f} x {A4[1]:.1f}
A3 transparent sheet: {A3[0]:.1f} x {A3[1]:.1f}
Net footprint including lock tabs: {net_w:.1f} x {net_h:.1f}
A4 true-scale remaining margin: {(A4[0] - net_w) / 2:.1f} mm left/right, {(A4[1] - net_h) / 2:.1f} mm top/bottom
A3 true-scale remaining margin: {(A3[0] - net_w) / 2:.1f} mm left/right, {(A3[1] - net_h) / 2:.1f} mm top/bottom

Practical note:
A4 technically fits but is tight for printer non-printable margins. A3 is safer
for real DIY cutting, labels, and alignment marks.
"""
    (DATA_DIR / "diy_package_measurements.txt").write_text(text, encoding="utf-8")


def export_png(svg_path: Path, png_path: Path) -> bool:
    inkscape = shutil.which("inkscape")
    if not inkscape:
        print(f"Skipped PNG export, inkscape not found: {png_path}")
        return False
    subprocess.run(
        [
            inkscape,
            str(svg_path),
            "--export-type=png",
            f"--export-filename={png_path}",
            f"--export-dpi={DPI}",
            "--export-background-opacity=0",
            "--export-area-page",
        ],
        check=True,
    )
    return True


def generate_page(page_name: str, size: tuple[float, float]) -> None:
    svg = Svg(*size)
    draw_grid(svg)
    draw_template(svg)
    draw_legend(svg, page_name.upper())

    svg_path = SVG_DIR / f"diy_package_template_{page_name}_transparent.svg"
    png_path = PNG_DIR / f"diy_package_template_{page_name}_transparent.png"
    svg_path.write_text(svg.to_string(), encoding="utf-8")
    exported = export_png(svg_path, png_path)

    print(f"Generated SVG: {svg_path}")
    if exported:
        print(f"Generated PNG: {png_path}")


def main() -> None:
    generate_page("a3", A3)
    generate_page("a4", A4)
    write_measurements()
    print(f"Generated measurements: {DATA_DIR / 'diy_package_measurements.txt'}")


if __name__ == "__main__":
    main()
