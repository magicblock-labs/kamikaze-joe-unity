using System.Collections.Generic;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Nft;
using Solana.Unity.Wallet;
using UnityEngine;

public class DisableOnLogin : MonoBehaviour
{
    private void OnEnable()
    {
        Web3.OnNFTsUpdate += OnNFTsUpdate;
    }

    private void OnDisable()
    {
        Web3.OnNFTsUpdate -= OnNFTsUpdate;
    }

    private void OnNFTsUpdate(List<Nft> nfts, int total)
    {
        Debug.Log($"NFTs updated. Total: {total}");
    }

    private void OnBalanceChange(double solBalance)
    {
        Debug.Log($"Balance changed to {solBalance}");
    }

    private void d(Account account)
    {
        Debug.Log(account.PublicKey);
    }
}
