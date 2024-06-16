using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopUIHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField userIdInputField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    
    private void OnEnable()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
        searchButton.onClick.AddListener(OnSearchButtonClick);
    }
    
    private void OnDisable()
    {
        loginButton.onClick.RemoveListener(OnLoginButtonClick);
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
    }

    private void OnLoginButtonClick()
    {
        string userId = userIdInputField.text;
        if (userId.Equals(string.Empty))
        {
            Debug.Log($"User ID Input Field is Empty");
            return;
        }
        
        Log.LogSend($"Login with {userId}");
        NetworkData loginData = new (ENetworkDataType.Login, userId);
        NetworkManager.Instance.Result.EnqueueData(loginData);
    }
}
