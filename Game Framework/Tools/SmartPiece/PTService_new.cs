using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartPieceLocation
{
    string id;
    string bank;
    int index;
    public SmartPieceLocation(string _id, string _bank, int _index)
    {
        id = _id;
        bank = _bank;
        index = _index;
    }
}

public enum Bank { A, B, C, D, E, F };

public class PTService_new : MonoBehaviour
{
    public static PTService_new Instance { get; private set; }
    private static string fullClassName = "com.blokparty.ptserviceproxy.PTServiceProxy";
    AndroidJavaObject proxyObject = null;
    string id = "null";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
        proxyObject = CreateProxyObject();
    }

    AndroidJavaObject CreateProxyObject()
    {
        var actClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var context = actClass.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass pluginClass = new AndroidJavaClass(fullClassName);
        if (pluginClass != null)
        {
            return pluginClass.CallStatic<AndroidJavaObject>("createInstance", context);
        }
        else
        {
            return null;
        }
    }

    public void ShowControlButtonMenu()
    {
        if (proxyObject == null)
        {
            return;
        }
        proxyObject.Call("showMenu");
    }

    public void ResetNfc()
    {
        if (proxyObject == null)
        {
            return;
        }
        Debug.Log("Calling resetNFC in PTService");
        proxyObject.Call("resetNFC");
    }

    public void GpioInitialize()
    {
        if (proxyObject == null)
        {
            return;
        }
        Debug.Log("Calling gpioInitialize in PTService");
        proxyObject.Call("gpioInitialize");
    }

    /// <summary>
    /// Scan using all antennas
    /// </summary>
    public List<SmartPieceLocation> AutoScan()
    {
        if (proxyObject == null)
        {
            return null;
        }
        AndroidJavaObject[] scanResults = proxyObject.Call<AndroidJavaObject[]>("autoScan");
        if (scanResults.Length > 0)
        {
            List<SmartPieceLocation> spList = new List<SmartPieceLocation>();
            for (int i = 0; i < scanResults.Length; i++)
            {
                string id = scanResults[i].Get<string>("id");
                AndroidJavaObject loc = scanResults[i].Get<AndroidJavaObject>("location");
                string bank = loc.Get<string>("first");
                int index = int.Parse(loc.Get<string>("second"));
                spList.Add(new SmartPieceLocation(id, bank, index));
                Debug.Log("SM #" + (i + 1) + " with id: " + id + "at: " + bank + " " + index);
            }
            return spList;
        }
        else
        {
            Debug.Log("No smart piece found");
            return null;
        }
    }

    /// <summary>
    /// Scan by screen space point
    /// </summary>
    /// <param name="x">left to right, 0 to 1919</param>
    /// <param name="y">top to bottom, 0 to 1079</param>
    public string Scan(int x, int y)
    {
        if (proxyObject == null)
        {
            return id;
        }
        AndroidJavaObject scanResult = proxyObject.Call<AndroidJavaObject>("scan", x, y);
        if (scanResult == null)
        {
            id = "null";
            Debug.Log("No smart piece found at (" + x + "," + y + ")");
        }
        else
        {
            id = scanResult.Get<string>("id");
            AndroidJavaObject loc = scanResult.Get<AndroidJavaObject>("location");
            string bank = loc.Get<string>("first");
            string index = loc.Get<string>("second");
            Debug.Log("Found smart piece at (x,y):" + x + "," + y + " with id: " + id + " at :" + bank + " " + index);
        }
        return id;
    }

    /// <summary>
    /// Scan by specific antenna
    /// </summary>
    /// <param name="bank">left to right, "A" to "F"</param>
    /// <param name="index">top to bottom, 0 to 5, or 6 to 11</param>
    public string Scan(string bank, int index)
    {
        if (proxyObject == null)
        {
            return id;
        }
        AndroidJavaObject scanResult = proxyObject.Call<AndroidJavaObject>("scanBlock", bank, index);
        if (scanResult == null)
        {
            id = "null";
            Debug.Log("No smart piece found at (" + bank + "," + index + ")");
        }
        else
        {
            id = scanResult.Get<string>("id");
            AndroidJavaObject loc = scanResult.Get<AndroidJavaObject>("location");
            Debug.Log("Found smart piece with id: " + id + " at :" + bank + " " + index);
        }
        return id;
    }

    public void enableMenu(int flag)
    {
        if (proxyObject == null)
        {
            return;
        }
        proxyObject.Call("enableMenu", flag);

    }

    private AndroidJavaObject javaArrayFromCS(string[] values)
    {
        AndroidJavaClass arrayClass = new AndroidJavaClass("java.lang.reflect.Array");
        AndroidJavaObject arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance", new AndroidJavaClass("java.lang.String"), values.Length);
        for (int i = 0; i < values.Length; ++i)
        {
            arrayClass.CallStatic("set", arrayObject, i, new AndroidJavaObject("java.lang.String", values[i]));
        }

        return arrayObject;
    }
}
