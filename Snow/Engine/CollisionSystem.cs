using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public static class CollisionSystem
    {
        public static bool CheckAABB(Rectangle a, Rectangle b)
        {
            return a.Right > b.Left &&
                   a.Left < b.Right &&
                   a.Bottom > b.Top &&
                   a.Top < b.Bottom;
        }

        public static void ResolveCollision(Actor actor, Tilemap tilemap, float deltaTime)
        {
            Vector2 position = actor.Position;
            Vector2 velocity = actor.Velocity;
            
            Rectangle actorBox;
            if (actor is Game.Player player)
            {
                actorBox = player.GetCollisionBox();
            }
            else
            {
                actorBox = new Rectangle(
                    (int)position.X,
                    (int)position.Y,
                    16,
                    24
                );
            }

            Vector2 movement = velocity * deltaTime;
            
            position.X += movement.X;
            if (actor is Game.Player p1)
            {
                actor.Position = position;
                actorBox = p1.GetCollisionBox();
            }
            else
            {
                actorBox.X = (int)position.X;
            }
            
            if (CheckTileCollision(actorBox, tilemap))
            {
                while (CheckTileCollision(actorBox, tilemap) && Math.Abs(movement.X) > 0.01f)
                {
                    position.X -= Math.Sign(movement.X) * 0.5f;
                    if (actor is Game.Player p2)
                    {
                        actor.Position = position;
                        actorBox = p2.GetCollisionBox();
                    }
                    else
                    {
                        actorBox.X = (int)position.X;
                    }
                }
                velocity.X = 0;
            }

            position.Y += movement.Y;
            if (actor is Game.Player p3)
            {
                actor.Position = position;
                actorBox = p3.GetCollisionBox();
            }
            else
            {
                actorBox.Y = (int)position.Y;
            }
            
            if (CheckTileCollision(actorBox, tilemap))
            {
                while (CheckTileCollision(actorBox, tilemap) && Math.Abs(movement.Y) > 0.01f)
                {
                    position.Y -= Math.Sign(movement.Y) * 0.5f;
                    if (actor is Game.Player p4)
                    {
                        actor.Position = position;
                        actorBox = p4.GetCollisionBox();
                    }
                    else
                    {
                        actorBox.Y = (int)position.Y;
                    }
                }
                velocity.Y = 0;
            }

            Rectangle groundCheck;
            if (actor is Game.Player p5)
            {
                var box = p5.GetCollisionBox();
                groundCheck = new Rectangle(
                    box.X + 2,
                    box.Bottom,
                    box.Width - 4,
                    2
                );
            }
            else
            {
                groundCheck = new Rectangle(
                    actorBox.X + 2,
                    actorBox.Bottom,
                    actorBox.Width - 4,
                    2
                );
            }
            bool isGrounded = CheckTileCollision(groundCheck, tilemap);

            actor.Position = position;
            actor.Velocity = velocity;
            
            if (actor is Game.Player player2)
            {
                var physicsField = player2.GetType().GetField("_physics", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (physicsField != null)
                {
                    var physics = physicsField.GetValue(player2) as PhysicsComponent;
                    if (physics != null)
                    {
                        physics.IsGrounded = isGrounded;
                        physics.Velocity = velocity;
                    }
                }
            }
        }

        private static bool CheckTileCollision(Rectangle box, Tilemap tilemap)
        {
            int startX = Math.Max(0, box.Left / tilemap.TileSize);
            int endX = Math.Min(tilemap.Width - 1, (box.Right - 1) / tilemap.TileSize);
            int startY = Math.Max(0, box.Top / tilemap.TileSize);
            int endY = Math.Min(tilemap.Height - 1, (box.Bottom - 1) / tilemap.TileSize);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    if (tilemap.IsSolid(x, y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}