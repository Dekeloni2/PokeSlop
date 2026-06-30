using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
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

        private BattlePhase _phase;

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
            // battle UI comes next
        }

        // ── Phase: SelectingMove ─────────────────────────────────────────────

        private void UpdateMoveSelection()
        {
            // TODO: show move menu and collect input for each active slot
            // When both slots have chosen, move to execution
            if (_slot0Choice != null && (_playerSlot1 == null || _slot1Choice != null))
                _phase = BattlePhase.ExecutingTurn;
        }

        // ── Phase: ExecutingTurn ─────────────────────────────────────────────

        private void UpdateTurnExecution()
        {
            // TODO: resolve moves in Speed order, apply damage, check reactions
            if (IsBattleOver())
                _phase = BattlePhase.BattleOver;
            else
                _phase = BattlePhase.SelectingMove;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool IsBattleOver()
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