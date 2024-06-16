using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomButtonHandler : MonoBehaviour
{
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button inventoryButton;
    
    private readonly List<string> _selectedItemList = new();

    private void OnEnable()
    {
        buyButton.onClick.AddListener(OnBuyButtonClick);
        sellButton.onClick.AddListener(OnSellButtonClick);
        inventoryButton.onClick.AddListener(OnInventoryButtonClick);
    }

    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(OnBuyButtonClick);
        sellButton.onClick.RemoveListener(OnSellButtonClick);
        inventoryButton.onClick.RemoveListener(OnInventoryButtonClick);
    }


    private void OnBuyButtonClick()
    {
        foreach (var itemInfo in ItemListHandler.Instance.Result.ItemInfoList)
        {
            if (itemInfo.IsSelected)
            {
                _selectedItemList.Add(itemInfo.name);
            }
        }

        if (_selectedItemList.Count == 0)
        {
            Debug.Log($"No Item Selected");
            return;
        }
        
        string[] selectedItems = _selectedItemList.ToArray();
        string sendData = string.Join(",", selectedItems);

        NetworkData data = new (ENetworkDataType.Buy, sendData);
        NetworkManager.Instance.Result.EnqueueData(data);
        
        // TODO : 데이터베이스에서 확인 된 상태를 받아서 처리
        // TODO : 구매가 완료된 아이템에 한하여 인벤토리에 추가
        
        ResetSelectState();
    }

    private void ResetSelectState()
    {
        _selectedItemList.Clear();
        ItemListHandler.Instance.Result.ItemDeselectAll();
    }

    private void OnSellButtonClick()
    {
        Log.LogSend("Sell");
        Debug.Log($"Sell Button Clicked");
    }

    private void OnInventoryButtonClick()
    {
        Log.LogSend("Inventory");
        Debug.Log($"Inventory Button Clicked");
    }
}
