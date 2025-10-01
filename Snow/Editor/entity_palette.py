from PyQt5.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel, 
                             QPushButton, QScrollArea, QFrame, QButtonGroup)
from PyQt5.QtCore import Qt, pyqtSignal
from PyQt5.QtGui import QColor, QPainter

class EntityButton(QPushButton):
    def __init__(self, entity_type, color, parent=None):
        super().__init__(parent)
        self.entity_type = entity_type
        self.entity_color = color
        self.setCheckable(True)
        self.setFixedHeight(60)
        self.setStyleSheet("""
            QPushButton {
                background-color: #383838;
                border: 2px solid #505050;
                border-radius: 6px;
                padding: 8px;
                text-align: left;
                color: #e0e0e0;
            }
            QPushButton:hover {
                background-color: #484848;
                border-color: #606060;
            }
            QPushButton:checked {
                background-color: #3c7fb0;
                border-color: #5090c0;
            }
        """)
        self.update_text()
    
    def update_text(self):
        self.setText(self.entity_type)

class EntityPalette(QWidget):
    entitySelected = pyqtSignal(str)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedWidth(220)
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        header = QLabel("Entity Palette")
        header.setStyleSheet("font-weight: bold; font-size: 14px; color: #e0e0e0; padding: 4px;")
        layout.addWidget(header)
        
        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setStyleSheet("QScrollArea { border: none; background-color: #2a2a2a; }")
        
        scroll_content = QWidget()
        scroll_layout = QVBoxLayout(scroll_content)
        scroll_layout.setContentsMargins(4, 4, 4, 4)
        scroll_layout.setSpacing(6)
        
        self.button_group = QButtonGroup(self)
        self.button_group.setExclusive(True)
        
        entities = [
            ("PlayerSpawn", QColor(100, 255, 100), "Player spawn point"),
            ("Slime", QColor(100, 200, 100), "Patrolling enemy"),
            ("Coin", QColor(255, 215, 0), "Collectible coin"),
            ("Chest", QColor(139, 69, 19), "Interactive chest"),
            ("Spike", QColor(150, 150, 150), "Hazard/damage"),
        ]
        
        for i, (entity_type, color, description) in enumerate(entities):
            btn = self.create_entity_button(entity_type, color, description)
            self.button_group.addButton(btn, i)
            scroll_layout.addWidget(btn)
            
            if i == 0:
                btn.setChecked(True)
        
        scroll_layout.addStretch()
        
        scroll_area.setWidget(scroll_content)
        layout.addWidget(scroll_area)
        
        info_frame = QFrame()
        info_frame.setStyleSheet("QFrame { background-color: #2d2d2d; border-radius: 4px; padding: 8px; }")
        info_layout = QVBoxLayout(info_frame)
        info_layout.setContentsMargins(8, 8, 8, 8)
        
        info_label = QLabel("Controls:")
        info_label.setStyleSheet("font-weight: bold; color: #e0e0e0;")
        info_layout.addWidget(info_label)
        
        controls = [
            "Left Click - Place/Select",
            "Right Click - Delete",
            "Middle Mouse - Pan",
            "Mouse Wheel - Zoom",
            "WASD - Move Camera",
            "Delete - Remove Selected",
            "R - Reset View"
        ]
        
        for control in controls:
            label = QLabel(control)
            label.setStyleSheet("color: #b0b0b0; font-size: 11px;")
            info_layout.addWidget(label)
        
        layout.addWidget(info_frame)
    
    def create_entity_button(self, entity_type, color, description):
        container = QWidget()
        container_layout = QVBoxLayout(container)
        container_layout.setContentsMargins(0, 0, 0, 0)
        container_layout.setSpacing(2)
        
        btn = EntityButton(entity_type, color)
        btn.clicked.connect(lambda: self.on_entity_selected(entity_type))
        
        return btn
    
    def on_entity_selected(self, entity_type):
        self.entitySelected.emit(entity_type)
