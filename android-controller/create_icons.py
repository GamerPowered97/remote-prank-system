from PIL import Image, ImageDraw, ImageFilter
import math
import os

def draw_icon(size=512, is_round=False):
    # Create high-res canvas with RGBA
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center = size / 2.0
    radius = size * 0.46

    # 1. Background (Rounded Rect or Circle)
    if is_round:
        draw.ellipse(
            [center - radius, center - radius, center + radius, center + radius],
            fill=(11, 15, 25, 255) # Deep Space Slate #0B0F19
        )
    else:
        # Modern Squircle / Smooth rounded rectangle
        corner_rad = size * 0.22
        draw.rounded_rectangle(
            [size * 0.04, size * 0.04, size * 0.96, size * 0.96],
            radius=corner_rad,
            fill=(11, 15, 25, 255)
        )

    # 2. Subtle ambient outer glow ring (cyan / electric blue)
    glow_r = size * 0.38
    draw.ellipse(
        [center - glow_r, center - glow_r, center + glow_r, center + glow_r],
        outline=(14, 165, 233, 60),
        width=int(size * 0.03)
    )

    # 3. Outer vibrant electric blue circle (matches Windows tray icon: RGB 14, 165, 233 / #0EA5E9)
    main_r = size * 0.32
    draw.ellipse(
        [center - main_r, center - main_r, center + main_r, center + main_r],
        fill=(14, 165, 233, 255)
    )

    # 4. Dark contrast ring (matches Windows tray icon: RGB 15, 23, 42 / #0F172A)
    dark_r = size * 0.23
    draw.ellipse(
        [center - dark_r, center - dark_r, center + dark_r, center + dark_r],
        fill=(15, 23, 42, 255)
    )

    # 5. Center bright cyan target core (matches Windows tray icon: RGB 56, 189, 248 / #38BDF8)
    core_r = size * 0.12
    draw.ellipse(
        [center - core_r, center - core_r, center + core_r, center + core_r],
        fill=(56, 189, 248, 255)
    )

    # Center glint
    glint_r = size * 0.04
    draw.ellipse(
        [center - glint_r, center - glint_r, center + glint_r, center + glint_r],
        fill=(255, 255, 255, 230)
    )

    # 6. Remote transmission arcs (top-left & top-right pulse arcs)
    for wave_mult, wave_width, alpha in [(0.42, 0.02, 180), (0.46, 0.015, 120)]:
        arc_r = size * wave_mult
        w_px = max(2, int(size * wave_width))
        # Draw upper arc
        bbox = [center - arc_r, center - arc_r, center + arc_r, center + arc_r]
        draw.arc(bbox, start=210, end=330, fill=(56, 189, 248, alpha), width=w_px)

    return img

densities = {
    "mipmap-mdpi": 48,
    "mipmap-hdpi": 72,
    "mipmap-xhdpi": 96,
    "mipmap-xxhdpi": 144,
    "mipmap-xxxhdpi": 192,
}

base_res = "android-controller/app/src/main/res"

# Generate 512 for Play Store / high-res assets
icon_512 = draw_icon(512, is_round=False)
icon_512.save(os.path.join(base_res, "ic_launcher_512.png"))

for folder, dim in densities.items():
    target_dir = os.path.join(base_res, folder)
    os.makedirs(target_dir, exist_ok=True)

    # Standard (squircle)
    img_sq = draw_icon(dim * 4, is_round=False).resize((dim, dim), Image.Resampling.LANCZOS)
    img_sq.save(os.path.join(target_dir, "ic_launcher.png"))

    # Round
    img_rd = draw_icon(dim * 4, is_round=True).resize((dim, dim), Image.Resampling.LANCZOS)
    img_rd.save(os.path.join(target_dir, "ic_launcher_round.png"))

print("All Android launcher icons generated successfully!")
