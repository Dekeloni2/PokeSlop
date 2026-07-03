using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
<<<<<<< Updated upstream
using FinalProject.Data;

namespace FinalProject.States
{
    // Handles a single battle: 2 player creatures vs 1 wild creature.
    // Pushed onto the state stack by OverworldState when an encounter triggers.
    // Pops itself when the battle ends.
    public class BattleState : GameState
    {
        // The two player slots — slot 1 may be null if the player only has 1 creature
        private readonly Creature _playerSlot0;
        private readonly Creature _playerSlot1;

        // The single wild creature
        private readonly Creature _wild;
=======

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
>>>>>>> Stashed changes

        private BattlePhase _phase;
        private PlayerMoveList? _playerChoice;

<<<<<<< Updated upstream
        // Which player slot is currently selecting a move (0 or 1)
        private int _selectingSlot;

        // Moves chosen this turn — index matches slot
        private MoveChoice _slot0Choice;
        private MoveChoice _slot1Choice;

        public BattleState(Game1 game, GameStateManager sm, Creature wild)
            : base(game, sm)
        {
            List<Creature> party = game.PlayerData.Party;

            _playerSlot0   = party.Count > 0 ? party[0] : null;
            _playerSlot1   = party.Count > 1 ? party[1] : null;
            _wild          = wild;
            _phase         = BattlePhase.SelectingMove;
            _selectingSlot = 0;
=======
        public BattleState(Game1 game, GameStateManager sm, Teacher teacher)
            : base(game, sm)
        {
            _teacher = teacher;
            _phase   = BattlePhase.SelectingMove;
>>>>>>> Stashed changes
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
            // TODO: Attack/Act/Item/Spare menu + HP bars — battle UI comes next
        }

        // ── Phase: SelectingMove ─────────────────────────────────────────────

        private void UpdateMoveSelection()
        {
<<<<<<< Updated upstream
            // TODO: show move menu and collect input for each active slot
            // When both slots have chosen, move to execution
            if (_slot0Choice != null && (_playerSlot1 == null || _slot1Choice != null))
=======
            // TODO: show the Attack/Act/Item/Spare menu and collect input,
            // setting _playerChoice to whichever the player picked.
            if (_playerChoice.HasValue)
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        {
            bool playerLost = !(_playerSlot0?.IsAlive ?? false)
                           && !(_playerSlot1?.IsAlive ?? false);
            return !_wild.IsAlive || playerLost;
        }
    }

    // Holds a player's move choice for one slot this turn
    public class MoveChoice
    {
        public Creature User { get; }
        public MoveData Move { get; }

        public MoveChoice(Creature user, MoveData move)
        {
            User = user;
            Move = move;
        }
    }

    public enum BattlePhase { SelectingMove, ExecutingTurn, BattleOver }
}
=======
            => !Game.PlayerData.IsAlive || !_teacher.IsAlive || _teacher.IsSpared;
    }

    // The four choices on the Undertale-style battle menu.
    public enum PlayerMoveList { Attack, Act, Item, Spare }

    public enum BattlePhase { SelectingMove, ExecutingTurn, BattleOver }
}
>>>>>>> Stashed changes
