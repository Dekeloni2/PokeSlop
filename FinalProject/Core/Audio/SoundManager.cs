using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace FinalProject.Core.Audio
{
    // Central registry + playback for audio, mirroring SpriteManager: assets are
    // registered once by name in Game1.LoadContent, then played by name from
    // anywhere via SoundManager.Play(...) / PlayMusic(...).
    //
    // Short one-shot cues (hits, menu blips) are SoundEffects; looping background
    // tracks are Songs, driven by MonoGame's global MediaPlayer.
    //
    // Missing assets are skipped rather than thrown, so a not-yet-imported sound
    // can't crash the game — Play simply no-ops until the file exists in the
    // content pipeline.
    public class SoundManager
    {
        private static readonly Dictionary<string, SoundEffect> _sounds = new();
        private static readonly Dictionary<string, Song>        _songs  = new();

        private static ContentManager _content;

        // 0..1 multipliers applied at play time, so the whole mix can be
        // balanced or muted from one place (e.g. an options menu later).
        public static float SfxVolume   { get; set; } = 1f;
        public static float MusicVolume { get; set; } = 1f;

        public SoundManager(ContentManager content) => _content = content;

        // ── Registration (call these in Game1.LoadContent) ────────────────────

        // Register a short sound effect. fileName is its content key
        // (a .wav imported with the "Sound Effect" processor).
        public static void AddSound(string name, string fileName)
        {
            if (_content == null) return;
            try   { _sounds[name] = _content.Load<SoundEffect>(fileName); }
            catch { /* not imported yet — leave unregistered so Play() no-ops */ }
        }

        // Register a background/looping track. fileName is its content key
        // (an .ogg/.mp3 imported with the "Song" processor).
        public static void AddSong(string name, string fileName)
        {
            if (_content == null) return;
            try   { _songs[name] = _content.Load<Song>(fileName); }
            catch { /* not imported yet — leave unregistered so PlayMusic() no-ops */ }
        }

        // ── Playback ──────────────────────────────────────────────────────────

        // Fire-and-forget one-shot. volume is scaled by SfxVolume; pitch and pan
        // are -1..1 (0 = default) for quick variation.
        public static void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
        {
            if (_sounds.TryGetValue(name, out SoundEffect sfx))
                sfx.Play(MathHelper.Clamp(volume * SfxVolume, 0f, 1f), pitch, pan);
        }

        // Start a background track. Loops by default and replaces whatever's
        // currently playing.
        public static void PlayMusic(string name, bool loop = true)
        {
            if (!_songs.TryGetValue(name, out Song song)) return;

            MediaPlayer.Volume      = MathHelper.Clamp(MusicVolume, 0f, 1f);
            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Play(song);
        }

        public static void StopMusic()   => MediaPlayer.Stop();
        public static void PauseMusic()  => MediaPlayer.Pause();
        public static void ResumeMusic() => MediaPlayer.Resume();
    }
}
