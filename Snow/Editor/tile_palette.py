from PyQt5.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QWidget, QSpinBox
from PyQt5.QtCore import Qt, pyqtSignal
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap

class TilePaletteFixed(QDialog):
    tileSelected = pyqtSignal(int)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Tile Palette - Grid Locked")
        self.setGeometry(100, 100, 520, 700)
        self.setWindowFlags(Qt.Window)
        
        self.tile_surfaces = []
        self.selected_tile = 0
        self.base_tile_size = 16
        
        self.display_tile_size = 48
        self.zoom = 3.0
        
        self.tileset_columns = 8
        self.tileset_rows = 0
        self.original_tileset_width = 0
        
        self.scroll_offset = 0
        self.tile_spacing = 4
        self.grid_padding = 20
        
        self.setMouseTracking(True)
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(10)
        
        header = QWidget()
        header.setStyleSheet("background-color: #2a2a2a; border-radius: 6px; padding: 8px;")
        header_layout = QVBoxLayout(header)
        header_layout.setSpacing(8)
        
        top_row = QWidget()
        top_layout = QHBoxLayout(top_row)
        top_layout.setContentsMargins(0, 0, 0, 0)
        
        title = QLabel("Tile Palette")
        title.setStyleSheet("font-weight: bold; font-size: 15pt; color: #e0e0e0;")
        top_layout.addWidget(title)
        
        top_layout.addStretch()
        
        zoom_out_btn = QPushButton("-")
        zoom_out_btn.setFixedSize(36, 36)
        zoom_out_btn.clicked.connect(self.zoom_out)
        zoom_out_btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 3px; 
                font-weight: bold; 
                font-size: 18pt; 
            } 
            QPushButton:hover { background-color: #484848; }
        """)
        top_layout.addWidget(zoom_out_btn)
        
        self.zoom_label = QLabel(f"{int(self.zoom * 100)}%")
        self.zoom_label.setFixedWidth(70)
        self.zoom_label.setAlignment(Qt.AlignCenter)
        self.zoom_label.setStyleSheet("color: #b0b0b0; font-size: 13pt; font-weight: bold;")
        top_layout.addWidget(self.zoom_label)
        
        zoom_in_btn = QPushButton("+")
        zoom_in_btn.setFixedSize(36, 36)
        zoom_in_btn.clicked.connect(self.zoom_in)
        zoom_in_btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 3px; 
                font-weight: bold; 
                font-size: 18pt; 
            } 
            QPushButton:hover { background-color: #484848; }
        """)
        top_layout.addWidget(zoom_in_btn)
        
        header_layout.addWidget(top_row)
        
        grid_row = QWidget()
        grid_layout = QHBoxLayout(grid_row)
        grid_layout.setContentsMargins(0, 0, 0, 0)
        
        grid_label = QLabel("Columns:")
        grid_label.setStyleSheet("color: #b0b0b0; font-size: 11pt;")
        grid_layout.addWidget(grid_label)
        
        self.columns_spin = QSpinBox()
        self.columns_spin.setRange(1, 32)
        self.columns_spin.setValue(self.tileset_columns)
        self.columns_spin.setStyleSheet("""
            QSpinBox { 
                background-color: #353535; 
                border: 1px solid #505050; 
                border-radius: 3px; 
                padding: 4px 8px;
                color: #e0e0e0;
                font-size: 11pt;
            }
        """)
        self.columns_spin.valueChanged.connect(self.on_columns_changed)
        grid_layout.addWidget(self.columns_spin)
        
        auto_detect_btn = QPushButton("Auto")
        auto_detect_btn.setFixedWidth(60)
        auto_detect_btn.clicked.connect(self.auto_detect_columns)
        auto_detect_btn.setStyleSheet("""
            QPushButton { 
                background-color: #3c7fb0; 
                border: 1px solid #5090c0; 
                border-radius: 3px; 
                padding: 4px 8px;
                color: white;
                font-size: 10pt;
            } 
            QPushButton:hover { background-color: #4c8fc0; }
        """)
        grid_layout.addWidget(auto_detect_btn)
        
        grid_layout.addStretch()
        
        self.info_label = QLabel("")
        self.info_label.setStyleSheet("color: #888; font-size: 10pt;")
        grid_layout.addWidget(self.info_label)
        
        header_layout.addWidget(grid_row)
        
        layout.addWidget(header)
        
        self.canvas = QWidget()
        self.canvas.setMinimumHeight(550)
        self.canvas.paintEvent = self.paint_canvas
        self.canvas.mousePressEvent = self.canvas_mouse_press
        self.canvas.wheelEvent = self.canvas_wheel
        layout.addWidget(self.canvas)
        
        self.setStyleSheet("QDialog { background-color: #1e1e1e; }")
    
    def set_tiles(self, tile_surfaces):
        print(f"TilePalette.set_tiles: {len(tile_surfaces)} tiles")
        self.tile_surfaces = tile_surfaces
        self.selected_tile = 0
        self.scroll_offset = 0
        
        self.auto_detect_columns()
        self.canvas.update()
    
    def auto_detect_columns(self):
        if not self.tile_surfaces:
            return
        
        total_tiles = len(self.tile_surfaces)
        
        common_widths = [8, 10, 12, 16, 20, 24, 32]
        best_columns = int(total_tiles ** 0.5)
        
        for width in common_widths:
            if total_tiles % width == 0:
                best_columns = width
                break
        
        self.tileset_columns = best_columns
        self.columns_spin.blockSignals(True)
        self.columns_spin.setValue(best_columns)
        self.columns_spin.blockSignals(False)
        
        self.calculate_layout()
        self.canvas.update()
        
        print(f"Auto-detected: {self.tileset_columns} columns")
    
    def on_columns_changed(self, value):
        self.tileset_columns = value
        self.calculate_layout()
        self.canvas.update()
    
    def calculate_layout(self):
        if not self.tile_surfaces or self.tileset_columns == 0:
            self.tileset_rows = 0
            return
        
        total_tiles = len(self.tile_surfaces)
        self.tileset_rows = (total_tiles + self.tileset_columns - 1) // self.tileset_columns
        
        self.info_label.setText(f"{self.tileset_columns}×{self.tileset_rows} = {total_tiles} tiles")
        
        print(f"Layout: {self.tileset_columns} cols × {self.tileset_rows} rows = {total_tiles} tiles")
    
    def update_display_size(self):
        self.display_tile_size = int(self.base_tile_size * self.zoom)
        self.zoom_label.setText(f"{int(self.zoom * 100)}%")
    
    def zoom_in(self):
        self.zoom = min(12.0, self.zoom * 1.5)
        self.update_display_size()
        self.canvas.update()
    
    def zoom_out(self):
        self.zoom = max(1.0, self.zoom / 1.5)
        self.update_display_size()
        self.canvas.update()
    
    def canvas_mouse_press(self, event):
        if event.button() == Qt.LeftButton and self.tile_surfaces:
            tile_index = self.get_tile_at_pos(event.pos())
            if 0 <= tile_index < len(self.tile_surfaces):
                self.selected_tile = tile_index
                self.tileSelected.emit(tile_index)
                self.canvas.update()
                print(f"Selected tile {tile_index}")
    
    def canvas_wheel(self, event):
        scroll_amount = event.angleDelta().y() // 120 * 60
        self.scroll_offset -= scroll_amount
        self.scroll_offset = max(0, self.scroll_offset)
        self.canvas.update()
    
    def get_tile_at_pos(self, pos):
        if self.tileset_columns == 0:
            return -1
        
        adjusted_x = pos.x() - self.grid_padding
        adjusted_y = pos.y() + self.scroll_offset - self.grid_padding
        
        if adjusted_x < 0 or adjusted_y < 0:
            return -1
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        col = adjusted_x // tile_total_size
        row = adjusted_y // tile_total_size
        
        if col < 0 or col >= self.tileset_columns:
            return -1
        
        tile_index = int(row * self.tileset_columns + col)
        
        if tile_index >= len(self.tile_surfaces):
            return -1
        
        return tile_index
    
    def paint_canvas(self, event):
        painter = QPainter(self.canvas)
        painter.setRenderHint(QPainter.Antialiasing, False)
        painter.fillRect(self.canvas.rect(), QColor(25, 25, 25))
        
        if not self.tile_surfaces or self.tileset_columns == 0:
            painter.setPen(QColor(140, 140, 140))
            font = painter.font()
            font.setPointSize(13)
            painter.setFont(font)
            painter.drawText(self.canvas.rect(), Qt.AlignCenter, 
                           "No tiles loaded\n\nLoad a tileset from File > Load Tileset")
            return
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        grid_width = self.tileset_columns * tile_total_size - self.tile_spacing
        grid_height = self.tileset_rows * tile_total_size - self.tile_spacing
        
        y_offset = self.grid_padding - self.scroll_offset
        x_offset = self.grid_padding
        
        painter.fillRect(
            x_offset - 4, 
            y_offset - 4, 
            grid_width + 8, 
            grid_height + 8,
            QColor(35, 35, 35)
        )
        
        painter.setPen(QPen(QColor(80, 120, 180, 120), 2))
        painter.drawRect(
            x_offset - 2, 
            y_offset - 2, 
            grid_width + 4, 
            grid_height + 4
        )
        
        start_row = max(0, int((self.scroll_offset - self.grid_padding) / tile_total_size))
        end_row = min(self.tileset_rows, int((self.scroll_offset + self.canvas.height()) / tile_total_size) + 2)
        
        for row in range(start_row, end_row):
            for col in range(self.tileset_columns):
                tile_index = row * self.tileset_columns + col
                
                if tile_index >= len(self.tile_surfaces):
                    break
                
                x = x_offset + col * tile_total_size
                y = y_offset + row * tile_total_size
                
                painter.fillRect(x, y, self.display_tile_size, self.display_tile_size, QColor(45, 45, 45))
                
                tile = self.tile_surfaces[tile_index]
                scaled_tile = tile.scaled(
                    self.display_tile_size, 
                    self.display_tile_size, 
                    Qt.KeepAspectRatio, 
                    Qt.FastTransformation
                )
                
                painter.drawPixmap(x, y, scaled_tile)
                
                if tile_index == self.selected_tile:
                    painter.setPen(QPen(QColor(80, 180, 255), 4))
                    painter.drawRect(x - 2, y - 2, self.display_tile_size + 4, self.display_tile_size + 4)
                else:
                    painter.setPen(QPen(QColor(70, 70, 70), 1))
                    painter.drawRect(x, y, self.display_tile_size, self.display_tile_size)
