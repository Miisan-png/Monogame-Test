import sys
import os
from PyQt5.QtWidgets import QApplication
from PyQt5.QtGui import QPalette, QColor
from PyQt5.QtCore import Qt

os.environ["QT_AUTO_SCREEN_SCALE_FACTOR"] = "1"
os.environ["QT_SCALE_FACTOR"] = "0.8"

def main():
    QApplication.setAttribute(Qt.AA_EnableHighDpiScaling, True)
    QApplication.setAttribute(Qt.AA_UseHighDpiPixmaps, True)
    
    app = QApplication(sys.argv)
    app.setStyle('Fusion')
    
    font = app.font()
    font.setPointSize(10)
    app.setFont(font)
    
    dark_palette = QPalette()
    dark_palette.setColor(QPalette.Window, QColor(30, 30, 30))
    dark_palette.setColor(QPalette.WindowText, QColor(220, 220, 220))
    dark_palette.setColor(QPalette.Base, QColor(20, 20, 20))
    dark_palette.setColor(QPalette.AlternateBase, QColor(35, 35, 35))
    dark_palette.setColor(QPalette.ToolTipBase, QColor(40, 40, 40))
    dark_palette.setColor(QPalette.ToolTipText, QColor(220, 220, 220))
    dark_palette.setColor(QPalette.Text, QColor(220, 220, 220))
    dark_palette.setColor(QPalette.Button, QColor(40, 40, 40))
    dark_palette.setColor(QPalette.ButtonText, QColor(220, 220, 220))
    dark_palette.setColor(QPalette.BrightText, QColor(255, 100, 100))
    dark_palette.setColor(QPalette.Link, QColor(80, 180, 255))
    dark_palette.setColor(QPalette.Highlight, QColor(60, 140, 200))
    dark_palette.setColor(QPalette.HighlightedText, QColor(255, 255, 255))
    
    app.setPalette(dark_palette)
    
    app.setStyleSheet("""
        QWidget {
            font-size: 11pt;
        }
        QLabel {
            font-size: 11pt;
        }
        QPushButton {
            font-size: 11pt;
            min-height: 28px;
            padding: 6px 14px;
        }
        QSpinBox, QDoubleSpinBox, QLineEdit {
            font-size: 11pt;
            min-height: 26px;
            padding: 4px 8px;
        }
        QGroupBox {
            font-size: 11pt;
            font-weight: bold;
            padding-top: 16px;
            margin-top: 8px;
        }
        QMenuBar {
            font-size: 11pt;
            background-color: #2d2d2d;
            padding: 6px;
        }
        QMenuBar::item {
            padding: 6px 14px;
        }
        QStatusBar {
            font-size: 11pt;
            background-color: #2d2d2d;
            border-top: 1px solid #404040;
            padding: 6px;
        }
        QToolBar {
            spacing: 8px;
            padding: 10px;
        }
        QDockWidget::title {
            font-size: 11pt;
            font-weight: bold;
            padding: 8px;
        }
    """)
    
    from unified_editor_window import UnifiedEditorWindow
    editor = UnifiedEditorWindow()
    editor.show()
    
    sys.exit(app.exec_())

if __name__ == "__main__":
    main()
