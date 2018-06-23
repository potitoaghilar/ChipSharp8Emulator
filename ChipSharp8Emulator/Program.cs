using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChipSharp8Emulator {

    class Program {

        static void Main(string[] args) {
            
            Chip8 chip8 = new Chip8();
            chip8.LoadROM("./rom/WIPEOFF");
            //chip8.LoadROM("./rom/pong.rom");
            chip8.onRenderListener += Chip8_onRenderListener;
            chip8.Run();
            
        }

        private static void Chip8_onRenderListener(Graphics g, byte[] pixels, int width, int height, int zoom) {

            if (g == null) { return; }

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {

                    Color color = Color.FromArgb(pixels[y * width + x], pixels[y * width + x], pixels[y * width + x]);
                    g.FillRectangle(new SolidBrush(color), x * zoom, y * zoom, zoom, zoom);

                }
            }
        }
    }

}
