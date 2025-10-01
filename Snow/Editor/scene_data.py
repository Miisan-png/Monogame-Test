class SceneData:
    def __init__(self):
        self.name = "New Scene"
        self.tilemap = ""
        self.tileset = ""
        self.background_color = "#87CEEB"
        self.entities = []
        self.particle_emitters = []

class EntityData:
    def __init__(self, entity_id="", entity_type="", x=0, y=0):
        self.id = entity_id
        self.type = entity_type
        self.x = x
        self.y = y
        self.properties = {}
        self.sprite = ""
        self.animations = {}

class ParticleEmitterData:
    def __init__(self, emitter_id="", x=0, y=0):
        self.id = emitter_id
        self.x = x
        self.y = y
        self.type = ""
        self.particle_texture = ""
        self.emission_rate = 20
        self.properties = {}

class SceneParser:
    @staticmethod
    def parse_scene(filepath):
        scene_data = SceneData()
        
        with open(filepath, 'r') as f:
            lines = f.readlines()
        
        current_section = None
        current_entity = None
        current_emitter = None
        
        for line in lines:
            line = line.strip()
            
            if not line or line.startswith('#'):
                continue
            
            if line.startswith('[') and line.endswith(']'):
                if current_entity:
                    scene_data.entities.append(current_entity)
                    current_entity = None
                if current_emitter:
                    scene_data.particle_emitters.append(current_emitter)
                    current_emitter = None
                
                current_section = line[1:-1]
                
                if current_section.startswith('entity'):
                    current_entity = EntityData()
                    entity_id = SceneParser._extract_attribute(current_section, 'id')
                    if entity_id:
                        current_entity.id = entity_id
                
                elif current_section.startswith('particle_emitter'):
                    current_emitter = ParticleEmitterData()
                    emitter_id = SceneParser._extract_attribute(current_section, 'id')
                    if emitter_id:
                        current_emitter.id = emitter_id
                
                continue
            
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip()
                
                if current_section == 'scene':
                    SceneParser._parse_scene_property(scene_data, key, value)
                elif current_entity:
                    SceneParser._parse_entity_property(current_entity, key, value)
                elif current_emitter:
                    SceneParser._parse_emitter_property(current_emitter, key, value)
        
        if current_entity:
            scene_data.entities.append(current_entity)
        if current_emitter:
            scene_data.particle_emitters.append(current_emitter)
        
        return scene_data
    
    @staticmethod
    def _parse_scene_property(scene_data, key, value):
        value = value.strip('"')
        if key == 'name':
            scene_data.name = value
        elif key == 'tilemap':
            scene_data.tilemap = value
        elif key == 'tileset':
            scene_data.tileset = value
        elif key == 'background_color':
            scene_data.background_color = value
    
    @staticmethod
    def _parse_entity_property(entity, key, value):
        if key == 'type':
            entity.type = value.strip('"')
        elif key == 'sprite':
            entity.sprite = value.strip('"')
        elif key == 'position':
            parts = value.split(',')
            entity.x = float(parts[0].strip())
            entity.y = float(parts[1].strip())
        elif key == 'properties':
            entity.properties = SceneParser._parse_dict(value)
        elif key == 'animations':
            entity.animations = SceneParser._parse_animations(value)
    
    @staticmethod
    def _parse_emitter_property(emitter, key, value):
        if key == 'position':
            parts = value.split(',')
            emitter.x = float(parts[0].strip())
            emitter.y = float(parts[1].strip())
        elif key == 'type':
            emitter.type = value.strip('"')
        elif key == 'particle_texture':
            emitter.particle_texture = value.strip('"')
        elif key == 'emission_rate':
            emitter.emission_rate = int(value)
        elif key == 'properties':
            emitter.properties = SceneParser._parse_dict(value)
    
    @staticmethod
    def _parse_dict(value):
        result = {}
        value = value.strip('{}').strip()
        if not value:
            return result
        
        in_quotes = False
        current_token = ""
        tokens = []
        
        for char in value:
            if char == '"':
                in_quotes = not in_quotes
                current_token += char
            elif char == ',' and not in_quotes:
                tokens.append(current_token.strip())
                current_token = ""
            else:
                current_token += char
        
        if current_token:
            tokens.append(current_token.strip())
        
        for token in tokens:
            if ':' in token:
                k, v = token.split(':', 1)
                k = k.strip().strip('"')
                v = v.strip().strip('"')
                
                try:
                    if '.' in v:
                        result[k] = float(v)
                    else:
                        result[k] = int(v)
                except ValueError:
                    if v.lower() == 'true':
                        result[k] = True
                    elif v.lower() == 'false':
                        result[k] = False
                    else:
                        result[k] = v
        
        return result
    
    @staticmethod
    def _parse_animations(value):
        result = {}
        return result
    
    @staticmethod
    def _extract_attribute(section, attribute):
        start = section.find(f'{attribute}="')
        if start == -1:
            return None
        start += len(attribute) + 2
        end = section.find('"', start)
        if end == -1:
            return None
        return section[start:end]
    
    @staticmethod
    def write_scene(filepath, scene_data):
        with open(filepath, 'w') as f:
            f.write('[scene]\n')
            f.write(f'name = "{scene_data.name}"\n')
            f.write(f'tilemap = "{scene_data.tilemap}"\n')
            f.write(f'tileset = "{scene_data.tileset}"\n')
            f.write(f'background_color = {scene_data.background_color}\n')
            f.write('\n')
            
            for entity in scene_data.entities:
                f.write(f'[entity id="{entity.id}"]\n')
                f.write(f'type = "{entity.type}"\n')
                f.write(f'position = {entity.x}, {entity.y}\n')
                
                if entity.properties:
                    props_str = ', '.join([f'"{k}": {SceneParser._format_value(v)}' 
                                          for k, v in entity.properties.items()])
                    f.write(f'properties = {{{props_str}}}\n')
                
                if entity.sprite:
                    f.write(f'sprite = "{entity.sprite}"\n')
                
                f.write('\n')
            
            for emitter in scene_data.particle_emitters:
                f.write(f'[particle_emitter id="{emitter.id}"]\n')
                f.write(f'position = {emitter.x}, {emitter.y}\n')
                f.write(f'type = "{emitter.type}"\n')
                if emitter.particle_texture:
                    f.write(f'particle_texture = "{emitter.particle_texture}"\n')
                f.write(f'emission_rate = {emitter.emission_rate}\n')
                f.write('\n')
    
    @staticmethod
    def _format_value(value):
        if isinstance(value, str):
            return f'"{value}"'
        elif isinstance(value, bool):
            return 'true' if value else 'false'
        else:
            return str(value)
