using UnityEngine;
using UnityEngine.UI;

public class HP : MonoBehaviour
{
    public Player player;

    // óŒ(ëO)
    public Slider hpBar;

    // â©êF(å„ÇÎ)
    public Slider delayedBar;

    public float smoothSpeed = 150f;

    void Start()
    {
        if (player == null || hpBar == null || delayedBar == null)
        {
            Debug.LogWarning("[HP] player / hpBar / delayedBar Ç™ñ¢ê›íËÇ≈Ç∑");
            enabled = false;
            return;
        }

        hpBar.maxValue = player.maxHp;
        delayedBar.maxValue = player.maxHp;

        hpBar.value = player.hp;
        delayedBar.value = player.hp;
    }

    void Update()
    {
        if (player == null)
            return;

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