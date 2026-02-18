using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using MSCLoader;

using static I386PC.I386API;

namespace I386PC;

public class OrderList {
    public List<OrderPart> parts;
    public int price;
    public GameObject gameObject;
    public string phoneNumber;
    public OrderType type;

    public bool ordered => gameObject.name == "0" || gameObject.name == "xx";

    public OrderList() {
        parts = new List<OrderPart>();
        price = 0;
    }
}
public class OrderPart {
    public string partName;
    public string vinName;
    public string keyword;
}

public enum OrderState {
    Connect,       // -> NotConnected, Viewing or NewMagazine
    NotConnected,  // -> Connect
    NewMagazine,   // -> Invalid
    Invalid,       // -> Downloading
    Downloading,   // -> Downloaded
    Downloaded,    // -> Viewing
    DownloadError, // -> Connect
    Viewing,       // -> NewMagazine, Connect
}

public enum OrderType {
    Wheels,
    Random,
    Regular,
}

public class Cat {
    int ordersIndex = 0;
    Transform phone_numbers;
    PlayMakerFSM order_spawner_fsm;
    FsmGameObject current_listing;

    PlayMakerHashTableProxy vin_spawners;
    PlayMakerHashTableProxy pic_spawners;
    PlayMakerHashTableProxy keywords;

    public List<OrderList> orders;
    public OrderState ordersState;

    int totalBytesDownloaded;
    Coroutine routine;

    bool error;
    bool reconnect;
    bool newCatalogue;
    bool downloaded;

    public void load() {
        I386 i386 = I386API.GetInstance();

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

        if (SaveLoad.ValueExists(instance, "downloaded")) {
            downloaded = SaveLoad.ReadValue<bool>(instance, "downloaded");
            if (downloaded) {
                populateOrders(true);
            }
        }

        Texture2D t = new Texture2D(2048, 2048);
        t.LoadImage(_386PC.Properties.Resources.FLOPPY_CAT);
        I386Command c = new I386Command(enter, update);
        i386.AddCommand("cat", c);
        I386Diskette d = i386.CreateDiskette(new Vector3(-9.823606f, 0.2121708f, 13.98593f), new Vector3(270f, 271f, 0f));
        d.LoadExe("cat", 320);
        d.SetTexture(t);
        
        error = false;
        newCatalogue = false;
        reconnect = false;
    }
    public void save() {
        SaveLoad.WriteValue<bool>(instance, "downloaded", downloaded && !newCatalogue);
    }

    private IEnumerator populateOrdersAsync(bool start = false) {
        downloaded = false;
        totalBytesDownloaded = 0;
        orders = new List<OrderList>();

        if (!i386.modemConnected) {
            ordersState = OrderState.DownloadError;
            routine = null;
            yield break;
        }

        ordersState = OrderState.Downloading;

        // for each listing
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

            if (order_list._arrayList.Count == 3) {
                // PIC
                orderList.type = OrderType.Wheels;
            }
            else if (order_list._arrayList.Count > 8) {
                // RANDOM
                orderList.type = OrderType.Random;
            }
            else {
                // REGULAR
                orderList.type = OrderType.Regular;
            }

            orderList.price = (int)order_list._arrayList[0];
            orderList.gameObject = phone_number.gameObject;
            orderList.phoneNumber = phone_number.gameObject.name;

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
                }
                else {
                    orderPart.partName = string.Empty;
                }

                if (keywords._hashTable.ContainsKey(vin_name)) {
                    orderPart.keyword = keywords._hashTable[vin_name].ToString();
                }
                else {
                    orderPart.keyword = string.Empty; 
                }

                orderPart.vinName = vin_name;

                if (!start) {
                    int len = orderPart.partName.Length + orderPart.vinName.Length + orderPart.keyword.Length;
                    if (i386.baud <= 600) {
                        while (len > 0) {

                            if (!i386.modemConnected) {
                                ordersState = OrderState.DownloadError;
                                routine = null;
                                yield break;
                            }

                            len--;
                            totalBytesDownloaded += 1;
                            yield return new WaitForSeconds(i386.GetDownloadTime(1));
                        }
                    }
                    else {

                        if (!i386.modemConnected) {
                            ordersState = OrderState.DownloadError;
                            routine = null;
                            yield break;
                        }

                        totalBytesDownloaded += len;
                        yield return new WaitForSeconds(i386.GetDownloadTime(len));
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

        if (i386.modemConnected) {
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
        i386.POS_ClearScreen();
        i386.POS_WriteNewLine("\t\t\t\t\t     Classifieds Online Catalogue");
        i386.POS_WriteNewLine("\t\t\t\t\t--------------------------------------");
    }

    private void viewOrder(OrderList order) {
        if (i386.GetKeyDown(KeyCode.LeftArrow)) {
            ordersIndex = ordersIndex - 1;
            if (ordersIndex < 0) {
                ordersIndex = orders.Count - 1;
            }
            error = false;
        }
        if (i386.GetKeyDown(KeyCode.RightArrow)) {
            ordersIndex = ordersIndex + 1;
            if (ordersIndex >= orders.Count) {
                ordersIndex = 0;
            }
            error = false;
        }

        if (i386.modemConnected) {
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
        for (int j = 0; j < order.parts.Count; ++j) {
            i386.POS_WriteNewLine($"\t\t\t\t\t++ {order.parts[j].partName}");
        }
        i386.POS_WriteNewLine("\t\t\t\t\t-------------------------------------");
        i386.POS_Write($"\t\t\t\t\t\t{(ordersIndex + 1).ToString("00")}/{orders.Count} - ${order.price}");
        if (!order.ordered) {

            if (i386.GetKeyDown(KeyCode.Space)) {
                if (i386.modemConnected) {
                    spawnOrder(order);
                }
                else {
                    error = true;
                }
            }

            if (error) {
                i386.POS_Write($" [ERROR] - {order.phoneNumber}");
            }
            else {
                i386.POS_Write($" [BUY] - {order.phoneNumber}");
            }
        }
        else {
            i386.POS_Write(" [ON ORDER]");
        }
        i386.POS_NewLine();
    }
    private void viewDownload() {
        viewHeader();
        switch (ordersState) {
            case OrderState.Downloading:
                i386.POS_Write($"\t\t\t\t\t     Downloading.... {orders.Count}/{phone_numbers.childCount} ");
                break;
            case OrderState.Downloaded:
                i386.POS_Write($"\t\t\t\t\t      Download Finished - ");
                break;
            case OrderState.DownloadError:
                i386.POS_Write($"\t\t\t\t\t             Download Error");
                break;
        }

        if (ordersState != OrderState.DownloadError) {
            if (totalBytesDownloaded < 1024) {
                i386.POS_Write($"{totalBytesDownloaded}b");
            }
            else {
                i386.POS_Write($"{(totalBytesDownloaded / 1024.0f).ToString("0.000")}kb");
            }
        }

        i386.POS_NewLine();

        if (ordersState == OrderState.Downloaded) {
            i386.POS_WriteNewLine($"\t\t\t\t\t        Press Space to Continue");
            if (i386.GetKeyDown(KeyCode.Space)) {
                ordersState = OrderState.Viewing;
            }
        }

        if (ordersState == OrderState.DownloadError) {
            i386.POS_WriteNewLine($"\t\t\t\t\t         Press Space to Retry");
            if (i386.GetKeyDown(KeyCode.Space)) {
                ordersState = OrderState.Connect;
            }
        }
    }
    private void viewNewMagazine() {
        viewHeader();
        i386.POS_WriteNewLine($"\t\t\t\t\t New magazine available for download");
        i386.POS_WriteNewLine($"\t\t\t\t\t       Press Space to Download");
        if (i386.GetKeyDown(KeyCode.Space)) {
            ordersState = OrderState.Invalid;
        }
    }
    private void viewNotConnected() {
        viewHeader();
        i386.POS_WriteNewLine($"\t\t\t\t\t            Not Connected");
        i386.POS_WriteNewLine($"\t\t\t\t\t        Press Space to Connect");
        if (i386.GetKeyDown(KeyCode.Space)) {
            ordersState = OrderState.Connect;
        }
    }
    private void viewConnect() {
        viewHeader();
        i386.POS_WriteNewLine($"\t\t\t\t\t             Connecting...");
        connect();
    }

    private bool enter() {
        ordersIndex = 0;
        ordersState = OrderState.Connect;
        return false; // dont exit 
    }
    private bool update() {
        try {
            if (i386.GetKey(KeyCode.LeftControl) && i386.GetKeyDown(KeyCode.C)) {
                if (routine != null) {
                    i386.commandFsm.StopCoroutine(routine);
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
        if (i386.modemConnected) {
            ordersState = OrderState.NewMagazine;
        }
    }
}
