from PyQt5.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel
from PyQt5.QtCore import Qt, pyqtSignal, QRect
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap

class TilePalette(QWidget):
    tileSelected = pyqtSignal(int)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setMinimumWidth(280)
        
        self.tile_surfaces = []
        self.selected_tile = 0
        self.base_tile_size = 16
        self.display_tile_size = 32
        self.zoom = 2.0
        self.tiles_per_row = 6
        self.scroll_offset = 0
        self.tile_spacing = 2
        
        self.setMouseTracking(True)
        
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        
        header = QWidget()
        header.setStyleSheet("background-color: #282828; padding: 8px;")
        header_layout = QHBoxLayout(header)
        header_layout.setContentsMargins(8, 4, 8, 4)
        
        title = QLabel("Tile Palette")
        title.setStyleSheet("font-weight: bold; font-size: 14px; color: #e0e0e0;")
        header_layout.addWidget(title)
        
        header_layout.addStretch()
        
        zoom_out_btn = QPushButton("-")
        zoom_out_btn.setFixedSize(28, 28)
        zoom_out_btn.clicked.connect(self.zoom_out)
        zoom_out_btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 3px; font-weight: bold; font-size: 14px; } QPushButton:hover { background-color: #484848; }")
        header_layout.addWidget(zoom_out_btn)
        
        self.zoom_label = QLabel(f"{int(self.zoom * 100)}%")
        self.zoom_label.setFixedWidth(50)
        self.zoom_label.setAlignment(Qt.AlignCenter)
        self.zoom_label.setStyleSheet("color: #b0b0b0; font-size: 13px;")
        header_layout.addWidget(self.zoom_label)
        
        zoom_in_btn = QPushButton("+")
        zoom_in_btn.setFixedSize(28, 28)
        zoom_in_btn.clicked.connect(self.zoom_in)
        zoom_in_btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 3px; font-weight: bold; font-size: 14px; } QPushButton:hover { background-color: #484848; }")
        header_layout.addWidget(zoom_in_btn)
        
        layout.addWidget(header)
        
        self.canvas_widget = QWidget()
        self.canvas_widget.setMinimumHeight(400)
        layout.addWidget(self.canvas_widget)
    
    def set_tiles(self, tile_surfaces):
        print(f"TilePalette.set_tiles called with {len(tile_surfaces)} tiles")
        self.tile_surfaces = tile_surfaces
        self.selected_tile = 0
        self.scroll_offset = 0
        self.update_display_size()
        self.update()
        print(f"Palette updated, tiles_per_row={self.tiles_per_row}, display_tile_size={self.display_tile_size}")
    
    def update_display_size(self):
        self.display_tile_size = int(self.base_tile_size * self.zoom)
        self.tiles_per_row = max(1, (self.width() - 20) // (self.display_tile_size + self.tile_spacing))
        self.zoom_label.setText(f"{int(self.zoom * 100)}%")
    
    def zoom_in(self):
        self.zoom = min(8.0, self.zoom * 1.5)
        self.update_display_size()
        self.update()
    
    def zoom_out(self):
        self.zoom = max(0.5, self.zoom / 1.5)
        self.update_display_size()
        self.update()
    
    def resizeEvent(self, event):
        super().resizeEvent(event)
        self.update_display_size()
    
    def mousePressEvent(self, event):
        if event.button() == Qt.LeftButton and self.tile_surfaces:
            tile_index = self.get_tile_at_pos(event.pos())
            if 0 <= tile_index < len(self.tile_surfaces):
                self.selected_tile = tile_index
                self.tileSelected.emit(tile_index)
                self.update()
    
    def wheelEvent(self, event):
        self.scroll_offset -= event.angleDelta().y() // 120 * 30
        self.scroll_offset = max(0, self.scroll_offset)
        self.update()
    
    def get_tile_at_pos(self, pos):
        header_height = 40
        adjusted_y = pos.y() - header_height + self.scroll_offset
        adjusted_x = pos.x() - 10
        
        if adjusted_y < 0 or adjusted_x < 0:
            return -1
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        row = adjusted_y // tile_total_size
        col = adjusted_x // tile_total_size
        return int(row * self.tiles_per_row + col)
    
    def paintEvent(self, event):
        painter = QPainter(self)
        painter.fillRect(self.rect(), QColor(20, 20, 20))
        
        if not self.tile_surfaces:
            painter.setPen(QColor(120, 120, 120))
            painter.setFont(painter.font())
            font = painter.font()
            font.setPointSize(12)
            painter.setFont(font)
            text_rect = self.rect()
            text_rect.moveTop(text_rect.top() + 60)
            painter.drawText(text_rect, Qt.AlignCenter, "No tiles loaded\nClick 'Load Tileset'")
            return
        
        header_height = 40
        y_offset = header_height - self.scroll_offset
        x_start = 10
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        for i, tile in enumerate(self.tile_surfaces):
            row = i // self.tiles_per_row
            col = i % self.tiles_per_row
            
            x = x_start + col * tile_total_size
            y = y_offset + row * tile_total_size
            
            if y + self.display_tile_size < header_height or y > self.height():
                continue
            
            scaled_tile = tile.scaled(
                self.display_tile_size, 
                self.display_tile_size, 
                Qt.KeepAspectRatio, 
                Qt.FastTransformation
            )
            
            painter.fillRect(x, y, self.display_tile_size, self.display_tile_size, QColor(35, 35, 35))
            painter.drawPixmap(x, y, scaled_tile)
            
            if i == self.selected_tile:
                painter.setPen(QPen(QColor(80, 180, 255), 3))
                painter.drawRect(x - 2, y - 2, self.display_tile_size + 4, self.display_tile_size + 4)
            else:
                painter.setPen(QPen(QColor(50, 50, 50), 1))
                painter.drawRect(x, y, self.display_tile_size, self.display_tile_size)








