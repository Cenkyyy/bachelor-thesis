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
        if (player == null || player.PlayerData == null)
            return;

        healthBar?.Initialize(player.PlayerData);
        manaBar?.Initialize(player.PlayerData);
        hungerBar?.Initialize(player.PlayerData);
        xpBar?.Initialize(player.PlayerData);
    }
}
