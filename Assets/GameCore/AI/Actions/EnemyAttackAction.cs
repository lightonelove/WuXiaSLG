using System.Collections;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyAttackAction : BaseEnemyAction
    {
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float apCost = 30f;
        [SerializeField] private float attackDuration = 1f;
        [SerializeField] private string attackAnimationTrigger = "Attack";
        
        protected override bool CanExecuteInternal(EnemyCore enemy)
        {
            if (enemy.CurrentActionPoints < apCost)
                return false;
            
            CharacterCore player = GameObject.FindObjectOfType<CharacterCore>();
            if (player == null)
                return false;
            
            float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
            return distance <= attackRange;
        }
        
        public override IEnumerator Execute(EnemyCore enemy)
        {
            CharacterCore player = GameObject.FindObjectOfType<CharacterCore>();
            if (player == null) yield break;
            
            // 面向玩家
            enemy.transform.LookAt(player.transform);
            
            // 播放攻擊動畫
            Animator animator = enemy.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrEmpty(attackAnimationTrigger))
            {
                animator.SetTrigger(attackAnimationTrigger);
            }
            
            // 等待攻擊動畫前半段
            yield return new WaitForSeconds(attackDuration * 0.5f);
            
            // 檢查距離並造成傷害
            float distance = Vector3.Distance(enemy.transform.position, player.transform.position);
            if (distance <= attackRange)
            {
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"[AI] Enemy dealt {attackDamage} damage to player");
                }
            }
            
            // 等待攻擊動畫結束
            yield return new WaitForSeconds(attackDuration * 0.5f);
            
            // 消耗AP
            enemy.SpendActionPoints(apCost);
        }
        
        public override string GetActionName()
        {
            return $"Attack (Range: {attackRange}, Damage: {attackDamage})";
        }
    }
}