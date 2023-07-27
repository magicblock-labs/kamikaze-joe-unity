using codebase.utility;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ShowPublicKey : MonoBehaviour
{
    private Text _txtPk;
    private Toast _toast;

    private void Awake()
    {
        _txtPk = GetComponent<Text>();
        _toast = GetComponent<Toast>();
        var button = GetComponent<Button>();
        button.onClick.AddListener(CopyPublicKey);
    }

    private void OnEnable()
    {
        Web3.OnWalletChangeState += WalletChanged;
    }

    private void OnDisable()
    {
        Web3.OnWalletChangeState -= WalletChanged;
    }

    private void WalletChanged()
    {
        if(Web3.Account == null) return;
        string pk = Web3.Account.PublicKey.ToString();
        _txtPk.text = $"{pk.Substring(0, 4)}...{pk.Substring(pk.Length - 4)}";
        Debug.Log($"Logged in with: {pk}");
    }
    
    public void CopyPublicKey()
    {
        if(Web3.Account == null) return;
        Clipboard.Copy(Web3.Account.PublicKey.ToString());
        _toast.ShowToast("Public key copied to clipboard", 3);
    }
}
