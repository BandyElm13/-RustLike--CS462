public class EnergyHUD : HealthHUD
{
    [UnityEngine.Tooltip("How many segments the bar is split into (each = Maximum / Segments energy).")]
    public int segmentCount = 15;

    protected override string Title => "Energy";
    protected override float Value => PlayerShooting.energy;
    protected override float Maximum => PlayerShooting.maxEnergy > 0f ? PlayerShooting.maxEnergy : maximum;
    protected override int SegmentCount => UnityEngine.Mathf.Max(1, segmentCount);
    protected override bool GradualFill => true;

    private void Reset()
    {
        maximum = 30f;
        segmentHeight = 21f;
        position = new UnityEngine.Vector2(12f, -100f);
    }
}
