from PyQt5.QtWidgets import (QWidget, QVBoxLayout, QGroupBox, QFormLayout, 
                             QSpinBox, QCheckBox, QPushButton, QLabel)
from PyQt5.QtCore import Qt

class PropertiesPanel(QWidget):
    def __init__(self, canvas, parent=None):
        super().__init__(parent)
        self.canvas = canvas
        self.setFixedWidth(280)
        
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(12)
        
        world_group = QGroupBox("World Settings")
        world_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        world_layout = QFormLayout(world_group)
        world_layout.setSpacing(8)
        
        self.world_width_spin = QSpinBox()
        self.world_width_spin.setRange(10, 500)
        self.world_width_spin.setValue(self.canvas.grid_width)
        self.world_width_spin.valueChanged.connect(self.on_world_size_changed)
        self.world_width_spin.setStyleSheet("QSpinBox { padding: 4px; }")
        
        self.world_height_spin = QSpinBox()
        self.world_height_spin.setRange(10, 500)
        self.world_height_spin.setValue(self.canvas.grid_height)
        self.world_height_spin.valueChanged.connect(self.on_world_size_changed)
        self.world_height_spin.setStyleSheet("QSpinBox { padding: 4px; }")
        
        self.tile_size_spin = QSpinBox()
        self.tile_size_spin.setRange(8, 64)
        self.tile_size_spin.setValue(self.canvas.tile_size)
        self.tile_size_spin.valueChanged.connect(self.on_tile_size_changed)
        self.tile_size_spin.setStyleSheet("QSpinBox { padding: 4px; }")
        
        world_layout.addRow("Width:", self.world_width_spin)
        world_layout.addRow("Height:", self.world_height_spin)
        world_layout.addRow("Tile Size:", self.tile_size_spin)
        
        layout.addWidget(world_group)
        
        viewport_group = QGroupBox("Viewport")
        viewport_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        viewport_layout = QFormLayout(viewport_group)
        viewport_layout.setSpacing(8)
        
        self.viewport_width_spin = QSpinBox()
        self.viewport_width_spin.setRange(100, 1000)
        self.viewport_width_spin.setValue(self.canvas.viewport_width)
        self.viewport_width_spin.valueChanged.connect(self.on_viewport_changed)
        self.viewport_width_spin.setStyleSheet("QSpinBox { padding: 4px; }")
        
        self.viewport_height_spin = QSpinBox()
        self.viewport_height_spin.setRange(100, 1000)
        self.viewport_height_spin.setValue(self.canvas.viewport_height)
        self.viewport_height_spin.valueChanged.connect(self.on_viewport_changed)
        self.viewport_height_spin.setStyleSheet("QSpinBox { padding: 4px; }")
        
        viewport_layout.addRow("Width:", self.viewport_width_spin)
        viewport_layout.addRow("Height:", self.viewport_height_spin)
        
        layout.addWidget(viewport_group)
        
        display_group = QGroupBox("Display Options")
        display_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        display_layout = QVBoxLayout(display_group)
        display_layout.setSpacing(6)
        
        self.grid_checkbox = QCheckBox("Show Grid (G)")
        self.grid_checkbox.setChecked(True)
        self.grid_checkbox.toggled.connect(self.on_grid_toggled)
        
        self.viewport_checkbox = QCheckBox("Show Viewport (V)")
        self.viewport_checkbox.setChecked(True)
        self.viewport_checkbox.toggled.connect(self.on_viewport_toggled)
        
        self.collision_checkbox = QCheckBox("Show Collision (C)")
        self.collision_checkbox.setChecked(True)
        self.collision_checkbox.toggled.connect(self.on_collision_toggled)
        
        display_layout.addWidget(self.grid_checkbox)
        display_layout.addWidget(self.viewport_checkbox)
        display_layout.addWidget(self.collision_checkbox)
        
        layout.addWidget(display_group)
        
        actions_group = QGroupBox("Actions")
        actions_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        actions_layout = QVBoxLayout(actions_group)
        actions_layout.setSpacing(8)
        
        clear_tiles_btn = QPushButton("Clear Tiles")
        clear_tiles_btn.clicked.connect(self.canvas.clear_world)
        clear_tiles_btn.setStyleSheet("QPushButton { padding: 6px; background-color: #383838; border: 1px solid #505050; border-radius: 4px; } QPushButton:hover { background-color: #484848; }")
        
        clear_collision_btn = QPushButton("Clear Collisions")
        clear_collision_btn.clicked.connect(self.canvas.clear_collisions)
        clear_collision_btn.setStyleSheet("QPushButton { padding: 6px; background-color: #383838; border: 1px solid #505050; border-radius: 4px; } QPushButton:hover { background-color: #484848; }")
        
        reset_view_btn = QPushButton("Reset View (R)")
        reset_view_btn.clicked.connect(self.canvas.reset_view)
        reset_view_btn.setStyleSheet("QPushButton { padding: 6px; background-color: #383838; border: 1px solid #505050; border-radius: 4px; } QPushButton:hover { background-color: #484848; }")
        
        actions_layout.addWidget(clear_tiles_btn)
        actions_layout.addWidget(clear_collision_btn)
        actions_layout.addWidget(reset_view_btn)
        
        layout.addWidget(actions_group)
        layout.addStretch()
    
    def on_world_size_changed(self):
        width = self.world_width_spin.value()
        height = self.world_height_spin.value()
        self.canvas.resize_world(width, height)
    
    def on_tile_size_changed(self):
        self.canvas.tile_size = self.tile_size_spin.value()
        if self.canvas.tileset_image:
            self.canvas.extract_tiles()
        self.canvas.update()
    
    def on_viewport_changed(self):
        self.canvas.viewport_width = self.viewport_width_spin.value()
        self.canvas.viewport_height = self.viewport_height_spin.value()
        self.canvas.update()
    
    def on_grid_toggled(self, checked):
        self.canvas.show_grid = checked
        self.canvas.update()
    
    def on_viewport_toggled(self, checked):
        self.canvas.show_viewport = checked
        self.canvas.update()
    
    def on_collision_toggled(self, checked):
        self.canvas.show_collision = checked
        self.canvas.update()
