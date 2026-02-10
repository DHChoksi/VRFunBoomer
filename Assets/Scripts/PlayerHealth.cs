using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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

    [Header("Death Screen")]
    [SerializeField] Canvas gameOverCanvas;
    [SerializeField] float deathFadeSpeed = 0.75f;
    [SerializeField] Color deathRed = new Color(0.4f, 0f, 0f, 1f);
    [SerializeField] float reloadDelay = 2f;

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
            vignette.color.Override(Color.black);
        }
        else
        {
            Debug.LogError("Vignette not found in Volume!");
        }

        if (gameOverCanvas)
            gameOverCanvas.enabled = false;
    }

    // ------------------------------------------------
    // CALLED BY ENEMY
    // ------------------------------------------------
    public void OnEnemyAttack()
    {
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

        yield return StartCoroutine(FadeVignette(vignetteMax));
        yield return new WaitForSeconds(hurtDuration);
        yield return StartCoroutine(FadeVignette(vignetteMin));

        if (movement)
            movement.enabled = true;

        currentState = PlayerState.Normal;
    }

    // ------------------------------------------------
    // VIGNETTE FADE (NORMAL TIME)
    // ------------------------------------------------
    IEnumerator FadeVignette(float target)
    {
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
        if (currentState == PlayerState.Dead)
            return;

        currentState = PlayerState.Dead;

        if (movement)
            movement.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        // Dark red vignette
        vignette.color.Override(deathRed);

        // Fade to full black
        yield return StartCoroutine(FadeVignetteUnscaled(1f));

        if (gameOverCanvas)
            gameOverCanvas.enabled = true;

        // Freeze time
        Time.timeScale = 0f;

        // Wait while frozen
        yield return new WaitForSecondsRealtime(reloadDelay);

        // Reload
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------
    // VIGNETTE FADE (UNSCALED TIME)
    // ------------------------------------------------
    IEnumerator FadeVignetteUnscaled(float target)
    {
        while (!Mathf.Approximately(vignette.intensity.value, target))
        {
            float value = Mathf.MoveTowards(
                vignette.intensity.value,
                target,
                Time.unscaledDeltaTime * deathFadeSpeed
            );

            vignette.intensity.Override(value);
            yield return null;
        }
    }
}
