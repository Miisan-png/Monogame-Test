import os
import struct

def get_png_size(path):
    """Read PNG width and height directly from the file header."""
    with open(path, "rb") as f:
        header = f.read(24)
        if header.startswith(b"\211PNG\r\n\032\n") and header[12:16] == b"IHDR":
            width, height = struct.unpack(">II", header[16:24])
            return width, height
    return None, None

def scan_icons(base_path="data"):
    print("=" * 80)
    print("SNOW ENGINE - ICON ASSET SCANNER")
    print("=" * 80)
    
    icon_stats = {
        'total_files': 0,
        'png_files': 0,
        'ui_files': 0,
        'folders': 0,
        'sizes': {}
    }
    
    for root, dirs, files in os.walk(base_path):
        icon_stats['folders'] += len(dirs)
        
        for file in files:
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, base_path)
            
            icon_stats['total_files'] += 1
            
            if file.lower().endswith('.png'):
                icon_stats['png_files'] += 1
                try:
                    w, h = get_png_size(full_path)
                    if w and h:
                        size = f"{w}x{h}"
                        icon_stats['sizes'][size] = icon_stats['sizes'].get(size, 0) + 1
                        print(f"[PNG] {rel_path:<60} {size:>10}")
                    else:
                        print(f"[ERROR] {rel_path:<60} Invalid PNG")
                except Exception:
                    print(f"[ERROR] {rel_path:<60} Cannot read")
            
            elif file.lower().endswith('.ui'):
                icon_stats['ui_files'] += 1
                print(f"[UI]  {rel_path:<60}")
    
    print("\n" + "=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Total Files:    {icon_stats['total_files']}")
    print(f"PNG Images:     {icon_stats['png_files']}")
    print(f"UI Files:       {icon_stats['ui_files']}")
    print(f"Folders:        {icon_stats['folders']}")
    print("\nImage Sizes:")
    for size, count in sorted(icon_stats['sizes'].items()):
        print(f"  {size:>10}: {count:>3} images")
    print("=" * 80)

if __name__ == "__main__":
    if os.path.exists("data"):
        scan_icons("data")
    else:
        print("ERROR: 'data' folder not found. Run this script from Snow/Editor directory.")
