using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill; // this is just the colored bar that shrinks

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;

        fill.color = gradient.Evaluate(1f);
    }

    public void SetHealth(int health)
    {
        slider.value = health;

        if (health > 0)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
        else
        {
            fill.color = Color.gray; // dead state
        }
    }
}
