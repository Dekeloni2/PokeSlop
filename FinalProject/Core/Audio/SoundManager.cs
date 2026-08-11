using System;
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
            catch (Exception e) { LogFailure("sound", name, fileName, e); }
        }

        // same but for background tracks, .ogg/.mp3 built as Song
        public static void AddSong(string name, string fileName)
        {
            if (_content == null) return;
            try   { _songs[name] = _content.Load<Song>(fileName); }
            catch (Exception e) { LogFailure("song", name, fileName, e); }
        }

        // A cue that won't load is still not worth taking the game down for —
        // silence beats a crash. But it used to be swallowed outright, which
        // made "this track doesn't play" indistinguishable from "this track
        // was never meant to play": Play() and PlayMusic() both no-op on an
        // unregistered name, so a failed load left no trace anywhere. Say it
        // out loud instead. See GameLog for where that ends up per platform.
        private static void LogFailure(string kind, string name, string fileName, Exception e)
            => GameLog.Write($"AUDIO LOAD FAILED ({kind} \"{name}\" from \"{fileName}\"): {e.GetType().Name}: {e.Message}");

        // ── playback ──────────────────────────────────────────────────────────

        // one-shots are kept around while they play so StopAll can cut them off.
        // SoundEffect.Play() hands back no reference at all, so anything started
        // that way would keep ringing out over the death sequence with no way to
        // silence it. going through instances is what makes them stoppable
        private static readonly List<SoundEffectInstance> _oneShots = new();

        // fire and forget from the caller's side. pitch/pan are -1..1, 0 is normal
        public static void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
        {
            if (!_sounds.TryGetValue(name, out SoundEffect sfx)) return;

            PruneOneShots();

            SoundEffectInstance instance = sfx.CreateInstance();
            instance.Volume = MathHelper.Clamp(volume * SfxVolume, 0f, 1f);
            instance.Pitch  = MathHelper.Clamp(pitch, -1f, 1f);
            instance.Pan    = MathHelper.Clamp(pan, -1f, 1f);
            instance.Play();

            _oneShots.Add(instance);
        }

        // how long a registered one-shot actually runs — for callers that need
        // to sequence something after it finishes (the toilet's flush, then
        // the payoff line) without a callback system. Zero for anything not
        // registered, so a missing sound just means no wait instead of a crash
        public static TimeSpan GetDuration(string name)
            => _sounds.TryGetValue(name, out SoundEffect sfx) ? sfx.Duration : TimeSpan.Zero;

        // drops the ones that have finished. runs on every Play so the list
        // can't grow all fight — the text blip alone fires ~20 times a second,
        // and platforms cap how many instances can exist at once
        private static void PruneOneShots()
        {
            for (int i = _oneShots.Count - 1; i >= 0; i--)
            {
                if (_oneShots[i].State != SoundState.Stopped) continue;

                _oneShots[i].Dispose();
                _oneShots.RemoveAt(i);
            }
        }

        // everything off at once — one-shots, loops and music. for the player
        // dying mid attack, where whatever was mid-swing would otherwise carry
        // straight over the top of the death sequence
        public static void StopAll()
        {
            foreach (SoundEffectInstance instance in _oneShots)
            {
                instance.Stop();
                instance.Dispose();
            }
            _oneShots.Clear();

            StopAllLoops();
            StopMusic();
        }

        // default blip for text typing out, undertale style
        public const string TextBeepName = "beep";

        // shared menu cues, used by the battle menus and anything else later
        public const string MenuMove   = "menuMove";   // moving the cursor
        public const string MenuSelect = "menuSelect"; // confirming an option

        // anything that puts HP back, wherever it comes from
        public const string HealSound  = "snd_heal_c";

        // the ending's phone call
        public const string PhoneRing  = "snd_phone";

        // only blips if text[from..to) has an actual character in it, so
        // spaces and newlines stay quiet
        public static void PlayTextBeep(string text, int from, int to)
        {
            for (int i = from; i < to; i++)
                if (!char.IsWhiteSpace(text[i])) { Play(TextBeepName); return; }
        }

        // same, but with a custom blip (the GAME OVER text uses snd_txtasg)
        public static void PlayTextBeep(string beepName, string text, int from, int to)
        {
            for (int i = from; i < to; i++)
                if (!char.IsWhiteSpace(text[i])) { Play(beepName); return; }
        }

        // the currently-playing track's own volume (e.g. the hallway plays
        // quieter than everything else) — remembered so RefreshMusicVolume
        // can re-apply MusicVolume without forgetting it
        private static float _currentTrackVolume = 1f;

        // starts a track, loops by default and replaces whatever is playing.
        // volume is per track on top of MusicVolume, since tracks come from
        // different places and aren't mastered to the same level
        public static void PlayMusic(string name, bool loop = true, float volume = 1f)
        {
            if (!_songs.TryGetValue(name, out Song song)) return;

            _currentTrackVolume = volume;
            MediaPlayer.Volume      = MathHelper.Clamp(MusicVolume * volume, 0f, 1f);
            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Play(song);
        }

        // adjust the current track's volume live (used to fade music out). the
        // argument is the per-track volume, same scale as PlayMusic's parameter
        public static void SetMusicVolume(float volume)
        {
            _currentTrackVolume = volume;
            MediaPlayer.Volume = MathHelper.Clamp(MusicVolume * volume, 0f, 1f);
        }

        // re-applies MusicVolume against whatever's already playing, without
        // touching that track's own volume — for when the player moves the
        // music slider but nothing about the current track changes. Calling
        // SetMusicVolume(1f) for this used to silently reset a quieter track
        // (like the hallway's) back to full volume.
        public static void RefreshMusicVolume()
            => MediaPlayer.Volume = MathHelper.Clamp(MusicVolume * _currentTrackVolume, 0f, 1f);

        public static void StopMusic() => MediaPlayer.Stop();

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
