﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class FixedStringValueExpression : IStringExpression
{
    public const string Regex = @"^" + ModUtility.IdentifierRegexPart + @"\s*$";

    private string _value;

    public FixedStringValueExpression(string identifier)
    {
        _value = identifier;
    }

    public string Value => _value;

    public string GetString() => Value.ToString();

    public override string ToString() => _value;
}
