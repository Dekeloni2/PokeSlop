
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Core
{
    // Manages a stack of GameStates. Only the top state runs each frame.
    //
    //   Replace → swap entirely, no going back (menu → overworld)
    //   Push    → overlay on top, current state pauses (overworld → battle)
    //   Pop     → remove top, state beneath resumes (battle ends → overworld)
    public class GameStateManager
    {
        private readonly Stack<GameState> _stack = new Stack<GameState>();

        public GameState Current => _stack.Count > 0 ? _stack.Peek() : null;

        public void Replace(GameState newState)
        {
            if (_stack.Count > 0)
            {
                _stack.Peek().OnExit();
                _stack.Pop();
            }

            _stack.Push(newState);
            newState.OnEnter();
        }

        public void Push(GameState newState)
        {
            if (_stack.Count > 0)
                _stack.Peek().Pause();

            _stack.Push(newState);
            newState.OnEnter();
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;

            _stack.Peek().OnExit();
            _stack.Pop();

            if (_stack.Count > 0)
                _stack.Peek().Resume();
        }

        public void Update(GameTime gameTime)     => Current?.Update(gameTime);
        public void Draw(SpriteBatch spriteBatch) => Current?.Draw(spriteBatch);
    }
}