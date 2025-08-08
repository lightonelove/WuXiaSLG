using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 技能瞄準碰撞檢測器 - 附加到瞄準Cube上用於檢測觸發器碰撞
/// </summary>
public class SkillTargetingCollisionDetector : MonoBehaviour
{
    public CharacterCore characterCore;
    public LayerMask floorLayerMask;
    private HashSet<Collider> collidingObjects = new HashSet<Collider>();

    /// <summary>
    /// 初始化碰撞檢測器
    /// </summary>
    /// <param name="core">Character Core 引用</param>
    /// <param name="layerMask">Floor 層遮罩</param>
    public void Initialize(CharacterCore core, LayerMask layerMask)
    {
        characterCore = core;
        floorLayerMask = layerMask;
    }

    void OnTriggerEnter(Collider other)
    {
        
        // 檢查是否為Floor層的物件
        if (IsInLayerMask(other.gameObject, floorLayerMask))
        {
            Debug.Log(other.name);
            // 添加到碰撞列表
            if (collidingObjects.Add(other))
            {
                Debug.Log($"[SkillTargeting] 進入碰撞: {other.gameObject.name}");
                
                // 通知Character Core更新狀態
                if (characterCore != null)
                {
                    characterCore.OnTargetingCollisionChanged(collidingObjects);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // 檢查是否為Floor層的物件
        if (IsInLayerMask(other.gameObject, floorLayerMask))
        {
            // 從碰撞列表移除
            if (collidingObjects.Remove(other))
            {
                Debug.Log($"[SkillTargeting] 退出碰撞: {other.gameObject.name}");
                
                // 通知Character Core更新狀態
                if (characterCore != null)
                {
                    characterCore.OnTargetingCollisionChanged(collidingObjects);
                }
            }
        }
    }

    /// <summary>
    /// 檢查物件是否在指定的Layer Mask中
    /// </summary>
    /// <param name="obj">要檢查的物件</param>
    /// <param name="layerMask">Layer Mask</param>
    /// <returns>是否在Layer Mask中</returns>
    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((layerMask.value & (1 << obj.layer)) > 0);
    }

    /// <summary>
    /// 清除所有碰撞記錄
    /// </summary>
    public void ClearCollisions()
    {
        collidingObjects.Clear();
        if (characterCore != null)
        {
            characterCore.OnTargetingCollisionChanged(collidingObjects);
        }
    }

    /// <summary>
    /// 獲取當前碰撞的物件數量
    /// </summary>
    /// <returns>碰撞物件數量</returns>
    public int GetCollisionCount()
    {
        return collidingObjects.Count;
    }

    /// <summary>
    /// 獲取所有碰撞物件的名稱
    /// </summary>
    /// <returns>碰撞物件名稱字串</returns>
    public string GetCollisionObjectNames()
    {
        if (collidingObjects.Count == 0)
            return "";

        string names = "";
        int index = 0;
        foreach (Collider col in collidingObjects)
        {
            if (col != null)
            {
                names += col.gameObject.name;
                if (index < collidingObjects.Count - 1)
                    names += ", ";
            }
            index++;
        }
        return names;
    }
}