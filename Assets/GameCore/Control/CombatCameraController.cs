using System.Collections;
using UnityEngine;

namespace Wuxia.GameCore
{
    public class CombatCameraController : MonoBehaviour
    {
        [Header("邊界觸發設定")]
        [SerializeField] private float edgeThreshold = 10f; // 距離邊界多少像素觸發移動
        [SerializeField] private float edgeScrollSpeed = 10f; // 邊界滾動速度
        
        [Header("相機移動設定")]
        [SerializeField] private float smoothTime = 0.1f; // 平滑移動時間
        [SerializeField] private Vector2 movementLimits = new Vector2(50f, 50f); // X和Z軸的移動限制
        
        [Header("中鍵拖曳設定")]
        [SerializeField] private float dragSensitivity = 0.5f; // 拖曳靈敏度
        [SerializeField] private bool invertDragX = false; // 反轉X軸拖曳方向
        [SerializeField] private bool invertDragY = false; // 反轉Y軸拖曳方向
        
        [Header("目標跟隨設定")]
        [SerializeField] private bool enableTargetFollow = true; // 是否啟用目標跟隨
        [SerializeField] private float followSmoothTime = 0.3f; // 跟隨平滑時間
        [SerializeField] private Vector3 followOffset = new Vector3(0, 0, -5f); // 跟隨偏移（相對於目標）
        [SerializeField] private bool lockUserControl = false; // 跟隨時是否鎖定用戶控制
        
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
        
        // 中鍵拖曳相關變數
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private Vector3 dragStartPosition;
        
        // 目標跟隨相關變數
        private Transform followTarget;
        private bool isFollowing = false;
        private Vector3 followVelocity;
        
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
            // 如果正在跟隨目標
            if (isFollowing && followTarget != null)
            {
                HandleTargetFollow();
                
                // 如果鎖定用戶控制，跳過其他輸入處理
                if (lockUserControl)
                {
                    ApplyZoom();
                    return;
                }
            }
            
            HandleMiddleMouseDrag();
            
            // 只有在沒有拖曳且沒有跟隨目標時才處理邊界滾動
            if (!isDragging && !isFollowing)
            {
                HandleEdgeScrolling();
            }
            
            HandleZoom();
            MoveCamera();
            ApplyZoom();
        }
        
        private void HandleMiddleMouseDrag()
        {
            // 檢測中鍵按下
            if (Input.GetMouseButtonDown(2)) // 2 是中鍵
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
                dragStartPosition = transform.position;
            }
            
            // 處理拖曳
            if (isDragging && Input.GetMouseButton(2))
            {
                Vector3 currentMousePosition = Input.mousePosition;
                Vector3 mouseDelta = currentMousePosition - lastMousePosition;
                
                // 根據相機模式計算移動量
                float moveScale = dragSensitivity;
                
                if (mainCamera.orthographic)
                {
                    // Orthographic 模式：根據 orthographicSize 調整移動速度
                    moveScale *= currentOrthoSize / orthoSizeDefault;
                }
                else
                {
                    // Perspective 模式：根據相機距離調整移動速度
                    moveScale *= currentCameraDistance / cameraHeight;
                }
                
                // 計算世界空間的移動方向
                Vector3 moveDirection = Vector3.zero;
                
                // X軸移動（左右拖曳）
                float xMove = mouseDelta.x * moveScale * 0.01f;
                if (invertDragX) xMove = -xMove;
                moveDirection.x = -xMove; // 負號讓拖曳方向更自然
                
                // Z軸移動（上下拖曳）
                float zMove = mouseDelta.y * moveScale * 0.01f;
                if (invertDragY) zMove = -zMove;
                moveDirection.z = -zMove; // 負號讓拖曳方向更自然
                
                // 應用移動
                Vector3 newTargetPosition = targetPosition + moveDirection;
                
                // 限制移動範圍
                newTargetPosition.x = Mathf.Clamp(newTargetPosition.x,
                    initialPosition.x - movementLimits.x,
                    initialPosition.x + movementLimits.x);
                    
                newTargetPosition.z = Mathf.Clamp(newTargetPosition.z,
                    initialPosition.z - movementLimits.y,
                    initialPosition.z + movementLimits.y);
                
                // 保持高度
                if (mainCamera.orthographic)
                {
                    newTargetPosition.y = cameraHeight;
                }
                else
                {
                    newTargetPosition.y = currentCameraDistance;
                }
                
                targetPosition = newTargetPosition;
                lastMousePosition = currentMousePosition;
            }
            
            // 檢測中鍵釋放
            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
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
        
        private void HandleTargetFollow()
        {
            if (followTarget == null)
            {
                StopFollowing();
                return;
            }
            
            // 計算跟隨目標位置
            Vector3 desiredPosition = followTarget.position + followOffset;
            
            // 根據相機模式調整高度
            if (mainCamera.orthographic)
            {
                desiredPosition.y = cameraHeight;
            }
            else
            {
                desiredPosition.y = currentCameraDistance;
            }
            
            // 應用移動限制
            desiredPosition.x = Mathf.Clamp(desiredPosition.x,
                initialPosition.x - movementLimits.x,
                initialPosition.x + movementLimits.x);
                
            desiredPosition.z = Mathf.Clamp(desiredPosition.z,
                initialPosition.z - movementLimits.y,
                initialPosition.z + movementLimits.y);
            
            // 平滑移動到目標位置
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
            
            // 更新 targetPosition 以保持同步
            targetPosition = desiredPosition;
        }
        
        private void MoveCamera()
        {
            // 如果正在跟隨目標，跳過一般移動（已在 HandleTargetFollow 處理）
            if (isFollowing && followTarget != null)
            {
                return;
            }
            
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
            Vector3 newPosition = new Vector3(worldPosition.x, height, worldPosition.z) + followOffset;
            
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
        
        // 公開方法：開始跟隨目標
        public void StartFollowing(Transform target, bool lockControl = false)
        {
            if (!enableTargetFollow) return;
            
            followTarget = target;
            isFollowing = true;
            lockUserControl = lockControl;
            isDragging = false; // 停止任何進行中的拖曳
            
            // 立即聚焦到目標位置（包含 offset）
            if (target != null)
            {
                Vector3 targetPositionWithOffset = target.position + followOffset;
                FocusOnPosition(targetPositionWithOffset);
            }
        }
        
        // 公開方法：停止跟隨
        public void StopFollowing()
        {
            isFollowing = false;
            followTarget = null;
            lockUserControl = false;
            followVelocity = Vector3.zero;
        }
        
        // 公開方法：檢查是否正在跟隨
        public bool IsFollowing()
        {
            return isFollowing && followTarget != null;
        }
        
        // 公開方法：設定是否鎖定用戶控制
        public void SetLockUserControl(bool locked)
        {
            lockUserControl = locked;
        }
        
        // 攻擊縮放相關變數
        private Coroutine attackZoomCoroutine;
        private bool isAttackZooming = false;
        private float attackZoomStartOrthoSize;
        private float attackZoomStartFOV;
        private float attackZoomStartDistance;
        
        // 公開方法：開始攻擊縮放（配合狀態機使用）
        public void StartAttackZoom(float zoomPercent = 0.85f, float duration = 0.5f)
        {
            if (isAttackZooming) return;
            
            isAttackZooming = true;
            
            // 記錄開始時的縮放值
            attackZoomStartOrthoSize = targetOrthoSize;
            attackZoomStartFOV = targetPerspectiveFOV;
            attackZoomStartDistance = targetCameraDistance;
            
            // 停止之前的縮放協程
            if (attackZoomCoroutine != null)
            {
                StopCoroutine(attackZoomCoroutine);
            }
            
            attackZoomCoroutine = StartCoroutine(AnimateAttackZoom(zoomPercent, duration, true));
        }
        
        // 公開方法：結束攻擊縮放（配合狀態機使用）
        public void EndAttackZoom(float duration = 0.5f)
        {
            if (!isAttackZooming) return;
            
            isAttackZooming = false;
            
            // 停止之前的縮放協程
            if (attackZoomCoroutine != null)
            {
                StopCoroutine(attackZoomCoroutine);
            }
            
            attackZoomCoroutine = StartCoroutine(AnimateAttackZoom(1f, duration, false));
        }
        
        // 執行攻擊縮放動畫
        private IEnumerator AnimateAttackZoom(float zoomPercent, float duration, bool isZoomIn)
        {
            float elapsed = 0f;
            
            // 獲取起始值和目標值
            float startOrthoSize = currentOrthoSize;
            float startFOV = currentPerspectiveFOV;
            float startDistance = currentCameraDistance;
            
            float targetOrtho, targetFOV, targetDistance;
            
            if (isZoomIn)
            {
                // 縮放進入
                targetOrtho = attackZoomStartOrthoSize * zoomPercent;
                targetFOV = attackZoomStartFOV * zoomPercent;
                targetDistance = attackZoomStartDistance * zoomPercent;
            }
            else
            {
                // 縮放恢復
                targetOrtho = attackZoomStartOrthoSize;
                targetFOV = attackZoomStartFOV;
                targetDistance = attackZoomStartDistance;
            }
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                
                if (mainCamera.orthographic)
                {
                    targetOrthoSize = Mathf.Lerp(startOrthoSize, targetOrtho, t);
                }
                else
                {
                    targetPerspectiveFOV = Mathf.Lerp(startFOV, targetFOV, t);
                    targetCameraDistance = Mathf.Lerp(startDistance, targetDistance, t);
                }
                
                yield return null;
            }
            
            // 確保達到目標值
            if (mainCamera.orthographic)
            {
                targetOrthoSize = targetOrtho;
            }
            else
            {
                targetPerspectiveFOV = targetFOV;
                targetCameraDistance = targetDistance;
            }
            
            attackZoomCoroutine = null;
        }
        
        // 公開方法：執行臨時縮放效果（用於戰鬥特效等）
        public IEnumerator TemporaryZoomEffect(float zoomPercent, float duration, float holdTime = 0f)
        {
            // 記錄原始縮放值
            float originalOrthoSize = targetOrthoSize;
            float originalFOV = targetPerspectiveFOV;
            float originalDistance = targetCameraDistance;
            
            // 計算目標縮放值（zoomPercent: 1.0 = 100%原始大小, 0.85 = 85%）
            float targetZoomOrtho = originalOrthoSize * zoomPercent;
            float targetZoomFOV = originalFOV * zoomPercent;
            float targetZoomDistance = originalDistance * zoomPercent;
            
            // 縮放進入
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t); // 使用 SmoothStep 讓動畫更平滑
                
                if (mainCamera.orthographic)
                {
                    targetOrthoSize = Mathf.Lerp(originalOrthoSize, targetZoomOrtho, t);
                }
                else
                {
                    targetPerspectiveFOV = Mathf.Lerp(originalFOV, targetZoomFOV, t);
                    targetCameraDistance = Mathf.Lerp(originalDistance, targetZoomDistance, t);
                }
                
                yield return null;
            }
            
            // 確保達到目標值
            if (mainCamera.orthographic)
            {
                targetOrthoSize = targetZoomOrtho;
            }
            else
            {
                targetPerspectiveFOV = targetZoomFOV;
                targetCameraDistance = targetZoomDistance;
            }
            
            // 保持縮放狀態
            if (holdTime > 0f)
            {
                yield return new WaitForSeconds(holdTime);
            }
            
            // 縮放恢復
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                
                if (mainCamera.orthographic)
                {
                    targetOrthoSize = Mathf.Lerp(targetZoomOrtho, originalOrthoSize, t);
                }
                else
                {
                    targetPerspectiveFOV = Mathf.Lerp(targetZoomFOV, originalFOV, t);
                    targetCameraDistance = Mathf.Lerp(targetZoomDistance, originalDistance, t);
                }
                
                yield return null;
            }
            
            // 確保恢復到原始值
            if (mainCamera.orthographic)
            {
                targetOrthoSize = originalOrthoSize;
            }
            else
            {
                targetPerspectiveFOV = originalFOV;
                targetCameraDistance = originalDistance;
            }
        }
        
        /// <summary>
        /// 投射物技能相機控制協程
        /// </summary>
        /// <param name="shooterPos">發射者位置</param>
        /// <param name="targetPos">目標位置</param>
        /// <param name="moveDuration">移動時間</param>
        /// <param name="waitTime">等待時間</param>
        /// <returns></returns>
        public IEnumerator ProjectileSkillCameraControl(Vector3 shooterPos, Vector3 targetPos, float moveDuration = 0.3f, float waitTime = 0.1f)
        {
            // 保存當前的目標位置
            Vector3 originalTargetPosition = targetPosition;
            
            // 計算發射者和目標之間的中間點
            Vector3 midPoint = (shooterPos + targetPos) * 0.5f;
            
            // 計算相機的目標位置（保持當前高度）
            Vector3 cameraTargetPos = new Vector3(midPoint.x, targetPosition.y, midPoint.z) + followOffset;
            
            // 應用移動限制
            cameraTargetPos.x = Mathf.Clamp(cameraTargetPos.x,
                initialPosition.x - movementLimits.x,
                initialPosition.x + movementLimits.x);
                
            cameraTargetPos.z = Mathf.Clamp(cameraTargetPos.z,
                initialPosition.z - movementLimits.y,
                initialPosition.z + movementLimits.y);
            
            // 清除跟隨狀態
            StopFollowing();
            
            // 設定目標位置，讓相機自然移動過去
            targetPosition = cameraTargetPos;
            
            // 等待相機移動到位
            yield return new WaitForSeconds(moveDuration);
            
            // 等待一小段時間讓玩家看清楚場景
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
            
            // 可以在這裡觸發技能動畫的回調
            // 投射物技能相機控制完成，可以開始播放技能動畫
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