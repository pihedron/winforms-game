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
        public Pen pen = new(defaultColor, 2);
        public Vector scroll = new();
        public Vector vel = new();
        public Dictionary<string, string> movement = new()
        {
            { "W", "jump" },
            { "A", "left" },
            { "D", "right" },
        };
        public Dictionary<string, string> toggles = new()
        {
            { "G", "graphics" },
            { "M", "mouse peek" },
        };

        private const int spacing = 4;

        public PauseMenu()
        {
            pfc.AddFontFile("../../../font/font.ttf");
            ff = new("Pixel Operator 8", pfc);
            font = new(ff, 16, FontStyle.Regular, GraphicsUnit.Pixel);
            pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
        }

        public void Show(Graphics g)
        {
            StringFormat stringFormat = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("[ESC] continue\n\n[UP] [DOWN] scroll", font, brush, Game.view.x / 2, Game.view.y / 2, stringFormat);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            int x = (int)((int)(Game.view.x / 8) - scroll.x);
            int y = (int)((int)(Game.view.y / 8) + spacing - scroll.y);
            bool a;
            bool b;
            foreach (var gate in Node.gates)
            {
                a = false;
                b = false;

                brush.Color = gate.Value.color;

                g.DrawString(gate.Key, font, brush, x, y, stringFormat);
                y += 16 + spacing;

                brush.Color = Color.White;

                DrawRow(g, x, ref y, new bool[3] { a, b, gate.Value.eval(a, b) });
                a = true;

                DrawRow(g, x, ref y, new bool[3] { a, b, gate.Value.eval(a, b) });
                b = true;

                DrawRow(g, x, ref y, new bool[3] { a, b, gate.Value.eval(a, b) });

                y += spacing;
            }
            brush.Color = defaultColor;

            x = (int)((int)(Game.view.x * 6 / 8) - scroll.x);
            y = (int)((int)(Game.view.y / 8) + spacing - scroll.y);

            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;

            g.DrawString("MOVEMENT", font, brush, x, y, stringFormat);

            y += Game.size / 2 + spacing;

            foreach (var bind in movement)
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                g.DrawRectangle(pen, x, y, Game.size / 2, Game.size / 2);
                g.DrawString(bind.Key, font, brush, x + Game.size / 4, y + Game.size / 4, stringFormat);

                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;

                g.DrawString(bind.Value, font, brush, x + Game.size / 2 + spacing, y + Game.size / 4, stringFormat);

                y += Game.size / 2 + spacing;
            }

            y += Game.size / 2 + spacing;

            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;

            g.DrawString("TOGGLES", font, brush, x, y, stringFormat);

            y += Game.size / 2 + spacing;

            foreach (var bind in toggles)
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                g.DrawRectangle(pen, x, y, Game.size / 2, Game.size / 2);
                g.DrawString(bind.Key, font, brush, x + Game.size / 4, y + Game.size / 4, stringFormat);

                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;

                g.DrawString(bind.Value, font, brush, x + Game.size / 2 + spacing, y + Game.size / 4, stringFormat);

                y += Game.size / 2 + spacing;
            }
        }

        public void DrawRow(Graphics g, int x, ref int y, bool[] bools)
        {
            for (int i = 0; i < bools.Length; i++)
            {
                bool b = bools[i];
                if (i == bools.Length - 1)
                {
                    int btm = y + Game.size / 2;
                    int mid = (y + btm) / 2;
                    g.DrawLine(pen, x, mid, x + Game.size / 2, mid);
                    g.DrawLine(pen, x + Game.size / 2, mid, x + Game.size / 4, y);
                    g.DrawLine(pen, x + Game.size / 2, mid, x + Game.size / 4, btm);
                    x += Game.size / 2 + spacing;
                }
                if (b)
                {
                    g.FillRectangle(brush, x, y, Game.size / 2, Game.size / 2);
                }
                else
                {
                    g.DrawRectangle(pen, x, y, Game.size / 2, Game.size / 2);
                }
                x += Game.size / 2 + spacing;
            }
            y += Game.size / 2 + spacing;
        }
    }
}
