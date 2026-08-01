#!/usr/bin/env python3
"""Generate the 1:1 millimetre FBX model for the updated in-box layout.

Run with Blender:
    blender --background --python generate_3d_in_box_model.py

The source dimensions mirror physical/mesurement/data/measurement.txt and
physical/mesurement/generate_measurement_package.py. Dimensions are authored in
millimetres, then exported as real-world metres so Blender imports 97 mm as
0.097 m instead of 97 m. X = width, Y = depth, Z = height. The magnetic lid is
parented to hinge empties so it can be rotated open from the correct rear edge.
"""

from __future__ import annotations

from pathlib import Path
import math

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
OUTPUT_FBX = ROOT / "3DInBoxModel.fbx"
MM_TO_M = 0.001
M_TO_MM = 1000.0


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
    "front_name_field": (89.0, 11.5),
    "throne": (19.6, 19.6, 19.6),
    "health_die": (16.0, 16.0, 16.0),
    "farmer_die": (10.0, 10.0, 10.0),
    "farmer_stack": (20.0, 30.0, 20.0),
    "base_outer": (97.0, 72.0, 43.0),
    "base_inner": (93.0, 68.0, 41.0),
    "board": 2.0,
    "lid_panel": (97.0, 72.0, 2.0),
    "lid_outer_edge_thickness": 2.0,
    "lid_outer_edge_drop": 2.0,
    "front_flap": (97.0, 20.0, 2.0),
    "magnet_diameter": 8.0,
    "magnet_thickness": 1.0,
    "magnet_edge_offset": 18.0,
    "magnet_vertical_center": 10.0,
    "closed_total": (97.0, 72.0, 45.0),
    "compression_liner": 1.0,
}

PLAYER_NAMES = ["gray", "blue", "red", "green"]
PLAYER_COLORS = {
    "gray": (0.64, 0.64, 0.64, 1.0),
    "blue": (0.42, 0.62, 0.82, 1.0),
    "red": (0.78, 0.37, 0.31, 1.0),
    "green": (0.58, 0.66, 0.43, 1.0),
}


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene.unit_settings.length_unit = "MILLIMETERS"


def mm(value: float) -> float:
    return value * MM_TO_M


def point_mm(values: tuple[float, float, float]) -> tuple[float, float, float]:
    return tuple(mm(value) for value in values)


def material(name: str, color: tuple[float, float, float, float]) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled:
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.7
    return mat


def cube(
    name: str,
    min_corner: tuple[float, float, float],
    max_corner: tuple[float, float, float],
    mat: bpy.types.Material,
) -> bpy.types.Object:
    x1, y1, z1 = min_corner
    x2, y2, z2 = max_corner
    verts = [
        point_mm((x1, y1, z1)),
        point_mm((x2, y1, z1)),
        point_mm((x2, y2, z1)),
        point_mm((x1, y2, z1)),
        point_mm((x1, y1, z2)),
        point_mm((x2, y1, z2)),
        point_mm((x2, y2, z2)),
        point_mm((x1, y2, z2)),
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(f"{name}_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    return obj


def beveled_cube(
    name: str,
    min_corner: tuple[float, float, float],
    max_corner: tuple[float, float, float],
    mat: bpy.types.Material,
    bevel: float = 0.8,
    segments: int = 5,
) -> bpy.types.Object:
    x1, y1, z1 = min_corner
    x2, y2, z2 = max_corner
    bpy.ops.mesh.primitive_cube_add(
        size=1,
        location=point_mm(((x1 + x2) / 2, (y1 + y2) / 2, (z1 + z2) / 2)),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.dimensions = point_mm((x2 - x1, y2 - y1, z2 - z1))
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    bevel_modifier = obj.modifiers.new(f"{name}_edge_rounding", "BEVEL")
    bevel_modifier.width = mm(bevel)
    bevel_modifier.segments = segments
    bevel_modifier.affect = "EDGES"
    bevel_modifier.profile = 0.5
    bpy.ops.object.shade_smooth()
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=bevel_modifier.name)
    obj.select_set(False)
    return obj


def cube_local(
    name: str,
    min_corner: tuple[float, float, float],
    max_corner: tuple[float, float, float],
    mat: bpy.types.Material,
    parent: bpy.types.Object,
) -> bpy.types.Object:
    obj = cube(name, min_corner, max_corner, mat)
    obj.parent = parent
    return obj


def empty(
    name: str,
    location: tuple[float, float, float],
    parent: bpy.types.Object | None = None,
) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = mm(6)
    obj.location = point_mm(location)
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    return obj


def parent_to(obj: bpy.types.Object, parent: bpy.types.Object) -> bpy.types.Object:
    obj.parent = parent
    return obj


def joined_mesh(
    name: str,
    parts: list[bpy.types.Object],
    parent: bpy.types.Object | None = None,
) -> bpy.types.Object:
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.parent = parent
    return obj


def joined_throne(
    name: str,
    min_corner: tuple[float, float, float],
    size: tuple[float, float, float],
    player_mat: bpy.types.Material,
    parent: bpy.types.Object,
) -> bpy.types.Object:
    throne_x, throne_y, throne_z = min_corner
    throne_w, throne_d, throne_h = size
    health = SPEC["health_die"][0]
    wall_thickness = (throne_w - health) / 2
    base_plate_h = 1.5

    parts = [
        cube(
            f"{name}_base_part",
            (throne_x, throne_y, throne_z),
            (throne_x + throne_w, throne_y + throne_d, throne_z + base_plate_h),
            player_mat,
        ),
        cube(
            f"{name}_left_part",
            (throne_x, throne_y, throne_z + base_plate_h),
            (throne_x + wall_thickness, throne_y + throne_d, throne_z + throne_h),
            player_mat,
        ),
        cube(
            f"{name}_right_part",
            (throne_x + throne_w - wall_thickness, throne_y, throne_z + base_plate_h),
            (throne_x + throne_w, throne_y + throne_d, throne_z + throne_h),
            player_mat,
        ),
        cube(
            f"{name}_back_part",
            (throne_x, throne_y + throne_d - wall_thickness, throne_z + base_plate_h),
            (throne_x + throne_w, throne_y + throne_d, throne_z + throne_h),
            player_mat,
        ),
    ]

    return joined_mesh(name, parts, parent)


def cylinder_y(
    name: str,
    center: tuple[float, float, float],
    diameter: float,
    depth: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=48,
        radius=mm(diameter / 2),
        depth=mm(depth),
        location=point_mm(center),
        rotation=(math.pi / 2, 0, 0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return obj


def cylinder_y_local(
    name: str,
    center: tuple[float, float, float],
    diameter: float,
    depth: float,
    mat: bpy.types.Material,
    parent: bpy.types.Object,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=48,
        radius=mm(diameter / 2),
        depth=mm(depth),
        location=(0, 0, 0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.location = point_mm(center)
    obj.rotation_euler = (math.pi / 2, 0, 0)
    return obj


def material_name(obj: bpy.types.Object) -> str:
    if not obj.data.materials:
        return ""
    return obj.data.materials[0].name


def material_names(obj: bpy.types.Object) -> list[str]:
    return [mat.name for mat in obj.data.materials]


def dimensions(obj: bpy.types.Object) -> tuple[float, float, float]:
    return tuple(round(value * M_TO_MM, 4) for value in obj.dimensions)


def bounds_dimensions(objects: list[bpy.types.Object]) -> tuple[float, float, float]:
    mins = [float("inf"), float("inf"), float("inf")]
    maxs = [float("-inf"), float("-inf"), float("-inf")]
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            for axis in range(3):
                mins[axis] = min(mins[axis], world[axis])
                maxs[axis] = max(maxs[axis], world[axis])
    return tuple(round((maxs[axis] - mins[axis]) * M_TO_MM, 4) for axis in range(3))


def build_model() -> dict[str, bpy.types.Object]:
    clear_scene()

    mats = {
        "base": material("rigid_board_dark", (0.05, 0.05, 0.05, 1.0)),
        "lid": material("lid_panel_black", (0.02, 0.02, 0.02, 1.0)),
        "cards": material("card_stack_light_gray", (0.78, 0.78, 0.78, 1.0)),
        "rules": material("folded_rule_sheet_warm", (0.92, 0.82, 0.60, 1.0)),
        "tray": material("tray_wood", (0.70, 0.55, 0.32, 1.0)),
        "tray_light": material("front_name_field", (0.88, 0.77, 0.53, 1.0)),
        "health": material("health_die_ivory", (0.94, 0.91, 0.84, 1.0)),
        "liner": material("optional_compression_liner", (0.90, 0.90, 0.90, 1.0)),
        "magnet": material("magnet_dark_metal", (0.02, 0.02, 0.025, 1.0)),
    }
    for player, color in PLAYER_COLORS.items():
        mats[player] = material(f"player_{player}", color)

    objects: dict[str, bpy.types.Object] = {}

    base_w, base_d, base_h = SPEC["base_outer"]
    inner_w, inner_d, inner_h = SPEC["base_inner"]
    board = SPEC["board"]

    root = empty("Element_War_Complete", (0, 0, 0))
    groups = {
        "root": root,
        "base": empty("Outer_Box_Base", (0, 0, 0), root),
        "lid": empty("Magnetic_Lid_Assembly", (0, 0, 0), root),
        "stack": empty("Internal_Stack", (0, 0, 0), root),
    }
    groups["cards"] = empty("Cards", (0, 0, 0), groups["stack"])
    groups["rules"] = empty("Rules", (0, 0, 0), groups["stack"])
    groups["tray"] = empty("Tray_Assembly", (0, 0, 0), groups["stack"])
    groups["tray_structure"] = empty("Tray_Structure", (0, 0, 0), groups["tray"])
    groups["players"] = empty("Players", (0, 0, 0), groups["tray"])
    player_groups = {
        player: empty(
            f"Player_{index}_{player.title()}",
            (0, 0, 0),
            groups["players"],
        )
        for index, player in enumerate(PLAYER_NAMES, start=1)
    }
    objects.update(groups)
    objects.update({f"group_{player}": group for player, group in player_groups.items()})

    objects["outer_box_base"] = joined_mesh(
        "outer_box_base",
        [
            cube("outer_box_bottom_part", (0, 0, 0), (base_w, base_d, board), mats["base"]),
            cube("outer_box_wall_front_part", (0, 0, board), (base_w, board, base_h), mats["base"]),
            cube("outer_box_wall_back_part", (0, base_d - board, board), (base_w, base_d, base_h), mats["base"]),
            cube("outer_box_wall_left_part", (0, board, board), (board, base_d - board, base_h), mats["base"]),
            cube("outer_box_wall_right_part", (base_w - board, board, board), (base_w, base_d - board, base_h), mats["base"]),
        ],
        groups["base"],
    )

    lid_h = SPEC["lid_panel"][2]
    lid_hinge = empty(
        "magnetic_lid_rear_hinge_pivot",
        (base_w / 2, base_d, base_h),
        groups["lid"],
    )
    objects["magnetic_lid_rear_hinge_pivot"] = lid_hinge
    edge_t = SPEC["lid_outer_edge_thickness"]
    edge_drop = SPEC["lid_outer_edge_drop"]
    objects["lid_top_assembly"] = joined_mesh(
        "lid_top_assembly",
        [
            cube_local(
                "lid_top_panel_part",
                (-base_w / 2, -base_d, 0),
                (base_w / 2, 0, lid_h),
                mats["lid"],
                lid_hinge,
            ),
            cube_local(
                "lid_outer_edge_left_part",
                (-base_w / 2 - edge_t, -base_d, -edge_drop),
                (-base_w / 2, 0, lid_h),
                mats["lid"],
                lid_hinge,
            ),
            cube_local(
                "lid_outer_edge_right_part",
                (base_w / 2, -base_d, -edge_drop),
                (base_w / 2 + edge_t, 0, lid_h),
                mats["lid"],
                lid_hinge,
            ),
            cube_local(
                "lid_outer_edge_back_part",
                (-base_w / 2, 0, -edge_drop),
                (base_w / 2, edge_t, lid_h),
                mats["lid"],
                lid_hinge,
            ),
        ],
        lid_hinge,
    )

    flap_hinge = empty(
        "magnetic_front_flap_hinge_pivot",
        (0, -base_d, 0),
        lid_hinge,
    )
    objects["magnetic_front_flap_hinge_pivot"] = flap_hinge
    flap_w, flap_h, flap_t = SPEC["front_flap"]
    front_flap_parts = [
        cube_local(
            "magnetic_front_flap_part",
            (-flap_w / 2, -flap_t, -flap_h),
            (flap_w / 2, 0, 0),
            mats["lid"],
            flap_hinge,
        )
    ]

    magnet_x_positions = (
        -flap_w / 2 + SPEC["magnet_edge_offset"],
        flap_w / 2 - SPEC["magnet_edge_offset"],
    )
    magnet_y = -flap_t / 2
    magnet_z = -flap_h + SPEC["magnet_vertical_center"]
    for index, magnet_x in enumerate(magnet_x_positions, start=1):
        front_flap_parts.append(cylinder_y_local(
            f"front_flap_magnet_{index}",
            (magnet_x, magnet_y, magnet_z),
            SPEC["magnet_diameter"],
            SPEC["magnet_thickness"],
            mats["magnet"],
            flap_hinge,
        ))
    objects["magnetic_front_flap_assembly"] = joined_mesh(
        "magnetic_front_flap_assembly",
        front_flap_parts,
        flap_hinge,
    )

    liner_h = SPEC["compression_liner"]
    objects["compression_liner_optional"] = parent_to(cube(
        "compression_liner_optional",
        (board, board, board + inner_h - liner_h),
        (board + inner_w, board + inner_d, board + inner_h),
        mats["liner"],
    ), groups["stack"])

    card_w, card_d, card_h = SPEC["cards"]
    card_x = board + (inner_w - card_w) / 2
    card_y = board + (inner_d - card_d) / 2
    objects["cards_52_stack"] = parent_to(cube(
        "cards_52_stack",
        (card_x, card_y, board),
        (card_x + card_w, card_y + card_d, board + card_h),
        mats["cards"],
    ), groups["cards"])

    rule_w, rule_d, rule_h = SPEC["rules"]
    rule_x = board + (inner_w - rule_w) / 2
    rule_y = board + (inner_d - rule_d) / 2
    rule_z = board + card_h
    objects["folded_rule_sheet"] = parent_to(cube(
        "folded_rule_sheet",
        (rule_x, rule_y, rule_z),
        (rule_x + rule_w, rule_y + rule_d, rule_z + rule_h),
        mats["rules"],
    ), groups["rules"])

    tray_w, tray_d, tray_h = SPEC["tray_outer"]
    tray_x = board + (inner_w - tray_w) / 2
    tray_y = board + (inner_d - tray_d) / 2
    tray_z = rule_z + rule_h
    floor_h = SPEC["tray_floor"]
    wall_top = tray_z + tray_h
    wall_z = tray_z + floor_h

    inner_x = tray_x + SPEC["tray_side_wall"]
    name_y = tray_y + SPEC["tray_front_wall"]
    name_h = SPEC["front_name_zone_d"]
    pocket_wall_y = name_y + name_h
    pocket_y = pocket_wall_y + SPEC["pocket_front_wall"]
    pocket_top_y = pocket_y + SPEC["player_zone_d"]

    player_w = (SPEC["inner_w"] - 3 * SPEC["tray_divider"]) / 4
    tray_parts = [
        cube("tray_floor_part", (tray_x, tray_y, tray_z), (tray_x + tray_w, tray_y + tray_d, wall_z), mats["tray"]),
        cube(
            "tray_wall_front_part",
            (tray_x, tray_y, wall_z),
            (tray_x + tray_w, tray_y + SPEC["tray_front_wall"], wall_top),
            mats["tray"],
        ),
        cube(
            "tray_wall_back_part",
            (tray_x, tray_y + tray_d - SPEC["tray_back_wall"], wall_z),
            (tray_x + tray_w, tray_y + tray_d, wall_top),
            mats["tray"],
        ),
        cube(
            "tray_wall_left_part",
            (tray_x, tray_y, wall_z),
            (tray_x + SPEC["tray_side_wall"], tray_y + tray_d, wall_top),
            mats["tray"],
        ),
        cube(
            "tray_wall_right_part",
            (tray_x + tray_w - SPEC["tray_side_wall"], tray_y, wall_z),
            (tray_x + tray_w, tray_y + tray_d, wall_top),
            mats["tray"],
        ),
        cube(
            "front_name_field_block_part",
            (inner_x, name_y, wall_z),
            (inner_x + SPEC["inner_w"], name_y + name_h, wall_top),
            mats["tray_light"],
        ),
        cube(
            "pocket_front_wall_part",
            (inner_x, pocket_wall_y, wall_z),
            (inner_x + SPEC["inner_w"], pocket_wall_y + SPEC["pocket_front_wall"], wall_top),
            mats["tray"],
        ),
    ]
    divider_positions = [
        inner_x + player_w,
        inner_x + 2 * player_w + SPEC["tray_divider"],
        inner_x + 3 * player_w + 2 * SPEC["tray_divider"],
    ]
    for index, divider_x in enumerate(divider_positions, start=1):
        tray_parts.append(cube(
            f"tray_divider_{index}_part",
            (divider_x, pocket_y, wall_z),
            (divider_x + SPEC["tray_divider"], pocket_top_y, wall_top),
            mats["tray"],
        ))
    objects["player_tray"] = joined_mesh("player_tray", tray_parts, groups["tray_structure"])

    for player_index, player in enumerate(PLAYER_NAMES):
        pocket_x = inner_x + player_index * (player_w + SPEC["tray_divider"])

        throne_x = pocket_x + (player_w - SPEC["throne"][0]) / 2
        throne_y = pocket_y + 0.6
        throne_z = wall_z
        throne_w, throne_d, throne_h = SPEC["throne"]

        player_group = player_groups[player]

        objects[f"{player}_throne"] = joined_throne(
            f"{player}_throne",
            (throne_x, throne_y, throne_z),
            (throne_w, throne_d, throne_h),
            mats[player],
            player_group,
        )

        health = SPEC["health_die"][0]
        wall_thickness = (throne_w - health) / 2
        base_plate_h = 1.5
        health_x = throne_x + wall_thickness
        health_y = throne_y + wall_thickness
        health_z = throne_z + base_plate_h
        objects[f"{player}_health_die"] = parent_to(beveled_cube(
            f"{player}_health_die",
            (health_x, health_y, health_z),
            (health_x + health, health_y + health, health_z + health),
            mats["health"],
            bevel=0.9,
            segments=6,
        ), player_group)

        stack_x = pocket_x + (player_w - SPEC["farmer_stack"][0]) / 2
        stack_y = throne_y + SPEC["throne"][1] + 0.4
        farmer = SPEC["farmer_die"][0]
        die_number = 1
        for layer in range(2):
            for row in range(3):
                for column in range(2):
                    x1 = stack_x + column * farmer
                    y1 = stack_y + row * farmer
                    z1 = wall_z + layer * farmer
                    objects[f"{player}_farmer_die_{die_number:02d}"] = parent_to(beveled_cube(
                        f"{player}_farmer_die_{die_number:02d}",
                        (x1, y1, z1),
                        (x1 + farmer, y1 + farmer, z1 + farmer),
                        mats[player],
                        bevel=0.6,
                        segments=5,
                    ), player_group)
                    die_number += 1

    return objects


def validate(objects: dict[str, bpy.types.Object]) -> None:
    checks = {
        "base_outer": (dimensions(objects["outer_box_base"]), SPEC["base_outer"]),
        "closed_total": (bounds_dimensions([objects["outer_box_base"], objects["lid_top_assembly"]]), (101.0, 74.0, 45.0)),
        "lid_top_assembly": (dimensions(objects["lid_top_assembly"]), (101.0, 74.0, 4.0)),
        "magnetic_front_flap_assembly": (dimensions(objects["magnetic_front_flap_assembly"]), (97.0, 2.0, 20.0)),
        "cards_52_stack": (dimensions(objects["cards_52_stack"]), SPEC["cards"]),
        "folded_rule_sheet": (dimensions(objects["folded_rule_sheet"]), SPEC["rules"]),
        "tray_outer": (dimensions(objects["player_tray"]), SPEC["tray_outer"]),
        "gray_throne": (dimensions(objects["gray_throne"]), SPEC["throne"]),
        "gray_health_die": (dimensions(objects["gray_health_die"]), SPEC["health_die"]),
        "farmer_die": (dimensions(objects["gray_farmer_die_01"]), SPEC["farmer_die"]),
    }
    for label, (actual, expected) in checks.items():
        expected_tuple = tuple(round(value, 4) for value in expected)
        if actual != expected_tuple:
            raise RuntimeError(f"{label} dimension mismatch: {actual} != {expected_tuple}")
        print(f"validated {label}: {actual} mm")

    lid_hinge = objects["magnetic_lid_rear_hinge_pivot"]
    flap_hinge = objects["magnetic_front_flap_hinge_pivot"]
    print(
        "validated magnetic lid hinge pivot:",
        tuple(round(value * M_TO_MM, 4) for value in lid_hinge.location),
        "mm",
    )
    print(
        "validated front flap hinge local pivot:",
        tuple(round(value * M_TO_MM, 4) for value in flap_hinge.location),
        "mm from rear hinge",
    )
    for name in ("lid_top_assembly", "magnetic_front_flap_assembly", "player_tray", "outer_box_base"):
        print(f"validated materials {name}: {material_names(objects[name])}")
    if "front_name_field" not in material_names(objects["player_tray"]):
        raise RuntimeError("player_tray is missing front_name_field material slot")
    if "magnet_dark_metal" not in material_names(objects["magnetic_front_flap_assembly"]):
        raise RuntimeError("magnetic_front_flap_assembly is missing magnet material slot")
    throne_materials = material_names(objects["gray_throne"])
    if throne_materials != ["player_gray"]:
        raise RuntimeError(f"gray throne material mismatch: {throne_materials}")
    if material_name(objects["gray_health_die"]) != "health_die_ivory":
        raise RuntimeError(f"gray health die material mismatch: {material_name(objects['gray_health_die'])}")
    print(f"validated separate throne materials: {throne_materials}")
    print(f"validated separate health die material: {material_name(objects['gray_health_die'])}")

    for name in ("gray_farmer_die_01", "gray_health_die", "gray_throne"):
        vertex_count = len(objects[name].data.vertices)
        if vertex_count <= 8:
            raise RuntimeError(f"{name} was not bevelled/joined as expected")
        print(f"validated mesh detail {name}: {vertex_count} vertices")


def export_fbx() -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.scene.objects:
        if obj.type in {"MESH", "EMPTY"}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_FBX),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=False,
        global_scale=1.0,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="AUTO",
    )
    print(f"exported {OUTPUT_FBX}")


if __name__ == "__main__":
    built_objects = build_model()
    validate(built_objects)
    export_fbx()
