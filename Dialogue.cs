using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class Dialogue
    {
        public PrivateFontCollection pfc = new();
        public FontFamily ff;
        public Font font;
        public SolidBrush brush = new(Color.White);
        public string text;
        public Dialogue(string text)
        {
            pfc.AddFontFile("../../../font/font.ttf");
            ff = new("Pixel Operator 8", pfc);
            font = new(ff, 16, FontStyle.Regular, GraphicsUnit.Pixel);
            this.text = text;
        }

        public void Show(Graphics g, Vector pos)
        {
            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Far
            };
            g.DrawString(text, font, brush, pos.x, pos.y - 16, stringFormat);
        }
    }
}
