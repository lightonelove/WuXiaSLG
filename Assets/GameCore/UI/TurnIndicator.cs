using UnityEngine;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 回合指示器 - 在當前回合角色腳下顯示圓圈
    /// </summary>
    public class TurnIndicator : MonoBehaviour
    {
        [Header("指示器設定")]
        [SerializeField] private float radius = 1.5f;
        [SerializeField] private float height = 0.1f;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private int segments = 64;
        
        [Header("顏色設定")]
        [SerializeField] private Color allyColor = new Color(0f, 1f, 0f, 0.8f);
        [SerializeField] private Color enemyColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinAlpha = 0.3f;
        [SerializeField] private float pulseMaxAlpha = 0.8f;
        
        [Header("動畫設定")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 30f;
        
        private GameObject indicatorObject;
        private LineRenderer lineRenderer;
        private Material indicatorMaterial;
        private CombatEntity targetEntity;
        private bool isActive = false;
        private float pulseTimer = 0f;
        
        void Awake()
        {
            CreateIndicatorObject();
        }
        
        private void CreateIndicatorObject()
        {
            indicatorObject = new GameObject("TurnIndicatorVisual");
            indicatorObject.transform.SetParent(transform);
            indicatorObject.transform.localPosition = Vector3.zero;
            
            lineRenderer = indicatorObject.AddComponent<LineRenderer>();
            
            indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material = indicatorMaterial;
            
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            
            lineRenderer.positionCount = segments + 1;
            
            GenerateCirclePoints();
            
            indicatorObject.SetActive(false);
        }
        
        private void GenerateCirclePoints()
        {
            float angleStep = 360f / segments;
            Vector3[] positions = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                positions[i] = new Vector3(x, height, z);
            }
            
            lineRenderer.SetPositions(positions);
        }
        
        public void ShowIndicator(CombatEntity entity)
        {
            if (entity == null) return;
            
            targetEntity = entity;
            transform.position = entity.transform.position;
            
            bool isAlly = IsAllyEntity(entity);
            Color baseColor = isAlly ? allyColor : enemyColor;
            lineRenderer.startColor = baseColor;
            lineRenderer.endColor = baseColor;
            
            indicatorObject.SetActive(true);
            isActive = true;
            pulseTimer = 0f;
        }
        
        private bool IsAllyEntity(CombatEntity entity)
        {
            if (entity.GetComponent<CharacterCore>() != null)
            {
                return true; // 有 CharacterCore 的是友軍
            }
            
            if (entity.GetComponent<EnemyCore>() != null)
            {
                return entity.Faction == CombatEntityFaction.Ally; // 有 EnemyCore 的用 Faction 判斷
            }
            
            return false; // 其他情況視為敵方
        }
        
        public void HideIndicator()
        {
            indicatorObject.SetActive(false);
            isActive = false;
            targetEntity = null;
        }
        
        void Update()
        {
            if (!isActive || targetEntity == null) return;
            
            transform.position = targetEntity.transform.position;
            
            if (enablePulse)
            {
                UpdatePulse();
            }
            
            if (enableRotation)
            {
                UpdateRotation();
            }
        }
        
        private void UpdatePulse()
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 
                (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            
            Color currentColor = lineRenderer.startColor;
            currentColor.a = alpha;
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }
        
        private void UpdateRotation()
        {
            indicatorObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        public void SetRadius(float newRadius)
        {
            radius = newRadius;
            GenerateCirclePoints();
        }
        
        public void SetColors(Color allyCol, Color enemyCol)
        {
            allyColor = allyCol;
            enemyColor = enemyCol;
            
            if (isActive && targetEntity != null)
            {
                bool isAlly = IsAllyEntity(targetEntity);
                Color baseColor = isAlly ? allyColor : enemyColor;
                lineRenderer.startColor = baseColor;
                lineRenderer.endColor = baseColor;
            }
        }
        
        void OnDestroy()
        {
            if (indicatorMaterial != null)
            {
                DestroyImmediate(indicatorMaterial);
            }
        }
        
        void OnValidate()
        {
            if (Application.isPlaying && lineRenderer != null)
            {
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                GenerateCirclePoints();
            }
        }
    }
}