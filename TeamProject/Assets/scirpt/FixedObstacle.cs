using UnityEngine;

public class FixedObstacle : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageToPlayer = 10;
    public float knockbackForce = 0f; // 0이면 넉백 없음

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only affect player
        if (collision.CompareTag("Player"))
        {
            // Damage player
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageToPlayer);
            }

            // Optional knockback
            if (knockbackForce > 0)
            {
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}
