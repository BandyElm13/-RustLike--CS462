public class EnergyHUD : HealthHUD
{
    protected override string Title => "Energy";
    protected override float Value => PlayerShooting.energy;
    protected override int SegmentCount => UnityEngine.Mathf.RoundToInt(maximum);

    private void Reset()
    {
        maximum = 10f;
        segmentHeight = 21f;
        position = new UnityEngine.Vector2(12f, -100f);
    }
}
