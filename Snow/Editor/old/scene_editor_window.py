import os
from PyQt5.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout, 
                             QAction, QFileDialog, QMessageBox, QLabel, QToolBar,
                             QPushButton, QDockWidget, QLineEdit, QDialog, QFormLayout)
from PyQt5.QtCore import Qt
from PyQt5.QtGui import QIcon
from scene_canvas import SceneCanvas
from entity_palette import EntityPalette
from entity_properties_panel import EntityPropertiesPanel
from scene_data import SceneData, SceneParser

class SceneEditorWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Snow Engine - Scene Editor")
        self.setGeometry(100, 100, 1600, 900)
        
        self.current_file = None
        self.scene_data = None
        
        self.init_ui()
        self.init_menus()
        self.init_toolbar()
        self.init_status_bar()
        
    def init_ui(self):
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        
        main_layout = QHBoxLayout(central_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)
        
        self.canvas = SceneCanvas(self)
        self.canvas.entitySelected.connect(self.on_entity_selected)
        self.canvas.entityMoved.connect(self.on_entity_moved)
        
        self.palette_widget = EntityPalette(self)
        self.palette_widget.entitySelected.connect(self.on_entity_tool_selected)
        
        self.palette_dock = QDockWidget("Entity Palette", self)
        self.palette_dock.setWidget(self.palette_widget)
        self.palette_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.palette_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.LeftDockWidgetArea, self.palette_dock)
        
        self.properties = EntityPropertiesPanel(self)
        self.properties.propertyChanged.connect(self.on_property_changed)
        
        self.properties_dock = QDockWidget("Properties", self)
        self.properties_dock.setWidget(self.properties)
        self.properties_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.properties_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.RightDockWidgetArea, self.properties_dock)
        
        main_layout.addWidget(self.canvas)
    
    def init_menus(self):
        menubar = self.menuBar()
        menubar.setStyleSheet("QMenuBar { background-color: #2d2d2d; padding: 4px; } QMenuBar::item { padding: 4px 12px; } QMenuBar::item:selected { background-color: #3c3c3c; }")
        
        file_menu = menubar.addMenu("File")
        
        new_action = QAction("New Scene", self)
        new_action.setShortcut("Ctrl+N")
        new_action.triggered.connect(self.new_scene)
        file_menu.addAction(new_action)
        
        open_action = QAction("Open Scene", self)
        open_action.setShortcut("Ctrl+O")
        open_action.triggered.connect(self.open_scene)
        file_menu.addAction(open_action)
        
        save_action = QAction("Save", self)
        save_action.setShortcut("Ctrl+S")
        save_action.triggered.connect(self.save_scene)
        file_menu.addAction(save_action)
        
        save_as_action = QAction("Save As...", self)
        save_as_action.setShortcut("Ctrl+Shift+S")
        save_as_action.triggered.connect(self.save_scene_as)
        file_menu.addAction(save_as_action)
        
        file_menu.addSeparator()
        
        exit_action = QAction("Exit", self)
        exit_action.setShortcut("Ctrl+Q")
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)
        
        view_menu = menubar.addMenu("View")
        
        zoom_in_action = QAction("Zoom In", self)
        zoom_in_action.setShortcut("Ctrl+=")
        zoom_in_action.triggered.connect(self.zoom_in)
        view_menu.addAction(zoom_in_action)
        
        zoom_out_action = QAction("Zoom Out", self)
        zoom_out_action.setShortcut("Ctrl+-")
        zoom_out_action.triggered.connect(self.zoom_out)
        view_menu.addAction(zoom_out_action)
        
        zoom_reset_action = QAction("Reset View", self)
        zoom_reset_action.setShortcut("Ctrl+0")
        zoom_reset_action.triggered.connect(self.canvas.reset_view)
        view_menu.addAction(zoom_reset_action)
        
        view_menu.addSeparator()
        
        grid_action = QAction("Toggle Grid (G)", self)
        grid_action.triggered.connect(self.toggle_grid)
        view_menu.addAction(grid_action)
        
        tiles_action = QAction("Toggle Tiles (T)", self)
        tiles_action.triggered.connect(self.toggle_tiles)
        view_menu.addAction(tiles_action)
    
    def init_toolbar(self):
        toolbar = QToolBar("Tools")
        toolbar.setMovable(False)
        toolbar.setStyleSheet("QToolBar { background-color: #2d2d2d; padding: 6px; spacing: 4px; border-bottom: 1px solid #404040; }")
        self.addToolBar(toolbar)
        
        file_section = QWidget()
        file_layout = QHBoxLayout(file_section)
        file_layout.setContentsMargins(0, 0, 16, 0)
        file_layout.setSpacing(4)
        
        new_btn = self.create_tool_button("New", self.new_scene)
        open_btn = self.create_tool_button("Open", self.open_scene)
        save_btn = self.create_tool_button("Save", self.save_scene)
        
        file_layout.addWidget(new_btn)
        file_layout.addWidget(open_btn)
        file_layout.addWidget(save_btn)
        toolbar.addWidget(file_section)
        
        toolbar.addSeparator()
        
        scene_props_btn = self.create_tool_button("Scene Properties", self.open_scene_properties)
        scene_props_btn.setStyleSheet(scene_props_btn.styleSheet() + "QPushButton { padding: 6px 16px; }")
        toolbar.addWidget(scene_props_btn)
    
    def create_tool_button(self, text, callback):
        btn = QPushButton(text)
        btn.clicked.connect(callback)
        btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 4px; padding: 8px 14px; color: #e0e0e0; font-size: 13px; } QPushButton:hover { background-color: #484848; } QPushButton:pressed { background-color: #2d2d2d; }")
        return btn
    
    def init_status_bar(self):
        self.status_bar = self.statusBar()
        self.status_bar.setStyleSheet("QStatusBar { background-color: #2d2d2d; border-top: 1px solid #404040; padding: 4px; }")
        
        self.mouse_pos_label = QLabel("Position: (0, 0)")
        self.entity_count_label = QLabel("Entities: 0")
        self.zoom_label = QLabel("Zoom: 200%")
        self.scene_name_label = QLabel("Scene: New Scene")
        
        for label in [self.mouse_pos_label, self.entity_count_label, self.zoom_label, self.scene_name_label]:
            label.setStyleSheet("color: #b0b0b0; padding: 0 8px;")
        
        self.status_bar.addWidget(self.mouse_pos_label)
        self.status_bar.addWidget(self.entity_count_label)
        self.status_bar.addPermanentWidget(self.scene_name_label)
        self.status_bar.addPermanentWidget(self.zoom_label)
    
    def new_scene(self):
        dialog = NewSceneDialog(self)
        if dialog.exec_() == QDialog.Accepted:
            self.scene_data = dialog.get_scene_data()
            self.canvas.set_scene_data(self.scene_data)
            self.current_file = None
            self.update_window_title()
            self.update_status()
    
    def open_scene(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Open Scene", "", "Scene Files (*.scene)")
        
        if file_path:
            try:
                self.scene_data = SceneParser.parse_scene(file_path)
                self.canvas.set_scene_data(self.scene_data)
                self.current_file = file_path
                self.update_window_title()
                self.update_status()
                QMessageBox.information(self, "Success", f"Scene loaded: {self.scene_data.name}")
            except Exception as e:
                QMessageBox.warning(self, "Error", f"Failed to load scene:\n{str(e)}")
    
    def save_scene(self):
        if not self.scene_data:
            QMessageBox.warning(self, "No Scene", "Create or open a scene first.")
            return
        
        if self.current_file:
            try:
                SceneParser.write_scene(self.current_file, self.scene_data)
                QMessageBox.information(self, "Success", "Scene saved successfully!")
            except Exception as e:
                QMessageBox.warning(self, "Error", f"Failed to save scene:\n{str(e)}")
        else:
            self.save_scene_as()
    
    def save_scene_as(self):
        if not self.scene_data:
            QMessageBox.warning(self, "No Scene", "Create or open a scene first.")
            return
        
        file_path, _ = QFileDialog.getSaveFileName(
            self, "Save Scene", "", "Scene Files (*.scene)")
        
        if file_path:
            try:
                SceneParser.write_scene(file_path, self.scene_data)
                self.current_file = file_path
                self.update_window_title()
                QMessageBox.information(self, "Success", "Scene saved successfully!")
            except Exception as e:
                QMessageBox.warning(self, "Error", f"Failed to save scene:\n{str(e)}")
    
    def open_scene_properties(self):
        if not self.scene_data:
            QMessageBox.warning(self, "No Scene", "Create or open a scene first.")
            return
        
        dialog = ScenePropertiesDialog(self, self.scene_data)
        if dialog.exec_() == QDialog.Accepted:
            dialog.apply_changes()
            self.canvas.load_level_background()
            self.canvas.update()
            self.update_status()
    
    def on_entity_tool_selected(self, entity_type):
        self.canvas.set_entity_tool(entity_type)
    
    def on_entity_selected(self, entity):
        self.properties.set_entity(entity)
        self.canvas.update()
    
    def on_entity_moved(self, entity):
        self.properties.set_entity(entity)
    
    def on_property_changed(self, entity):
        self.canvas.update()
    
    def delete_selected_entity(self):
        if self.canvas.selected_entity:
            self.canvas.delete_entity(self.canvas.selected_entity)
    
    def zoom_in(self):
        self.canvas.zoom = min(self.canvas.max_zoom, self.canvas.zoom * 1.3)
        self.canvas.update()
        self.update_status()
    
    def zoom_out(self):
        self.canvas.zoom = max(self.canvas.min_zoom, self.canvas.zoom / 1.3)
        self.canvas.update()
        self.update_status()
    
    def toggle_grid(self):
        self.canvas.show_grid = not self.canvas.show_grid
        self.canvas.update()
    
    def toggle_tiles(self):
        self.canvas.show_tiles = not self.canvas.show_tiles
        self.canvas.update()
    
    def update_mouse_pos(self, x, y):
        self.mouse_pos_label.setText(f"Position: ({x}, {y})")
    
    def update_status(self):
        self.zoom_label.setText(f"Zoom: {int(self.canvas.zoom * 100)}%")
        
        if self.scene_data:
            entity_count = len(self.scene_data.entities)
            self.entity_count_label.setText(f"Entities: {entity_count}")
            self.scene_name_label.setText(f"Scene: {self.scene_data.name}")
    
    def update_window_title(self):
        if self.current_file:
            filename = os.path.basename(self.current_file)
            self.setWindowTitle(f"Snow Engine - Scene Editor - {filename}")
        else:
            self.setWindowTitle(f"Snow Engine - Scene Editor - {self.scene_data.name if self.scene_data else 'Untitled'}")


class NewSceneDialog(QDialog):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("New Scene")
        self.setModal(True)
        self.setFixedSize(500, 300)
        
        self.scene_data = SceneData()
        
        layout = QVBoxLayout(self)
        layout.setSpacing(12)
        
        form_layout = QFormLayout()
        form_layout.setSpacing(10)
        
        self.name_edit = QLineEdit("New Scene")
        self.name_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        form_layout.addRow("Scene Name:", self.name_edit)
        
        tilemap_layout = QHBoxLayout()
        self.tilemap_edit = QLineEdit()
        self.tilemap_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        tilemap_btn = QPushButton("Browse...")
        tilemap_btn.clicked.connect(self.browse_tilemap)
        tilemap_btn.setStyleSheet("QPushButton { padding: 6px 12px; }")
        tilemap_layout.addWidget(self.tilemap_edit)
        tilemap_layout.addWidget(tilemap_btn)
        form_layout.addRow("Tilemap (JSON):", tilemap_layout)
        
        tileset_layout = QHBoxLayout()
        self.tileset_edit = QLineEdit()
        self.tileset_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        tileset_btn = QPushButton("Browse...")
        tileset_btn.clicked.connect(self.browse_tileset)
        tileset_btn.setStyleSheet("QPushButton { padding: 6px 12px; }")
        tileset_layout.addWidget(self.tileset_edit)
        tileset_layout.addWidget(tileset_btn)
        form_layout.addRow("Tileset Image:", tileset_layout)
        
        self.bg_color_edit = QLineEdit("#87CEEB")
        self.bg_color_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        form_layout.addRow("Background Color:", self.bg_color_edit)
        
        layout.addLayout(form_layout)
        
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        
        create_btn = QPushButton("Create")
        create_btn.clicked.connect(self.accept)
        create_btn.setStyleSheet("QPushButton { padding: 8px 24px; background-color: #3c7fb0; border-radius: 4px; } QPushButton:hover { background-color: #4c8fc0; }")
        
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        cancel_btn.setStyleSheet("QPushButton { padding: 8px 24px; }")
        
        button_layout.addWidget(create_btn)
        button_layout.addWidget(cancel_btn)
        
        layout.addLayout(button_layout)
    
    def browse_tilemap(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Select Tilemap", "", "JSON Files (*.json)")
        if file_path:
            self.tilemap_edit.setText(file_path)
    
    def browse_tileset(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Select Tileset", "", "Image Files (*.png *.jpg *.jpeg)")
        if file_path:
            self.tileset_edit.setText(file_path)
    
    def get_scene_data(self):
        self.scene_data.name = self.name_edit.text()
        self.scene_data.tilemap = self.tilemap_edit.text()
        self.scene_data.tileset = self.tileset_edit.text()
        self.scene_data.background_color = self.bg_color_edit.text()
        return self.scene_data


class ScenePropertiesDialog(QDialog):
    def __init__(self, parent, scene_data):
        super().__init__(parent)
        self.setWindowTitle("Scene Properties")
        self.setModal(True)
        self.setFixedSize(500, 300)
        
        self.scene_data = scene_data
        
        layout = QVBoxLayout(self)
        layout.setSpacing(12)
        
        form_layout = QFormLayout()
        form_layout.setSpacing(10)
        
        self.name_edit = QLineEdit(scene_data.name)
        self.name_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        form_layout.addRow("Scene Name:", self.name_edit)
        
        tilemap_layout = QHBoxLayout()
        self.tilemap_edit = QLineEdit(scene_data.tilemap)
        self.tilemap_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        tilemap_btn = QPushButton("Browse...")
        tilemap_btn.clicked.connect(self.browse_tilemap)
        tilemap_btn.setStyleSheet("QPushButton { padding: 6px 12px; }")
        tilemap_layout.addWidget(self.tilemap_edit)
        tilemap_layout.addWidget(tilemap_btn)
        form_layout.addRow("Tilemap (JSON):", tilemap_layout)
        
        tileset_layout = QHBoxLayout()
        self.tileset_edit = QLineEdit(scene_data.tileset)
        self.tileset_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        tileset_btn = QPushButton("Browse...")
        tileset_btn.clicked.connect(self.browse_tileset)
        tileset_btn.setStyleSheet("QPushButton { padding: 6px 12px; }")
        tileset_layout.addWidget(self.tileset_edit)
        tileset_layout.addWidget(tileset_btn)
        form_layout.addRow("Tileset Image:", tileset_layout)
        
        self.bg_color_edit = QLineEdit(scene_data.background_color)
        self.bg_color_edit.setStyleSheet("QLineEdit { padding: 6px; }")
        form_layout.addRow("Background Color:", self.bg_color_edit)
        
        layout.addLayout(form_layout)
        
        button_layout = QHBoxLayout()
        button_layout.addStretch()
        
        save_btn = QPushButton("Save")
        save_btn.clicked.connect(self.accept)
        save_btn.setStyleSheet("QPushButton { padding: 8px 24px; background-color: #3c7fb0; border-radius: 4px; } QPushButton:hover { background-color: #4c8fc0; }")
        
        cancel_btn = QPushButton("Cancel")
        cancel_btn.clicked.connect(self.reject)
        cancel_btn.setStyleSheet("QPushButton { padding: 8px 24px; }")
        
        button_layout.addWidget(save_btn)
        button_layout.addWidget(cancel_btn)
        
        layout.addLayout(button_layout)
    
    def browse_tilemap(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Select Tilemap", "", "JSON Files (*.json)")
        if file_path:
            self.tilemap_edit.setText(file_path)
    
    def browse_tileset(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Select Tileset", "", "Image Files (*.png *.jpg *.jpeg)")
        if file_path:
            self.tileset_edit.setText(file_path)
    
    def apply_changes(self):
        self.scene_data.name = self.name_edit.text()
        self.scene_data.tilemap = self.tilemap_edit.text()
        self.scene_data.tileset = self.tileset_edit.text()
        self.scene_data.background_color = self.bg_color_edit.text()
