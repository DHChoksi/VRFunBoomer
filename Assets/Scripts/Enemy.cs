using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Follow,
        Attack,
        Dead
    }

    [Header("State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("References")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Animator animator;

    [Header("Patrol")]
    [SerializeField] Transform pointA;
    [SerializeField] Transform pointB;
    Transform currentPatrolTarget;

    [Header("Detection")]
    [SerializeField] float detectionRange = 10f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] LayerMask playerLayer;

    [Header("Attack")]
    [SerializeField] float attackCooldown = 1.5f;
    bool canAttack = true;

    [Header("Death")]
    [SerializeField] GameObject enemyRoot;

    Transform player;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        currentPatrolTarget = pointA;
    }

    void Update()
    {
        if (currentState == EnemyState.Dead)
            return;

        DetectPlayer();

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Follow:
                Follow();
                break;

            case EnemyState.Attack:
                TryAttack();
                break;
        }
    }

    // ------------------------------------------------
    // PLAYER DETECTION (OverlapSphere from enemy)
    // ------------------------------------------------
    void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRange,
            playerLayer
        );

        if (hits.Length == 0)
        {
            player = null;
            currentState = EnemyState.Patrol;
            return;
        }

        player = hits[0].transform;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
            currentState = EnemyState.Attack;
        else
            currentState = EnemyState.Follow;
    }

    // ------------------------------------------------
    // PATROL
    // ------------------------------------------------
    void Patrol()
    {
        agent.isStopped = false;
        agent.SetDestination(currentPatrolTarget.position);

        if (Vector3.Distance(transform.position, currentPatrolTarget.position) < 0.5f)
        {
            currentPatrolTarget =
                currentPatrolTarget == pointA ? pointB : pointA;
        }
    }

    // ------------------------------------------------
    // FOLLOW
    // ------------------------------------------------
    void Follow()
    {
        if (!player) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // ------------------------------------------------
    // ATTACK (ANIMATION-DRIVEN)
    // ------------------------------------------------
    void TryAttack()
    {
        if (!canAttack || player == null)
            return;

        Debug.Log("Enemy ATTACK triggered");

        Transform playerRoot = player.root;

        var health = playerRoot.GetComponentInChildren<PlayerHealth>();
        if (health == null)
        {
            Debug.LogError("❌ PlayerHealth NOT FOUND on XR player!");
            return;
        }

        Debug.Log("✅ PlayerHealth FOUND");
        health.OnEnemyAttack();

        animator.SetTrigger("Attack");
        StartCoroutine(AttackCooldown());
    }


    System.Collections.IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // ------------------------------------------------
    // DEATH (Any State → Die)
    // ------------------------------------------------
    public void OnBombHit()
    {
        if (currentState == EnemyState.Dead)
            return;

        currentState = EnemyState.Dead;

        agent.isStopped = true;
        agent.enabled = false;

        animator.SetTrigger("Die");

        Invoke(nameof(DisableEnemy), 2f);
    }

    void DisableEnemy()
    {
        if (enemyRoot)
            enemyRoot.SetActive(false);
    }

    // ------------------------------------------------
    // DEBUG
    // ------------------------------------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
