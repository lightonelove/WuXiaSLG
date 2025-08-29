# GridPainter 方形拖拉功能測試說明

## 功能概述
已為 GridPainter 新增方形拖拉功能，可以批量填滿或刪除 Cell 範圍。

## 新增的功能

### 1. 方形拖拉模式
- 在 GridPainter Inspector 中新增 "Rectangle Drag Mode" 切換選項
- 啟用後可以使用拖拉方式批量編輯 Cell

### 2. 操作方式
- **拖拉滑鼠左鍵**: 填滿範圍內的 Cell
- **拖拉滑鼠右鍵**: 刪除範圍內的 Cell

### 3. 視覺回饋
- 拖拉時會顯示半透明的預覽框
- 綠色表示填滿操作，紅色表示刪除操作

## 測試步驟

1. 創建一個新的 GameObject
2. 新增 GridPainter 組件
3. 設定 FieldPreset 資源
4. 在 Inspector 中啟用 Paint 模式
5. 啟用 "Rectangle Drag Mode"
6. 在 Scene View 中：
   - 按住滑鼠左鍵拖拉以填滿 Cell 範圍
   - 按住滑鼠右鍵拖拉以刪除 Cell 範圍

## 技術實現

### 新增的變數
- `_Editor_RectangleDragMode`: 啟用/停用方形拖拉模式
- `_Editor_DragStart`: 拖拉起始位置
- `_Editor_DragEnd`: 拖拉結束位置
- `_Editor_IsDragging`: 拖拉狀態
- `_Editor_DragStarted`: 拖拉開始標記

### 新增的方法
- `ProcessRectangleDragMode()`: 處理拖拉模式的輸入和視覺化
- `ExecuteRectangleOperation()`: 執行方形範圍的批量操作
- `DrawRectangleDragPreview()`: 繪製拖拉範圍預覽

## 注意事項
- 此功能只在 Paint 模式啟用時可用
- 支援 2D 和 3D 場景模式
- 拖拉範圍會自動計算最小/最大邊界
- 操作完成後會自動觸發 OnChange() 事件重新生成 Prefab