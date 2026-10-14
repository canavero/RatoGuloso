using UnityEngine;

// Pequenas funções de compatibilidade para o projeto funcionar tanto na Unity 2022 LTS
// quanto na Unity 6 (onde alguns nomes da API mudaram).
public static class Compat
{
    public static Vector2 GetVelocity(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(this Rigidbody2D rb, Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    public static T FindFirst<T>() where T : Object
    {
#if UNITY_2022_2_OR_NEWER
        return Object.FindAnyObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
