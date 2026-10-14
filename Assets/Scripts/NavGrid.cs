using System.Collections.Generic;
using UnityEngine;

// Grade de navegação simples para o gato preto achar caminho pela cozinha (busca em largura / BFS).
// A grade é montada uma vez no começo da fase olhando quais células têm parede (colliders estáticos).
public static class NavGrid
{
    static bool[,] walkable;
    static Vector2 origin;      // canto inferior esquerdo da área
    static float cellSize;
    static int width, height;

    static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    public static bool IsBuilt => walkable != null;

    public static void Build(Vector2 min, Vector2 max, float cell, float clearance)
    {
        Physics2D.SyncTransforms();
        origin = min;
        cellSize = cell;
        width = Mathf.Max(1, Mathf.RoundToInt((max.x - min.x) / cell));
        height = Mathf.Max(1, Mathf.RoundToInt((max.y - min.y) / cell));
        walkable = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool free = true;
                foreach (Collider2D col in Physics2D.OverlapCircleAll(CellCenter(x, y), clearance))
                {
                    // parede = collider sólido e estático (sem Rigidbody2D)
                    if (!col.isTrigger && col.attachedRigidbody == null)
                    {
                        free = false;
                        break;
                    }
                }
                walkable[x, y] = free;
            }
        }
    }

    public static Vector2 CellCenter(int x, int y)
    {
        return origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
    }

    static Vector2Int ToCell(Vector2 p)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((p.x - origin.x) / cellSize), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((p.y - origin.y) / cellSize), 0, height - 1);
        return new Vector2Int(x, y);
    }

    static bool IsWalkable(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < width && c.y < height && walkable[c.x, c.y];
    }

    // Se a célula estiver bloqueada (encostado na parede), procura a livre mais próxima.
    static Vector2Int NearestWalkable(Vector2Int c)
    {
        if (IsWalkable(c)) return c;
        for (int r = 1; r <= 4; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2Int n = new Vector2Int(c.x + dx, c.y + dy);
                    if (IsWalkable(n)) return n;
                }
        return c;
    }

    // Caminho de 'from' até 'to' como lista de pontos no mundo. Lista vazia = sem caminho.
    public static List<Vector2> FindPath(Vector2 from, Vector2 to)
    {
        List<Vector2> path = new List<Vector2>();
        if (!IsBuilt) return path;

        Vector2Int start = NearestWalkable(ToCell(from));
        Vector2Int goal = NearestWalkable(ToCell(to));
        if (!IsWalkable(start) || !IsWalkable(goal)) return path;

        Vector2Int[,] cameFrom = new Vector2Int[width, height];
        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        bool found = false;
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            if (cur == goal)
            {
                found = true;
                break;
            }
            foreach (Vector2Int d in Directions)
            {
                Vector2Int n = cur + d;
                if (!IsWalkable(n) || visited[n.x, n.y]) continue;
                // diagonal só se não "cortar a quina" da parede
                if (d.x != 0 && d.y != 0 &&
                    (!IsWalkable(new Vector2Int(cur.x + d.x, cur.y)) || !IsWalkable(new Vector2Int(cur.x, cur.y + d.y))))
                    continue;
                visited[n.x, n.y] = true;
                cameFrom[n.x, n.y] = cur;
                queue.Enqueue(n);
            }
        }
        if (!found) return path;

        for (Vector2Int c = goal; c != start; c = cameFrom[c.x, c.y]) path.Add(CellCenter(c.x, c.y));
        path.Add(CellCenter(start.x, start.y));
        path.Reverse();
        return path;
    }
}
