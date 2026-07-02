using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Battle;
using FinalProject.Core;
using FinalProject.Data;
using FinalProject.Entities;

namespace FinalProject.States
{
    // Handles a single battle: 2 player creatures vs 1 wild creature.
    // Pushed onto the state stack by OverworldState when an encounter triggers.
    // Pops itself when the battle ends.
    
    public class BattleState : GameState
    {
        // The two player slots — slot 1 may be null if the player only has 1 creature
        private Player _player;
        private readonly int _playerChoice;
        
        private enum PlayerMoveList 
        {
            Attack, // deal dmg to teacher (sounds better in my head)
            Act, // list
            Item, // list
            Spare // only if teacher is spareable
        }
        
        private readonly Teacher _teacher;
        
        

        private BattlePhase _phase;

        // Which player slot is currently selecting a move (0 or 1)
        private int _selectingSlot;

        public BattleState(Game1 game, GameStateManager sm)
            : base(game, sm)
        {
            List<Teacher> party = game.PlayerData.Party;
            
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
            if (_playerChoice != null && Enum.IsDefined(typeof(PlayerMoveList), _playerChoice))
            {
                _phase = BattlePhase.ExecutingTurn;
            }
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
            return (_player._health <= 0 || _teacher.CurrentHp <= 0 || _teacher.IsSpared);
        }
    }

    // Holds a player's move choice for one slot this turn
    public class PlayerMoveChoice
    {
        public MoveData Move { get; }

        public PlayerMoveChoice(MoveData move)
        {
            Move = move;
        }
    }
    
    public class TeacherMoveChoice
    {
        public MoveData Move { get; }

        public TeacherMoveChoice(MoveData move)
        {
            Move = move;
        }
    }

    public enum BattlePhase { SelectingMove, ExecutingTurn, TeacherAttack, BattleOver }
}