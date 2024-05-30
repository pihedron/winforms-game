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

        public void Show(Graphics g)
        {
            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near
            };
            //g.DrawString("PAUSED", font, brush, Game.view.x / 2, Game.view.y / 2, stringFormat);
            int y = 4;
            foreach (var gate in Node.gates)
            {
                brush.Color = gate.Value.color;
                g.DrawString(gate.Key, font, brush, 0, y, stringFormat);
                y += 16 + 4;
            }
            brush.Color = defaultColor;
        }
    }
}
