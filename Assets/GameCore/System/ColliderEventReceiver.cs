using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用 Collider 事件接收器 - 可以讓其他 MonoBehaviour 訂閱碰撞事件
/// 支援 OnTrigger 和 OnCollision 兩種碰撞檢測模式
/// </summary>
public class ColliderEventReceiver : MonoBehaviour
{
    [Header("事件設定")]
    [SerializeField] private bool enableTriggerEvents = true; // 是否啟用 Trigger 事件
    [SerializeField] private bool enableCollisionEvents = true; // 是否啟用 Collision 事件
    
    [Header("圖層篩選")]
    [SerializeField] private LayerMask targetLayerMask = -1; // 要檢測的圖層遮罩，-1 表示所有圖層
    
    [Header("Debug 設定")]
    [SerializeField] private bool enableDebugLog = false; // 是否啟用 Debug 輸出
    
    // ===== Trigger 事件 =====
    /// <summary>
    /// Trigger Enter 事件 - 當物件進入觸發器時
    /// </summary>
    public UnityEvent<Collider> OnTriggerEnterEvent = new UnityEvent<Collider>();
    
    /// <summary>
    /// Trigger Stay 事件 - 當物件停留在觸發器中時
    /// </summary>
    public UnityEvent<Collider> OnTriggerStayEvent = new UnityEvent<Collider>();
    
    /// <summary>
    /// Trigger Exit 事件 - 當物件離開觸發器時
    /// </summary>
    public UnityEvent<Collider> OnTriggerExitEvent = new UnityEvent<Collider>();
    
    // ===== Collision 事件 =====
    /// <summary>
    /// Collision Enter 事件 - 當物件開始碰撞時
    /// </summary>
    public UnityEvent<Collision> OnCollisionEnterEvent = new UnityEvent<Collision>();
    
    /// <summary>
    /// Collision Stay 事件 - 當物件持續碰撞時
    /// </summary>
    public UnityEvent<Collision> OnCollisionStayEvent = new UnityEvent<Collision>();
    
    /// <summary>
    /// Collision Exit 事件 - 當物件結束碰撞時
    /// </summary>
    public UnityEvent<Collision> OnCollisionExitEvent = new UnityEvent<Collision>();
    
    // ===== C# 事件（用於程式碼訂閱） =====
    /// <summary>
    /// Trigger Enter C# 事件
    /// </summary>
    public System.Action<Collider> TriggerEntered;
    
    /// <summary>
    /// Trigger Stay C# 事件
    /// </summary>
    public System.Action<Collider> TriggerStaying;
    
    /// <summary>
    /// Trigger Exit C# 事件
    /// </summary>
    public System.Action<Collider> TriggerExited;
    
    /// <summary>
    /// Collision Enter C# 事件
    /// </summary>
    public System.Action<Collision> CollisionEntered;
    
    /// <summary>
    /// Collision Stay C# 事件
    /// </summary>
    public System.Action<Collision> CollisionStaying;
    
    /// <summary>
    /// Collision Exit C# 事件
    /// </summary>
    public System.Action<Collision> CollisionExited;
    
    // ===== Unity Trigger 方法 =====
    void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerEvents) return;
        if (!IsTargetLayer(other.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnTriggerEnter: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnTriggerEnterEvent?.Invoke(other);
        
        // 呼叫 C# 事件
        TriggerEntered?.Invoke(other);
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!enableTriggerEvents) return;
        if (!IsTargetLayer(other.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnTriggerStay: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnTriggerStayEvent?.Invoke(other);
        
        // 呼叫 C# 事件
        TriggerStaying?.Invoke(other);
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!enableTriggerEvents) return;
        if (!IsTargetLayer(other.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnTriggerExit: {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnTriggerExitEvent?.Invoke(other);
        
        // 呼叫 C# 事件
        TriggerExited?.Invoke(other);
    }
    
    // ===== Unity Collision 方法 =====
    void OnCollisionEnter(Collision collision)
    {
        if (!enableCollisionEvents) return;
        if (!IsTargetLayer(collision.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnCollisionEnter: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnCollisionEnterEvent?.Invoke(collision);
        
        // 呼叫 C# 事件
        CollisionEntered?.Invoke(collision);
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (!enableCollisionEvents) return;
        if (!IsTargetLayer(collision.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnCollisionStay: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnCollisionStayEvent?.Invoke(collision);
        
        // 呼叫 C# 事件
        CollisionStaying?.Invoke(collision);
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (!enableCollisionEvents) return;
        if (!IsTargetLayer(collision.gameObject.layer)) return;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ColliderEventReceiver] OnCollisionExit: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");
        }
        
        // 呼叫 UnityEvent
        OnCollisionExitEvent?.Invoke(collision);
        
        // 呼叫 C# 事件
        CollisionExited?.Invoke(collision);
    }
    
    // ===== 輔助方法 =====
    /// <summary>
    /// 檢查是否為目標圖層
    /// </summary>
    /// <param name="layer">要檢查的圖層</param>
    /// <returns>是否為目標圖層</returns>
    private bool IsTargetLayer(int layer)
    {
        return (targetLayerMask.value & (1 << layer)) != 0;
    }
    
    /// <summary>
    /// 設定目標圖層遮罩
    /// </summary>
    /// <param name="layerMask">新的圖層遮罩</param>
    public void SetTargetLayerMask(LayerMask layerMask)
    {
        targetLayerMask = layerMask;
    }
    
    /// <summary>
    /// 啟用或停用 Trigger 事件
    /// </summary>
    /// <param name="enabled">是否啟用</param>
    public void SetTriggerEventsEnabled(bool enabled)
    {
        enableTriggerEvents = enabled;
    }
    
    /// <summary>
    /// 啟用或停用 Collision 事件
    /// </summary>
    /// <param name="enabled">是否啟用</param>
    public void SetCollisionEventsEnabled(bool enabled)
    {
        enableCollisionEvents = enabled;
    }
    
    /// <summary>
    /// 啟用或停用 Debug 輸出
    /// </summary>
    /// <param name="enabled">是否啟用</param>
    public void SetDebugLogEnabled(bool enabled)
    {
        enableDebugLog = enabled;
    }
    
    /// <summary>
    /// 清除所有事件訂閱
    /// </summary>
    public void ClearAllEventSubscriptions()
    {
        // 清除 UnityEvent
        //OnTriggerEnterEvent?.RemoveAllListeners();
        //OnTriggerStayEvent?.RemoveAllListeners();
        //OnTriggerExitEvent?.RemoveAllListeners();
        OnCollisionEnterEvent?.RemoveAllListeners();
        OnCollisionStayEvent?.RemoveAllListeners();
        OnCollisionExitEvent?.RemoveAllListeners();
        
        // 清除 C# 事件
        TriggerEntered = null;
        TriggerStaying = null;
        TriggerExited = null;
        CollisionEntered = null;
        CollisionStaying = null;
        CollisionExited = null;
    }
    
    void OnDestroy()
    {
        // 組件銷毀時清除所有事件訂閱
        //ClearAllEventSubscriptions();
    }
}