
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LibPlayerSpawnManager : UdonSharpBehaviour
{
    protected static T[] Append<T>(T[] array, T value)
    {
        T[] result = new T[array.Length + 1];
        array.CopyTo(result, 0);
        result[array.Length] = value;
        return result;
    }
    protected static string JoinDebugInfo<T>(T[] array)
    {
        string result = "";
        for(int i = 0; i<array.Length; i++)
        {
            result += array[i].ToString();
            result += ", ";
        }
        return result;
    }

}
