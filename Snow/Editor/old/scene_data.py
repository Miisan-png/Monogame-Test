class SceneData:
    def __init__(self):
        self.name = "New Scene"
        self.tilemap = ""
        self.tileset = ""
        self.background_color = "#000000"
        self.entities = []
        self.particle_emitters = []
        self.lights = []
        self.audio_sources = []
        self.triggers = []
        self.spawn_points = []
        self.camera_settings = CameraSettingsData()
        self.properties = {}

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

class LightData:
    def __init__(self, light_id="", x=0, y=0):
        self.id = light_id
        self.x = x
        self.y = y
        self.radius = 80.0
        self.color = "#ffffff"
        self.intensity = 1.0
        self.type = "point"
        self.properties = {}

class AudioSourceData:
    def __init__(self, audio_id="", x=0, y=0):
        self.id = audio_id
        self.x = x
        self.y = y
        self.audio_file = ""
        self.volume = 1.0
        self.loop = False
        self.autoplay = False
        self.min_distance = 50.0
        self.max_distance = 300.0
        self.properties = {}

class TriggerData:
    def __init__(self, trigger_id="", x=0, y=0):
        self.id = trigger_id
        self.x = x
        self.y = y
        self.width = 32.0
        self.height = 32.0
        self.trigger_type = ""
        self.action = ""
        self.properties = {}

class SpawnPointData:
    def __init__(self, spawn_id="", x=0, y=0):
        self.id = spawn_id
        self.x = x
        self.y = y
        self.entity_type = ""
        self.spawn_delay = 0.0
        self.max_spawns = -1
        self.properties = {}

class CameraSettingsData:
    def __init__(self):
        self.room_width = 320
        self.room_height = 180
        self.follow_mode = "room"
        self.smooth_speed = 0.0
        self.properties = {}

class SceneParser:
    @staticmethod
    def parse_scene(filepath):
        scene_data = SceneData()
        
        with open(filepath, 'r') as f:
            lines = f.readlines()
        
        current_section = None
        current_object = None
        
        for line in lines:
            line = line.strip()
            
            if not line or line.startswith('#'):
                continue
            
            if line.startswith('[') and line.endswith(']'):
                if current_object:
                    SceneParser._add_object(scene_data, current_section, current_object)
                    current_object = None
                
                current_section = line[1:-1]
                current_object = SceneParser._create_object(current_section)
                continue
            
            if '=' in line:
                key, value = line.split('=', 1)
                key = key.strip()
                value = value.strip()
                
                if current_section == 'scene':
                    SceneParser._parse_scene_property(scene_data, key, value)
                elif current_object:
                    SceneParser._parse_object_property(current_object, current_section, key, value)
        
        if current_object:
            SceneParser._add_object(scene_data, current_section, current_object)
        
        return scene_data
    
    @staticmethod
    def _create_object(section):
        if section.startswith('entity'):
            return EntityData(entity_id=SceneParser._extract_id(section))
        elif section.startswith('particle_emitter'):
            return ParticleEmitterData(emitter_id=SceneParser._extract_id(section))
        elif section.startswith('light'):
            return LightData(light_id=SceneParser._extract_id(section))
        elif section.startswith('audio'):
            return AudioSourceData(audio_id=SceneParser._extract_id(section))
        elif section.startswith('trigger'):
            return TriggerData(trigger_id=SceneParser._extract_id(section))
        elif section.startswith('spawn_point'):
            return SpawnPointData(spawn_id=SceneParser._extract_id(section))
        elif section == 'camera':
            return CameraSettingsData()
        return None
    
    @staticmethod
    def _add_object(scene_data, section, obj):
        if isinstance(obj, EntityData):
            scene_data.entities.append(obj)
        elif isinstance(obj, ParticleEmitterData):
            scene_data.particle_emitters.append(obj)
        elif isinstance(obj, LightData):
            scene_data.lights.append(obj)
        elif isinstance(obj, AudioSourceData):
            scene_data.audio_sources.append(obj)
        elif isinstance(obj, TriggerData):
            scene_data.triggers.append(obj)
        elif isinstance(obj, SpawnPointData):
            scene_data.spawn_points.append(obj)
        elif isinstance(obj, CameraSettingsData):
            scene_data.camera_settings = obj
    
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
        else:
            scene_data.properties[key] = SceneParser._parse_value(value)
    
    @staticmethod
    def _parse_object_property(obj, section, key, value):
        if isinstance(obj, EntityData):
            SceneParser._parse_entity_property(obj, key, value)
        elif isinstance(obj, LightData):
            SceneParser._parse_light_property(obj, key, value)
        elif isinstance(obj, AudioSourceData):
            SceneParser._parse_audio_property(obj, key, value)
        elif isinstance(obj, TriggerData):
            SceneParser._parse_trigger_property(obj, key, value)
        elif isinstance(obj, SpawnPointData):
            SceneParser._parse_spawn_property(obj, key, value)
        elif isinstance(obj, ParticleEmitterData):
            SceneParser._parse_emitter_property(obj, key, value)
        elif isinstance(obj, CameraSettingsData):
            SceneParser._parse_camera_property(obj, key, value)
    
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
    def _parse_light_property(light, key, value):
        if key == 'position':
            parts = value.split(',')
            light.x = float(parts[0].strip())
            light.y = float(parts[1].strip())
        elif key == 'radius':
            light.radius = float(value)
        elif key == 'color':
            light.color = value.strip('"')
        elif key == 'intensity':
            light.intensity = float(value)
        elif key == 'type':
            light.type = value.strip('"')
        elif key == 'properties':
            light.properties = SceneParser._parse_dict(value)
    
    @staticmethod
    def _parse_audio_property(audio, key, value):
        if key == 'position':
            parts = value.split(',')
            audio.x = float(parts[0].strip())
            audio.y = float(parts[1].strip())
        elif key == 'audio_file':
            audio.audio_file = value.strip('"')
        elif key == 'volume':
            audio.volume = float(value)
        elif key == 'loop':
            audio.loop = value.lower() == 'true'
        elif key == 'autoplay':
            audio.autoplay = value.lower() == 'true'
        elif key == 'min_distance':
            audio.min_distance = float(value)
        elif key == 'max_distance':
            audio.max_distance = float(value)
        elif key == 'properties':
            audio.properties = SceneParser._parse_dict(value)
    
    @staticmethod
    def _parse_trigger_property(trigger, key, value):
        if key == 'position':
            parts = value.split(',')
            trigger.x = float(parts[0].strip())
            trigger.y = float(parts[1].strip())
        elif key == 'size':
            parts = value.split(',')
            trigger.width = float(parts[0].strip())
            trigger.height = float(parts[1].strip())
        elif key == 'width':
            trigger.width = float(value)
        elif key == 'height':
            trigger.height = float(value)
        elif key == 'trigger_type':
            trigger.trigger_type = value.strip('"')
        elif key == 'action':
            trigger.action = value.strip('"')
        elif key == 'properties':
            trigger.properties = SceneParser._parse_dict(value)
    
    @staticmethod
    def _parse_spawn_property(spawn, key, value):
        if key == 'position':
            parts = value.split(',')
            spawn.x = float(parts[0].strip())
            spawn.y = float(parts[1].strip())
        elif key == 'entity_type':
            spawn.entity_type = value.strip('"')
        elif key == 'spawn_delay':
            spawn.spawn_delay = float(value)
        elif key == 'max_spawns':
            spawn.max_spawns = int(value)
        elif key == 'properties':
            spawn.properties = SceneParser._parse_dict(value)
    
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
    def _parse_camera_property(camera, key, value):
        if key == 'room_width':
            camera.room_width = int(value)
        elif key == 'room_height':
            camera.room_height = int(value)
        elif key == 'follow_mode':
            camera.follow_mode = value.strip('"')
        elif key == 'smooth_speed':
            camera.smooth_speed = float(value)
        elif key == 'properties':
            camera.properties = SceneParser._parse_dict(value)
    
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
                result[k] = SceneParser._parse_value(v)
        
        return result
    
    @staticmethod
    def _parse_value(value):
        try:
            if '.' in value:
                return float(value)
            else:
                return int(value)
        except ValueError:
            if value.lower() == 'true':
                return True
            elif value.lower() == 'false':
                return False
            else:
                return value
    
    @staticmethod
    def _parse_animations(value):
        return {}
    
    @staticmethod
    def _extract_id(section):
        start = section.find('id="')
        if start == -1:
            return ""
        start += 4
        end = section.find('"', start)
        if end == -1:
            return ""
        return section[start:end]
    
    @staticmethod
    def write_scene(filepath, scene_data):
        with open(filepath, 'w') as f:
            f.write('[scene]\n')
            f.write(f'name = "{scene_data.name}"\n')
            f.write(f'tilemap = "{scene_data.tilemap}"\n')
            f.write(f'tileset = "{scene_data.tileset}"\n')
            f.write(f'background_color = {scene_data.background_color}\n')
            
            if scene_data.properties:
                for key, value in scene_data.properties.items():
                    f.write(f'{key} = {SceneParser._format_value(value)}\n')
            f.write('\n')
            
            if scene_data.camera_settings.room_width != 320 or scene_data.camera_settings.room_height != 180:
                f.write('[camera]\n')
                f.write(f'room_width = {scene_data.camera_settings.room_width}\n')
                f.write(f'room_height = {scene_data.camera_settings.room_height}\n')
                f.write(f'follow_mode = "{scene_data.camera_settings.follow_mode}"\n')
                f.write(f'smooth_speed = {scene_data.camera_settings.smooth_speed}\n')
                f.write('\n')
            
            for entity in scene_data.entities:
                f.write(f'[entity id="{entity.id}"]\n')
                f.write(f'type = "{entity.type}"\n')
                f.write(f'position = {entity.x}, {entity.y}\n')
                
                if entity.properties:
                    props_str = ', '.join([f'{k}: {SceneParser._format_value(v)}' 
                                          for k, v in entity.properties.items()])
                    f.write(f'properties = {{{props_str}}}\n')
                
                if entity.sprite:
                    f.write(f'sprite = "{entity.sprite}"\n')
                
                f.write('\n')
            
            for light in scene_data.lights:
                f.write(f'[light id="{light.id}"]\n')
                f.write(f'position = {light.x}, {light.y}\n')
                f.write(f'radius = {light.radius}\n')
                f.write(f'color = {light.color}\n')
                f.write(f'intensity = {light.intensity}\n')
                f.write(f'type = "{light.type}"\n')
                f.write('\n')
            
            for audio in scene_data.audio_sources:
                f.write(f'[audio id="{audio.id}"]\n')
                f.write(f'position = {audio.x}, {audio.y}\n')
                f.write(f'audio_file = "{audio.audio_file}"\n')
                f.write(f'volume = {audio.volume}\n')
                f.write(f'loop = {str(audio.loop).lower()}\n')
                f.write(f'autoplay = {str(audio.autoplay).lower()}\n')
                f.write(f'min_distance = {audio.min_distance}\n')
                f.write(f'max_distance = {audio.max_distance}\n')
                f.write('\n')
            
            for trigger in scene_data.triggers:
                f.write(f'[trigger id="{trigger.id}"]\n')
                f.write(f'position = {trigger.x}, {trigger.y}\n')
                f.write(f'size = {trigger.width}, {trigger.height}\n')
                f.write(f'trigger_type = "{trigger.trigger_type}"\n')
                f.write(f'action = "{trigger.action}"\n')
                f.write('\n')
            
            for spawn in scene_data.spawn_points:
                f.write(f'[spawn_point id="{spawn.id}"]\n')
                f.write(f'position = {spawn.x}, {spawn.y}\n')
                f.write(f'entity_type = "{spawn.entity_type}"\n')
                f.write(f'spawn_delay = {spawn.spawn_delay}\n')
                f.write(f'max_spawns = {spawn.max_spawns}\n')
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
