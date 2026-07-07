using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;

namespace FinalProject.States
{
    // Handles a single boss fight: the player vs one Teacher.
    // Undertale-style — Attack, Act, Item, Spare. ACT is meant to build
    // toward being able to Spare the boss; Attack deals damage instead.
    // Pushed onto the state stack when a boss encounter starts, pops itself
    // when the fight ends (Teacher defeated, spared, or the player loses).
    public class BattleState : GameState
    {
        private readonly Teacher _teacher;

        private BattlePhase _phase;
        private PlayerMoveList? _playerChoice;
        
        private static int boxSpawnPosX = 100;
        private static int boxSpawnPosY = 100;
        private static int boxWidth = 300;
        private static int boxHeight = 200;
        
        private Rectangle battleBox = new Rectangle(boxSpawnPosX, boxSpawnPosY, boxWidth, boxHeight);
        
        public BattleState(Game1 game, GameStateManager sm, Teacher teacher)
            : base(game, sm)
        {
            _teacher = teacher;
            _phase   = BattlePhase.SelectingMove;
        }
        
        public override void Update(GameTime gameTime)
        {
            switch (_phase)
            {
                case BattlePhase.SelectingMove:
                    UpdateMoveSelection();
                    break;

                case BattlePhase.ExecutingTurn:
                    UpdateTurnExecution();
                    break;

                case BattlePhase.BattleOver:
                    StateManager.Pop();
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game.GraphicsDevice.Clear(Color.Black);
            if (_phase == BattlePhase.Intro)


            // TODO: Attack/Act/Item/Spare menu + HP bars — battle UI comes next
        }

        // ── Phase: SelectingMove ─────────────────────────────────────────────

        private void UpdateMoveSelection()
        {
            // TODO: show the Attack/Act/Item/Spare menu and collect input,
            // setting _playerChoice to whichever the player picked.
            if (_playerChoice.HasValue)
                _phase = BattlePhase.ExecutingTurn;
        }

        // ── Phase: ExecutingTurn ─────────────────────────────────────────────

        private void UpdateTurnExecution()
        {
            // TODO: resolve the chosen action —
            //   Attack: deal damage to _teacher
            //   Act:    make progress toward Spare being viable
            //   Item:   use an item (heal/buff/etc.)
            //   Spare:  if ACT progress allows it, _teacher.Spare()
            // then the Teacher's turn (attack back, using _teacher.Moves).

            _playerChoice = null;

            _phase = IsBattleOver() ? BattlePhase.BattleOver : BattlePhase.SelectingMove;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool IsBattleOver()
            => !Game.PlayerData.IsAlive || !_teacher.IsAlive || _teacher.IsSpared;
    }

    // The four choices on the Undertale-style battle menu.
    public enum PlayerMoveList { Attack, Act, Item, Spare }

    public enum BattlePhase {Intro, SelectingMove, ExecutingTurn, BattleOver }
}
