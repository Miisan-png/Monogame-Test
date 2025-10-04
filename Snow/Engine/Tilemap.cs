using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace Snow.Engine
{
    public class Tilemap
    {
        private Texture2D _tileset;
        private TilemapData _data;
        private int _tilesPerRow;
        private int _tilesPerColumn;

        public int TileSize => _data.TileSize;
        public int GridWidth => _data.GridWidth;
        public int GridHeight => _data.GridHeight;
        public int Width => _data.GridWidth * _data.TileSize;
        public int Height => _data.GridHeight * _data.TileSize;

        public Tilemap(string mapPath, string tilesetPath, GraphicsDevice graphicsDevice)
        {
            _data = TilemapFormat.Load(mapPath);
            
            if (_data == null)
            {
                System.Console.WriteLine($"[Tilemap] Creating default tilemap");
                _data = TilemapFormat.CreateNew(16, 40, 23);
            }

            if (File.Exists(tilesetPath))
            {
                using (FileStream stream = new FileStream(tilesetPath, FileMode.Open))
                {
                    _tileset = Texture2D.FromStream(graphicsDevice, stream);
                }

                _tilesPerRow = _tileset.Width / _data.TileSize;
                _tilesPerColumn = _tileset.Height / _data.TileSize;
                
                System.Console.WriteLine($"[Tilemap] Tileset loaded: {tilesetPath}");
                System.Console.WriteLine($"[Tilemap] Tiles: {_tilesPerRow}x{_tilesPerColumn}");
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            if (_tileset == null || _data == null) return;

            int startX = Math.Max(0, (int)(camera.Position.X / _data.TileSize));
            int startY = Math.Max(0, (int)(camera.Position.Y / _data.TileSize));
            int endX = Math.Min(_data.GridWidth, (int)((camera.Position.X + camera.ViewportWidth) / _data.TileSize) + 1);
            int endY = Math.Min(_data.GridHeight, (int)((camera.Position.Y + camera.ViewportHeight) / _data.TileSize) + 1);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int tileId = _data.Tiles[y][x];
                    if (tileId <= 0) continue;

                    int tileIndex = tileId - 1;
                    int tx = tileIndex % _tilesPerRow;
                    int ty = tileIndex / _tilesPerRow;

                    Rectangle sourceRect = new Rectangle(
                        tx * _data.TileSize,
                        ty * _data.TileSize,
                        _data.TileSize,
                        _data.TileSize
                    );

                    Vector2 position = new Vector2(x * _data.TileSize, y * _data.TileSize);

                    Color tintColor = Color.White;
                    if (_data.Spikes[y][x])
                    {
                        tintColor = new Color(255, 150, 150);
                    }

                    spriteBatch.Draw(_tileset, position, sourceRect, tintColor);
                }
            }
        }

        public bool IsSolid(int x, int y)
        {
            if (x < 0 || x >= _data.GridWidth || y < 0 || y >= _data.GridHeight)
                return false;
            return _data.Collision[y][x];
        }

        public bool IsSpike(int x, int y)
        {
            if (x < 0 || x >= _data.GridWidth || y < 0 || y >= _data.GridHeight)
                return false;
            return _data.Spikes[y][x];
        }

        public bool CheckSpikeCollision(Rectangle bounds)
        {
            int startX = Math.Max(0, bounds.Left / _data.TileSize);
            int startY = Math.Max(0, bounds.Top / _data.TileSize);
            int endX = Math.Min(_data.GridWidth - 1, bounds.Right / _data.TileSize);
            int endY = Math.Min(_data.GridHeight - 1, bounds.Bottom / _data.TileSize);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (_data.Spikes[y][x])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsColliding(Rectangle bounds)
        {
            int startX = Math.Max(0, bounds.Left / _data.TileSize);
            int startY = Math.Max(0, bounds.Top / _data.TileSize);
            int endX = Math.Min(_data.GridWidth - 1, bounds.Right / _data.TileSize);
            int endY = Math.Min(_data.GridHeight - 1, bounds.Bottom / _data.TileSize);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (_data.Collision[y][x])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Rectangle GetTileBounds(int x, int y)
        {
            return new Rectangle(x * _data.TileSize, y * _data.TileSize, _data.TileSize, _data.TileSize);
        }

        public int GetTileAt(int x, int y)
        {
            if (x < 0 || x >= _data.GridWidth || y < 0 || y >= _data.GridHeight)
                return 0;
            return _data.Tiles[y][x];
        }

        public void SetTileAt(int x, int y, int tileId)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                _data.Tiles[y][x] = tileId;
            }
        }

        public void SetCollisionAt(int x, int y, bool solid)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                _data.Collision[y][x] = solid;
            }
        }

        public void SetSpikeAt(int x, int y, bool spike)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                _data.Spikes[y][x] = spike;
            }
        }
    }
}
