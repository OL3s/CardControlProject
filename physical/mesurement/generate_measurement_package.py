#!/usr/bin/env python3
"""
Generate the edited Element War production-diagram package.

Output structure relative to this script:
    png/*.png
    svg/*.svg
    data/measurement.txt

Dependency:
    matplotlib

Run:
    ../../../.venv/bin/python generate_measurement_package.py

This edited copy keeps the original box/card/lid geometry, but updates the
player tray to four closed pockets with a fixed front name field.
"""

from __future__ import annotations

from pathlib import Path
import math
import textwrap

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.lines import Line2D
from matplotlib.patches import Arc, Circle, FancyBboxPatch, Polygon, Rectangle


ROOT = Path(__file__).resolve().parent
PNG_DIR = ROOT / "png"
SVG_DIR = ROOT / "svg"
DATA_DIR = ROOT / "data"

for directory in (PNG_DIR, SVG_DIR, DATA_DIR):
    directory.mkdir(parents=True, exist_ok=True)


# ============================================================
# Finished manufacturing targets, in millimetres
# ============================================================
SPEC = {
    "cards": (88.0, 63.0, 17.0),
    "rules": (89.0, 64.0, 1.0),

    "tray_outer": (92.0, 67.0, 21.5),
    "tray_floor": 1.5,
    "tray_wall": 20.0,
    "tray_side_wall": 1.5,
    "tray_divider": 1.0,
    "tray_front_wall": 1.5,
    "tray_back_wall": 1.5,
    "pocket_front_wall": 1.5,
    "inner_w": 89.0,
    "inner_d": 64.0,
    "player_zone_d": 51.0,
    "front_name_zone_d": 11.5,
    "player_column_w": 21.5,

    "front_name_field": (89.0, 11.5),
    "throne": (19.6, 19.6, 19.6),
    "health_die": (16.0, 16.0, 16.0),
    "farmer_die": (10.0, 10.0, 10.0),
    "farmer_stack": (20.0, 30.0, 20.0),

    "base_outer": (97.0, 72.0, 43.0),
    "base_inner": (93.0, 68.0, 41.0),
    "board": 2.0,

    "lid_panel": (97.0, 72.0, 2.0),
    "rear_spine": (97.0, 43.0, 2.0),
    "front_flap": (97.0, 20.0, 2.0),
    "hinge_gap": 3.0,
    "closed_total": (97.0, 72.0, 45.0),
    "opening_angle": 110.0,

    "magnet_diameter": 8.0,
    "magnet_thickness": 1.0,
    "magnet_edge_offset": 18.0,
    "magnet_vertical_center": 10.0,

    "compression_liner": 1.0,
}

PLAYER_COLORS = ["#bcbcbc", "#8fb3db", "#dc8b7d", "#a8b782"]
PLAYER_NAMES = ["Gray", "Blue", "Red", "Green"]

STACK_H = SPEC["cards"][2] + SPEC["rules"][2] + SPEC["tray_outer"][2]
FREE_H = SPEC["base_inner"][2] - STACK_H
FREE_H_AFTER_LINER = FREE_H - SPEC["compression_liner"]
SIDE_CLEAR_X = (SPEC["base_inner"][0] - SPEC["tray_outer"][0]) / 2
SIDE_CLEAR_Y = (SPEC["base_inner"][1] - SPEC["tray_outer"][1]) / 2

PLAYER_CLEAR_W = (
    SPEC["inner_w"] - 3 * SPEC["tray_divider"]
) / 4
POCKET_Y = (
    SPEC["tray_front_wall"]
    + SPEC["front_name_zone_d"]
    + SPEC["pocket_front_wall"]
)
POCKET_TOP = POCKET_Y + SPEC["player_zone_d"]
BACK_CLEAR = (
    SPEC["player_zone_d"]
    - 0.6
    - SPEC["throne"][1]
    - 0.4
    - SPEC["farmer_stack"][1]
)
LATERAL_CLEAR_TOTAL = PLAYER_CLEAR_W - SPEC["farmer_stack"][0]
LATERAL_CLEAR_SIDE = LATERAL_CLEAR_TOTAL / 2

TARGET_CADDY_CLEAR_W = 20.8
FOUR_CADDIES_08 = 4 * (TARGET_CADDY_CLEAR_W + 2 * 0.8)
FOUR_CADDIES_10 = 4 * (TARGET_CADDY_CLEAR_W + 2 * 1.0)


# ============================================================
# General helpers
# ============================================================
def setup(ax, xlim, ylim, title):
    ax.set_aspect("equal")
    ax.set_xlim(*xlim)
    ax.set_ylim(*ylim)
    ax.axis("off")
    ax.set_title(title, fontsize=15, pad=12)


def dim_h(ax, x1, x2, y, label, fs=8):
    ax.annotate(
        "",
        xy=(x1, y),
        xytext=(x2, y),
        arrowprops=dict(arrowstyle="<->", lw=0.9),
    )
    ax.text(
        (x1 + x2) / 2,
        y + 0.8,
        label,
        ha="center",
        va="bottom",
        fontsize=fs,
    )


def dim_v(ax, y1, y2, x, label, fs=8):
    ax.annotate(
        "",
        xy=(x, y1),
        xytext=(x, y2),
        arrowprops=dict(arrowstyle="<->", lw=0.9),
    )
    ax.text(
        x + 0.8,
        (y1 + y2) / 2,
        label,
        ha="left",
        va="center",
        rotation=90,
        fontsize=fs,
    )


def save_dual(fig, stem):
    fig.savefig(PNG_DIR / f"{stem}.png", dpi=230, bbox_inches="tight")
    fig.savefig(SVG_DIR / f"{stem}.svg", bbox_inches="tight")
    plt.close(fig)


def add_top_player_layout(ax, origin=(0.0, 0.0), scale=1.0, labels=True):
    ox, oy = origin

    tray_w, tray_d, _ = SPEC["tray_outer"]
    wall = SPEC["tray_side_wall"]
    divider = SPEC["tray_divider"]
    inner_w = SPEC["inner_w"]
    player_zone_d = SPEC["player_zone_d"]
    player_col_w = PLAYER_CLEAR_W
    name_y = SPEC["tray_front_wall"]
    name_zone_d = SPEC["front_name_zone_d"]
    pocket_front_wall = SPEC["pocket_front_wall"]
    player_y = POCKET_Y

    ax.add_patch(
        FancyBboxPatch(
            (ox, oy),
            tray_w * scale,
            tray_d * scale,
            boxstyle="round,pad=0,rounding_size=1.2",
            fill=False,
            lw=1.5,
        )
    )

    # Fixed walls and name field make the tray safe to lift out as one unit.
    for x, y, w, h in (
        (0, 0, tray_w, SPEC["tray_front_wall"]),
        (0, tray_d - SPEC["tray_back_wall"], tray_w, SPEC["tray_back_wall"]),
        (0, 0, wall, tray_d),
        (tray_w - wall, 0, wall, tray_d),
        (wall, name_y + name_zone_d, inner_w, pocket_front_wall),
    ):
        ax.add_patch(
            Rectangle(
                (ox + x * scale, oy + y * scale),
                w * scale,
                h * scale,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.7,
            )
        )

    ax.add_patch(
        Rectangle(
            (ox + wall * scale, oy + name_y * scale),
            inner_w * scale,
            name_zone_d * scale,
            facecolor="#ead8b5",
            edgecolor="black",
            lw=0.9,
        )
    )

    for index, (name, color) in enumerate(
        zip(PLAYER_NAMES, PLAYER_COLORS)
    ):
        px = ox + (
            wall + index * (player_col_w + divider)
        ) * scale
        py = oy + player_y * scale

        if index < 3:
            divider_x = px + player_col_w * scale
            ax.add_patch(
                Rectangle(
                    (divider_x, oy + name_y * scale),
                    divider * scale,
                    name_zone_d * scale,
                    facecolor="#c6ad83",
                    edgecolor="black",
                    lw=0.4,
                    alpha=0.35,
                )
            )
            ax.add_patch(
                Rectangle(
                    (divider_x, py),
                    divider * scale,
                    player_zone_d * scale,
                    facecolor="#c6ad83",
                    edgecolor="black",
                    lw=0.5,
                )
            )

        ax.add_patch(
            Rectangle(
                (px, py),
                player_col_w * scale,
                player_zone_d * scale,
                fill=False,
                lw=0.8,
            )
        )

        throne_w = SPEC["throne"][0] * scale
        throne_d = SPEC["throne"][1] * scale
        throne_x = px + (
            player_col_w - SPEC["throne"][0]
        ) * scale / 2
        throne_y = py + 0.6 * scale

        ax.add_patch(
            FancyBboxPatch(
                (throne_x, throne_y),
                throne_w,
                throne_d,
                boxstyle="round,pad=0,rounding_size=0.5",
                facecolor=color,
                alpha=0.70,
                edgecolor="black",
                lw=0.9,
            )
        )

        die_draw = 12.0 * scale
        ax.add_patch(
            Rectangle(
                (
                    throne_x + (throne_w - die_draw) / 2,
                    throne_y + (throne_d - die_draw) / 2,
                ),
                die_draw,
                die_draw,
                facecolor="#f2efe7",
                edgecolor="black",
                lw=0.5,
            )
        )

        stack_x = px + (
            player_col_w - SPEC["farmer_stack"][0]
        ) * scale / 2
        stack_y = throne_y + (SPEC["throne"][1] + 0.4) * scale
        die = SPEC["farmer_die"][0] * scale

        for column in range(2):
            for row in range(3):
                ax.add_patch(
                    Rectangle(
                        (
                            stack_x + column * die,
                            stack_y + row * die,
                        ),
                        die,
                        die,
                        facecolor=color,
                        alpha=0.45,
                        edgecolor="black",
                        lw=0.4,
                    )
                )

        if labels:
            ax.text(
                px + player_col_w * scale / 2,
                oy + (name_y + name_zone_d / 2) * scale,
                name,
                ha="center",
                va="center",
                fontsize=6,
                weight="bold",
            )


# ============================================================
# data/measurement.txt
# ============================================================
def write_measurements():
    measurement_text = f"""ELEMENT WAR — PRODUKSJONSMÅL
Alle mål er ferdige nominelle mål i millimeter.

BOKSTYPE
Rigid bokboks med hengslet toppanel og magnetisk frontflapp.

BOKS
Base utvendig: {SPEC['base_outer'][0]:.1f} × {SPEC['base_outer'][1]:.1f} × {SPEC['base_outer'][2]:.1f}
Base innvendig: {SPEC['base_inner'][0]:.1f} × {SPEC['base_inner'][1]:.1f} × {SPEC['base_inner'][2]:.1f}
Lukket totalmål: {SPEC['closed_total'][0]:.1f} × {SPEC['closed_total'][1]:.1f} × {SPEC['closed_total'][2]:.1f}
Rigid board: {SPEC['board']:.1f}

FOLDBART MAGNETLOKK
Toppanel: {SPEC['lid_panel'][0]:.1f} × {SPEC['lid_panel'][1]:.1f} × {SPEC['lid_panel'][2]:.1f}
Bakrygg / spine: {SPEC['rear_spine'][0]:.1f} × {SPEC['rear_spine'][1]:.1f} × {SPEC['rear_spine'][2]:.1f}
Frontflapp: {SPEC['front_flap'][0]:.1f} × {SPEC['front_flap'][1]:.1f} × {SPEC['front_flap'][2]:.1f}
Fleksibelt hengselgap: {SPEC['hinge_gap']:.1f}
Åpningsvinkel: ca. {SPEC['opening_angle']:.1f} grader

MAGNETER — STARTPUNKT
Antall: 2
Diameter: {SPEC['magnet_diameter']:.1f}
Tykkelse: {SPEC['magnet_thickness']:.1f}
Senter fra sidekant: {SPEC['magnet_edge_offset']:.1f}
Senter fra flappens nedre kant: {SPEC['magnet_vertical_center']:.1f}
Endelig styrke, polaritet og innbygging bekreftes av leverandør.

INNVENDIG STACK
52 kort: {SPEC['cards'][0]:.1f} × {SPEC['cards'][1]:.1f} × {SPEC['cards'][2]:.1f}
Foldet regelark: {SPEC['rules'][0]:.1f} × {SPEC['rules'][1]:.1f} × {SPEC['rules'][2]:.1f}
Spillerbrett: {SPEC['tray_outer'][0]:.1f} × {SPEC['tray_outer'][1]:.1f} × {SPEC['tray_outer'][2]:.1f}
Total stablehøyde: {STACK_H:.1f}
Fri høyde: {FREE_H:.1f}
Valgfri kompresjonsliner: {SPEC['compression_liner']:.1f}
Fri høyde med liner: {FREE_H_AFTER_LINER:.1f}

SPILLERBRETT
Utvendig: {SPEC['tray_outer'][0]:.1f} × {SPEC['tray_outer'][1]:.1f}
Gulv: {SPEC['tray_floor']:.1f}
Vegghøyde over gulv: {SPEC['tray_wall']:.1f}
Total høyde: {SPEC['tray_outer'][2]:.1f}
Sidevegger: {SPEC['tray_side_wall']:.1f}
Tre delte skillevegger: {SPEC['tray_divider']:.1f}
Front-/bakvegg: {SPEC['tray_front_wall']:.1f} / {SPEC['tray_back_wall']:.1f}
Fast frontvegg foran spillerlommer: {SPEC['pocket_front_wall']:.1f}
Innvendig bredde: {SPEC['inner_w']:.1f}
Innvendig dybde: {SPEC['inner_d']:.1f}
Fire lukkede spillerlommer: {PLAYER_CLEAR_W:.1f} × {SPEC['player_zone_d']:.1f}
Fast front/navnefelt: {SPEC['front_name_field'][0]:.1f} × {SPEC['front_name_field'][1]:.1f}

PER SPILLER
Trone maks envelope: {SPEC['throne'][0]:.1f} × {SPEC['throne'][1]:.1f} × {SPEC['throne'][2]:.1f}
Helseterning: {SPEC['health_die'][0]:.1f} kube
Bondeterninger: 12 stk, {SPEC['farmer_die'][0]:.1f} kube
Lagring: 2 × 3 × 2
Bondeterningstack: {SPEC['farmer_stack'][0]:.1f} × {SPEC['farmer_stack'][1]:.1f} × {SPEC['farmer_stack'][2]:.1f}

FRONT / NAVNEFELT
Fast felt i tray: {SPEC['front_name_field'][0]:.1f} × {SPEC['front_name_field'][1]:.1f}

PASSFORM OG UTREGNINGER
Sideklaring mellom spillerbrett og base: {SIDE_CLEAR_X:.2f} per side i bredde
Dybdeklaring mellom spillerbrett og base: {SIDE_CLEAR_Y:.2f} per side
Fri spillerbredde:
({SPEC['inner_w']:.1f} - 3 × {SPEC['tray_divider']:.1f}) / 4 = {PLAYER_CLEAR_W:.1f}
Klaring rundt 20 mm bred komponent:
({PLAYER_CLEAR_W:.1f} - 20.0) / 2 = {LATERAL_CLEAR_SIDE:.2f} per side
Dybdelogikk:
{SPEC['tray_front_wall']:.1f} front + {SPEC['front_name_zone_d']:.1f} navn + {SPEC['pocket_front_wall']:.1f} frontvegg + {SPEC['player_zone_d']:.1f} lomme + {SPEC['tray_back_wall']:.1f} bak = {SPEC['tray_outer'][1]:.1f}
Spillerlomme: 0.6 klaring + 19.6 trone + 0.4 gap + 30.0 terningstack + {BACK_CLEAR:.1f} klaring = {SPEC['player_zone_d']:.1f}

"""
    (DATA_DIR / "measurement.txt").write_text(
        measurement_text,
        encoding="utf-8",
    )


# ============================================================
# 01 — Clean manufacturing specification
# ============================================================
def diagram_01():
    fig, ax = plt.subplots(figsize=(14, 9))
    setup(
        ax,
        (0, 140),
        (0, 100),
        "ELEMENT WAR — ferdige produksjonsmål",
    )

    def section(x, y_top, width, height, title, lines):
        ax.add_patch(
            Rectangle(
                (x, y_top - height),
                width,
                height,
                fill=False,
                lw=1.2,
            )
        )
        ax.add_patch(
            Rectangle(
                (x, y_top - 7),
                width,
                7,
                facecolor="#eeeeee",
                edgecolor="black",
                lw=1.0,
            )
        )
        ax.text(
            x + 3,
            y_top - 3.5,
            title,
            ha="left",
            va="center",
            fontsize=11,
            weight="bold",
        )
        y = y_top - 11
        for line in lines:
            ax.text(
                x + 4,
                y,
                "• " + line,
                ha="left",
                va="top",
                fontsize=9,
            )
            y -= 5.0

    section(
        4,
        92,
        64,
        36,
        "BOKS OG LOKK",
        [
            f"Base utvendig: {SPEC['base_outer'][0]:.0f} × {SPEC['base_outer'][1]:.0f} × {SPEC['base_outer'][2]:.0f} mm",
            f"Base innvendig: {SPEC['base_inner'][0]:.0f} × {SPEC['base_inner'][1]:.0f} × {SPEC['base_inner'][2]:.0f} mm",
            f"Lukket totalmål: {SPEC['closed_total'][0]:.0f} × {SPEC['closed_total'][1]:.0f} × {SPEC['closed_total'][2]:.0f} mm",
            f"Rigid board: {SPEC['board']:.0f} mm nominelt",
            f"Toppanel: {SPEC['lid_panel'][0]:.0f} × {SPEC['lid_panel'][1]:.0f} × {SPEC['lid_panel'][2]:.0f} mm",
        ],
    )

    section(
        72,
        92,
        64,
        36,
        "FOLDBART MAGNETLOKK",
        [
            f"Bakrygg: {SPEC['rear_spine'][0]:.0f} × {SPEC['rear_spine'][1]:.0f} × {SPEC['rear_spine'][2]:.0f} mm",
            f"Frontflapp: {SPEC['front_flap'][0]:.0f} × {SPEC['front_flap'][1]:.0f} × {SPEC['front_flap'][2]:.0f} mm",
            f"Hengselgap: {SPEC['hinge_gap']:.0f} mm nominelt",
            f"Åpningsvinkel: ca. {SPEC['opening_angle']:.0f}°",
            "To skjulte magnetpunkter",
        ],
    )

    section(
        4,
        50,
        64,
        36,
        "INNVENDIG STACK",
        [
            f"Spillerbrett: {SPEC['tray_outer'][0]:.0f} × {SPEC['tray_outer'][1]:.0f} × {SPEC['tray_outer'][2]:.1f} mm",
            f"Regelark: {SPEC['rules'][0]:.0f} × {SPEC['rules'][1]:.0f} × {SPEC['rules'][2]:.0f} mm",
            f"52 kort: {SPEC['cards'][0]:.0f} × {SPEC['cards'][1]:.0f} × {SPEC['cards'][2]:.0f} mm",
            f"Total stack: {STACK_H:.1f} mm",
            f"Fri høyde: {FREE_H:.1f} mm",
        ],
    )

    section(
        72,
        50,
        64,
        36,
        "DELER OG KLARING",
        [
            f"Front/navnefelt: {SPEC['front_name_field'][0]:.1f} × {SPEC['front_name_field'][1]:.1f} mm",
            f"4 lukkede lommer: {PLAYER_CLEAR_W:.1f} × {SPEC['player_zone_d']:.1f} mm",
            f"Trone: {SPEC['throne'][0]:.1f} × {SPEC['throne'][1]:.1f} × {SPEC['throne'][2]:.1f} mm",
            f"Helseterning: {SPEC['health_die'][0]:.0f} mm kube",
            f"12 bondeterninger: {SPEC['farmer_die'][0]:.0f} mm kube",
        ],
    )

    ax.add_patch(
        Rectangle(
            (4, 5),
            132,
            7,
            facecolor="#f7f1e5",
            edgecolor="black",
            lw=1.0,
        )
    )
    ax.text(
        70,
        8.5,
        f"PASSFORM: 92 × 67 mm tray i 93 × 68 mm basehull = {SIDE_CLEAR_X:.1f} mm klaring per side.",
        ha="center",
        va="center",
        fontsize=9,
    )
    save_dual(fig, "01_clean_manufacturing_spec")


# ============================================================
# 02 — Magnetic hinged lid, open and closed
# ============================================================
def diagram_02():
    fig, axes = plt.subplots(1, 2, figsize=(15, 7))
    fig.suptitle(
        "ELEMENT WAR — foldbart magnetlokk",
        fontsize=16,
        y=0.98,
    )

    base_w = SPEC["base_outer"][0]
    base_h = SPEC["base_outer"][2]
    top_thickness = SPEC["lid_panel"][2]
    flap_h = SPEC["front_flap"][1]

    ax = axes[0]
    setup(ax, (-8, 110), (-5, 60), "Lukket")
    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            base_w,
            base_h,
            boxstyle="round,pad=0,rounding_size=1",
            facecolor="#222222",
            edgecolor="black",
            lw=1.2,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, base_h),
            base_w,
            top_thickness,
            facecolor="#333333",
            edgecolor="black",
            lw=1.0,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, base_h - flap_h),
            base_w,
            flap_h,
            facecolor="#2b2b2b",
            edgecolor="black",
            lw=1.0,
        )
    )

    for magnet_x in (
        SPEC["magnet_edge_offset"],
        base_w - SPEC["magnet_edge_offset"],
    ):
        ax.add_patch(
            Circle(
                (
                    magnet_x,
                    base_h
                    - flap_h
                    + SPEC["magnet_vertical_center"],
                ),
                SPEC["magnet_diameter"] / 2,
                fill=False,
                ls="--",
                lw=1.0,
            )
        )
        ax.text(
            magnet_x,
            base_h
            - flap_h
            + SPEC["magnet_vertical_center"],
            "M",
            ha="center",
            va="center",
            fontsize=7,
        )

    dim_h(ax, 0, base_w, -3, "97 mm")
    dim_v(
        ax,
        0,
        base_h + top_thickness,
        102,
        "45 mm lukket",
    )
    dim_v(
        ax,
        base_h - flap_h,
        base_h,
        -5,
        "20 mm frontflapp",
    )

    ax.text(
        base_w / 2,
        52,
        "Frontflappen lukker mot frontveggen.\n"
        "Magneter skjules i flapp og motstykke.",
        ha="center",
        va="top",
        fontsize=8,
    )

    ax = axes[1]
    setup(ax, (-20, 125), (-8, 115), "Åpen")

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            base_w,
            base_h,
            boxstyle="round,pad=0,rounding_size=1",
            facecolor="#222222",
            edgecolor="black",
            lw=1.2,
        )
    )

    hinge_x = base_w
    hinge_y = base_h
    theta = math.radians(
        180 - SPEC["opening_angle"]
    )
    panel_length = SPEC["lid_panel"][1]
    panel_thickness = SPEC["lid_panel"][2]

    ux = -math.cos(theta)
    uy = math.sin(theta)
    nx = -uy
    ny = ux

    p0 = (hinge_x, hinge_y)
    p1 = (
        hinge_x + ux * panel_length,
        hinge_y + uy * panel_length,
    )
    panel_poly = [
        p0,
        p1,
        (
            p1[0] + nx * panel_thickness,
            p1[1] + ny * panel_thickness,
        ),
        (
            p0[0] + nx * panel_thickness,
            p0[1] + ny * panel_thickness,
        ),
    ]
    ax.add_patch(
        Polygon(
            panel_poly,
            closed=True,
            facecolor="#333333",
            edgecolor="black",
            lw=1.0,
        )
    )

    fux = -uy
    fuy = -ux
    flap_poly = [
        p1,
        (
            p1[0] + fux * flap_h,
            p1[1] + fuy * flap_h,
        ),
        (
            p1[0] + fux * flap_h + nx * panel_thickness,
            p1[1] + fuy * flap_h + ny * panel_thickness,
        ),
        (
            p1[0] + nx * panel_thickness,
            p1[1] + ny * panel_thickness,
        ),
    ]
    ax.add_patch(
        Polygon(
            flap_poly,
            closed=True,
            facecolor="#2b2b2b",
            edgecolor="black",
            lw=1.0,
        )
    )

    ax.add_patch(
        Arc(
            (hinge_x, hinge_y),
            28,
            28,
            theta1=90,
            theta2=200,
            lw=0.9,
        )
    )
    ax.text(
        hinge_x - 18,
        hinge_y + 14,
        "≈110°",
        fontsize=8,
    )

    ax.text(
        47,
        100,
        "Konstruksjon:\n"
        "• rigid toppanel\n"
        "• bakrygg / spine\n"
        "• fleksibelt hengselområde\n"
        "• magnetisk frontflapp",
        ha="left",
        va="top",
        fontsize=8.5,
        bbox=dict(
            boxstyle="round,pad=0.4",
            fill=False,
        ),
    )

    save_dual(
        fig,
        "02_magnetic_hinged_lid_open_closed",
    )


# ============================================================
# 03 — Lid board-panel map
# ============================================================
def diagram_03():
    fig, ax = plt.subplots(figsize=(10, 14))
    setup(
        ax,
        (-8, 116),
        (0, 186),
        "ELEMENT WAR — board-paneler for foldbart magnetlokk",
    )

    width = SPEC["lid_panel"][0]
    front_h = SPEC["front_flap"][1]
    top_h = SPEC["lid_panel"][1]
    spine_h = SPEC["rear_spine"][1]
    gap = SPEC["hinge_gap"]
    y0 = 25.0

    ax.add_patch(
        Rectangle(
            (0, y0),
            width,
            spine_h,
            facecolor="#888888",
            edgecolor="black",
            lw=1.1,
        )
    )
    ax.text(
        width / 2,
        y0 + spine_h / 2,
        "BAKRYGG / SPINE\n97 × 43 × 2 mm",
        ha="center",
        va="center",
        fontsize=11,
    )

    gap_1_y = y0 + spine_h
    ax.add_patch(
        Rectangle(
            (0, gap_1_y),
            width,
            gap,
            facecolor="#f1e2c0",
            edgecolor="black",
            lw=0.7,
        )
    )
    ax.text(
        width / 2,
        gap_1_y + gap / 2,
        "3 mm fleksibelt hengselgap",
        ha="center",
        va="center",
        fontsize=8,
    )

    top_y = gap_1_y + gap
    ax.add_patch(
        Rectangle(
            (0, top_y),
            width,
            top_h,
            facecolor="#333333",
            edgecolor="black",
            lw=1.1,
        )
    )
    ax.text(
        width / 2,
        top_y + top_h / 2,
        "TOPPANEL\n97 × 72 × 2 mm",
        color="white",
        ha="center",
        va="center",
        fontsize=12,
    )

    gap_2_y = top_y + top_h
    ax.add_patch(
        Rectangle(
            (0, gap_2_y),
            width,
            gap,
            facecolor="#f1e2c0",
            edgecolor="black",
            lw=0.7,
        )
    )
    ax.text(
        width / 2,
        gap_2_y + gap / 2,
        "3 mm fleksibelt hengselgap",
        ha="center",
        va="center",
        fontsize=8,
    )

    flap_y = gap_2_y + gap
    ax.add_patch(
        Rectangle(
            (0, flap_y),
            width,
            front_h,
            facecolor="#555555",
            edgecolor="black",
            lw=1.1,
        )
    )
    ax.text(
        width / 2,
        flap_y + front_h / 2,
        "MAGNETISK FRONTFLAPP\n97 × 20 × 2 mm",
        color="white",
        ha="center",
        va="center",
        fontsize=10,
    )

    for magnet_x in (
        SPEC["magnet_edge_offset"],
        width - SPEC["magnet_edge_offset"],
    ):
        magnet_y = (
            flap_y + SPEC["magnet_vertical_center"]
        )
        ax.add_patch(
            Circle(
                (magnet_x, magnet_y),
                SPEC["magnet_diameter"] / 2,
                fill=False,
                ls="--",
                lw=1.1,
            )
        )
        ax.text(
            magnet_x,
            magnet_y,
            "M",
            ha="center",
            va="center",
            fontsize=8,
        )

    dim_h(
        ax,
        0,
        width,
        18,
        "97 mm ferdig panelbredde",
    )
    dim_v(
        ax,
        y0,
        y0 + spine_h,
        102,
        "43 mm spine",
    )
    dim_v(
        ax,
        top_y,
        top_y + top_h,
        106,
        "72 mm toppanel",
    )
    dim_v(
        ax,
        flap_y,
        flap_y + front_h,
        110,
        "20 mm frontflapp",
    )

    ax.text(
        0,
        182,
        "Board-panelkart før omslag",
        ha="left",
        va="top",
        fontsize=11,
        weight="bold",
    )
    ax.text(
        0,
        178,
        "Leverandøren legger til omslagsmargin, turn-ins, "
        "papirretning og endelig hengselkonstruksjon.",
        ha="left",
        va="top",
        fontsize=8.7,
    )
    ax.text(
        0,
        6,
        "Magnettype, styrke og matching motstykke "
        "fastsettes med emballasjeleverandør.",
        ha="left",
        va="top",
        fontsize=8.2,
    )

    save_dual(
        fig,
        "03_magnetic_lid_board_panel_map",
    )


# ============================================================
# 04 — Internal stack production section, long side
# ============================================================
def diagram_04():
    fig, ax = plt.subplots(figsize=(13, 7))
    setup(
        ax,
        (-8, 112),
        (-6, 52),
        "ELEMENT WAR — produksjonssnitt, innvendig stack, lang side",
    )

    outer_w = SPEC["base_outer"][0]
    outer_h = SPEC["base_outer"][2]
    inner_w = SPEC["base_inner"][0]
    inner_h = SPEC["base_inner"][2]
    board = SPEC["board"]

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            outer_w,
            outer_h,
            boxstyle="round,pad=0,rounding_size=1",
            fill=False,
            lw=2.0,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, 0),
            outer_w,
            board,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, board),
            board,
            inner_h,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (outer_w - board, board),
            board,
            inner_h,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )

    card_x = board + (
        inner_w - SPEC["cards"][0]
    ) / 2
    ax.add_patch(
        Rectangle(
            (card_x, board),
            SPEC["cards"][0],
            SPEC["cards"][2],
            facecolor="#d8d8d8",
            edgecolor="black",
            lw=1.0,
        )
    )
    for index in range(1, 10):
        y = (
            board
            + SPEC["cards"][2] * index / 10
        )
        ax.add_line(
            Line2D(
                [
                    card_x,
                    card_x + SPEC["cards"][0],
                ],
                [y, y],
                lw=0.35,
                alpha=0.5,
            )
        )
    ax.text(
        card_x + SPEC["cards"][0] / 2,
        board + SPEC["cards"][2] / 2,
        "52 kort\n88 × 63 × 17",
        ha="center",
        va="center",
        fontsize=9,
    )

    rule_x = board + (
        inner_w - SPEC["rules"][0]
    ) / 2
    rule_y = board + SPEC["cards"][2]
    ax.add_patch(
        Rectangle(
            (rule_x, rule_y),
            SPEC["rules"][0],
            SPEC["rules"][2],
            facecolor="#f1e2c0",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.text(
        rule_x + SPEC["rules"][0] / 2,
        rule_y + 1.6,
        "foldet regelark 89 × 64 × 1",
        ha="center",
        fontsize=8,
    )

    tray_x = board + (
        inner_w - SPEC["tray_outer"][0]
    ) / 2
    tray_y = rule_y + SPEC["rules"][2]
    ax.add_patch(
        Rectangle(
            (tray_x, tray_y),
            SPEC["tray_outer"][0],
            SPEC["tray_outer"][2],
            facecolor="#c6a573",
            alpha=0.75,
            edgecolor="black",
            lw=1.0,
        )
    )
    ax.text(
        tray_x + SPEC["tray_outer"][0] / 2,
        tray_y + SPEC["tray_outer"][2] / 2,
        "spillerbrett + komponenter\n92 × 67 × 21.5",
        ha="center",
        va="center",
        fontsize=9,
    )

    liner_y = (
        board
        + inner_h
        - SPEC["compression_liner"]
    )
    ax.add_patch(
        Rectangle(
            (board, liner_y),
            inner_w,
            SPEC["compression_liner"],
            facecolor="#eeeeee",
            edgecolor="black",
            lw=0.7,
            hatch="//",
        )
    )
    ax.text(
        board + inner_w / 2,
        liner_y + 0.5,
        "valgfri 1 mm kompresjonsliner",
        ha="center",
        va="center",
        fontsize=7,
    )

    dim_h(ax, 0, outer_w, -3, "97 mm")
    dim_v(ax, 0, outer_h, 103, "43 mm base")
    dim_v(
        ax,
        board,
        board + inner_h,
        108,
        "41 mm innvendig",
    )
    ax.text(
        50,
        48,
        f"Stack: {STACK_H:.1f} mm | "
        f"fri høyde: {FREE_H:.1f} mm | "
        f"med liner: {FREE_H_AFTER_LINER:.1f} mm",
        ha="center",
        va="top",
        fontsize=9,
    )

    save_dual(
        fig,
        "04_internal_stack_long_side_production_section",
    )


# ============================================================
# 05 — Internal stack production section, short side
# ============================================================
def diagram_05():
    fig, ax = plt.subplots(figsize=(11, 7))
    setup(
        ax,
        (-8, 87),
        (-6, 52),
        "ELEMENT WAR — produksjonssnitt, innvendig stack, kort side",
    )

    outer_w = SPEC["base_outer"][1]
    outer_h = SPEC["base_outer"][2]
    inner_w = SPEC["base_inner"][1]
    inner_h = SPEC["base_inner"][2]
    board = SPEC["board"]

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            outer_w,
            outer_h,
            boxstyle="round,pad=0,rounding_size=1",
            fill=False,
            lw=2.0,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, 0),
            outer_w,
            board,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, board),
            board,
            inner_h,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (outer_w - board, board),
            board,
            inner_h,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )

    card_x = board + (inner_w - SPEC["cards"][1]) / 2
    ax.add_patch(
        Rectangle(
            (card_x, board),
            SPEC["cards"][1],
            SPEC["cards"][2],
            facecolor="#d8d8d8",
            edgecolor="black",
            lw=1.0,
        )
    )
    for index in range(1, 10):
        y = board + SPEC["cards"][2] * index / 10
        ax.add_line(
            Line2D(
                [card_x, card_x + SPEC["cards"][1]],
                [y, y],
                lw=0.35,
                alpha=0.5,
            )
        )
    ax.text(
        card_x + SPEC["cards"][1] / 2,
        board + SPEC["cards"][2] / 2,
        "52 kort\n88 × 63 × 17",
        ha="center",
        va="center",
        fontsize=9,
    )

    rule_x = board + (inner_w - SPEC["rules"][1]) / 2
    rule_y = board + SPEC["cards"][2]
    ax.add_patch(
        Rectangle(
            (rule_x, rule_y),
            SPEC["rules"][1],
            SPEC["rules"][2],
            facecolor="#f1e2c0",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.text(
        rule_x + SPEC["rules"][1] / 2,
        rule_y + 1.6,
        "foldet regelark 89 × 64 × 1",
        ha="center",
        fontsize=8,
    )

    tray_x = board + (inner_w - SPEC["tray_outer"][1]) / 2
    tray_y = rule_y + SPEC["rules"][2]
    ax.add_patch(
        Rectangle(
            (tray_x, tray_y),
            SPEC["tray_outer"][1],
            SPEC["tray_outer"][2],
            facecolor="#c6a573",
            alpha=0.75,
            edgecolor="black",
            lw=1.0,
        )
    )
    ax.text(
        tray_x + SPEC["tray_outer"][1] / 2,
        tray_y + SPEC["tray_outer"][2] / 2,
        "spillerbrett + komponenter\n92 × 67 × 21.5",
        ha="center",
        va="center",
        fontsize=9,
    )

    liner_y = board + inner_h - SPEC["compression_liner"]
    ax.add_patch(
        Rectangle(
            (board, liner_y),
            inner_w,
            SPEC["compression_liner"],
            facecolor="#eeeeee",
            edgecolor="black",
            lw=0.7,
            hatch="//",
        )
    )
    ax.text(
        board + inner_w / 2,
        liner_y + 0.5,
        "valgfri 1 mm kompresjonsliner",
        ha="center",
        va="center",
        fontsize=7,
    )

    dim_h(ax, 0, outer_w, -3, "72 mm")
    dim_v(ax, 0, outer_h, 78, "43 mm base")
    dim_v(ax, board, board + inner_h, 83, "41 mm innvendig")
    ax.text(
        36,
        48,
        f"Stack: {STACK_H:.1f} mm | "
        f"fri høyde: {FREE_H:.1f} mm | "
        f"med liner: {FREE_H_AFTER_LINER:.1f} mm",
        ha="center",
        va="top",
        fontsize=9,
    )

    save_dual(
        fig,
        "05_internal_stack_short_side_production_section",
    )


# ============================================================
# 06 — Finished dimensions per component
# ============================================================
def diagram_06():
    fig, axes = plt.subplots(3, 2, figsize=(14, 13))
    fig.suptitle(
        "ELEMENT WAR — produksjonsmål per del",
        fontsize=16,
        y=0.995,
    )

    def component_views(
        ax,
        title,
        top_w,
        top_d,
        side_w,
        side_h,
        color,
        notes,
        top_details=None,
        side_details=None,
    ):
        ax.set_aspect("equal")
        ax.axis("off")
        ax.set_xlim(0, 110)
        ax.set_ylim(0, 80)
        ax.set_title(title, fontsize=12)
        ax.text(6, 72, "TOPP", fontsize=8, weight="bold")
        ax.text(59, 72, "SIDE", fontsize=8, weight="bold")

        top_scale = min(45 / top_w, 32 / top_d)
        side_scale = min(45 / side_w, 32 / side_h)

        top_x = 6
        top_y = 25
        side_x = 59
        side_y = 25

        ax.add_patch(
            Rectangle(
                (top_x, top_y),
                top_w * top_scale,
                top_d * top_scale,
                facecolor=color,
                edgecolor="black",
                lw=1.0,
                alpha=0.75,
            )
        )
        ax.add_patch(
            Rectangle(
                (side_x, side_y),
                side_w * side_scale,
                side_h * side_scale,
                facecolor=color,
                edgecolor="black",
                lw=1.0,
                alpha=0.75,
            )
        )

        if top_details:
            top_details(
                ax,
                top_x,
                top_y,
                top_scale,
            )
        if side_details:
            side_details(
                ax,
                side_x,
                side_y,
                side_scale,
            )

        ax.text(
            6,
            15,
            f"Topp: {top_w:g} × {top_d:g} mm",
            fontsize=8,
        )
        ax.text(
            59,
            15,
            f"Side: {side_w:g} × {side_h:g} mm",
            fontsize=8,
        )
        ax.text(
            6,
            7,
            notes,
            fontsize=7.5,
            ha="left",
            va="top",
        )

    component_views(
        axes[0, 0],
        "Spillerbrett",
        92,
        67,
        92,
        21.5,
        "#c6a573",
        "Ferdig yttermål. 4 lukkede spillerlommer + fast navnefelt.",
    )
    component_views(
        axes[0, 1],
        "Kort + regelark",
        89,
        64,
        89,
        18,
        "#f1e2c0",
        "Kort: 88×63×17. Regelark: 89×64×1.",
    )
    component_views(
        axes[1, 0],
        "Lukket spillerlomme",
        PLAYER_CLEAR_W,
        SPEC["player_zone_d"],
        PLAYER_CLEAR_W,
        SPEC["tray_outer"][2],
        "#ead8b5",
        "Fri lomme per spiller. Sidevegger deles mellom naboer.",
    )
    component_views(
        axes[1, 1],
        "Kongetrone",
        19.6,
        19.6,
        19.6,
        19.6,
        "#777777",
        "Maks ferdig envelope. Intern åpning til 16 mm terning.",
    )

    def stack_top(
        ax,
        x,
        y,
        scale,
    ):
        for column in range(2):
            for row in range(3):
                ax.add_patch(
                    Rectangle(
                        (
                            x + column * 10 * scale,
                            y + row * 10 * scale,
                        ),
                        10 * scale,
                        10 * scale,
                        fill=False,
                        edgecolor="black",
                        lw=0.6,
                    )
                )

    def stack_side(
        ax,
        x,
        y,
        scale,
    ):
        for column in range(3):
            for row in range(2):
                ax.add_patch(
                    Rectangle(
                        (
                            x + column * 10 * scale,
                            y + row * 10 * scale,
                        ),
                        10 * scale,
                        10 * scale,
                        fill=False,
                        edgecolor="black",
                        lw=0.6,
                    )
                )

    component_views(
        axes[2, 0],
        "Bondeterning-stack",
        20,
        30,
        30,
        20,
        "#6c98c6",
        "12 × 10 mm terninger. Lagring 2×3×2.",
        top_details=stack_top,
        side_details=stack_side,
    )
    component_views(
        axes[2, 1],
        "Rigid boksbase",
        97,
        72,
        97,
        43,
        "#333333",
        "Innvendig 93×68×41 mm. 2 mm board.",
    )

    save_dual(
        fig,
        "06_component_finished_dimensions",
    )


# ============================================================
# 07 — Full assembly, top grouping
# ============================================================
def diagram_07():
    fig, ax = plt.subplots(figsize=(14, 9))
    setup(
        ax,
        (-10, 120),
        (-20, 95),
        "ELEMENT WAR — full gruppering, toppvisning",
    )

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            SPEC["base_outer"][0],
            SPEC["base_outer"][1],
            boxstyle="round,pad=0,rounding_size=1.2",
            fill=False,
            lw=2.0,
        )
    )

    board = SPEC["board"]
    ax.add_patch(
        Rectangle(
            (board, board),
            SPEC["base_inner"][0],
            SPEC["base_inner"][1],
            fill=False,
            ls="--",
            lw=1.0,
        )
    )

    tray_x = board + SIDE_CLEAR_X
    tray_y = board + SIDE_CLEAR_Y
    add_top_player_layout(
        ax,
        origin=(tray_x, tray_y),
        scale=1.0,
        labels=True,
    )

    ax.add_patch(
        Rectangle(
            (104, 8),
            12,
            8.7,
            facecolor="#d8d8d8",
            edgecolor="black",
        )
    )
    ax.text(
        110,
        12.3,
        "kort\n88×63",
        ha="center",
        va="center",
        fontsize=6,
    )
    ax.add_patch(
        Rectangle(
            (103.5, 18.5),
            13,
            9.0,
            facecolor="#f1e2c0",
            edgecolor="black",
        )
    )
    ax.text(
        110,
        23.0,
        "regler\n89×64",
        ha="center",
        va="center",
        fontsize=6,
    )
    ax.text(
        104,
        32,
        "Innlegg under brettet",
        fontsize=8,
        weight="bold",
        ha="left",
    )

    dim_h(
        ax,
        0,
        SPEC["base_outer"][0],
        -7,
        "base utvendig 97 mm",
    )
    dim_v(
        ax,
        0,
        SPEC["base_outer"][1],
        102,
        "72 mm",
    )
    dim_h(
        ax,
        tray_x,
        tray_x + SPEC["tray_outer"][0],
        76,
        "spillerbrett 92 mm",
    )
    dim_v(
        ax,
        tray_y,
        tray_y + SPEC["tray_outer"][1],
        -7,
        "67 mm",
    )

    ax.text(
        102,
        66,
        "Gruppering:\n"
        "• Base\n"
        "• Lukket tray med 4 spillerlommer\n"
        "• Fast front/navnefelt\n"
        "• Foldet regelark\n"
        "• Kortstokk\n"
        "• Lokk separat",
        ha="left",
        va="top",
        fontsize=9,
        bbox=dict(
            boxstyle="round,pad=0.4",
            fill=False,
        ),
    )

    save_dual(
        fig,
        "07_full_assembly_top_grouping",
    )


# ============================================================
# 08 — Full assembly, long side
# ============================================================
def diagram_08():
    fig, ax = plt.subplots(figsize=(14, 7))
    setup(
        ax,
        (-10, 135),
        (-6, 58),
        "ELEMENT WAR — full gruppering, lang sideprofil",
    )

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            SPEC["base_outer"][0],
            SPEC["base_outer"][2],
            boxstyle="round,pad=0,rounding_size=1.0",
            fill=False,
            lw=2.0,
        )
    )

    board = SPEC["board"]
    ax.add_patch(
        Rectangle(
            (0, 0),
            SPEC["base_outer"][0],
            board,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, board),
            board,
            SPEC["base_inner"][2],
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (
                SPEC["base_outer"][0] - board,
                board,
            ),
            board,
            SPEC["base_inner"][2],
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )

    card_x = board + (
        SPEC["base_inner"][0]
        - SPEC["cards"][0]
    ) / 2
    ax.add_patch(
        Rectangle(
            (card_x, board),
            SPEC["cards"][0],
            SPEC["cards"][2],
            facecolor="#d8d8d8",
            edgecolor="black",
        )
    )

    rule_x = board + (
        SPEC["base_inner"][0]
        - SPEC["rules"][0]
    ) / 2
    rule_y = board + SPEC["cards"][2]
    ax.add_patch(
        Rectangle(
            (rule_x, rule_y),
            SPEC["rules"][0],
            SPEC["rules"][2],
            facecolor="#f1e2c0",
            edgecolor="black",
        )
    )

    tray_x = board + SIDE_CLEAR_X
    tray_y = rule_y + SPEC["rules"][2]
    ax.add_patch(
        Rectangle(
            (tray_x, tray_y),
            SPEC["tray_outer"][0],
            SPEC["tray_floor"],
            facecolor="#c6a573",
            edgecolor="black",
        )
    )

    component_y = tray_y + SPEC["tray_floor"]
    for rel_x, width in (
        (0, SPEC["tray_side_wall"]),
        (
            SPEC["tray_outer"][0] - SPEC["tray_side_wall"],
            SPEC["tray_side_wall"],
        ),
    ):
        ax.add_patch(
            Rectangle(
                (tray_x + rel_x, component_y),
                width,
                SPEC["tray_wall"],
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )

    for index, color in enumerate(PLAYER_COLORS):
        player_x = (
            tray_x
            + SPEC["tray_side_wall"]
            + index
            * (
                PLAYER_CLEAR_W
                + SPEC["tray_divider"]
            )
        )
        ax.add_patch(
            Rectangle(
                (
                    player_x + LATERAL_CLEAR_SIDE,
                    component_y,
                ),
                20.0,
                19.6,
                facecolor=color,
                alpha=0.40,
                edgecolor="black",
                lw=0.7,
            )
        )
        ax.text(
            player_x + PLAYER_CLEAR_W / 2,
            component_y + 9.8,
            PLAYER_NAMES[index],
            ha="center",
            va="center",
            fontsize=6,
        )
        if index < 3:
            ax.add_patch(
                Rectangle(
                    (
                        player_x + PLAYER_CLEAR_W,
                        component_y,
                    ),
                    SPEC["tray_divider"],
                    20.0,
                    facecolor="#c6ad83",
                    edgecolor="black",
                    lw=0.4,
                )
            )

    ax.add_patch(
        Rectangle(
            (0, SPEC["base_outer"][2]),
            SPEC["base_outer"][0],
            2,
            fill=False,
            ls="--",
            lw=0.8,
        )
    )
    ax.text(
        48.5,
        47.8,
        "lokk-plan",
        ha="center",
        va="bottom",
        fontsize=8,
    )

    dim_h(
        ax,
        0,
        SPEC["base_outer"][0],
        -3,
        "97 mm",
    )
    dim_v(
        ax,
        0,
        SPEC["base_outer"][2],
        103,
        "43 mm",
    )

    ax.text(
        108,
        49,
        "Lag fra bunn til topp:\n"
        "1. Kort\n"
        "2. Foldet regelark\n"
        "3. Lukket spillertray\n"
        "4. 4 spillerlommer\n"
        "5. Lokk med magnetflapp",
        ha="left",
        va="top",
        fontsize=8.7,
        bbox=dict(
            boxstyle="round,pad=0.4",
            fill=False,
        ),
    )

    save_dual(
        fig,
        "08_full_assembly_long_side_grouping",
    )


# ============================================================
# 10 — Exploded assembly
# ============================================================
def diagram_10():
    fig, ax = plt.subplots(figsize=(15, 12))
    setup(
        ax,
        (-8, 125),
        (-6, 190),
        "ELEMENT WAR — eksplodert gruppering",
    )

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            SPEC["base_outer"][0],
            SPEC["base_outer"][1],
            boxstyle="round,pad=0,rounding_size=1.2",
            fill=False,
            lw=2.0,
        )
    )
    ax.text(
        SPEC["base_outer"][0] / 2,
        -3,
        "BASE 97 × 72",
        ha="center",
        fontsize=9,
    )

    card_x = 5
    card_y = 18
    ax.add_patch(
        Rectangle(
            (card_x, card_y),
            SPEC["cards"][0],
            SPEC["cards"][1],
            facecolor="#d8d8d8",
            edgecolor="black",
        )
    )
    ax.text(
        card_x + 44,
        card_y + 31.5,
        "52 KORT\n88 × 63 × 17",
        ha="center",
        va="center",
        fontsize=9,
    )

    rule_x = 4.5
    rule_y = 43
    ax.add_patch(
        Rectangle(
            (rule_x, rule_y),
            SPEC["rules"][0],
            SPEC["rules"][1],
            facecolor="#f1e2c0",
            edgecolor="black",
        )
    )
    ax.text(
        rule_x + 44.5,
        rule_y + 32,
        "FOLDET REGELARK\n89 × 64 × 1",
        ha="center",
        va="center",
        fontsize=9,
    )

    tray_x = 2.5
    tray_y = 72
    add_top_player_layout(
        ax,
        origin=(tray_x, tray_y),
        scale=1.0,
        labels=False,
    )
    ax.text(
        tray_x + 46,
        tray_y + 70,
        "LUKKET SPILLERTRAY + 4 LOMMER",
        ha="center",
        va="bottom",
        fontsize=10,
    )

    group_x = 103
    group_y = 85
    ax.add_patch(
        Rectangle(
            (group_x, group_y),
            16,
            16,
            facecolor=PLAYER_COLORS[1],
            alpha=0.7,
            edgecolor="black",
        )
    )
    ax.text(
        group_x + 8,
        group_y + 8,
        "TRONE",
        ha="center",
        va="center",
        fontsize=7,
    )
    for column in range(2):
        for row in range(3):
            ax.add_patch(
                Rectangle(
                    (
                        group_x + column * 8,
                        group_y + 22 + row * 8,
                    ),
                    8,
                    8,
                    facecolor=PLAYER_COLORS[1],
                    alpha=0.45,
                    edgecolor="black",
                )
            )
    ax.text(
        group_x + 8,
        group_y + 50,
        "12 BONDETERNINGER\n2×3×2",
        ha="center",
        va="bottom",
        fontsize=7,
    )

    lid_y = 145
    ax.add_patch(
        Rectangle(
            (0, lid_y),
            SPEC["lid_panel"][0],
            SPEC["lid_panel"][1] * 0.35,
            fill=False,
            ls="--",
            lw=1.0,
        )
    )
    ax.add_patch(
        Rectangle(
            (
                0,
                lid_y
                + SPEC["lid_panel"][1] * 0.35
                + 3,
            ),
            SPEC["front_flap"][0],
            SPEC["front_flap"][1],
            fill=False,
            ls="--",
            lw=1.0,
        )
    )
    ax.text(
        48.5,
        lid_y + 12,
        "LOKKPANEL",
        ha="center",
        va="center",
        fontsize=9,
    )
    ax.text(
        48.5,
        lid_y + 39,
        "MAGNETISK FRONTFLAPP",
        ha="center",
        va="center",
        fontsize=8,
    )

    for start, end in (
        ((49, 139), (49, 132)),
        ((49, 69), (49, 63)),
        ((49, 39), (49, 32)),
        ((49, 15), (49, 8)),
    ):
        ax.annotate(
            "",
            xy=end,
            xytext=start,
            arrowprops=dict(
                arrowstyle="->",
                lw=1,
            ),
        )

    ax.text(
        102,
        180,
        "Exploded order:\n"
        "• Lokk / flapp\n"
        "• Closed player tray with pockets\n"
        "• Folded rule sheet\n"
        "• Card deck\n"
        "• Base",
        ha="left",
        va="top",
        fontsize=9,
        bbox=dict(
            boxstyle="round,pad=0.4",
            fill=False,
        ),
    )

    save_dual(
        fig,
        "10_exploded_assembly_grouping",
    )


# ============================================================
# 11 — Per-player group detail
# ============================================================
def diagram_11():
    fig, axes = plt.subplots(1, 2, figsize=(14, 6))
    fig.suptitle(
        "ELEMENT WAR — spillergruppe detalj",
        fontsize=16,
        y=0.98,
    )

    ax = axes[0]
    setup(ax, (-2, 30), (-2, 60), "Topp")

    ax.add_patch(
        Rectangle(
            (0, 0),
            PLAYER_CLEAR_W,
            SPEC["player_zone_d"],
            fill=False,
            lw=1.2,
        )
    )
    ax.add_patch(
        Rectangle(
            (
                (
                    PLAYER_CLEAR_W
                    - SPEC["throne"][0]
                )
                / 2,
                0.6,
            ),
            SPEC["throne"][0],
            SPEC["throne"][1],
            facecolor=PLAYER_COLORS[1],
            alpha=0.7,
            edgecolor="black",
        )
    )
    ax.add_patch(
        Rectangle(
            (
                (
                    PLAYER_CLEAR_W
                    - SPEC["health_die"][0]
                )
                / 2,
                2.4,
            ),
            SPEC["health_die"][0],
            SPEC["health_die"][1],
            facecolor="#f2efe7",
            edgecolor="black",
        )
    )

    stack_x = (
        PLAYER_CLEAR_W
        - SPEC["farmer_stack"][0]
    ) / 2
    stack_y = 0.6 + SPEC["throne"][1] + 0.4
    for column in range(2):
        for row in range(3):
            ax.add_patch(
                Rectangle(
                    (
                        stack_x + column * 10,
                        stack_y + row * 10,
                    ),
                    10,
                    10,
                    facecolor=PLAYER_COLORS[1],
                    alpha=0.45,
                    edgecolor="black",
                )
            )

    dim_h(
        ax,
        0,
        PLAYER_CLEAR_W,
        -1.5,
        "21.5 mm",
    )
    dim_v(
        ax,
        0,
        SPEC["player_zone_d"],
        24.5,
        "51 mm",
    )
    ax.text(
        1,
        57,
        "1 lukket spillerlomme",
        fontsize=9,
        weight="bold",
    )

    ax = axes[1]
    setup(ax, (-2, 60), (-4, 32), "Side")

    ax.add_patch(
        Rectangle(
            (0, 0),
            SPEC["player_zone_d"],
            SPEC["tray_outer"][2],
            fill=False,
            lw=1.0,
        )
    )

    throne_x = 0.6
    ax.add_patch(
        Rectangle(
            (throne_x, 0),
            SPEC["throne"][1],
            SPEC["throne"][2],
            facecolor=PLAYER_COLORS[2],
            alpha=0.65,
            edgecolor="black",
        )
    )
    for column in range(3):
        for row in range(2):
            ax.add_patch(
                Rectangle(
                    (
                        throne_x + SPEC["throne"][1] + 0.4 + column * 10,
                        row * 10,
                    ),
                    10,
                    10,
                    facecolor=PLAYER_COLORS[2],
                    alpha=0.45,
                    edgecolor="black",
                )
            )

    dim_h(ax, 0, SPEC["player_zone_d"], -3, "51 mm lomme")
    dim_h(ax, throne_x, throne_x + SPEC["throne"][1], 27, "19.6")
    dim_h(
        ax,
        throne_x + SPEC["throne"][1] + 0.4,
        throne_x + SPEC["throne"][1] + 0.4 + SPEC["farmer_stack"][1],
        27,
        "30",
    )
    dim_v(ax, 0, SPEC["tray_outer"][2], 55, "21.5 mm høy")
    ax.text(
        25,
        30,
        "0.6 + 19.6 + 0.4 + 30 + 0.4 = 51 mm dybde",
        ha="center",
        fontsize=8,
    )

    save_dual(
        fig,
        "11_player_group_detail",
    )


# ============================================================
# 09 — Full assembly, short side
# ============================================================
def diagram_09():
    fig, ax = plt.subplots(figsize=(11, 8))
    setup(
        ax,
        (-8, 90),
        (-6, 55),
        "ELEMENT WAR — full gruppering, kort sideprofil",
    )

    depth = SPEC["base_outer"][1]
    height = SPEC["base_outer"][2]
    inner_depth = SPEC["base_inner"][1]
    board = SPEC["board"]

    ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            depth,
            height,
            boxstyle="round,pad=0,rounding_size=1",
            fill=False,
            lw=2.0,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, 0),
            depth,
            board,
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (0, board),
            board,
            SPEC["base_inner"][2],
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )
    ax.add_patch(
        Rectangle(
            (
                depth - board,
                board,
            ),
            board,
            SPEC["base_inner"][2],
            facecolor="#333333",
            edgecolor="black",
            lw=0.8,
        )
    )

    card_x = board + (
        inner_depth - SPEC["cards"][1]
    ) / 2
    ax.add_patch(
        Rectangle(
            (card_x, board),
            SPEC["cards"][1],
            SPEC["cards"][2],
            facecolor="#d8d8d8",
            edgecolor="black",
        )
    )

    rule_x = board + (
        inner_depth - SPEC["rules"][1]
    ) / 2
    rule_y = board + SPEC["cards"][2]
    ax.add_patch(
        Rectangle(
            (rule_x, rule_y),
            SPEC["rules"][1],
            SPEC["rules"][2],
            facecolor="#f1e2c0",
            edgecolor="black",
        )
    )

    tray_y = rule_y + SPEC["rules"][2]
    tray_x = board + SIDE_CLEAR_Y
    ax.add_patch(
        Rectangle(
            (
                tray_x,
                tray_y,
            ),
            SPEC["tray_outer"][1],
            SPEC["tray_outer"][2],
            facecolor="#c6a573",
            alpha=0.7,
            edgecolor="black",
        )
    )

    component_y = tray_y + SPEC["tray_floor"]

    for rel_x, width in (
        (0, SPEC["tray_front_wall"]),
        (
            SPEC["tray_front_wall"]
            + SPEC["front_name_zone_d"],
            SPEC["pocket_front_wall"],
        ),
        (
            SPEC["tray_outer"][1]
            - SPEC["tray_back_wall"],
            SPEC["tray_back_wall"],
        ),
    ):
        ax.add_patch(
            Rectangle(
                (tray_x + rel_x, component_y),
                width,
                SPEC["tray_wall"],
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )

    ax.add_patch(
        Rectangle(
            (
                tray_x + SPEC["tray_front_wall"],
                component_y,
            ),
            SPEC["front_name_zone_d"],
            SPEC["tray_wall"],
            facecolor="#ead8b5",
            edgecolor="black",
        )
    )
    ax.text(
        tray_x + SPEC["tray_front_wall"] + SPEC["front_name_zone_d"] / 2,
        component_y + SPEC["tray_wall"] / 2,
        "navn",
        ha="center",
        va="center",
        fontsize=7,
    )

    throne_x = tray_x + POCKET_Y + 0.6
    ax.add_patch(
        Rectangle(
            (throne_x, component_y),
            SPEC["throne"][1],
            SPEC["throne"][2],
            facecolor=PLAYER_COLORS[0],
            alpha=0.6,
            edgecolor="black",
        )
    )
    for column in range(3):
        for row in range(2):
            ax.add_patch(
                Rectangle(
                    (
                        throne_x
                        + SPEC["throne"][1]
                        + 0.4
                        + column * 10,
                        component_y + row * 10,
                    ),
                    10,
                    10,
                    facecolor=PLAYER_COLORS[0],
                    alpha=0.45,
                    edgecolor="black",
                )
            )

    dim_h(
        ax,
        0,
        depth,
        -3,
        "72 mm base dybde",
    )
    dim_h(
        ax,
        tray_x + SPEC["tray_front_wall"],
        tray_x + SPEC["tray_front_wall"] + SPEC["front_name_zone_d"],
        45,
        "11.5",
    )
    dim_h(
        ax,
        tray_x + POCKET_Y,
        tray_x + POCKET_TOP,
        45,
        "51",
    )
    dim_v(
        ax,
        0,
        height,
        78,
        "43 mm",
    )
    ax.text(
        36,
        52,
        "Kort side:\n1.5 + 11.5 + 1.5 + 51 + 1.5 = 67 mm",
        ha="center",
        va="top",
        fontsize=9,
    )

    save_dual(
        fig,
        "09_full_assembly_short_side_grouping",
    )


# ============================================================
# 12 — Isolated removable player separator, multiple views
# ============================================================
def diagram_12():
    fig = plt.figure(figsize=(15, 11))
    top_ax = fig.add_axes([0.05, 0.50, 0.62, 0.43])
    long_ax = fig.add_axes([0.05, 0.08, 0.62, 0.28])
    short_ax = fig.add_axes([0.71, 0.50, 0.25, 0.43])
    notes_ax = fig.add_axes([0.71, 0.08, 0.25, 0.28])

    tray_w, tray_d, _ = SPEC["tray_outer"]
    tray_floor = SPEC["tray_floor"]
    wall_h = SPEC["tray_wall"]
    side_wall = SPEC["tray_side_wall"]
    divider = SPEC["tray_divider"]

    setup(top_ax, (-8, 102), (-10, 76), "Avtakbart spillerbrett — lukkede lommer")
    add_top_player_layout(top_ax, origin=(0, 0), scale=1.0, labels=True)
    dim_h(top_ax, 0, tray_w, -6.5, "92 mm utvendig")
    dim_v(top_ax, 0, tray_d, 97, "67 mm")
    dim_h(top_ax, side_wall, side_wall + PLAYER_CLEAR_W, 72, "21.5 mm fri spillerbredde")
    dim_v(top_ax, POCKET_Y, POCKET_TOP, -5, "51 mm lukket spillerlomme")
    dim_v(
        top_ax,
        SPEC["tray_front_wall"],
        SPEC["tray_front_wall"] + SPEC["front_name_zone_d"],
        -5,
        "11.5 mm navnefelt",
    )
    top_ax.text(
        47,
        -9,
        "Fast frontvegg, bakvegg og delte sidevegger holder alle spillergrupper på plass.",
        ha="center",
        va="top",
        fontsize=8,
    )

    setup(long_ax, (-8, 102), (-4, 29), "Lang side")
    long_ax.add_patch(
        Rectangle((0, 0), tray_w, tray_floor, facecolor="#c6a573", edgecolor="black", lw=0.9)
    )
    wall_positions = [
        0,
        side_wall + PLAYER_CLEAR_W,
        side_wall + 2 * PLAYER_CLEAR_W + divider,
        side_wall + 3 * PLAYER_CLEAR_W + 2 * divider,
        tray_w - side_wall,
    ]
    for index, wall_x in enumerate(wall_positions):
        wall_width = side_wall if index in (0, 4) else divider
        long_ax.add_patch(
            Rectangle(
                (wall_x, tray_floor),
                wall_width,
                wall_h,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )
    for index, color in enumerate(PLAYER_COLORS):
        player_x = side_wall + index * (PLAYER_CLEAR_W + divider)
        long_ax.add_patch(
            Rectangle(
                (player_x + LATERAL_CLEAR_SIDE, tray_floor),
                20.0,
                19.6,
                facecolor=color,
                alpha=0.45,
                edgecolor="black",
                lw=0.5,
            )
        )
        long_ax.text(
            player_x + PLAYER_CLEAR_W / 2,
            tray_floor + 9.8,
            PLAYER_NAMES[index],
            ha="center",
            va="center",
            fontsize=7,
        )
    dim_h(long_ax, 0, tray_w, -2.6, "92 mm")
    dim_v(long_ax, 0, tray_floor + wall_h, 97, "21.5 mm totalt")
    dim_v(long_ax, tray_floor, tray_floor + wall_h, -5, "20 mm vegghøyde")
    long_ax.text(
        46,
        26,
        "Sidevegg på begge sider av hver spillerlomme, men én fysisk vegg deles mellom naboer.",
        ha="center",
        va="top",
        fontsize=8,
    )

    setup(short_ax, (-6, 77), (-5, 34), "Kort side")
    short_ax.add_patch(
        Rectangle((0, 0), tray_d, tray_floor, facecolor="#c6a573", edgecolor="black", lw=0.9)
    )
    for rel_x, width, label in (
        (0, SPEC["tray_front_wall"], "front"),
        (SPEC["tray_front_wall"] + SPEC["front_name_zone_d"], SPEC["pocket_front_wall"], "front\nlomme"),
        (tray_d - SPEC["tray_back_wall"], SPEC["tray_back_wall"], "bak"),
    ):
        short_ax.add_patch(
            Rectangle(
                (rel_x, tray_floor),
                width,
                wall_h,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )
        short_ax.text(rel_x + width / 2, tray_floor + wall_h + 1.0, label, ha="center", fontsize=6)
    short_ax.add_patch(
        Rectangle(
            (SPEC["tray_front_wall"], tray_floor),
            SPEC["front_name_zone_d"],
            wall_h,
            facecolor="#ead8b5",
            edgecolor="black",
            lw=0.8,
        )
    )
    short_ax.text(
        SPEC["tray_front_wall"] + SPEC["front_name_zone_d"] / 2,
        tray_floor + wall_h / 2,
        "navn",
        ha="center",
        va="center",
        fontsize=7,
    )
    throne_x = POCKET_Y + 0.6
    short_ax.add_patch(
        Rectangle(
            (throne_x, tray_floor),
            SPEC["throne"][1],
            SPEC["throne"][2],
            facecolor=PLAYER_COLORS[1],
            alpha=0.65,
            edgecolor="black",
            lw=0.8,
        )
    )
    short_ax.text(throne_x + SPEC["throne"][1] / 2, tray_floor + 9.8, "trone", ha="center", va="center", fontsize=7)
    stack_x = throne_x + SPEC["throne"][1] + 0.4
    for column in range(3):
        for row in range(2):
            short_ax.add_patch(
                Rectangle(
                    (stack_x + column * 10, tray_floor + row * 10),
                    10,
                    10,
                    facecolor=PLAYER_COLORS[1],
                    alpha=0.45,
                    edgecolor="black",
                    lw=0.6,
                )
            )
    dim_h(short_ax, 0, tray_d, -2.5, "67 mm")
    dim_h(short_ax, SPEC["tray_front_wall"], SPEC["tray_front_wall"] + SPEC["front_name_zone_d"], 27, "11.5 navn")
    dim_h(short_ax, POCKET_Y, POCKET_TOP, 30, "51 spillerlomme")
    dim_v(short_ax, 0, tray_floor + wall_h, 72, "21.5 mm")
    short_ax.text(35.5, 33, "1.5 + 11.5 + 1.5 + 51 + 1.5 = 67 mm", ha="center", fontsize=8)

    notes_ax.axis("off")
    notes_ax.text(0.0, 1.0, "ANBEFALT EDIT", fontsize=11, weight="bold", va="top")
    notes_ax.text(
        0.0,
        0.90,
        "Én samlet tray med lukkede lommer:\n\n"
        "• fire spillerlommer\n"
        "• vegg foran og bak hver lomme\n"
        "• sidevegger delt mellom naboer\n"
        "• fast navnefelt foran\n"
        "• ingen løs spacer som lukking\n\n"
        "Spilleren legger brikkene tilbake i\n"
        "sin lomme, og hele trayet kan løftes\n"
        "rett tilbake i boksen.",
        fontsize=9,
        va="top",
        bbox=dict(boxstyle="round,pad=0.5", fill=False, lw=1.0),
    )

    fig.suptitle("ELEMENT WAR — lukket avtakbar spillertray", fontsize=17, y=0.98)
    save_dual(fig, "12_player_separator_top_and_sides")
    return

    tray_w = SPEC["tray_outer"][0]
    tray_d = SPEC["tray_outer"][1]
    tray_floor = SPEC["tray_floor"]
    wall_h = SPEC["tray_wall"]
    side_wall = SPEC["tray_side_wall"]
    divider = SPEC["tray_divider"]
    inner_w = SPEC["inner_w"]
    inner_d = SPEC["inner_d"]
    front_back_margin = (
        tray_d - inner_d
    ) / 2
    player_zone_d = SPEC["player_zone_d"]
    spacer_zone_d = SPEC["spacer_zone_d"]

    setup(
        top_ax,
        (-8, 102),
        (-10, 76),
        "Avtakbart spillerbrett — topp",
    )

    top_ax.add_patch(
        FancyBboxPatch(
            (0, 0),
            tray_w,
            tray_d,
            boxstyle="round,pad=0,rounding_size=1.2",
            fill=False,
            lw=2.0,
        )
    )

    inner_x = side_wall
    inner_y = front_back_margin
    player_y = inner_y + spacer_zone_d

    spacer_x = inner_x + (
        inner_w - SPEC["spacer"][0]
    ) / 2
    spacer_y = inner_y + (
        spacer_zone_d - SPEC["spacer"][1]
    ) / 2

    top_ax.add_patch(
        Rectangle(
            (spacer_x, spacer_y),
            SPEC["spacer"][0],
            SPEC["spacer"][1],
            facecolor="#ead8b5",
            edgecolor="black",
            lw=1.0,
        )
    )
    top_ax.text(
        spacer_x + SPEC["spacer"][0] / 2,
        spacer_y + SPEC["spacer"][1] / 2,
        "AVTAKBAR PAPPKLOSS / NAVNSTRIPE\n88.4 × 12.4",
        ha="center",
        va="center",
        fontsize=8,
    )

    top_ax.add_patch(
        Rectangle(
            (0, player_y),
            side_wall,
            player_zone_d,
            facecolor="#c6ad83",
            edgecolor="black",
            lw=0.7,
        )
    )
    top_ax.add_patch(
        Rectangle(
            (
                tray_w - side_wall,
                player_y,
            ),
            side_wall,
            player_zone_d,
            facecolor="#c6ad83",
            edgecolor="black",
            lw=0.7,
        )
    )

    for index, (name, color) in enumerate(
        zip(PLAYER_NAMES, PLAYER_COLORS)
    ):
        player_x = (
            inner_x
            + index
            * (
                PLAYER_CLEAR_W
                + divider
            )
        )

        top_ax.add_patch(
            Rectangle(
                (
                    player_x,
                    player_y,
                ),
                PLAYER_CLEAR_W,
                player_zone_d,
                fill=False,
                lw=1.0,
            )
        )
        top_ax.text(
            player_x + PLAYER_CLEAR_W / 2,
            player_y + player_zone_d + 1.2,
            name,
            ha="center",
            va="bottom",
            fontsize=8,
        )

        throne_x = player_x + (
            PLAYER_CLEAR_W
            - SPEC["throne"][0]
        ) / 2
        throne_y = player_y + 0.2

        top_ax.add_patch(
            FancyBboxPatch(
                (
                    throne_x,
                    throne_y,
                ),
                SPEC["throne"][0],
                SPEC["throne"][1],
                boxstyle="round,pad=0,rounding_size=0.6",
                facecolor=color,
                edgecolor="black",
                lw=0.9,
            )
        )

        health_draw = 12.0
        top_ax.add_patch(
            Rectangle(
                (
                    throne_x
                    + (
                        SPEC["throne"][0]
                        - health_draw
                    )
                    / 2,
                    throne_y
                    + (
                        SPEC["throne"][1]
                        - health_draw
                    )
                    / 2,
                ),
                health_draw,
                health_draw,
                facecolor="#f3efe6",
                edgecolor="black",
                lw=0.6,
            )
        )

        stack_x = player_x + (
            PLAYER_CLEAR_W
            - SPEC["farmer_stack"][0]
        ) / 2
        stack_y = player_y + 20.0
        for column in range(2):
            for row in range(3):
                top_ax.add_patch(
                    Rectangle(
                        (
                            stack_x
                            + column * 10,
                            stack_y
                            + row * 10,
                        ),
                        10,
                        10,
                        facecolor=color,
                        alpha=0.55,
                        edgecolor="black",
                        lw=0.6,
                    )
                )

        if index < 3:
            wall_x = (
                player_x
                + PLAYER_CLEAR_W
            )
            top_ax.add_patch(
                Rectangle(
                    (
                        wall_x,
                        player_y,
                    ),
                    divider,
                    player_zone_d,
                    facecolor="#c6ad83",
                    edgecolor="black",
                    lw=0.6,
                )
            )

    top_ax.add_patch(
        Rectangle(
            (
                0,
                tray_d - front_back_margin,
            ),
            tray_w,
            front_back_margin,
            facecolor="#c6ad83",
            edgecolor="black",
            lw=0.6,
            alpha=0.7,
        )
    )

    dim_h(
        top_ax,
        0,
        tray_w,
        -6.5,
        "92 mm utvendig",
    )
    dim_v(
        top_ax,
        0,
        tray_d,
        97,
        "67 mm",
    )
    dim_h(
        top_ax,
        inner_x,
        inner_x + PLAYER_CLEAR_W,
        72,
        "21.5 mm fri spillerbredde",
    )
    dim_v(
        top_ax,
        player_y,
        player_y + player_zone_d,
        -5,
        "50 mm spillerdybde",
    )
    dim_v(
        top_ax,
        inner_y,
        inner_y + spacer_zone_d,
        -5,
        "13 mm spacer-sone",
    )

    top_ax.text(
        47,
        -9,
        "Ytterveggene ved Gray og Green er del av samme tray.",
        ha="center",
        va="top",
        fontsize=8,
    )

    setup(
        long_ax,
        (-8, 102),
        (-4, 29),
        "Lang side",
    )
    long_ax.add_patch(
        Rectangle(
            (0, 0),
            tray_w,
            tray_floor,
            facecolor="#c6a573",
            edgecolor="black",
            lw=0.9,
        )
    )

    wall_positions = [
        0,
        side_wall + PLAYER_CLEAR_W,
        (
            side_wall
            + 2 * PLAYER_CLEAR_W
            + divider
        ),
        (
            side_wall
            + 3 * PLAYER_CLEAR_W
            + 2 * divider
        ),
        tray_w - side_wall,
    ]

    for index, wall_x in enumerate(
        wall_positions
    ):
        wall_width = (
            side_wall
            if index in (0, 4)
            else divider
        )
        long_ax.add_patch(
            Rectangle(
                (
                    wall_x,
                    tray_floor,
                ),
                wall_width,
                wall_h,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )

    for index, color in enumerate(
        PLAYER_COLORS
    ):
        player_x = (
            side_wall
            + index
            * (
                PLAYER_CLEAR_W
                + divider
            )
        )
        long_ax.add_patch(
            Rectangle(
                (
                    player_x
                    + LATERAL_CLEAR_SIDE,
                    tray_floor,
                ),
                20.0,
                19.6,
                facecolor=color,
                alpha=0.45,
                edgecolor="black",
                lw=0.5,
            )
        )
        long_ax.text(
            player_x + PLAYER_CLEAR_W / 2,
            tray_floor + 9.8,
            PLAYER_NAMES[index],
            ha="center",
            va="center",
            fontsize=7,
        )

    dim_h(
        long_ax,
        0,
        tray_w,
        -2.6,
        "92 mm",
    )
    dim_v(
        long_ax,
        0,
        tray_floor + wall_h,
        97,
        "21.5 mm totalt",
    )
    dim_v(
        long_ax,
        tray_floor,
        tray_floor + wall_h,
        -5,
        "20 mm vegghøyde",
    )

    long_ax.text(
        46,
        26,
        "1.5 mm gulv + 20 mm vegger. "
        "Hele brettet kan løftes ut ferdig fylt.",
        ha="center",
        va="top",
        fontsize=8,
    )

    setup(
        short_ax,
        (-6, 77),
        (-5, 34),
        "Kort side",
    )
    short_ax.add_patch(
        Rectangle(
            (0, 0),
            tray_d,
            tray_floor,
            facecolor="#c6a573",
            edgecolor="black",
            lw=0.9,
        )
    )

    start = front_back_margin
    short_ax.add_patch(
        Rectangle(
            (start, tray_floor),
            spacer_zone_d,
            SPEC["spacer"][2],
            facecolor="#ead8b5",
            edgecolor="black",
            lw=0.8,
        )
    )
    short_ax.text(
        start + spacer_zone_d / 2,
        tray_floor + SPEC["spacer"][2] / 2,
        "spacer",
        ha="center",
        va="center",
        fontsize=7,
    )

    throne_x = start + spacer_zone_d
    short_ax.add_patch(
        Rectangle(
            (throne_x, tray_floor),
            20,
            SPEC["throne"][2],
            facecolor=PLAYER_COLORS[1],
            alpha=0.65,
            edgecolor="black",
            lw=0.8,
        )
    )
    short_ax.text(
        throne_x + 10,
        tray_floor + SPEC["throne"][2] / 2,
        "trone",
        ha="center",
        va="center",
        fontsize=7,
    )

    stack_x = throne_x + 20
    for column in range(3):
        for row in range(2):
            short_ax.add_patch(
                Rectangle(
                    (
                        stack_x + column * 10,
                        tray_floor + row * 10,
                    ),
                    10,
                    10,
                    facecolor=PLAYER_COLORS[1],
                    alpha=0.45,
                    edgecolor="black",
                    lw=0.6,
                )
            )

    short_ax.add_patch(
        Rectangle(
            (
                tray_d
                - front_back_margin,
                tray_floor,
            ),
            front_back_margin,
            wall_h,
            facecolor="#c6ad83",
            edgecolor="black",
            lw=0.6,
        )
    )

    dim_h(
        short_ax,
        0,
        tray_d,
        -2.5,
        "67 mm",
    )
    dim_h(
        short_ax,
        start,
        start + 13,
        27,
        "13",
    )
    dim_h(
        short_ax,
        throne_x,
        throne_x + 20,
        27,
        "20",
    )
    dim_h(
        short_ax,
        stack_x,
        stack_x + 30,
        27,
        "30",
    )
    dim_v(
        short_ax,
        0,
        tray_floor + wall_h,
        72,
        "21.5 mm",
    )
    short_ax.text(
        35.5,
        32,
        "13 + 20 + 30 = 63 mm innvendig",
        ha="center",
        fontsize=8,
    )

    notes_ax.axis("off")
    notes_ax.text(
        0.0,
        1.0,
        "ANBEFALT LØSNING",
        fontsize=11,
        weight="bold",
        va="top",
    )
    notes_ax.text(
        0.0,
        0.90,
        "Én avtakbar tray med delte vegger:\n\n"
        "• venstre yttervegg før Gray\n"
        "• tre delte skillevegger\n"
        "• høyre yttervegg etter Green\n"
        "• bakvegg holder terningene\n"
        "• pappklossen lukker fronten\n\n"
        "Hele brettet kan tas ut, fylles og\n"
        "settes ferdig tilbake i boksen.",
        fontsize=9,
        va="top",
        bbox=dict(
            boxstyle="round,pad=0.5",
            fill=False,
            lw=1.0,
        ),
    )

    fig.suptitle(
        "ELEMENT WAR — avtakbar spillerseparator / tray",
        fontsize=17,
        y=0.98,
    )

    save_dual(
        fig,
        "12_player_separator_top_and_sides",
    )


# ============================================================
# 13 — Shared-wall tray versus four caddies
# ============================================================
def diagram_13():
    fig, axes = plt.subplots(2, 1, figsize=(14, 9))
    fig.suptitle(
        "ELEMENT WAR — lukket tray, bredde og dybde",
        fontsize=16,
        y=0.98,
    )

    ax = axes[0]
    setup(
        ax,
        (-3, 98),
        (-8, 26),
        "A. Bredde — fire lommer med delte sidevegger",
    )
    segments = [
        (SPEC["tray_side_wall"], "#c6ad83", ""),
        (PLAYER_CLEAR_W, PLAYER_COLORS[0], "Gray\nlomme"),
        (SPEC["tray_divider"], "#c6ad83", ""),
        (PLAYER_CLEAR_W, PLAYER_COLORS[1], "Blue\nlomme"),
        (SPEC["tray_divider"], "#c6ad83", ""),
        (PLAYER_CLEAR_W, PLAYER_COLORS[2], "Red\nlomme"),
        (SPEC["tray_divider"], "#c6ad83", ""),
        (PLAYER_CLEAR_W, PLAYER_COLORS[3], "Green\nlomme"),
        (SPEC["tray_side_wall"], "#c6ad83", ""),
    ]

    x = 0.0
    for width, color, label in segments:
        ax.add_patch(
            Rectangle((x, 0), width, 12, facecolor=color, edgecolor="black", lw=0.7)
        )
        if label:
            ax.text(x + width / 2, 6, label, ha="center", va="center", fontsize=8)
        x += width
    dim_h(ax, 0, SPEC["tray_outer"][0], -3, "92 mm totalt")
    ax.text(
        46,
        20,
        "1.5 + 21.5 + 1 + 21.5 + 1 + 21.5 + 1 + 21.5 + 1.5 = 92 mm",
        ha="center",
        fontsize=9,
    )
    ax.text(
        46,
        16,
        "Hver lomme har sidevegg på begge sider, men nabospillere deler materialet.",
        ha="center",
        fontsize=9,
    )

    ax = axes[1]
    setup(
        ax,
        (-3, 72),
        (-8, 30),
        "B. Dybde — lukket lomme og fast navnefelt",
    )
    depth_segments = [
        (SPEC["tray_front_wall"], "#c6ad83", ""),
        (SPEC["front_name_zone_d"], "#ead8b5", "navn\n11.5"),
        (SPEC["pocket_front_wall"], "#c6ad83", ""),
        (SPEC["player_zone_d"], "#c7d8ea", "spillerlomme\n51"),
        (SPEC["tray_back_wall"], "#c6ad83", ""),
    ]
    x = 0.0
    for width, color, label in depth_segments:
        ax.add_patch(
            Rectangle((x, 0), width, 12, facecolor=color, edgecolor="black", lw=0.7)
        )
        if label:
            ax.text(x + width / 2, 6, label, ha="center", va="center", fontsize=8)
        x += width
    dim_h(ax, 0, SPEC["tray_outer"][1], -3, "67 mm totalt")
    ax.text(
        33.5,
        25,
        "1.5 front + 11.5 navn + 1.5 frontvegg + 51 lomme + 1.5 bak = 67 mm",
        ha="center",
        fontsize=9,
    )
    ax.text(
        33.5,
        20,
        f"Lomme: 0.6 klaring + 19.6 trone + 0.4 gap + 30 terninger + {BACK_CLEAR:.1f} klaring = 51 mm",
        ha="center",
        fontsize=9,
    )

    save_dual(fig, "13_separator_wall_options_math")
    return

    ax = axes[0]
    setup(
        ax,
        (-3, 98),
        (-8, 26),
        "A. Én tray med delte vegger — anbefalt",
    )

    segments = [
        (
            SPEC["tray_side_wall"],
            "#c6ad83",
        ),
        (
            PLAYER_CLEAR_W,
            PLAYER_COLORS[0],
        ),
        (
            SPEC["tray_divider"],
            "#c6ad83",
        ),
        (
            PLAYER_CLEAR_W,
            PLAYER_COLORS[1],
        ),
        (
            SPEC["tray_divider"],
            "#c6ad83",
        ),
        (
            PLAYER_CLEAR_W,
            PLAYER_COLORS[2],
        ),
        (
            SPEC["tray_divider"],
            "#c6ad83",
        ),
        (
            PLAYER_CLEAR_W,
            PLAYER_COLORS[3],
        ),
        (
            SPEC["tray_side_wall"],
            "#c6ad83",
        ),
    ]

    x = 0.0
    for width, color in segments:
        ax.add_patch(
            Rectangle(
                (x, 0),
                width,
                12,
                facecolor=color,
                edgecolor="black",
                lw=0.7,
            )
        )
        x += width

    dim_h(
        ax,
        0,
        SPEC["tray_outer"][0],
        -3,
        "92 mm totalt",
    )
    ax.text(
        46,
        20,
        "1.5 + 21.5 + 1 + 21.5 + 1 + "
        "21.5 + 1 + 21.5 + 1.5 = 92 mm",
        ha="center",
        fontsize=9,
    )
    ax.text(
        46,
        16,
        "Komponentbredde 20 mm → "
        "0.75 mm klaring på hver side.",
        ha="center",
        fontsize=9,
    )

    ax = axes[1]
    setup(
        ax,
        (-3, 98),
        (-8, 30),
        "B. Fire separate spillerbokser — mindre optimalt",
    )

    x = 0.0
    for color in PLAYER_COLORS:
        ax.add_patch(
            Rectangle(
                (x, 0),
                0.8,
                12,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )
        ax.add_patch(
            Rectangle(
                (x + 0.8, 0),
                TARGET_CADDY_CLEAR_W,
                12,
                facecolor=color,
                edgecolor="black",
                lw=0.6,
            )
        )
        ax.add_patch(
            Rectangle(
                (
                    x
                    + 0.8
                    + TARGET_CADDY_CLEAR_W,
                    0,
                ),
                0.8,
                12,
                facecolor="#c6ad83",
                edgecolor="black",
                lw=0.6,
            )
        )
        x += TARGET_CADDY_CLEAR_W + 1.6

    dim_h(
        ax,
        0,
        FOUR_CADDIES_08,
        -3,
        f"{FOUR_CADDIES_08:.1f} mm med 0.8 mm vegger",
    )
    ax.axvline(
        SPEC["inner_w"],
        color="red",
        ls="--",
        lw=1.0,
    )
    ax.text(
        SPEC["inner_w"],
        15,
        "89 mm tilgjengelig",
        color="red",
        ha="center",
        fontsize=8,
    )
    ax.text(
        46,
        25,
        f"0.8 mm vegger: {FOUR_CADDIES_08:.1f} mm "
        f"→ {FOUR_CADDIES_08 - SPEC['inner_w']:.1f} mm for bredt.",
        ha="center",
        fontsize=9,
    )
    ax.text(
        46,
        20,
        f"1.0 mm vegger: {FOUR_CADDIES_10:.1f} mm "
        f"→ {FOUR_CADDIES_10 - SPEC['inner_w']:.1f} mm for bredt.",
        ha="center",
        fontsize=9,
    )
    ax.text(
        46,
        16,
        "Separate bokser dobler veggene mellom spillerne.",
        ha="center",
        fontsize=9,
    )

    save_dual(
        fig,
        "13_separator_wall_options_math",
    )


def main():
    write_measurements()
    diagram_01()
    diagram_02()
    diagram_03()
    diagram_04()
    diagram_05()
    diagram_06()
    diagram_07()
    diagram_08()
    diagram_09()
    diagram_10()
    diagram_11()
    diagram_12()
    diagram_13()

    print(f"Generated PNG files in: {PNG_DIR}")
    print(f"Generated SVG files in: {SVG_DIR}")
    print(f"Generated measurements: {DATA_DIR / 'measurement.txt'}")


if __name__ == "__main__":
    main()
