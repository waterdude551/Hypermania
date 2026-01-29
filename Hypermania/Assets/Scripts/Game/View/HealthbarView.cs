using UnityEngine;
using UnityEngine.UI;

namespace Game.View
{
    [RequireComponent(typeof(Slider))]
    public class HealthBarView : MonoBehaviour
    {
        public void SetMaxHealth(float health)
        {
            Slider slider = GetComponent<Slider>();
            slider.maxValue = health;
            slider.value = health;
        }

        public void SetHealth(float health)
        {
            Slider slider = GetComponent<Slider>();
            slider.value = health;
        }
    }
}
