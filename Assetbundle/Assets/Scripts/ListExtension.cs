
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class ListExtension
{

    public static T GetAndRemove<T>(this List<T> list, int index)
    {
        if (list.Count <= index || index < 0)
        {
            Debug.LogError("Index over range " + list.Count + ", No : " + index + ")");
        }

        T target = list[index];
        list.Remove(target);
        return target;
    }

    public static void SafeRemove<T>(this List<T> list, T data)
    {
        if (list.Contains(data))
        {
            list.Remove(data);
        }
    }

    public static T PopFirst<T>(this List<T> list)
    {
        return list.GetAndRemove(0);
    }

    public static T PopLast<T>(this List<T> list)
    {
        return list.GetAndRemove(list.Count - 1);
    }

    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int k = UnityEngine.Random.Range(0, list.Count);
            T temp = list[i];
            list[i] = list[k];
            list[k] = temp;
        }
    }

    public static bool IsEmpty<T>(this List<T> list)
    {
        return list.Count <= 0;
    }

    public static void AddFirst<T>(this List<T> list, T element)
    {
        if (list.IsEmpty())
            list.Add(element);
        else
            list.Insert(0, element);
    }
}

public static class HashSetExtension
{
    public static bool IsEmpty<T>(this HashSet<T> list)
    {
        return list.Count <= 0;
    }
}


public static class ArrayExtensions
{

    //　Sort by multi key sort
    public static void Sort<TSource, TResult>(
        this TSource[] array,
        Func<TSource, TResult> selector1,
        bool isDesc1,
        Func<TSource, TResult> selector2,
        bool isDesc2) where TResult : IComparable
    {

        Array.Sort(array, (x, y) => {

            int result = 0;
            if (isDesc1)
            {
                result = selector1(y).CompareTo(selector1(x));
            }
            else
            {
                result = selector1(x).CompareTo(selector1(y));
            }

            if (result != 0)
            {
                return result;
            }

            if (isDesc2)
            {
                result = selector2(y).CompareTo(selector2(x));
            }
            else
            {
                result = selector2(x).CompareTo(selector2(y));
            }

            return result;
        });
    }
}
