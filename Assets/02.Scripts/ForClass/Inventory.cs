using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores acquired ItemData references and notifies UI listeners when the list changes.
/// </summary>
public class Inventory : MonoBehaviour
{
    [SerializeField]
    private List<ItemData> items = new List<ItemData>();

    /// <summary>
    /// Raised whenever the inventory list changes.
    /// </summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// Read-only view of the currently acquired item data.
    /// </summary>
    public IReadOnlyList<ItemData> Items => items;

    /// <summary>
    /// Adds an ItemData reference and refreshes any subscribed UI.
    /// </summary>
    public void AddItem(ItemData item)
    {
        AddItem(item, null);
    }

    /// <summary>
    /// Play 모니터 연동용 — 아이템 추가 후 로그를 남깁니다.
    /// </summary>
    public void AddItem(ItemData item, ItemInteractionPlayMonitor monitor)
    {
        if (item == null)
        {
            Debug.LogWarning("[Inventory] null ItemData cannot be added.", this);
            return;
        }

        items.Add(item);
        monitor?.LogInventoryAdd(item, items.Count);
        OnInventoryChanged?.Invoke();
    }
}
