using System;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

public class DetectEnergyChange : MonoBehaviour
{

    public static Action<int> OnNumberPressed;
    public static Action OnExplosion;
    private Text _text;


    private void Start()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        OnNumberPressed += OnNumberPressedHandler;
    }

    private void OnDisable()
    {
        OnNumberPressed += OnNumberPressedHandler;
    }

    private void OnNumberPressedHandler(int keyPressed)
    {
        _text.text = "Energy: " + keyPressed;
        CharacterGridMovement.EnergyToUse = keyPressed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnNumberPressed?.Invoke(1);
        } else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OnNumberPressed?.Invoke(2);
        } else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnNumberPressed?.Invoke(3);
        } else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            OnNumberPressed?.Invoke(4);
        } else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            OnNumberPressed?.Invoke(5);
        }else if (Input.GetKeyDown(KeyCode.Space))
        {
            OnExplosion?.Invoke();
        }
    }
}
