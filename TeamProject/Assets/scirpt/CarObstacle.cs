using UnityEngine;

public class CarObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] waypoints;    // Movement waypoints
    public float speed = 3f;         // Car moving speed
    public bool loop = true;         // Loop movement if true

    private int currentIndex = 0;    // Current waypoint index
    private int direction = 1;       // Direction for non-loop mode

    [Header("Player Damage Settings")]
    public int damageToPlayer = 10;  // Damage applied to player
    public float knockbackForce = 0f; // Knockback applied to the player

    private void Update()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < 0.05f)
        {
            if (loop)
            {
                // Move in cycle: 0 → 1 → 2 → ... → n → 0
                currentIndex = (currentIndex + 1) % waypoints.Length;
            }
            else
            {
                // Move back and forth: 0 → 1 → 2 → ... → n → ... → 2 → 1 → 0
                currentIndex += direction;
                if (currentIndex >= waypoints.Length || currentIndex < 0)
                {
                    direction *= -1;
                    currentIndex += direction * 2;
                }
            }
        }

        // Car rotates toward movement direction
        if (dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detect collision with player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageToPlayer);
            }

            // Apply knockback to player on collision
            if (knockbackForce > 0f)
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
