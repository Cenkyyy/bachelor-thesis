using UnityEngine;

public class PlayerStatBarsController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private HealthBarUI healthBar;
    [SerializeField] private ManaBarUI manaBar;
    [SerializeField] private HungerBarUI hungerBar;
    [SerializeField] private XpBarUI xpBar;

    private void Start()
    {
        if (player == null || player.Data == null)
            return;

        healthBar?.Initialize(player.Data);
        manaBar?.Initialize(player.Data);
        hungerBar?.Initialize(player.Data);
        xpBar?.Initialize(player.Data);
    }
}
