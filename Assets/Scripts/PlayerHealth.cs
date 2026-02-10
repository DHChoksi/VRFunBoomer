using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public enum PlayerState
    {
        Normal,
        Hurt,
        Dead
    }

    [Header("State")]
    public PlayerState currentState = PlayerState.Normal;

    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth;
    [SerializeField] float enemyDamage = 10f;

    [Header("UI")]
    [SerializeField] Slider healthSlider;

    [Header("Hurt Effect (Vignette)")]
    [SerializeField] Volume postProcessVolume;
    [SerializeField] float vignetteMin = 0f;
    [SerializeField] float vignetteMax = 0.45f;
    [SerializeField] float vignetteFadeSpeed = 3f;
    [SerializeField] float hurtDuration = 1f;

    [Header("Movement")]
    [SerializeField] HeadBobMoveProvider movement;

    Vignette vignette;

    void Awake()
    {
        currentHealth = maxHealth;

        if (healthSlider)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (postProcessVolume &&
            postProcessVolume.profile.TryGet(out vignette))
        {
            vignette.intensity.Override(vignetteMin);
        }
        else
        {
            Debug.LogError("Vignette not found in Volume!");
        }
    }

    // ------------------------------------------------
    // CALLED BY ENEMY
    // ------------------------------------------------
    public void OnEnemyAttack()
    {
        Debug.Log("PlayerHealth.OnEnemyAttack() called");

        if (currentState == PlayerState.Hurt || currentState == PlayerState.Dead)
            return;

        TakeDamage(enemyDamage);
    }

    // ------------------------------------------------
    // DAMAGE
    // ------------------------------------------------
    void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthSlider)
            healthSlider.value = currentHealth;

        Debug.Log($"Player Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(HurtRoutine());
    }

    // ------------------------------------------------
    // HURT STATE
    // ------------------------------------------------
    IEnumerator HurtRoutine()
    {
        currentState = PlayerState.Hurt;

        if (movement)
            movement.enabled = false;

        // Fade IN
        yield return StartCoroutine(FadeVignette(vignetteMax));

        yield return new WaitForSeconds(hurtDuration);

        // Fade OUT
        yield return StartCoroutine(FadeVignette(vignetteMin));

        if (movement)
            movement.enabled = true;

        currentState = PlayerState.Normal;
    }

    // ------------------------------------------------
    // VIGNETTE FADE
    // ------------------------------------------------
    IEnumerator FadeVignette(float target)
    {
        if (vignette == null)
            yield break;

        float start = vignette.intensity.value;

        while (!Mathf.Approximately(vignette.intensity.value, target))
        {
            float value = Mathf.MoveTowards(
                vignette.intensity.value,
                target,
                Time.deltaTime * vignetteFadeSpeed
            );

            vignette.intensity.Override(value);
            yield return null;
        }
    }

    // ------------------------------------------------
    // DEATH
    // ------------------------------------------------
    void Die()
    {
        Debug.Log("Player died");

        currentState = PlayerState.Dead;

        if (movement)
            movement.enabled = false;

        if (vignette != null)
            vignette.intensity.Override(vignetteMax);
    }
}
