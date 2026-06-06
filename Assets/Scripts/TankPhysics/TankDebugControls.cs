using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankDebugControls : MonoBehaviour
{
    [Header("Tanks Zone: Debug")]
    public float teleportHeight = 1.5f;
    public Vector3 defaultSpawnPosition = new Vector3(-20f, 1f, -75f);
    [Header("Настройки Спавна")]
    public GameObject dummyTankPrefab;

    private Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    void Start()
    {
        if (DeveloperConsole.Instance != null)
        {
            DeveloperConsole.Instance.AddCommand("up", CmdUp);
            DeveloperConsole.Instance.AddCommand("flip", CmdFlip);
            DeveloperConsole.Instance.AddCommand("respawn", CmdRespawn);
            DeveloperConsole.Instance.AddCommand("spawn_bot", CmdSpawnBot);
        }
    }

    void Update()
    {
        if (DeveloperConsole.Instance != null && DeveloperConsole.Instance.IsOpen) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.tKey.wasPressedThisFrame) TeleportUp();
        if (keyboard.rKey.wasPressedThisFrame) ResetRotation();
    }

    public void TeleportUp()
    {
        rb.position += Vector3.up * teleportHeight;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    public void ResetRotation()
    {
        float currentYaw = rb.rotation.eulerAngles.y;
        rb.MoveRotation(Quaternion.Euler(0f, currentYaw, 0f));
        rb.angularVelocity = Vector3.zero;
        rb.position += Vector3.up * 0.5f;
    }

    public void Respawn(Vector3 position)
    {
        rb.position = position;
        rb.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void CmdUp(string[] args)
    {
        TeleportUp();
        DeveloperConsole.Instance.LogMessage("Tanks Zone: Танк подпрыгнул!", Color.green);
    }

    private void CmdFlip(string[] args)
    {
        ResetRotation();
        DeveloperConsole.Instance.LogMessage("Tanks Zone: Танк поставлен на гусеницы!", Color.green);
    }

    private void CmdRespawn(string[] args)
    {
        Vector3 spawnPos = defaultSpawnPosition;

        if (args.Length >= 3)
        {
            if (float.TryParse(args[0], out float x) &&
                float.TryParse(args[1], out float y) &&
                float.TryParse(args[2], out float z))
            { spawnPos = new Vector3(x, y, z); }
        }
        Respawn(spawnPos);
        DeveloperConsole.Instance.LogMessage($"Tanks Zone: Танк возрожден на {spawnPos}", Color.cyan);
    }

    private void CmdSpawnBot(string[] args)
    {
        if (dummyTankPrefab == null)
        {
            DeveloperConsole.Instance.LogMessage("Ошибка: Не назначен префаб dummyTankPrefab!", Color.red);
            return;
        }
        int count = 1;
        Vector3 startPos = transform.position + transform.forward * 15f;
        if (args.Length == 1)
        {
            int.TryParse(args[0], out count);
        }
        else if (args.Length == 3)
        {
            float.TryParse(args[0], out startPos.x);
            float.TryParse(args[1], out startPos.y);
            float.TryParse(args[2], out startPos.z);
        }
        else if (args.Length >= 4)
        {
            float.TryParse(args[0], out startPos.x);
            float.TryParse(args[1], out startPos.y);
            float.TryParse(args[2], out startPos.z);
            int.TryParse(args[3], out count);
        }
        count = Mathf.Clamp(count, 1, 50);
        float verticalSpacing = 4f;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = startPos + Vector3.up * (i * verticalSpacing + 2f);
            Instantiate(dummyTankPrefab, pos, transform.rotation * Quaternion.Euler(0, 180, 0));
        }
        string msg = count > 1 ? $"Заспавнено {count} ботов столбом!" : "Болванка заспавнена!";
        DeveloperConsole.Instance.LogMessage($"Успех: {msg}", Color.green);
    }
}