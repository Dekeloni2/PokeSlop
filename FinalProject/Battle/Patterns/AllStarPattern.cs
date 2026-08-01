using System;
using FinalProject.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FinalProject.Battle.Patterns;

public class AllStarPattern : IBulletPattern
{
    public float Duration => 30f;
    
    private static readonly string[] Lyrics = new string[]
    {
        // Verse 1
        "Somebody", "once", "told", "me", "the", "world", "is", "gonna", "roll", "me",
        "I", "ain't", "the", "sharpest", "tool", "in", "the", "shed",
        "She", "was", "looking", "kind", "of", "dumb", "with", "her", "finger", "and", "her", "thumb",
        "In", "the", "shape", "of", "an", "\"L\"", "on", "her", "forehead",
        
        // Pre Chorus
        "WELL,", "the", "years", "start", "comin'", "and", "they", "don't", "stop", "comin'",
        "Fed", "to", "the", "rules", "and", "I", "hit", "the", "ground", "runnin'",
        "Didn't", "make", "sense", "not", "to", "live", "for", "fun",
        "Your", "brain", "gets", "smart", "but", "your", "head", "gets", "dumb",
        "So", "much", "to", "do,", "so", "much", "to", "see",
        "So", "what's", "wrong", "with", "taking", "the", "backstreets?",
        "You'll", "never", "know", "if", "you", "don't", "go",
        "You'll", "never", "shine", "if", "you", "don't", "glow",
        
        // Chorus
        "Hey", "now,", "you're", "an", "all", "star",
        "Get", "your", "game", "on,", "go", "play",
        "Hey", "now,", "you're", "a", "rock", "star",
        "Get", "the", "show", "on,", "get", "paid",
        "And", "all", "that", "glitters", "is", "gold",
        "Only", "shootin'", "stars", "break", "the", "mold"
    };
    
    private int _wordIndex = 0;
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 0.35f; // Delay between word spawns
    private int _spawnSide = 0; // Rotates between 0: Top, 1: Left, 2: Right
    
    private SpriteFont _cachedFont;
    
    public void Start(DodgeContext context)
    {
        context.SetTeacherVisible(false);

        // Slightly compress the box for extra tension
        Rectangle box = context.CurrentBox;
        Rectangle patternBox = new Rectangle(box.X + 20, box.Y, box.Width - 40, box.Height);
        context.ResizeBoxTo(patternBox, 0.5f);
        
        _wordIndex = 0;
        _spawnTimer = 0f;
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (context.Elapsed >= Duration)
            return;

        _spawnTimer += dt;

        if (_spawnTimer >= SpawnInterval && _wordIndex < Lyrics.Length)
        {
            _spawnTimer -= SpawnInterval; // Prevents skipping
            SpawnWordProjectile(context, Lyrics[_wordIndex]);
            _wordIndex++;
        }
    }

    private void SpawnWordProjectile(DodgeContext context, string word)
    {
        Rectangle box = context.CurrentBox;
        Vector2 spawnPos;
        Vector2 velocity;

        // Base speed scale
        float speed = GameSettings.DodgeProjectileSpeed * 1.1f;

        // Rotate sides to create varied bullet directions
        switch (_spawnSide)
        {
            case 0: // Rain down from the top
                float randomX = Random.Shared.Next(box.Left + 10, box.Right - 30);
                spawnPos = new Vector2(randomX, box.Top - 10);
                velocity = new Vector2(0f, speed);
                break;

            case 1: // Sweep in from the left
                float randomYLeft = Random.Shared.Next(box.Top + 10, box.Bottom - 10);
                spawnPos = new Vector2(box.Left - 20, randomYLeft);
                velocity = new Vector2(speed * 0.9f, 0f);
                break;

            case 2: // Sweep in from the right
            default:
                float randomYRight = Random.Shared.Next(box.Top + 10, box.Bottom - 10);
                spawnPos = new Vector2(box.Right + 20, randomYRight);
                velocity = new Vector2(-speed * 0.9f, 0f);
                break;
        }

        // Cycle spawn side for next word
        _spawnSide = (_spawnSide + 1) % 3;

        // Passing _cachedFont (or null) safely instantiates the word bullet
        context.SpawnProjectile(spawnPos, velocity, word, _cachedFont, 3f);
    }
    
    public void DrawUi(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle box)
    {
        // Captures the active battle font as soon as rendering starts
        if (font != null)
            _cachedFont = font;
    }
}