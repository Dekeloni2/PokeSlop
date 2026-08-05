using System.Collections.Generic;

namespace FinalProject.World
{
    // A boss encounter trigger: the door's tiles sit on the Objects layer (solid,
    // like an NPC), so walking into one opens a "want to enter?" Yes/No prompt
    // for TeacherId. Yes plays that teacher's own greeting
    // (BattleDialogue.OnEncounter from Teachers/{TeacherId}.json) and then
    // starts the fight; No just closes the prompt and leaves the player
    // standing there. Loaded from <mapname>.bossgates.json, same convention as
    // MapWarp's .warps.json — and reuses its WarpTile, since a tile reference
    // is a tile reference either way. See OverworldState.CheckBossGates.
    public class BossGate
    {
        public List<WarpTile> Tiles      { get; set; } = new();
        public string         TeacherId  { get; set; }
        public string         PromptText { get; set; }

        // shown instead of PromptText when RouteTracker says both David and
        // Yakir have been killed — Ben Dor can tell what route the player is
        // on before the fight even starts. Pacifist and Neutral read the same
        // (PromptText); null falls back to PromptText too, so a gate that
        // doesn't care about the route just doesn't set this.
        public string GenocidePromptText { get; set; }

        // shown instead of the prompt once RouteTracker says the fight is
        // over — which one depends on how it ended. Either can be left out
        // of the JSON; a resolved gate with no text for that outcome just
        // stays quiet, like a beaten NPC that's already gone from the map.
        public string SparedText { get; set; }
        public string KilledText { get; set; }

        public bool ContainsTile(int tileX, int tileY)
        {
            foreach (WarpTile t in Tiles)
                if (t.X == tileX && t.Y == tileY)
                    return true;
            return false;
        }
    }
}
