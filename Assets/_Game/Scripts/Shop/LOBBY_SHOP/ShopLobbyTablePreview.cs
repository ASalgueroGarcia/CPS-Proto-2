using UnityEngine;
using UnityEngine.UI;

// for the preview of the items before open it.
public class ShopLobbyTablePreview : MonoBehaviour
{
    [Header("ICONS OF THE ITEMS ON THE TABLE")]
    [SerializeField] private Image[] icons =new Image[18];
    
    public void RefreshPreview(PowerUpData[] currentPowerUps)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] != null)
            {
                bool hItem = i < currentPowerUps.Length && currentPowerUps[i] != null;
                icons[i].gameObject.SetActive(hItem);
                if (hItem)
                {
                    icons[i].sprite = currentPowerUps[i].powerUpIcon;
                }
            }
        }
    }
}