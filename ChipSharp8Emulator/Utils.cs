using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipSharp8Emulator {

    class Utils {

        public static string toHex(Nybble val, bool prefix = true) {
            return toHex(((int)val), prefix);
        }

        public static string toHex(byte val, bool prefix = true) {
            return toHex(((int)val), prefix);
        }

        public static string toHex(ushort val, bool prefix = true) {
            return toHex(((int)val), prefix);
        }

        public static string toHex(int val, bool prefix = true) {
            if (prefix) {
                return "0x" + val.ToString("X");
            } else {
                return val.ToString("X");
            }
        }

    }

}
