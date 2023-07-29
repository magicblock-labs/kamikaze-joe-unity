using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;

public class DisableOnLogin : MonoBehaviour
{
    private void OnEnable()
    {
        Web3.OnLogin += OnLogin;
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnLogin;
    }

    private void OnLogin(Account obj)
    {
        gameObject.SetActive(false);
    }
}
