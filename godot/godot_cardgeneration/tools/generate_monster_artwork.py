#!/usr/bin/env python3
"""Generate deterministic, background-only monster artwork for the default deck.

The recipes intentionally contain no card frame, text, icons, stats, or tier marks.
They render at 590x890 and upscale with Lanczos to the canonical 2360x3560 master.
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
ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "assets" / "artwork" / "monsters"


@dataclass(frozen=True)
class Monster:
    card_id: str
    title: str
    element: str
    form: str
    tier: int


MONSTERS = (
    Monster("monster_neutral_1_a", "Shard Sprout", "neutral", "shard", 1),
    Monster("monster_neutral_1_b", "Bastion Calf", "neutral", "calf", 1),
    Monster("monster_neutral_1_c", "Geode Warden", "neutral", "geode", 1),
    Monster("monster_neutral_1_d", "Monolith Bull", "neutral", "bull", 1),
    Monster("monster_neutral_2_a", "Shieldback", "neutral", "shieldback", 2),
    Monster("monster_neutral_2_b", "Resonance Vault", "neutral", "vault", 2),
    Monster("monster_neutral_2_c", "Fracture Colossus", "neutral", "colossus", 2),
    Monster("monster_neutral_3_a", "Fortress Eye", "neutral", "fortress", 3),
    Monster("monster_grass_1_a", "Seed Gleam", "grass", "seed", 1),
    Monster("monster_grass_1_b", "Root Claw", "grass", "rootclaw", 1),
    Monster("monster_grass_1_c", "Vine Turner", "grass", "vine", 1),
    Monster("monster_grass_1_d", "Bark Horn", "grass", "barkhorn", 1),
    Monster("monster_grass_2_a", "Mycelium Crown", "grass", "mycelium", 2),
    Monster("monster_grass_2_b", "Twining Trunk", "grass", "twiner", 2),
    Monster("monster_grass_2_c", "Grove Bearer", "grass", "grove", 2),
    Monster("monster_grass_3_a", "Worldroot Warden", "grass", "worldroot", 3),
    Monster("monster_flame_1_a", "Sparkling", "flame", "spark", 1),
    Monster("monster_flame_1_b", "Slag Jumper", "flame", "jumper", 1),
    Monster("monster_flame_1_c", "Wick Phantom", "flame", "wick", 1),
    Monster("monster_flame_1_d", "Ember Ram", "flame", "ram", 1),
    Monster("monster_flame_2_a", "Multistrike", "flame", "multistrike", 2),
    Monster("monster_flame_2_b", "Melt Jaw", "flame", "jaw", 2),
    Monster("monster_flame_2_c", "Crater Charger", "flame", "charger", 2),
    Monster("monster_flame_3_a", "Rebound Prince", "flame", "prince", 3),
    Monster("monster_water_1_a", "Droplet Seed", "water", "droplet", 1),
    Monster("monster_water_1_b", "Pool Crab", "water", "crab", 1),
    Monster("monster_water_1_c", "Pressure Eel", "water", "eel", 1),
    Monster("monster_water_1_d", "Well Shield", "water", "shell", 1),
    Monster("monster_water_2_a", "Confluence", "water", "confluence", 2),
    Monster("monster_water_2_b", "Current Bender", "water", "bender", 2),
    Monster("monster_water_2_c", "Deep Chamber", "water", "chamber", 2),
    Monster("monster_water_3_a", "Abyssal Vault", "water", "abyss", 3),
)


PALETTES = {
    "neutral": {"bg": (8, 10, 13), "field": (30, 33, 38), "body": (52, 57, 64), "body2": (86, 92, 100), "accent": (137, 146, 156), "light": (211, 217, 222)},
    "grass": {"bg": (5, 11, 9), "field": (17, 37, 27), "body": (33, 51, 36), "body2": (65, 72, 43), "accent": (105, 151, 64), "light": (201, 225, 83)},
    "flame": {"bg": (12, 7, 9), "field": (48, 21, 25), "body": (45, 39, 42), "body2": (91, 44, 34), "accent": (207, 81, 35), "light": (255, 193, 67)},
    "water": {"bg": (4, 9, 15), "field": (13, 31, 48), "body": (25, 48, 61), "body2": (31, 75, 89), "accent": (54, 154, 177), "light": (169, 231, 235)},
}


class Painter:
    def __init__(self, monster: Monster):
        self.monster = monster
        self.palette = PALETTES[monster.element]
        self.random = random.Random(monster.card_id)
        self.image = Image.new("RGB", WORK_SIZE, self.palette["bg"])
        self.draw = ImageDraw.Draw(self.image, "RGBA")
        self._paint_background()

    def _paint_background(self) -> None:
        p = self.palette
        # Layered oval values keep the outer edge dark beneath renderer overlays.
        for radius in range(390, 40, -12):
            t = 1 - radius / 390
            alpha = int(7 + 18 * math.sin(t * math.pi))
            self.draw.ellipse((295 - radius * .67, 445 - radius, 295 + radius * .67, 445 + radius), fill=(*p["field"], alpha))
        for _ in range(700):
            x = self.random.randrange(18, 572)
            y = self.random.randrange(35, 855)
            value = self.random.randrange(8, 26)
            self.draw.point((x, y), fill=(value, value, value, self.random.randrange(8, 25)))
        self.draw.ellipse((70, 500, 520, 755), fill=(0, 0, 0, 35))
        for i in range(3):
            inset = i * 42
            self.draw.ellipse((80 + inset, 525 + inset // 2, 510 - inset, 730 - inset // 2), outline=(*p["body2"], 75), width=2)

    def glow(self, shapes, color=None, blur=26) -> None:
        layer = Image.new("RGBA", WORK_SIZE)
        d = ImageDraw.Draw(layer, "RGBA")
        rgba = (*(color or self.palette["accent"]), 145)
        for kind, points, width in shapes:
            if kind == "line":
                d.line(points, fill=rgba, width=width, joint="curve")
            elif kind == "ellipse":
                d.ellipse(points, fill=rgba)
            elif kind == "polygon":
                d.polygon(points, fill=rgba)
        self.image = Image.alpha_composite(self.image.convert("RGBA"), layer.filter(ImageFilter.GaussianBlur(blur))).convert("RGB")
        self.draw = ImageDraw.Draw(self.image, "RGBA")

    def line(self, points, fill=None, width=10) -> None:
        self.draw.line(points, fill=fill or (*self.palette["body2"], 255), width=width, joint="curve")

    def polygon(self, points, fill=None, outline=None, width=3) -> None:
        self.draw.polygon(points, fill=fill or (*self.palette["body"], 255))
        if outline:
            self.draw.line([*points, points[0]], fill=outline, width=width, joint="curve")

    def ellipse(self, box, fill=None, outline=None, width=3) -> None:
        self.draw.ellipse(box, fill=fill or (*self.palette["body"], 255), outline=outline, width=width)

    def eye(self, center, size=18, vertical=False) -> None:
        x, y = center
        p = self.palette
        if vertical:
            pts = [(x, y - size), (x + size * .65, y), (x, y + size), (x - size * .65, y)]
        else:
            pts = [(x - size, y), (x, y - size * .65), (x + size, y), (x, y + size * .65)]
        self.glow([("polygon", pts, 0)], p["accent"], 13)
        self.polygon(pts, (*p["light"], 255))
        self.ellipse((x - 3, y - 6, x + 3, y + 6), (18, 14, 16, 255))

    def finish(self) -> Image.Image:
        # A subtle vignette preserves contrast where the game paints its panels.
        mask = Image.new("L", WORK_SIZE, 0)
        md = ImageDraw.Draw(mask)
        for i in range(100):
            md.ellipse((i, i * 1.25, 590 - i, 890 - i * 1.25), outline=max(0, 145 - i), width=2)
        shade = Image.new("RGBA", WORK_SIZE, (0, 0, 0, 0))
        shade.putalpha(mask)
        result = Image.alpha_composite(self.image.convert("RGBA"), shade)
        return result.resize(MASTER_SIZE, Image.Resampling.LANCZOS)


def legs(p: Painter, anchors, width=19, length=105) -> None:
    color = (*p.palette["body"], 255)
    for x, y, lean in anchors:
        p.line([(x, y), (x + lean, y + length)], color, width)
        p.ellipse((x + lean - width // 2, y + length - 7, x + lean + width, y + length + 8), color)


def stone_cracks(p: Painter, count=7) -> None:
    accent = (*p.palette["accent"], 135)
    for i in range(count):
        x = 115 + i * 55 + p.random.randrange(-12, 13)
        y = 680 + p.random.randrange(-20, 22)
        p.line([(x, y), (x + p.random.randrange(-26, 27), y + 24), (x + p.random.randrange(-35, 36), y + 43)], accent, 3)


def render_neutral(p: Painter) -> None:
    f = p.monster.form
    stone_cracks(p, 8)
    if f in {"shard", "calf", "bull", "shieldback"}:
        body = (175, 430, 425, 650) if f != "calf" else (190, 470, 405, 625)
        p.glow([("ellipse", body, 0)])
        p.ellipse(body)
        legs(p, [(215, 595, -25), (270, 615, -8), (335, 612, 10), (385, 588, 24)], 22, 105 if f != "calf" else 75)
        if f == "shard":
            for x, h in ((215, 105), (285, 145), (355, 115)):
                p.polygon([(x - 34, 475), (x, 475 - h), (x + 38, 490)], (*p.palette["body2"], 255), (*p.palette["accent"], 180))
        elif f == "calf":
            p.polygon([(205, 465), (285, 390), (375, 470), (345, 545), (235, 540)], (*p.palette["body2"], 255), (*p.palette["accent"], 190), 5)
            p.polygon([(240, 450), (295, 410), (350, 452)], (*p.palette["body"], 255))
        elif f == "bull":
            p.polygon([(190, 505), (105, 420), (210, 460)], (*p.palette["body2"], 255))
            p.polygon([(400, 505), (485, 420), (380, 460)], (*p.palette["body2"], 255))
            p.line([(115, 420), (165, 375), (220, 450)], (*p.palette["accent"], 220), 8)
            p.line([(475, 420), (425, 375), (370, 450)], (*p.palette["accent"], 220), 8)
        else:
            for i in range(3):
                p.polygon([(145 + i * 25, 510 - i * 28), (292, 350 - i * 16), (445 - i * 25, 510 - i * 28), (395, 570), (195, 570)], (*p.palette["body2"], 255), (*p.palette["accent"], 130), 4)
        p.eye((295, 515 if f != "calf" else 500), 15, True)
    elif f in {"geode", "vault"}:
        p.glow([("ellipse", (190, 400, 400, 660), 0)], blur=34)
        p.ellipse((160, 405, 430, 650), (*p.palette["body"], 255), (*p.palette["body2"], 255), 8)
        for i in range(4 if f == "vault" else 2):
            p.draw.arc((180 + i * 18, 425 + i * 16, 410 - i * 18, 635 - i * 12), 190, 350, fill=(*p.palette["accent"], 210), width=9)
        p.polygon([(295, 455), (350, 530), (295, 610), (240, 530)], (29, 25, 30, 255), (*p.palette["light"], 220), 7)
        p.eye((295, 530), 18, True)
        leg_count = 8 if f == "vault" else 6
        legs(p, [(185 + i * 220 / (leg_count - 1), 600, (i - leg_count / 2) * 8) for i in range(leg_count)], 15, 95)
    else:
        # Colossus and apex share massive geometry, with deeper armor on the apex.
        p.glow([("line", [(295, 370), (295, 650)], 25)], blur=35)
        legs(p, [(235, 620, -22), (350, 620, 20)], 45, 125)
        p.polygon([(220, 330), (370, 330), (425, 625), (330, 665), (295, 615), (255, 670), (165, 625)], (*p.palette["body"], 255), (*p.palette["body2"], 255), 8)
        p.polygon([(295, 390), (340, 500), (295, 625), (250, 500)], (40, 30, 32, 255), (*p.palette["accent"], 220), 7)
        p.line([(165, 410), (100, 585), (205, 565)], (*p.palette["body2"], 255), 45)
        p.line([(425, 410), (490, 585), (385, 565)], (*p.palette["body2"], 255), 45)
        if f == "fortress":
            for i in range(3):
                p.polygon([(125 + i * 25, 350 + i * 28), (295, 275 + i * 22), (465 - i * 25, 350 + i * 28), (410, 445), (180, 445)], (*p.palette["body2"], 245), (*p.palette["accent"], 130), 5)
            p.eye((295, 450), 24, True)
        else:
            p.eye((295, 505), 19, True)


def roots(p: Painter, origin=(295, 590), branches=8, width=13) -> None:
    ox, oy = origin
    for i in range(branches):
        angle = math.pi * (.08 + .84 * i / max(1, branches - 1))
        end = (ox + math.cos(angle) * 235, oy + abs(math.sin(angle)) * 145)
        mid = ((ox + end[0]) / 2 + p.random.randrange(-30, 31), oy + 50)
        p.line([(ox, oy), mid, end], (*p.palette["body2"], 240), max(4, width - i % 3 * 2))
        p.line([end, (end[0] + p.random.randrange(-28, 29), end[1] + 28)], (*p.palette["accent"], 130), 3)


def leaves(p: Painter, centers, size=23) -> None:
    for x, y, angle in centers:
        dx, dy = math.cos(angle) * size, math.sin(angle) * size
        nx, ny = -math.sin(angle) * size * .45, math.cos(angle) * size * .45
        p.polygon([(x - dx, y - dy), (x + nx, y + ny), (x + dx, y + dy), (x - nx, y - ny)], (*p.palette["accent"], 230))


def render_grass(p: Painter) -> None:
    f = p.monster.form
    roots(p, branches=10 if p.monster.tier > 1 else 6)
    if f == "seed":
        p.glow([("ellipse", (225, 390, 365, 620), 0)])
        p.ellipse((225, 390, 365, 620), (*p.palette["body2"], 255), (*p.palette["accent"], 180), 5)
        p.line([(295, 420), (280, 340), (250, 305)], (*p.palette["accent"], 255), 12)
        p.line([(295, 420), (320, 345), (350, 315)], (*p.palette["accent"], 255), 12)
        leaves(p, [(245, 300, -.6), (354, 310, .6)], 28)
        p.eye((295, 475), 17)
    elif f in {"rootclaw", "barkhorn"}:
        p.polygon([(225, 420), (350, 400), (405, 575), (330, 640), (205, 600), (175, 500)], (*p.palette["body"], 255), (*p.palette["accent"], 150), 5)
        for x in (230, 275, 320, 365):
            p.line([(x, 585), (x + (x - 295) * .6, 700)], (*p.palette["body2"], 255), 18)
        if f == "barkhorn":
            p.line([(245, 470), (155, 365), (125, 430)], (*p.palette["accent"], 255), 22)
            p.line([(350, 455), (410, 380)], (*p.palette["body2"], 230), 18)
            leaves(p, [(410, 370, -.4), (445, 395, .4), (385, 345, -1)], 22)
        else:
            p.polygon([(200, 665), (295, 620), (390, 665), (295, 705)], (*p.palette["body2"], 255))
        p.eye((295, 495), 16, True)
    elif f == "vine":
        path = [(270, 680), (205, 610), (245, 525), (355, 465), (330, 370), (255, 330)]
        p.glow([("line", path, 28)])
        p.line(path, (*p.palette["body2"], 255), 42)
        for x, y in path[1:-1]:
            p.draw.arc((x - 65, y - 45, x + 65, y + 80), 10, 260, fill=(*p.palette["accent"], 230), width=10)
        p.ellipse((225, 290, 295, 370), (*p.palette["accent"], 255))
        p.eye((260, 335), 13)
    elif f == "mycelium":
        p.glow([("line", [(295, 660), (295, 390)], 20), ("ellipse", (205, 350, 385, 470), 0)])
        p.line([(295, 660), (295, 390)], (*p.palette["body2"], 255), 70)
        for x in (220, 260, 300, 340, 380):
            p.ellipse((x - 45, 330 + abs(x - 300) // 3, x + 45, 420 + abs(x - 300) // 4), (*p.palette["accent"], 230), (*p.palette["light"], 150), 3)
        for x, y in ((250, 510), (340, 545), (275, 610)):
            p.ellipse((x - 10, y - 10, x + 10, y + 10), (*p.palette["light"], 255))
            p.line([(295, 560), (x, y)], (*p.palette["accent"], 170), 4)
        p.eye((295, 465), 15, True)
    elif f == "twiner":
        p.glow([("line", [(250, 670), (350, 350)], 35)])
        p.line([(260, 670), (225, 560), (355, 480), (320, 350)], (*p.palette["body2"], 255), 65)
        p.line([(330, 670), (370, 570), (235, 480), (275, 355)], (*p.palette["body"], 255), 55)
        for y in range(420, 640, 45):
            p.draw.arc((205, y - 35, 385, y + 35), 10, 180, fill=(*p.palette["accent"], 210), width=8)
        p.line([(250, 500), (130, 570), (75, 520)], (*p.palette["body2"], 255), 28)
        p.eye((295, 430), 16)
    else:
        # Grove bearer and apex become increasingly tree-like and symbiotic.
        trunk_width = 120 if f == "grove" else 175
        p.glow([("line", [(295, 680), (295, 310)], trunk_width // 3)], blur=42)
        p.line([(295, 680), (295, 370)], (*p.palette["body"], 255), trunk_width)
        branch_count = 6 if f == "grove" else 10
        branch_tips = []
        for i in range(branch_count):
            side = -1 if i % 2 == 0 else 1
            y = 560 - (i // 2) * 58
            end = (295 + side * (110 + (i % 3) * 28), y - 75)
            p.line([(295, y + 30), end], (*p.palette["body2"], 255), max(10, 30 - i))
            branch_tips.append((end[0], end[1], side * .55))
        leaves(p, branch_tips, 31)
        for x, y, _ in branch_tips[::2]:
            p.eye((x, y), 7)
        p.eye((295, 470), 24 if f == "worldroot" else 18, True)


def flame_shape(p: Painter, center, scale=1.0, inner=True) -> None:
    x, y = center
    pts = [(x, y - 105 * scale), (x + 30 * scale, y - 45 * scale), (x + 65 * scale, y - 75 * scale), (x + 55 * scale, y + 45 * scale), (x, y + 85 * scale), (x - 58 * scale, y + 45 * scale), (x - 42 * scale, y - 30 * scale)]
    p.glow([("polygon", pts, 0)], blur=int(28 * scale))
    p.polygon(pts, (*p.palette["accent"], 245))
    if inner:
        p.polygon([(x, y - 35 * scale), (x + 25 * scale, y + 38 * scale), (x, y + 62 * scale), (x - 25 * scale, y + 38 * scale)], (*p.palette["light"], 255))


def render_flame(p: Painter) -> None:
    f = p.monster.form
    stone_cracks(p, 7)
    if f == "spark":
        flame_shape(p, (295, 500), .8)
        p.ellipse((260, 475, 330, 545), (30, 27, 29, 255))
        p.eye((280, 500), 9)
        p.eye((312, 500), 9)
        for angle in range(0, 360, 60):
            a = math.radians(angle)
            p.line([(295 + math.cos(a) * 70, 500 + math.sin(a) * 90), (295 + math.cos(a) * 115, 500 + math.sin(a) * 130)], (*p.palette["light"], 210), 5)
    elif f in {"jumper", "multistrike"}:
        p.ellipse((190, 420, 390, 600), (*p.palette["body"], 255), (*p.palette["accent"], 180), 6)
        legs(p, [(220, 560, -90), (270, 580, -45), (335, 580, 55), (375, 550, 100)], 22, 100)
        chamber_count = 3 if f == "multistrike" else 1
        for i in range(chamber_count):
            x = 250 + i * 45
            flame_shape(p, (x, 490 + abs(i - 1) * 18), .25)
        if f == "multistrike":
            for shift, alpha in ((-90, 65), (90, 65)):
                p.draw.ellipse((190 + shift, 420, 390 + shift, 600), outline=(*p.palette["accent"], alpha), width=14)
        p.eye((295, 455), 15)
    elif f == "wick":
        p.glow([("ellipse", (230, 350, 360, 650), 0)])
        p.ellipse((235, 390, 355, 650), (*p.palette["body"], 255))
        legs(p, [(260, 610, -40), (330, 610, 40)], 13, 100)
        p.line([(250, 440), (180, 330), (220, 250)], (*p.palette["body2"], 255), 11)
        p.line([(295, 405), (295, 250), (280, 205)], (*p.palette["body2"], 255), 11)
        p.line([(340, 440), (410, 330), (370, 250)], (*p.palette["body2"], 255), 11)
        for pos in ((220, 240), (280, 195), (370, 240)):
            flame_shape(p, pos, .22)
        p.eye((295, 470), 15, True)
    elif f in {"ram", "charger"}:
        p.ellipse((165, 425, 425, 640), (*p.palette["body"], 255), (*p.palette["body2"], 255), 7)
        legs(p, [(210, 600, -30), (270, 615, -10), (335, 610, 15), (390, 590, 38)], 25, 100)
        p.draw.arc((115, 300, 300, 520), 105, 300, fill=(*p.palette["accent"], 255), width=24)
        p.draw.arc((290, 300, 475, 520), 240, 75, fill=(*p.palette["accent"], 255), width=24)
        if f == "charger":
            for r in (0, 18, 36):
                p.draw.arc((120 + r, 305 + r, 470 - r, 555 - r), 195, 345, fill=(*p.palette["light"], 150), width=5)
            p.polygon([(295, 320), (320, 365), (295, 405), (270, 365)], (*p.palette["light"], 255))
        p.eye((295, 475), 17)
    elif f == "jaw":
        p.glow([("ellipse", (150, 350, 440, 650), 0)], blur=38)
        p.polygon([(150, 420), (230, 320), (390, 350), (445, 470), (390, 650), (205, 630), (145, 520)], (*p.palette["body"], 255), (*p.palette["accent"], 180), 7)
        p.polygon([(205, 465), (385, 465), (350, 600), (235, 600)], (27, 15, 16, 255))
        for i in range(5):
            x = 235 + i * 30
            p.polygon([(x, 468), (x + 13, 515), (x + 27, 468)], (*p.palette["light"], 255))
            p.polygon([(x, 598), (x + 13, 550), (x + 27, 598)], (*p.palette["accent"], 255))
        p.eye((225, 425), 13)
        p.eye((360, 425), 13)
    else:
        p.glow([("ellipse", (100, 280, 490, 700), 0)], blur=48)
        p.line([(180, 650), (250, 530), (205, 390), (295, 300), (385, 390), (340, 530), (410, 650)], (*p.palette["body"], 255), 75)
        for center, scale in (((295, 350), .7), ((190, 500), .5), ((400, 500), .5)):
            flame_shape(p, center, scale)
        p.draw.arc((115, 270, 475, 690), 30, 330, fill=(*p.palette["accent"], 180), width=12)
        p.eye((270, 410), 19)
        p.eye((330, 410), 19)


def water_rings(p: Painter, center=(295, 650), count=4) -> None:
    x, y = center
    for i in range(count):
        rx = 90 + i * 60
        ry = 25 + i * 18
        p.draw.ellipse((x - rx, y - ry, x + rx, y + ry), outline=(*p.palette["accent"], 120 - i * 16), width=4)


def droplet(p: Painter, center, size=80, fill=None) -> None:
    x, y = center
    pts = [(x, y - size), (x + size * .72, y + size * .12), (x + size * .48, y + size * .72), (x, y + size), (x - size * .48, y + size * .72), (x - size * .72, y + size * .12)]
    p.glow([("polygon", pts, 0)], blur=max(12, size // 3))
    p.polygon(pts, fill or (*p.palette["body2"], 225), (*p.palette["accent"], 210), 5)


def render_water(p: Painter) -> None:
    f = p.monster.form
    water_rings(p)
    if f == "droplet":
        droplet(p, (295, 500), 105)
        p.ellipse((276, 480, 314, 530), (134, 86, 52, 255))
        p.eye((295, 505), 12, True)
    elif f == "crab":
        p.draw.arc((145, 410, 445, 650), 180, 360, fill=(*p.palette["body2"], 255), width=38)
        p.ellipse((190, 440, 400, 615), (*p.palette["accent"], 145), (*p.palette["body2"], 255), 8)
        for side in (-1, 1):
            for i in range(2):
                x = 220 if side < 0 else 370
                p.line([(x, 540 + i * 30), (x + side * 100, 570 + i * 48)], (*p.palette["accent"], 210), 16)
        p.eye((260, 500), 11)
        p.eye((330, 500), 11)
    elif f == "eel":
        path = [(235, 680), (185, 600), (245, 515), (365, 460), (350, 350), (270, 300)]
        p.glow([("line", path, 45)], blur=34)
        p.line(path, (*p.palette["body2"], 230), 55)
        for i, (x, y) in enumerate(path[1:4]):
            p.polygon([(x, y - 25), (x + 25, y), (x, y + 25), (x - 25, y)], (*p.palette["light"], 190))
        p.eye((275, 320), 13)
    elif f == "shell":
        p.ellipse((145, 380, 445, 650), (*p.palette["body"], 255), (*p.palette["body2"], 255), 13)
        p.draw.arc((175, 410, 415, 625), 195, 345, fill=(*p.palette["accent"], 230), width=25)
        p.line([(250, 560), (145, 660), (100, 625)], (*p.palette["body2"], 225), 45)
        p.eye((295, 490), 18)
    elif f == "confluence":
        droplet(p, (295, 500), 90)
        for x in (155, 435):
            droplet(p, (x, 540), 48)
            p.line([(x, 540), (245 if x < 295 else 345, 525)], (*p.palette["accent"], 220), 16)
        for x, y in ((270, 480), (320, 500), (295, 550)):
            p.ellipse((x - 10, y - 10, x + 10, y + 10), (153, 100, 61, 255))
        p.eye((295, 515), 14, True)
    elif f == "bender":
        p.glow([("ellipse", (125, 330, 465, 680), 0)], blur=42)
        p.line([(250, 680), (205, 570), (350, 500), (330, 370)], (*p.palette["body2"], 240), 55)
        p.line([(340, 680), (390, 560), (235, 485), (265, 365)], (*p.palette["body"], 255), 45)
        p.draw.arc((135, 340, 455, 665), 25, 335, fill=(*p.palette["accent"], 230), width=18)
        p.polygon([(190, 490), (215, 515), (190, 540), (170, 515)], (*p.palette["light"], 210))
        p.polygon([(400, 490), (420, 515), (400, 540), (375, 515)], (*p.palette["light"], 210))
        p.eye((295, 430), 16)
    elif f == "chamber":
        p.ellipse((135, 350, 455, 665), (*p.palette["body"], 255), (*p.palette["body2"], 255), 12)
        for i in range(3):
            y = 430 + i * 70
            p.ellipse((235, y - 42, 355, y + 42), (*p.palette["accent"], 140), (*p.palette["light"], 180), 5)
        p.line([(295, 390), (295, 630)], (*p.palette["light"], 100), 6)
        legs(p, [(190, 600, -30), (250, 625, -10), (340, 625, 12), (400, 600, 35)], 25, 90)
        p.eye((295, 515), 16, True)
    else:
        p.glow([("ellipse", (85, 250, 505, 720), 0)], blur=55)
        for i in range(4):
            p.draw.arc((85 + i * 35, 275 + i * 30, 505 - i * 35, 715 - i * 30), 185, 355, fill=(*p.palette["accent"], 180 + i * 15), width=24 - i * 3)
        p.ellipse((175, 355, 415, 665), (*p.palette["body"], 235), (*p.palette["body2"], 255), 10)
        p.ellipse((220, 405, 370, 620), (*p.palette["accent"], 120), (*p.palette["light"], 170), 7)
        p.eye((295, 510), 25, True)


RENDERERS = {"neutral": render_neutral, "grass": render_grass, "flame": render_flame, "water": render_water}


def generate(monster: Monster, output_dir: Path, force: bool) -> str:
    output_path = output_dir / f"{monster.card_id}.png"
    if output_path.exists() and not force:
        return f"skip  {output_path.name} (already exists)"
    painter = Painter(monster)
    RENDERERS[monster.element](painter)
    output_dir.mkdir(parents=True, exist_ok=True)
    painter.finish().save(output_path, format="PNG", optimize=True)
    return f"write {output_path.name}"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--id", dest="card_id", help="Generate only one canonical monster ID")
    parser.add_argument("--element", choices=sorted(PALETTES), help="Generate one elemental family")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--force", action="store_true", help="Overwrite existing artwork")
    args = parser.parse_args()

    if args.card_id and args.element:
        parser.error("--id and --element cannot be used together")
    selected = [
        monster
        for monster in MONSTERS
        if args.card_id in (None, monster.card_id) and args.element in (None, monster.element)
    ]
    if not selected:
        parser.error(f"unknown monster ID: {args.card_id}")
    for monster in selected:
        print(generate(monster, args.output_dir, args.force))


if __name__ == "__main__":
    main()
