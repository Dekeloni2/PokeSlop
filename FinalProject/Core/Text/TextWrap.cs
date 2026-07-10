using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Core.Text
{
    // word-wraps text to fit a pixel width. Used by DialogueBox and ActionMenu.
    public static class TextWrap
    {
        public static List<string> ToLines(SpriteFont font, string text, float maxWidth, float scale = 1f)
        {
            var lines = new List<string>();

            foreach (string paragraph in text.Split('\n'))
            {
                string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string currentLine = "";

                foreach (string word in words)
                {
                    string candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
                    if (font.MeasureString(candidate).X * scale > maxWidth && currentLine.Length > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = candidate;
                    }
                }
                lines.Add(currentLine);
            }

            return lines;
        }
    }
}
