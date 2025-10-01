from PyQt5.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QSpinBox, QScrollArea
from PyQt5.QtCore import Qt, pyqtSignal, QSize
from PyQt5.QtGui import QPainter, QColor, QPen, QPixmap, QIcon
import os

class TilePaletteWindow(QWidget):
    tileSelected = pyqtSignal(int)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setMinimumSize(300, 400)
        
        self.tile_surfaces = []
        self.selected_tile = 0
        self.base_tile_size = 16
        self.original_tileset_width = 0
        self.original_tileset_columns = 8
        
        self.display_tile_size = 32
        self.zoom = 2.0
        
        self.scroll_offset = 0
        self.tile_spacing = 2
        self.grid_padding = 12
        
        script_dir = os.path.dirname(os.path.abspath(__file__))
        self.icon_path = os.path.join(script_dir, "data", "icons")
        
        self.setMouseTracking(True)
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        header = QWidget()
        header.setStyleSheet("background-color: #2a2a2a; border-radius: 4px; padding: 8px;")
        header_layout = QVBoxLayout(header)
        header_layout.setSpacing(8)
        header_layout.setContentsMargins(8, 8, 8, 8)
        
        top_row = QWidget()
        top_layout = QHBoxLayout(top_row)
        top_layout.setContentsMargins(0, 0, 0, 0)
        top_layout.setSpacing(8)
        
        title = QLabel("Tile Palette")
        title.setStyleSheet("font-weight: bold; font-size: 13pt; color: #e0e0e0;")
        top_layout.addWidget(title)
        
        top_layout.addStretch()
        
        zoom_out_btn = QPushButton(self.get_icon("arrow-down.png"), "")
        zoom_out_btn.setFixedSize(28, 28)
        zoom_out_btn.setIconSize(QSize(16, 16))
        zoom_out_btn.setToolTip("Zoom Out")
        zoom_out_btn.clicked.connect(self.zoom_out)
        zoom_out_btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 3px;
            } 
            QPushButton:hover { background-color: #484848; }
        """)
        top_layout.addWidget(zoom_out_btn)
        
        self.zoom_label = QLabel(f"{int(self.zoom * 100)}%")
        self.zoom_label.setFixedWidth(50)
        self.zoom_label.setAlignment(Qt.AlignCenter)
        self.zoom_label.setStyleSheet("color: #b0b0b0; font-size: 10pt; font-weight: bold;")
        top_layout.addWidget(self.zoom_label)
        
        zoom_in_btn = QPushButton(self.get_icon("arrow-up.png"), "")
        zoom_in_btn.setFixedSize(28, 28)
        zoom_in_btn.setIconSize(QSize(16, 16))
        zoom_in_btn.setToolTip("Zoom In")
        zoom_in_btn.clicked.connect(self.zoom_in)
        zoom_in_btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 3px;
            } 
            QPushButton:hover { background-color: #484848; }
        """)
        top_layout.addWidget(zoom_in_btn)
        
        header_layout.addWidget(top_row)
        
        grid_row = QWidget()
        grid_layout = QHBoxLayout(grid_row)
        grid_layout.setContentsMargins(0, 0, 0, 0)
        grid_layout.setSpacing(8)
        
        grid_icon_label = QLabel()
        grid_icon_pixmap = QPixmap(os.path.join(self.icon_path, "grid.png"))
        if not grid_icon_pixmap.isNull():
            grid_icon_label.setPixmap(grid_icon_pixmap.scaled(16, 16, Qt.KeepAspectRatio, Qt.SmoothTransformation))
        grid_layout.addWidget(grid_icon_label)
        
        grid_label = QLabel("Columns:")
        grid_label.setStyleSheet("color: #b0b0b0; font-size: 10pt;")
        grid_layout.addWidget(grid_label)
        
        self.columns_spin = QSpinBox()
        self.columns_spin.setRange(1, 64)
        self.columns_spin.setValue(self.original_tileset_columns)
        self.columns_spin.setFixedWidth(80)
        self.columns_spin.setStyleSheet("""
            QSpinBox { 
                background-color: #353535; 
                border: 1px solid #505050; 
                border-radius: 3px; 
                padding: 3px 6px;
                color: #e0e0e0;
                font-size: 10pt;
            }
        """)
        self.columns_spin.valueChanged.connect(self.on_columns_changed)
        grid_layout.addWidget(self.columns_spin)
        
        auto_detect_btn = QPushButton(self.get_icon("refresh.png"), "Auto")
        auto_detect_btn.setIconSize(QSize(14, 14))
        auto_detect_btn.setFixedHeight(26)
        auto_detect_btn.setToolTip("Auto-detect columns from tileset")
        auto_detect_btn.clicked.connect(self.auto_detect_columns)
        auto_detect_btn.setStyleSheet("""
            QPushButton { 
                background-color: #3c7fb0; 
                border: 1px solid #5090c0; 
                border-radius: 3px; 
                padding: 3px 8px;
                color: white;
                font-size: 10pt;
            } 
            QPushButton:hover { background-color: #4c8fc0; }
        """)
        grid_layout.addWidget(auto_detect_btn)
        
        grid_layout.addStretch()
        
        self.info_label = QLabel("")
        self.info_label.setStyleSheet("color: #888; font-size: 9pt;")
        grid_layout.addWidget(self.info_label)
        
        header_layout.addWidget(grid_row)
        
        layout.addWidget(header)
        
        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        scroll_area.setStyleSheet("""
            QScrollArea { 
                background-color: #1a1a1a; 
                border: 1px solid #404040;
                border-radius: 4px;
            }
        """)
        
        self.canvas = QWidget()
        self.canvas.setMinimumHeight(400)
        self.canvas.paintEvent = self.paint_canvas
        self.canvas.mousePressEvent = self.canvas_mouse_press
        
        scroll_area.setWidget(self.canvas)
        layout.addWidget(scroll_area)
        
        self.scroll_area = scroll_area
    
    def get_icon(self, icon_name):
        icon_full_path = os.path.join(self.icon_path, icon_name)
        if os.path.exists(icon_full_path):
            return QIcon(icon_full_path)
        return QIcon()
    
    def set_tiles(self, tile_surfaces):
        self.tile_surfaces = tile_surfaces
        self.selected_tile = 0
        
        if tile_surfaces and len(tile_surfaces) > 0:
            self.base_tile_size = tile_surfaces[0].width()
        
        self.auto_detect_columns()
        self.update_display_size()
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
        
        self.original_tileset_columns = best_columns
        self.columns_spin.blockSignals(True)
        self.columns_spin.setValue(best_columns)
        self.columns_spin.blockSignals(False)
        
        self.update_info_label()
        self.canvas.update()
    
    def on_columns_changed(self, value):
        self.original_tileset_columns = value
        self.update_info_label()
        self.canvas.update()
    
    def update_info_label(self):
        if not self.tile_surfaces or self.original_tileset_columns == 0:
            return
        
        total_tiles = len(self.tile_surfaces)
        rows = (total_tiles + self.original_tileset_columns - 1) // self.original_tileset_columns
        self.info_label.setText(f"{self.original_tileset_columns}×{rows} = {total_tiles}")
    
    def update_display_size(self):
        self.display_tile_size = int(self.base_tile_size * self.zoom)
        self.zoom_label.setText(f"{int(self.zoom * 100)}%")
        
        if self.tile_surfaces:
            total_tiles = len(self.tile_surfaces)
            rows = (total_tiles + self.original_tileset_columns - 1) // self.original_tileset_columns
            tile_total_size = self.display_tile_size + self.tile_spacing
            
            canvas_height = self.grid_padding * 2 + rows * tile_total_size
            self.canvas.setMinimumHeight(max(400, canvas_height))
    
    def zoom_in(self):
        self.zoom = min(8.0, self.zoom * 1.4)
        self.update_display_size()
        self.canvas.update()
    
    def zoom_out(self):
        self.zoom = max(0.5, self.zoom / 1.4)
        self.update_display_size()
        self.canvas.update()
    
    def canvas_mouse_press(self, event):
        if event.button() == Qt.LeftButton and self.tile_surfaces:
            tile_index = self.get_tile_at_pos(event.pos())
            if 0 <= tile_index < len(self.tile_surfaces):
                self.selected_tile = tile_index
                self.tileSelected.emit(tile_index)
                self.canvas.update()
    
    def get_tile_at_pos(self, pos):
        if self.original_tileset_columns == 0:
            return -1
        
        adjusted_x = pos.x() - self.grid_padding
        adjusted_y = pos.y() - self.grid_padding
        
        if adjusted_x < 0 or adjusted_y < 0:
            return -1
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        col = adjusted_x // tile_total_size
        row = adjusted_y // tile_total_size
        
        if col < 0 or col >= self.original_tileset_columns:
            return -1
        
        tile_index = int(row * self.original_tileset_columns + col)
        
        if tile_index >= len(self.tile_surfaces):
            return -1
        
        return tile_index
    
    def paint_canvas(self, event):
        painter = QPainter(self.canvas)
        painter.setRenderHint(QPainter.Antialiasing, False)
        painter.fillRect(self.canvas.rect(), QColor(20, 20, 20))
        
        if not self.tile_surfaces or self.original_tileset_columns == 0:
            painter.setPen(QColor(120, 120, 120))
            font = painter.font()
            font.setPointSize(12)
            painter.setFont(font)
            painter.drawText(self.canvas.rect(), Qt.AlignCenter, 
                           "No tileset loaded\n\nLoad a tileset to begin\nFile > Load Tileset (Ctrl+T)")
            return
        
        tile_total_size = self.display_tile_size + self.tile_spacing
        
        total_tiles = len(self.tile_surfaces)
        rows = (total_tiles + self.original_tileset_columns - 1) // self.original_tileset_columns
        
        grid_width = self.original_tileset_columns * tile_total_size - self.tile_spacing
        grid_height = rows * tile_total_size - self.tile_spacing
        
        y_offset = self.grid_padding
        x_offset = self.grid_padding
        
        painter.fillRect(
            x_offset - 4, 
            y_offset - 4, 
            grid_width + 8, 
            grid_height + 8,
            QColor(30, 30, 30)
        )
        
        for row in range(rows):
            for col in range(self.original_tileset_columns):
                tile_index = row * self.original_tileset_columns + col
                
                if tile_index >= len(self.tile_surfaces):
                    break
                
                x = x_offset + col * tile_total_size
                y = y_offset + row * tile_total_size
                
                painter.fillRect(x, y, self.display_tile_size, self.display_tile_size, QColor(40, 40, 40))
                
                tile = self.tile_surfaces[tile_index]
                scaled_tile = tile.scaled(
                    self.display_tile_size, 
                    self.display_tile_size, 
                    Qt.KeepAspectRatio, 
                    Qt.SmoothTransformation
                )
                
                painter.drawPixmap(x, y, scaled_tile)
                
                if tile_index == self.selected_tile:
                    painter.setPen(QPen(QColor(80, 180, 255), 3))
                    painter.drawRect(x - 1, y - 1, self.display_tile_size + 2, self.display_tile_size + 2)
                    
                    painter.setPen(QPen(QColor(120, 200, 255, 100), 1))
                    painter.drawRect(x - 3, y - 3, self.display_tile_size + 6, self.display_tile_size + 6)
                else:
                    painter.setPen(QPen(QColor(60, 60, 60), 1))
                    painter.drawRect(x, y, self.display_tile_size, self.display_tile_size)
