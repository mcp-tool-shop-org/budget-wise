"""Generate all required MSIX/Store asset images from the base icon."""
from PIL import Image, ImageDraw, ImageFont
import os

def create_budget_icon(size: int, padding_ratio: float = 0.1) -> Image.Image:
    """Create a budget/envelope themed icon at the specified size."""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Add padding for store assets
    padding = int(size * padding_ratio)
    effective_size = size - (2 * padding)
    center = size // 2

    # Background circle with gradient effect (simplified to solid green)
    circle_radius = effective_size // 2
    for i in range(circle_radius, 0, -1):
        ratio = i / circle_radius
        r = int(46 + (76 - 46) * ratio)
        g = int(125 + (175 - 125) * ratio)
        b = int(50 + (80 - 50) * ratio)
        draw.ellipse(
            [center - i, center - i, center + i, center + i],
            fill=(r, g, b, 255)
        )

    # Scale all other elements relative to effective_size
    scale = effective_size / 256

    # White envelope rectangle
    env_margin = int(51 * scale) + padding
    env_top = int(64 * scale) + padding
    env_bottom = size - int(43 * scale) - padding
    corner_radius = int(13 * scale)

    draw.rounded_rectangle(
        [env_margin, env_top, size - env_margin, env_bottom],
        radius=corner_radius,
        fill=(255, 255, 255, 245)
    )

    # Envelope flap (V shape)
    flap_offset = int(6 * scale)
    flap_points = [
        (env_margin, env_top + flap_offset),
        (center, center - int(6 * scale)),
        (size - env_margin, env_top + flap_offset)
    ]
    draw.line(flap_points, fill=(76, 175, 80, 255), width=max(2, int(6 * scale)))

    # Dollar sign
    try:
        font_size = int(85 * scale)
        font = ImageFont.truetype("segoeui.ttf", font_size)
    except:
        try:
            font = ImageFont.truetype("arial.ttf", int(85 * scale))
        except:
            font = ImageFont.load_default()

    dollar_color = (46, 125, 50, 255)
    text = "$"
    bbox = draw.textbbox((0, 0), text, font=font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    text_x = center - text_width // 2
    text_y = center - text_height // 4
    draw.text((text_x, text_y), text, fill=dollar_color, font=font)

    # Small accent lines
    line_y1 = int(size * 0.58)
    line_width = max(1, int(4 * scale))
    accent_color = (129, 199, 132, 255)

    left_start = env_margin + int(26 * scale)
    left_end = center - int(32 * scale)
    right_start = center + int(32 * scale)
    right_end = size - env_margin - int(26 * scale)

    draw.line([(left_start, line_y1), (left_end, line_y1)], fill=accent_color, width=line_width)
    draw.line([(right_start, line_y1), (right_end, line_y1)], fill=accent_color, width=line_width)

    return img

def create_wide_tile(width: int, height: int) -> Image.Image:
    """Create a wide tile with the icon on the left and text space on right."""
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))

    # Create icon at height size and position it
    icon_size = int(height * 0.7)
    icon = create_budget_icon(icon_size, padding_ratio=0.05)

    # Position icon on the left
    icon_x = int(width * 0.15)
    icon_y = (height - icon_size) // 2
    img.paste(icon, (icon_x, icon_y), icon)

    return img

def create_splash_screen(width: int, height: int) -> Image.Image:
    """Create a splash screen with centered icon."""
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))

    # Create a larger icon centered
    icon_size = min(width, height) // 2
    icon = create_budget_icon(icon_size, padding_ratio=0.05)

    # Center the icon
    icon_x = (width - icon_size) // 2
    icon_y = (height - icon_size) // 2
    img.paste(icon, (icon_x, icon_y), icon)

    return img

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    assets_dir = os.path.join(project_root, "src", "NextLedger.App", "Assets")
    os.makedirs(assets_dir, exist_ok=True)

    # Square logos at various scales
    square_assets = [
        ("Square44x44Logo", 44),
        ("Square44x44Logo.targetsize-24_altform-unplated", 24),
        ("Square44x44Logo.targetsize-48_altform-unplated", 48),
        ("Square44x44Logo.targetsize-256_altform-unplated", 256),
        ("Square71x71Logo", 71),
        ("Square150x150Logo", 150),
        ("Square310x310Logo", 310),
        ("StoreLogo", 50),
        ("StoreLogo.scale-200", 100),
    ]

    for name, size in square_assets:
        img = create_budget_icon(size, padding_ratio=0.1)
        path = os.path.join(assets_dir, f"{name}.png")
        img.save(path, "PNG")
        print(f"Saved: {path}")

    # Wide tile
    wide = create_wide_tile(310, 150)
    wide_path = os.path.join(assets_dir, "Wide310x150Logo.png")
    wide.save(wide_path, "PNG")
    print(f"Saved: {wide_path}")

    # Splash screen
    splash = create_splash_screen(620, 300)
    splash_path = os.path.join(assets_dir, "SplashScreen.png")
    splash.save(splash_path, "PNG")
    print(f"Saved: {splash_path}")

    # Badge logo (small, simple)
    badge = create_budget_icon(24, padding_ratio=0.05)
    badge_path = os.path.join(assets_dir, "BadgeLogo.png")
    badge.save(badge_path, "PNG")
    print(f"Saved: {badge_path}")

    print("\nAll store assets generated successfully!")

if __name__ == "__main__":
    main()
