using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class ItemListHandler : Singleton<ItemListHandler> 
{
    [SerializeField] private Transform itemScrollContent;
    
    private GameObject _itemInfoPrefab;
    private readonly ConcurrentBag<GameObject> _itemList = new();
    private readonly ConcurrentBag<ItemInfoHandler> _itemInfoList = new();
    
    public ConcurrentQueue<string> ItemDataQueue { get; private set; } = new();

    public ConcurrentBag<ItemInfoHandler> ItemInfoList => _itemInfoList;
    
    private UnityMainThreadDispatcher _dispatcher;

    private void Awake()
    {
        _itemInfoPrefab = Resources.Load<GameObject>("ItemPrefab");
    }

    private void Start()
    {
        StartCoroutine(WaitServerConnectRoutine());
    }
    
    private IEnumerator WaitServerConnectRoutine()
    {
        NetworkManager manager = NetworkManager.Instance.Result;
        _dispatcher = UnityMainThreadDispatcher.Instance.Result;

        while (!manager.IsRunning)
        {
            yield return null;
        }

        ReceiveItemData();
    }
    
    private async void Update()
    {
        string data;
        
        while(!ItemDataQueue.TryDequeue(out data))
        {
            await Task.Delay(100);
        }

        await AddItemData(data);
    }

    private void ReceiveItemData()
    {
        NetworkData data = new (ENetworkDataType.Get, ETableList.item_list.ToString());
        NetworkManager.Instance.Result.EnqueueData(data);
    }

    private async Task AddItemData(string data)
    {
        await Task.Run(() =>
        {
            string tableName = "";
            int i = 0;

            while (data[i] != '@')
            {
                tableName += data[i++];
            }

            data = data.Remove(0, i + 1);
            string[] item = data.Split(',');
            
            _dispatcher.Enqueue(() =>
            {
                GameObject itemInfo = Instantiate(_itemInfoPrefab, itemScrollContent);
                ItemInfoHandler infoHandler = itemInfo.GetComponent<ItemInfoHandler>();
                infoHandler.SetItemInfo(item);
                itemInfo.name = $"{infoHandler.UserID}_{infoHandler.ItemName}";
                
                _itemList.Add(itemInfo);
                _itemInfoList.Add(itemInfo.GetComponent<ItemInfoHandler>());
            });
        });
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
