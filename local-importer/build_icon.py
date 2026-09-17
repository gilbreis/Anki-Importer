from pathlib import Path
from math import cos, sin, pi

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
out = ROOT / "artifacts" / "anki-importer.ico"
out.parent.mkdir(parents=True, exist_ok=True)

size = 256
img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# Fundo escuro arredondado, inspirado na linguagem visual do Anki sem copiar o logo oficial.
draw.rounded_rectangle((18, 18, 238, 238), radius=42, fill=(38, 41, 46, 255), outline=(88, 92, 99, 255), width=8)
draw.rounded_rectangle((34, 34, 222, 222), radius=32, fill=(52, 56, 62, 255))

# Faixa superior suave.
draw.pieslice((20, -40, 236, 190), start=180, end=360, fill=(108, 113, 120, 180))


def star_points(cx, cy, outer, inner, points=5, rotation=-pi / 2):
    result = []
    for i in range(points * 2):
        radius = outer if i % 2 == 0 else inner
        angle = rotation + i * pi / points
        result.append((cx + cos(angle) * radius, cy + sin(angle) * radius))
    return result


def draw_star(cx, cy, outer, inner):
    outline = star_points(cx, cy, outer + 9, inner + 6)
    fill = star_points(cx, cy, outer, inner)
    draw.polygon(outline, fill=(245, 247, 250, 255))
    draw.polygon(fill, fill=(33, 150, 243, 255))


draw_star(142, 157, 62, 31)
draw_star(183, 82, 27, 13)

img.save(out, format="ICO", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
print(out)
