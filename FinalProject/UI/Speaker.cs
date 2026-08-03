using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Graphics;
using FinalProject.Data;

namespace FinalProject.UI
{
    // Who is talking: the face shown in the dialogue box and the blip their text
    // types with. Both come straight off the teacher's existing battle data — a
    // speaker needs no new art and no new sound.
    public class Speaker
    {
        // The sprite part used as the face.
        private const string HeadPart = "head";

        // null when the teacher has no head part (the training dummy) — the box
        // then runs full width with no face, but still in the right voice.
        public Texture2D FaceTexture { get; }
        public Rectangle FaceSource  { get; }

        public bool HasFace => FaceTexture != null;

        // SoundManager key for the text blip. null means the default beep, the
        // same fallback battle uses — so a teacher who sounds default in a fight
        // sounds default here too.
        public string Voice { get; }

        public Speaker(Texture2D faceTexture, Rectangle faceSource, string voice)
        {
            FaceTexture = faceTexture;
            FaceSource  = faceSource;
            Voice       = voice;
        }

        public static Speaker ForTeacher(TeacherStats stats)
        {
            if (stats == null) return null;

            TeacherSpriteData sprite = stats.Sprite;
            Spritesheet sheet = sprite == null ? null : SpriteManager.GetSprite(sprite.Sheet);

            if (sheet?.Texture != null)
            {
                foreach (TeacherPartData part in sprite.Parts)
                    if (string.Equals(part.Name, HeadPart, StringComparison.OrdinalIgnoreCase))
                        return new Speaker(sheet.Texture, part.Src, stats.SpeakingVoice);
            }

            // no head to show, but he still gets his own voice
            return new Speaker(null, Rectangle.Empty, stats.SpeakingVoice);
        }
    }
}
