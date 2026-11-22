using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.UI;

public class TapController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ActivateTab(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ActivateTab(int tabNo)
    {
        if (tabNo < 0 || tabNo >= pages.Length)
        {
            return;
        }

        for( int i = 0; i< pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabImages[i].color = Color.grey;

        }
        pages[tabNo].SetActive(true);
        tabImages[tabNo].color = Color.white;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ShowPageByIndex(tabNo);
        }
    }
}
