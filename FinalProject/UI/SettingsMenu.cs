using System;
using FinalProject.Core;
using FinalProject.Core.Audio;

namespace FinalProject.UI
{
    // Music/SFX/FPS settings — the selection and adjustment logic only, no
    // drawing. This shows up in two very different places (the main menu's
    // full-page layout, the in-game C menu's small side panel), so instead
    // of duplicating the volume/FPS logic in both, each caller just reads
    // DisplayFor(i) and draws it however fits its own layout.
    public class SettingsMenu
    {
        public const int MusicVolumeIndex = 0;
        public const int SfxVolumeIndex   = 1;
        public const int FpsIndex         = 2;
        public const int BackIndex        = 3;

        public static readonly string[] Labels = { "Music Volume", "SFX Volume", "FPS Target", "Back" };

        private static readonly int[] FpsOptions = { 30, 60 };

        private readonly Game1 _game; // needed for SetTargetFps, which restarts the game's fixed-step timer

        public int SelectedIndex { get; private set; }

        public SettingsMenu(Game1 game) => _game = game;

        public void ResetSelection() => SelectedIndex = 0;

        public void MoveUp()
        {
            SelectedIndex = (SelectedIndex - 1 + Labels.Length) % Labels.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        public void MoveDown()
        {
            SelectedIndex = (SelectedIndex + 1) % Labels.Length;
            SoundManager.Play(SoundManager.MenuMove);
        }

        // Back has nothing to adjust — the caller checks SelectedIndex ==
        // BackIndex itself before deciding what "confirm" means there
        public void Adjust(int direction)
        {
            switch (SelectedIndex)
            {
                case MusicVolumeIndex:
                    GameSettings.SetMusicVolume(GameSettings.MusicVolume + direction * 10);
                    break;

                case SfxVolumeIndex:
                    GameSettings.SetSfxVolume(GameSettings.SfxVolume + direction * 10);
                    break;

                case FpsIndex:
                    int fpsIndex = Math.Max(0, Array.IndexOf(FpsOptions, GameSettings.TargetFps));
                    fpsIndex = (fpsIndex + direction + FpsOptions.Length) % FpsOptions.Length;
                    GameSettings.SetTargetFps(_game, FpsOptions[fpsIndex]);
                    break;
            }
        }

        // what a caller draws for row i — reads live off GameSettings so it
        // never needs to be told to refresh
        public string DisplayFor(int index) => index switch
        {
            MusicVolumeIndex => $"Music Volume : < {GameSettings.MusicVolume}% >",
            SfxVolumeIndex   => $"SFX Volume   : < {GameSettings.SfxVolume}% >",
            FpsIndex         => $"FPS Target   : < {GameSettings.TargetFps} FPS >",
            _                => Labels[index]
        };
    }
}
