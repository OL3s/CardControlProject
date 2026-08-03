# Card Artwork

This directory is the runtime source for the 52 card-front illustrations described in
[`shared/docs/card-artwork.md`](../../../../shared/docs/card-artwork.md).

- Put the 32 monster masters in `monsters/`.
- Put the 20 terrain masters in `terrain/`.
- Use PNG files with the exact canonical card ID, without the generated `default_` prefix.
- Use the exact `59:89` aspect ratio; the preferred master size is `2360 x 3560` pixels.
- Do not paint frames, panels, icons, text, or tier diamonds into the illustration.

Examples:

```text
monsters/monster_grass_2_a.png
terrain/terrain_water_1_c.png
```

The default deck already points to these paths. Until a master exists, its preview uses
the missing-image diagnostic.
