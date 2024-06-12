using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemListHandler : Singleton<ItemListHandler> 
{
    [SerializeField] private Transform itemScrollContent;
    
    private GameObject _itemInfoPrefab;
    private readonly List<GameObject> _itemList = new();
    private readonly List<ItemInfoHandler> _itemInfoList = new();
    
    public List<ItemInfoHandler> ItemList => _itemInfoList;

    private void Awake()
    {
        _itemInfoPrefab = Resources.Load<GameObject>("ItemPrefab");
    }

    private void Start()
    {
        for (int i = 0; i < 100; i++)
        {
            GameObject itemInfo = Instantiate(_itemInfoPrefab, itemScrollContent);
            itemInfo.name = $"ItemInfo_{i + 1}";
            _itemList.Add(itemInfo);
            _itemInfoList.Add(itemInfo.GetComponent<ItemInfoHandler>());
        }
    }

    public void ItemDeselectAll()
    {
        foreach (var itemInfo in _itemInfoList)
        {
            if (!itemInfo.IsSelected)
            {
                continue;
            }
            
            itemInfo.Deselect();
        }
    }
}
