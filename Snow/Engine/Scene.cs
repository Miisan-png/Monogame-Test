using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class Scene
    {
        public string Name { get; set; }
        public Tilemap Tilemap { get; set; }
        public Vector2 PlayerSpawnPosition { get; set; }
        public List<IEntity> Entities { get; set; }

        public Scene()
        {
            Entities = new List<IEntity>();
        }
    }
}