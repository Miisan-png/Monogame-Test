from PyQt5.QtWidgets import QWidget
from PyQt5.QtCore import Qt, QRect, QPoint
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap
import numpy as np

class LevelCanvas(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.parent = parent
        self.setMinimumSize(800, 600)
        self.setFocusPolicy(Qt.StrongFocus)
        
        self.tile_size = 16
        self.zoom = 2.0
        self.min_zoom = 0.25
        self.max_zoom = 8.0
        
        self.camera_x = 0
        self.camera_y = 0
        
        self.grid_width = 100
        self.grid_height = 100
        self.world_data = np.zeros((self.grid_height, self.grid_width), dtype=int)
        self.collision_data = np.zeros((self.grid_height, self.grid_width), dtype=bool)
        
        self.show_grid = True
        self.show_viewport = True
        self.show_collision = True
        self.viewport_width = 320
        self.viewport_height = 180
        
        self.selected_tile = 0
        self.tool = "brush"
        self.painting = False
        self.erasing = False
        self.last_painted_pos = None
        
        self.rect_start = None
        self.rect_end = None
        self.rect_preview = False
        
        self.tileset_image = None
        self.tile_surfaces = []
        
        self.setMouseTracking(True)
        
    def set_tileset(self, image_path):
        print(f"Loading tileset from: {image_path}")
        self.tileset_image = QPixmap(image_path)
        if self.tileset_image.isNull():
            print("ERROR: Failed to load tileset image!")
            return
        print(f"Tileset loaded: {self.tileset_image.width()}x{self.tileset_image.height()}")
        self.extract_tiles()
        print(f"Extracted {len(self.tile_surfaces)} tiles")
        self.update()
    
    def extract_tiles(self):
        if not self.tileset_image:
            print("ERROR: No tileset image to extract from")
            return
        
        self.tile_surfaces = []
        tiles_x = self.tileset_image.width() // self.tile_size
        tiles_y = self.tileset_image.height() // self.tile_size
        
        print(f"Extracting tiles: {tiles_x}x{tiles_y} grid with tile_size={self.tile_size}")
        
        for y in range(tiles_y):
            for x in range(tiles_x):
                tile_rect = QRect(x * self.tile_size, y * self.tile_size, self.tile_size, self.tile_size)
                tile_pixmap = self.tileset_image.copy(tile_rect)
                if not tile_pixmap.isNull():
                    self.tile_surfaces.append(tile_pixmap)
        
        print(f"Successfully extracted {len(self.tile_surfaces)} tile surfaces")
    
    def set_tool(self, tool):
        self.tool = tool
        if tool == "brush":
            self.setCursor(Qt.CrossCursor)
        elif tool == "rect":
            self.setCursor(Qt.CrossCursor)
        elif tool == "fill":
            self.setCursor(Qt.PointingHandCursor)
        elif tool == "collision":
            self.setCursor(Qt.CrossCursor)
        elif tool == "eraser":
            self.setCursor(Qt.PointingHandCursor)
    
    def wheelEvent(self, event):
        old_zoom = self.zoom
        zoom_factor = 1.2 if event.angleDelta().y() > 0 else 1.0 / 1.2
        self.zoom = max(self.min_zoom, min(self.max_zoom, self.zoom * zoom_factor))
        
        if old_zoom != self.zoom:
            mouse_pos = event.pos()
            world_x = (mouse_pos.x() + self.camera_x) / old_zoom
            world_y = (mouse_pos.y() + self.camera_y) / old_zoom
            
            new_screen_x = world_x * self.zoom
            new_screen_y = world_y * self.zoom
            
            self.camera_x = new_screen_x - mouse_pos.x()
            self.camera_y = new_screen_y - mouse_pos.y()

        self.update()
        self.parent.update_status()
    
    def mousePressEvent(self, event):
        if event.button() == Qt.LeftButton:
            self.painting = True
            world_x, world_y = self.screen_to_world(event.pos())
            
            if self.tool == "brush":
                self.paint_tile(event.pos())
            elif self.tool == "rect":
                self.rect_start = (world_x, world_y)
                self.rect_preview = True
            elif self.tool == "fill":
                self.flood_fill(world_x, world_y)
            elif self.tool == "collision":
                self.toggle_collision(event.pos())
            elif self.tool == "eraser":
                self.erase_tile(event.pos())
                
        elif event.button() == Qt.RightButton:
            self.erasing = True
            if self.tool == "collision":
                self.remove_collision(event.pos())
            else:
                self.erase_tile(event.pos())
                
        elif event.button() == Qt.MiddleButton:
            self.setCursor(Qt.ClosedHandCursor)
            self.last_pan_pos = event.pos()
    
    def mouseMoveEvent(self, event):
        world_x, world_y = self.screen_to_world(event.pos())
        
        if self.painting:
            if self.tool == "brush":
                self.paint_tile(event.pos())
            elif self.tool == "rect":
                self.rect_end = (world_x, world_y)
                self.update()
            elif self.tool == "collision":
                self.add_collision(event.pos())
            elif self.tool == "eraser":
                self.erase_tile(event.pos())
                
        elif self.erasing:
            if self.tool == "collision":
                self.remove_collision(event.pos())
            else:
                self.erase_tile(event.pos())
                
        elif event.buttons() & Qt.MiddleButton:
            delta = event.pos() - self.last_pan_pos
            self.camera_x -= delta.x()
            self.camera_y -= delta.y()
            self.last_pan_pos = event.pos()
            self.update()
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            has_collision = self.collision_data[world_y, world_x]
            tile_id = self.world_data[world_y, world_x]
            self.parent.update_mouse_pos(world_x, world_y, tile_id, has_collision)
        else:
            self.parent.update_mouse_pos(world_x, world_y, 0, False)
    
    def mouseReleaseEvent(self, event):
        if event.button() == Qt.LeftButton:
            if self.tool == "rect" and self.rect_start and self.rect_end:
                self.paint_rectangle()
                self.rect_start = None
                self.rect_end = None
                self.rect_preview = False
                
            self.painting = False
            self.last_painted_pos = None
            
        elif event.button() == Qt.RightButton:
            self.erasing = False
            
        elif event.button() == Qt.MiddleButton:
            self.set_tool(self.tool)
    
    def keyPressEvent(self, event):
        move_speed = 40
        if event.key() == Qt.Key_W:
            self.camera_y -= move_speed
        elif event.key() == Qt.Key_S:
            self.camera_y += move_speed
        elif event.key() == Qt.Key_A:
            self.camera_x -= move_speed
        elif event.key() == Qt.Key_D:
            self.camera_x += move_speed
        elif event.key() == Qt.Key_G:
            self.show_grid = not self.show_grid
        elif event.key() == Qt.Key_V:
            self.show_viewport = not self.show_viewport
        elif event.key() == Qt.Key_C:
            self.show_collision = not self.show_collision
        elif event.key() == Qt.Key_R:
            self.reset_view()
        
        self.update()
        self.parent.update_status()
    
    def screen_to_world(self, screen_pos):
        scaled_tile_size = self.tile_size * self.zoom
        world_x = int((screen_pos.x() + self.camera_x) // scaled_tile_size)
        world_y = int((screen_pos.y() + self.camera_y) // scaled_tile_size)
        return world_x, world_y
    
    def paint_tile(self, pos):
        world_x, world_y = self.screen_to_world(pos)
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            current_pos = (world_x, world_y)
            if self.last_painted_pos != current_pos:
                self.world_data[world_y, world_x] = self.selected_tile + 1
                self.last_painted_pos = current_pos
                self.update()
    
    def paint_rectangle(self):
        if not self.rect_start or not self.rect_end:
            return
        
        x1, y1 = self.rect_start
        x2, y2 = self.rect_end
        
        min_x = max(0, min(x1, x2))
        max_x = min(self.grid_width - 1, max(x1, x2))
        min_y = max(0, min(y1, y2))
        max_y = min(self.grid_height - 1, max(y1, y2))
        
        for y in range(min_y, max_y + 1):
            for x in range(min_x, max_x + 1):
                self.world_data[y, x] = self.selected_tile + 1
        
        self.update()
    
    def flood_fill(self, start_x, start_y):
        if not (0 <= start_x < self.grid_width and 0 <= start_y < self.grid_height):
            return
        
        target_tile = self.world_data[start_y, start_x]
        fill_tile = self.selected_tile + 1
        
        if target_tile == fill_tile:
            return
        
        stack = [(start_x, start_y)]
        
        while stack:
            x, y = stack.pop()
            
            if not (0 <= x < self.grid_width and 0 <= y < self.grid_height):
                continue
            
            if self.world_data[y, x] != target_tile:
                continue
            
            self.world_data[y, x] = fill_tile
            
            stack.append((x + 1, y))
            stack.append((x - 1, y))
            stack.append((x, y + 1))
            stack.append((x, y - 1))
        
        self.update()
    
    def erase_tile(self, pos):
        world_x, world_y = self.screen_to_world(pos)
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            self.world_data[world_y, world_x] = 0
            self.update()
    
    def toggle_collision(self, pos):
        world_x, world_y = self.screen_to_world(pos)
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            self.collision_data[world_y, world_x] = not self.collision_data[world_y, world_x]
            self.update()
    
    def add_collision(self, pos):
        world_x, world_y = self.screen_to_world(pos)
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            self.collision_data[world_y, world_x] = True
            self.update()
    
    def remove_collision(self, pos):
        world_x, world_y = self.screen_to_world(pos)
        
        if 0 <= world_x < self.grid_width and 0 <= world_y < self.grid_height:
            self.collision_data[world_y, world_x] = False
            self.update()
    
    def reset_view(self):
        self.camera_x = 0
        self.camera_y = 0
        self.zoom = 2.0
        self.update()
        self.parent.update_status()
    
    def clear_world(self):
        self.world_data = np.zeros((self.grid_height, self.grid_width), dtype=int)
        self.update()
    
    def clear_collisions(self):
        self.collision_data = np.zeros((self.grid_height, self.grid_width), dtype=bool)
        self.update()
    
    def resize_world(self, width, height):
        old_world_data = self.world_data.copy()
        old_collision_data = self.collision_data.copy()
        
        self.grid_width = width
        self.grid_height = height
        self.world_data = np.zeros((height, width), dtype=int)
        self.collision_data = np.zeros((height, width), dtype=bool)
        
        copy_height = min(old_world_data.shape[0], height)
        copy_width = min(old_world_data.shape[1], width)
        
        self.world_data[:copy_height, :copy_width] = old_world_data[:copy_height, :copy_width]
        self.collision_data[:copy_height, :copy_width] = old_collision_data[:copy_height, :copy_width]
        
        self.update()
    
    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing, False)
        painter.fillRect(self.rect(), QColor(20, 20, 20))
        
        if not self.tile_surfaces:
            painter.setPen(QColor(120, 120, 120))
            font = painter.font()
            font.setPointSize(14)
            painter.setFont(font)
            painter.drawText(self.rect(), Qt.AlignCenter, "Load a tileset to begin editing\n\nFile > Load Tileset (Ctrl+T)")
            return
        
        scaled_tile_size = self.tile_size * self.zoom
        
        start_x = max(0, int(self.camera_x // scaled_tile_size))
        end_x = min(self.grid_width, int((self.camera_x + self.width()) // scaled_tile_size) + 2)
        start_y = max(0, int(self.camera_y // scaled_tile_size))
        end_y = min(self.grid_height, int((self.camera_y + self.height()) // scaled_tile_size) + 2)
        
        for y in range(start_y, end_y):
            for x in range(start_x, end_x):
                screen_x = x * scaled_tile_size - self.camera_x
                screen_y = y * scaled_tile_size - self.camera_y
                
                tile_id = self.world_data[y, x]
                if tile_id > 0 and tile_id <= len(self.tile_surfaces):
                    tile_pixmap = self.tile_surfaces[tile_id - 1]
                    scaled_pixmap = tile_pixmap.scaled(
                        int(scaled_tile_size), 
                        int(scaled_tile_size), 
                        Qt.KeepAspectRatio, 
                        Qt.FastTransformation
                    )
                    painter.drawPixmap(int(screen_x), int(screen_y), scaled_pixmap)
                
                if self.show_collision and self.collision_data[y, x]:
                    collision_color = QColor(255, 100, 150, 100)
                    painter.fillRect(int(screen_x), int(screen_y), int(scaled_tile_size), int(scaled_tile_size), collision_color)
                
                if self.show_grid and self.zoom >= 0.5:
                    painter.setPen(QColor(40, 40, 40))
                    painter.drawRect(int(screen_x), int(screen_y), int(scaled_tile_size), int(scaled_tile_size))
        
        if self.rect_preview and self.rect_start and self.rect_end:
            x1, y1 = self.rect_start
            x2, y2 = self.rect_end
            
            min_x = min(x1, x2)
            max_x = max(x1, x2)
            min_y = min(y1, y2)
            max_y = max(y1, y2)
            
            rect_screen_x = min_x * scaled_tile_size - self.camera_x
            rect_screen_y = min_y * scaled_tile_size - self.camera_y
            rect_width = (max_x - min_x + 1) * scaled_tile_size
            rect_height = (max_y - min_y + 1) * scaled_tile_size
            
            painter.setPen(QPen(QColor(80, 180, 255, 200), 2))
            painter.drawRect(int(rect_screen_x), int(rect_screen_y), int(rect_width), int(rect_height))
        
        if self.show_viewport:
            viewport_x = (self.viewport_width / 2) * self.zoom - self.camera_x
            viewport_y = (self.viewport_height / 2) * self.zoom - self.camera_y
            viewport_w = self.viewport_width * self.zoom
            viewport_h = self.viewport_height * self.zoom
            
            painter.setPen(QPen(QColor(255, 100, 100, 180), 2))
            painter.drawRect(int(viewport_x - viewport_w/2), int(viewport_y - viewport_h/2), 
                           int(viewport_w), int(viewport_h))
