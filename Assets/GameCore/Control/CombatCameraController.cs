using UnityEngine;

namespace GameCore.Control
{
    public class CombatCameraController : MonoBehaviour
    {
        [Header("邊界觸發設定")]
        [SerializeField] private float edgeThreshold = 10f; // 距離邊界多少像素觸發移動
        [SerializeField] private float edgeScrollSpeed = 10f; // 邊界滾動速度
        
        [Header("相機移動設定")]
        [SerializeField] private float smoothTime = 0.1f; // 平滑移動時間
        [SerializeField] private Vector2 movementLimits = new Vector2(50f, 50f); // X和Z軸的移動限制
        
        [Header("高度設定")]
        [SerializeField] private float cameraHeight = 10f; // 相機高度
        
        private Camera mainCamera;
        private Vector3 targetPosition;
        private Vector3 currentVelocity;
        private Vector3 initialPosition;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
            }
            
            if (mainCamera == null)
            {
                Debug.LogError("CombatCameraController: 找不到相機組件！");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            initialPosition = transform.position;
            targetPosition = transform.position;
        }
        
        private void Update()
        {
            HandleEdgeScrolling();
            MoveCamera();
        }
        
        private void HandleEdgeScrolling()
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 moveDirection = Vector3.zero;
            
            // 處理滑鼠在視窗外的情況
            // 左邊界（包含視窗外）
            if (mousePosition.x <= edgeThreshold)
            {
                moveDirection.x = -1;
                // 如果滑鼠在視窗外，增加移動速度
                if (mousePosition.x < 0)
                {
                    moveDirection.x = -1 * (1 + Mathf.Abs(mousePosition.x) / edgeThreshold);
                }
            }
            // 右邊界（包含視窗外）
            else if (mousePosition.x >= Screen.width - edgeThreshold)
            {
                moveDirection.x = 1;
                // 如果滑鼠在視窗外，增加移動速度
                if (mousePosition.x > Screen.width)
                {
                    moveDirection.x = 1 * (1 + (mousePosition.x - Screen.width) / edgeThreshold);
                }
            }
            
            // 下邊界（包含視窗外）
            if (mousePosition.y <= edgeThreshold)
            {
                moveDirection.z = -1;
                // 如果滑鼠在視窗外，增加移動速度
                if (mousePosition.y < 0)
                {
                    moveDirection.z = -1 * (1 + Mathf.Abs(mousePosition.y) / edgeThreshold);
                }
            }
            // 上邊界（包含視窗外）
            else if (mousePosition.y >= Screen.height - edgeThreshold)
            {
                moveDirection.z = 1;
                // 如果滑鼠在視窗外，增加移動速度
                if (mousePosition.y > Screen.height)
                {
                    moveDirection.z = 1 * (1 + (mousePosition.y - Screen.height) / edgeThreshold);
                }
            }
            
            // 應用移動
            if (moveDirection != Vector3.zero)
            {
                Vector3 newTargetPosition = targetPosition + moveDirection.normalized * edgeScrollSpeed * Time.deltaTime;
                
                // 限制移動範圍
                newTargetPosition.x = Mathf.Clamp(newTargetPosition.x, 
                    initialPosition.x - movementLimits.x, 
                    initialPosition.x + movementLimits.x);
                    
                newTargetPosition.z = Mathf.Clamp(newTargetPosition.z, 
                    initialPosition.z - movementLimits.y, 
                    initialPosition.z + movementLimits.y);
                
                // 保持高度不變
                newTargetPosition.y = cameraHeight;
                
                targetPosition = newTargetPosition;
            }
        }
        
        private void MoveCamera()
        {
            // 平滑移動相機到目標位置
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }
        
        // 公開方法：重置相機位置
        public void ResetCameraPosition()
        {
            targetPosition = initialPosition;
        }
        
        // 公開方法：聚焦到特定位置
        public void FocusOnPosition(Vector3 worldPosition)
        {
            Vector3 newPosition = new Vector3(worldPosition.x, cameraHeight, worldPosition.z);
            
            // 應用限制
            newPosition.x = Mathf.Clamp(newPosition.x, 
                initialPosition.x - movementLimits.x, 
                initialPosition.x + movementLimits.x);
                
            newPosition.z = Mathf.Clamp(newPosition.z, 
                initialPosition.z - movementLimits.y, 
                initialPosition.z + movementLimits.y);
            
            targetPosition = newPosition;
        }
        
        // 公開方法：聚焦到特定物件
        public void FocusOnGameObject(GameObject target)
        {
            if (target != null)
            {
                FocusOnPosition(target.transform.position);
            }
        }
        
        // 在編輯器中顯示移動範圍
        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? initialPosition : transform.position;
            
            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(movementLimits.x * 2, 0.1f, movementLimits.y * 2);
            Gizmos.DrawWireCube(new Vector3(center.x, cameraHeight, center.z), size);
        }
    }
}