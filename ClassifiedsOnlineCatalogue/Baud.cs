using UnityEngine;
using I386API;

namespace ClassifiedsOnlineCatalogue;

internal class Baud {

    internal readonly float[] s = new float[] {
        300, 600, 1200, 2400, 4800, 9600,
    };

    internal void load() {
        Texture2D texture = new Texture2D(128, 128);
        texture.LoadImage(Properties.Resources.FLOPPY_BAUD);
        texture.name = "FLOPPY_BAUD";

        Command command = Command.Create("baud", enter, null);

        Diskette diskette = Diskette.Create("baud", new Vector3(-9.9434f, 0.2114929f, 13.99708f), new Vector3(270f, 271.8562f, 0f));
        diskette.SetTexture(texture);
    }

    private bool enter() {
        if (I386.Args.Length > 1) {
            bool invalid = true;
            if (float.TryParse(I386.Args[1], out float baud)) {
                for (int i = 0; i < s.Length; ++i) {
                    if (s[i] == baud) {
                        I386.Baud = baud;
                        I386.POS_WriteNewLine($"baud={I386.Baud}");
                        invalid = false;
                        break;
                    }
                }
            }

            if (invalid) {
                string msg = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("Invalid baud rate") : "Invalid baud rate";
                I386.POS_WriteNewLine(msg);
            }
        }
        else {
            I386.POS_WriteNewLine($"baud={I386.Baud}");
        }

        return true; // exit
    }
}
