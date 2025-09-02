using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 架勢點數管理系統 - 負責架勢相關的狀態管理
/// 架勢點數在每回合開始時會自動恢復，可用於防禦、格檔等系統
/// </summary>
namespace Wuxia.GameCore
{
    public class PosturePoint : MonoBehaviour
    {
        [Header("架勢點數系統")]
        [SerializeField]
        [Tooltip("當前架勢點數")]
        private float currentPosturePoint = 100f;
        
        [SerializeField]
        [Tooltip("最大架勢點數")]
        private float maxPosturePoint = 100f;
        
        [SerializeField]
        [Tooltip("每回合恢復的架勢點數")]
        private float recoveryPerTurn = 30f;

        // 唯讀屬性，供外部腳本安全地讀取數值
        public float CurrentPP => currentPosturePoint;
        public float MaxPP => maxPosturePoint;
        public float RecoveryPerTurn => recoveryPerTurn;
        public bool IsPostureBroken => currentPosturePoint <= 0;

        [Header("事件系統")]
        [Tooltip("當架勢點數變更時觸發的事件。參數: 當前架勢點數, 最大架勢點數")]
        public UnityEvent<float, float> OnPostureChanged;
        
        [Tooltip("當架勢被破壞時觸發的事件")]
        public UnityEvent OnPostureBroken;
        
        [Tooltip("當架勢恢復時觸發的事件")]
        public UnityEvent OnPostureRecovered;

        private void Awake()
        {
            // 初始化架勢點數
            currentPosturePoint = maxPosturePoint;
        }

        /// <summary>
        /// 消耗架勢點數
        /// </summary>
        /// <param name="amount">消耗數量</param>
        public void ConsumePosture(float amount)
        {
            if (amount <= 0) return;

            bool wasPostureBroken = IsPostureBroken;
            
            currentPosturePoint -= amount;
            currentPosturePoint = Mathf.Max(0, currentPosturePoint);
            
            Debug.Log($"{gameObject.name} 消耗了 {amount} 點架勢，剩餘架勢: {currentPosturePoint}/{maxPosturePoint}");
            
            // 觸發架勢變更事件
            OnPostureChanged?.Invoke(currentPosturePoint, maxPosturePoint);
            
            // 檢查架勢是否被破壞
            if (!wasPostureBroken && IsPostureBroken)
            {
                Debug.Log($"{gameObject.name} 的架勢被破壞！");
                OnPostureBroken?.Invoke();
            }
        }

        /// <summary>
        /// 恢復架勢點數
        /// </summary>
        /// <param name="amount">恢復數量</param>
        public void RecoverPosture(float amount)
        {
            if (amount <= 0) return;

            bool wasPostureBroken = IsPostureBroken;
            
            currentPosturePoint += amount;
            currentPosturePoint = Mathf.Min(currentPosturePoint, maxPosturePoint);
            
            Debug.Log($"{gameObject.name} 恢復了 {amount} 點架勢，當前架勢: {currentPosturePoint}/{maxPosturePoint}");
            
            // 觸發架勢變更事件
            OnPostureChanged?.Invoke(currentPosturePoint, maxPosturePoint);
            
            // 檢查架勢是否從破壞狀態恢復
            if (wasPostureBroken && !IsPostureBroken)
            {
                Debug.Log($"{gameObject.name} 的架勢已恢復！");
                OnPostureRecovered?.Invoke();
            }
        }

        /// <summary>
        /// 回合開始時的架勢恢復
        /// </summary>
        public void OnTurnStart()
        {
            Debug.Log($"{gameObject.name} 回合開始，恢復架勢點數");
            RecoverPosture(recoveryPerTurn);
        }

        /// <summary>
        /// 重置架勢到最大值
        /// </summary>
        public void ResetPosture()
        {
            bool wasPostureBroken = IsPostureBroken;
            
            currentPosturePoint = maxPosturePoint;
            
            Debug.Log($"{gameObject.name} 架勢重置到最大值: {maxPosturePoint}");
            
            // 觸發架勢變更事件
            OnPostureChanged?.Invoke(currentPosturePoint, maxPosturePoint);
            
            // 如果之前架勢被破壞，現在恢復了
            if (wasPostureBroken)
            {
                OnPostureRecovered?.Invoke();
            }
        }

        /// <summary>
        /// 檢查是否有足夠的架勢點數
        /// </summary>
        /// <param name="amount">需要的架勢點數</param>
        /// <returns>是否有足夠的架勢點數</returns>
        public bool HasEnoughPosture(float amount)
        {
            return currentPosturePoint >= amount;
        }

        /// <summary>
        /// 設定最大架勢點數
        /// </summary>
        /// <param name="newMaxPosture">新的最大架勢點數</param>
        public void SetMaxPosture(float newMaxPosture)
        {
            maxPosturePoint = newMaxPosture;
            
            // 如果當前架勢點數超過新的最大值，調整當前值
            if (currentPosturePoint > maxPosturePoint)
            {
                currentPosturePoint = maxPosturePoint;
                OnPostureChanged?.Invoke(currentPosturePoint, maxPosturePoint);
            }
        }

        /// <summary>
        /// 設定每回合恢復量
        /// </summary>
        /// <param name="newRecovery">新的每回合恢復量</param>
        public void SetRecoveryPerTurn(float newRecovery)
        {
            recoveryPerTurn = newRecovery;
        }

        /// <summary>
        /// 設定當前架勢點數（直接設置，慎用）
        /// </summary>
        /// <param name="newValue">新的架勢點數值</param>
        public void SetCurrentPosture(float newValue)
        {
            bool wasPostureBroken = IsPostureBroken;
            
            currentPosturePoint = Mathf.Clamp(newValue, 0, maxPosturePoint);
            
            // 觸發架勢變更事件
            OnPostureChanged?.Invoke(currentPosturePoint, maxPosturePoint);
            
            // 檢查架勢狀態變化
            if (!wasPostureBroken && IsPostureBroken)
            {
                OnPostureBroken?.Invoke();
            }
            else if (wasPostureBroken && !IsPostureBroken)
            {
                OnPostureRecovered?.Invoke();
            }
        }
    }
}