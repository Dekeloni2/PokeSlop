using System;
using Microsoft.Xna.Framework;
using FinalProject.Core;

namespace FinalProject.Battle.Patterns;

public class BoatPattern : IBulletPattern
{
    public float Duration => 10f;
    
    private float _smokeSpawnTimer;
    private float _smokeSpawnInterval = 0.9f; // How often it releases smoke
    private int _lastThird = -1;

    public void Start(DodgeContext context)
    {
        // shrinking the box
        Rectangle box = context.CurrentBox;
        Rectangle tinyBox = new Rectangle(box.X + 30, box.Y, box.Width - 60, box.Height - 60);
        context.ResizeBoxTo(tinyBox, 0.8f);
    }

    public void Update(GameTime gameTime, DodgeContext context)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Rectangle box = context.CurrentBox;
        
        if (context.Elapsed >= Duration) return;
        
        _smokeSpawnTimer += dt;
        if (_smokeSpawnTimer >= _smokeSpawnInterval)
        {
            _smokeSpawnTimer = 0f;

            // Calculate 1/3 of the box width
            float smokeWidth = box.Width / 3f;

            // which location the smoke will spawn, prevents a repeat of same one in a row
            int currentThird;
            do
            {
                currentThird = Random.Shared.Next(3);
            }
            while (currentThird == _lastThird);

            _lastThird = currentThird;
            
            // Find the center X of that specific third
            float targetX = box.Left + (currentThird * smokeWidth) + (smokeWidth / 2f);

            int smokeDensity = 4;
            float startX = targetX - (smokeWidth * 0.5f);
            float spacing = smokeWidth / (smokeDensity - 1);

            for (int i = 0; i < smokeDensity; i++)
            {
                Vector2 spawnPos = new Vector2(startX + (i * spacing), box.Bottom);
                Vector2 velocity = new Vector2(0f, -GameSettings.DodgeProjectileSpeed * 0.7f);
                
                context.SpawnProjectile(spawnPos, velocity, ProjectileType.Smoke);
            }
        }
    }
}