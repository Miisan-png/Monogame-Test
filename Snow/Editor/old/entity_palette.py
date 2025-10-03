from PyQt5.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel, 
                             QPushButton, QScrollArea, QFrame, QButtonGroup, QTabWidget)
from PyQt5.QtCore import Qt, pyqtSignal
from PyQt5.QtGui import QColor, QPainter

class EntityButton(QPushButton):
    def __init__(self, entity_type, color, description, parent=None):
        super().__init__(parent)
        self.entity_type = entity_type
        self.entity_color = color
        self.description = description
        self.setCheckable(True)
        self.setFixedHeight(70)
        self.setStyleSheet("""
            QPushButton {
                background-color: #383838;
                border: 2px solid #505050;
                border-radius: 6px;
                padding: 10px;
                text-align: left;
                color: #e0e0e0;
                font-size: 12pt;
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
        self.setText(f"{self.entity_type}\n{self.description}")
    
    def paintEvent(self, event):
        super().paintEvent(event)
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing)
        
        color_rect_size = 20
        margin = 12
        painter.fillRect(
            self.width() - color_rect_size - margin,
            (self.height() - color_rect_size) // 2,
            color_rect_size,
            color_rect_size,
            self.entity_color
        )
        painter.setPen(QColor(200, 200, 200))
        painter.drawRect(
            self.width() - color_rect_size - margin,
            (self.height() - color_rect_size) // 2,
            color_rect_size,
            color_rect_size
        )

class EntityPalette(QWidget):
    entitySelected = pyqtSignal(str)
    objectSelected = pyqtSignal(str, str)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setMinimumWidth(240)
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(10)
        
        header = QLabel("Objects")
        header.setStyleSheet("font-weight: bold; font-size: 14pt; color: #e0e0e0; padding: 6px;")
        layout.addWidget(header)
        
        tabs = QTabWidget()
        tabs.setStyleSheet("""
            QTabWidget::pane {
                border: 1px solid #404040;
                background-color: #2a2a2a;
            }
            QTabBar::tab {
                background-color: #353535;
                color: #b0b0b0;
                padding: 8px 16px;
                margin: 2px;
                border-top-left-radius: 4px;
                border-top-right-radius: 4px;
            }
            QTabBar::tab:selected {
                background-color: #3c7fb0;
                color: #ffffff;
            }
            QTabBar::tab:hover {
                background-color: #454545;
            }
        """)
        
        entities_tab = self.create_entities_tab()
        environment_tab = self.create_environment_tab()
        gameplay_tab = self.create_gameplay_tab()
        
        tabs.addTab(entities_tab, "Entities")
        tabs.addTab(environment_tab, "Environment")
        tabs.addTab(gameplay_tab, "Gameplay")
        
        layout.addWidget(tabs)
        
        info_frame = QFrame()
        info_frame.setStyleSheet("QFrame { background-color: #2d2d2d; border-radius: 4px; padding: 10px; }")
        info_layout = QVBoxLayout(info_frame)
        info_layout.setContentsMargins(10, 10, 10, 10)
        
        info_label = QLabel("Controls:")
        info_label.setStyleSheet("font-weight: bold; color: #e0e0e0; font-size: 11pt;")
        info_layout.addWidget(info_label)
        
        controls = [
            "Left Click - Place/Select",
            "Right Click - Delete",
            "Middle Mouse - Pan",
            "Mouse Wheel - Zoom",
            "WASD - Move Camera",
            "Delete - Remove Selected",
            "R - Reset View",
            "G - Toggle Grid"
        ]
        
        for control in controls:
            label = QLabel(control)
            label.setStyleSheet("color: #b0b0b0; font-size: 10pt; padding: 2px;")
            info_layout.addWidget(label)
        
        layout.addWidget(info_frame)
    
    def create_entities_tab(self):
        widget = QWidget()
        layout = QVBoxLayout(widget)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setStyleSheet("QScrollArea { border: none; background-color: #2a2a2a; }")
        
        scroll_content = QWidget()
        scroll_layout = QVBoxLayout(scroll_content)
        scroll_layout.setContentsMargins(4, 4, 4, 4)
        scroll_layout.setSpacing(8)
        
        self.entity_button_group = QButtonGroup(self)
        self.entity_button_group.setExclusive(True)
        
        entities = [
            ("PlayerSpawn", QColor(100, 255, 100), "Player spawn point"),
            ("Slime", QColor(100, 200, 100), "Patrolling enemy"),
            ("Coin", QColor(255, 215, 0), "Collectible item"),
            ("Chest", QColor(139, 69, 19), "Interactive chest"),
            ("Spike", QColor(150, 150, 150), "Damage hazard"),
        ]
        
        for i, (entity_type, color, description) in enumerate(entities):
            btn = EntityButton(entity_type, color, description)
            btn.clicked.connect(lambda checked, t=entity_type: self.on_entity_selected(t))
            self.entity_button_group.addButton(btn, i)
            scroll_layout.addWidget(btn)
            
            if i == 0:
                btn.setChecked(True)
        
        scroll_layout.addStretch()
        scroll_area.setWidget(scroll_content)
        layout.addWidget(scroll_area)
        
        return widget
    
    def create_environment_tab(self):
        widget = QWidget()
        layout = QVBoxLayout(widget)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setStyleSheet("QScrollArea { border: none; background-color: #2a2a2a; }")
        
        scroll_content = QWidget()
        scroll_layout = QVBoxLayout(scroll_content)
        scroll_layout.setContentsMargins(4, 4, 4, 4)
        scroll_layout.setSpacing(8)
        
        self.env_button_group = QButtonGroup(self)
        self.env_button_group.setExclusive(True)
        
        env_objects = [
            ("Light", "point", QColor(255, 200, 100), "Point light source"),
            ("AmbientLight", "ambient", QColor(180, 200, 255), "Ambient lighting"),
            ("AudioSource", "audio", QColor(100, 200, 255), "Spatial audio"),
            ("ParticleEmitter", "particle", QColor(200, 100, 255), "Particle effects"),
        ]
        
        for i, (obj_type, subtype, color, description) in enumerate(env_objects):
            btn = EntityButton(obj_type, color, description)
            btn.clicked.connect(lambda checked, t=obj_type, s=subtype: self.on_object_selected(t, s))
            self.env_button_group.addButton(btn, i)
            scroll_layout.addWidget(btn)
        
        scroll_layout.addStretch()
        scroll_area.setWidget(scroll_content)
        layout.addWidget(scroll_area)
        
        return widget
    
    def create_gameplay_tab(self):
        widget = QWidget()
        layout = QVBoxLayout(widget)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(8)
        
        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setStyleSheet("QScrollArea { border: none; background-color: #2a2a2a; }")
        
        scroll_content = QWidget()
        scroll_layout = QVBoxLayout(scroll_content)
        scroll_layout.setContentsMargins(4, 4, 4, 4)
        scroll_layout.setSpacing(8)
        
        self.gameplay_button_group = QButtonGroup(self)
        self.gameplay_button_group.setExclusive(True)
        
        gameplay_objects = [
            ("Trigger", "trigger", QColor(255, 255, 100), "Trigger zone"),
            ("SpawnPoint", "spawn", QColor(255, 150, 255), "Enemy spawn point"),
        ]
        
        for i, (obj_type, subtype, color, description) in enumerate(gameplay_objects):
            btn = EntityButton(obj_type, color, description)
            btn.clicked.connect(lambda checked, t=obj_type, s=subtype: self.on_object_selected(t, s))
            self.gameplay_button_group.addButton(btn, i)
            scroll_layout.addWidget(btn)
        
        scroll_layout.addStretch()
        scroll_area.setWidget(scroll_content)
        layout.addWidget(scroll_area)
        
        return widget
    
    def on_entity_selected(self, entity_type):
        self.entitySelected.emit(entity_type)
        self.objectSelected.emit("entity", entity_type)
    
    def on_object_selected(self, obj_type, subtype):
        self.objectSelected.emit(obj_type, subtype)
