using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Block : Box
    {
        public bool isDangerous = false;
        public bool isEnd = false;
        public Bitmap image;

        public Block(Vector pos, Vector dim, string imagePath) : base(pos, dim)
        {
            image = new(imagePath);
        }

        public Block(Vector pos, Vector dim, string imagePath, bool isDangerous) : base(pos, dim)
        {
            this.isDangerous = isDangerous;
            image = new(imagePath);
        }
    }
}
