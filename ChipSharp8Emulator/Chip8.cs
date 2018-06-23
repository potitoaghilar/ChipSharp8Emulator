using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChipSharp8Emulator {

    public class Chip8 {

        // Main objects
        private byte[] memory;
        private byte[] registers;
        private ushort[] stack;
        private byte[] input;
        private byte[] display;
        private byte delay_timer;
        private byte sound_timer;

        // Pointers
        private ushort pc; // Program counter
        private ushort I; // Address register
        private ushort sp; // Stack pointer

        // Hardware vars
        private static int memorySize = 4096; // 4K memory
        private static int stackSize = 48; // 48 bytes for the stack = 24 levels of nesting
        private static ushort memoryEntryPoint = 0x200; // Entry point at address 512
        private static ushort fontsetEntryPoint = 0x50; // Entry point at address 80
        private static int displayWidth = 64;
        private static int displayHeight = 32;

        // Emulator zoom
        private int zoom;

        // Running vars
        private Thread runThread;
        private bool isRunning = false;

        // Emulator screen graphics
        MainWindow mainWindow;
        Graphics g;
        string romName = "";

        // OnRender event handler
        public event OnRenderListener onRenderListener;
        public delegate void OnRenderListener(Graphics g, byte[] pixels, int width, int height, int zoom);

        Random random = new Random();

        public Chip8() {

            // Create objects
            memory = new byte[memorySize];
            registers = new byte[16];
            stack = new ushort[stackSize];
            input = new byte[16];
            display = new byte[(displayWidth + 1) * (displayHeight + 1)];

            // Init pointers
            pc = memoryEntryPoint;
            I = 0x0;
            sp = 0x0;

            // Load fontset
            LoadFontset();

            // Set zoom
            zoom = 10;

            // Show emulator window
            ShowWindow();

            // Welcome
            Console.WriteLine("Chip8 Emulator is starting\n");
            
        }

        private void ShowWindow() {
            new Thread(() => {
                // Show main window
                mainWindow = new MainWindow();
                mainWindow.Text += romName;
                mainWindow.ClientSize = new Size(640, 320);
                g = mainWindow.Controls["container"].CreateGraphics();
                mainWindow.chip8 = this;
                mainWindow.ShowDialog();
            }).Start();
        }

        private void LoadFontset() {
            byte[] fontset = Fontset.getFontset();
            // Copy fontset to memory
            for (int i = 0; i < fontset.Length; i++) {
                memory[fontsetEntryPoint + i] = fontset[i];
            }
        }

        public void LoadROM(String path) {
            byte[] romData = File.ReadAllBytes(path);

            // Copy rom to memory
            for (int i = 0; i < romData.Length; i++) {
                memory[memoryEntryPoint + i] = romData[i];
            }

            // Change window title
            if (mainWindow != null) {
                mainWindow.Text += path;
            } else {
                romName = path;
            }

            Console.WriteLine("Loaded ROM: " + path + "\n");
        }

        public void Run() {
            isRunning = true;
            
            runThread = new Thread(() => {
                while (isRunning) {
                    
                    ExecuteCycle();
                    Thread.Sleep(16); // 60 FPS

                }
            });

            runThread.Start();
        }

        public void Stop() {
            isRunning = false;
        }

        private void ExecuteCycle() {
            bool needRender = false;

            ushort opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);

            Console.Write(Utils.toHex(opcode) + ": ");

            switch (opcode & 0xF000) {

                case 0x000: {

                        switch (opcode & 0x0FFF) {

                            case 0x00E0: { // 00E0: Clears the screen.
                                    for (int i = 0; i < display.Length; i++) {
                                        display[i] = 0;
                                    }
                                    pc += 2;
                                    Console.WriteLine("Clears the screen");
                                    break;
                                }

                            case 0x00EE: { // 00EE: Returns from a subroutine.
                                    sp--;
                                    pc = (ushort)(stack[sp] + 2);
                                    stack[sp] = 0;
                                    Console.WriteLine("Returns from a subroutine at " + Utils.toHex(pc));
                                    break;
                                }

                            default: {
                                    NotImplementedYet();
                                    Stop();
                                    break;
                                }
                        }

                        break;
                    }

                case 0x1000: { // 1NNN: Jumps to address NNN.
                        ushort nnn = (ushort)(opcode & 0x0FFF);
                        pc = nnn;
                        Console.WriteLine("Jumps to address " + Utils.toHex(nnn));
                        break;
                    }

                case 0x2000: { // 2NNN: Calls subroutine at NNN.
                        stack[sp++] = pc;
                        ushort nnn = (ushort)(opcode & 0x0FFF);
                        pc = nnn;
                        Console.WriteLine("Calls subroutine at " + Utils.toHex(nnn));
                        break;
                    }

                case 0x3000: { // 3XNN: Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        byte nn = (byte)(opcode & 0x00FF);
                        if (registers[x] == nn) {
                            pc += 4;
                        } else {
                            pc += 2;
                        }
                        Console.WriteLine("Skip next instruction if V" + Utils.toHex(x, false) + " == " + Utils.toHex(nn) + ". Jump is " + (registers[x] == nn ? "" : "NOT ") + "taken!");
                        break;
                    }

                case 0x4000: { // 4XNN: Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block)
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        byte nn = (byte)(opcode & 0x00FF);
                        if (registers[x] != nn) {
                            pc += 4;
                        } else {
                            pc += 2;
                        }
                        Console.WriteLine("Skips the next instruction if V" + Utils.toHex(x, false) + " != " + Utils.toHex(nn) + ". Jump is " + (registers[x] != nn ? "" : "NOT ") + "taken!");
                        break;
                    }

                case 0x6000: { // 6XNN: Sets VX to NN.
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        byte nn = (byte)(opcode & 0x00FF);
                        registers[x] = nn;
                        pc += 2;
                        Console.WriteLine("Sets V" + Utils.toHex(x, false) + " to " + Utils.toHex(nn));
                        break;
                    }

                case 0x7000: { // 7XNN: Adds NN to VX. (Carry flag is not changed)
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        byte nn = (byte)(opcode & 0x00FF);
                        registers[x] += nn;
                        pc += 2;
                        Console.WriteLine("Adds " + Utils.toHex(nn) + " to V" + Utils.toHex(x, false));
                        break;
                    }

                case 0x8000: {

                        switch (opcode & 0x000F) {

                            case 0x0000: { // 8XY0: Sets VX to the value of VY.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    byte y = (byte)((opcode & 0x00F0) >> 4);
                                    registers[x] = registers[y];
                                    pc += 2;
                                    Console.WriteLine("Sets V" + Utils.toHex(x, false) + " to the value of V" + Utils.toHex(y, false));
                                    break;
                                }

                            case 0x0002: { // 8XY2: Sets VX to VX and VY. (Bitwise AND operation)
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    byte y = (byte)((opcode & 0x00F0) >> 4);
                                    registers[x] &= registers[y];
                                    pc += 2;
                                    Console.WriteLine("Sets V" + Utils.toHex(x, false) + " to " + registers[x]);
                                    break;
                                }

                            case 0x0004: { // 8XY4: Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    byte y = (byte)((opcode & 0x00F0) >> 4);
                                    registers[0xF] = (byte)(((registers[x] + registers[y]) & 0xF00) >> 8);
                                    registers[x] += registers[y];
                                    pc += 2;
                                    Console.WriteLine("Adds V" + Utils.toHex(y, false) + " to V" + Utils.toHex(x));
                                    break;
                                }

                            case 0x0005: { // 8XY5: VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    byte y = (byte)((opcode & 0x00F0) >> 4);
                                    registers[0xF] = (byte)(registers[x] < registers[y] ? 0 : 1);
                                    registers[x] -= registers[y];
                                    pc += 2;
                                    Console.WriteLine("V" + Utils.toHex(y, false) + " is subtracted from V" + Utils.toHex(x, false));
                                    break;
                                }

                            default: {
                                    NotImplementedYet();
                                    Stop();
                                    break;
                                }
                        }

                        break;
                    }

                case 0xA000: { // ANNN: Sets I to the address NNN.
                        ushort nnn = (ushort)(opcode & 0x0FFF);
                        I = nnn;
                        pc += 2;
                        Console.WriteLine("Sets I to the addess " + Utils.toHex(nnn));
                        break;
                    }

                case 0xC000: { // CXNN: Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        ushort nn = (ushort)(opcode & 0x00FF);
                        registers[x] = (byte)(random.Next(0, 256) & nn);
                        pc += 2;
                        Console.WriteLine("Sets V" + Utils.toHex(x, false) + " to " + Utils.toHex(nn));
                        break;
                    }

                case 0xD000: { // DXYN: Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. Each row of 8 pixels is read as bit-coded starting from memory location I; I value doesn’t change after the execution of this instruction. As described above, VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that doesn’t happen
                        byte x = (byte)((opcode & 0x0F00) >> 8);
                        byte y = (byte)((opcode & 0x00F0) >> 4);
                        byte n = (byte)(opcode & 0x000F);

                        // Draw
                        for (int i = 0; i < n; i++) {
                            for (int o = 0; o < 8; o++) {

                                byte pixelColor = 0;
                                if (((memory[I + i] >> (7 - o)) & 0x1) == 1) {
                                    pixelColor = 0xFF;
                                }

                                int pixelIndex = (registers[y] + i) * displayWidth + registers[x] + o;

                                // Collision detection
                                if (display[pixelIndex] == 0xFF && pixelColor == 0x0) {
                                    registers[0xF] = 0x1;
                                }

                                display[pixelIndex] = pixelColor;

                            }
                        }

                        pc += 2;
                        needRender = true;
                        Console.WriteLine("Drawing sprite at position (" + registers[x] + ", " + registers[y] + ") of size { " + n + " x " + "8 }");
                        break;
                    }

                case 0xE000: {

                        switch (opcode & 0x00FF) {

                            case 0x009E: { // EX9E: Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block)
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    bool condition = input[registers[x]] == 1;
                                    if (condition) {
                                        pc += 4;
                                    } else {
                                        pc += 2;
                                    }
                                    Console.WriteLine("Skips the next instruction if the key stored in V" + Utils.toHex(x, false) + " is pressed. Jump is " + (condition ? "" : "NOT ") + "taken");
                                    break;
                                }

                            case 0x00A1: { // EXA1: Skips the next instruction if the key stored in VX isn't pressed. (Usually the next instruction is a jump to skip a code block)
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    bool condition = input[registers[x]] == 0;
                                    if (condition) {
                                        pc += 4;
                                    } else {
                                        pc += 2;
                                    }
                                    Console.WriteLine("Skips the next instruction if the key stored in V" + Utils.toHex(x, false) + " isn't pressed. Jump is " + (condition ? "" : "NOT ") + "taken");
                                    break;
                                }

                            default: {
                                    NotImplementedYet();
                                    Stop();
                                    break;
                                }
                        }

                        break;
                    }

                case 0xF000: {

                        switch (opcode & 0x00FF) {

                            case 0x001E: { // FX1E: Adds VX to I.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    I += registers[x];
                                    pc += 2;
                                    Console.WriteLine("Adds V" + Utils.toHex(x, false) + " to I");
                                    break;
                                }

                            case 0x0007: { // FX07: Sets VX to the value of the delay timer.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    registers[x] = delay_timer;
                                    pc += 2;
                                    Console.WriteLine("Sets V" + Utils.toHex(x, false) + " to the value of the delay timer: " + Utils.toHex(delay_timer));
                                    break;
                                }

                            case 0x0015: { // FX15: Sets the delay timer to VX.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    delay_timer = registers[x];
                                    pc += 2;
                                    Console.WriteLine("Sets the delay timer to V" + Utils.toHex(x, false) + ": " + Utils.toHex(delay_timer));
                                    break;
                                }

                            case 0x0018: { // FX18: Sets the sound timer to VX.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    sound_timer = registers[x];
                                    pc += 2;
                                    Console.WriteLine("Sets the sound timer to V" + Utils.toHex(x, false) + ": " + Utils.toHex(sound_timer));
                                    break;
                                }

                            case 0x0029: { // FX29: Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    I = memory[fontsetEntryPoint + registers[x] * 5];
                                    pc += 2;
                                    Console.WriteLine("Sets I to the location of the sprite for the character in V" + Utils.toHex(x, false));
                                    break;
                                }

                            case 0x0033: { // FX33: Stores the binary-coded decimal representation of VX, with the most significant of three digits at the address in I, the middle digit at I plus 1, and the least significant digit at I plus 2. (In other words, take the decimal representation of VX, place the hundreds digit in memory at location in I, the tens digit at location I+1, and the ones digit at location I+2.)
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    byte value = registers[x];

                                    byte h = (byte)((value - (value % 100)) / 100);
                                    byte d = (byte)((value - h * 100 - (value % 10)) / 10);
                                    byte u = (byte)(value - h * 100 - d * 10);

                                    pc += 2;
                                    Console.WriteLine("Stores binary-coded decimal rappresentation of V" + Utils.toHex(x, false) + " in I, I+1 and I+2 as { " + h + ", " + d + ", " + u + " }");
                                    break;
                                }

                            case 0x0065: { // FX65: Fills V0 to VX (including VX) with values from memory starting at address I. I is increased by 1 for each value written.
                                    byte x = (byte)((opcode & 0x0F00) >> 8);
                                    for (int i = 0; i <= x; i++) {
                                        registers[x] = memory[I];
                                        I++;
                                    }
                                    pc += 2;
                                    Console.WriteLine("Fills V0 to V" + Utils.toHex(x, false) + " with values starting from I = " + Utils.toHex(I - x - 1));
                                    break;
                                }

                            default: {
                                    NotImplementedYet();
                                    Stop();
                                    break;
                                }
                        }

                        break;
                    }

                default: {
                        NotImplementedYet();
                        Stop();
                        break;
                    }
            }

            // Decrease timers
            if (delay_timer > 0) {
                delay_timer--;
            }
            if (sound_timer > 0) {
                sound_timer--;
            }

            if (needRender) {
                Render();
            }
        }

        private void NotImplementedYet() {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Opcode not implemented yet");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void Render() {
            onRenderListener?.Invoke(g, display, displayWidth, displayHeight, zoom);
        }


    }
}