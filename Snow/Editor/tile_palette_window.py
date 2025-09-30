from PyQt5.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QWidget
from PyQt5.QtCore import Qt, pyqtSignal, QRect
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap

class TilePaletteWindow(QDialog):
    tileSelected = pyqtSignal(int)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Tile Palette")
        self.setGeometry(100, 100, 400, 600)
        self.setWindowFlags(Qt.Window)
        
        self.tile_surfaces = []
        self.selected_tile = 0
        self.base_tile_size = 16
        self.display_tile_size = 48
        self.zoom = 3.0
        self.tiles_per_row = 6
        self.scroll_offset = 0
        self.tile_spacing = 4
        
        self.setMouseTracking(True)
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        header = QWidget()
        header_layout = QHBoxLayout(header)
        header_layout.setContentsMargins(0, 0, 0, 0)
        
        title = QLabel("Select Tile")
        title.setStyleSheet("font-weight: bold; font-size: 14px; color: #e0e0e0;")
        header_layout.addWidget(title)
        
        header_layout.addStretch()
        
        zoom_out_btn = QPushButton("-")
        zoom_out_btn.setFixedSize(32, 32)
        zoom_out_btn.clicked.connect(self.zoom_out)
        zoom_out_btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 3px; font-weight: bold; font-size: 16px; } QPushButton:hover { background-color: #484848; }")
        header_layout.addWidget(zoom_out_btn)
        
        self.zoom_label = QLabel(f"{int(self.zoom * 100)}%")
        self.zoom_label.setFixedWidth(60)
        self.zoom_label.setAlignment(Qt.AlignCenter)
        self.zoom_label.setStyleSheet("color: #b0b0b0; font-size: 14px; font-weight: bold;")
        header_layout.addWidget(self.zoom_label)
        
        zoom_in_btn = QPushButton("+")
        zoom_in_btn.setFixedSize(32, 32)
        zoom_in_btn.clicked.connect(self.zoom_in)
        zoom_in_btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 3px; font-weight: bold; font-size: 16px; } QPushButton:hover { background-color: #484848; }")
        header_layout.addWidget(zoom_in_btn)
        
        layout.addWidget(header)
        
        self.canvas = QWidget()
        self.canvas.setMinimumHeight(500)
        self.canvas.paintEvent = self.paint_canvas
        self.canvas.mousePressEvent = self.canvas_mouse_press
        self.canvas.wheelEvent = self.canvas_wheel
        layout.addWidget(self.canvas)
        
        self.setStyleSheet("QDialog { background-color: #1e1e1e; }")
    
    def set_tiles(self, tile_surfaces):
        print(f"TilePaletteWindow.set_tiles called with {len(tile_surfaces)} tiles")
        self.tile_surfaces = tile_surfaces
        self.selected_tile = 0
        self.scroll_offset = 0
        self.update_display_size()
        self.canvas.update()
        print(f"Palette window updated, tiles_per_row={self.tiles_per_row}, display_tile_size={self.display_tile_size}")
    
    def update_display_size(self):
        self.display_tile_size = int(self.base_tile_size * self.zoom)
        self.tiles_per_row = max(1, (self.canvas.width() - 20) // (self.display_tile_size + self.tile_spacing))
        self.zoom_label.setText(f"{int(self.zoom * 100)}%")
    
    def zoom_in(self):
        self.zoom = min(8.0, self.zoom * 1.5)
        self.update_display_size()
        self.canvas.update()
    
    def zoom_out(self):
        self.zoom = max(1.0, self.zoom / 1.5)
        self.update_display_size()
        self.canvas.update()
    
    def resizeEvent(self, event):
        super().resizeEvent(event)
        self.update_display_size()
    
    def canvas_mouse_press(self, event):
        if event.button() == Qt.LeftButton and self.tile_surfaces:
            tile_index = self.get_tile_at_pos(event.pos())
            if 0 <= tile_index < len(self.tile_surfaces):
                self.selected_tile = tile_index
                self.tileSelected.emit(tile_index)
                self.canvas.update()
                print(f"Selected tile: {tile_index}")
    
    def canvas_wheel(self, event):
        self.scroll_offset -= event.angleDelta().y() // 120 * 40
        self.scroll_offset = max(0, self.scroll_offset)
        self.canvas.update()
    
    def get_tile_at_pos(self, pos):
        adjusted_y = pos.y() + self.scroll_offset
        adjusted_x = pos.x() - 10
        
        if adjusted_y < 0 or adjusted_x < 0:
            return -1
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        row = adjusted_y // tile_total_size
        col = adjusted_x // tile_total_size
        return int(row * self.tiles_per_row + col)
    
    def paint_canvas(self, event):
        painter = QPainter(self.canvas)
        painter.fillRect(self.canvas.rect(), QColor(25, 25, 25))
        
        if not self.tile_surfaces:
            painter.setPen(QColor(140, 140, 140))
            font = painter.font()
            font.setPointSize(13)
            painter.setFont(font)
            painter.drawText(self.canvas.rect(), Qt.AlignCenter, "No tiles loaded\nLoad tileset from main window")
            return
        
        y_offset = -self.scroll_offset
        x_start = 10
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        for i, tile in enumerate(self.tile_surfaces):
            row = i // self.tiles_per_row
            col = i % self.tiles_per_row
            
            x = x_start + col * tile_total_size
            y = y_offset + row * tile_total_size
            
            if y + self.display_tile_size < 0 or y > self.canvas.height():
                continue
            
            painter.fillRect(x, y, self.display_tile_size, self.display_tile_size, QColor(40, 40, 40))
            
            scaled_tile = tile.scaled(
                self.display_tile_size, 
                self.display_tile_size, 
                Qt.KeepAspectRatio, 
                Qt.FastTransformation
            )
            
            painter.drawPixmap(x, y, scaled_tile)
            
            if i == self.selected_tile:
                painter.setPen(QPen(QColor(80, 180, 255), 4))
                painter.drawRect(x - 2, y - 2, self.display_tile_size + 4, self.display_tile_size + 4)
            else:
                painter.setPen(QPen(QColor(60, 60, 60), 1))
                painter.drawRect(x, y, self.display_tile_size, self.display_tile_size)








