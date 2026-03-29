using System.Collections.Generic;
using UnityEngine;

namespace ClassifiedsOnlineCatalogue;

internal class OrderList {
    internal List<OrderPart> parts;
    internal int price;
    internal GameObject gameObject;
    internal string phoneNumber;
    internal OrderType type;
    internal string line0;
    internal string line1;
    internal string line2;
    internal string line3;
    internal int len;

    internal bool ordered => gameObject.name == "0" || gameObject.name == "xx";

    internal OrderList() {
        parts = new List<OrderPart>();
        price = 0;
    }
}
