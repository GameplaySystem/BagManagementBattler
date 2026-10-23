BAG UI SPRITES — WHITE / GRAYSCALE

Four separate RGBA PNGs. Exact texture and silhouette extents:
Grid_Cell_1x1.png          256 x 256
Item_Background_1x1.png    256 x 256
Item_Background_1x2.png    256 x 512 (vertical)
Item_Background_2x1.png    512 x 256 (horizontal)

Rebuilt programmatically to match the approved style, with consistent corner
radii, borders, white highlight rims, and lower gray lips. No external shadow.
No color contamination: R=G=B. Transparent rounded corners, antialiased edges.

UNITY IMPORT
Texture Type: Sprite (2D and UI)
Sprite Mode: Single (each PNG is already separate)
Pixels Per Unit: 256 for a one-unit cell
Pivot: Center; Mesh Type: Full Rect
Filter Mode: Bilinear; Wrap Mode: Clamp
Compression: None; Generate Mip Maps: Off; Alpha Is Transparency: On
Max Size: 512 or higher
Apply tint through SpriteRenderer.color or UI Image.color.
A white rim also takes the tint, as expected with normal multiplicative tinting.

OPTIONAL 9-SLICING
In Sprite Editor set all four borders to 40 pixels. Use Sliced draw mode / Image
type to resize while preserving corners and bevel thickness. Avoid sizes below
80 pixels in either dimension. Keep the vertical and horizontal tiles oriented
as supplied so the shading stays at the bottom.

At 256 PPU the item footprints are exactly 1x1, 1x2, and 2x1 world units.
For a small gap between items, reduce renderer size slightly using Sliced mode.

Preview.png is a labeled preview only. build_sprites.py is the reproducible
source (Python with Pillow and NumPy). No Unity project integration was tested.
