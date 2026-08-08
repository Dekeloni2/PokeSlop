using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FinalProject.Core.Graphics;

namespace FinalProject.World
{
    // A stationary NPC placed on a map's "NPCs" object layer in Tiled — an
    // object with "Id" and (optionally) "Sprite"/"Scale"/"Kind" custom
    // properties. Id doubles as the RouteTracker key and, for a battle NPC,
    // the Teachers/{id}.json filename, so winning or sparing its fight is
    // what makes it disappear from the map for good.
    //
    // Kind picks what happens on bump through NpcInteractionRegistry and
    // defaults to Battle, so every NPC placed before Kind existed keeps
    // working with no map edits. A new behavior means a new INpcInteraction
    // and a registry entry — same shape as IBulletPattern/PatternRegistry —
    // not a new subclass of this one.
    public class Npc
    {
        public int    TileX      { get; }
        public int    TileY      { get; }
        public string Id         { get; }
        public string SpriteName { get; }

        // multiplies the sprite's drawn size. overworld art is authored at
        // whatever size suited the drawing, but the maps here are 20x20 tiles —
        // this is what brings an oversized sprite down to standing height
        // without re-exporting the png
        public float Scale { get; }

        private readonly INpcInteraction _interaction;

        public Npc(int tileX, int tileY, string id, string spriteName,
                   float scale = 1f, string kind = null)
        {
            TileX        = tileX;
            TileY        = tileY;
            Id           = id;
            SpriteName   = spriteName;
            Scale        = scale <= 0f ? 1f : scale;
            _interaction = NpcInteractionRegistry.Resolve(kind ?? NpcInteractionRegistry.DefaultKind);
        }

        // runs whatever this NPC does on bump. the caller only decides WHEN
        // (facing tile, not already resolved) — WHAT happens is entirely this
        // NPC's own business, resolved once at construction from Kind
        public void Interact(NpcInteractionContext context) => _interaction.Trigger(this, context);

        // scaled to standing height first, then bottom-anchored to its tile
        // off the SCALED size — same idea as the player's own draw offset,
        // so a tall sprite stands on the tile instead of floating in it or
        // sinking through the floor
        public void Draw(SpriteBatch spriteBatch, int tileSize)
        {
            Spritesheet sheet = SpriteManager.GetSprite(SpriteName);
            if (sheet?.Texture == null) return;

            Rectangle src = sheet[0, 0];
            int w = (int)(src.Width  * Scale);
            int h = (int)(src.Height * Scale);

            var dst = new Rectangle(
                (int)(TileX * tileSize + tileSize / 2f - w / 2f),
                TileY * tileSize + tileSize - h,
                w, h);

            spriteBatch.Draw(sheet.Texture, dst, src, Color.White);
        }
    }
}
