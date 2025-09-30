using System;
using System.IO;
using System.Text.Json;

namespace Snow.Engine
{
    public class LevelData
    {
        public int TileSize { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int[][] WorldData { get; set; }
        public bool[][] CollisionData { get; set; }
    }

    public class LevelLoader
    {
        public static LevelData LoadLevel(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Level file not found: {filePath}");
            }

            string jsonContent = File.ReadAllText(filePath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var levelData = new LevelData
            {
                TileSize = root.GetProperty("tile_size").GetInt32(),
                GridWidth = root.GetProperty("grid_width").GetInt32(),
                GridHeight = root.GetProperty("grid_height").GetInt32()
            };

            var worldDataArray = root.GetProperty("world_data");
            levelData.WorldData = new int[levelData.GridHeight][];
            int row = 0;
            foreach (var rowElement in worldDataArray.EnumerateArray())
            {
                levelData.WorldData[row] = new int[levelData.GridWidth];
                int col = 0;
                foreach (var tile in rowElement.EnumerateArray())
                {
                    levelData.WorldData[row][col] = tile.GetInt32();
                    col++;
                }
                row++;
            }

            var collisionDataArray = root.GetProperty("collision_data");
            levelData.CollisionData = new bool[levelData.GridHeight][];
            row = 0;
            foreach (var rowElement in collisionDataArray.EnumerateArray())
            {
                levelData.CollisionData[row] = new bool[levelData.GridWidth];
                int col = 0;
                foreach (var tile in rowElement.EnumerateArray())
                {
                    levelData.CollisionData[row][col] = tile.GetBoolean();
                    col++;
                }
                row++;
            }

            return levelData;
        }
    }
}
