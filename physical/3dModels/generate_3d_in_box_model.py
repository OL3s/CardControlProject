#!/usr/bin/env python3
"""Generate the 1:1 millimetre FBX model for the updated in-box layout.

Run with Blender:
    blender --background --python generate_3d_in_box_model.py

The source dimensions mirror physical/mesurement/data/measurement.txt and
physical/mesurement/generate_measurement_package.py. Coordinates are authored
directly in millimetres: X = width, Y = depth, Z = height. The magnetic lid is
parented to hinge empties so it can be rotated open from the correct rear edge.
"""

from __future__ import annotations

from pathlib import Path
import math

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
OUTPUT_FBX = ROOT / "3DInBoxModel.fbx"


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
    bpy.context.scene.unit_settings.scale_length = 0.001
    bpy.context.scene.unit_settings.length_unit = "MILLIMETERS"


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
        (x1, y1, z1),
        (x2, y1, z1),
        (x2, y2, z1),
        (x1, y2, z1),
        (x1, y1, z2),
        (x2, y1, z2),
        (x2, y2, z2),
        (x1, y2, z2),
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
    obj.empty_display_size = 6
    obj.location = location
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    return obj


def parent_to(obj: bpy.types.Object, parent: bpy.types.Object) -> bpy.types.Object:
    obj.parent = parent
    return obj


def cylinder_y(
    name: str,
    center: tuple[float, float, float],
    diameter: float,
    depth: float,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=48,
        radius=diameter / 2,
        depth=depth,
        location=center,
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
        radius=diameter / 2,
        depth=depth,
        location=(0, 0, 0),
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.name = f"{name}_mesh"
    obj.data.materials.append(mat)
    obj.parent = parent
    obj.location = center
    obj.rotation_euler = (math.pi / 2, 0, 0)
    return obj


def material_name(obj: bpy.types.Object) -> str:
    if not obj.data.materials:
        return ""
    return obj.data.materials[0].name


def dimensions(obj: bpy.types.Object) -> tuple[float, float, float]:
    return tuple(round(value, 4) for value in obj.dimensions)


def bounds_dimensions(objects: list[bpy.types.Object]) -> tuple[float, float, float]:
    mins = [float("inf"), float("inf"), float("inf")]
    maxs = [float("-inf"), float("-inf"), float("-inf")]
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            for axis in range(3):
                mins[axis] = min(mins[axis], world[axis])
                maxs[axis] = max(maxs[axis], world[axis])
    return tuple(round(maxs[axis] - mins[axis], 4) for axis in range(3))


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

    objects["outer_box_bottom"] = parent_to(cube(
        "outer_box_bottom", (0, 0, 0), (base_w, base_d, board), mats["base"]
    ), groups["base"])
    objects["outer_box_wall_front"] = parent_to(cube(
        "outer_box_wall_front", (0, 0, board), (base_w, board, base_h), mats["base"]
    ), groups["base"])
    objects["outer_box_wall_back"] = parent_to(cube(
        "outer_box_wall_back", (0, base_d - board, board), (base_w, base_d, base_h), mats["base"]
    ), groups["base"])
    objects["outer_box_wall_left"] = parent_to(cube(
        "outer_box_wall_left", (0, board, board), (board, base_d - board, base_h), mats["base"]
    ), groups["base"])
    objects["outer_box_wall_right"] = parent_to(cube(
        "outer_box_wall_right", (base_w - board, board, board), (base_w, base_d - board, base_h), mats["base"]
    ), groups["base"])

    lid_h = SPEC["lid_panel"][2]
    lid_hinge = empty(
        "magnetic_lid_rear_hinge_pivot",
        (base_w / 2, base_d, base_h),
        groups["lid"],
    )
    objects["magnetic_lid_rear_hinge_pivot"] = lid_hinge
    objects["lid_top_panel_closed"] = cube_local(
        "lid_top_panel_closed",
        (-base_w / 2, -base_d, 0),
        (base_w / 2, 0, lid_h),
        mats["lid"],
        lid_hinge,
    )

    edge_t = SPEC["lid_outer_edge_thickness"]
    edge_drop = SPEC["lid_outer_edge_drop"]
    objects["lid_outer_edge_left"] = cube_local(
        "lid_outer_edge_left",
        (-base_w / 2 - edge_t, -base_d, -edge_drop),
        (-base_w / 2, 0, lid_h),
        mats["lid"],
        lid_hinge,
    )
    objects["lid_outer_edge_right"] = cube_local(
        "lid_outer_edge_right",
        (base_w / 2, -base_d, -edge_drop),
        (base_w / 2 + edge_t, 0, lid_h),
        mats["lid"],
        lid_hinge,
    )
    objects["lid_outer_edge_back"] = cube_local(
        "lid_outer_edge_back",
        (-base_w / 2, 0, -edge_drop),
        (base_w / 2, edge_t, lid_h),
        mats["lid"],
        lid_hinge,
    )

    flap_hinge = empty(
        "magnetic_front_flap_hinge_pivot",
        (0, -base_d, 0),
        lid_hinge,
    )
    objects["magnetic_front_flap_hinge_pivot"] = flap_hinge
    flap_w, flap_h, flap_t = SPEC["front_flap"]
    objects["magnetic_front_flap_closed"] = cube_local(
        "magnetic_front_flap_closed",
        (-flap_w / 2, -flap_t, -flap_h),
        (flap_w / 2, 0, 0),
        mats["lid"],
        flap_hinge,
    )

    magnet_x_positions = (
        -flap_w / 2 + SPEC["magnet_edge_offset"],
        flap_w / 2 - SPEC["magnet_edge_offset"],
    )
    magnet_y = -flap_t / 2
    magnet_z = -flap_h + SPEC["magnet_vertical_center"]
    for index, magnet_x in enumerate(magnet_x_positions, start=1):
        objects[f"front_flap_magnet_{index}"] = cylinder_y_local(
            f"front_flap_magnet_{index}",
            (magnet_x, magnet_y, magnet_z),
            SPEC["magnet_diameter"],
            SPEC["magnet_thickness"],
            mats["magnet"],
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

    objects["tray_floor"] = parent_to(cube(
        "tray_floor", (tray_x, tray_y, tray_z), (tray_x + tray_w, tray_y + tray_d, wall_z), mats["tray"]
    ), groups["tray_structure"])
    objects["tray_wall_front"] = parent_to(cube(
        "tray_wall_front",
        (tray_x, tray_y, wall_z),
        (tray_x + tray_w, tray_y + SPEC["tray_front_wall"], wall_top),
        mats["tray"],
    ), groups["tray_structure"])
    objects["tray_wall_back"] = parent_to(cube(
        "tray_wall_back",
        (tray_x, tray_y + tray_d - SPEC["tray_back_wall"], wall_z),
        (tray_x + tray_w, tray_y + tray_d, wall_top),
        mats["tray"],
    ), groups["tray_structure"])
    objects["tray_wall_left"] = parent_to(cube(
        "tray_wall_left",
        (tray_x, tray_y, wall_z),
        (tray_x + SPEC["tray_side_wall"], tray_y + tray_d, wall_top),
        mats["tray"],
    ), groups["tray_structure"])
    objects["tray_wall_right"] = parent_to(cube(
        "tray_wall_right",
        (tray_x + tray_w - SPEC["tray_side_wall"], tray_y, wall_z),
        (tray_x + tray_w, tray_y + tray_d, wall_top),
        mats["tray"],
    ), groups["tray_structure"])

    inner_x = tray_x + SPEC["tray_side_wall"]
    name_y = tray_y + SPEC["tray_front_wall"]
    name_h = SPEC["front_name_zone_d"]
    pocket_wall_y = name_y + name_h
    pocket_y = pocket_wall_y + SPEC["pocket_front_wall"]
    pocket_top_y = pocket_y + SPEC["player_zone_d"]

    objects["front_name_field_block"] = parent_to(cube(
        "front_name_field_block",
        (inner_x, name_y, wall_z),
        (inner_x + SPEC["inner_w"], name_y + name_h, wall_top),
        mats["tray_light"],
    ), groups["tray_structure"])
    objects["pocket_front_wall"] = parent_to(cube(
        "pocket_front_wall",
        (inner_x, pocket_wall_y, wall_z),
        (inner_x + SPEC["inner_w"], pocket_wall_y + SPEC["pocket_front_wall"], wall_top),
        mats["tray"],
    ), groups["tray_structure"])

    player_w = (SPEC["inner_w"] - 3 * SPEC["tray_divider"]) / 4
    divider_positions = [
        inner_x + player_w,
        inner_x + 2 * player_w + SPEC["tray_divider"],
        inner_x + 3 * player_w + 2 * SPEC["tray_divider"],
    ]
    for index, divider_x in enumerate(divider_positions, start=1):
        objects[f"tray_divider_{index}"] = parent_to(cube(
            f"tray_divider_{index}",
            (divider_x, pocket_y, wall_z),
            (divider_x + SPEC["tray_divider"], pocket_top_y, wall_top),
            mats["tray"],
        ), groups["tray_structure"])

    for player_index, player in enumerate(PLAYER_NAMES):
        pocket_x = inner_x + player_index * (player_w + SPEC["tray_divider"])

        throne_x = pocket_x + (player_w - SPEC["throne"][0]) / 2
        throne_y = pocket_y + 0.6
        throne_z = wall_z
        throne_w, throne_d, throne_h = SPEC["throne"]
        wall_thickness = (throne_w - SPEC["health_die"][0]) / 2
        base_plate_h = 1.5

        player_group = player_groups[player]

        objects[f"{player}_throne_base"] = parent_to(cube(
            f"{player}_throne_base",
            (throne_x, throne_y, throne_z),
            (throne_x + throne_w, throne_y + throne_d, throne_z + base_plate_h),
            mats[player],
        ), player_group)
        objects[f"{player}_throne_left"] = parent_to(cube(
            f"{player}_throne_left",
            (throne_x, throne_y, throne_z + base_plate_h),
            (throne_x + wall_thickness, throne_y + throne_d, throne_z + throne_h),
            mats[player],
        ), player_group)
        objects[f"{player}_throne_right"] = parent_to(cube(
            f"{player}_throne_right",
            (throne_x + throne_w - wall_thickness, throne_y, throne_z + base_plate_h),
            (throne_x + throne_w, throne_y + throne_d, throne_z + throne_h),
            mats[player],
        ), player_group)
        objects[f"{player}_throne_back"] = parent_to(cube(
            f"{player}_throne_back",
            (throne_x, throne_y + throne_d - wall_thickness, throne_z + base_plate_h),
            (throne_x + throne_w, throne_y + throne_d, throne_z + throne_h),
            mats[player],
        ), player_group)

        health_x = throne_x + wall_thickness
        health_y = throne_y + wall_thickness
        health_z = throne_z + base_plate_h
        die = SPEC["health_die"][0]
        objects[f"{player}_health_die"] = parent_to(cube(
            f"{player}_health_die",
            (health_x, health_y, health_z),
            (health_x + die, health_y + die, health_z + die),
            mats["health"],
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
                    objects[f"{player}_farmer_die_{die_number:02d}"] = parent_to(cube(
                        f"{player}_farmer_die_{die_number:02d}",
                        (x1, y1, z1),
                        (x1 + farmer, y1 + farmer, z1 + farmer),
                        mats[player],
                    ), player_group)
                    die_number += 1

    return objects


def validate(objects: dict[str, bpy.types.Object]) -> None:
    base_parts = [
        objects["outer_box_bottom"],
        objects["outer_box_wall_front"],
        objects["outer_box_wall_back"],
        objects["outer_box_wall_left"],
        objects["outer_box_wall_right"],
    ]
    tray_parts = [
        objects["tray_floor"],
        objects["tray_wall_front"],
        objects["tray_wall_back"],
        objects["tray_wall_left"],
        objects["tray_wall_right"],
        objects["front_name_field_block"],
        objects["pocket_front_wall"],
        objects["tray_divider_1"],
        objects["tray_divider_2"],
        objects["tray_divider_3"],
    ]
    checks = {
        "base_outer": (bounds_dimensions(base_parts), SPEC["base_outer"]),
        "closed_total": (bounds_dimensions(base_parts + [objects["lid_top_panel_closed"]]), SPEC["closed_total"]),
        "lid_top_panel": (dimensions(objects["lid_top_panel_closed"]), SPEC["lid_panel"]),
        "magnetic_front_flap_closed": (dimensions(objects["magnetic_front_flap_closed"]), (97.0, 2.0, 20.0)),
        "cards_52_stack": (dimensions(objects["cards_52_stack"]), SPEC["cards"]),
        "folded_rule_sheet": (dimensions(objects["folded_rule_sheet"]), SPEC["rules"]),
        "tray_outer": (bounds_dimensions(tray_parts), SPEC["tray_outer"]),
        "front_name_field_block": (dimensions(objects["front_name_field_block"]), (89.0, 11.5, 20.0)),
        "health_die": (dimensions(objects["gray_health_die"]), SPEC["health_die"]),
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
        tuple(round(value, 4) for value in lid_hinge.location),
        "mm",
    )
    print(
        "validated front flap hinge local pivot:",
        tuple(round(value, 4) for value in flap_hinge.location),
        "mm from rear hinge",
    )
    for name in ("lid_top_panel_closed", "magnetic_front_flap_closed", "front_name_field_block", "tray_floor"):
        print(f"validated material {name}: {material_name(objects[name])}")


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
