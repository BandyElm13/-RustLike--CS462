public class EnergyHUD : HealthHUD
{
    protected override string Title => "Energy";
    protected override float Value => PlayerShooting.energy;

    private void Reset()
    {
        maximum = 10f;
        position = new UnityEngine.Vector2(12f, -100f);
    }
}
