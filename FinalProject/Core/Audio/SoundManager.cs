using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace FinalProject.Core.Audio
{
    // audio registry, same idea as SpriteManager. register by name in
    // Game1.LoadContent, then play by name from anywhere.
    // short cues are SoundEffects, background tracks are Songs (MediaPlayer).
    // files that aren't imported yet get skipped instead of throwing, so a
    // missing sound just stays silent
    public class SoundManager
    {
        private static readonly Dictionary<string, SoundEffect> _sounds = new();
        private static readonly Dictionary<string, Song>        _songs  = new();

        private static ContentManager _content;

        // 0..1, applied when something plays (options menu later?)
        public static float SfxVolume   { get; set; } = 1f;
        public static float MusicVolume { get; set; } = 1f;

        public SoundManager(ContentManager content) => _content = content;

        // ── registration (called from Game1.LoadContent) ──────────────────────

        // fileName is the content key, a .wav built with the Sound Effect processor
        public static void AddSound(string name, string fileName)
        {
            if (_content == null) return;
            try   { _sounds[name] = _content.Load<SoundEffect>(fileName); }
            catch { /* not built yet, leave it out so Play does nothing */ }
        }

        // same but for background tracks, .ogg/.mp3 built as Song
        public static void AddSong(string name, string fileName)
        {
            if (_content == null) return;
            try   { _songs[name] = _content.Load<Song>(fileName); }
            catch { /* not built yet */ }
        }

        // ── playback ──────────────────────────────────────────────────────────

        // one-shot, can't be stopped. pitch/pan are -1..1, 0 is normal
        public static void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
        {
            if (_sounds.TryGetValue(name, out SoundEffect sfx))
                sfx.Play(MathHelper.Clamp(volume * SfxVolume, 0f, 1f), pitch, pan);
        }

        // default blip for text typing out, undertale style
        public const string TextBeepName = "beep";

        public static void PlayTextBeep() => Play(TextBeepName);

        // only blips if text[from..to) has an actual character in it, so
        // spaces and newlines stay quiet
        public static void PlayTextBeep(string text, int from, int to)
        {
            for (int i = from; i < to; i++)
                if (!char.IsWhiteSpace(text[i])) { Play(TextBeepName); return; }
        }

        // starts a track, loops by default and replaces whatever is playing
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

        // ── loops ─────────────────────────────────────────────────────────────
        // Play() can't be stopped, so anything that runs while something is
        // happening (the napoleon rumble) needs a live instance. kept by name
        // so callers just do StartLoop("rumble") / StopLoop("rumble")

        private static readonly Dictionary<string, SoundEffectInstance> _loops = new();

        public static void StartLoop(string name, float volume = 1f)
        {
            if (_loops.ContainsKey(name)) return; // already going
            if (!_sounds.TryGetValue(name, out SoundEffect sfx)) return;

            SoundEffectInstance instance = sfx.CreateInstance();
            instance.IsLooped = true;
            instance.Volume   = MathHelper.Clamp(volume * SfxVolume, 0f, 1f);
            instance.Play();

            _loops[name] = instance;
        }

        public static void StopLoop(string name)
        {
            if (!_loops.TryGetValue(name, out SoundEffectInstance instance)) return;

            instance.Stop();
            instance.Dispose();
            _loops.Remove(name);
        }

        // in case a fight ends while a loop is still going (player dies mid
        // attack), otherwise it just keeps playing
        public static void StopAllLoops()
        {
            foreach (SoundEffectInstance instance in _loops.Values)
            {
                instance.Stop();
                instance.Dispose();
            }
            _loops.Clear();
        }
    }
}
