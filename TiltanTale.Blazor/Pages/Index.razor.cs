using System;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace TiltanTale.Blazor.Pages
{
    // Where the browser's frame loop meets the game's.
    //
    // On desktop, Game.Run() blocks and owns the loop for the life of the
    // process. A browser tab can't be held like that — the page has its own
    // event loop and blocking it freezes everything, including the rendering
    // we're trying to do. So the page drives instead: requestAnimationFrame
    // calls back into TickDotNet, and each call advances the game exactly one
    // frame. Run() here only sets the game up and returns.
    public partial class Index
    {
        private Game _game;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // The canvas has to exist in the DOM before the graphics device
            // can attach to it, which is why this waits for the first render
            // rather than starting from OnInitialized.
            if (firstRender)
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void TickDotNet()
        {
            if (_game == null)
            {
                _game = new BrowserTiltanTaleGame();
                _game.Run(); // initialises and returns; does not loop
            }

            _game.Tick();
        }
    }
}
