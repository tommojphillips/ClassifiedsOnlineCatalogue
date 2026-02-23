using UnityEngine;
using MSCLoader;
using I386API;

namespace ClassifiedsOnlineCatalogue;

internal class Del {
    internal void load() {
        Texture2D texture = new Texture2D(128, 128);
        texture.LoadImage(Properties.Resources.FLOPPY_DEL);
        texture.name = "FLOPPY_DEL";

        Command command = Command.Create("del", enter, null);

        Diskette diskette = Diskette.Create("del", new Vector3(-9.900195f, 0.2155392f, 13.99451f), new Vector3(274.0774f, 90.10452f, 180.4562f));
        diskette.SetTexture(texture);
    }

    private bool enter() {
        bool invalid = true;

        if (I386.Args.Length > 1) {
            GameObject g = GameObject.Find("COMPUTER/Memory");
            PlayMakerArrayListProxy c_drive = g.GetArrayListProxy("C");
            invalid = !c_drive.Remove(I386.Args[1], "", true);
        }
        
        if (invalid) {
            string msg = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("File not found") : "File not found";
            I386.POS_WriteNewLine(msg);
        }
        else {
            string tpl = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("Deleted file '{0}'") : "Deleted file '{0}'";
            I386.POS_WriteNewLine(string.Format(tpl, I386.Args[1]));
        }

        return true; // exit
    }
}
