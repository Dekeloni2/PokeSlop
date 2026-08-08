using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FinalProject.Battle.Hazards;

public class OnionRing
{
    public float Radius { get; private set; }
    public float GateAngle { get; private set; }  // the angle where the empty gap is (radians)
    public float GateWidth { get; private set; }  // gap width (radians)
    
    public bool HasHitPlayer { get; set; } = false;
    
    private float _shrinkSpeed;
    private float _thickness = 8f; // thickness of the ring line
    
    public bool IsExpired => Radius <= 10f; // disappears near the center
    
    public OnionRing(float startRadius, float gateAngle, float gateWidth, float shrinkSpeed) // constructor
    {
        Radius = startRadius;
        GateAngle = gateAngle;
        GateWidth = gateWidth;
        _shrinkSpeed = shrinkSpeed;
    }
    
    public void Update(float dt)
    {
        Radius -= _shrinkSpeed * dt;
    }
    
    // checks if the player is touching the ring and not inside the gate
    public bool CheckCollision(Vector2 center, Vector2 playerPos, float playerRadius = 6f)
    {
        float dist = Vector2.Distance(center, playerPos);

        // Is the player near the ring boundary?
        if (Math.Abs(dist - Radius) <= (_thickness / 2f + playerRadius))
        {
            // Calculate angle from center to player
            Vector2 dir = playerPos - center;
            float angle = (float)Math.Atan2(dir.Y, dir.X);

            // Normalize angles to 0..2PI
            float diff = MathHelper.WrapAngle(angle - GateAngle);

            // If the player's angle is NOT inside the gate width, they hit the ring!
            if (Math.Abs(diff) > GateWidth / 2f)
            {
                return true;
            }
        }

        return false;
    }
}