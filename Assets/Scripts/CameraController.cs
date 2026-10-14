using UnityEngine;

// Ajusta o zoom da câmera para a arena inteira caber em qualquer proporção de tela
// e faz a tela "tremer" quando o rato leva dano.
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float minOrthographicSize = 6.25f;
    [Tooltip("Metade da largura (em unidades) que sempre precisa aparecer na tela.")]
    public float requiredHalfWidth = 10.5f;

    static CameraController instance;

    Camera cam;
    Vector3 basePosition;
    float shakeTimer;
    float shakeDuration;
    float shakeStrength;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void LateUpdate()
    {
        cam.orthographicSize = Mathf.Max(minOrthographicSize, requiredHalfWidth / Mathf.Max(0.1f, cam.aspect));

        Vector3 offset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTimer / shakeDuration);
            offset = (Vector3)(Random.insideUnitCircle * shakeStrength * k);
        }
        transform.position = basePosition + offset;
    }

    public static void Shake(float strength, float duration)
    {
        if (instance == null) return;
        instance.shakeStrength = strength;
        instance.shakeDuration = Mathf.Max(0.01f, duration);
        instance.shakeTimer = instance.shakeDuration;
    }
}
