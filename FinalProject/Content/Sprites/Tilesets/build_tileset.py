from PIL import Image, ImageDraw
import random

random.seed(7)

T = 20  # tile size, matches Tiltan_tilesets.tsx
COLS = 8
ROWS = 5
W, H = T * COLS, T * ROWS

sheet = Image.new("RGBA", (W, H), (0, 0, 0, 0))

def tile_canvas():
    return Image.new("RGBA", (T, T), (0, 0, 0, 0))

def paste(im, col, row):
    sheet.alpha_composite(im, (col * T, row * T))

def fill(draw, color):
    draw.rectangle([0, 0, T - 1, T - 1], fill=color)

def speckle(draw, color, count, rng=None):
    rng = rng or range(0, T)
    for _ in range(count):
        x = random.randint(0, T - 1)
        y = random.randint(0, T - 1)
        draw.point((x, y), fill=color)

# ---------- palette ----------
STONE      = (107, 90, 115, 255)
STONE_DK   = (78, 61, 84, 255)
STONE_LT   = (138, 119, 145, 255)
BRICK      = (125, 90, 140, 255)
BRICK_DK   = (74, 51, 84, 255)
BRICK_LT   = (154, 122, 168, 255)
GRASS      = (76, 140, 90, 255)
GRASS_DK   = (58, 110, 70, 255)
DIRT       = (138, 107, 74, 255)
DIRT_DK    = (107, 79, 54, 255)
WATER      = (79, 168, 201, 255)
WATER_DK   = (47, 122, 151, 255)
WATER_LT   = (140, 210, 230, 255)
GOLD       = (242, 211, 59, 255)
GOLD_DK    = (184, 154, 30, 255)
VINE       = (58, 110, 70, 255)
VINE_DK    = (35, 67, 31, 255)
STAR       = (255, 243, 107, 255)
STAR_DK    = (203, 185, 46, 255)
WOOD       = (120, 84, 58, 255)
WOOD_DK    = (82, 56, 38, 255)
GREY       = (100, 100, 110, 255)
GREY_DK    = (60, 60, 68, 255)
BARK       = (36, 26, 28, 255)
STATUE     = (154, 138, 163, 255)
STATUE_DK  = (90, 77, 97, 255)

def outline(draw, color=(30, 22, 33, 255)):
    draw.rectangle([0, 0, T - 1, T - 1], outline=color)

# ============================================================
# ROW 0 - Ground floors (walkable)
# ============================================================

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, STONE)
for i in range(0, T, 5):
    d.line([(0, i), (T, i)], fill=STONE_DK)
for i in range(0, T, 7):
    d.line([(i, 0), (i, T)], fill=STONE_DK)
speckle(d, STONE_LT, 10)
paste(im, 0, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, STONE)
for i in range(0, T, 5):
    d.line([(0, i), (T, i)], fill=STONE_DK)
for i in range(0, T, 7):
    d.line([(i, 0), (i, T)], fill=STONE_DK)
d.line([(3, 2), (8, 9), (6, 15)], fill=STONE_DK)
speckle(d, STONE_LT, 8)
paste(im, 1, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, GRASS)
for _ in range(14):
    x, y = random.randint(1, T - 2), random.randint(1, T - 2)
    d.line([(x, y), (x, y - 2)], fill=GRASS_DK)
paste(im, 2, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, DIRT)
speckle(d, DIRT_DK, 20)
paste(im, 3, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, WATER)
for i in range(2, T, 6):
    d.line([(0, i), (T, i - 3)], fill=WATER_LT)
d.rectangle([0, 0, T - 1, T - 1], outline=WATER_DK)
paste(im, 4, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, STONE)
cx, cy = T // 2, T // 2
d.polygon([(cx, 1), (T - 1, cy), (cx, T - 2), (1, cy)], outline=STONE_LT)
d.point((cx, cy), fill=STONE_LT)
paste(im, 5, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
fill(d, WOOD)
for i in range(0, T, 5):
    d.line([(0, i), (T, i)], fill=WOOD_DK)
paste(im, 6, 0)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([0, 0, T - 1, T // 2 - 1], fill=GRASS)
d.rectangle([0, T // 2, T - 1, T - 1], fill=STONE)
for x in range(0, T, 3):
    d.line([(x, T // 2 - 2), (x, T // 2)], fill=GRASS_DK)
paste(im, 7, 0)

# ============================================================
# ROW 1 - Ground decorative overlays (walkable, transparent bg)
# ============================================================

im = tile_canvas(); d = ImageDraw.Draw(im)
d.line([(6, 0), (6, 8), (9, 12), (9, T - 1)], fill=VINE, width=2)
d.line([(14, 0), (14, 6), (11, 10), (12, T - 1)], fill=VINE_DK, width=1)
paste(im, 0, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
for (fx, fy) in [(6, 7), (13, 6), (9, 13), (15, 14)]:
    d.ellipse([fx - 2, fy - 2, fx + 2, fy + 2], fill=GOLD, outline=GOLD_DK)
paste(im, 1, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
cx, cy = T // 2, T // 2
d.line([(cx, cy - 8), (cx, cy + 8)], fill=STAR_DK)
d.line([(cx - 8, cy), (cx + 8, cy)], fill=STAR_DK)
d.polygon([(cx, cy - 5), (cx + 2, cy - 2), (cx + 5, cy), (cx + 2, cy + 2),
           (cx, cy + 5), (cx - 2, cy + 2), (cx - 5, cy), (cx - 2, cy - 2)], fill=STAR)
paste(im, 2, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
for _ in range(24):
    x, y = random.randint(0, T - 1), random.randint(0, T - 1)
    d.point((x, y), fill=VINE)
paste(im, 3, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.ellipse([4, 8, 16, 15], fill=WATER, outline=WATER_DK)
d.line([(7, 11), (12, 11)], fill=WATER_LT)
paste(im, 4, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
for (px, py, r) in [(5, 6, 2), (12, 9, 3), (8, 15, 2)]:
    d.ellipse([px - r, py - r, px + r, py + r], fill=GREY, outline=GREY_DK)
paste(im, 5, 1)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.line([(2, 2), (9, 10), (7, 18)], fill=STONE_DK)
d.line([(9, 10), (15, 14)], fill=STONE_DK)
paste(im, 6, 1)

mask = Image.new("L", (T, T), 0)
md = ImageDraw.Draw(mask)
md.pieslice([-T, -T, T, T], 0, 90, fill=255)
stone_layer = Image.new("RGBA", (T, T), STONE)
grass_layer = Image.new("RGBA", (T, T), GRASS)
im = Image.composite(stone_layer, grass_layer, mask)
paste(im, 7, 1)

# ============================================================
# ROW 2 - Objects: walls & small solid obstacles (blocking)
# ============================================================

def brick_pattern(d):
    fill(d, BRICK)
    row_h = 5
    for ry, y in enumerate(range(0, T, row_h)):
        d.line([(0, y), (T, y)], fill=BRICK_DK)
        shift = (row_h // 2) if (ry % 2 == 0) else 0
        for x in range(-shift, T, 7):
            d.line([(x, y), (x, y + row_h)], fill=BRICK_DK)
    speckle(d, BRICK_LT, 6)

im = tile_canvas(); d = ImageDraw.Draw(im)
brick_pattern(d)
outline(d)
paste(im, 0, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([0, 0, T - 1, 5], fill=STONE_LT)
d.rectangle([0, 6, T - 1, T - 1], fill=BRICK)
d.line([(0, 6), (T, 6)], fill=BRICK_DK)
for x in range(0, T, 7):
    d.line([(x, 6), (x, T)], fill=BRICK_DK)
outline(d)
paste(im, 1, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
brick_pattern(d)
d.rectangle([0, 0, 3, T - 1], fill=BRICK_DK)
outline(d)
paste(im, 2, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
brick_pattern(d)
d.line([(10, 0), (8, 9), (13, T - 1)], fill=(20, 14, 22, 255), width=1)
outline(d)
paste(im, 3, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
brick_pattern(d)
d.rectangle([4, 4, T - 5, T - 5], fill=(20, 20, 26, 255))
for x in range(5, T - 4, 4):
    d.line([(x, 4), (x, T - 5)], fill=GREY)
outline(d)
paste(im, 4, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.line([(2, 4), (2, T - 1)], fill=WOOD_DK, width=2)
d.line([(9, 2), (9, T - 1)], fill=WOOD_DK, width=2)
d.line([(16, 4), (16, T - 1)], fill=WOOD_DK, width=2)
d.line([(0, 8), (T, 6)], fill=WOOD, width=2)
d.line([(0, 15), (T, 13)], fill=WOOD, width=2)
paste(im, 5, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
brick_pattern(d)
d.rectangle([8, 10, 11, T - 3], fill=WOOD_DK)
d.polygon([(6, 5), (9, 1), (12, 5), (10, 9), (8, 9)], fill=GOLD)
d.polygon([(7, 6), (9, 3), (11, 6), (9, 8)], fill=STAR)
outline(d)
paste(im, 6, 2)

im = tile_canvas(); d = ImageDraw.Draw(im)
for (px, py, r) in [(6, 12, 4), (13, 13, 5), (10, 8, 4)]:
    d.ellipse([px - r, py - r, px + r, py + r], fill=STONE, outline=STONE_DK)
paste(im, 7, 2)

# ============================================================
# ROW 3 - Objects: top halves of 2-tall props, + single-tile props
# ============================================================

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([3, 6, T - 4, T - 1], fill=STATUE, outline=STATUE_DK)
d.rectangle([1, 0, T - 2, 7], fill=STATUE_DK)
d.rectangle([2, 1, T - 3, 6], fill=STATUE)
paste(im, 0, 3)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.line([(10, T - 1), (10, 10)], fill=BARK, width=3)
d.line([(10, 12), (3, 4)], fill=BARK, width=2)
d.line([(10, 10), (16, 2)], fill=BARK, width=2)
d.line([(10, 14), (15, 8)], fill=BARK, width=2)
paste(im, 1, 3)

# Doorway arch, built as one coherent 40x40 piece then split into quadrants
door_full = Image.new("RGBA", (2 * T, 2 * T), (0, 0, 0, 0))
dd = ImageDraw.Draw(door_full)
W2 = H2 = 2 * T
DARK = (24, 16, 26, 255)
for y in range(0, H2, 5):
    dd.rectangle([0, y, W2 - 1, y + 4], fill=BRICK)
for y in range(0, H2, 5):
    dd.line([(0, y), (W2, y)], fill=BRICK_DK)
for x in range(0, W2, 7):
    dd.line([(x, 0), (x, H2)], fill=BRICK_DK)
ax0, ax1 = 6, 34
dd.pieslice([ax0 - 2, -6, ax1 + 2, 38], 180, 360, fill=STATUE)
dd.rectangle([ax0 - 2, 16, ax1 + 2, 39], fill=STATUE)
ix0, ix1 = ax0 + 4, ax1 - 4
dd.pieslice([ix0 - 2, -2, ix1 + 2, 38], 180, 360, fill=DARK)
dd.rectangle([ix0 - 2, 16, ix1 + 2, 39], fill=DARK)
dd.rectangle([ax0 - 2, 36, ax1 + 2, 39], fill=STATUE_DK)

paste(door_full.crop((0, 0, T, T)), 2, 3)
paste(door_full.crop((T, 0, 2 * T, T)), 3, 3)
paste(door_full.crop((0, T, T, 2 * T)), 2, 4)
paste(door_full.crop((T, T, 2 * T, 2 * T)), 3, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.ellipse([6, 2, 14, 10], fill=STATUE, outline=STATUE_DK)
d.rectangle([5, 10, 15, T - 1], fill=STATUE, outline=STATUE_DK)
paste(im, 4, 3)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([9, 6, 11, T - 1], fill=WOOD_DK)
d.rectangle([3, 2, T - 4, 8], fill=WOOD, outline=WOOD_DK)
paste(im, 5, 3)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([9, 8, 11, T - 1], fill=GREY_DK)
d.ellipse([6, 1, 14, 9], fill=GOLD, outline=GOLD_DK)
paste(im, 6, 3)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([2, 2, T - 3, T - 3], fill=WOOD, outline=WOOD_DK)
d.line([(2, 2), (T - 3, T - 3)], fill=WOOD_DK)
d.line([(T - 3, 2), (2, T - 3)], fill=WOOD_DK)
paste(im, 7, 3)

# ============================================================
# ROW 4 - Objects: bottom halves matching row 3, cols 0-1,4-7
# ============================================================

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([3, 0, T - 4, 12], fill=STATUE, outline=STATUE_DK)
d.rectangle([0, 13, T - 1, T - 1], fill=STATUE_DK)
d.rectangle([1, 14, T - 2, T - 2], fill=STATUE)
paste(im, 0, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.line([(10, 0), (10, 14)], fill=BARK, width=4)
d.polygon([(6, T - 1), (10, 12), (14, T - 1)], fill=BARK)
paste(im, 1, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([4, 0, T - 5, 6], fill=STATUE)
d.rectangle([1, 7, T - 2, T - 1], fill=STATUE_DK, outline=(30,22,33,255))
paste(im, 4, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([3, 2, T - 4, T - 2], fill=WOOD, outline=WOOD_DK)
d.line([(3, 6), (T - 4, 6)], fill=WOOD_DK)
d.line([(3, 13), (T - 4, 13)], fill=WOOD_DK)
paste(im, 5, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([9, 0, 11, 10], fill=GREY_DK)
d.polygon([(6, T - 1), (9, 10), (11, 10), (14, T - 1)], fill=GREY)
paste(im, 6, 4)

im = tile_canvas(); d = ImageDraw.Draw(im)
d.rectangle([3, 9, 10, T - 2], fill=WOOD, outline=WOOD_DK)
d.rectangle([9, 6, T - 3, T - 5], fill=WOOD, outline=WOOD_DK)
paste(im, 7, 4)

sheet.save("tiltan_tale_ruins_v2.png")
print("saved", sheet.size)
