from PyQt5.QtWidgets import QWidget
from PyQt5.QtCore import Qt, QPoint, pyqtSignal
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap, QFont
import json
import os

class SceneCanvas(QWidget):
    entitySelected = pyqtSignal(object)
    entityMoved = pyqtSignal(object)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.parent = parent
        self.setMinimumSize(800, 600)
        self.setFocusPolicy(Qt.StrongFocus)
        
        self.zoom = 2.0
        self.min_zoom = 0.25
        self.max_zoom = 8.0
        
        self.camera_x = 0
        self.camera_y = 0
        
        self.scene_data = None
        self.level_data = None
        self.tileset_image = None
        self.tile_surfaces = []
        self.tile_size = 16
        
        self.show_grid = True
        self.show_tiles = True
        self.show_collision = False
        
        self.entities = []
        self.selected_entity = None
        self.dragging = False
        self.drag_start = None
        self.entity_drag_offset = None
        
        self.entity_tool = "PlayerSpawn"
        
        self.setMouseTracking(True)
        
        self.entity_colors = {
            "PlayerSpawn": QColor(100, 255, 100),
            "Slime": QColor(100, 200, 100),
            "Coin": QColor(255, 215, 0),
            "Chest": QColor(139, 69, 19),
            "Spike": QColor(150, 150, 150),
        }
    
    def set_scene_data(self, scene_data):
        self.scene_data = scene_data
        self.entities = scene_data.entities if scene_data else []
        self.load_level_background()
        self.update()
    
    def load_level_background(self):
        if not self.scene_data or not self.scene_data.tilemap:
            return
        
        tilemap_path = self.scene_data.tilemap
        tileset_path = self.scene_data.tileset
        
        if not os.path.isabs(tilemap_path):
            script_dir = os.path.dirname(os.path.abspath(__file__))
            project_root = os.path.dirname(script_dir)
            tilemap_path = os.path.join(project_root, tilemap_path)
        
        if tileset_path and not os.path.isabs(tileset_path):
            script_dir = os.path.dirname(os.path.abspath(__file__))
            project_root = os.path.dirname(script_dir)
            tileset_path = os.path.join(project_root, tileset_path)
        
        if not os.path.exists(tilemap_path):
            print(f"Tilemap not found: {tilemap_path}")
            return
        
        try:
            with open(tilemap_path, 'r') as f:
                self.level_data = json.load(f)
            
            self.tile_size = self.level_data.get('tile_size', 16)
            
            if tileset_path and os.path.exists(tileset_path):
                self.load_tileset(tileset_path)
            else:
                print(f"Tileset not found: {tileset_path}")
            
            print(f"Loaded level: {self.level_data['grid_width']}x{self.level_data['grid_height']}")
        except Exception as e:
            print(f"Failed to load level: {e}")
    
    def load_tileset(self, tileset_path):
        self.tileset_image = QPixmap(tileset_path)
        if self.tileset_image.isNull():
            print(f"Failed to load tileset: {tileset_path}")
            return
        
        self.extract_tiles()
        print(f"Tileset loaded: {len(self.tile_surfaces)} tiles")
    
    def extract_tiles(self):
        if not self.tileset_image:
            return
        
        self.tile_surfaces = []
        tiles_x = self.tileset_image.width() // self.tile_size
        tiles_y = self.tileset_image.height() // self.tile_size
        
        for y in range(tiles_y):
            for x in range(tiles_x):
                tile_pixmap = self.tileset_image.copy(
                    x * self.tile_size, 
                    y * self.tile_size, 
                    self.tile_size, 
                    self.tile_size
                )
                self.tile_surfaces.append(tile_pixmap)
    
    def set_entity_tool(self, entity_type):
        self.entity_tool = entity_type
        self.setCursor(Qt.CrossCursor)
    
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
        if self.parent:
            self.parent.update_status()
    
    def mousePressEvent(self, event):
        if event.button() == Qt.LeftButton:
            world_pos = self.screen_to_world(event.pos())
            
            clicked_entity = self.get_entity_at_position(world_pos)
            
            if clicked_entity:
                self.selected_entity = clicked_entity
                self.dragging = True
                self.entity_drag_offset = QPoint(
                    int(world_pos[0] - clicked_entity.x),
                    int(world_pos[1] - clicked_entity.y)
                )
                self.entitySelected.emit(clicked_entity)
            else:
                self.place_entity(world_pos)
        
        elif event.button() == Qt.MiddleButton:
            self.setCursor(Qt.ClosedHandCursor)
            self.last_pan_pos = event.pos()
        
        elif event.button() == Qt.RightButton:
            world_pos = self.screen_to_world(event.pos())
            clicked_entity = self.get_entity_at_position(world_pos)
            if clicked_entity:
                self.delete_entity(clicked_entity)
    
    def mouseMoveEvent(self, event):
        if self.dragging and self.selected_entity:
            world_pos = self.screen_to_world(event.pos())
            self.selected_entity.x = world_pos[0] - self.entity_drag_offset.x()
            self.selected_entity.y = world_pos[1] - self.entity_drag_offset.y()
            self.entityMoved.emit(self.selected_entity)
            self.update()
        
        elif event.buttons() & Qt.MiddleButton:
            delta = event.pos() - self.last_pan_pos
            self.camera_x -= delta.x()
            self.camera_y -= delta.y()
            self.last_pan_pos = event.pos()
            self.update()
        
        world_pos = self.screen_to_world(event.pos())
        if self.parent:
            self.parent.update_mouse_pos(int(world_pos[0]), int(world_pos[1]))
    
    def mouseReleaseEvent(self, event):
        if event.button() == Qt.LeftButton:
            self.dragging = False
            self.entity_drag_offset = None
        elif event.button() == Qt.MiddleButton:
            self.setCursor(Qt.ArrowCursor)
    
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
        elif event.key() == Qt.Key_Delete:
            if self.selected_entity:
                self.delete_entity(self.selected_entity)
        elif event.key() == Qt.Key_R:
            self.reset_view()
        elif event.key() == Qt.Key_G:
            self.show_grid = not self.show_grid
        elif event.key() == Qt.Key_T:
            self.show_tiles = not self.show_tiles
        
        self.update()
    
    def screen_to_world(self, screen_pos):
        world_x = (screen_pos.x() + self.camera_x) / self.zoom
        world_y = (screen_pos.y() + self.camera_y) / self.zoom
        return (world_x, world_y)
    
    def world_to_screen(self, world_x, world_y):
        screen_x = world_x * self.zoom - self.camera_x
        screen_y = world_y * self.zoom - self.camera_y
        return (int(screen_x), int(screen_y))
    
    def get_entity_at_position(self, world_pos):
        entity_size = 16
        for entity in reversed(self.entities):
            if (entity.x <= world_pos[0] <= entity.x + entity_size and
                entity.y <= world_pos[1] <= entity.y + entity_size):
                return entity
        return None
    
    def place_entity(self, world_pos):
        if not self.entity_tool:
            return
        
        from scene_data import EntityData
        entity_id = f"{self.entity_tool.lower()}_{len([e for e in self.entities if e.type == self.entity_tool]) + 1}"
        
        entity = EntityData(entity_id, self.entity_tool, int(world_pos[0]), int(world_pos[1]))
        
        if self.entity_tool == "Slime":
            entity.properties = {"patrol_distance": 50, "speed": 30}
        elif self.entity_tool == "Coin":
            entity.properties = {"value": 10}
        
        self.entities.append(entity)
        self.selected_entity = entity
        self.entitySelected.emit(entity)
        self.update()
    
    def delete_entity(self, entity):
        if entity in self.entities:
            self.entities.remove(entity)
            if self.selected_entity == entity:
                self.selected_entity = None
                self.entitySelected.emit(None)
            self.update()
    
    def reset_view(self):
        self.camera_x = 0
        self.camera_y = 0
        self.zoom = 2.0
        self.update()
        if self.parent:
            self.parent.update_status()
    
    def paintEvent(self, event):
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing, False)
        
        if self.scene_data and self.scene_data.background_color:
            bg_color = self.parse_color(self.scene_data.background_color)
            painter.fillRect(self.rect(), bg_color)
        else:
            painter.fillRect(self.rect(), QColor(20, 20, 20))
        
        if self.show_tiles and self.level_data and self.tile_surfaces:
            self.draw_tiles(painter)
        
        if self.show_grid and self.level_data and self.zoom >= 0.5:
            self.draw_grid(painter)
        
        self.draw_entities(painter)
        
        if not self.scene_data:
            painter.setPen(QColor(120, 120, 120))
            font = painter.font()
            font.setPointSize(14)
            painter.setFont(font)
            painter.drawText(self.rect(), Qt.AlignCenter, 
                           "Switch to Scene Mode and create a scene")
    
    def draw_tiles(self, painter):
        scaled_tile_size = self.tile_size * self.zoom
        world_data = self.level_data['world_data']
        grid_width = self.level_data['grid_width']
        grid_height = self.level_data['grid_height']
        
        start_x = max(0, int(self.camera_x // scaled_tile_size))
        end_x = min(grid_width, int((self.camera_x + self.width()) // scaled_tile_size) + 2)
        start_y = max(0, int(self.camera_y // scaled_tile_size))
        end_y = min(grid_height, int((self.camera_y + self.height()) // scaled_tile_size) + 2)
        
        for y in range(start_y, end_y):
            for x in range(start_x, end_x):
                tile_id = world_data[y][x]
                if tile_id > 0 and tile_id <= len(self.tile_surfaces):
                    screen_pos = self.world_to_screen(x * self.tile_size, y * self.tile_size)
                    tile_pixmap = self.tile_surfaces[tile_id - 1]
                    scaled_pixmap = tile_pixmap.scaled(
                        int(scaled_tile_size), 
                        int(scaled_tile_size),
                        Qt.KeepAspectRatio,
                        Qt.FastTransformation
                    )
                    painter.drawPixmap(screen_pos[0], screen_pos[1], scaled_pixmap)
    
    def draw_grid(self, painter):
        scaled_tile_size = self.tile_size * self.zoom
        grid_width = self.level_data['grid_width']
        grid_height = self.level_data['grid_height']
        
        painter.setPen(QPen(QColor(60, 60, 60), 1))
        
        start_x = max(0, int(self.camera_x // scaled_tile_size))
        end_x = min(grid_width, int((self.camera_x + self.width()) // scaled_tile_size) + 2)
        start_y = max(0, int(self.camera_y // scaled_tile_size))
        end_y = min(grid_height, int((self.camera_y + self.height()) // scaled_tile_size) + 2)
        
        for y in range(start_y, end_y + 1):
            screen_pos = self.world_to_screen(0, y * self.tile_size)
            end_pos = self.world_to_screen(grid_width * self.tile_size, y * self.tile_size)
            painter.drawLine(screen_pos[0], screen_pos[1], end_pos[0], end_pos[1])
        
        for x in range(start_x, end_x + 1):
            screen_pos = self.world_to_screen(x * self.tile_size, 0)
            end_pos = self.world_to_screen(x * self.tile_size, grid_height * self.tile_size)
            painter.drawLine(screen_pos[0], screen_pos[1], end_pos[0], end_pos[1])
    
    def draw_entities(self, painter):
        font = QFont("Arial", 9, QFont.Bold)
        painter.setFont(font)
        
        for entity in self.entities:
            screen_pos = self.world_to_screen(entity.x, entity.y)
            size = int(16 * self.zoom)
            
            color = self.entity_colors.get(entity.type, QColor(200, 200, 200))
            painter.fillRect(screen_pos[0], screen_pos[1], size, size, color)
            
            if entity == self.selected_entity:
                painter.setPen(QPen(QColor(255, 255, 0), 3))
            else:
                painter.setPen(QPen(QColor(255, 255, 255, 200), 2))
            
            painter.drawRect(screen_pos[0], screen_pos[1], size, size)
            
            if self.zoom >= 1.0:
                painter.setPen(QColor(0, 0, 0))
                text_rect = painter.boundingRect(screen_pos[0], screen_pos[1] + size + 2, 
                                                 200, 20, Qt.AlignLeft, entity.type)
                painter.fillRect(text_rect.adjusted(-2, -1, 2, 1), QColor(0, 0, 0, 200))
                painter.setPen(QColor(255, 255, 255))
                painter.drawText(screen_pos[0], screen_pos[1] + size + 14, entity.type)
    
    def parse_color(self, color_str):
        if color_str.startswith('#'):
            color_str = color_str[1:]
            if len(color_str) == 6:
                r = int(color_str[0:2], 16)
                g = int(color_str[2:4], 16)
                b = int(color_str[4:6], 16)
                return QColor(r, g, b)
        return QColor(20, 20, 20)
