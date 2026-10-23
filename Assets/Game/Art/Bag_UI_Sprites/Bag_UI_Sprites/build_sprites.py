from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np
import zipfile
P=Path(__file__).resolve().parent
S=4

def tile(w,h,flat=False):
    W,H=w*S,h*S
    out=Image.new('RGBA',(W,H),(255,255,255,0))
    def layer(box,r,top,bottom):
        mask=Image.new('L',(W,H)); ImageDraw.Draw(mask).rounded_rectangle(tuple(int(v*S) for v in box),radius=r*S,fill=255)
        y=np.arange(H)/S
        t=np.clip((y-box[1])/max(1,box[3]-box[1]),0,1)
        vals=np.round(top+(bottom-top)*t).astype('uint8')
        rgb=np.repeat(np.repeat(vals[:,None,None],W,axis=1),3,axis=2)
        im=Image.fromarray(rgb,'RGB').convert('RGBA'); im.putalpha(mask); out.alpha_composite(im)
    if flat:
        layer((0,0,w-0.25,h-0.25),30,185,165)
        layer((3,3,w-3.25,h-3.25),27,250,237)
    else:
        layer((0,0,w-0.25,h-0.25),34,137,123)
        layer((2,2,w-2.25,h-2.25),32,202,163)
        layer((4,4,w-4.25,h-11.25),30,244,217)
        layer((10,10,w-10.25,h-17.25),24,255,252)
        layer((16,16,w-16.25,h-23.25),18,249,237)
        # soft white central face, with the shading confined near edges
    out=out.resize((w,h),Image.Resampling.LANCZOS)
    a=np.array(out); a[a[:,:,3]==0,:3]=255
    return Image.fromarray(a)

items=[('Grid_Cell_1x1',256,256,True),('Item_Background_1x1',256,256,False),('Item_Background_1x2',256,512,False),('Item_Background_2x1',512,256,False)]
for name,w,h,flat in items:
    im=tile(w,h,flat); im.save(P/(name+'.png'))
    a=np.array(im)
    assert im.size==(w,h) and a[:,:,3].min()==0 and a[:,:,3].max()==255
    assert np.array_equal(a[:,:,0],a[:,:,1]) and np.array_equal(a[:,:,1],a[:,:,2])
    print(name,im.size,'grayscale / alpha verified')
# Separate labeled preview, never used as a game asset.
preview=Image.new('RGB',(1456,630),(49,57,69)); d=ImageDraw.Draw(preview)
x=32
for name,w,h,flat in items:
    im=Image.open(P/(name+'.png')); preview.paste(im,(x,560-h),im)
    d.text((x,580),name,fill='white'); x+=w+32
preview.save(P/'Preview.png')
(P/'README.txt').write_text('''BAG UI SPRITES — WHITE / GRAYSCALE

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
''')
with zipfile.ZipFile(P.parent/'Bag_UI_Sprites.zip','w',zipfile.ZIP_DEFLATED) as z:
    for f in sorted(P.iterdir()): z.write(f,'Bag_UI_Sprites/'+f.name)
