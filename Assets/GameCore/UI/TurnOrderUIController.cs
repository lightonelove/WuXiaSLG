using System.Collections.Generic;
using UnityEngine;

public class TurnOrderUIController : MonoBehaviour
{
    [Header("UI元件連結")]
    [Tooltip("行動順序圖示的Prefab")]
    [SerializeField] private GameObject turnOrderIconPrefab;

    [Tooltip("用來放置所有圖示的容器物件 (需掛載Horizontal Layout Group)")]
    [SerializeField] private Transform iconContainer;

    private List<TurnOrderIcon> spawnedIcons = new List<TurnOrderIcon>();

    /// <summary>
    /// 更新整個行動順序UI的顯示。
    /// </summary>
    /// <param name="predictedOrder">由TurnManager預測出來的角色順序列表</param>
    public void UpdateTurnOrderDisplay(List<ClonedCombatEntity> predictedOrder)
    {
        if (turnOrderIconPrefab == null || iconContainer == null)
        {
            Debug.LogError("TurnOrderUIController尚未設定Prefab或Container！");
            return;
        }

        // 確保我們有足夠的圖示物件可以使用，不夠就生成
        while (spawnedIcons.Count < predictedOrder.Count)
        {
            GameObject newIconObj = Instantiate(turnOrderIconPrefab, iconContainer);
            TurnOrderIcon newIcon = newIconObj.GetComponent<TurnOrderIcon>();
            if (newIcon != null)
            {
                spawnedIcons.Add(newIcon);
            }
        }

        // 使用預測的順序來設定每一個圖示
        for (int i = 0; i < spawnedIcons.Count; i++)
        {
            if (i < predictedOrder.Count)
            {
                // 如果預測列表裡有角色，就設定圖示並顯示
                spawnedIcons[i].Setup(predictedOrder[i].realEntity);
            }
            else
            {
                // 如果圖示數量多於預測數量，則隱藏多餘的圖示
                spawnedIcons[i].gameObject.SetActive(false);
            }
        }
    }
}