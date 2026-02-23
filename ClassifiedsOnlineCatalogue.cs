using MSCLoader;

namespace ClassifiedsOnlineCatalogue;

public class ClassifiedsOnlineCatalogue : Mod {
    public override string ID => "ClassifiedsOnlineCatalogue";
    public override string Name => "Classifieds Online Catalogue";
    public override string Author => "tommojphillips";
    public override string Version => "1.5";
    public override string Description => "Order used car parts online!";
    public override Game SupportedGames => Game.MyWinterCar;

    internal static ClassifiedsOnlineCatalogue instance;

    private Cat cat;
    private Baud baud;
    private Del del;

    public override void ModSetup() {
        // minimal startup logs
        if (!ModLoader.IsModPresent("I386API")) {
            ModConsole.Error("[ClassifiedsOnlineCatalogue] I386 API required!");
            ModUI.ShowMessage("I386 API not installed.\nI386 API required!", Name);
            return;
        }

        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.OnSave, Mod_OnSave);
    }
    private void Mod_OnLoad() {
        instance = this;
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

    // Helper to expose translation to other classes
    public string Localize(string s) {
        return cat != null ? cat.Localize(s) : s;
    }
}
