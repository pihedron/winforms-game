using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class PauseMenu
    {
        public PrivateFontCollection pfc = new();
        public FontFamily ff;
        public Font font;
        public static Color defaultColor = Color.White;
        public SolidBrush brush = new(defaultColor);
        public PauseMenu()
        {
            pfc.AddFontFile("../../../font/font.ttf");
            ff = new("Pixel Operator 8", pfc);
            font = new(ff, 16, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public void Show(Graphics g, Vector pos)
        {
            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("PAUSED", font, brush, pos.x, pos.y, stringFormat);
        }
    }
}
