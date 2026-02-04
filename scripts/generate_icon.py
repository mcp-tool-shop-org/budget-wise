"""Generate NextLedger app icon in ICO format using Pillow."""
from PIL import Image, ImageDraw, ImageFont
import os

def create_budget_icon(size: int) -> Image.Image:
    """Create a budget/envelope themed icon at the specified size."""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Calculate proportions
    margin = size // 16
    center = size // 2

    # Background circle with gradient effect (simplified to solid green)
    circle_radius = size // 2 - margin
    for i in range(circle_radius, 0, -1):
        # Gradient from light green to dark green
        ratio = i / circle_radius
        r = int(46 + (76 - 46) * ratio)  # 2E to 4C
        g = int(125 + (175 - 125) * ratio)  # 7D to AF
        b = int(50 + (80 - 50) * ratio)  # 32 to 50
        draw.ellipse(
            [center - i, center - i, center + i, center + i],
            fill=(r, g, b, 255)
        )

    # White envelope rectangle
    env_margin = size // 5
    env_top = size // 4
    env_bottom = size - size // 6
    corner_radius = size // 20

    # Draw rounded rectangle for envelope
    draw.rounded_rectangle(
        [env_margin, env_top, size - env_margin, env_bottom],
        radius=corner_radius,
        fill=(255, 255, 255, 245)
    )

    # Envelope flap (V shape)
    flap_points = [
        (env_margin, env_top + size // 16),
        (center, center - size // 16),
        (size - env_margin, env_top + size // 16)
    ]
    draw.line(flap_points, fill=(76, 175, 80, 255), width=max(2, size // 40))

    # Dollar sign
    try:
        # Try to use a nice font
        font_size = size // 3
        font = ImageFont.truetype("segoeui.ttf", font_size)
    except:
        try:
            font = ImageFont.truetype("arial.ttf", size // 3)
        except:
            font = ImageFont.load_default()

    dollar_color = (46, 125, 50, 255)  # Dark green
    text = "$"
    bbox = draw.textbbox((0, 0), text, font=font)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    text_x = center - text_width // 2
    text_y = center - text_height // 4
    draw.text((text_x, text_y), text, fill=dollar_color, font=font)

    # Small accent lines (budget rows)
    line_y1 = int(size * 0.58)
    line_y2 = int(size * 0.68)
    line_width = max(1, size // 50)
    accent_color = (129, 199, 132, 255)  # Light green

    # Left lines
    draw.line([(env_margin + size//10, line_y1), (center - size//8, line_y1)],
              fill=accent_color, width=line_width)
    # Right lines
    draw.line([(center + size//8, line_y1), (size - env_margin - size//10, line_y1)],
              fill=accent_color, width=line_width)

    return img

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    assets_dir = os.path.join(project_root, "src", "NextLedger.App", "Assets")
    os.makedirs(assets_dir, exist_ok=True)

    # Generate icons at multiple sizes for ICO
    sizes = [16, 24, 32, 48, 64, 128, 256]
    images = []

    for size in sizes:
        img = create_budget_icon(size)
        images.append(img)
        # Also save individual PNGs for other uses
        if size in [32, 256]:
            png_path = os.path.join(assets_dir, f"NextLedger_{size}.png")
            img.save(png_path, "PNG")
            print(f"Saved: {png_path}")

    # Save as ICO with all sizes
    ico_path = os.path.join(assets_dir, "NextLedger.ico")
    images[0].save(
        ico_path,
        format='ICO',
        sizes=[(img.width, img.height) for img in images],
        append_images=images[1:]
    )
    print(f"Saved: {ico_path}")

    # Also save to project root for easy access
    root_ico = os.path.join(project_root, "src", "NextLedger.App", "NextLedger.ico")
    images[0].save(
        root_ico,
        format='ICO',
        sizes=[(img.width, img.height) for img in images],
        append_images=images[1:]
    )
    print(f"Saved: {root_ico}")

if __name__ == "__main__":
    main()
