// PlaceInteraction.cs
using UnityEngine;

public abstract class PlaceInteraction : MonoBehaviour
{
    /// <summary>
    /// 특정 키의 상태가 변경될 때 호출됩니다.
    /// </summary>
    /// <param name="key">변경된 키</param>
    /// <param name="value">새로운 값</param>
    public abstract void OnStateChanged(string key, object value);
}

