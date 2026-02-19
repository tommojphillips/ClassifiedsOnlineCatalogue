using UnityEngine;
using MSCLoader;
using I386API;

using static I386API.I386API;

namespace ClassifiedsOnlineCatalogue;

public class Del {
    public Texture2D texture;
    public I386Command command;
    public I386Diskette diskette;

    public void load() {
        texture = new Texture2D(2048, 2048);
        texture.LoadImage(I386API.Resources.FLOPPY_DEL);
        texture.name = "FLOPPY_DEL";

        command = new I386Command(enter, null);
        i386.AddCommand("del", command);
        
        diskette = i386.CreateDiskette(new Vector3(-9.900195f, 0.2155392f, 13.99451f), new Vector3(274.0774f, 90.10452f, 180.4562f));
        diskette.LoadExe("del", 320);
        diskette.SetTexture(texture);
    }

    private bool enter() {
        bool invalid = true;

        if (i386.argv.Length > 1) {
            GameObject g = GameObject.Find("COMPUTER/Memory");
            PlayMakerArrayListProxy c_drive = g.GetArrayListProxy("C");
            invalid = !c_drive.Remove(i386.argv[1], "", true);
        }
        
        if (invalid) {
            i386.POS_WriteNewLine("Could not find file");
        }
        else {
            i386.POS_WriteNewLine($"Deleted {i386.argv[1]}");
        }

        return true; // exit
    }
}
