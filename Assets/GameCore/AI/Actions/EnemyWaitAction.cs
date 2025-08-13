using System.Collections;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyWaitAction : BaseEnemyAction
    {
        [SerializeField] private float waitDuration = 1f;
        [SerializeField] private float apRecovery = 20f;
        [SerializeField] private bool playIdleAnimation = true;
        [SerializeField] private string idleAnimationTrigger = "Idle";
        
        public override void InitializeAction(EnemyCore enemy)
        {
            base.InitializeAction(enemy);
            
            // EnemyWaitAction 特定的初始化邏輯
            if (playIdleAnimation)
            {
                Animator animator = enemy.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyWaitAction 需要 Animator 但找不到該元件");
                }
                else if (string.IsNullOrEmpty(idleAnimationTrigger))
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyWaitAction 未設定待機動畫觸發器");
                }
                else
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} Idle animation trigger '{idleAnimationTrigger}' 已準備就緒");
                }
            }
        }
        
        protected override bool CanExecuteInternal(EnemyCore enemy)
        {
            return true; // 等待行動總是可以執行
        }
        
        public override IEnumerator Execute(EnemyCore enemy)
        {
            Debug.Log($"[AI] Enemy waiting for {waitDuration} seconds");
            
            // 播放待機動畫
            Animator animator = enemy.GetComponent<Animator>();
            if (playIdleAnimation && animator != null && !string.IsNullOrEmpty(idleAnimationTrigger))
            {
                animator.SetTrigger(idleAnimationTrigger);
            }
            
            // 等待指定時間
            yield return new WaitForSeconds(waitDuration);
            
            // 恢復AP（但不超過最大值）
            float currentAP = enemy.CurrentActionPoints;
            float newAP = Mathf.Min(currentAP + apRecovery, enemy.MaxActionPoints);
            
            // 注意：這裡直接設置currentActionPoints，因為SpendActionPoints是減少AP的
            // 在實際實現中，你可能需要在EnemyCore中加入RestoreActionPoints方法
            Debug.Log($"[AI] Enemy recovered {apRecovery} AP. Current AP: {newAP}");
        }
        
        public override string GetActionName()
        {
            return $"Wait ({waitDuration}s, +{apRecovery} AP)";
        }
    }
}