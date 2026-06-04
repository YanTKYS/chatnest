"""
ChatNest icon generator v2
Design: Purple rounded square + large white LINE-style speech bubble (no text lines)
"""
from PIL import Image, ImageDraw


def create_icon(size: int) -> Image.Image:
    s = size
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # ── Background ──────────────────────────────────────────────────────────
    purple = (124, 58, 237, 255)        # #7C3AED vivid violet-purple
    bg_r = round(s * 0.215)            # Apple-standard ~22% corner radius
    draw.rounded_rectangle([0, 0, s - 1, s - 1], radius=bg_r, fill=purple)

    # ── Bubble body ──────────────────────────────────────────────────────────
    # Large rounded rect — fills most of the icon, shifted slightly upward
    # to leave visual space for the tail below
    pad    = round(s * 0.14)           # padding from icon edge
    tail_h = round(s * 0.175)          # vertical space reserved for tail
    bx1 = pad
    by1 = pad
    bx2 = s - pad
    by2 = s - pad - tail_h            # bubble bottom edge

    b_r = round(s * 0.115)            # bubble corner radius (generous, LINE-style)
    white = (255, 255, 255, 255)
    draw.rounded_rectangle([bx1, by1, bx2, by2], radius=b_r, fill=white)

    # ── LINE-style tail ───────────────────────────────────────────────────────
    # A neat downward triangle at the bottom-left area of the bubble,
    # overlapping the bubble edge so the join looks seamless.
    #
    # Anchor points (attached to bubble bottom):
    #   left  = just inside the left rounded corner
    #   right = ~26 % of bubble width from left
    # Tip: points diagonally down-left, past the bubble boundary
    t_attach_l = round(bx1 + s * 0.065)
    t_attach_r = round(bx1 + s * 0.255)
    t_attach_y = by2 - 1              # 1 px inside bubble so fill merges

    t_tip_x = round(bx1 + s * 0.035)
    t_tip_y = round(by2 + tail_h * 0.80)

    draw.polygon(
        [(t_attach_l, t_attach_y), (t_attach_r, t_attach_y), (t_tip_x, t_tip_y)],
        fill=white,
    )

    return img


def build_ico(out_path: str):
    sizes = [256, 128, 64, 48, 32, 24, 16]
    frames = [create_icon(sz) for sz in sizes]

    frames[0].save(
        out_path,
        format="ICO",
        sizes=[(f.width, f.height) for f in frames],
        append_images=frames[1:],
    )
    print(f"Saved {out_path}  ({[f.width for f in frames]}px)")

    preview = out_path.replace(".ico", "_preview.png")
    frames[0].save(preview)
    print(f"Preview: {preview}")


if __name__ == "__main__":
    build_ico("chatnest.ico")
