using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public enum EntityState
    {
        Idle,
        //Walk,
        //Attack,
    }

    public class Entity(Vector pos, Vector dim, float movePower, float jumpHeight, string name) : Box(pos, dim)
    {
        public Vector vel = new(0, 0);

        public bool isGrounded = false;

        public float movePower = movePower;
        public float jumpHeight = jumpHeight;

        public string name = name;

        public EntityState state = EntityState.Idle;
        public Dictionary<EntityState, Bitmap[]> stateFrames = [];
        public int frameIndex = 0;

        public void GetFrames()
        {
            foreach (var stateValue in Enum.GetValues(typeof(EntityState)).Cast<EntityState>())
            {
                List<Bitmap> frames = [];
                foreach (var file in Directory.EnumerateFiles($"../../../img/{name}/{stateValue.ToString().ToLower()}"))
                {
                    frames.Add(new(file));
                }

                stateFrames[stateValue] = [.. frames];
            }
        }
    }
}
