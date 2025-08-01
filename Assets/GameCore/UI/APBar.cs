using UnityEngine;
using UnityEngine.UI;

public class APBar : MonoBehaviour
{
    public Slider slider;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void UpdateApBarUI(float ap)
    {
        slider.value = ap;
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
