using System;

namespace FinalProject.Battle
{
    // one line in an ActionMenu page. Activate runs when the player confirms it
    public class MenuOption
    {
        public string Text     { get; }
        public Action Activate { get; }

        public MenuOption(string text, Action activate)
        {
            Text     = text;
            Activate = activate;
        }
    }
}
