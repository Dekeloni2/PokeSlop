using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FinalProject.Data
{
    // one piece of a teacher. undertale style enemies are built out of separate
    // parts that each drift on their own, that's what makes them look alive
    // instead of like a static picture.
    // Src is the rect on the sheet, Offset is where the piece sits relative to
    // the teacher's anchor (just where it was in the assembled drawing), and the
    // bob values are how far/fast it drifts
    public class TeacherPartData
    {
        public string    Name   { get; }
        public Rectangle Src    { get; }
        public Vector2   Offset { get; }

        // swapped in while the teacher is hurt, only the head has one.
        // null means this part looks the same either way
        public Rectangle? SrcHurt { get; }

        public float BobX  { get; } // px it drifts sideways
        public float BobY  { get; } // px it drifts up and down
        public float Speed { get; } // cycles per second
        public float Phase { get; } // 0..1, how offset it is from the other parts

        public TeacherPartData(string name, Rectangle src, Vector2 offset,
            float bobX, float bobY, float speed, float phase,
            Rectangle? srcHurt = null)
        {
            Name    = name;
            Src     = src;
            Offset  = offset;
            SrcHurt = srcHurt;
            BobX    = bobX;
            BobY    = bobY;
            Speed   = speed;
            Phase   = phase;
        }
    }

    // the whole teacher sprite. parts draw in list order so the first one is
    // furthest back (far arm, body, head, near arm)
    public class TeacherSpriteData
    {
        public string  Sheet  { get; } // SpriteManager key
        public Vector2 Anchor { get; } // nudge from the default screen spot

        // scales the whole thing. offsets and bob get scaled too, otherwise the
        // parts would drift apart as the art gets bigger
        public float Scale { get; }

        public IReadOnlyList<TeacherPartData> Parts { get; }

        public TeacherSpriteData(string sheet, Vector2 anchor, float scale,
            IReadOnlyList<TeacherPartData> parts)
        {
            Sheet  = sheet;
            Anchor = anchor;
            Scale  = scale <= 0f ? 1f : scale;
            Parts  = parts ?? new List<TeacherPartData>();
        }
    }
}
