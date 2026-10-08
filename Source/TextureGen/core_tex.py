"""
Textures for the broodmother's core (procedural):

    Textures/Things/Buildings/Misc/Neph_BroodmotherNavel.png   the opening into her: a puckered, milk-wet hollow in her flesh
    Textures/Things/Buildings/Misc/Neph_BroodmotherHeart.png   her heart: a great gem heart nested in her flesh
    Textures/Things/Item/Neph_BroodmotherHeart.png             the heart, torn out

    python core_tex.py
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

FLESH = np.array([0.93, 0.92, 0.93])
CREASE = np.array([0.50, 0.49, 0.54])
BLUSH = np.array([0.95, 0.62, 0.66])
MILK = np.array([1.0, 0.96, 0.88])


def value_noise(n, scale, rng):
    g = rng.random((scale + 1, scale + 1))
    x = np.linspace(0, scale, n, endpoint=False)
    x0 = np.floor(x).astype(int)
    t = x - x0
    t = t * t * (3 - 2 * t)
    a = g[x0][:, x0] * (1 - t)[None, :] + g[x0][:, x0 + 1] * t[None, :]
    b = g[x0 + 1][:, x0] * (1 - t)[None, :] + g[x0 + 1][:, x0 + 1] * t[None, :]
    return a * (1 - t)[:, None] + b * t[:, None]


def fbm(n, rng, octaves=(4, 8, 16, 32)):
    out, amp, tot = np.zeros((n, n)), 1.0, 0.0
    for s in octaves:
        out += value_noise(n, s, rng) * amp
        tot += amp
        amp *= 0.5
    return out / tot


def grid(n):
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    return x, -y  # +y up


def save(rgba, rel):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").save(path)
    print(rel)


def flesh_mound(n, rng, radius):
    """A round swell of her flesh, soft-edged, with folds; returns (rgb, alpha, r, angle)."""
    x, y = grid(n)
    r = np.hypot(x, y)
    a = np.arctan2(y, x)
    wobble = 1 + 0.06 * (fbm(n, rng, (3, 6)) - 0.5) * 2
    edge = radius * wobble
    alpha = np.clip((edge - r) / 0.06, 0, 1)
    folds = np.abs(fbm(n, rng, (4, 8, 16)) - 0.5) * 2
    crease = np.clip(1 - folds, 0, 1) ** 8
    col = FLESH * (1 - crease[..., None] * 0.55) + CREASE * (crease[..., None] * 0.55)
    col *= (0.9 + 0.12 * fbm(n, rng))[..., None]
    # shading: lit from the top left, darker at the rim
    shade = 1 - 0.18 * np.clip(r / radius, 0, 1) ** 2 + 0.06 * (y - x) / 2
    col *= shade[..., None]
    return col, alpha, r, a


def navel():
    n = 384
    rng = np.random.default_rng(0xA7E1)
    col, alpha, r, a = flesh_mound(n, rng, 0.97)
    # radial puckering towards the hole
    swirl = a * 11 + r * 6 + 2.5 * fbm(n, rng, (3, 6))
    pucker = np.clip(np.cos(swirl), 0, 1) ** 3
    ring = np.clip(1 - np.abs(r - 0.4) / 0.24, 0, 1)
    col = col * (1 - (pucker * ring * 0.5)[..., None]) + CREASE * (pucker * ring * 0.5)[..., None]
    blush = np.clip(1 - np.abs(r - 0.35) / 0.25, 0, 1)[..., None] * 0.55
    col = col * (1 - blush) + BLUSH * blush
    # the hole: dark, deep pink, falling away into nothing
    hole = np.clip((0.26 - r) / 0.08, 0, 1)[..., None]
    depth = np.clip(r / 0.26, 0, 1)[..., None]
    inner = np.array([0.22, 0.08, 0.12]) * depth + np.array([0.04, 0.01, 0.02]) * (1 - depth)
    col = col * (1 - hole) + inner * hole
    # milk running down into it
    # milk welling at the lip and running down into it in a few uneven trickles
    lip = np.clip(1 - np.abs(r - 0.28) / 0.035, 0, 1) * (0.5 + 0.5 * fbm(n, rng, (6, 12)))
    x, y = grid(n)
    trickle = np.zeros((n, n))
    for ang, length, w in ((2.1, 0.30, 0.030), (2.9, 0.20, 0.022), (4.4, 0.36, 0.034), (5.6, 0.16, 0.020)):
        t = (r - 0.27) / length
        wig = 0.05 * np.sin(r * 30 + ang * 3)
        d = np.abs(np.angle(np.exp(1j * (a - ang - wig)))) * r
        width = w * (1 - np.clip(t, 0, 1)) + 0.006
        trickle = np.maximum(trickle, np.clip(1 - d / width, 0, 1) * ((t > 0) & (t < 1)))
    wet = np.clip(lip + trickle, 0, 1) * 0.85
    col = col * (1 - wet[..., None]) + MILK * wet[..., None]
    save(np.dstack([col, alpha]), "Textures/Things/Buildings/Misc/Neph_BroodmotherNavel.png")


def heart_mask(x, y, scale):
    """The classic heart curve, scaled; < 0 inside."""
    x, y = x / scale * 1.1, y / scale * 1.1 + 0.12
    return (x * x + y * y - 1) ** 3 - x * x * y ** 3


def gem(n, dy, scale, rng):
    """A faceted, iridescent heart gem, raised by dy. Returns rgb, alpha."""
    x, y = grid(n)
    y = y + dy
    # antialiased silhouette: the curve's value is a poor distance, so supersample its sign instead
    xs, ys = grid(n * 4)
    def silhouette(sc):
        m = (heart_mask(xs, ys + dy, sc) < 0).astype(float)
        return m.reshape(n, 4, n, 4).mean(axis=(1, 3))
    inside = silhouette(scale)
    edge = inside - silhouette(scale * 0.96)
    # facets: angular bands around the centre
    a = np.arctan2(y, x)
    facet = (np.floor((a + np.pi) / (2 * np.pi) * 12) % 3) / 2
    rr = np.hypot(x, y) / scale
    pearl = np.array([0.98, 0.95, 0.98])
    pink = np.array([0.98, 0.70, 0.82])
    lilac = np.array([0.80, 0.72, 0.98])
    sheen = 0.5 + 0.5 * np.sin(rr * 7 + facet * 0.6 + 5 * fbm(n, rng, (3, 6)))
    col = pink * (1 - sheen[..., None]) + lilac * sheen[..., None]
    core = np.clip(1 - rr * 1.3, 0, 1)[..., None]
    col = col * (1 - core) + pearl * core
    col *= (0.94 + 0.06 * facet)[..., None]
    # highlight, top left
    hl = np.clip(1 - np.hypot(x + 0.35 * scale, y - 0.35 * scale) / (0.25 * scale), 0, 1) ** 2
    col = col * (1 - hl[..., None]) + np.array([1, 1, 1]) * hl[..., None]
    # dark rim
    rim = edge
    col = col * (1 - rim[..., None] * 0.5) + np.array([0.45, 0.25, 0.4]) * (rim[..., None] * 0.5)
    return col, inside


def heart_building():
    n = 384
    rng = np.random.default_rng(0x4EA27)
    col, alpha, r, a = flesh_mound(n, rng, 0.98)
    # tendrils of milk-veined flesh reaching in to hold the heart
    veins = 1 - np.abs(fbm(n, rng, (5, 10, 20)) - 0.5) * 2
    veins = np.clip((veins - 0.9) / 0.1, 0, 1) ** 2 * 0.55
    col = col * (1 - veins[..., None]) + MILK * veins[..., None]
    blush = np.clip(1 - r / 0.75, 0, 1)[..., None] * 0.5
    col = col * (1 - blush) + BLUSH * blush
    x, y = grid(n)
    g, gin = gem(n, 0.04, 0.52, rng)
    # a soft glow around the gem
    glow = np.clip(-heart_mask(x, y + 0.04, 0.7) * 3, 0, 1)[..., None] * 0.35
    col = col * (1 - glow) + np.array([1.0, 0.85, 0.93]) * glow
    col = col * (1 - gin[..., None]) + g * gin[..., None]
    save(np.dstack([col, alpha]), "Textures/Things/Buildings/Misc/Neph_BroodmotherHeart.png")


def heart_item():
    n = 128
    rng = np.random.default_rng(0x4EA28)
    g, gin = gem(n, 0.08, 0.72, rng)
    save(np.dstack([g, gin]), "Textures/Things/Item/Neph_BroodmotherHeart.png")


if __name__ == "__main__":
    navel()
    heart_building()
    heart_item()
