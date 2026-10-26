"""Cut Tilly out of her Magic Patterns backgrounds.

    python -I docs/print/_build/cutout.py

Reads the original Magic Patterns JPGs in characters-src/ and writes transparent
PNGs to docs/print/characters/. The originals are drawn on a flat cream
background (or white, for the face mark), so the background is every pixel close
to the corner colour that is reachable from the image border, plus any enclosed
pocket of that colour big enough to be a gap between an arm and the body rather
than a highlight. Edges get a short alpha ramp so the cut is not jagged.

Needs Pillow and numpy only.
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
SRC = HERE / "characters-src"
OUT = HERE.parent / "characters"

# name -> colour tolerance from the background. The gymnastics mat stays (it is
# part of the pose). The swimming pool does not cut cleanly, so that pose is only
# ever used framed and is not listed here.
POSES = {
    "tilly-twirl": 22,
    "tilly-dance": 20,
    "tilly-volleyball": 22,
    "tilly-soccer": 20,
    "tilly-gymnastics": 20,
    "tilly-show-and-tell": 20,
    "tilly-hello": 20,
}

BIG_POCKET = 18  # px an enclosed pocket must survive erosion by to count as background (arm gaps, not hearts or eye whites)


def shift_or(mask: np.ndarray) -> np.ndarray:
    out = mask.copy()
    out[1:, :] |= mask[:-1, :]
    out[:-1, :] |= mask[1:, :]
    out[:, 1:] |= mask[:, :-1]
    out[:, :-1] |= mask[:, 1:]
    return out


def shift_and(mask: np.ndarray) -> np.ndarray:
    out = mask.copy()
    out[1:, :] &= mask[:-1, :]
    out[:-1, :] &= mask[1:, :]
    out[:, 1:] &= mask[:, :-1]
    out[:, :-1] &= mask[:, 1:]
    return out


def reconstruct(seed: np.ndarray, within: np.ndarray) -> np.ndarray:
    """Grow seed inside `within` until it stops changing (flood fill)."""
    grown = seed & within
    while True:
        nxt = shift_or(grown) & within
        if (nxt == grown).all():
            return grown
        grown = nxt


def cut(name: str, tol: int) -> Image.Image:
    rgb = np.asarray(Image.open(SRC / f"{name}.jpg").convert("RGB")).astype(np.float32)
    h, w, _ = rgb.shape
    border = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    bg = np.median(border, axis=0)
    dist = np.sqrt(((rgb - bg) ** 2).sum(axis=2))

    candidate = dist < tol
    edge = np.zeros_like(candidate)
    edge[0, :] = edge[-1, :] = edge[:, 0] = edge[:, -1] = True
    background = reconstruct(edge, candidate)

    pockets = candidate & ~background
    core = pockets
    for _ in range(BIG_POCKET):
        core = shift_and(core)
    background |= reconstruct(core, pockets)

    # Alpha: opaque inside, a 2px ramp on the dist value at the boundary.
    alpha = np.where(background, 0.0, 1.0)
    rim = shift_or(shift_or(background)) & ~background
    ramp = np.clip((dist - tol * 0.5) / (tol * 1.2), 0, 1)
    alpha = np.where(rim, np.maximum(ramp, 0.35), alpha)
    alpha = np.where(background, 0.0, alpha)

    rgba = np.dstack([rgb, alpha * 255]).astype(np.uint8)
    img = Image.fromarray(rgba, "RGBA")
    box = img.getbbox()
    pad = 8
    box = (max(box[0] - pad, 0), max(box[1] - pad, 0), min(box[2] + pad, w), min(box[3] + pad, h))
    return img.crop(box)


def face_mark() -> Image.Image:
    """The face logomark is a pink rounded tile on white; keep the tile only."""
    rgb = Image.open(SRC / "tilly-face-mark.jpg").convert("RGB")
    a = np.asarray(rgb).astype(np.float32)
    dist = np.sqrt(((a - 255) ** 2).sum(axis=2))
    ys, xs = np.where(dist > 12)
    box = (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)
    tile = rgb.crop(box)
    side = min(tile.size)
    tile = tile.crop((0, 0, side, side))
    mask = Image.new("L", (side * 4, side * 4), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, side * 4 - 1, side * 4 - 1), radius=int(side * 4 * 0.22), fill=255)
    mask = mask.resize((side, side), Image.LANCZOS)
    tile.putalpha(mask)
    return tile


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, tol in POSES.items():
        img = cut(name, tol)
        img.save(OUT / f"{name}.png", optimize=True)
        print(f"✓ {name:24} {img.size[0]}x{img.size[1]}")
    face_mark().save(OUT / "tilly-face-mark.png", optimize=True)
    print("✓ tilly-face-mark")


if __name__ == "__main__":
    main()
