from PyQt5.QtWidgets import (QWidget, QVBoxLayout, QFormLayout, QLabel, 
                             QLineEdit, QSpinBox, QDoubleSpinBox, QPushButton,
                             QGroupBox, QHBoxLayout, QCheckBox)
from PyQt5.QtCore import Qt, pyqtSignal

class EntityPropertiesPanel(QWidget):
    propertyChanged = pyqtSignal(object)
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedWidth(280)
        self.current_entity = None
        self.property_widgets = {}
        self.init_ui()
    
    def init_ui(self):
        layout = QVBoxLayout(self)
        layout.setContentsMargins(8, 8, 8, 8)
        layout.setSpacing(12)
        
        header = QLabel("Entity Properties")
        header.setStyleSheet("font-weight: bold; font-size: 14px; color: #e0e0e0; padding: 4px;")
        layout.addWidget(header)
        
        self.basic_group = QGroupBox("Basic")
        self.basic_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        basic_layout = QFormLayout(self.basic_group)
        basic_layout.setSpacing(8)
        
        self.id_edit = QLineEdit()
        self.id_edit.setStyleSheet("QLineEdit { padding: 4px; }")
        self.id_edit.textChanged.connect(self.on_property_changed)
        basic_layout.addRow("ID:", self.id_edit)
        
        self.type_label = QLabel("-")
        self.type_label.setStyleSheet("color: #b0b0b0;")
        basic_layout.addRow("Type:", self.type_label)
        
        self.x_spin = QDoubleSpinBox()
        self.x_spin.setRange(-10000, 10000)
        self.x_spin.setDecimals(0)
        self.x_spin.setStyleSheet("QDoubleSpinBox { padding: 4px; }")
        self.x_spin.valueChanged.connect(self.on_property_changed)
        basic_layout.addRow("X:", self.x_spin)
        
        self.y_spin = QDoubleSpinBox()
        self.y_spin.setRange(-10000, 10000)
        self.y_spin.setDecimals(0)
        self.y_spin.setStyleSheet("QDoubleSpinBox { padding: 4px; }")
        self.y_spin.valueChanged.connect(self.on_property_changed)
        basic_layout.addRow("Y:", self.y_spin)
        
        layout.addWidget(self.basic_group)
        
        self.custom_group = QGroupBox("Custom Properties")
        self.custom_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        self.custom_layout = QFormLayout(self.custom_group)
        self.custom_layout.setSpacing(8)
        
        layout.addWidget(self.custom_group)
        
        actions_group = QGroupBox("Actions")
        actions_group.setStyleSheet("QGroupBox { font-weight: bold; font-size: 13px; padding-top: 14px; }")
        actions_layout = QVBoxLayout(actions_group)
        actions_layout.setSpacing(8)
        
        self.delete_btn = QPushButton("Delete Entity")
        self.delete_btn.setStyleSheet("""
            QPushButton {
                padding: 6px;
                background-color: #c84040;
                border: 1px solid #d85050;
                border-radius: 4px;
                color: white;
            }
            QPushButton:hover {
                background-color: #d85050;
            }
            QPushButton:disabled {
                background-color: #383838;
                color: #606060;
                border-color: #505050;
            }
        """)
        self.delete_btn.clicked.connect(self.on_delete_clicked)
        self.delete_btn.setEnabled(False)
        actions_layout.addWidget(self.delete_btn)
        
        layout.addWidget(actions_group)
        
        layout.addStretch()
        
        self.set_entity(None)
    
    def set_entity(self, entity):
        self.current_entity = entity
        
        if entity is None:
            self.basic_group.setEnabled(False)
            self.custom_group.setEnabled(False)
            self.delete_btn.setEnabled(False)
            self.id_edit.clear()
            self.type_label.setText("-")
            self.x_spin.setValue(0)
            self.y_spin.setValue(0)
            self.clear_custom_properties()
            return
        
        self.basic_group.setEnabled(True)
        self.custom_group.setEnabled(True)
        self.delete_btn.setEnabled(True)
        
        self.id_edit.blockSignals(True)
        self.x_spin.blockSignals(True)
        self.y_spin.blockSignals(True)
        
        self.id_edit.setText(entity.id)
        self.type_label.setText(entity.type)
        self.x_spin.setValue(entity.x)
        self.y_spin.setValue(entity.y)
        
        self.id_edit.blockSignals(False)
        self.x_spin.blockSignals(False)
        self.y_spin.blockSignals(False)
        
        self.load_custom_properties()
    
    def load_custom_properties(self):
        self.clear_custom_properties()
        
        if not self.current_entity or not self.current_entity.properties:
            return
        
        for key, value in self.current_entity.properties.items():
            if isinstance(value, bool):
                widget = QCheckBox()
                widget.setChecked(value)
                widget.toggled.connect(lambda v, k=key: self.on_custom_property_changed(k, v))
            elif isinstance(value, int):
                widget = QSpinBox()
                widget.setRange(-100000, 100000)
                widget.setValue(value)
                widget.valueChanged.connect(lambda v, k=key: self.on_custom_property_changed(k, v))
                widget.setStyleSheet("QSpinBox { padding: 4px; }")
            elif isinstance(value, float):
                widget = QDoubleSpinBox()
                widget.setRange(-100000, 100000)
                widget.setValue(value)
                widget.valueChanged.connect(lambda v, k=key: self.on_custom_property_changed(k, v))
                widget.setStyleSheet("QDoubleSpinBox { padding: 4px; }")
            else:
                widget = QLineEdit(str(value))
                widget.textChanged.connect(lambda v, k=key: self.on_custom_property_changed(k, v))
                widget.setStyleSheet("QLineEdit { padding: 4px; }")
            
            self.property_widgets[key] = widget
            self.custom_layout.addRow(f"{key}:", widget)
    
    def clear_custom_properties(self):
        while self.custom_layout.count():
            item = self.custom_layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()
        self.property_widgets.clear()
    
    def on_property_changed(self):
        if not self.current_entity:
            return
        
        self.current_entity.id = self.id_edit.text()
        self.current_entity.x = self.x_spin.value()
        self.current_entity.y = self.y_spin.value()
        
        self.propertyChanged.emit(self.current_entity)
    
    def on_custom_property_changed(self, key, value):
        if not self.current_entity:
            return
        
        self.current_entity.properties[key] = value
        self.propertyChanged.emit(self.current_entity)
    
    def on_delete_clicked(self):
        if self.current_entity and hasattr(self.parent(), 'delete_selected_entity'):
            self.parent().delete_selected_entity()
