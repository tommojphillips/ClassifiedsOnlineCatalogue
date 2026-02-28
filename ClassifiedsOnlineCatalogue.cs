using MSCLoader;

namespace ClassifiedsOnlineCatalogue;

public class ClassifiedsOnlineCatalogue : Mod {
    public override string ID => "ClassifiedsOnlineCatalogue";
    public override string Name => "Classifieds Online Catalogue";
    public override string Author => "tommojphillips";
    public override string Version => "1.4.1";
    public override string Description => "Order used car parts online!";
    public override Game SupportedGames => Game.MyWinterCar;

    internal static ClassifiedsOnlineCatalogue instance;

    private Cat cat;
    private Baud baud;
    private Del del;

    public override void ModSetup() {
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.OnSave, Mod_OnSave);
    }
    private void Mod_OnLoad() {
        if (!ModLoader.IsModPresent("I386API")) {
            ModConsole.Error("[ClassifiedsOnlineCatalogue] I386 API required!");
            ModUI.ShowMessage("I386 API not installed.\nI386 API required!", Name);
            return;
        }
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
}
