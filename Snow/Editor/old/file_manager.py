import json
import os

class FileManager:
    @staticmethod
    def save_level(file_path, canvas):
        try:
            data = {
                'tile_size': canvas.tile_size,
                'grid_width': canvas.grid_width,
                'grid_height': canvas.grid_height,
                'viewport_width': canvas.viewport_width,
                'viewport_height': canvas.viewport_height,
                'world_data': canvas.world_data.tolist(),
                'collision_data': canvas.collision_data.tolist()
            }
            
            with open(file_path, 'w') as f:
                json.dump(data, f, indent=2)
            
            return True, "Level saved successfully"
        except Exception as e:
            return False, f"Failed to save: {str(e)}"
    
    @staticmethod
    def load_level(file_path, canvas):
        try:
            with open(file_path, 'r') as f:
                data = json.load(f)
            
            import numpy as np
            
            canvas.tile_size = data.get('tile_size', 16)
            canvas.grid_width = data.get('grid_width', 100)
            canvas.grid_height = data.get('grid_height', 100)
            canvas.viewport_width = data.get('viewport_width', 320)
            canvas.viewport_height = data.get('viewport_height', 180)
            
            world_data = data.get('world_data', [])
            if world_data:
                canvas.world_data = np.array(world_data, dtype=int)
            else:
                canvas.world_data = np.zeros((canvas.grid_height, canvas.grid_width), dtype=int)
            
            collision_data = data.get('collision_data', [])
            if collision_data:
                canvas.collision_data = np.array(collision_data, dtype=bool)
            else:
                canvas.collision_data = np.zeros((canvas.grid_height, canvas.grid_width), dtype=bool)
            
            canvas.update()
            
            return True, "Level loaded successfully"
        except Exception as e:
            return False, f"Failed to load: {str(e)}"
