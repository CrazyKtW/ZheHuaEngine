﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using XLua;

public class XLuaComponent : MonoBehaviour
{
    private static XLuaComponent _instance;
    public static XLuaComponent instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("Without XLuaComponent instance");
                return null;
            }
            else
            {
                return _instance;
            }
        }
    }

    private LuaEnv _luaEnv;//Lua 环境
    public LuaEnv luaEnv
    {
        get { return _luaEnv; }
    }

    void Awake()
    {
        _luaEnv = new LuaEnv();
        _luaEnv.AddLoader(LuaLoader);
        Debug.Log("XLuaComponent Awake");
        _instance = this;
    }

    bool inited = false;
    public void Launch()
    {
        _luaEnv.DoString("require 'Main'");
        inited = true;
    }

    public void LoadLuaBundleAndLaunch()
    {
        Debug.LogError(LuaConst.LuaBundlePath);
        StartCoroutine(LoadBundleLuaFiles(LuaConst.LuaBundlePath));
    }

    void Update ()
    {
        if(inited)
        {
            Tick();
        }
    }

    public void Tick()
    {
        if (_luaEnv != null)
        {
            _luaEnv.Tick();
        }
    }

    private byte[] LuaLoader(ref string fileName)
    {
        byte[] fileData = null;
        Debug.LogError("LuaLoader " + fileName);

#if UNITY_EDITOR
        UnityGameFramework.Runtime.BaseComponent baseComponent = UnityGameFramework.Runtime.GameEntry.GetComponent<UnityGameFramework.Runtime.BaseComponent>();
        if (baseComponent.EditorResourceMode)
        {
            fileData = LoadFromFile(fileName);
        }
        else
        {
            fileData = LoadFromBundle(fileName);
        }
#elif UNITY_ANDROID
        fileData = LoadFromBundle (fileName);
#elif UNITY_IPHONE
        fileData = LoadFromBundle (fileName);
#else
#endif
        return fileData;
    }

    private byte[] LoadFromFile(string fileName)
    {
        string filePath = LuaConst.LuaEditorPath + fileName.Replace('.', '/') + ".lua";
        if (!File.Exists(filePath)) return null;

        try
        {
            //byte[] luaBytes = File.ReadAllBytes(filePath);
            string luaText = File.ReadAllText(filePath);
            byte[] luaBytes = System.Text.UTF8Encoding.UTF8.GetBytes(luaText);
            return luaBytes;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
    }

    public Dictionary<string, byte[]> BundleLuaData
    {
        set;
        private get;
    }

    private byte[] LoadFromBundle(string fileName)
    {
        fileName = fileName.ToLower();
        Debug.LogError("LoadFromBundle " + fileName);
        if (BundleLuaData == null)
        {
            Debug.LogError("Lua Bundle Data Did Not Init");
            return null;
        }
        else
        {
            if (BundleLuaData.ContainsKey(fileName))
            {
                return BundleLuaData[fileName];
            }
            else
            {
                Debug.LogError("Can not find " + fileName);
                return null;
            }
        }
    }

    private IEnumerator LoadBundleLuaFiles(string path)
    {
        WWW download = new WWW(path);

        yield return download;

        AssetBundle bundle = download.assetBundle;

        string[] assetNames = bundle.GetAllAssetNames();
        int lastCount = ".lua.bytes".Length;

        BundleLuaData = new Dictionary<string, byte[]>();
        for (int i = 0; i < assetNames.Length; i++)
        {
            Debug.LogError(i + " " + assetNames[i]);
            int pathNameLenght = assetNames[i].Length;
            int beginIndex = assetNames[i].IndexOf("luatemp");
            beginIndex += "luatemp/".Length;
            int luaNameLengt = pathNameLenght - beginIndex - lastCount;
            string luaPath = assetNames[i].Substring(beginIndex, luaNameLengt);
            luaPath = luaPath.Replace('/', '.');
            Debug.LogError(luaPath);

            TextAsset temp = bundle.LoadAsset<TextAsset>(assetNames[i]);
            BundleLuaData.Add(assetNames[i], temp.bytes);
        }

        bundle.Unload(false);

        Launch();
    }
}