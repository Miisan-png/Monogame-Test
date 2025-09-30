using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class Tilemap
    {
        private int[][] _tiles;
        private bool[][] _collision;
        private List<Texture2D> _tileset;
        private int _tileSize;
        private int _width;
        private int _height;

        public int TileSize => _tileSize;
        public int Width => _width;
        public int Height => _height;

        public Tilemap(LevelData levelData, List<Texture2D> tileset)
        {
            _tiles = levelData.WorldData;
            _collision = levelData.CollisionData;
            _tileset = tileset;
            _tileSize = levelData.TileSize;
            _width = levelData.GridWidth;
            _height = levelData.GridHeight;
        }

        public bool IsSolid(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _width || tileY < 0 || tileY >= _height)
                return true;
            
            return _collision[tileY][tileX];
        }

        public bool IsSolidAtPosition(float worldX, float worldY)
        {
            int tileX = (int)(worldX / _tileSize);
            int tileY = (int)(worldY / _tileSize);
            return IsSolid(tileX, tileY);
        }

        public Rectangle GetTileBounds(int tileX, int tileY)
        {
            return new Rectangle(
                tileX * _tileSize,
                tileY * _tileSize,
                _tileSize,
                _tileSize
            );
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            int startX = (int)(camera.Position.X / _tileSize) - 1;
            int endX = (int)((camera.Position.X + camera.ViewWidth) / _tileSize) + 1;
            int startY = (int)(camera.Position.Y / _tileSize) - 1;
            int endY = (int)((camera.Position.Y + camera.ViewHeight) / _tileSize) + 1;

            startX = System.Math.Max(0, startX);
            endX = System.Math.Min(_width, endX);
            startY = System.Math.Max(0, startY);
            endY = System.Math.Min(_height, endY);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int tileId = _tiles[y][x];
                    if (tileId > 0 && tileId <= _tileset.Count)
                    {
                        Vector2 position = new Vector2(x * _tileSize, y * _tileSize);
                        spriteBatch.Draw(_tileset[tileId - 1], position, Color.White);
                    }
                }
            }
        }
    }
}
