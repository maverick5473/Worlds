﻿using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public interface IFilterableValue
{
    bool ShouldFilter();
}

public class XmlSerializableFilterableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable where TValue : IKeyedValue<TKey>, IFilterableValue
{
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(List<TValue>));

        if (reader.Read() && !reader.IsEmptyElement)
        {
            List<TValue> values = (List<TValue>)serializer.Deserialize(reader);

            for (int i = 0; i < values.Count; i++)
            {
                this.Add(values[i].GetKey(), values[i]);
            }
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(List<TValue>));

        List<TValue> values = new List<TValue>(this.Count);
        
        foreach (TValue value in Values)
        {
            if (value.ShouldFilter()) continue;

            values.Add(value);
        }

        serializer.Serialize(writer, values);
    }
}

