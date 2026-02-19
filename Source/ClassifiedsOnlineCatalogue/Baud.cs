using UnityEngine;
using I386API;

using static I386API.I386API;

namespace ClassifiedsOnlineCatalogue;

public class Baud {
    public Texture2D texture;
    public I386Command command;
    public I386Diskette diskette;

    readonly float[] s = new float[] {
        300, 600, 1200, 2400, 4800, 9600,
    };
 
    public void load() {
        texture = new Texture2D(2048, 2048);
        texture.LoadImage(I386API.Resources.FLOPPY_BAUD);
        texture.name = "FLOPPY_BAUD";

        command = new I386Command(enter, null);
        i386.AddCommand("baud", command);

        diskette = i386.CreateDiskette(new Vector3(-9.9434f, 0.2114929f, 13.99708f), new Vector3(270f, 271.8562f, 0f));
        diskette.LoadExe("baud", 320);
        diskette.SetTexture(texture);
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
