using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    private Enemy enemy;
    private Slider slider;

    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        slider = GetComponent<Slider>();

        if (enemy == null)
        {
            return;
        }

        slider.maxValue = enemy.maxHp;
    }

    void Update()
    {
        if (enemy == null)
            return;

        slider.value = enemy.hp;
    }
}