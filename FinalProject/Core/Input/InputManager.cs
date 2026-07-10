using Microsoft.Xna.Framework.Input;

namespace FinalProject.Core.Input;

public class InputManager
{
    private KeyboardState _previous;
    private KeyboardState _current;

    // Must be called once per frame before anything reads input.
    public void Update()
    {
        _previous = _current;
        _current  = Keyboard.GetState();
    }

    // True every frame the key is held down.
    public bool IsKeyDown(Keys key)
        => _current.IsKeyDown(key);

    // True only on the first frame the key goes down.
    public bool IsKeyPressed(Keys key)
        => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    // True only on the first frame the key is released.
    public bool IsKeyReleased(Keys key)
        => !_current.IsKeyDown(key) && _previous.IsKeyDown(key);
}
