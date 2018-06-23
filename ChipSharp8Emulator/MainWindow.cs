﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChipSharp8Emulator {
    public partial class MainWindow : Form {
        public MainWindow() {
            InitializeComponent();
        }

        public Chip8 chip8;

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e) {
            chip8.Stop();
        }
    }
}
