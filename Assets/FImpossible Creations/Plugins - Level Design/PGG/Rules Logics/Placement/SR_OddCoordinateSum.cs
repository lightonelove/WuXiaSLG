using UnityEngine;
using FIMSpace.Generating;
using FIMSpace.Generating.Rules;

namespace GameCore.PGGRules
{
    /// <summary>
    /// 自定義 Spawn Rule：當 X 和 Z 座標相加為奇數時才允許生成
    /// </summary>
    public class SR_OddCoordinateSum : SpawnRuleBase, ISpawnProcedureType
    {
        public override string TitleName() { return "奇數座標和檢查"; }
        public override string Tooltip() { return "當 Cell 的 X 座標和 Z 座標相加為奇數時才允許生成"; }
        
        // 設定這是一個 Rule 類型（條件檢查）
        public EProcedureType Type { get { return EProcedureType.Rule; } }

        [Header("座標檢查設定")]
        [Tooltip("是否使用世界座標而非格子座標")]
        public bool UseWorldPosition = false;
        
        [Tooltip("座標偏移量（可選）")]
        public Vector3Int CoordinateOffset = Vector3Int.zero;
        
        [Header("進階選項")]
        [Tooltip("是否反轉條件（偶數時生成）")]
        public bool InvertCondition = false;

        /// <summary>
        /// 檢查規則的主要方法
        /// </summary>
        public override void CheckRuleOn(FieldModification mod, ref SpawnData spawn, FieldSetup preset, FieldCell cell, FGenGraph<FieldCell, FGenPoint> grid, Vector3? restrictDirection = null)
        {
            // 獲取當前 Cell 的座標
            Vector3Int cellCoords;
            
            if (UseWorldPosition)
            {
                // 使用世界座標
                Vector3 worldPos = spawn.GetWorldPositionWithFullOffset();
                cellCoords = new Vector3Int(
                    Mathf.RoundToInt(worldPos.x),
                    Mathf.RoundToInt(worldPos.y),
                    Mathf.RoundToInt(worldPos.z)
                );
            }
            else
            {
                // 使用格子座標
                cellCoords = cell.Pos;
            }
            
            // 加上偏移量
            cellCoords += CoordinateOffset;
            
            // 計算 X + Z 的和
            int coordinateSum = cellCoords.x + cellCoords.z;
            
            // 判斷是否為奇數
            bool isOdd = (coordinateSum % 2) != 0;
            
            // 根據是否反轉條件來設定結果
            if (InvertCondition)
            {
                CellAllow = !isOdd; // 偶數時允許
            }
            else
            {
                CellAllow = isOdd;  // 奇數時允許
            }
            
            // Debug 輸出（可選）
            if (_EditorDebug)
            {
                Debug.Log($"[OddCoordinateSum] Cell({cellCoords.x}, {cellCoords.z}) Sum={coordinateSum} IsOdd={isOdd} Allow={CellAllow}");
            }
        }

        /// <summary>
        /// 每次開始檢查序列時重置規則
        /// </summary>
        public override void ResetRule(FGenGraph<FieldCell, FGenPoint> grid, FieldSetup preset)
        {
            base.ResetRule(grid, preset);
            CellAllow = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在編輯器中顯示額外資訊
        /// </summary>
        public override void NodeFooter(UnityEditor.SerializedObject so, FieldModification mod)
        {
            base.NodeFooter(so, mod);
            
            // 顯示範例說明
            UnityEditor.EditorGUILayout.HelpBox(
                "範例：\n" +
                "• Cell(0,1): 0+1=1 (奇數) → 生成\n" +
                "• Cell(1,1): 1+1=2 (偶數) → 不生成\n" +
                "• Cell(2,3): 2+3=5 (奇數) → 生成",
                UnityEditor.MessageType.Info);
        }
#endif
    }
}