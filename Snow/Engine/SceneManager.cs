using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Snow.Engine
{
    public class SceneManager
    {
        private GraphicsDevice _graphicsDevice;
        private GraphicsManager _graphicsManager;
        private Scene _currentScene;
        private EntityFactoryContext _factoryContext;

        public Scene CurrentScene => _currentScene;

        public SceneManager(GraphicsDevice graphicsDevice, GraphicsManager graphicsManager)
        {
            _graphicsDevice = graphicsDevice;
            _graphicsManager = graphicsManager;
        }

        public void SetFactoryContext(EntityFactoryContext context)
        {
            _factoryContext = context;
        }

        public Scene LoadScene(string scenePath)
        {
            try
            {
                var sceneData = SceneParser.ParseScene(scenePath);
                
                Tilemap tilemap = null;
                if (!string.IsNullOrEmpty(sceneData.Tilemap) && !string.IsNullOrEmpty(sceneData.Tileset))
                {
                    tilemap = new Tilemap(sceneData.Tilemap, sceneData.Tileset, _graphicsDevice);
                }

                Vector2 playerSpawn = Vector2.Zero;
                List<IEntity> entities = new List<IEntity>();

                foreach (var entityData in sceneData.Entities)
                {
                    if (entityData.Type == "PlayerSpawn")
                    {
                        playerSpawn = new Vector2(entityData.X, entityData.Y);
                    }
                    else
                    {
                        var entity = EntityFactory.CreateEntity(entityData, _graphicsDevice, _graphicsManager, _factoryContext);
                        if (entity != null)
                        {
                            entities.Add(entity);
                        }
                    }
                }

                _currentScene = new Scene
                {
                    Name = sceneData.Name,
                    Tilemap = tilemap,
                    PlayerSpawnPosition = playerSpawn,
                    Entities = entities
                };

                return _currentScene;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SceneManager] Error loading scene: {ex.Message}");
                throw;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_currentScene?.Entities == null) return;

            foreach (var entity in _currentScene.Entities)
            {
                entity.Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, GameTime gameTime)
        {
            if (_currentScene?.Tilemap == null) return;

            _currentScene.Tilemap.Draw(spriteBatch, camera);
        }

        public void DrawEntities(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_currentScene?.Entities == null) return;

            foreach (var entity in _currentScene.Entities)
            {
                entity.Draw(spriteBatch, gameTime);
            }
        }
    }
}