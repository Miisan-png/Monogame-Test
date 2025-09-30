import os
from PyQt5.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout, 
                             QAction, QFileDialog, QMessageBox, QLabel, QToolBar,
                             QPushButton, QButtonGroup, QSplitter, QDockWidget)
from PyQt5.QtCore import Qt
from PyQt5.QtGui import QIcon
from level_canvas import LevelCanvas
from tile_palette_window import TilePaletteWindow
from properties_panel import PropertiesPanel
from file_manager import FileManager

class EditorWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Snow Engine - Level Editor")
        self.setGeometry(100, 100, 1600, 900)
        
        self.current_file = None
        self.file_manager = FileManager()
        
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
        
        self.canvas = LevelCanvas(self)
        
        self.palette_widget = TilePaletteWindow(self)
        self.palette_widget.tileSelected.connect(self.on_tile_selected)
        
        self.palette_dock = QDockWidget("Tile Palette", self)
        self.palette_dock.setWidget(self.palette_widget)
        self.palette_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea | Qt.BottomDockWidgetArea)
        self.palette_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.RightDockWidgetArea, self.palette_dock)
        
        self.properties = PropertiesPanel(self.canvas, self)
        
        self.properties_dock = QDockWidget("Properties", self)
        self.properties_dock.setWidget(self.properties)
        self.properties_dock.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)
        self.properties_dock.setFeatures(QDockWidget.DockWidgetMovable | QDockWidget.DockWidgetFloatable)
        self.addDockWidget(Qt.LeftDockWidgetArea, self.properties_dock)
        
        main_layout.addWidget(self.canvas)
    
    def init_menus(self):
        menubar = self.menuBar()
        menubar.setStyleSheet("QMenuBar { background-color: #2d2d2d; padding: 4px; } QMenuBar::item { padding: 4px 12px; } QMenuBar::item:selected { background-color: #3c3c3c; }")
        
        file_menu = menubar.addMenu("File")
        
        new_action = QAction("New Level", self)
        new_action.setShortcut("Ctrl+N")
        new_action.triggered.connect(self.new_file)
        file_menu.addAction(new_action)
        
        open_action = QAction("Open Level", self)
        open_action.setShortcut("Ctrl+O")
        open_action.triggered.connect(self.open_file)
        file_menu.addAction(open_action)
        
        save_action = QAction("Save", self)
        save_action.setShortcut("Ctrl+S")
        save_action.triggered.connect(self.save_file)
        file_menu.addAction(save_action)
        
        save_as_action = QAction("Save As...", self)
        save_as_action.setShortcut("Ctrl+Shift+S")
        save_as_action.triggered.connect(self.save_file_as)
        file_menu.addAction(save_as_action)
        
        file_menu.addSeparator()
        
        load_tileset_action = QAction("Load Tileset", self)
        load_tileset_action.setShortcut("Ctrl+T")
        load_tileset_action.triggered.connect(self.load_tileset)
        file_menu.addAction(load_tileset_action)
        
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
        grid_action.triggered.connect(lambda: self.properties.grid_checkbox.setChecked(not self.properties.grid_checkbox.isChecked()))
        view_menu.addAction(grid_action)
        
        viewport_action = QAction("Toggle Viewport (V)", self)
        viewport_action.triggered.connect(lambda: self.properties.viewport_checkbox.setChecked(not self.properties.viewport_checkbox.isChecked()))
        view_menu.addAction(viewport_action)
        
        collision_action = QAction("Toggle Collision (C)", self)
        collision_action.triggered.connect(lambda: self.properties.collision_checkbox.setChecked(not self.properties.collision_checkbox.isChecked()))
        view_menu.addAction(collision_action)
    
    def init_toolbar(self):
        toolbar = QToolBar("Tools")
        toolbar.setMovable(False)
        toolbar.setStyleSheet("QToolBar { background-color: #2d2d2d; padding: 6px; spacing: 4px; border-bottom: 1px solid #404040; }")
        self.addToolBar(toolbar)
        
        file_section = QWidget()
        file_layout = QHBoxLayout(file_section)
        file_layout.setContentsMargins(0, 0, 16, 0)
        file_layout.setSpacing(4)
        
        new_btn = self.create_tool_button("New", self.new_file)
        open_btn = self.create_tool_button("Open", self.open_file)
        save_btn = self.create_tool_button("Save", self.save_file)
        
        file_layout.addWidget(new_btn)
        file_layout.addWidget(open_btn)
        file_layout.addWidget(save_btn)
        toolbar.addWidget(file_section)
        
        toolbar.addSeparator()
        
        tileset_btn = self.create_tool_button("Load Tileset", self.load_tileset)
        tileset_btn.setStyleSheet(tileset_btn.styleSheet() + "QPushButton { padding: 6px 16px; }")
        toolbar.addWidget(tileset_btn)
        
        palette_btn = self.create_tool_button("Show Palette", self.show_palette)
        palette_btn.setStyleSheet(palette_btn.styleSheet() + "QPushButton { padding: 6px 16px; }")
        toolbar.addWidget(palette_btn)
        
        toolbar.addSeparator()
        
        tools_section = QWidget()
        tools_layout = QHBoxLayout(tools_section)
        tools_layout.setContentsMargins(0, 0, 0, 0)
        tools_layout.setSpacing(4)
        
        self.tool_group = QButtonGroup(self)
        
        brush_btn = self.create_toggle_button("Brush", "brush")
        brush_btn.setChecked(True)
        
        rect_btn = self.create_toggle_button("Rectangle", "rect")
        fill_btn = self.create_toggle_button("Fill", "fill")
        collision_btn = self.create_toggle_button("Collision", "collision")
        eraser_btn = self.create_toggle_button("Eraser", "eraser")
        
        self.tool_group.addButton(brush_btn)
        self.tool_group.addButton(rect_btn)
        self.tool_group.addButton(fill_btn)
        self.tool_group.addButton(collision_btn)
        self.tool_group.addButton(eraser_btn)
        
        tools_layout.addWidget(brush_btn)
        tools_layout.addWidget(rect_btn)
        tools_layout.addWidget(fill_btn)
        tools_layout.addWidget(collision_btn)
        tools_layout.addWidget(eraser_btn)
        
        toolbar.addWidget(tools_section)
    
    def create_tool_button(self, text, callback):
        btn = QPushButton(text)
        btn.clicked.connect(callback)
        btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 4px; padding: 8px 14px; color: #e0e0e0; font-size: 13px; } QPushButton:hover { background-color: #484848; } QPushButton:pressed { background-color: #2d2d2d; }")
        return btn
    
    def create_toggle_button(self, text, tool):
        btn = QPushButton(text)
        btn.setCheckable(True)
        btn.clicked.connect(lambda: self.set_tool(tool))
        btn.setStyleSheet("QPushButton { background-color: #383838; border: 1px solid #505050; border-radius: 4px; padding: 8px 14px; color: #e0e0e0; font-size: 13px; } QPushButton:hover { background-color: #484848; } QPushButton:checked { background-color: #3c7fb0; border-color: #5090c0; } QPushButton:pressed { background-color: #2d2d2d; }")
        return btn
    
    def init_status_bar(self):
        self.status_bar = self.statusBar()
        self.status_bar.setStyleSheet("QStatusBar { background-color: #2d2d2d; border-top: 1px solid #404040; padding: 4px; }")
        
        self.mouse_pos_label = QLabel("Position: (0, 0)")
        self.tile_info_label = QLabel("Tile: 0")
        self.collision_info_label = QLabel("Collision: No")
        self.zoom_label = QLabel("Zoom: 200%")
        self.tool_label = QLabel("Tool: Brush")
        
        for label in [self.mouse_pos_label, self.tile_info_label, self.collision_info_label, self.zoom_label, self.tool_label]:
            label.setStyleSheet("color: #b0b0b0; padding: 0 8px;")
        
        self.status_bar.addWidget(self.mouse_pos_label)
        self.status_bar.addWidget(self.tile_info_label)
        self.status_bar.addWidget(self.collision_info_label)
        self.status_bar.addPermanentWidget(self.tool_label)
        self.status_bar.addPermanentWidget(self.zoom_label)
    
    def set_tool(self, tool):
        self.canvas.set_tool(tool)
        tool_names = {
            "brush": "Brush",
            "rect": "Rectangle",
            "fill": "Fill",
            "collision": "Collision",
            "eraser": "Eraser"
        }
        self.tool_label.setText(f"Tool: {tool_names.get(tool, 'Unknown')}")
    
    def on_tile_selected(self, tile_index):
        self.canvas.selected_tile = tile_index
    
    def load_tileset(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Load Tileset", "", 
            "Image Files (*.png *.jpg *.jpeg *.bmp)")
        
        if file_path:
            print(f"Selected tileset: {file_path}")
            self.canvas.set_tileset(file_path)
            if self.canvas.tile_surfaces:
                print(f"Updating palette with {len(self.canvas.tile_surfaces)} tiles")
                self.palette_widget.set_tiles(self.canvas.tile_surfaces)
                self.palette_dock.show()
            else:
                print("ERROR: Canvas has no tile surfaces after loading!")
    
    def show_palette(self):
        if self.palette_widget.tile_surfaces:
            self.palette_dock.show()
            self.palette_dock.raise_()
        else:
            QMessageBox.information(self, "No Tileset", "Load a tileset first before opening the palette.")
    
    def new_file(self):
        self.canvas.clear_world()
        self.canvas.clear_collisions()
        self.current_file = None
        self.setWindowTitle("Snow Engine - Level Editor")
    
    def open_file(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Open Level", "", "JSON Files (*.json)")
        
        if file_path:
            success, message = self.file_manager.load_level(file_path, self.canvas)
            if success:
                self.current_file = file_path
                self.setWindowTitle(f"Snow Engine - Level Editor - {os.path.basename(file_path)}")
                self.properties.world_width_spin.setValue(self.canvas.grid_width)
                self.properties.world_height_spin.setValue(self.canvas.grid_height)
                self.properties.tile_size_spin.setValue(self.canvas.tile_size)
                self.properties.viewport_width_spin.setValue(self.canvas.viewport_width)
                self.properties.viewport_height_spin.setValue(self.canvas.viewport_height)
            else:
                QMessageBox.warning(self, "Error", message)
    
    def save_file(self):
        if self.current_file:
            success, message = self.file_manager.save_level(self.current_file, self.canvas)
            if not success:
                QMessageBox.warning(self, "Error", message)
        else:
            self.save_file_as()
    
    def save_file_as(self):
        file_path, _ = QFileDialog.getSaveFileName(
            self, "Save Level", "", "JSON Files (*.json)")
        
        if file_path:
            success, message = self.file_manager.save_level(file_path, self.canvas)
            if success:
                self.current_file = file_path
                self.setWindowTitle(f"Snow Engine - Level Editor - {os.path.basename(file_path)}")
            else:
                QMessageBox.warning(self, "Error", message)
    
    def zoom_in(self):
        self.canvas.zoom = min(self.canvas.max_zoom, self.canvas.zoom * 1.3)
        self.canvas.update()
        self.update_status()
    
    def zoom_out(self):
        self.canvas.zoom = max(self.canvas.min_zoom, self.canvas.zoom / 1.3)
        self.canvas.update()
        self.update_status()
    
    def update_mouse_pos(self, x, y, tile_id=0, has_collision=False):
        self.mouse_pos_label.setText(f"Position: ({x}, {y})")
        self.tile_info_label.setText(f"Tile: {tile_id}")
        self.collision_info_label.setText(f"Collision: {'Yes' if has_collision else 'No'}")
    
    def update_status(self):
        self.zoom_label.setText(f"Zoom: {int(self.canvas.zoom * 100)}%")
