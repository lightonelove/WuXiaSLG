using UnityEngine;
using UnityEngine.UI; // 引用UI命名空間
using System.Collections.Generic;
using System.Linq;

namespace Wuxia.GameCore
{
    public class TurnOrderIcon : MonoBehaviour
    {
        [Tooltip("用來顯示角色頭像的Image元件")] [SerializeField]
        private Image characterPortraitImage;

        /// <summary>
        /// 設定此圖示要顯示的角色資料。
        /// </summary>
        /// <param name="character">要顯示的角色</param>
        public void Setup(CombatEntity character)
        {
            if (character == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (characterPortraitImage != null && character.PortraitIcon != null)
            {
                characterPortraitImage.sprite = character.PortraitIcon;
            }
            else
            {
                // 如果沒有設定圖片，可以顯示一個預設顏色或隱藏Image
                if (characterPortraitImage != null) characterPortraitImage.color = Color.gray;
            }
        }
    }
}