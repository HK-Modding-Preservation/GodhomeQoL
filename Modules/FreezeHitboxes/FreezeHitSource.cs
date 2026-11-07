namespace GodhomeQoL.Modules.Tools;

internal static class FreezeHitSource
{
    private const int CircleSegments = 24;
    private const int RecentFrames = 5;

    internal sealed class Shape
    {
        internal Shape(Vector2[] points, bool closed)
        {
            Points = points;
            Closed = closed;
        }

        internal Vector2[] Points { get; }

        internal bool Closed { get; }
    }

    private sealed class Snapshot
    {
        internal readonly List<Shape> Shapes = new();
        internal string Name = string.Empty;
        internal int Frame;
    }

    private static readonly List<Shape> frozenShapes = new();
    private static Snapshot? candidate;
    private static Snapshot? lastDamaging;
    private static bool inTakeDamage;

    internal static IReadOnlyList<Shape> Shapes => frozenShapes;

    internal static string Name { get; private set; } = string.Empty;

    internal static void Hook() => On.HeroController.TakeDamage += OnTakeDamage;

    internal static void Unhook()
    {
        On.HeroController.TakeDamage -= OnTakeDamage;
        candidate = null;
        lastDamaging = null;
        Clear();
    }

    internal static void Freeze()
    {
        Snapshot? source = inTakeDamage
            ? candidate
            : lastDamaging != null && Time.frameCount - lastDamaging.Frame <= RecentFrames ? lastDamaging : null;
        frozenShapes.Clear();
        Name = string.Empty;
        if (source == null)
        {
            return;
        }

        frozenShapes.AddRange(source.Shapes);
        Name = source.Name;
    }

    internal static void Clear()
    {
        frozenShapes.Clear();
        Name = string.Empty;
    }

    private static void OnTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
    {
        int before = GetTotalHealth();
        candidate = Capture(go);
        inTakeDamage = true;
        try
        {
            orig(self, go, damageSide, damageAmount, hazardType);
        }
        finally
        {
            inTakeDamage = false;
        }

        if (candidate != null && (GetTotalHealth() < before || (self != null && self.cState.dead)))
        {
            lastDamaging = candidate;
        }
    }

    private static int GetTotalHealth()
    {
        PlayerData? data = PlayerData.instance;
        return data == null ? 0 : data.GetInt("health") + data.GetInt("healthBlue");
    }

    private static Snapshot? Capture(GameObject? go)
    {
        if (go == null)
        {
            return null;
        }

        try
        {
            Snapshot snapshot = new() { Name = GetDisplayName(go), Frame = Time.frameCount };
            Collider2D[] colliders = go.GetComponents<Collider2D>();
            bool anyEnabled = colliders.Any(collider => collider != null && collider.enabled);
            foreach (Collider2D collider in colliders)
            {
                if (collider != null && (collider.enabled || !anyEnabled))
                {
                    AddShapes(snapshot.Shapes, collider);
                }
            }

            return snapshot;
        }
        catch (Exception ex)
        {
            LogDebug($"Freeze hit source capture failed: {ex.Message}");
            return null;
        }
    }

    private static void AddShapes(List<Shape> shapes, Collider2D collider)
    {
        Transform transform = collider.transform;
        Vector2 offset = collider.offset;
        switch (collider)
        {
            case BoxCollider2D box:
                AddBox(shapes, transform, offset, box.size);
                break;
            case CapsuleCollider2D capsule:
                AddBox(shapes, transform, offset, capsule.size);
                break;
            case PolygonCollider2D polygon:
                for (int i = 0; i < polygon.pathCount; i++)
                {
                    Vector2[] path = polygon.GetPath(i);
                    if (path.Length > 1)
                    {
                        shapes.Add(new Shape(path.Select(point => ToWorld(transform, point + offset)).ToArray(), true));
                    }
                }

                break;
            case EdgeCollider2D edge:
                if (edge.points.Length > 1)
                {
                    shapes.Add(new Shape(edge.points.Select(point => ToWorld(transform, point + offset)).ToArray(), false));
                }

                break;
            case CircleCollider2D circle:
                Vector2 center = ToWorld(transform, offset);
                Vector3 scale = transform.lossyScale;
                float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                Vector2[] points = new Vector2[CircleSegments];
                for (int i = 0; i < CircleSegments; i++)
                {
                    float angle = i * Mathf.PI * 2f / CircleSegments;
                    points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                shapes.Add(new Shape(points, true));
                break;
        }
    }

    private static void AddBox(List<Shape> shapes, Transform transform, Vector2 offset, Vector2 size)
    {
        Vector2 half = size / 2f;
        shapes.Add(new Shape(new[]
        {
            ToWorld(transform, offset + new Vector2(-half.x, half.y)),
            ToWorld(transform, offset + half),
            ToWorld(transform, offset + new Vector2(half.x, -half.y)),
            ToWorld(transform, offset - half)
        }, true));
    }

    private static Vector2 ToWorld(Transform transform, Vector2 local) => transform.TransformPoint(local);

    private static string GetDisplayName(GameObject go)
    {
        string name = CleanName(go.name);
        Transform? parent = go.transform.parent;
        return parent != null ? $"{CleanName(parent.name)} / {name}" : name;
    }

    private static string CleanName(string name)
    {
        string result = name.Replace("(Clone)", string.Empty).Trim();
        int bracket = result.LastIndexOf(" (", StringComparison.Ordinal);
        if (bracket > 0 && result.EndsWith(")", StringComparison.Ordinal) && int.TryParse(result.Substring(bracket + 2, result.Length - bracket - 3), out _))
        {
            result = result.Substring(0, bracket);
        }

        return result;
    }
}
