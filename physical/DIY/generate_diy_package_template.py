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


# The DIY sheet is a cardboard tuck/lock prototype, not the magnetic production
# box. The finished box target is 93 x 68 x 41 mm. The larger cut dimensions
# below compensate for the 0.50 mm cardboard used by the prototype.
SPEC = {
    "base_outer": (93.0, 68.0, 41.0),
    "cut_panel": (94.5, 68.5, 41.5),
    "closed_total": (93.0, 68.0, 41.0),
    "tray_outer": (92.0, 67.0, 21.5),
    "base_inner": (93.0, 68.0, 41.0),
    "board": 0.5,
    "lid_panel": (94.5, 68.5, 0.5),
    "front_flap": (94.5, 20.0, 0.5),
    "magnet_edge_offset": 18.0,
}

A4 = (210.0, 297.0)
A3 = (297.0, 420.0)
DPI = 300
MM_PER_INCH = 25.4

CUT = "#101010"
FOLD = "#101010"
PARTIAL_CUT = "#101010"
GRID = "#2f8fcf"
DIM = "#333333"

NET = {
    "bottom": (SPEC["cut_panel"][0], SPEC["cut_panel"][1]),
    "wall_h": SPEC["cut_panel"][2],
    "lid": (SPEC["lid_panel"][0], SPEC["lid_panel"][1]),
    "insert_tab": (SPEC["cut_panel"][0] - 10.0, SPEC["front_flap"][1]),
    "front_inner_flap": (SPEC["cut_panel"][0] - 10.0, 16.0),
    "lid_side_flap": (20.0, 54.0),
    # Side-wall flaps leave 1 mm clearance at each end for folding.
    "wall_side_flap_depth": 39.5,
    "a3_intermediate_wall": 41.5,
    "lock_tab_depth": 8.0,
    "lock_tab_h": 16.0,
    "slot": (1.8, 8.0),
    "slot_x_from_fold": 1.2,
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


def net_size(page_name: str) -> tuple[float, float]:
    bottom_w, bottom_d = NET["bottom"]
    wall_h = NET["wall_h"]
    front_flap_d = NET["front_inner_flap"][1]
    lid_d = NET["lid"][1]
    insert_d = NET["insert_tab"][1]
    lock_margin = NET["lock_tab_depth"] if page_name == "a3" else 0.0
    intermediate = NET["a3_intermediate_wall"] if page_name == "a3" else 0.0
    width = lock_margin + wall_h + intermediate + bottom_w + intermediate + wall_h + lock_margin
    height = front_flap_d + wall_h + bottom_d + wall_h + lid_d + insert_d
    return width, height


def cut_line(svg: Svg, points: list[tuple[float, float]], width: float = 0.34) -> None:
    svg.line(points, stroke=CUT, width=width)


def fold_line(svg: Svg, points: list[tuple[float, float]], width: float = 0.28) -> None:
    svg.line(points, stroke=FOLD, width=width, dash="3.2,2.4")


def partial_cut_line(svg: Svg, points: list[tuple[float, float]], width: float = 0.34) -> None:
    svg.line(points, stroke=PARTIAL_CUT, width=width, dash="1.0,1.0")


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
    notch_radius = 4.0
    bottom_y = y - depth
    notch_x = (x0 + x1) / 2
    svg.path(
        [
            ("M", x0, y),
            ("L", x0 + inset, bottom_y),
            ("L", notch_x - notch_radius, bottom_y),
            ("Q", notch_x, bottom_y + notch_radius, notch_x + notch_radius, bottom_y),
            ("L", x1 - inset, bottom_y),
            ("L", x1, y),
        ],
        width=0.34,
    )


def side_panel_path(
    svg: Svg,
    attach_x: float,
    outer_x: float,
    y0: float,
    y1: float,
    mirror: bool,
    outward_locks: bool,
) -> None:
    tab_h = NET["lock_tab_h"]
    tab_d = NET["lock_tab_depth"]
    centers = (y0 + 16.0, y1 - 16.0)
    if outward_locks:
        direction = -1.0 if mirror else 1.0
    else:
        # A4 uses tongues cut into the side-wall material rather than an
        # extension beyond the sheet footprint.
        direction = 1.0 if mirror else -1.0

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


def plain_side_panel_path(
    svg: Svg,
    attach_x: float,
    outer_x: float,
    y0: float,
    y1: float,
) -> None:
    cut_line(svg, [(attach_x, y0), (outer_x, y0), (outer_x, y1), (attach_x, y1)])


def wall_side_flap_path(
    svg: Svg,
    attach_x: float,
    y0: float,
    y1: float,
    direction: float,
    depth: float,
) -> None:
    """Draw a front/back wall flap that folds into a side-wall compartment."""
    inset = 4.0
    outer_x = attach_x + direction * depth
    cut_line(
        svg,
        [
            (attach_x, y0),
            (outer_x, y0 + inset),
            (outer_x, y1 - inset),
            (attach_x, y1),
        ],
    )
    fold_line(svg, [(attach_x, y0), (attach_x, y1)])


def intermediate_side_wall_path(
    svg: Svg,
    attach_x: float,
    y0: float,
    y1: float,
    direction: float,
) -> None:
    """Draw the A3 wall panel between the bottom and the main side wall."""
    outer_x = attach_x + direction * NET["a3_intermediate_wall"]
    cut_line(svg, [(attach_x, y0), (outer_x, y0), (outer_x, y1), (attach_x, y1)])
    fold_line(svg, [(attach_x, y0), (attach_x, y1)])
    fold_line(svg, [(outer_x, y0), (outer_x, y1)])


def rounded_slot(svg: Svg, cx: float, cy: float, width: float, height: float, partial: bool = False) -> None:
    svg.rect(
        cx - width / 2,
        cy - height / 2,
        width,
        height,
        stroke=PARTIAL_CUT if partial else CUT,
        width=0.30,
        dash="1.0,1.0" if partial else None,
        rx=width / 2,
    )


def draw_a4_interlocks(
    svg: Svg,
    x0: float,
    x1: float,
    y0: float,
    y1: float,
    front_y: float,
    back_y: float,
    lid_y: float,
    flap_depth: float,
) -> None:
    """Draw half-depth slits that interlock each side wall with its flaps."""
    wall_h = NET["wall_h"]
    side_centres = (x0 - wall_h / 2, x1 + wall_h / 2)
    side_cut_depth = wall_h / 2
    flap_cut_depth = flap_depth / 2

    for side_x in side_centres:
        partial_cut_line(svg, [(side_x, y0), (side_x, y0 + side_cut_depth)])
        partial_cut_line(svg, [(side_x, y1), (side_x, y1 - side_cut_depth)])

    for flap_y in ((front_y + y0) / 2, (back_y + lid_y) / 2):
        partial_cut_line(svg, [(x0 - flap_depth, flap_y), (x0 - flap_depth + flap_cut_depth, flap_y)])
        partial_cut_line(svg, [(x1 + flap_depth, flap_y), (x1 + flap_depth - flap_cut_depth, flap_y)])


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


def draw_template(svg: Svg, page_name: str) -> None:
    bottom_w, bottom_d = NET["bottom"]
    wall_h = NET["wall_h"]
    lid_w, lid_d = NET["lid"]
    insert_w, insert_d = NET["insert_tab"]
    front_flap_w, front_flap_d = NET["front_inner_flap"]
    lock_depth = NET["lock_tab_depth"]
    total_w, total_h = net_size(page_name)
    outward_locks = page_name == "a3"
    lock_margin = NET["lock_tab_depth"] if outward_locks else 0.0
    intermediate = NET["a3_intermediate_wall"] if outward_locks else 0.0

    x0 = (svg.width - total_w) / 2 + lock_margin + wall_h + intermediate
    x1 = x0 + bottom_w
    y0 = (svg.height - total_h) / 2 + front_flap_d + wall_h
    y1 = y0 + bottom_d
    front_y = y0 - wall_h
    back_y = y1
    lid_y = back_y + wall_h
    insert_y = lid_y + lid_d
    front_inner_y = front_y - front_flap_d

    left_attach_x = x0 - intermediate
    right_attach_x = x1 + intermediate
    left_outer_x = left_attach_x - wall_h
    right_outer_x = right_attach_x + wall_h

    if outward_locks:
        intermediate_side_wall_path(svg, x0, y0, y1, -1.0)
        intermediate_side_wall_path(svg, x1, y0, y1, 1.0)
        side_panel_path(svg, left_attach_x, left_outer_x, y0, y1, mirror=True, outward_locks=True)
        side_panel_path(svg, right_attach_x, right_outer_x, y0, y1, mirror=False, outward_locks=True)
    else:
        plain_side_panel_path(svg, x0, x0 - wall_h, y0, y1)
        plain_side_panel_path(svg, x1, x1 + wall_h, y0, y1)

    # The front/back side flap is one wall span only. The A3 intermediate wall
    # is a separate fold panel and must not make this flap twice as deep.
    flap_depth = NET["wall_side_flap_depth"]
    wall_side_flap_path(svg, x0, front_y, y0, -1.0, flap_depth)
    wall_side_flap_path(svg, x1, front_y, y0, 1.0, flap_depth)
    wall_side_flap_path(svg, x0, back_y, lid_y, -1.0, flap_depth)
    wall_side_flap_path(svg, x1, back_y, lid_y, 1.0, flap_depth)

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

    if outward_locks:
        slot_w, slot_h = NET["slot"]
        slot_offset = NET["slot_x_from_fold"]
        for slot_y in (y0 + 16.0, y1 - 16.0):
            for slot_x in (x0 + slot_offset, x1 - slot_offset):
                rounded_slot(svg, slot_x, slot_y, slot_w, slot_h, partial=True)
    else:
        draw_a4_interlocks(svg, x0, x1, y0, y1, front_y, back_y, lid_y, flap_depth)

    panel_label(svg, x0, y0, bottom_w, bottom_d, "Bunn", "94.5 x 68.5 mm")
    panel_label(svg, x0, front_y, bottom_w, wall_h, "Frontvegg", "94.5 x 41.5 mm")
    panel_label(svg, x0, back_y, bottom_w, wall_h, "Bakvegg", "94.5 x 41.5 mm")
    panel_label(svg, x0, lid_y, lid_w, lid_d, "Lokk", "94.5 x 68.5 mm")
    panel_label(svg, left_outer_x, y0, wall_h, bottom_d, "Venstre\nsidevegg", "41.5 x 68.5 mm")
    panel_label(svg, right_attach_x, y0, wall_h, bottom_d, "Hoyre\nsidevegg", "41.5 x 68.5 mm")
    if outward_locks:
        panel_label(svg, x0 - intermediate, y0, intermediate, bottom_d, "Mellomvegg", "41.5 x 68.5 mm")
        panel_label(svg, x1, y0, intermediate, bottom_d, "Mellomvegg", "41.5 x 68.5 mm")
    panel_label(svg, insert_x0, insert_y, insert_w, insert_d, "Innstikksflik", "84.5 x 20 mm")
    panel_label(svg, front_flap_x0, front_inner_y, front_flap_w, front_flap_d, "Innvendig\nfrontflik", "84.5 x 16 mm")

    dim_h(svg, x0, x1, y0 + 6.0, f"{bottom_w:.1f} mm")
    dim_v(svg, y0, y1, x1 - 6.0, f"{bottom_d:.1f} mm")
    dim_v(svg, front_y, y0, x0 + 6.0, f"{wall_h:.1f} mm")
    dim_v(svg, back_y, lid_y, x1 - 6.0, f"{wall_h:.1f} mm")
    dim_v(svg, lid_y, insert_y, x0 + 6.0, f"{lid_d:.1f} mm")


def draw_legend(svg: Svg, page_name: str) -> None:
    net_w, net_h = net_size(page_name.lower())
    lock_note = "outward locks + bottom slots" if page_name.lower() == "A3" else "plain side walls"
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
        f"Finished target 93 x 68 x 41 mm; {lock_note}; net {net_w:.1f} x {net_h:.1f} mm",
        size=1.85,
        anchor="start",
    )

    legend_x = svg.width - 57
    cut_line(svg, [(legend_x, 12), (legend_x + 13, 12)], width=0.34)
    svg.text(legend_x + 16, 12, "solid = full cut", size=2.0, anchor="start")
    fold_line(svg, [(legend_x, 8), (legend_x + 13, 8)], width=0.28)
    svg.text(legend_x + 16, 8, "dashed = fold", size=2.0, anchor="start")
    partial_cut_line(svg, [(legend_x, 4), (legend_x + 13, 4)], width=0.30)
    svg.text(legend_x + 16, 4, "dotted = partial cut / slit", size=2.0, anchor="start")

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
    a4_w, a4_h = net_size("a4")
    a3_w, a3_h = net_size("a3")
    text = f"""ELEMENT WAR - DIY CARDBOARD PACKAGE TEMPLATE
All dimensions are in millimetres. Print PNG/SVG at 100% scale.

DIY finished target:
Outer box: {SPEC['base_outer'][0]:.1f} x {SPEC['base_outer'][1]:.1f} x {SPEC['base_outer'][2]:.1f}
Cut panel allowance: {SPEC['cut_panel'][0]:.1f} x {SPEC['cut_panel'][1]:.1f} x {SPEC['cut_panel'][2]:.1f}
Closed target: {SPEC['closed_total'][0]:.1f} x {SPEC['closed_total'][1]:.1f} x {SPEC['closed_total'][2]:.1f}
Internal tray reference: {SPEC['tray_outer'][0]:.1f} x {SPEC['tray_outer'][1]:.1f} x {SPEC['tray_outer'][2]:.1f}

DIY tuck/lock net panels:
Bottom cut panel: {bottom_w:.1f} x {bottom_d:.1f}
Front wall cut panel: {bottom_w:.1f} x {wall_h:.1f}
Back wall cut panel: {bottom_w:.1f} x {wall_h:.1f}
Left side wall cut panel: {wall_h:.1f} x {bottom_d:.1f}
Right side wall cut panel: {wall_h:.1f} x {bottom_d:.1f}
Lid: {lid_w:.1f} x {lid_d:.1f}
Lid insert tab: {insert_w:.1f} x {insert_d:.1f}
Inner front flap: {front_w:.1f} x {front_d:.1f}

Locking details (A3 only):
Lock tab protrusion: {NET['lock_tab_depth']:.1f}
Lock tab height: {NET['lock_tab_h']:.1f}
Lock slot: {slot_w:.1f} x {slot_h:.1f}
Lock slot centre from side-wall fold: {NET['slot_x_from_fold']:.1f}
A4 front/back side flap depth: {NET['wall_side_flap_depth']:.1f} (1.0 mm + 1.0 mm bend clearance)
A4 side-wall locks and bottom slots: none
A4 interlock: 2 half-depth cuts in each side wall + 1 half-depth cut in each front/back side flap
A4 side-wall cut depth: {NET['wall_h'] / 2:.2f}
A4 side-flap cut depth: {NET['wall_side_flap_depth'] / 2:.2f}
A3 intermediate side wall: {NET['a3_intermediate_wall']:.1f}
A3 front/back side flap depth: {NET['wall_side_flap_depth']:.1f}

Canvas and fit:
A4 transparent sheet: {A4[0]:.1f} x {A4[1]:.1f}
A3 transparent sheet: {A3[0]:.1f} x {A3[1]:.1f}
A4 compact net footprint: {a4_w:.1f} x {a4_h:.1f}
A4 true-scale remaining margin: {(A4[0] - a4_w) / 2:.1f} mm left/right, {(A4[1] - a4_h) / 2:.1f} mm top/bottom
A3 outward-lock net footprint: {a3_w:.1f} x {a3_h:.1f}
A3 true-scale remaining margin: {(A3[0] - a3_w) / 2:.1f} mm left/right, {(A3[1] - a3_h) / 2:.1f} mm top/bottom

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
    draw_template(svg, page_name)
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
