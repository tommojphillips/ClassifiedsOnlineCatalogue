using UnityEngine;

using static I386PC.I386API;

namespace I386PC;

public class Baud {
    readonly float[] s = new float[] {
        300, 600, 1200, 2400, 4800, 9600,
    };
 
    public void load() {
        Texture2D t1 = new Texture2D(2048, 2048);
        t1.LoadImage(_386PC.Properties.Resources.FLOPPY_BAUD);

        I386Command c1 = new I386Command(enter, null);
        i386.AddCommand("baud", c1);

        I386Diskette d1 = i386.CreateDiskette(new Vector3(-9.808896f, 0.2169641f, 13.98685f), new Vector3(270f, 271f, 0f));
        d1.LoadExe("baud", 320);
        d1.SetTexture(t1);
    }

    private bool enter() {
        if (i386.argv.Length > 1) {
            bool invalid = true;
            if (float.TryParse(i386.argv[1], out float baud)) {
                for (int i = 0; i < s.Length; ++i) {
                    if (s[i] == baud) {
                        i386.baud = baud;
                        i386.POS_WriteNewLine($"baud={i386.baud}");
                        invalid = false;
                        break;
                    }
                }
            }

            if (invalid) {
                i386.POS_WriteNewLine("invalid baud rate");
            }
        }
        else {
            i386.POS_WriteNewLine($"baud={i386.baud}");
        }

        return true; // exit
    }
}
