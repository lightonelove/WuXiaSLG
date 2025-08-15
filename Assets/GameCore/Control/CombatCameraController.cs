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
        
        [Header("縮放設定")]
        [SerializeField] private float zoomSpeed = 2f; // 縮放速度
        [SerializeField] private float zoomSmoothTime = 0.1f; // 縮放平滑時間
        [Space]
        [Header("Orthographic 模式設定")]
        [SerializeField] private float orthoSizeMin = 3f; // 最小正交大小
        [SerializeField] private float orthoSizeMax = 15f; // 最大正交大小
        [SerializeField] private float orthoSizeDefault = 10f; // 預設正交大小
        [Space]
        [Header("Perspective 模式設定")]
        [SerializeField] private float perspectiveFOVMin = 20f; // 最小視野角度
        [SerializeField] private float perspectiveFOVMax = 80f; // 最大視野角度
        [SerializeField] private float perspectiveFOVDefault = 60f; // 預設視野角度
        [SerializeField] private float perspectiveDistanceMin = 5f; // 最小相機距離
        [SerializeField] private float perspectiveDistanceMax = 30f; // 最大相機距離
        
        private Camera mainCamera;
        private Vector3 targetPosition;
        private Vector3 currentVelocity;
        private Vector3 initialPosition;
        
        // 縮放相關變數
        private float targetOrthoSize;
        private float currentOrthoSize;
        private float orthoSizeVelocity;
        
        private float targetPerspectiveFOV;
        private float currentPerspectiveFOV;
        private float fovVelocity;
        
        private float targetCameraDistance;
        private float currentCameraDistance;
        private float distanceVelocity;
        
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
            
            // 初始化縮放值
            if (mainCamera.orthographic)
            {
                targetOrthoSize = orthoSizeDefault;
                currentOrthoSize = orthoSizeDefault;
                mainCamera.orthographicSize = orthoSizeDefault;
            }
            else
            {
                targetPerspectiveFOV = perspectiveFOVDefault;
                currentPerspectiveFOV = perspectiveFOVDefault;
                mainCamera.fieldOfView = perspectiveFOVDefault;
                
                // 計算初始距離
                currentCameraDistance = cameraHeight;
                targetCameraDistance = cameraHeight;
            }
        }
        
        private void Update()
        {
            HandleEdgeScrolling();
            HandleZoom();
            MoveCamera();
            ApplyZoom();
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
                
                // 根據相機模式調整高度
                if (mainCamera.orthographic)
                {
                    newTargetPosition.y = cameraHeight;
                }
                else
                {
                    // Perspective 模式時，高度根據縮放調整
                    newTargetPosition.y = currentCameraDistance;
                }
                
                targetPosition = newTargetPosition;
            }
        }
        
        private void HandleZoom()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                if (mainCamera.orthographic)
                {
                    // Orthographic 模式：調整 orthographicSize
                    targetOrthoSize -= scrollInput * zoomSpeed * 10f;
                    targetOrthoSize = Mathf.Clamp(targetOrthoSize, orthoSizeMin, orthoSizeMax);
                }
                else
                {
                    // Perspective 模式：可以選擇調整 FOV 或相機距離
                    // 這裡我們同時調整兩者以獲得更好的效果
                    
                    // 調整 FOV
                    targetPerspectiveFOV -= scrollInput * zoomSpeed * 20f;
                    targetPerspectiveFOV = Mathf.Clamp(targetPerspectiveFOV, perspectiveFOVMin, perspectiveFOVMax);
                    
                    // 調整相機距離
                    targetCameraDistance -= scrollInput * zoomSpeed * 5f;
                    targetCameraDistance = Mathf.Clamp(targetCameraDistance, perspectiveDistanceMin, perspectiveDistanceMax);
                    
                    // 更新目標位置的高度
                    targetPosition.y = targetCameraDistance;
                }
            }
        }
        
        private void ApplyZoom()
        {
            if (mainCamera.orthographic)
            {
                // 平滑調整 Orthographic Size
                currentOrthoSize = Mathf.SmoothDamp(currentOrthoSize, targetOrthoSize, ref orthoSizeVelocity, zoomSmoothTime);
                mainCamera.orthographicSize = currentOrthoSize;
            }
            else
            {
                // 平滑調整 Perspective FOV
                currentPerspectiveFOV = Mathf.SmoothDamp(currentPerspectiveFOV, targetPerspectiveFOV, ref fovVelocity, zoomSmoothTime);
                mainCamera.fieldOfView = currentPerspectiveFOV;
                
                // 平滑調整相機距離
                currentCameraDistance = Mathf.SmoothDamp(currentCameraDistance, targetCameraDistance, ref distanceVelocity, zoomSmoothTime);
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
            
            // 重置縮放
            if (mainCamera.orthographic)
            {
                targetOrthoSize = orthoSizeDefault;
            }
            else
            {
                targetPerspectiveFOV = perspectiveFOVDefault;
                targetCameraDistance = cameraHeight;
                targetPosition.y = cameraHeight;
            }
        }
        
        // 公開方法：重置縮放
        public void ResetZoom()
        {
            if (mainCamera.orthographic)
            {
                targetOrthoSize = orthoSizeDefault;
            }
            else
            {
                targetPerspectiveFOV = perspectiveFOVDefault;
                targetCameraDistance = cameraHeight;
                targetPosition.y = cameraHeight;
            }
        }
        
        // 公開方法：設定縮放等級（0-1）
        public void SetZoomLevel(float normalizedZoom)
        {
            normalizedZoom = Mathf.Clamp01(normalizedZoom);
            
            if (mainCamera.orthographic)
            {
                targetOrthoSize = Mathf.Lerp(orthoSizeMin, orthoSizeMax, 1f - normalizedZoom);
            }
            else
            {
                targetPerspectiveFOV = Mathf.Lerp(perspectiveFOVMax, perspectiveFOVMin, normalizedZoom);
                targetCameraDistance = Mathf.Lerp(perspectiveDistanceMax, perspectiveDistanceMin, normalizedZoom);
                targetPosition.y = targetCameraDistance;
            }
        }
        
        // 公開方法：聚焦到特定位置
        public void FocusOnPosition(Vector3 worldPosition)
        {
            float height = mainCamera.orthographic ? cameraHeight : currentCameraDistance;
            Vector3 newPosition = new Vector3(worldPosition.x, height, worldPosition.z);
            
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