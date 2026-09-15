using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int hp = 4000;
  
    public void TakeDamage(int damage)
    {
        hp -= damage;

        Debug.Log("“GHP : " +  hp);

        if (hp <= 0 )
        {
            hp = 0;
            Debug.Log("“G‚ð“|‚µ‚½");
            Destroy(gameObject);
        }
    }
}
