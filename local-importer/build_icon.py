from pathlib import Path
from io import BytesIO

import cairosvg
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
svg = ROOT / "assets" / "anki-importer.svg"
out = ROOT / "artifacts" / "anki-importer.ico"
out.parent.mkdir(parents=True, exist_ok=True)

png = cairosvg.svg2png(url=str(svg), output_width=256, output_height=256)
image = Image.open(BytesIO(png)).convert("RGBA")
image.save(out, format="ICO", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
print(out)
