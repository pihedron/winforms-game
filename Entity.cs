using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Game
{
    public enum EntityState
    {
        Idle,
        Walk,
        Die,
    }

    public class Entity : Box
    {
        public Vector vel = new(0, 0);

        public bool isGrounded = false;
        public bool isDying = false;

        public float movePower;
        public float jumpHeight;

        public string name;

        public EntityState state = EntityState.Idle;
        public Dictionary<EntityState, Bitmap[]> stateFrames = new();
        public int frameIndex = 0;
        public bool isFacingLeft = false;

        public Entity(Vector pos, Vector dim, float movePower, float jumpHeight, string name) : base(pos, dim)
        {
            this.movePower = movePower;
            this.jumpHeight = jumpHeight;
            this.name = name;
        }

        public void GetFrames()
        {
            foreach (var stateValue in Enum.GetValues(typeof(EntityState)).Cast<EntityState>())
            {
                List<Bitmap> frames = new();
                foreach (var file in Directory.EnumerateFiles($"{Game.prefix}img/{name}/{stateValue.ToString().ToLower()}").OrderBy(file => int.Parse(Regex.Match(file, @"\d+").Value)))
                {
                    Bitmap bitmap = new(file);
                    frames.Add(bitmap);
                    Bitmap reflected = (Bitmap)bitmap.Clone();
                    reflected.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    frames.Add(reflected);
                }

                stateFrames[stateValue] = frames.ToArray();
            }
        }
    }
}
