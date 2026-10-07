"""
Textures for the overgrown broodmother biome (procedural, seamless):

    Textures/Biomes/Neph_OvergrownBroodmother.png   world-map tile: pale Nephila flesh, grey folds, pink blush, veined with milk
    Textures/Terrain/Neph_MilkRamp.png              shallow milk (the water-depth shader's colour ramp)
    Textures/Terrain/Neph_MilkDeepRamp.png          deep milk

    python broodmother_tex.py
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def tile_noise(n, scale, rng):
    """Seamless value noise: random lattice wrapped on a torus, smoothly interpolated."""
    g = rng.random((scale, scale))
    x = np.arange(n) * scale / n
    x0 = np.floor(x).astype(int)
    t = x - x0
    t = t * t * (3 - 2 * t)
    x1 = (x0 + 1) % scale
    a = g[x0][:, x0] * (1 - t)[None, :] + g[x0][:, x1] * t[None, :]
    b = g[x1][:, x0] * (1 - t)[None, :] + g[x1][:, x1] * t[None, :]
    return a * (1 - t)[:, None] + b * t[:, None]


def fbm(n, rng, octaves=(4, 8, 16, 32), gain=0.5):
    out = np.zeros((n, n))
    amp, tot = 1.0, 0.0
    for s in octaves:
        out += tile_noise(n, s, rng) * amp
        tot += amp
        amp *= gain
    return out / tot


def world_tile():
    n = 512
    rng = np.random.default_rng(0xB800D)
    flesh = fbm(n, rng)
    folds = np.abs(fbm(n, rng, (3, 6, 12)) - 0.5) * 2          # ridged: the body's folds
    veins = 1 - np.abs(fbm(n, rng, (5, 10, 20)) - 0.5) * 2     # thin bright channels: milk
    veins = np.clip((veins - 0.86) / 0.14, 0, 1) ** 1.5

    # Nephila white: pale body, grey folds, pink blush where it swells, creamy milk channels
    base = np.array([0.93, 0.92, 0.93])
    dark = np.array([0.50, 0.49, 0.54])
    blush = np.array([0.95, 0.62, 0.66])
    milk = np.array([1.0, 0.96, 0.88])
    crease = np.clip(1 - folds, 0, 1) ** 6          # the dark lines where folds meet
    col = base * (1 - crease[..., None] * 0.7) + dark * (crease[..., None] * 0.7)
    col *= (1 - 0.12 * folds)[..., None]
    col *= (0.92 + 0.14 * flesh)[..., None]
    swell = np.clip((fbm(n, rng, (3, 6)) - 0.62) / 0.2, 0, 1)[..., None] * 0.45
    col = col * (1 - swell) + blush * swell
    col = col * (1 - veins[..., None]) + milk * veins[..., None]
    # faint iridescent sheen along the milk, as on Nephila milk
    hue = fbm(n, rng, (2, 4))
    # pink to lilac only - no greens
    sheen = np.stack([0.5 + 0.5 * np.sin(hue * 6.3), np.zeros_like(hue), 0.5 + 0.5 * np.cos(hue * 6.3)], -1) * 0.06
    col = np.clip(col + sheen * veins[..., None] - 0.02 * veins[..., None] * np.array([0, 1, 0]), 0, 1)
    Image.fromarray((col * 255).astype(np.uint8), "RGB").save(os.path.join(ROOT, "Textures", "Biomes", "Neph_OvergrownBroodmother.png"))


def ramp(path, shallow, deep):
    """64x64 colour ramp for Map/WaterDepth: shallow colour at one end, deep at the other."""
    n = 64
    t = np.linspace(0, 1, n)[:, None, None] * np.ones((1, n, 1))
    col = np.array(shallow) * (1 - t) + np.array(deep) * t
    Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8), "RGB").convert("RGBA").save(path)


if __name__ == "__main__":
    world_tile()
    terrain = os.path.join(ROOT, "Textures", "Terrain")
    ramp(os.path.join(terrain, "Neph_MilkRamp.png"), (0.97, 0.94, 0.95), (0.92, 0.86, 0.90))
    ramp(os.path.join(terrain, "Neph_MilkDeepRamp.png"), (0.90, 0.83, 0.89), (0.80, 0.70, 0.82))
    print("ok")
