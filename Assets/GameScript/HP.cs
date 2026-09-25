using UnityEngine;
using UnityEngine.UI;

public class HP : MonoBehaviour
{
    public Player player;

    // óŒ
    public Slider hpBar;

    // â©êF
    public Slider delayedBar;

    public float smoothSpeed = 150f;

    void Start()
    {
        hpBar.maxValue = player.maxHp;
        delayedBar.maxValue = player.maxHp;

        hpBar.value = player.hp;
        delayedBar.value = player.hp;
    }

    void Update()
    {
        // óŒ
        hpBar.value = player.hp;

        // â©êF
        delayedBar.value =
            Mathf.MoveTowards(
                delayedBar.value,
                player.hp,
                smoothSpeed * Time.deltaTime);
    }
}