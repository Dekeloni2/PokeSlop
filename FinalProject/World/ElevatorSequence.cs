using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Audio;
using FinalProject.Core.Input;
using FinalProject.UI;

namespace FinalProject.World
{
    // the lift on the elevator map: pick a floor, the doors shut, it rides and
    // judders, a bell rings and the doors open onto somewhere else. it takes
    // over input for as long as it's running, so the player can't walk out of
    // a moving lift.
    //
    // it needs a few things it doesn't own — the player's facing, the door
    // layer, and the map change itself — so the state hosting it hands those
    // over through IElevatorHost rather than exposing itself wholesale.
    public class ElevatorSequence
    {
        // what the sequence needs from whoever is running it. keeping it to
        // these five means the lift can't reach into the rest of the overworld
        public interface IElevatorHost
        {
            // a map change is already underway, don't start another
            bool IsChangingMap { get; }

            // the player is standing on the doorway out of the lift
            bool PlayerOnDoorTile { get; }

            void FacePlayerOut();
            void SetDoorsShut(bool shut);
            void ChangeMap(string map, int spawnX, int spawnY);
        }

        // the floors it can reach. a null map is one that isn't built yet, it
        // still rides but there's nowhere to walk out to
        private static readonly (int Floor, string Label, string Map, int X, int Y)[] Floors =
        {
            (0, "Floor 0", "entrance",    6, 14),
            (2, "Floor 2", "floor_2",    10, 18),
            (3, "Floor 3", "tiltan_hall", 53, 7),
        };

        private const float DoorSeconds  = 0.7f; // shutting and opening again
        private const float RideSeconds  = 2.5f;
        private const float RideShake    = 2.5f; // px of judder while moving
        private const float RideSettleSeconds = 0.8f; // it eases off over the last stretch

        private const string CancelLabel = "Cancel";

        private enum Phase { None, Closing, Riding, Opening }

        private Phase _phase = Phase.None;
        private float _timer;
        private int   _currentFloor = 0;
        private int   _pendingFloor = -1; // index into Floors, set once it arrives

        private readonly ChoiceBox _floorMenu;
        private readonly List<int> _floorChoices = new(); // indices shown in the menu
        private readonly Random _rng = new();

        public ElevatorSequence(Texture2D pixel, SpriteFont font)
            => _floorMenu = new ChoiceBox(pixel, font);

        // riding or choosing, either way it owns input and movement is locked
        public bool IsBusy => _phase != Phase.None || _floorMenu.IsActive;

        // opens the floor list, leaving out whichever floor we're already on
        public void OpenFloorMenu()
        {
            _floorChoices.Clear();
            var labels = new List<string>();

            for (int i = 0; i < Floors.Length; i++)
            {
                if (Floors[i].Floor == _currentFloor) continue;

                _floorChoices.Add(i);
                labels.Add(Floors[i].Label);
            }

            labels.Add(CancelLabel); // always last, sits past the end of _floorChoices
            _floorMenu.Open(labels);
        }

        public void Update(GameTime gameTime, InputManager input, IElevatorHost host)
        {
            // the ride comes first, once it's going the menu is closed anyway
            if (_phase != Phase.None)
            {
                UpdateRide(gameTime, host);
                return;
            }

            if (!_floorMenu.IsActive) return;

            if (_floorMenu.Update(input) && _floorMenu.SelectedIndex >= 0)
            {
                // the last entry is Cancel, anything before it is a floor
                int picked = _floorMenu.SelectedIndex;
                if (picked < _floorChoices.Count) Start(_floorChoices[picked], host);
            }
        }

        private void Start(int floorIndex, IElevatorHost host)
        {
            _pendingFloor = floorIndex;
            _phase        = Phase.Closing;
            _timer        = 0f;

            host.FacePlayerOut(); // turn to face out of the lift
            host.SetDoorsShut(true);
            SoundManager.Play("doorShut");
        }

        private void UpdateRide(GameTime gameTime, IElevatorHost host)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (_phase)
            {
                case Phase.Closing:
                    if (_timer >= DoorSeconds)
                    {
                        // music runs for the ride itself, doors shut before it
                        SoundManager.PlayMusic("elevator");
                        Advance(Phase.Riding);
                    }
                    break;

                case Phase.Riding:
                    // the judder is applied in Draw, this just times it
                    if (_timer >= RideSeconds)
                    {
                        SoundManager.StopMusic();
                        SoundManager.Play("bell");
                        SoundManager.Play("doorShut"); // same clip for opening
                        host.SetDoorsShut(false);
                        _currentFloor = Floors[_pendingFloor].Floor;
                        Advance(Phase.Opening);
                    }
                    break;

                case Phase.Opening:
                    if (_timer >= DoorSeconds) Advance(Phase.None);
                    break;
            }
        }

        private void Advance(Phase next)
        {
            _phase = next;
            _timer = 0f;
        }

        // once it's arrived, walking onto the door tiles takes you out onto the
        // floor that was picked
        public void CheckExit(IElevatorHost host)
        {
            // no IsMoving check now that movement is free, he's walking when he
            // crosses the door and would never trigger otherwise
            if (_pendingFloor < 0 || host.IsChangingMap) return;
            if (!host.PlayerOnDoorTile) return;

            (int _, string _, string map, int x, int y) = Floors[_pendingFloor];
            if (map == null) return; // floor isn't built yet, nothing to walk into

            _pendingFloor = -1;
            host.ChangeMap(map, x, y);
        }

        // the lift judders on the way between floors, settling over the last
        // stretch so it isn't still rattling when the doors open. screen space,
        // the caller applies it after the camera so the view shakes and not the
        // world
        public Vector2 ShakeOffset
        {
            get
            {
                if (_phase != Phase.Riding) return Vector2.Zero;

                float left = RideSeconds - _timer;
                float mag  = RideShake * MathHelper.Clamp(left / RideSettleSeconds, 0f, 1f);

                return new Vector2(
                    ((float)_rng.NextDouble() * 2f - 1f) * mag,
                    ((float)_rng.NextDouble() * 2f - 1f) * mag);
            }
        }

        public void Draw(SpriteBatch spriteBatch) => _floorMenu.Draw(spriteBatch);
    }
}
