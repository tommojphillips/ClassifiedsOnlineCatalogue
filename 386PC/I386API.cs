using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace I386;

public class I386API : Mod {
    public override string ID => "I386PC";
    public override string Name => "I386 PC";
    public override string Author => "tommojphillips";
    public override string Version => "1.1";
    public override string Description => "I386 PC";
    public override Game SupportedGames => Game.MyWinterCar;

    private static I386 i386;

    public override void ModSetup() {
        SetupFunction(Setup.OnLoad, Mod_OnLoad);
        SetupFunction(Setup.OnSave, Mod_OnSave);
        SetupFunction(Setup.ModSettings, Mod_Settings);
    }

    private void Mod_Settings() {

    }
    private void Mod_OnLoad() {
        i386 = new I386();

        phone_numbers = GameObject.Find("CARPARTS/PARTSYSTEM/PhoneNumbers").transform;

        GameObject order_spawner = GameObject.Find("CARPARTS/PARTSYSTEM/OrdersSpawnerYP");
        order_spawner_fsm = order_spawner.GetPlayMaker("Spawn");
        current_listing = order_spawner_fsm.GetVariable<FsmGameObject>("CurrentListing");

        GameObject spawners = GameObject.Find("CARPARTS/PARTSYSTEM/PostSystem/VINSpawners");
        vin_spawners = spawners.GetHashTableProxy("Spawners");
        pic_spawners = spawners.GetHashTableProxy("SpawnersPic");

        GameObject order_keywords = GameObject.Find("CARPARTS/PARTSYSTEM/PostSystem/KeywordsEN");
        keywords = order_keywords.GetComponent<PlayMakerHashTableProxy>();

        GameObject magazine = GameObject.Find("Systems/MarkettiMagazine");
        magazine.FsmInject("DayChanger", "Update", onNewMagazine, false, 0);

        if (SaveLoad.ValueExists(this, "downloaded")) {
            if (SaveLoad.ReadValue<bool>(this, "downloaded")) {
                populateOrders(true);
            }
        }

        Texture2D t = new Texture2D(2048, 2048);
        t.LoadImage(_386PC.Properties.Resources.FLOPPY_CAT);
        I386Command c = new I386Command(cat_e, cat_u);
        i386.AddCommand("cat", c);
        I386Diskette d = i386.CreateDiskette(new Vector3(-9.823606f, 0.2121708f, 13.98593f), new Vector3(270f, 271f, 0f));
        d.LoadExe("cat", 320);
        d.SetTexture(t);

        Texture2D t1 = new Texture2D(2048, 2048);
        t1.LoadImage(_386PC.Properties.Resources.FLOPPY_BAUD);
        I386Command c1 = new I386Command(baud_e, null);
        i386.AddCommand("baud", c1);
        I386Diskette d1 = i386.CreateDiskette(new Vector3(-9.808896f, 0.2169641f, 13.98685f), new Vector3(270f, 271f, 0f));
        d1.LoadExe("baud", 320);
        d1.SetTexture(t1);
    }
    private void Mod_OnSave() {
        SaveLoad.WriteValue<bool>(this, "downloaded", ordersState == OrderState.Viewing);
    }

    public static I386 GetInstance() {
        return i386;
    }

    int ordersIndex = 0;
    Transform phone_numbers;
    PlayMakerFSM order_spawner_fsm;
    FsmGameObject current_listing;

    PlayMakerHashTableProxy vin_spawners;
    PlayMakerHashTableProxy pic_spawners;
    PlayMakerHashTableProxy keywords;

    public class OrderList {
        public List<OrderPart> parts;
        public int price;
        public GameObject phoneNumber;

        public bool ordered => phoneNumber.name == "0" || phoneNumber.name == "xx";

        public OrderList() {
            parts = new List<OrderPart>();
            price = 0;
        }
    }
    public class OrderPart {
        public string name;
    }

    public enum OrderState {
        NewMagazine,
        Invalid,
        Downloading,
        Downloaded,
        Viewing,
    }
    public List<OrderList> orders;
    public OrderState ordersState;

    int totalBytesDownloaded;

    Coroutine populateOrdersRoutine;

    private IEnumerator populateOrdersAsync(bool y = false) {
        ordersState = OrderState.Downloading;
        totalBytesDownloaded = 0;
        orders = new List<OrderList>();
        for (int i = 0; i < phone_numbers.childCount; i++) {
            Transform phone_number = phone_numbers.GetChild(i);
            PlayMakerFSM phone_number_gen = phone_number.GetPlayMaker("Generate");
            if (phone_number_gen == null) {
                continue; // not a VIN phone number. probably taxi call or something.
            }

            GameObject vin_list = phone_number_gen.GetVariable<FsmGameObject>("VINLIST").Value;

            PlayMakerArrayListProxy order_list = phone_number.GetComponent<PlayMakerArrayListProxy>();
            PlayMakerArrayListProxy database_list = vin_list.GetComponent<PlayMakerArrayListProxy>();

            OrderList orderList = new OrderList();
            orderList.price = (int)order_list._arrayList[0];
            orderList.phoneNumber = phone_number.gameObject;
            for (int j = 1; j < order_list._arrayList.Count; ++j) {

                OrderPart orderPart = new OrderPart();

                int index = (int)order_list._arrayList[j];
                if (index >= database_list._arrayList.Count) {
                    continue;
                }

                string prefab = (string)database_list._arrayList[index];

                GameObject g = null;
                if (vin_spawners._hashTable.ContainsKey(prefab)) {
                    g = (GameObject)vin_spawners._hashTable[prefab];
                }
                else if (pic_spawners._hashTable.ContainsKey(prefab)) {
                    g = (GameObject)pic_spawners._hashTable[prefab];
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
                    orderPart.name = s1.Value;
                }
                else if (keywords._hashTable.ContainsKey(prefab)) {
                    orderPart.name = keywords._hashTable[prefab].ToString();
                }
                else {
                    orderPart.name = prefab;
                }

                if (!y) {
                    int len = orderPart.name.Length;
                    if (i386.baud <= 600) {
                        while (len > 0) {
                            len--;
                            totalBytesDownloaded += 1;
                            yield return new WaitForSeconds(i386.GetDownloadTime(1));
                        }
                    }
                    else {
                        totalBytesDownloaded += len;
                        yield return new WaitForSeconds(i386.GetDownloadTime(len));
                    }
                }

                orderList.parts.Add(orderPart);
            }
            orders.Add(orderList);
        }

        if (!y) {
            ordersIndex = 0;
            ordersState = OrderState.Downloaded;
        }
        else {
            ordersState = OrderState.Viewing;
        }
        populateOrdersRoutine = null;
    }

    private void populateOrders(bool y = false) {
        if (populateOrdersRoutine == null) {
            populateOrdersRoutine = order_spawner_fsm.StartCoroutine(populateOrdersAsync(y));
        }
    }
    private void spawnOrder(OrderList order) {
        current_listing.Value = order.phoneNumber;
        order_spawner_fsm.SendEvent("SPAWNITEM");
    }
    private void viewHeader() {
        i386.POS_ClearScreen();
        i386.POS_WriteNewLine("\t\t\t\t\t     Classifieds Online Catalogue");
        i386.POS_WriteNewLine("\t\t\t\t\t--------------------------------------");
    }

    private void viewOrder(OrderList order) {
        viewHeader();
        for (int j = 0; j < order.parts.Count; ++j) {
            i386.POS_WriteNewLine($"\t\t\t\t\t++ {order.parts[j].name}");
        }
        i386.POS_WriteNewLine("\t\t\t\t\t-------------------------------------");
        i386.POS_Write($"\t\t\t\t\t\t{(ordersIndex + 1).ToString("00")}/{orders.Count} - ${order.price}");
        if (!order.ordered) {
            i386.POS_Write($" [BUY] - {order.phoneNumber.name}");
        }
        else {
            i386.POS_Write(" [ON ORDER]");
        }
        i386.POS_NewLine();

        if (!order.ordered) {
            if (Input.GetKeyDown(KeyCode.Space)) {
                spawnOrder(order);
            }
        }
    }
    private void viewDownload() {
        viewHeader();
        switch (ordersState) {
            case OrderState.Downloading:
                i386.POS_Write($"\t\t\t\t\t     Downloading.... {orders.Count}/{phone_numbers.childCount} ");
                break;
            case OrderState.Downloaded:
                i386.POS_Write($"\t\t\t\t\t   Download Finished - {orders.Count}/{orders.Count} ");
                break;
        }

        if (totalBytesDownloaded < 1024) {
            i386.POS_Write($"{totalBytesDownloaded}b");
        }
        else {
            i386.POS_Write($"{(totalBytesDownloaded/1024.0f).ToString("0.000")}kb");
        }
        i386.POS_NewLine();

        if (ordersState == OrderState.Downloaded) { 
            i386.POS_Write($"\t\t\t\t\t        Press Space to Continue");
            if (Input.GetKeyDown(KeyCode.Space)) {
                ordersState = OrderState.Viewing;
            }
        }
    }
    private void viewNewMagazine() {
        viewHeader();
        i386.POS_WriteNewLine($"\t\t\t\t\t New magazine available for download"); 
        i386.POS_Write($"\t\t\t\t\t        Press Space to Continue");
        if (Input.GetKeyDown(KeyCode.Space)) {
            ordersState = OrderState.Invalid;
        }
    }

    private bool cat_e() {
        ordersIndex = 0;

        if (ordersState == OrderState.Downloading || ordersState == OrderState.Downloaded) {
            ordersState = OrderState.NewMagazine;
        }
        
        return false; // dont exit 
    }
    private bool cat_u() {
        try {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                ordersIndex = ordersIndex - 1;
                if (ordersIndex < 0) {
                    ordersIndex = orders.Count - 1;
                }
            }
            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                ordersIndex = ordersIndex + 1;
                if (ordersIndex >= orders.Count) {
                    ordersIndex = 0;
                }
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) {
                if (populateOrdersRoutine != null) {
                    i386.commandFsm.StopCoroutine(populateOrdersRoutine);
                    populateOrdersRoutine = null;
                }
                return true; // exit
            }

            switch (ordersState) {
                case OrderState.NewMagazine:
                    viewNewMagazine();
                    break;
                case OrderState.Invalid:
                    populateOrders();
                    break;
                case OrderState.Downloading:
                case OrderState.Downloaded:
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
        ordersState = OrderState.NewMagazine;
    }

    // baud command

    readonly float[] s = new float[] {
        300, 600, 1200, 2400, 4800, 9600,
    };
    private bool baud_e() {
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
