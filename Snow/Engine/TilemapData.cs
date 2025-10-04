using System;
using System.IO;
using System.Text;

namespace Snow.Engine
{
    public class TilemapData
    {
        public int TileSize { get; set; }
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }
        public int[][] Tiles { get; set; }
        public bool[][] Collision { get; set; }

        public TilemapData(int tileSize, int gridWidth, int gridHeight)
        {
            TileSize = tileSize;
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            
            Tiles = new int[gridHeight][];
            Collision = new bool[gridHeight][];
            
            for (int y = 0; y < gridHeight; y++)
            {
                Tiles[y] = new int[gridWidth];
                Collision[y] = new bool[gridWidth];
            }
        }
    }

    public static class TilemapFormat
    {
        private const uint MAGIC = 0x544D4150;
        private const ushort VERSION = 1;

        public static void Save(string path, TilemapData data)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(MAGIC);
                writer.Write(VERSION);
                writer.Write(data.TileSize);
                writer.Write(data.GridWidth);
                writer.Write(data.GridHeight);

                for (int y = 0; y < data.GridHeight; y++)
                {
                    for (int x = 0; x < data.GridWidth; x++)
                    {
                        writer.Write(data.Tiles[y][x]);
                    }
                }

                for (int y = 0; y < data.GridHeight; y++)
                {
                    for (int x = 0; x < data.GridWidth; x++)
                    {
                        writer.Write(data.Collision[y][x]);
                    }
                }
            }

            System.Console.WriteLine($"[TilemapFormat] Saved: {path}");
        }

        public static TilemapData Load(string path)
        {
            if (!File.Exists(path))
            {
                System.Console.WriteLine($"[TilemapFormat] File not found: {path}");
                return null;
            }

            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                uint magic = reader.ReadUInt32();
                if (magic != MAGIC)
                {
                    System.Console.WriteLine($"[TilemapFormat] Invalid magic number in {path}");
                    return null;
                }

                ushort version = reader.ReadUInt16();
                if (version != VERSION)
                {
                    System.Console.WriteLine($"[TilemapFormat] Unsupported version {version} in {path}");
                    return null;
                }

                int tileSize = reader.ReadInt32();
                int gridWidth = reader.ReadInt32();
                int gridHeight = reader.ReadInt32();

                var data = new TilemapData(tileSize, gridWidth, gridHeight);

                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        data.Tiles[y][x] = reader.ReadInt32();
                    }
                }

                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        data.Collision[y][x] = reader.ReadBoolean();
                    }
                }

                System.Console.WriteLine($"[TilemapFormat] Loaded: {path} ({gridWidth}x{gridHeight})");
                return data;
            }
        }

        public static TilemapData CreateNew(int tileSize, int gridWidth, int gridHeight)
        {
            return new TilemapData(tileSize, gridWidth, gridHeight);
        }
    }
}