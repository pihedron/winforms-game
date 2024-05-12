using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Entity
    {
        public Vector pos;
        public Vector dim;
        public Vector vel = new(0, 0);

        public bool isGrounded = false;

        public float movePower;
        public float jumpHeight;

        public Entity(Vector pos, Vector dim, float movePower, float jumpHeight)
        {
            this.pos = pos;
            this.dim = dim;
            this.movePower = movePower;
            this.jumpHeight = jumpHeight;
        }
    }
}
