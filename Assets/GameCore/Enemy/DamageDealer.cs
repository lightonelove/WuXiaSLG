using UnityEngine;

namespace Wuxia.GameCore
{
    public class DamageDealer : MonoBehaviour
{
    [SerializeField]
    private float damage = 25f;

    [SerializeField]
    private bool destroyOnDealDamage = true;

    /// <summary>
    /// 公開方法，讓外部可以讀取傷害值
    /// </summary>
    public float GetDamage()
    {
        return damage;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 這部分的邏輯完全不需要改變，它依然是尋找 DamageReceiver
        DamageReceiver receiver = other.GetComponent<DamageReceiver>();
        if (receiver != null)
        {

            if (destroyOnDealDamage)
            {
                Destroy(gameObject);
            }
        }
    }
}
}