import sys
from PyQt5.QtWidgets import QApplication
from PyQt5.QtGui import QPalette, QColor
from unified_editor_window import UnifiedEditorWindow

def main():
    app = QApplication(sys.argv)
    app.setStyle('Fusion')
    
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
    
    editor = UnifiedEditorWindow()
    editor.show()
    
    sys.exit(app.exec_())

if __name__ == "__main__":
    main()
