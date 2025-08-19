using UnityEngine;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 用於視覺化物件的 Forward 方向
    /// 可以掛在任何 GameObject 上（包括 CombatEntity）
    /// </summary>
    public class ForwardDirectionVisualizer : MonoBehaviour
    {
        [Header("視覺化設定")]
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private bool showInGame = false;
        [SerializeField] private float lineLength = 3f;
        [SerializeField] private Color lineColor = Color.blue;
        [SerializeField] private float lineThickness = 0.1f;
        
        [Header("箭頭設定")]
        [SerializeField] private bool showArrowHead = true;
        [SerializeField] private float arrowHeadLength = 0.5f;
        [SerializeField] private float arrowHeadAngle = 30f;
        
        [Header("高度偏移")]
        [SerializeField] private float heightOffset = 0.1f; // 避免線條埋在地面下
        
        private LineRenderer lineRenderer;
        private CombatEntity combatEntity;
        
        private void Awake()
        {
            // 嘗試取得 CombatEntity（如果有的話）
            combatEntity = GetComponent<CombatEntity>();
            
            if (showInGame)
            {
                CreateLineRenderer();
            }
        }
        
        private void CreateLineRenderer()
        {
            // 建立 LineRenderer 元件
            GameObject lineObj = new GameObject("ForwardDirectionLine");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineThickness;
            lineRenderer.endWidth = lineThickness;
            lineRenderer.positionCount = 2;
            
            // 設定不受光照影響
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        private void Update()
        {
            if (showInGame && lineRenderer != null)
            {
                UpdateLineRenderer();
            }
        }
        
        private void UpdateLineRenderer()
        {
            Vector3 startPos = transform.position + Vector3.up * heightOffset;
            Vector3 endPos = startPos + transform.forward * lineLength;
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }
        
        private void OnDestroy()
        {
            if (lineRenderer != null)
            {
                if (lineRenderer.gameObject != null)
                {
                    Destroy(lineRenderer.gameObject);
                }
            }
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showInEditor) return;
            
            DrawDirectionGizmo();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (showInEditor) return; // 如果已經在 OnDrawGizmos 中繪製，就不要重複
            
            DrawDirectionGizmo();
        }
        
        private void DrawDirectionGizmo()
        {
            Gizmos.color = lineColor;
            
            Vector3 startPos = transform.position + Vector3.up * heightOffset;
            Vector3 endPos = startPos + transform.forward * lineLength;
            
            // 繪製主線
            Gizmos.DrawLine(startPos, endPos);
            
            // 繪製箭頭
            if (showArrowHead)
            {
                Vector3 arrowDirection = (endPos - startPos).normalized;
                
                // 計算箭頭的兩個點
                Quaternion leftRotation = Quaternion.Euler(0, -arrowHeadAngle, 0);
                Quaternion rightRotation = Quaternion.Euler(0, arrowHeadAngle, 0);
                
                Vector3 leftArrow = leftRotation * -arrowDirection * arrowHeadLength;
                Vector3 rightArrow = rightRotation * -arrowDirection * arrowHeadLength;
                
                Gizmos.DrawLine(endPos, endPos + leftArrow);
                Gizmos.DrawLine(endPos, endPos + rightArrow);
            }
            
            // 如果有 CombatEntity，顯示額外資訊
            if (combatEntity != null)
            {
                // 在終點顯示實體名稱
                UnityEditor.Handles.Label(endPos + Vector3.up * 0.5f, 
                    $"{combatEntity.Name}\nFaction: {combatEntity.Faction}");
            }
        }
        #endif
        
        /// <summary>
        /// 設定線條顏色
        /// </summary>
        public void SetLineColor(Color color)
        {
            lineColor = color;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
        
        /// <summary>
        /// 設定線條長度
        /// </summary>
        public void SetLineLength(float length)
        {
            lineLength = Mathf.Max(0.1f, length);
        }
        
        /// <summary>
        /// 切換遊戲中顯示
        /// </summary>
        public void ToggleGameDisplay(bool show)
        {
            showInGame = show;
            
            if (showInGame && lineRenderer == null)
            {
                CreateLineRenderer();
            }
            else if (!showInGame && lineRenderer != null)
            {
                Destroy(lineRenderer.gameObject);
                lineRenderer = null;
            }
        }
    }
}