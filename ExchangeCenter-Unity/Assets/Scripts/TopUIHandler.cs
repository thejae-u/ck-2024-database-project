using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopUIHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    
    private void OnEnable()
    {
        searchButton.onClick.AddListener(OnSearchButtonClick);
    }
    
    private void OnDisable()
    {
        searchButton.onClick.RemoveListener(OnSearchButtonClick);
    }

    private void OnSearchButtonClick()
    {
        string searchText = searchInputField.text;
        if (searchText.Equals(string.Empty))
        {
            Debug.Log($"Search Input Field is Empty");
            return;    
        }
        
        Log.LogSend("Search");
        Debug.Log($"Search Button Clicked with {searchText}");
    }
}
