using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public class BitmapFont
    {
        private Texture2D _texture;
        private Dictionary<int, Glyph> _glyphs;
        private Dictionary<(int, int), int> _kerningPairs;
        private int _lineHeight;
        private int _base;

        public int LineHeight => _lineHeight;

        public BitmapFont(GraphicsDevice device, string fontPath)
        {
            _glyphs = new Dictionary<int, Glyph>();
            _kerningPairs = new Dictionary<(int, int), int>();

            string dir = Path.GetDirectoryName(fontPath);
            string[] lines = File.ReadAllLines(fontPath);

            foreach (string line in lines)
            {
                if (line.StartsWith("common"))
                {
                    var parts = line.Split(' ');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("lineHeight="))
                            _lineHeight = int.Parse(part.Substring(11));
                        else if (part.StartsWith("base="))
                            _base = int.Parse(part.Substring(5));
                    }
                }
                else if (line.StartsWith("page"))
                {
                    int start = line.IndexOf("file=\"") + 6;
                    int end = line.IndexOf("\"", start);
                    string texFile = line.Substring(start, end - start);
                    using var fs = new FileStream(Path.Combine(dir, texFile), FileMode.Open);
                    _texture = Texture2D.FromStream(device, fs);
                }
                else if (line.StartsWith("char id="))
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
                else if (line.StartsWith("kerning"))
                {
                    var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                    int first = 0, second = 0, amount = 0;

                    foreach (var part in parts)
                    {
                        var kv = part.Split('=');
                        if (kv.Length < 2) continue;
                        string key = kv[0];
                        string val = kv[1];

                        switch (key)
                        {
                            case "first": first = int.Parse(val); break;
                            case "second": second = int.Parse(val); break;
                            case "amount": amount = int.Parse(val); break;
                        }
                    }

                    _kerningPairs[(first, second)] = amount;
                }
            }
        }

        public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, float scale = 1f, TextAlignment alignment = TextAlignment.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (alignment != TextAlignment.Left)
            {
                Vector2 size = MeasureString(text, scale);
                if (alignment == TextAlignment.Center)
                    pos.X -= size.X * 0.5f;
                else if (alignment == TextAlignment.Right)
                    pos.X -= size.X;
            }

            float startX = pos.X;
            int prevChar = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    pos.X = startX;
                    pos.Y += _lineHeight * scale;
                    prevChar = 0;
                    continue;
                }

                if (_glyphs.TryGetValue(c, out Glyph g))
                {
                    if (prevChar != 0 && _kerningPairs.TryGetValue((prevChar, c), out int kerning))
                    {
                        pos.X += kerning * scale;
                    }

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
                    prevChar = c;
                }
            }
        }

        public void DrawStringMultiline(SpriteBatch sb, string text, Vector2 pos, Color color, float maxWidth, float lineSpacing = 1.0f, float scale = 1f, TextAlignment alignment = TextAlignment.Left)
        {
            if (string.IsNullOrEmpty(text)) return;

            string wrapped = WrapText(text, maxWidth, scale);
            string[] lines = wrapped.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 linePos = new Vector2(pos.X, pos.Y + i * _lineHeight * scale * lineSpacing);
                DrawString(sb, lines[i], linePos, color, scale, alignment);
            }
        }

        public Vector2 MeasureString(string text, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.Zero;

            float maxWidth = 0;
            float currentWidth = 0;
            int lineCount = 1;
            int prevChar = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    maxWidth = Math.Max(maxWidth, currentWidth);
                    currentWidth = 0;
                    lineCount++;
                    prevChar = 0;
                    continue;
                }

                if (_glyphs.TryGetValue(c, out Glyph g))
                {
                    if (prevChar != 0 && _kerningPairs.TryGetValue((prevChar, c), out int kerning))
                    {
                        currentWidth += kerning * scale;
                    }

                    currentWidth += g.XAdvance * scale;
                    prevChar = c;
                }
            }

            maxWidth = Math.Max(maxWidth, currentWidth);
            return new Vector2(maxWidth, lineCount * _lineHeight * scale);
        }

        public string WrapText(string text, float maxWidth, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0) return text;

            StringBuilder result = new StringBuilder();
            StringBuilder currentLine = new StringBuilder();
            StringBuilder currentWord = new StringBuilder();
            float currentLineWidth = 0;
            float currentWordWidth = 0;
            int prevChar = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\n')
                {
                    currentLine.Append(currentWord);
                    result.Append(currentLine);
                    result.Append('\n');
                    currentLine.Clear();
                    currentWord.Clear();
                    currentLineWidth = 0;
                    currentWordWidth = 0;
                    prevChar = 0;
                    continue;
                }

                if (c == ' ')
                {
                    if (currentLineWidth + currentWordWidth <= maxWidth)
                    {
                        currentLine.Append(currentWord);
                        currentLine.Append(' ');
                        currentLineWidth += currentWordWidth;
                        
                        if (_glyphs.TryGetValue(' ', out Glyph spaceGlyph))
                            currentLineWidth += spaceGlyph.XAdvance * scale;
                        
                        currentWord.Clear();
                        currentWordWidth = 0;
                    }
                    else
                    {
                        if (currentLine.Length > 0)
                        {
                            result.Append(currentLine);
                            result.Append('\n');
                        }
                        currentLine.Clear();
                        currentLine.Append(currentWord);
                        currentLine.Append(' ');
                        currentLineWidth = currentWordWidth;
                        
                        if (_glyphs.TryGetValue(' ', out Glyph spaceGlyph))
                            currentLineWidth += spaceGlyph.XAdvance * scale;
                        
                        currentWord.Clear();
                        currentWordWidth = 0;
                    }
                    prevChar = ' ';
                    continue;
                }

                if (_glyphs.TryGetValue(c, out Glyph g))
                {
                    float charWidth = g.XAdvance * scale;
                    
                    if (prevChar != 0 && _kerningPairs.TryGetValue((prevChar, c), out int kerning))
                    {
                        charWidth += kerning * scale;
                    }

                    currentWord.Append(c);
                    currentWordWidth += charWidth;
                    prevChar = c;
                }
            }

            if (currentWordWidth > 0)
            {
                if (currentLineWidth + currentWordWidth <= maxWidth)
                {
                    currentLine.Append(currentWord);
                }
                else
                {
                    if (currentLine.Length > 0)
                        result.Append(currentLine).Append('\n');
                    result.Append(currentWord);
                }
            }
            else if (currentLine.Length > 0)
            {
                result.Append(currentLine);
            }

            return result.ToString();
        }

        public Rectangle GetBounds(string text, Vector2 position, float scale = 1f)
        {
            Vector2 size = MeasureString(text, scale);
            return new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }
    }
}
