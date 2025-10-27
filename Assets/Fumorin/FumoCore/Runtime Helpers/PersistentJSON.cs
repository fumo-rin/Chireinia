using System.IO;
using System.Collections.Generic;
using UnityEngine;

#region JSON Playerprefs Alternate during WEBGL
public static partial class PersistentJSON
{
    private static bool IsWebGLBuild =>
        Application.platform == RuntimePlatform.WebGLPlayer;
    private static bool TrySaveWebGL<T>(T saveItem, string key, string json)
    {
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Saved {typeof(T).Name} to PlayerPrefs key '{key}'");
        return true;
    }
    private static bool TryLoadWebGL(out string json, string key)
    {
        json = null;
        if (!PlayerPrefs.HasKey(key))
        {
            if (DebugMode)
                Debug.LogWarning($"[WebGL] No PlayerPrefs key found for '{key}'");
            return false;
        }
        json = PlayerPrefs.GetString(key);
        if (DebugMode)
            Debug.Log($"[WebGL] Loaded JSON string for '{key}'");
        return true;
    }
    public static bool TryDeleteWebGL(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return false;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        if (DebugMode)
            Debug.Log($"[WebGL] Deleted PlayerPrefs key '{key}'");
        return true;
    }
}
#endregion
public static partial class PersistentJSON
{
    public static bool DebugMode => false;
    [System.Serializable]
    private class ListWrapper<TItem>
    {
        public List<TItem> Items;
        public ListWrapper(List<TItem> items) => Items = items;
    }
    [System.Serializable]
    private class PrimitiveWrapper<T>
    {
        public T Value;
        public PrimitiveWrapper(T value) => Value = value;
    }
    private static string SaveFilePath<T>(string fileName)
    {
        string typeName = typeof(T).Name;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            typeName = $"ListOf_{elementType.Name}";
        }
        string safeFileName = fileName.Replace(" ", "_");
        return Path.Combine(Application.persistentDataPath, $"Json Storage/{safeFileName}_{typeName}.json");
    }
    public static bool TrySave<T>(T saveItem, string key)
    {
        if (saveItem == null) return false;
        string json;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = System.Activator.CreateInstance(wrapperType, saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = new PrimitiveWrapper<T>(saveItem);
            json = JsonUtility.ToJson(wrapper, true);
        }
        else
        {
            json = JsonUtility.ToJson(saveItem, true);
        }
        if (IsWebGLBuild)
            return TrySaveWebGL(saveItem, key, json);

        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Json Storage"));
        string path = SaveFilePath<T>(key);
        File.WriteAllText(path, json);
        if (DebugMode) Debug.Log($"Saved {typeof(T).Name} to {path}");

        return true;
    }

    public static bool TryLoad<T>(out T target, string key)
    {
        target = default(T);
        string json = null;
        if (IsWebGLBuild)
        {
            if (!TryLoadWebGL(out json, key))
                return false;
        }
        else
        {
            string path = SaveFilePath<T>(key);
            if (!File.Exists(path))
            {
                if (DebugMode) Debug.LogWarning($"No save found at {path}");
                return false;
            }
            json = File.ReadAllText(path);
        }
        T item;
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = typeof(T).GetGenericArguments()[0];
            var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
            var wrapper = JsonUtility.FromJson(json, wrapperType);
            var itemsField = wrapperType.GetField("Items");
            item = (T)itemsField.GetValue(wrapper);
        }
        else if (IsPrimitiveOrString(typeof(T)))
        {
            var wrapper = JsonUtility.FromJson<PrimitiveWrapper<T>>(json);
            item = wrapper.Value;
        }
        else
        {
            item = JsonUtility.FromJson<T>(json);
        }
        if (item == null)
        {
            Debug.LogWarning($"Failed to deserialize {typeof(T).Name} from {(IsWebGLBuild ? "PlayerPrefs" : "file")}");
            return false;
        }
        target = item;
        if (DebugMode)
        {
            if (IsWebGLBuild)
                Debug.Log($"Loaded {typeof(T).Name} from PlayerPrefs key '{key}'");
            else
                Debug.Log($"Loaded {typeof(T).Name} from file");
        }
        return true;
    }
    private static bool IsPrimitiveOrString(System.Type t)
    {
        return t.IsPrimitive || t == typeof(string) ||
               t == typeof(decimal) || t == typeof(double) ||
               t == typeof(float);
    }
}