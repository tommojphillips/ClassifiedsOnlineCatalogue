using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using HutongGames.PlayMaker;
using MSCLoader;
using I386API;

namespace ClassifiedsOnlineCatalogue;

internal class Cat {
    int ordersIndex = 0;
    Transform phone_numbers;
    PlayMakerFSM order_spawner_fsm;
    FsmGameObject current_listing;

    PlayMakerHashTableProxy vin_spawners;
    PlayMakerHashTableProxy pic_spawners;
    PlayMakerHashTableProxy keywords;
    PlayMakerArrayListProxy line0;
    PlayMakerArrayListProxy line1;
    PlayMakerArrayListProxy line2;
    PlayMakerArrayListProxy line3;

    public List<OrderList> orders;
    public OrderState ordersState;

    // Traduções (pode ser para qualquer idioma)
    private Dictionary<string, string> translations;
    // Mapa de chaves normalizadas -> tradução (para lookup mais tolerante)
    private Dictionary<string, string> normalizedTranslations;

    int totalBytesDownloaded;
    Coroutine routine;

    bool error;
    bool reconnect;
    bool newCatalogue;
    bool downloaded;

    public void load() {
        phone_numbers = GameObject.Find("CARPARTS/PARTSYSTEM/PhoneNumbers").transform;

        // get order spawner
        GameObject order_spawner = GameObject.Find("CARPARTS/PARTSYSTEM/OrdersSpawnerYP");
        order_spawner_fsm = order_spawner.GetPlayMaker("Spawn");
        current_listing = order_spawner_fsm.GetVariable<FsmGameObject>("CurrentListing");

        // Get box spawner
        GameObject spawners = GameObject.Find("CARPARTS/PARTSYSTEM/PostSystem/VINSpawners");
        vin_spawners = spawners.GetHashTableProxy("Spawners");
        pic_spawners = spawners.GetHashTableProxy("SpawnersPic");

        // Get keywords EN
        GameObject order_keywords = GameObject.Find("CARPARTS/PARTSYSTEM/PostSystem/KeywordsEN");
        keywords = order_keywords.GetHashTableProxy("Partnames");
        line1 = order_keywords.GetArrayListProxy("LinesRandom1");
        line2 = order_keywords.GetArrayListProxy("LinesRandom2");
        line3 = order_keywords.GetArrayListProxy("LinesSelected");

        GameObject g = GameObject.Find("CARPARTS/PARTSYSTEM/PostSystem/VINLIST_TirePics");
        line0 = g.GetArrayListProxy("TextEN");

        // Keywords FI not used

        // Hook magazine update
        GameObject magazine = GameObject.Find("Systems/MarkettiMagazine");
        magazine.FsmInject("DayChanger", "Update", onNewMagazine, false, 0);

        // Inicializar dicionário de traduções vazio; será carregado do arquivo de configuração
        translations = new Dictionary<string, string>();
        try {
            LoadTranslationsFromFile();
        }
        catch (Exception) {
            // silent - critical errors use ModConsole.Error where appropriate
        }

        // Populate orders if marked as downloaded
        if (SaveLoad.ValueExists(ClassifiedsOnlineCatalogue.instance, "downloaded")) {
            downloaded = SaveLoad.ReadValue<bool>(ClassifiedsOnlineCatalogue.instance, "downloaded");
            if (downloaded) {
                populateOrders(true);
            }
        }

        // Load diskette texture
        Texture2D texture = new Texture2D(128, 128);
        texture.LoadImage(Properties.Resources.FLOPPY_CAT);
        texture.name = "FLOPPY_CAT";


        // Create command
        Command.Create("cat", enter, update);


        // Create diskette
        Diskette diskette = Diskette.Create("cat", new Vector3(-9.853718f, 0.2164819f, 13.99311f), new Vector3(275.3004f, 90.23483f, 179.6319f));
        diskette.SetTexture(texture);
        
        error = false;
        newCatalogue = false;
        reconnect = false;
    }
    public void save() {
        SaveLoad.WriteValue(ClassifiedsOnlineCatalogue.instance, "downloaded", downloaded && !newCatalogue);
    }

    private IEnumerator populateOrdersAsync(bool start = false) {
        downloaded = false;
        totalBytesDownloaded = 0;
        orders = new List<OrderList>();

        if (!I386.ModemConnected) {
            ordersState = OrderState.DownloadError;
            routine = null;
            yield break;
        }

        ordersState = OrderState.Downloading;

        // for each listing
        for (int i = 0; i < phone_numbers.childCount; i++) {
            OrderList orderList = new OrderList();
            PlayMakerArrayListProxy order_list;
            PlayMakerArrayListProxy database_list;

            try {
                Transform phone_number = phone_numbers.GetChild(i);
                PlayMakerFSM phone_number_gen = phone_number.GetPlayMaker("Generate");
                if (phone_number_gen == null) {
                    continue; // not a VIN phone number. probably taxi call or something.
                }

                GameObject vin_list = phone_number_gen.GetVariable<FsmGameObject>("VINLIST").Value;

                order_list = phone_number.GetComponent<PlayMakerArrayListProxy>();
                database_list = vin_list.GetComponent<PlayMakerArrayListProxy>();

                int line0Index = -1;
                int line1Index = -1;
                int line2Index = -1;
                int line3Index = -1;

                if (order_list._arrayList.Count == 3) {
                    // PIC
                    orderList.type = OrderType.Wheels;
                    line0Index = 1;
                }
                else if (order_list._arrayList.Count > 8) {
                    // RANDOM
                    orderList.type = OrderType.Random;
                    line1Index = 9;
                    line2Index = 10;
                }
                else {
                    // REGULAR
                    orderList.type = OrderType.Regular;
                    line3Index = 7;
                }

                if (line0Index >= 0 && line0Index < order_list._arrayList.Count) {
                    orderList.line0 = Translate((string)line0._arrayList[(int)order_list._arrayList[line0Index]]);
                }
                else {
                    orderList.line0 = string.Empty;
                }

                if (line1Index >= 0 && line1Index < order_list._arrayList.Count) {
                    orderList.line1 = Translate((string)line1._arrayList[(int)order_list._arrayList[line1Index]]);
                }
                else {
                    orderList.line1 = string.Empty;
                }

                if (line2Index >= 0 && line2Index < order_list._arrayList.Count) {
                    orderList.line2 = Translate((string)line2._arrayList[(int)order_list._arrayList[line2Index]]);
                }
                else {
                    orderList.line2 = string.Empty;
                }

                if (line3Index >= 0 && line3Index < order_list._arrayList.Count) {
                    orderList.line3 = Translate((string)line3._arrayList[(int)order_list._arrayList[line3Index]]);
                }
                else {
                    orderList.line3 = string.Empty;
                }

                orderList.gameObject = phone_number.gameObject;
                orderList.price = phone_number_gen.GetVariable<FsmInt>("PriceInt").Value;
                orderList.phoneNumber = phone_number_gen.GetVariable<FsmString>("Phonenumber").Value;
            }
            catch (Exception e) {
                ModConsole.Error($"[ClassifiedsOnlineCat] Error: {e.Message}: {e.StackTrace}");
                yield break;
            }

            // for each part
            for (int j = 1; j < order_list._arrayList.Count; ++j) {

                OrderPart orderPart = new OrderPart();
                int index = (int)order_list._arrayList[j];
                bool done = false;

                // Different rules for different types of listings
                switch (orderList.type) {
                    case OrderType.Wheels:
                        if (j == 2 /*5*/) {
                            done = true; // done
                        }
                        break;
                    case OrderType.Random:
                        if (index == 9999 || j >= 9) {
                            done = true; // done
                        }
                        break;
                    case OrderType.Regular:
                        if (j == 7) {
                            done = true; // done
                        }
                        break;
                }

                // Sanity check
                if (done || index >= database_list._arrayList.Count) {
                    break;
                }

                string vin_name = (string)database_list._arrayList[index];

                GameObject g = null;                
                switch (orderList.type) {
                    case OrderType.Wheels:
                        if (pic_spawners._hashTable.ContainsKey(vin_name)) {
                            g = (GameObject)pic_spawners._hashTable[vin_name];
                        }
                        break;
                    case OrderType.Random:                       
                    case OrderType.Regular:
                        if (vin_spawners._hashTable.ContainsKey(vin_name)) {
                            g = (GameObject)vin_spawners._hashTable[vin_name];
                        }
                        break;
                }

                if (g != null) {
                    PlayMakerFSM fsm = g.GetPlayMaker("Spawn");
                    if (fsm == null) {
                        ModConsole.Error($"{g.name}: Spawn FSM doesnt exist");
                        continue;
                    }

                    FsmGameObject s = fsm.GetVariable<FsmGameObject>("Prefab");
                    if (s == null) {
                        ModConsole.Error($"{g.name}: Spawn.Prefab FsmGameObject doesnt exist");
                        continue;
                    }

                    PlayMakerFSM fsm1 = s.Value.GetPlayMaker("Data");
                    if (fsm1 == null) {
                        ModConsole.Error($"{g.name}: Data FSM doesnt exist");
                        continue;
                    }

                    FsmString s1 = fsm1.GetVariable<FsmString>("Name");
                    if (s1 == null) {
                        ModConsole.Error($"{g.name}: Data.Name FsmString doesnt exist");
                        continue;
                    }

                    orderPart.partName = s1.Value;
                    // traduz nome da peça, se disponível
                    orderPart.partName = Translate(orderPart.partName);
                }
                else {
                    orderPart.partName = string.Empty;
                }

                if (keywords._hashTable.ContainsKey(vin_name)) {
                    orderPart.keyword = Translate(keywords._hashTable[vin_name].ToString());
                }
                else {
                    orderPart.keyword = string.Empty; 
                }

                orderPart.vinName = vin_name;

                if (!start) {
                    int len = orderPart.partName.Length + orderPart.vinName.Length + orderPart.keyword.Length + orderList.line1.Length + orderList.line2.Length;
                    if (I386.Baud <= 600) {
                        while (len > 0) {

                            if (!I386.ModemConnected) {
                                ordersState = OrderState.DownloadError;
                                routine = null;
                                yield break;
                            }

                            len--;
                            totalBytesDownloaded += 1;
                            yield return new WaitForSeconds(I386.GetDownloadTime(1));
                        }
                    }
                    else {

                        if (!I386.ModemConnected) {
                            ordersState = OrderState.DownloadError;
                            routine = null;
                            yield break;
                        }

                        totalBytesDownloaded += len;
                        yield return new WaitForSeconds(I386.GetDownloadTime(len));
                    }
                }

                orderList.parts.Add(orderPart);
            }
            orders.Add(orderList);
        }
        ordersIndex = 0;
        downloaded = true;
        newCatalogue = false;

        if (!start) {
            ordersState = OrderState.Downloaded;
        }
        else {
            ordersState = OrderState.Viewing;
        }

        routine = null;
    }
    private IEnumerator connectAsync() {

        if (I386.ModemConnected) {
            if (downloaded && !newCatalogue) {
                yield return new WaitForSeconds(0.65f);
                ordersState = OrderState.Viewing;
            }
            else {
                yield return new WaitForSeconds(0.95f);
                ordersState = OrderState.NewMagazine;
            }
        }
        else {
            yield return new WaitForSeconds(1.3f);
            ordersState = OrderState.NotConnected;
        }

        reconnect = false;
        routine = null;
    }

    private void populateOrders(bool y = false) {
        if (routine == null) {
            routine = order_spawner_fsm.StartCoroutine(populateOrdersAsync(y));
        }
    }
    private void connect() {
        if (routine == null) {
            routine = order_spawner_fsm.StartCoroutine(connectAsync());
        }
    }
    private void spawnOrder(OrderList order) {
        current_listing.Value = order.gameObject;
        order_spawner_fsm.SendEvent("SPAWNITEM");
    }
    private void viewHeader() {
        I386.POS_ClearScreen();
        string header = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("Online Classifieds Catalogue") : "Online Classifieds Catalogue";
        I386.POS_WriteNewLine("\t\t\t\t\t   " + header);
        I386.POS_WriteNewLine("\t\t\t\t\t--------------------------------------");
    }

    // Expose translation to other classes
    internal string Localize(string s) {
        return Translate(s);
    }

    // Retorna tradução se existir
    private string Translate(string s) {
        if (string.IsNullOrEmpty(s)) return s;
        // normalizar entrada (usar para buscas tolerantes)
        string normalizedInput = NormalizeKey(s);

        // tentativa direta
        if (translations != null) {
            string t;
            if (translations.TryGetValue(s, out t)) {
                // Direct translation found (no log to avoid noise)
                return t;
            }
        }

        // Normalizar e procurar em mapa normalizado
        if (normalizedTranslations != null) {
            string n = normalizedInput;
            if (!string.IsNullOrEmpty(n)) {
                string tv;
                if (normalizedTranslations.TryGetValue(n, out tv)) {
                    return tv;
                }

                // tentar correspondência por substring (entrada contém chave ou vice-versa)
                foreach (var kv in normalizedTranslations) {
                    if (n.Contains(kv.Key) || kv.Key.Contains(n)) {
                        // do not log successful translations to avoid noise
                        return kv.Value;
                    }
                }
                // fuzzy matching removed — rely on normalized exact and substring matches
            }
        }

        // no translation found (silent)

        return s;
    }

    // Normaliza chaves: remove diacríticos, reduz múltiplos espaços, converte pra lower
    private string NormalizeKey(string s) {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // remover marcas diacríticas
        string formD = s.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char ch in formD) {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        string noDiacritics = sb.ToString();
        // colapsar espaços
        string collapsed = Regex.Replace(noDiacritics, "\\s+", " ").Trim();
        // remover pontuação exceto / (usado em datas) — aceitar letras e números
        string cleaned = Regex.Replace(collapsed, "[^\\p{L}\\p{N}\\/ ]+", "");
        return cleaned.ToLowerInvariant();
    }


	// Carrega traduções a partir de um arquivo JSON simples localizado Mods\Config\Mod Settings\ClassifiedsOnlineCatalogue:
	// { "English text": "Texto em Português", ... }
	private void LoadTranslationsFromFile() {
        string asmLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string dir = Path.GetDirectoryName(asmLocation);
        // procurar arquivo em Mods\Config\Mod Settings\ClassifiedsOnlineCatalogue\translate.json
        string configDir = Path.Combine(Path.Combine(Path.Combine(dir, "Config"), "Mod Settings"), "ClassifiedsOnlineCatalogue");
        string file = Path.Combine(configDir, "translate.json");

        // Attempt to locate translations file; do not spam console on normal load
        if (!File.Exists(file)) {
            try {
                Directory.CreateDirectory(configDir);
            }
            catch {
                // ignore
            }
            WriteDefaultTranslationsFile(file);
            return;
        }

        string json = File.ReadAllText(file);
        // Regex simples para pares "key": "value"
        Regex rx = new Regex("\"(.*?)\"\\s*:\\s*\"(.*?)\"", RegexOptions.Singleline);
        MatchCollection matches = rx.Matches(json);
        int parsed = 0;
        foreach (Match m in matches) {
            try {
                string key = Regex.Unescape(m.Groups[1].Value);
                string val = Regex.Unescape(m.Groups[2].Value);
                if (translations == null) translations = new Dictionary<string, string>();
                translations[key] = val;
                parsed++;
            }
            catch {
                // ignorar entrada inválida
            }
        }

        // parsed silently

        // construir mapa normalizado para buscas tolerantes
        normalizedTranslations = new Dictionary<string, string>();
        if (translations != null) {
            foreach (var kv in translations) {
                string nk = NormalizeKey(kv.Key);
                if (!string.IsNullOrEmpty(nk)) {
                    normalizedTranslations[nk] = kv.Value;
                }
            }
            // loaded silently
        }
    }

    private void WriteDefaultTranslationsFile(string file) {
        try {
            var entries = new List<string>();
            foreach (var kv in translations) {
                string k = kv.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string v = kv.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                entries.Add($"\"{k}\": \"{v}\"");
            }
            string content = "{\n  " + string.Join(",\n  ", entries.ToArray()) + "\n}";
            File.WriteAllText(file, content);
        }
        catch (Exception e) {
            ModConsole.Error($"[ClassifiedsOnlineCatalogue] Could not create translate.json: {e.Message}");
        }
    }

    private void viewOrder(OrderList order) {
        if (I386.GetKeyDown(KeyCode.LeftArrow)) {
            ordersIndex = ordersIndex - 1;
            if (ordersIndex < 0) {
                ordersIndex = orders.Count - 1;
            }
            error = false;
        }
        if (I386.GetKeyDown(KeyCode.RightArrow)) {
            ordersIndex = ordersIndex + 1;
            if (ordersIndex >= orders.Count) {
                ordersIndex = 0;
            }
            error = false;
        }

        if (I386.ModemConnected) {
            error = false;
            if (reconnect) {
                ordersState = OrderState.Connect;
                return;
            }
            if (newCatalogue) {
                ordersState = OrderState.Connect;
                return;
            }
        }
        else {
            reconnect = true;
        }

        viewHeader();

        if (!string.IsNullOrEmpty(order.line0)) {
            I386.POS_WriteNewLine($"\t\t\t\t\t {order.line0}");
        }
        if (!string.IsNullOrEmpty(order.line1)) {
            I386.POS_WriteNewLine($"\t\t\t\t\t {order.line1}");
        }
        if (!string.IsNullOrEmpty(order.line2)) {
            I386.POS_WriteNewLine($"\t\t\t\t\t {order.line2}");
        }
        if (!string.IsNullOrEmpty(order.line3)) {
            I386.POS_WriteNewLine($"\t\t\t\t\t {order.line3}");
        }
        I386.POS_NewLine();

        for (int j = 0; j < order.parts.Count; ++j) {
            I386.POS_WriteNewLine($"\t\t\t\t\t++ {order.parts[j].partName}");
        }
        I386.POS_WriteNewLine("\t\t\t\t\t-------------------------------------");
        I386.POS_Write($"\t\t\t\t\t\t{(ordersIndex + 1).ToString("00")}/{orders.Count} - ${order.price}");
        if (!order.ordered) {

            if (I386.GetKeyDown(KeyCode.Space)) {
                if (I386.ModemConnected) {
                    spawnOrder(order);
                }
                else {
                    error = true;
                }
            }

            if (error) {
                string err = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("[ERROR] - {0}") : "[ERROR] - {0}";
                I386.POS_Write(" " + string.Format(err, order.phoneNumber));
            }
            else {
                string buy = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("[BUY]") : "[BUY]";
                I386.POS_Write($" {buy} - {order.phoneNumber}");
            }
        }
        else {
            string onOrder = ClassifiedsOnlineCatalogue.instance != null ? ClassifiedsOnlineCatalogue.instance.Localize("[ON ORDER]") : "[ON ORDER]";
            I386.POS_Write(" " + onOrder);
        }
        I386.POS_NewLine();
    }
    private void viewDownload() {
        viewHeader();
        switch (ordersState) {
            case OrderState.Downloading:
                I386.POS_Write($"\t\t\t\t\t      " + ClassifiedsOnlineCatalogue.instance.Localize("Downloading....") + $" {orders.Count}/{phone_numbers.childCount} ");
                break;
            case OrderState.Downloaded:
                I386.POS_Write($"\t\t\t\t\t      " + ClassifiedsOnlineCatalogue.instance.Localize("Download complete - "));
                break;
            case OrderState.DownloadError:
                I386.POS_Write($"\t\t\t\t\t             " + ClassifiedsOnlineCatalogue.instance.Localize("Download error"));
                break;
        }

        if (ordersState != OrderState.DownloadError) {
            if (totalBytesDownloaded < 1024) {
                I386.POS_Write($"{totalBytesDownloaded}b");
            }
            else {
                I386.POS_Write($"{(totalBytesDownloaded / 1024.0f).ToString("0.000")}kb");
            }
        }

        I386.POS_NewLine();

        if (ordersState == OrderState.Downloaded) {
            I386.POS_WriteNewLine($"\t\t\t\t\t    " + ClassifiedsOnlineCatalogue.instance.Localize("Press Space to Continue"));
            if (I386.GetKeyDown(KeyCode.Space)) {
                ordersState = OrderState.Viewing;
            }
        }

        if (ordersState == OrderState.DownloadError) {
            I386.POS_WriteNewLine($"\t\t\t\t\t         " + ClassifiedsOnlineCatalogue.instance.Localize("Press Space to Try Again"));
            if (I386.GetKeyDown(KeyCode.Space)) {
                ordersState = OrderState.Connect;
            }
        }
    }
    private void viewNewMagazine() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t " + ClassifiedsOnlineCatalogue.instance.Localize("New issue available for download"));
        I386.POS_WriteNewLine($"\t\t\t\t\t     " + ClassifiedsOnlineCatalogue.instance.Localize("Press Space to Download"));
        if (I386.GetKeyDown(KeyCode.Space)) {
            ordersState = OrderState.Invalid;
        }
    }
    private void viewNotConnected() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t            " + ClassifiedsOnlineCatalogue.instance.Localize("Not connected"));
        I386.POS_WriteNewLine($"\t\t\t\t\t        " + ClassifiedsOnlineCatalogue.instance.Localize("Press Space to Connect"));
        if (I386.GetKeyDown(KeyCode.Space)) {
            ordersState = OrderState.Connect;
        }
    }
    private void viewConnect() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t             " + ClassifiedsOnlineCatalogue.instance.Localize("Connecting..."));
        connect();
    }

    private bool enter() {
        ordersIndex = 0;
        ordersState = OrderState.Connect;
        return false; // dont exit 
    }
    private bool update() {
        try {
            if (I386.GetKey(KeyCode.LeftControl) && I386.GetKeyDown(KeyCode.C)) {
                if (routine != null) {
                    I386.StopCoroutine(routine);
                    routine = null;
                }
                return true; // exit
            }

            switch (ordersState) {
                case OrderState.Connect:
                    viewConnect();
                    break;
                case OrderState.NotConnected:
                    viewNotConnected();
                    break;
                case OrderState.NewMagazine:
                    viewNewMagazine();
                    break;
                case OrderState.Invalid:
                    populateOrders();
                    break;
                case OrderState.Downloading:
                case OrderState.Downloaded:
                case OrderState.DownloadError:
                    viewDownload();
                    break;
                case OrderState.Viewing:
                    viewOrder(orders[ordersIndex]);
                    break;
            }
        }
        catch (Exception e) {
            ModConsole.Print($"{e.Message}: {e.StackTrace}");
        }

        return false; // dont exit
    }

    private void onNewMagazine() {
        newCatalogue = true;
        if (I386.ModemConnected) {
            ordersState = OrderState.NewMagazine;
        }
    }
}
