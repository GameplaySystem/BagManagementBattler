SOLDIER SPRITES — Unity import pack

7 individual RGBA PNGs, each 512 x 512, plus a 2048 x 1024 atlas.
Transparent backgrounds; original antialiased outlines retained.
Heads face right. Flip the assembled character horizontally to face left.

EASIEST IMPORT
Copy PNG/ into Assets/Art/Soldiers.
Texture Type: Sprite (2D and UI). Sprite Mode: Single.
Pixels Per Unit: 256 for every part (adjust all together to fit your scene).
Mesh Type: Full Rect. Filter Mode: Bilinear. Wrap Mode: Clamp.
Alpha Source: Input Texture Alpha. Alpha Is Transparency: enabled.
Generate Mip Maps: disabled. Compression: None.
Max Size: at least 512 for individual PNGs; 2048 for the atlas.
Keep platform overrides from reducing the texture size.

PIVOTS / ASSEMBLY
Bodies: Center (0.5, 0.5).
Heads: Custom (0.5, 0.25), bottom-center attachment point.
Use a child Head transform placed near the top of the body, with a small
overlap; render the head above the body. Swap heads on this same transform.
The plain head has been scaled to better match the equipped heads.
Keep the same PPU and transform scale for all head variants.
Use SpriteRenderer flipX on both parts, or flip the shared parent.

OPTIONAL ATLAS
Import soldier_atlas_512.png as Multiple. Sprite Editor > Slice:
Grid By Cell Size, 512 x 512, Offset 0, Padding 0.
Top row, left to right: body_blue, body_red, head_plain, empty.
Bottom row: head_cap_blue, head_cap_red,
            head_bandana_blue, head_bandana_red.
Delete the empty slice if generated. Set the pivots above.
sprite_layout.json records exact names, rectangles and pivots;
rectangle coordinates use Unity bottom-left origin.
Import either individual PNGs or the atlas to avoid duplicate assets.

QUALITY CHECK
Exports checked for transparent margins, isolated silhouettes and clipping.
Hidden color outside the sprites was cleared. Artwork was not redrawn.
Preview/contact_sheet.png is an opaque viewing aid, not a game texture.
Unity runtime appearance and final character proportions still need a
quick in-scene check at your intended camera zoom.
