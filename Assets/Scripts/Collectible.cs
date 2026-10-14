using System.Collections.Generic;
using UnityEngine;

// Item coletável: queijo (pontos + tempo extra) ou relógio (muito tempo extra).
// Precisa de um Collider2D marcado como "Is Trigger".
public class Collectible : MonoBehaviour
{
    public enum Kind { Cheese, Clock }

    // todos os coletáveis ativos na cena (o GameManager usa para contar os queijos)
    public static readonly List<Collectible> All = new List<Collectible>();

    public Kind kind = Kind.Cheese;
    public int points = 10;
    public float timeBonus = 3f;

    [Header("Animação")]
    public Transform visual;            // filho com o sprite (flutua e balança)
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 3f;
    public float wobbleAngle = 10f;

    bool collected;
    float phase;
    Vector3 visualBasePos;

    void Awake()
    {
        phase = Random.value * 10f;
        if (visual != null) visualBasePos = visual.localPosition;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        if (visual == null || visual == transform) return;
        float t = (Time.time + phase) * bobSpeed;
        visual.localPosition = visualBasePos + Vector3.up * Mathf.Sin(t) * bobAmplitude;
        visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * wobbleAngle);
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        GameManager gm = GameManager.Instance;
        if (kind == Kind.Cheese)
        {
            AudioManager.PlaySfx("coletar");
            if (gm != null) gm.AddCheese(points, timeBonus);
        }
        else
        {
            AudioManager.PlaySfx("relogio");
            if (gm != null) gm.AddTime(timeBonus, points);
        }
        Destroy(gameObject);
    }
}
