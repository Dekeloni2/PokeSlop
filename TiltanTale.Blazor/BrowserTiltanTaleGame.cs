using FinalProject;

namespace TiltanTale.Blazor
{
    // Game1 with the browser's answers filled in.
    //
    // Two of Game1's defaults assume a window the game owns, and a tab isn't
    // that. Everything else — states, battles, maps, audio — is the same code
    // the desktop build runs; see DesktopTiltanTaleGame for the other side.
    public class BrowserTiltanTaleGame : Game1
    {
        // A page can't close itself, so Game.Exit() throws
        // PlatformNotSupportedException here. Saying so up front means the
        // "hold ESC to quit" timer never starts and the instructions screen
        // never offers a key that does nothing.
        public override bool CanExitToDesktop => false;

        // Fullscreen in a browser has to come from a user gesture the browser
        // trusts, not from the game noticing a keypress. F11 already does the
        // right thing without our help, and the canvas is scaled to fit its
        // page by CSS (see wwwroot/index.html) — the same letterboxing
        // GameSettings.FitToScreen gives the desktop back buffer.
        public override bool CanToggleFullscreen => false;
    }
}
