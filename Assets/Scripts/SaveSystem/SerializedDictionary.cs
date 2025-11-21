using System.Collections.Generic;

[System.Serializable]
public class SerializedDictionary<TKey, TValue>
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();

    public SerializedDictionary()
    {
        keys = new List<TKey>();
        values = new List<TValue>();
    }

    public SerializedDictionary(Dictionary<TKey, TValue> dictionary)
    {
        //Add the keys and values to each list
        foreach (KeyValuePair<TKey, TValue> entry in dictionary)
        {
            keys.Add(entry.Key);
            values.Add(entry.Value);
        }
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        //Create a dictionary using the two lists
        Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count; i++)
            d[keys[i]] = values[i];

        return d;
    }
}
