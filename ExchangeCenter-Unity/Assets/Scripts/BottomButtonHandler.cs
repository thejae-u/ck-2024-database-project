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

    private void OnEnable()
    {
        Debug.Log($"Setting up Button Click Listeners");
        buyButton.onClick.AddListener(OnBuyButtonClick);
        sellButton.onClick.AddListener(OnSellButtonClick);
        inventoryButton.onClick.AddListener(OnInventoryButtonClick);
    }

    private void OnDisable()
    {
        Debug.Log($"Removing Button Click Listeners");
        buyButton.onClick.RemoveListener(OnBuyButtonClick);
        sellButton.onClick.RemoveListener(OnSellButtonClick);
        inventoryButton.onClick.RemoveListener(OnInventoryButtonClick);
    }


    private void OnBuyButtonClick()
    {
        Debug.Log($"Buy Button Clicked");
    }

    private void OnSellButtonClick()
    {
        Debug.Log($"Sell Button Clicked");
    }

    private void OnInventoryButtonClick()
    {
        Debug.Log($"Inventory Button Clicked");
    }
}
