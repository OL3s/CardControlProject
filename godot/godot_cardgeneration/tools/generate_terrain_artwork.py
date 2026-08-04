#!/usr/bin/env python3
"""Generate deterministic, background-only terrain artwork for the default deck.

The recipes intentionally contain no card frame, text, icons, resource markers, or
central element sigil. They render at 590x890 and upscale with Lanczos to the
canonical 2360x3560 master. Every composition is balanced under 180-degree rotation.
"""

from __future__ import annotations

import argparse
import math
import random
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


WORK_SIZE = (590, 890)
MASTER_SIZE = (2360, 3560)
CENTER = (295, 445)
ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "assets" / "artwork" / "terrain"


@dataclass(frozen=True)
class Terrain:
    card_id: str
    title: str
    element: str
    form: str
    tier: int


TERRAINS = (
    Terrain("terrain_neutral_1_a", "Silent Slate Eye", "neutral", "slate_eye", 1),
    Terrain("terrain_neutral_1_b", "Moss Rift", "neutral", "moss_rift", 1),
    Terrain("terrain_neutral_1_c", "Ember Cleft", "neutral", "ember_cleft", 1),
    Terrain("terrain_neutral_1_d", "Rainstone Bowl", "neutral", "rain_bowl", 1),
    Terrain("terrain_neutral_1_e", "Root Blocks", "neutral", "root_blocks", 1),
    Terrain("terrain_neutral_2_a", "Fault Expanse", "neutral", "fault_expanse", 2),
    Terrain("terrain_neutral_2_b", "Karst Crown", "neutral", "karst_crown", 2),
    Terrain("terrain_neutral_2_c", "Bedrock Convergence", "neutral", "convergence", 2),
    Terrain("terrain_grass_1_a", "Spore Crown", "grass", "spore_crown", 1),
    Terrain("terrain_grass_1_b", "Slate Mycelium", "grass", "slate_mycelium", 1),
    Terrain("terrain_grass_1_c", "Twin Grove", "grass", "twin_grove", 1),
    Terrain("terrain_grass_2_a", "Rootwater Basin", "grass", "rootwater_basin", 2),
    Terrain("terrain_flame_1_a", "Ember Rose", "flame", "ember_rose", 1),
    Terrain("terrain_flame_1_b", "Slag Ring", "flame", "slag_ring", 1),
    Terrain("terrain_flame_1_c", "Twin Crater", "flame", "twin_crater", 1),
    Terrain("terrain_flame_2_a", "Green Caldera", "flame", "green_caldera", 2),
    Terrain("terrain_water_1_a", "Dark Mirror", "water", "dark_mirror", 1),
    Terrain("terrain_water_1_b", "Slate Islets", "water", "slate_islets", 1),
    Terrain("terrain_water_1_c", "Twin Pools", "water", "twin_pools", 1),
    Terrain("terrain_water_2_a", "Geothermal Current", "water", "geothermal_current", 2),
)


PALETTES = {
    "neutral": {
        "bg": (8, 10, 13), "field": (27, 30, 35), "body": (48, 53, 60),
        "body2": (78, 84, 92), "accent": (132, 142, 153), "light": (198, 205, 211),
    },
    "grass": {
        "bg": (5, 11, 8), "field": (15, 34, 24), "body": (27, 48, 32),
        "body2": (53, 76, 43), "accent": (104, 151, 61), "light": (194, 220, 81),
    },
    "flame": {
        "bg": (12, 7, 9), "field": (46, 19, 23), "body": (55, 37, 37),
        "body2": (92, 44, 32), "accent": (205, 78, 31), "light": (255, 188, 59),
    },
    "water": {
        "bg": (4, 9, 15), "field": (11, 29, 46), "body": (22, 47, 62),
        "body2": (28, 72, 88), "accent": (52, 151, 176), "light": (166, 226, 233),
    },
}

CUE_COLORS = {
    "stone": (114, 123, 134),
    "grass": (111, 158, 67),
    "flame": (221, 87, 32),
    "water": (64, 159, 187),
}


def rotated_points(points):
    return [(WORK_SIZE[0] - x, WORK_SIZE[1] - y) for x, y in points]


class Painter:
    def __init__(self, terrain: Terrain):
        self.terrain = terrain
        self.palette = PALETTES[terrain.element]
        self.random = random.Random(terrain.card_id)
        self.image = Image.new("RGB", WORK_SIZE, self.palette["bg"])
        self.draw = ImageDraw.Draw(self.image, "RGBA")
        self._paint_background()

    def _paint_background(self) -> None:
        p = self.palette
        for radius in range(430, 35, -10):
            t = 1 - radius / 430
            alpha = int(6 + 21 * math.sin(t * math.pi))
            self.draw.ellipse(
                (295 - radius * .67, 445 - radius, 295 + radius * .67, 445 + radius),
                fill=(*p["field"], alpha),
            )
        # Every texture mark has a rotated partner to preserve terrain orientation.
        for _ in range(430):
            x = self.random.randrange(20, 570)
            y = self.random.randrange(35, 445)
            value = self.random.randrange(9, 28)
            alpha = self.random.randrange(8, 24)
            self.draw.point((x, y), fill=(value, value, value, alpha))
            self.draw.point((590 - x, 890 - y), fill=(value, value, value, alpha))

    def glow(self, shapes, color=None, blur=24, symmetric=False) -> None:
        layer = Image.new("RGBA", WORK_SIZE)
        draw = ImageDraw.Draw(layer, "RGBA")
        rgba = (*(color or self.palette["accent"]), 150)
        expanded = list(shapes)
        if symmetric:
            for kind, points, width in shapes:
                if kind == "line":
                    expanded.append((kind, rotated_points(points), width))
                elif kind in {"ellipse", "polygon"}:
                    expanded.append((kind, self._rotate_shape(points, kind), width))
        for kind, points, width in expanded:
            if kind == "line":
                draw.line(points, fill=rgba, width=width, joint="curve")
            elif kind == "ellipse":
                draw.ellipse(points, fill=rgba)
            elif kind == "polygon":
                draw.polygon(points, fill=rgba)
        self.image = Image.alpha_composite(
            self.image.convert("RGBA"), layer.filter(ImageFilter.GaussianBlur(blur))
        ).convert("RGB")
        self.draw = ImageDraw.Draw(self.image, "RGBA")

    @staticmethod
    def _rotate_shape(shape, kind):
        if kind == "polygon":
            return rotated_points(shape)
        x1, y1, x2, y2 = shape
        return (590 - x2, 890 - y2, 590 - x1, 890 - y1)

    def line(self, points, fill=None, width=8, symmetric=False) -> None:
        color = fill or (*self.palette["body2"], 255)
        self.draw.line(points, fill=color, width=width, joint="curve")
        if symmetric:
            self.draw.line(rotated_points(points), fill=color, width=width, joint="curve")

    def polygon(self, points, fill=None, outline=None, width=3, symmetric=False) -> None:
        color = fill or (*self.palette["body"], 255)
        self.draw.polygon(points, fill=color)
        if outline:
            self.draw.line([*points, points[0]], fill=outline, width=width, joint="curve")
        if symmetric:
            other = rotated_points(points)
            self.draw.polygon(other, fill=color)
            if outline:
                self.draw.line([*other, other[0]], fill=outline, width=width, joint="curve")

    def ellipse(self, box, fill=None, outline=None, width=3, symmetric=False) -> None:
        color = fill or (*self.palette["body"], 255)
        self.draw.ellipse(box, fill=color, outline=outline, width=width)
        if symmetric:
            other = self._rotate_shape(box, "ellipse")
            self.draw.ellipse(other, fill=color, outline=outline, width=width)

    def arc(self, box, start, end, fill, width=5, symmetric=False) -> None:
        self.draw.arc(box, start, end, fill=fill, width=width)
        if symmetric:
            other = self._rotate_shape(box, "ellipse")
            self.draw.arc(other, start + 180, end + 180, fill=fill, width=width)

    def radial_polygon(self, radius, sides=6, rotation=0, fill=None, outline=None, width=4) -> None:
        cx, cy = CENTER
        points = [
            (
                cx + math.cos(rotation + i * math.tau / sides) * radius,
                cy + math.sin(rotation + i * math.tau / sides) * radius,
            )
            for i in range(sides)
        ]
        self.polygon(points, fill, outline, width)

    def finish(self) -> Image.Image:
        # Keep the renderer's central sigil zone quiet without painting the sigil itself.
        quiet = Image.new("RGBA", WORK_SIZE)
        qd = ImageDraw.Draw(quiet, "RGBA")
        qd.ellipse((205, 345, 385, 545), fill=(4, 7, 9, 45))
        quiet = quiet.filter(ImageFilter.GaussianBlur(25))
        result = Image.alpha_composite(self.image.convert("RGBA"), quiet)

        # Dark outer values protect corner resource overlays and the bottom panel.
        mask = Image.new("L", WORK_SIZE, 0)
        md = ImageDraw.Draw(mask)
        for i in range(105):
            md.ellipse((i, i * 1.25, 590 - i, 890 - i * 1.25), outline=max(0, 150 - i), width=2)
        shade = Image.new("RGBA", WORK_SIZE, (0, 0, 0, 0))
        shade.putalpha(mask)
        result = Image.alpha_composite(result, shade)
        return result.resize(MASTER_SIZE, Image.Resampling.LANCZOS)


def slate_ridge(p: Painter, y=260, spread=185, height=75, color=None) -> None:
    points = [
        (295 - spread, y + 22), (205, y - 18), (265, y - height),
        (340, y - 48), (295 + spread, y + 24), (390, y + 55), (190, y + 58),
    ]
    p.polygon(points, color or (*p.palette["body"], 255), (*p.palette["body2"], 170), 4, True)


def crack(p: Painter, points, color=None, width=5, glow=False, symmetric=True) -> None:
    chosen = color or (*p.palette["accent"], 190)
    if glow:
        p.glow([("line", points, width * 2)], chosen[:3], 18, symmetric)
    p.line(points, chosen, width, symmetric)


def pool(p: Painter, box, color=None, symmetric=False) -> None:
    chosen = color or CUE_COLORS["water"]
    p.glow([("ellipse", box, 0)], chosen, 22, symmetric)
    p.ellipse(box, (*chosen, 125), (*PALETTES["water"]["light"], 155), 4, symmetric)
    inset = 12
    p.arc(
        (box[0] + inset, box[1] + inset, box[2] - inset, box[3] - inset),
        15, 175, (*PALETTES["water"]["accent"], 150), 3, symmetric,
    )


def rosette(p: Painter, center, radius=44, petals=6, color=None, symmetric=False) -> None:
    cx, cy = center
    chosen = color or CUE_COLORS["grass"]
    for i in range(petals):
        angle = i * math.tau / petals
        x = cx + math.cos(angle) * radius * .55
        y = cy + math.sin(angle) * radius * .55
        dx = math.cos(angle) * radius
        dy = math.sin(angle) * radius
        nx = -math.sin(angle) * radius * .28
        ny = math.cos(angle) * radius * .28
        pts = [(x - nx, y - ny), (x + dx, y + dy), (x + nx, y + ny), (cx, cy)]
        p.polygon(pts, (*chosen, 190), (*p.palette["light"], 80), 2, symmetric)


def crater(p: Painter, center, radii=(82, 48), color=None, symmetric=False) -> None:
    cx, cy = center
    chosen = color or CUE_COLORS["flame"]
    rx, ry = radii
    p.glow([("ellipse", (cx - rx, cy - ry, cx + rx, cy + ry), 0)], chosen, 28, symmetric)
    p.ellipse(
        (cx - rx, cy - ry, cx + rx, cy + ry),
        (24, 14, 15, 230), (*chosen, 220), 9, symmetric,
    )
    p.ellipse(
        (cx - rx * .55, cy - ry * .5, cx + rx * .55, cy + ry * .5),
        (*chosen, 125), (*PALETTES["flame"]["light"], 175), 4, symmetric,
    )


def render_neutral(p: Painter) -> None:
    form = p.terrain.form
    if form == "slate_eye":
        slate_ridge(p, 275)
        for offset in (-65, 0, 65):
            crack(p, [(190 + offset, 330), (220 + offset, 360), (205 + offset, 395)], width=3)
        pool(p, (245, 383, 345, 455), CUE_COLORS["stone"])
    elif form == "moss_rift":
        slate_ridge(p, 280)
        for x in (145, 225, 315):
            crack(p, [(x, 330), (x + 25, 365), (x + 8, 410)], CUE_COLORS["grass"] + (180,), 6)
            rosette(p, (x + 12, 350), 27, 5, symmetric=True)
    elif form == "ember_cleft":
        slate_ridge(p, 270)
        for x in (155, 235):
            crack(p, [(x, 315), (x + 32, 355), (x + 5, 410)], CUE_COLORS["flame"] + (230,), 7, True)
    elif form == "rain_bowl":
        slate_ridge(p, 250, 205, 58)
        pool(p, (115, 320, 250, 402), symmetric=True)
        p.arc((85, 285, 505, 605), 195, 345, (*p.palette["body2"], 170), 16)
        p.arc((85, 285, 505, 605), 15, 165, (*p.palette["body2"], 170), 16)
    elif form == "root_blocks":
        p.polygon([(110, 230), (240, 210), (270, 340), (140, 375)], outline=(*p.palette["body2"], 170), symmetric=True)
        for x in (155, 205, 255):
            p.line([(x, 345), (x + 45, 395), (x + 15, 455)], (*CUE_COLORS["grass"], 185), 8, True)
        rosette(p, (160, 385), 30, 6, symmetric=True)
    elif form == "fault_expanse":
        for radius in (265, 205):
            p.radial_polygon(radius, 6, math.pi / 6, (*p.palette["body"], 75), (*p.palette["body2"], 170), 7)
        for angle in (0, math.pi / 3, 2 * math.pi / 3):
            x = 295 + math.cos(angle) * 115
            y = 445 + math.sin(angle) * 115
            crack(p, [(x, y), (x + math.cos(angle) * 100, y + math.sin(angle) * 100)], CUE_COLORS["flame"] + (220,), 8, True)
    elif form == "karst_crown":
        for radius in (255, 205, 155):
            p.arc((295 - radius, 445 - radius * .65, 295 + radius, 445 + radius * .65), 195, 345, (*p.palette["body2"], 190), 14)
            p.arc((295 - radius, 445 - radius * .65, 295 + radius, 445 + radius * .65), 15, 165, (*p.palette["body2"], 190), 14)
        pool(p, (115, 285, 215, 355), symmetric=True)
        pool(p, (360, 315, 445, 375), symmetric=True)
    else:
        for i in range(6):
            angle = i * math.tau / 6
            inner, outer = 125, 270
            points = [
                (295 + math.cos(angle - .38) * inner, 445 + math.sin(angle - .38) * inner),
                (295 + math.cos(angle - .28) * outer, 445 + math.sin(angle - .28) * outer),
                (295 + math.cos(angle + .28) * outer, 445 + math.sin(angle + .28) * outer),
                (295 + math.cos(angle + .38) * inner, 445 + math.sin(angle + .38) * inner),
            ]
            p.polygon(points, (*p.palette["body"], 180), (*p.palette["body2"], 120), 3)
        rosette(p, (180, 290), 30, 6, CUE_COLORS["grass"], True)
        crater(p, (410, 300), (45, 28), symmetric=True)
        pool(p, (115, 390, 205, 450), symmetric=True)


def render_grass(p: Painter) -> None:
    form = p.terrain.form
    if form == "spore_crown":
        for i in range(6):
            angle = i * math.tau / 6
            rosette(p, (295 + math.cos(angle) * 185, 445 + math.sin(angle) * 245), 48, 7)
        p.radial_polygon(125, 6, math.pi / 6, (8, 19, 13, 100), (*p.palette["accent"], 110), 4)
    elif form == "slate_mycelium":
        for i in range(6):
            angle = i * math.tau / 6
            x, y = 295 + math.cos(angle) * 205, 445 + math.sin(angle) * 270
            p.polygon([(x - 48, y - 22), (x + 34, y - 35), (x + 55, y + 20), (x - 25, y + 34)], (*CUE_COLORS["stone"], 150))
            p.line([(x, y), CENTER], (*p.palette["accent"], 145), 5)
        for radius in (150, 225):
            p.ellipse((295 - radius, 445 - radius * .72, 295 + radius, 445 + radius * .72), outline=(*p.palette["light"], 75), width=4)
    elif form == "twin_grove":
        for center, size in (((205, 300), 72), ((385, 590), 72), ((405, 320), 40), ((185, 570), 40), ((130, 445), 38), ((460, 445), 38)):
            rosette(p, center, size, 8)
        for x, y in ((260, 360), (330, 530)):
            p.polygon([(x, y - 24), (x + 22, y), (x, y + 24), (x - 22, y)], (*p.palette["light"], 180))
    else:
        p.radial_polygon(245, 6, math.pi / 6, (*p.palette["body"], 90), (*CUE_COLORS["stone"], 145), 8)
        for i in range(6):
            angle = i * math.tau / 6
            x, y = 295 + math.cos(angle) * 205, 445 + math.sin(angle) * 245
            p.line([(x, y), (295 + math.cos(angle) * 105, 445 + math.sin(angle) * 125)], (*p.palette["accent"], 210), 13)
            if i % 2 == 0:
                pool(p, (x - 48, y - 27, x + 48, y + 27))
            else:
                rosette(p, (x, y), 38, 6)


def render_flame(p: Painter) -> None:
    form = p.terrain.form
    if form == "ember_rose":
        for i in range(6):
            angle = i * math.tau / 6
            x, y = 295 + math.cos(angle) * 165, 445 + math.sin(angle) * 225
            tip = (295 + math.cos(angle) * 260, 445 + math.sin(angle) * 335)
            points = [(x - 34, y - 20), tip, (x + 34, y + 20), (295 + math.cos(angle) * 95, 445 + math.sin(angle) * 125)]
            p.glow([("polygon", points, 0)], CUE_COLORS["flame"], 24)
            p.polygon(points, (*p.palette["accent"], 210), (*p.palette["light"], 115), 3)
        p.ellipse((245, 390, 345, 500), (7, 7, 8, 230), (*p.palette["accent"], 120), 4)
    elif form == "slag_ring":
        for i in range(8):
            angle = i * math.tau / 8
            x, y = 295 + math.cos(angle) * 210, 445 + math.sin(angle) * 275
            points = [(x - 45, y - 28), (x + 30, y - 42), (x + 52, y + 18), (x - 20, y + 38)]
            p.polygon(points, (*CUE_COLORS["stone"], 180), (*p.palette["accent"], 140), 4)
        p.radial_polygon(185, 6, math.pi / 6, (20, 11, 12, 80), (*p.palette["light"], 135), 6)
    elif form == "twin_crater":
        crater(p, (295, 270), (125, 78), symmetric=True)
        for x in (175, 255, 335):
            crack(p, [(x, 340), (x + 20, 390), (x - 5, 430)], CUE_COLORS["flame"] + (210,), 6, True)
    else:
        crater(p, (175, 290), (90, 55), symmetric=True)
        crater(p, (410, 330), (70, 42), symmetric=True)
        for radius in (270, 220):
            p.radial_polygon(radius, 6, math.pi / 6, (30, 16, 17, 80), (*CUE_COLORS["stone"], 140), 8)
        for i in range(6):
            angle = i * math.tau / 6
            center = (295 + math.cos(angle) * 250, 445 + math.sin(angle) * 310)
            rosette(p, center, 30, 6, CUE_COLORS["grass"])
            p.line([center, (295 + math.cos(angle) * 165, 445 + math.sin(angle) * 205)], (*p.palette["accent"], 150), 5)


def render_water(p: Painter) -> None:
    form = p.terrain.form
    if form == "dark_mirror":
        for rx, ry in ((235, 310), (190, 245), (145, 185)):
            p.ellipse((295 - rx, 445 - ry, 295 + rx, 445 + ry), (8, 24, 35, 85), (*p.palette["accent"], 115), 5)
        for i in range(6):
            angle = i * math.tau / 6
            x, y = 295 + math.cos(angle) * 220, 445 + math.sin(angle) * 290
            p.arc((x - 38, y - 22, x + 38, y + 22), 15, 165, (*p.palette["light"], 120), 3)
        p.ellipse((235, 400, 355, 490), (3, 8, 13, 210), (*p.palette["accent"], 100), 4)
    elif form == "slate_islets":
        p.ellipse((70, 120, 520, 770), (9, 31, 44, 120), (*p.palette["accent"], 100), 5)
        for center, size in (((175, 260), 75), ((415, 630), 75), ((410, 280), 45), ((180, 610), 45), ((115, 445), 38), ((475, 445), 38)):
            x, y = center
            p.polygon([(x - size, y), (x - size * .35, y - size * .45), (x + size * .7, y - size * .2), (x + size, y + size * .25), (x - size * .3, y + size * .4)], (*CUE_COLORS["stone"], 185), (*p.palette["light"], 75), 3)
            p.arc((x - size * 1.15, y - size * .6, x + size * 1.15, y + size * .6), 15, 165, (*p.palette["accent"], 140), 3)
    elif form == "twin_pools":
        pool(p, (120, 205, 470, 365), symmetric=True)
        p.polygon([(120, 390), (245, 365), (345, 390), (470, 365), (410, 440), (180, 440)], (*CUE_COLORS["stone"], 135), symmetric=True)
        p.glow([("line", [(295, 360), (295, 530)], 15)], CUE_COLORS["water"], 25)
        p.line([(295, 350), (295, 540)], (*p.palette["accent"], 150), 7)
    else:
        for radius in (255, 205):
            p.radial_polygon(radius, 6, math.pi / 6, (8, 27, 38, 80), (*CUE_COLORS["stone"], 135), 8)
        loop = [(150, 300), (250, 225), (410, 290), (445, 445), (340, 610), (180, 600), (145, 445), (150, 300)]
        p.glow([("line", loop, 22)], CUE_COLORS["water"], 30)
        p.line(loop, (*p.palette["accent"], 210), 13)
        for x, y in loop[:-1]:
            pool(p, (x - 34, y - 22, x + 34, y + 22))
        for center in ((220, 350), (370, 540)):
            crater(p, center, (42, 25), CUE_COLORS["flame"])


RENDERERS = {
    "neutral": render_neutral,
    "grass": render_grass,
    "flame": render_flame,
    "water": render_water,
}


def generate(terrain: Terrain, output_dir: Path, force: bool) -> str:
    output_path = output_dir / f"{terrain.card_id}.png"
    if output_path.exists() and not force:
        return f"skip  {output_path.name} (already exists)"
    painter = Painter(terrain)
    RENDERERS[terrain.element](painter)
    output_dir.mkdir(parents=True, exist_ok=True)
    temporary_path = output_path.with_suffix(".png.tmp")
    painter.finish().save(temporary_path, format="PNG", optimize=True)
    temporary_path.replace(output_path)
    return f"write {output_path.name}"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--id", dest="card_id", help="Generate only one canonical terrain ID")
    parser.add_argument("--element", choices=sorted(PALETTES), help="Generate one elemental family")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--force", action="store_true", help="Overwrite existing artwork")
    args = parser.parse_args()

    if args.card_id and args.element:
        parser.error("--id and --element cannot be used together")
    selected = [
        terrain
        for terrain in TERRAINS
        if args.card_id in (None, terrain.card_id) and args.element in (None, terrain.element)
    ]
    if not selected:
        parser.error(f"unknown terrain ID: {args.card_id}")
    for terrain in selected:
        print(generate(terrain, args.output_dir, args.force))


if __name__ == "__main__":
    main()
