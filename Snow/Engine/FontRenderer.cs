using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Snow.Engine
{
    public class Glyph
    {
        public int Id;
        public Rectangle Source;
        public int XOffset;
        public int YOffset;
        public int XAdvance;
    }

    public class BitmapFont
    {
        private Texture2D _texture;
        private Dictionary<int, Glyph> _glyphs;
        private int _lineHeight;

        public BitmapFont(GraphicsDevice device, string fontPath)
        {
            _glyphs = new Dictionary<int, Glyph>();

            string dir = Path.GetDirectoryName(fontPath);
            string[] lines = File.ReadAllLines(fontPath);

            foreach (string line in lines)
            {
                if (line.StartsWith("common"))
                {
                    var parts = line.Split(' ');
                    foreach (var part in parts)
                        if (part.StartsWith("lineHeight="))
                            _lineHeight = int.Parse(part.Substring(11));
                }

                if (line.StartsWith("page"))
                {
                    int start = line.IndexOf("file=\"") + 6;
                    int end = line.IndexOf("\"", start);
                    string texFile = line.Substring(start, end - start);
                    using var fs = new FileStream(Path.Combine(dir, texFile), FileMode.Open);
                    _texture = Texture2D.FromStream(device, fs);
                }
                if (line.StartsWith("char id="))
                    {
                        var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                        int id = 0, x = 0, y = 0, w = 0, h = 0, xoff = 0, yoff = 0, xadv = 0;

                        foreach (var part in parts)
                        {
                            var kv = part.Split('=');
                            if (kv.Length < 2) continue;
                            string key = kv[0];
                            string val = kv[1];

                            switch (key)
                            {
                                case "id": id = int.Parse(val); break;
                                case "x": x = int.Parse(val); break;
                                case "y": y = int.Parse(val); break;
                                case "width": w = int.Parse(val); break;
                                case "height": h = int.Parse(val); break;
                                case "xoffset": xoff = int.Parse(val); break;
                                case "yoffset": yoff = int.Parse(val); break;
                                case "xadvance": xadv = int.Parse(val); break;
                            }
                        }

                        _glyphs[id] = new Glyph
                        {
                            Id = id,
                            Source = new Rectangle(x, y, w, h),
                            XOffset = xoff,
                            YOffset = yoff,
                            XAdvance = xadv
                        };

                }
            }
        }

        public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, float scale = 1f)
        {
            float startX = pos.X;
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    pos.X = startX;
                    pos.Y += _lineHeight * scale;
                    continue;
                }

                if (_glyphs.TryGetValue(c, out Glyph g))
                {
                    sb.Draw(
                        _texture,
                        new Vector2(pos.X + g.XOffset * scale, pos.Y + g.YOffset * scale),
                        g.Source,
                        color,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        0f
                    );
                    pos.X += g.XAdvance * scale;
                }
            }
        }
    }
}
