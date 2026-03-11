public static class NightTimeFlag
{
    public static bool IsNight { get; private set; }

    public static void Set(bool isNight)
    {
        IsNight = isNight;
    }
}
