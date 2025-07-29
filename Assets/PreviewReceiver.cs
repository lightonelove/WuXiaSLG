using UnityEngine;

public class PreviewReceiver : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EnemyCore owner;
    
    private void OnTriggerEnter(Collider other)
    {
        // 嘗試從碰到的物件上獲取 DamageDealer
        PreviewDealer dealer = other.GetComponent<PreviewDealer>();

        if (dealer != null)
        {
            owner.ToPreview();
        }
        // 如果對方是個 DamageDealer
    }

}
