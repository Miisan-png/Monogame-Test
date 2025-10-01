import os
from PyQt5.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout, QStackedWidget,
                             QAction, QFileDialog, QMessageBox, QLabel, QToolBar,
                             QPushButton, QDockWidget, QButtonGroup)
from PyQt5.QtCore import Qt, QSize
from PyQt5.QtGui import QIcon, QPixmap
from level_canvas import LevelCanvas
from scene_canvas import SceneCanvas
from tile_palette_window import TilePaletteWindow
from entity_palette import EntityPalette
from properties_panel import PropertiesPanel
from entity_properties_panel import EntityPropertiesPanel
from file_manager import FileManager
from scene_data import SceneData, SceneParser

class UnifiedEditorWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Snow Engine - Unified Editor")
        self.setGeometry(100, 100, 1800, 1000)
        
        self.current_mode = "tile"
        self.current_level_file = None
        self.current_scene_file = None
        self.file_manager = FileManager()
        self.scene_data = None
        
        script_dir = os.path.dirname(os.path.abspath(__file__))
        self.icon_path = os.path.join(script_dir, "data", "icons")
        
        self.init_ui()
        self.init_menus()
        self.init_toolbar()
        self.init_status_bar()
        
        self.switch_mode("tile")
        
    def init_ui(self):
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        
        main_layout = QHBoxLayout(central_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)
        
        self.canvas_stack = QStackedWidget()
        
        self.level_canvas = LevelCanvas(self)
        self.scene_canvas = SceneCanvas(self)
        
        self.canvas_stack.addWidget(self.level_canvas)
        self.canvas_stack.addWidget(self.scene_canvas)
        
        self.scene_canvas.entitySelected.connect(self.on_entity_selected)
        self.scene_canvas.entityMoved.connect(self.on_entity_moved)
        
        self.tile_palette = TilePaletteWindow(self)
        self.tile_palette.tileSelected.connect(self.on_tile_selected)
        
        self.tile_palette_dock = QDockWidget("Tile Palette", self)
        self.tile_palette_dock.setWidget(self.tile_palette)
        self.tile_palette_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea | Qt.BottomDockWidgetArea)
        self.tile_palette_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.RightDockWidgetArea, self.tile_palette_dock)
        
        self.entity_palette = EntityPalette(self)
        self.entity_palette.entitySelected.connect(self.on_entity_tool_selected)
        
        self.entity_palette_dock = QDockWidget("Entity Palette", self)
        self.entity_palette_dock.setWidget(self.entity_palette)
        self.entity_palette_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.entity_palette_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.LeftDockWidgetArea, self.entity_palette_dock)
        
        self.level_properties = PropertiesPanel(self.level_canvas, self)
        
        self.level_properties_dock = QDockWidget("Level Properties", self)
        self.level_properties_dock.setWidget(self.level_properties)
        self.level_properties_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.level_properties_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.LeftDockWidgetArea, self.level_properties_dock)
        
        self.entity_properties = EntityPropertiesPanel(self)
        self.entity_properties.propertyChanged.connect(self.on_property_changed)
        
        self.entity_properties_dock = QDockWidget("Entity Properties", self)
        self.entity_properties_dock.setWidget(self.entity_properties)
        self.entity_properties_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.entity_properties_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.RightDockWidgetArea, self.entity_properties_dock)
        
        main_layout.addWidget(self.canvas_stack)
    
    def init_menus(self):
        menubar = self.menuBar()
        menubar.setStyleSheet("QMenuBar { background-color: #2d2d2d; padding: 4px; } QMenuBar::item { padding: 4px 12px; } QMenuBar::item:selected { background-color: #3c3c3c; }")
        
        file_menu = menubar.addMenu("File")
        
        new_level_action = QAction(self.get_icon("add.png"), "New Level", self)
        new_level_action.setShortcut("Ctrl+N")
        new_level_action.triggered.connect(self.new_level)
        file_menu.addAction(new_level_action)
        
        open_level_action = QAction(self.get_icon("folder.png"), "Open Level", self)
        open_level_action.setShortcut("Ctrl+O")
        open_level_action.triggered.connect(self.open_level)
        file_menu.addAction(open_level_action)
        
        save_action = QAction(self.get_icon("save.png"), "Save", self)
        save_action.setShortcut("Ctrl+S")
        save_action.triggered.connect(self.save_current)
        file_menu.addAction(save_action)
        
        file_menu.addSeparator()
        
        load_tileset_action = QAction(self.get_icon("tileset.png"), "Load Tileset", self)
        load_tileset_action.setShortcut("Ctrl+T")
        load_tileset_action.triggered.connect(self.load_tileset)
        file_menu.addAction(load_tileset_action)
        
        file_menu.addSeparator()
        
        exit_action = QAction(self.get_icon("close.png"), "Exit", self)
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
        
        reset_view_action = QAction("Reset View", self)
        reset_view_action.setShortcut("Ctrl+0")
        reset_view_action.triggered.connect(self.reset_view)
        view_menu.addAction(reset_view_action)
    
    def init_toolbar(self):
        toolbar = QToolBar("Main")
        toolbar.setMovable(False)
        toolbar.setIconSize(QSize(24, 24))
        toolbar.setStyleSheet("QToolBar { background-color: #2d2d2d; padding: 8px; spacing: 6px; border-bottom: 1px solid #404040; }")
        self.addToolBar(toolbar)
        
        mode_group = QButtonGroup(self)
        mode_group.setExclusive(True)
        
        tile_mode_btn = self.create_toggle_button("Tile Mode", self.get_icon("tilemap/pen.png"), "tile")
        tile_mode_btn.setChecked(True)
        scene_mode_btn = self.create_toggle_button("Scene Mode", self.get_icon("scene.png"), "scene")
        
        mode_group.addButton(tile_mode_btn)
        mode_group.addButton(scene_mode_btn)
        
        toolbar.addWidget(tile_mode_btn)
        toolbar.addWidget(scene_mode_btn)
        
        toolbar.addSeparator()
        
        new_btn = self.create_icon_button(self.get_icon("add.png"), "New", self.new_level)
        open_btn = self.create_icon_button(self.get_icon("folder.png"), "Open", self.open_level)
        save_btn = self.create_icon_button(self.get_icon("save.png"), "Save", self.save_current)
        
        toolbar.addWidget(new_btn)
        toolbar.addWidget(open_btn)
        toolbar.addWidget(save_btn)
        
        toolbar.addSeparator()
        
        self.tile_tools_widget = QWidget()
        tile_tools_layout = QHBoxLayout(self.tile_tools_widget)
        tile_tools_layout.setContentsMargins(0, 0, 0, 0)
        tile_tools_layout.setSpacing(4)
        
        self.tile_tool_group = QButtonGroup(self)
        
        brush_btn = self.create_icon_toggle_button(self.get_icon("tilemap/pen.png"), "Brush", "brush")
        rect_btn = self.create_icon_toggle_button(self.get_icon("tools/rect.png"), "Rectangle", "rect")
        fill_btn = self.create_icon_toggle_button(self.get_icon("tilemap/fill.png"), "Fill", "fill")
        collision_btn = self.create_icon_toggle_button(self.get_icon("tools/box2d.png"), "Collision", "collision")
        eraser_btn = self.create_icon_toggle_button(self.get_icon("tilemap/eraser.png"), "Eraser", "eraser")
        
        brush_btn.setChecked(True)
        
        self.tile_tool_group.addButton(brush_btn)
        self.tile_tool_group.addButton(rect_btn)
        self.tile_tool_group.addButton(fill_btn)
        self.tile_tool_group.addButton(collision_btn)
        self.tile_tool_group.addButton(eraser_btn)
        
        tile_tools_layout.addWidget(brush_btn)
        tile_tools_layout.addWidget(rect_btn)
        tile_tools_layout.addWidget(fill_btn)
        tile_tools_layout.addWidget(collision_btn)
        tile_tools_layout.addWidget(eraser_btn)
        
        toolbar.addWidget(self.tile_tools_widget)
        
        toolbar.addSeparator()
        
        grid_btn = self.create_icon_button(self.get_icon("grid.png"), "Toggle Grid (G)", self.toggle_grid)
        toolbar.addWidget(grid_btn)
    
    def init_status_bar(self):
        self.status_bar = self.statusBar()
        self.status_bar.setStyleSheet("QStatusBar { background-color: #2d2d2d; border-top: 1px solid #404040; padding: 4px; }")
        
        self.mouse_pos_label = QLabel("Position: (0, 0)")
        self.info_label = QLabel("Mode: Tile")
        self.zoom_label = QLabel("Zoom: 200%")
        self.file_label = QLabel("File: None")
        
        for label in [self.mouse_pos_label, self.info_label, self.zoom_label, self.file_label]:
            label.setStyleSheet("color: #b0b0b0; padding: 0 8px;")
        
        self.status_bar.addWidget(self.mouse_pos_label)
        self.status_bar.addWidget(self.info_label)
        self.status_bar.addPermanentWidget(self.file_label)
        self.status_bar.addPermanentWidget(self.zoom_label)
    
    def get_icon(self, icon_name):
        icon_full_path = os.path.join(self.icon_path, icon_name)
        if os.path.exists(icon_full_path):
            return QIcon(icon_full_path)
        return QIcon()
    
    def create_toggle_button(self, text, icon, mode):
        btn = QPushButton(icon, text)
        btn.setCheckable(True)
        btn.clicked.connect(lambda: self.switch_mode(mode))
        btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 4px; 
                padding: 10px 18px; 
                color: #e0e0e0; 
                font-size: 14px; 
                font-weight: bold;
            } 
            QPushButton:hover { background-color: #484848; } 
            QPushButton:checked { 
                background-color: #3c7fb0; 
                border-color: #5090c0; 
            }
        """)
        return btn
    
    def create_icon_button(self, icon, tooltip, callback):
        btn = QPushButton(icon, "")
        btn.setToolTip(tooltip)
        btn.clicked.connect(callback)
        btn.setFixedSize(36, 36)
        btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 4px; 
            } 
            QPushButton:hover { background-color: #484848; } 
            QPushButton:pressed { background-color: #2d2d2d; }
        """)
        return btn
    
    def create_icon_toggle_button(self, icon, tooltip, tool):
        btn = QPushButton(icon, "")
        btn.setToolTip(tooltip)
        btn.setCheckable(True)
        btn.clicked.connect(lambda: self.set_tile_tool(tool))
        btn.setFixedSize(36, 36)
        btn.setStyleSheet("""
            QPushButton { 
                background-color: #383838; 
                border: 1px solid #505050; 
                border-radius: 4px; 
            } 
            QPushButton:hover { background-color: #484848; } 
            QPushButton:checked { 
                background-color: #3c7fb0; 
                border-color: #5090c0; 
            }
        """)
        return btn
    
    def switch_mode(self, mode):
        self.current_mode = mode
        
        if mode == "tile":
            self.canvas_stack.setCurrentWidget(self.level_canvas)
            self.tile_palette_dock.show()
            self.entity_palette_dock.hide()
            self.level_properties_dock.show()
            self.entity_properties_dock.hide()
            self.tile_tools_widget.setVisible(True)
            self.info_label.setText("Mode: Tile Editor")
        else:
            # Share tileset and level data with scene canvas when switching to scene mode
            if self.level_canvas.tile_surfaces:
                self.scene_canvas.tile_surfaces = self.level_canvas.tile_surfaces
                self.scene_canvas.tileset_image = self.level_canvas.tileset_image
                self.scene_canvas.tile_size = self.level_canvas.tile_size
            
            # Share level geometry
            if self.level_canvas.world_data is not None and len(self.level_canvas.world_data) > 0:
                if not self.scene_canvas.level_data:
                    self.scene_canvas.level_data = {}
                self.scene_canvas.level_data['world_data'] = self.level_canvas.world_data
                self.scene_canvas.level_data['grid_width'] = self.level_canvas.grid_width
                self.scene_canvas.level_data['grid_height'] = self.level_canvas.grid_height
                self.scene_canvas.level_data['tile_size'] = self.level_canvas.tile_size
            
            self.canvas_stack.setCurrentWidget(self.scene_canvas)
            self.scene_canvas.update()
            
            self.tile_palette_dock.hide()
            self.entity_palette_dock.show()
            self.level_properties_dock.hide()
            self.entity_properties_dock.show()
            self.tile_tools_widget.setVisible(False)
            self.info_label.setText("Mode: Scene Editor")
        
        self.update_status()
    
    def new_level(self):
        if self.current_mode == "tile":
            self.level_canvas.clear_world()
            self.level_canvas.clear_collisions()
            self.current_level_file = None
            self.file_label.setText("File: Untitled Level")
        else:
            from scene_editor_window import NewSceneDialog
            from PyQt5.QtWidgets import QDialog
            dialog = NewSceneDialog(self)
            if dialog.exec_() == QDialog.Accepted:
                self.scene_data = dialog.get_scene_data()
                self.scene_canvas.set_scene_data(self.scene_data)
                self.current_scene_file = None
                self.file_label.setText(f"File: {self.scene_data.name}")
                self.update_status()
    
    def open_level(self):
        if self.current_mode == "tile":
            file_path, _ = QFileDialog.getOpenFileName(
                self, "Open Level", "", "JSON Files (*.json)")
            
            if file_path:
                success, message = self.file_manager.load_level(file_path, self.level_canvas)
                if success:
                    self.current_level_file = file_path
                    self.file_label.setText(f"File: {os.path.basename(file_path)}")
                    self.level_properties.world_width_spin.setValue(self.level_canvas.grid_width)
                    self.level_properties.world_height_spin.setValue(self.level_canvas.grid_height)
                    self.level_properties.tile_size_spin.setValue(self.level_canvas.tile_size)
                    
                    # Share tileset with scene canvas
                    if self.level_canvas.tile_surfaces:
                        self.scene_canvas.tile_surfaces = self.level_canvas.tile_surfaces
                        self.scene_canvas.tileset_image = self.level_canvas.tileset_image
                        self.scene_canvas.tile_size = self.level_canvas.tile_size
                else:
                    QMessageBox.warning(self, "Error", message)
        else:
            file_path, _ = QFileDialog.getOpenFileName(
                self, "Open Scene", "", "Scene Files (*.scene)")
            
            if file_path:
                try:
                    self.scene_data = SceneParser.parse_scene(file_path)
                    self.scene_canvas.set_scene_data(self.scene_data)
                    self.current_scene_file = file_path
                    self.file_label.setText(f"File: {os.path.basename(file_path)}")
                    self.update_status()
                except Exception as e:
                    QMessageBox.warning(self, "Error", f"Failed to load scene:\n{str(e)}")
    
    def save_current(self):
        if self.current_mode == "tile":
            if self.current_level_file:
                success, message = self.file_manager.save_level(self.current_level_file, self.level_canvas)
                if not success:
                    QMessageBox.warning(self, "Error", message)
            else:
                self.save_level_as()
        else:
            if self.scene_data:
                if self.current_scene_file:
                    try:
                        SceneParser.write_scene(self.current_scene_file, self.scene_data)
                    except Exception as e:
                        QMessageBox.warning(self, "Error", f"Failed to save:\n{str(e)}")
                else:
                    self.save_scene_as()
    
    def save_level_as(self):
        file_path, _ = QFileDialog.getSaveFileName(
            self, "Save Level", "", "JSON Files (*.json)")
        
        if file_path:
            success, message = self.file_manager.save_level(file_path, self.level_canvas)
            if success:
                self.current_level_file = file_path
                self.file_label.setText(f"File: {os.path.basename(file_path)}")
            else:
                QMessageBox.warning(self, "Error", message)
    
    def save_scene_as(self):
        file_path, _ = QFileDialog.getSaveFileName(
            self, "Save Scene", "", "Scene Files (*.scene)")
        
        if file_path:
            try:
                SceneParser.write_scene(file_path, self.scene_data)
                self.current_scene_file = file_path
                self.file_label.setText(f"File: {os.path.basename(file_path)}")
            except Exception as e:
                QMessageBox.warning(self, "Error", f"Failed to save:\n{str(e)}")
    
    def load_tileset(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Load Tileset", "", "Image Files (*.png *.jpg *.jpeg *.bmp)")
        
        if file_path:
            self.level_canvas.set_tileset(file_path)
            if self.level_canvas.tile_surfaces:
                self.tile_palette.set_tiles(self.level_canvas.tile_surfaces)
                self.tile_palette_dock.show()
                
                # Share with scene canvas
                self.scene_canvas.tile_surfaces = self.level_canvas.tile_surfaces
                self.scene_canvas.tileset_image = self.level_canvas.tileset_image
                self.scene_canvas.tile_size = self.level_canvas.tile_size
    
    def set_tile_tool(self, tool):
        self.level_canvas.set_tool(tool)
    
    def on_tile_selected(self, tile_index):
        self.level_canvas.selected_tile = tile_index
    
    def on_entity_tool_selected(self, entity_type):
        self.scene_canvas.set_entity_tool(entity_type)
    
    def on_entity_selected(self, entity):
        self.entity_properties.set_entity(entity)
        self.scene_canvas.update()
    
    def on_entity_moved(self, entity):
        self.entity_properties.set_entity(entity)
    
    def on_property_changed(self, entity):
        self.scene_canvas.update()
    
    def delete_selected_entity(self):
        if self.scene_canvas.selected_entity:
            self.scene_canvas.delete_entity(self.scene_canvas.selected_entity)
    
    def zoom_in(self):
        if self.current_mode == "tile":
            self.level_canvas.zoom = min(self.level_canvas.max_zoom, self.level_canvas.zoom * 1.3)
            self.level_canvas.update()
        else:
            self.scene_canvas.zoom = min(self.scene_canvas.max_zoom, self.scene_canvas.zoom * 1.3)
            self.scene_canvas.update()
        self.update_status()
    
    def zoom_out(self):
        if self.current_mode == "tile":
            self.level_canvas.zoom = max(self.level_canvas.min_zoom, self.level_canvas.zoom / 1.3)
            self.level_canvas.update()
        else:
            self.scene_canvas.zoom = max(self.scene_canvas.min_zoom, self.scene_canvas.zoom / 1.3)
            self.scene_canvas.update()
        self.update_status()
    
    def reset_view(self):
        if self.current_mode == "tile":
            self.level_canvas.reset_view()
        else:
            self.scene_canvas.reset_view()
        self.update_status()
    
    def toggle_grid(self):
        if self.current_mode == "tile":
            self.level_canvas.show_grid = not self.level_canvas.show_grid
            self.level_canvas.update()
        else:
            self.scene_canvas.show_grid = not self.scene_canvas.show_grid
            self.scene_canvas.update()
    
    def update_mouse_pos(self, x, y, tile_id=0, has_collision=False):
        self.mouse_pos_label.setText(f"Position: ({x}, {y})")
        if self.current_mode == "tile":
            self.info_label.setText(f"Tile: {tile_id} | Collision: {'Yes' if has_collision else 'No'}")
    
    def update_status(self):
        if self.current_mode == "tile":
            self.zoom_label.setText(f"Zoom: {int(self.level_canvas.zoom * 100)}%")
        else:
            self.zoom_label.setText(f"Zoom: {int(self.scene_canvas.zoom * 100)}%")
