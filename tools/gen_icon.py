"""
ChatNest icon generator
Design: Blue (#1976D2) rounded square + white speech bubble with text lines
Matches the NoteNest design language (flat, clean, lines = text metaphor)
"""
from PIL import Image, ImageDraw
import math


def draw_rounded_rect(draw, x1, y1, x2, y2, r, fill):
    draw.rounded_rectangle([x1, y1, x2, y2], radius=r, fill=fill)


def create_icon(size: int) -> Image.Image:
    s = size
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # ── Background ──────────────────────────────────────────────────────────
    blue = (25, 118, 210, 255)          # #1976D2
    bg_r = round(s * 0.215)            # ~55 px @ 256 (Apple standard ~22%)
    draw_rounded_rect(draw, 0, 0, s - 1, s - 1, bg_r, blue)

    # ── Bubble geometry ──────────────────────────────────────────────────────
    # Body: a rounded rectangle sitting in the upper 70% of the canvas
    pad   = round(s * 0.185)           # outer padding
    tail_h = round(s * 0.13)           # height of tail area
    bx1, by1 = pad, pad
    bx2       = s - pad
    by2       = s - pad - tail_h

    b_r = round(s * 0.095)             # bubble corner radius
    white = (255, 255, 255, 255)
    draw_rounded_rect(draw, bx1, by1, bx2, by2, b_r, white)

    # Tail: triangle anchored at bottom-left of bubble
    # Points: bottom-left tip | attach-left | attach-right
    tail_tip_x = round(bx1 + s * 0.04)
    tail_tip_y = round(by2 + tail_h * 0.82)
    attach_l   = round(bx1 + s * 0.01)
    attach_r   = round(bx1 + s * 0.20)
    attach_y   = by2 + 2               # slightly below bubble bottom edge
    draw.polygon(
        [(tail_tip_x, tail_tip_y), (attach_l, attach_y), (attach_r, attach_y)],
        fill=white,
    )

    # ── Text lines inside bubble ─────────────────────────────────────────────
    # 3 lines mirroring NoteNest's "text = horizontal bars" metaphor
    # Colors are the bubble-blue so they read as content inside the bubble
    line_color = blue
    lh  = max(2, round(s * 0.036))     # line height
    lr  = lh // 2                      # line corner radius
    lx1 = round(bx1 + s * 0.105)       # left margin inside bubble
    lx2_full = round(bx2 - s * 0.105)  # full-width right edge

    bubble_h = by2 - by1
    line_ys = [
        round(by1 + bubble_h * 0.25),
        round(by1 + bubble_h * 0.50),
        round(by1 + bubble_h * 0.75),
    ]
    # First line shorter (like a header / sender name)
    line_widths = [0.55, 1.0, 0.80]

    for y, w in zip(line_ys, line_widths):
        rx2 = round(lx1 + (lx2_full - lx1) * w)
        draw_rounded_rect(draw, lx1, y, rx2, y + lh, lr, line_color)

    return img


def build_ico(out_path: str):
    sizes = [256, 128, 64, 48, 32, 24, 16]
    frames = []
    for sz in sizes:
        icon = create_icon(sz)
        # ICO needs RGB(A) – keep RGBA for transparency support
        frames.append(icon)

    # PIL saves ICO with the first image as primary; pass all sizes
    frames[0].save(
        out_path,
        format="ICO",
        sizes=[(f.width, f.height) for f in frames],
        append_images=frames[1:],
    )
    print(f"Saved {out_path}  (sizes: {[f.width for f in frames]})")

    # Also save a 256px PNG preview
    preview = out_path.replace(".ico", "_preview.png")
    frames[0].save(preview)
    print(f"Preview: {preview}")


if __name__ == "__main__":
    build_ico("chatnest.ico")
