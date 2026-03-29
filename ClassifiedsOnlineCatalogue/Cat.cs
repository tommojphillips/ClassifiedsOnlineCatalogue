using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;
using MSCLoader;
using I386API;

namespace ClassifiedsOnlineCatalogue;

internal class Cat {
    private List<OrderList> orders;

    private int index = 0;
    private OrderState state;
    private Coroutine routine;

    private Transform phone_numbers;
    private PlayMakerFSM order_spawner_fsm;
    private FsmGameObject current_listing;

    private PlayMakerHashTableProxy vin_spawners;
    private PlayMakerHashTableProxy pic_spawners;
    private PlayMakerHashTableProxy keywords;
    private PlayMakerArrayListProxy line0;
    private PlayMakerArrayListProxy line1;
    private PlayMakerArrayListProxy line2;
    private PlayMakerArrayListProxy line3;

    private int totalBytesDownloaded;

    private bool error;
    private bool reconnect;
    private bool newCatalogue;
    private bool downloaded;

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

        // Hook magazine update
        GameObject magazine = GameObject.Find("Systems/MarkettiMagazine");
        magazine.FsmInject("DayChanger", "Update", onNewMagazine, false, 0);

        orders = new List<OrderList>();

        // Populate orders if marked as downloaded
        if (SaveLoad.ValueExists(ClassifiedsOnlineCatalogue.instance, "downloaded")) {
            downloaded = SaveLoad.ReadValue<bool>(ClassifiedsOnlineCatalogue.instance, "downloaded");
            if (downloaded) {
                populateOrders();
            }
        }

        // Load diskette texture
        Texture2D texture = new Texture2D(128, 128);
        texture.LoadImage(Properties.Resources.FLOPPY_CAT);
        texture.name = "FLOPPY_CAT";

        // Create command
        Command command = Command.Create("cat", enter, update);

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

    private OrderList downloadOrder(Transform phone_number) {
        OrderList orderList = new OrderList();
        PlayMakerArrayListProxy order_list;
        PlayMakerArrayListProxy database_list;

        try {
            PlayMakerFSM phone_number_gen = phone_number.GetPlayMaker("Generate");
            if (phone_number_gen == null) {
                return null; // not a VIN phone number. probably taxi call or something.
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
                orderList.line0 = (string)line0._arrayList[(int)order_list._arrayList[line0Index]];
            }
            else {
                orderList.line0 = string.Empty;
            }

            if (line1Index >= 0 && line1Index < order_list._arrayList.Count) {
                orderList.line1 = (string)line1._arrayList[(int)order_list._arrayList[line1Index]];
            }
            else {
                orderList.line1 = string.Empty;
            }

            if (line2Index >= 0 && line2Index < order_list._arrayList.Count) {
                orderList.line2 = (string)line2._arrayList[(int)order_list._arrayList[line2Index]];
            }
            else {
                orderList.line2 = string.Empty;
            }

            if (line3Index >= 0 && line3Index < order_list._arrayList.Count) {
                orderList.line3 = (string)line3._arrayList[(int)order_list._arrayList[line3Index]];
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
            return null;
        }

        for (int j = 1; j < order_list._arrayList.Count; ++j) {
            OrderPart orderPart = new OrderPart();
            int index = (int)order_list._arrayList[j];
            bool done = false;

            // Different rules for different types of listings
            switch (orderList.type) {
                case OrderType.Wheels:
                    if (j == 2) {
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
            orderList.len += orderPart.partName.Length + orderPart.vinName.Length + orderPart.keyword.Length + orderList.line1.Length + orderList.line2.Length;            
            orderList.parts.Add(orderPart);
        }
        return orderList;
    }

    private IEnumerator populateOrdersAsync() {
        downloaded = false;
        totalBytesDownloaded = 0;
        orders.Clear();

        if (!I386.ModemConnected) {
            state = OrderState.DownloadError;
            routine = null;
            yield break;
        }

        state = OrderState.Downloading;

        for (int i = 0; i < phone_numbers.childCount; i++) {
            OrderList list = downloadOrder(phone_numbers.GetChild(i));
            if (list == null) {
                continue;
            }

            if (!I386.ModemConnected) {
                state = OrderState.DownloadError;
                routine = null;
                yield break;
            }

            totalBytesDownloaded += list.len;
            yield return new WaitForSeconds(I386.GetDownloadTime(list.len));

            orders.Add(list);
        }

        index = 0;
        downloaded = true;
        newCatalogue = false;
        state = OrderState.Downloaded;
        routine = null;
    }
    private IEnumerator connectAsync() {

        if (I386.ModemConnected) {
            if (downloaded && !newCatalogue) {
                yield return new WaitForSeconds(0.65f);
                state = OrderState.Viewing;
            }
            else {
                yield return new WaitForSeconds(0.95f);
                state = OrderState.NewMagazine;
            }
        }
        else {
            yield return new WaitForSeconds(1.3f);
            state = OrderState.NotConnected;
        }

        reconnect = false;
        routine = null;
    }

    private void populateOrders() {
        orders.Clear();
        for (int i = 0; i < phone_numbers.childCount; i++) {
            OrderList list = downloadOrder(phone_numbers.GetChild(i)); 
            if (list == null) {
                continue;
            }
            orders.Add(list);
        }

        index = 0;
        downloaded = true;
        newCatalogue = false;
        state = OrderState.Connect;
        routine = null;
    }    
    private void spawnOrder(OrderList order) {
        current_listing.Value = order.gameObject;
        order_spawner_fsm.SendEvent("SPAWNITEM");
    }
    private void viewHeader() {
        I386.POS_ClearScreen();
        I386.POS_WriteNewLine("\t\t\t\t\t     Classifieds Online Catalogue");
        I386.POS_WriteNewLine("\t\t\t\t\t--------------------------------------");
    }

    private void viewOrder(OrderList order) {
        if (I386.GetKeyDown(KeyCode.LeftArrow)) {
            index = index - 1;
            if (index < 0) {
                index = orders.Count - 1;
            }
            error = false;
        }
        if (I386.GetKeyDown(KeyCode.RightArrow)) {
            index = index + 1;
            if (index >= orders.Count) {
                index = 0;
            }
            error = false;
        }

        if (I386.ModemConnected) {
            error = false;
            if (reconnect) {
                state = OrderState.Connect;
                return;
            }
            if (newCatalogue) {
                state = OrderState.Connect;
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
        I386.POS_Write($"\t\t\t\t\t\t{(index + 1).ToString("00")}/{orders.Count} - ${order.price}");
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
                I386.POS_Write($" [ERROR] - {order.phoneNumber}");
            }
            else {
                I386.POS_Write($" [BUY] - {order.phoneNumber}");
            }
        }
        else {
            I386.POS_Write(" [ON ORDER]");
        }
        I386.POS_NewLine();
    }
    private void viewDownload() {
        viewHeader();
        switch (state) {
            case OrderState.Downloading:
                I386.POS_Write($"\t\t\t\t\t     Downloading.... {orders.Count}/{phone_numbers.childCount} ");
                break;
            case OrderState.Downloaded:
                I386.POS_Write($"\t\t\t\t\t      Download Finished - ");
                break;
            case OrderState.DownloadError:
                I386.POS_Write($"\t\t\t\t\t             Download Error");
                break;
        }

        if (state != OrderState.DownloadError) {
            if (totalBytesDownloaded < 1024) {
                I386.POS_Write($"{totalBytesDownloaded}b");
            }
            else {
                I386.POS_Write($"{(totalBytesDownloaded / 1024.0f).ToString("0.000")}kb");
            }
        }

        I386.POS_NewLine();

        if (state == OrderState.Downloaded) {
            I386.POS_WriteNewLine($"\t\t\t\t\t        Press Space to Continue");
            if (I386.GetKeyDown(KeyCode.Space)) {
                state = OrderState.Viewing;
            }
        }

        if (state == OrderState.DownloadError) {
            I386.POS_WriteNewLine($"\t\t\t\t\t         Press Space to Retry");
            if (I386.GetKeyDown(KeyCode.Space)) {
                state = OrderState.Connect;
            }
        }
    }
    private void viewNewMagazine() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t New magazine available for download");
        I386.POS_WriteNewLine($"\t\t\t\t\t       Press Space to Download");
        if (I386.GetKeyDown(KeyCode.Space)) {
            state = OrderState.Invalid;
        }
    }
    private void viewNotConnected() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t            Not Connected");
        I386.POS_WriteNewLine($"\t\t\t\t\t        Press Space to Connect");
        if (I386.GetKeyDown(KeyCode.Space)) {
            state = OrderState.Connect;
        }
    }
    private void viewConnect() {
        viewHeader();
        I386.POS_WriteNewLine($"\t\t\t\t\t             Connecting..."); 
        if (routine == null) {
            routine = I386.StartCoroutine(connectAsync());
        }
    }
    private void viewInvalid() {
        if (routine == null) {
            routine = I386.StartCoroutine(populateOrdersAsync());
        }
    }
    private bool enter() {
        index = 0;
        state = OrderState.Connect;
        routine = null;
        error = false;
        reconnect = false;
        return false; // dont exit 
    }
    private bool update() {
        try {
            if (I386.GetKey(KeyCode.LeftControl) && I386.GetKeyDown(KeyCode.C)) {
                return true; // exit
            }

            switch (state) {
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
                    viewInvalid();
                    break;
                case OrderState.Downloading:
                case OrderState.Downloaded:
                case OrderState.DownloadError:
                    viewDownload();
                    break;
                case OrderState.Viewing:
                    viewOrder(orders[index]);
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
            state = OrderState.NewMagazine;
        }
    }
}
