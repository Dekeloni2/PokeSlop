using System;
using Microsoft.Xna.Framework;

namespace FinalProject.Battle.Menu
{
    // one line in an ActionMenu page. Activate runs when the player confirms it.
    // Color is normally white, a teacher's name goes yellow on the MERCY page
    // once they can actually be spared
    public class MenuOption
    {
        public string Text     { get; }
        public Action Activate { get; }
        public Color  Color    { get; }

        public MenuOption(string text, Action activate, Color? color = null)
        {
            Text     = text;
            Activate = activate;
            Color    = color ?? Color.White;
        }
    }
}
