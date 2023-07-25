using UnityEngine;

// ReSharper disable once CheckNamespace

public class Loading : MonoBehaviour
{
    private static GameObject _loadingSpinner;
    private static GameObject _loadingSpinnerSmall;
    
    public static void StartLoading()
    {
        _loadingSpinner ??= GameObject.Find("Loading");
        if(_loadingSpinner != null)
            _loadingSpinner.transform.GetChild(0)?.gameObject.SetActive(true);
    }
    
    public static void StopLoading()
    {
        _loadingSpinner ??= GameObject.Find("Loading");
        if(_loadingSpinner != null)
            _loadingSpinner.transform.GetChild(0)?.gameObject.SetActive(false);
    }
    
    public static void StartLoadingSmall()
    {
        _loadingSpinnerSmall ??= GameObject.Find("LoadingSmall");
        if(_loadingSpinnerSmall != null)
            _loadingSpinnerSmall.transform.GetChild(0)?.gameObject.SetActive(true);
    }
    
    public static void StopLoadingSmall()
    {
        _loadingSpinnerSmall ??= GameObject.Find("LoadingSmall");
        if(_loadingSpinnerSmall != null)
            _loadingSpinnerSmall.transform.GetChild(0)?.gameObject.SetActive(false);
    }
}
