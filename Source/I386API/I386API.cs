using MSCLoader;
using ClassifiedsOnlineCatalogue;

namespace I386API;

public class I386API : Mod {
    public override string ID => "I386PC";
    public override string Name => "I386 PC";
    public override string Author => "tommojphillips";
    public override string Version => "1.3";
    public override string Description => "I386 PC";
    public override Game SupportedGames => Game.MyWinterCar;

    public static I386API instance { get; private set; }
    public static I386 i386 { get; private set; }

    private Cat cat;
    private Baud baud;
    private Del del;

    public override void ModSetup() {
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.OnSave, Mod_OnSave);
        SetupFunction(Setup.ModSettings, Mod_Settings);
    }

    private void Mod_Settings() {

    }
    private void Mod_OnLoad() {
        instance = this;
        i386 = new I386();

        cat = new Cat();
        cat.load();

        baud = new Baud();
        baud.load();

        del = new Del();
        del.load();
    }
    private void Mod_OnSave() {
        cat.save();
    }

    public static I386 GetInstance() {
        return i386;
    }
}
