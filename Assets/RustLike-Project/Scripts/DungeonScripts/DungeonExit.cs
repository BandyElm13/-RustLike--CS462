using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// End of the dungeon (on the BossRoom prefab). Standing on the room's floor (the pink "Square")
/// returns the player to the planet scene, but only once every enemy in the dungeon is dead;
/// until then a message says how many are left. Score and run time carry over (both static).
///
/// Also kills any enemy that falls below the dungeon, so a lost enemy can't block the exit.
/// </summary>
public class DungeonExit : MonoBehaviour
{
    [Tooltip("Scene loaded when the player leaves the dungeon.")]
    [SerializeField] private string returnScene = "Planet-1-RobotWorld";
    [Tooltip("Collider the player must stand on. Empty = first BoxCollider in this room (the floor).")]
    [SerializeField] private Collider exitFloor;
    [Tooltip("How high above the floor still counts as standing on it.")]
    [SerializeField] private float detectHeight = 3f;

    [Header("Fall Off Map")]
    [Tooltip("Enemies this far below the lowest room are killed.")]
    [SerializeField] private float killDepth = 15f;

    [Header("Message")]
    [SerializeField] private TMP_FontAsset messageFont;
    [SerializeField] private Color messageColor = new Color(1f, 0.5372549f, 0f, 1f);
    [SerializeField] private float messageDuration = 2f;

    private float killY = float.NegativeInfinity;
    private Transform player;
    private TextMeshProUGUI message;
    private float messageTimer;
    private bool leaving;

    private IEnumerator Start()
    {
        if (exitFloor == null) exitFloor = GetComponentInChildren<BoxCollider>();

        // Rooms are generated in the same frame as this one; wait a frame so they all exist.
        yield return null;

        float lowest = float.PositiveInfinity;
        foreach (ProceduralDungeons.DungeonRoom room in FindObjectsByType<ProceduralDungeons.DungeonRoom>())
            lowest = Mathf.Min(lowest, room.transform.position.y);
        if (!float.IsPositiveInfinity(lowest)) killY = lowest - killDepth;
    }

    private void Update()
    {
        KillFallenEnemies();
        UpdateMessage();

        if (leaving || exitFloor == null) return;
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null) return;
            player = found.transform;
        }

        if (!IsStandingOnExit(player.position)) return;

        int alive = AliveEnemies();
        if (alive > 0)
        {
            ShowMessage(alive == 1 ? "Defeat the last enemy to leave" : "Defeat all enemies to leave (" + alive + " left)");
            return;
        }

        leaving = true;
        SceneManager.LoadScene(returnScene); // score + RunTimer are static, so they carry over
    }

    private bool IsStandingOnExit(Vector3 position)
    {
        Bounds bounds = exitFloor.bounds;
        bounds.Expand(new Vector3(0f, detectHeight * 2f, 0f)); // floor is paper-thin; count the space above it
        return position.x >= bounds.min.x && position.x <= bounds.max.x
            && position.z >= bounds.min.z && position.z <= bounds.max.z
            && position.y >= exitFloor.bounds.min.y - 0.5f && position.y <= exitFloor.bounds.max.y + detectHeight;
    }

    private static int AliveEnemies()
    {
        int count = 0;
        foreach (EnemyStats enemy in FindObjectsByType<EnemyStats>())
            if (!enemy.IsDead) count++;
        return count;
    }

    private void KillFallenEnemies()
    {
        if (float.IsNegativeInfinity(killY)) return;
        foreach (EnemyStats enemy in FindObjectsByType<EnemyStats>())
            if (!enemy.IsDead && enemy.transform.position.y < killY)
                enemy.Kill();
    }

    // ---------------------------------------------------------------- message

    private void ShowMessage(string text)
    {
        if (message == null) CreateMessage();
        message.text = text;
        messageTimer = messageDuration;
    }

    private void UpdateMessage()
    {
        if (message == null) return;
        messageTimer -= Time.deltaTime;
        message.alpha = Mathf.Clamp01(messageTimer / 0.5f); // fade out over the last half second
    }

    private void CreateMessage()
    {
        GameObject canvasObject = new GameObject("DungeonExit_Message", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        message = HealthHUD.CreateText(canvasObject.transform, "", Vector2.zero, new Vector2(1400f, 80f), messageColor, messageFont);
        RectTransform rect = message.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -160f);
        message.fontSize = 40f;
    }
}
