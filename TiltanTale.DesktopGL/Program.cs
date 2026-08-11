using System;
using TiltanTale.DesktopGL;

// The desktop entry point. Run() owns the loop here — it blocks until the
// window closes. The browser head can't do that (the page's own event loop
// owns the frame timing), so it drives Tick() from requestAnimationFrame
// instead; see TiltanTale.Blazor/Pages/Index.razor.cs.
using var game = new DesktopTiltanTaleGame();
game.Run();
