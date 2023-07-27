using System;
using Cysharp.Threading.Tasks;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

public class ShowBalance : MonoBehaviour
{
    private Text _txtBalance;

    private void Awake()
    {
        _txtBalance = GetComponent<Text>();
    }

    private void OnEnable()
    {
        Web3.OnBalanceChange += BalanceChanged;
    }

    private void OnDisable()
    {
        Web3.OnBalanceChange -= BalanceChanged;
    }
    
    private void BalanceChanged(double balance)
    {
        _txtBalance.text = $"{Math.Round(balance, 3)} SOL";
        
        // Try to request airdrop if balance is 0
        if (Web3.Account != null && balance == 0 && Web3.Rpc.NodeAddress.ToString().Contains("devnet")) RequestAirdrop().Forget();
    }
    
    private async UniTask RequestAirdrop()
    { 
        await Web3.Wallet.RequestAirdrop(commitment: Commitment.Confirmed);
    }
}