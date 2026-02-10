using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] string playerTag = "Player";

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] string openTriggerName = "OpenChest";

    [Header("FX")]
    [SerializeField] GameObject glowFX;

    [Header("UI")]
    [SerializeField] Canvas youWinCanvas;

    bool opened;

    void Awake()
    {
        if (glowFX)
            glowFX.SetActive(false);

        if (youWinCanvas)
            youWinCanvas.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (opened)
            return;

        if (!other.CompareTag(playerTag))
            return;

        opened = true;

        if (animator)
            animator.SetTrigger(openTriggerName);
    }

    // ------------------------------------------------
    // CALLED BY ANIMATION EVENT
    // ------------------------------------------------
    public void OpenChest()
    {
        if (glowFX)
            glowFX.SetActive(true);
    }

    // ------------------------------------------------
    // CALLED BY ANIMATION EVENT (END)
    // ------------------------------------------------
    public void OnChestOpened()
    {
        if (youWinCanvas)
            youWinCanvas.enabled = true;

        PlayerWin();
    }

    void PlayerWin()
    {
        // Freeze game or set win state
        Time.timeScale = 0f;

        // If you have a player state manager, call it here
        // Example:
        // PlayerStateManager.Instance.SetWin();
    }
}
