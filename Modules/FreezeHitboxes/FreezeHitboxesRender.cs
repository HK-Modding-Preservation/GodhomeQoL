namespace GodhomeQoL.Modules.Tools;

internal sealed class FreezeHitboxesRender : MonoBehaviour
{
    private const int CleanupIntervalFrames = 30;

    private struct HitboxType : IComparable<HitboxType>
    {
        public static readonly HitboxType KnightHurtbox = new(Color.white, -1);
        public static readonly HitboxType Knight = new(Color.yellow, 0);
        public static readonly HitboxType Enemy = new(new Color(0.8f, 0f, 0f), 1);
        public static readonly HitboxType Attack = new(Color.cyan, 2);
        public static readonly HitboxType Terrain = new(new Color(0f, 0.8f, 0f), 3);
        public static readonly HitboxType Trigger = new(new Color(0.5f, 0.5f, 1f), 4);
        public static readonly HitboxType Breakable = new(new Color(1f, 0.75f, 0.8f), 5);
        public static readonly HitboxType Gate = new(new Color(0f, 0f, 0.5f), 6);
        public static readonly HitboxType HazardRespawn = new(new Color(0.5f, 0f, 0.5f), 7);
        public static readonly HitboxType Other = new(new Color(0.9f, 0.6f, 0.4f), 8);

        public readonly Color Color;
        public readonly int Depth;

        private HitboxType(Color color, int depth)
        {
            Color = color;
            Depth = depth;
        }

        public int CompareTo(HitboxType other)
        {
            return other.Depth.CompareTo(Depth);
        }
    }

    private readonly SortedDictionary<HitboxType, HashSet<Collider2D>> colliders = new()
    {
        { HitboxType.KnightHurtbox, new HashSet<Collider2D>() },
        { HitboxType.Knight, new HashSet<Collider2D>() },
        { HitboxType.Enemy, new HashSet<Collider2D>() },
        { HitboxType.Attack, new HashSet<Collider2D>() },
        { HitboxType.Terrain, new HashSet<Collider2D>() },
        { HitboxType.Trigger, new HashSet<Collider2D>() },
        { HitboxType.Breakable, new HashSet<Collider2D>() },
        { HitboxType.Gate, new HashSet<Collider2D>() },
        { HitboxType.HazardRespawn, new HashSet<Collider2D>() },
        { HitboxType.Other, new HashSet<Collider2D>() }
    };
    private int lastCleanupFrame = -1;

    public static float LineWidth => Math.Max(0.7f, Screen.width / 960f * GameCameras.instance.tk2dCam.ZoomFactor);

    private void Start()
    {
        foreach (Collider2D col in Resources.FindObjectsOfTypeAll<Collider2D>())
        {
            TryAddHitboxes(col);
        }
    }

    public void UpdateHitbox(GameObject go)
    {
        foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true))
        {
            TryAddHitboxes(col);
        }
    }

    private static Vector2 LocalToScreenPoint(Camera camera, Collider2D collider2D, Vector2 point)
    {
        Vector2 result = camera.WorldToScreenPoint((Vector2)collider2D.transform.TransformPoint(point + collider2D.offset));
        return new Vector2((int)Math.Round(result.x), (int)Math.Round(Screen.height - result.y));
    }

    private void TryAddHitboxes(Collider2D collider2D)
    {
        if (collider2D == null)
        {
            return;
        }

        if (collider2D is BoxCollider2D or PolygonCollider2D or EdgeCollider2D or CircleCollider2D)
        {
            GameObject go = collider2D.gameObject;
            if (collider2D.GetComponent<global::HeroBox>() != null)
            {
                colliders[HitboxType.KnightHurtbox].Add(collider2D);
            }
            else if (collider2D.GetComponent<DamageHero>() || collider2D.gameObject.LocateMyFSM("damages_hero"))
            {
                colliders[HitboxType.Enemy].Add(collider2D);
            }
            else if (go.GetComponent<HealthManager>() || go.LocateMyFSM("health_manager_enemy") || go.LocateMyFSM("health_manager"))
            {
                colliders[HitboxType.Other].Add(collider2D);
            }
            else if (go.layer == (int)PhysLayers.TERRAIN)
            {
                if (go.name.Contains("Breakable") || go.name.Contains("Collapse") || go.GetComponent<Breakable>() != null)
                {
                    colliders[HitboxType.Breakable].Add(collider2D);
                }
                else
                {
                    colliders[HitboxType.Terrain].Add(collider2D);
                }
            }
            else if (go == HeroController.instance?.gameObject && !collider2D.isTrigger)
            {
                colliders[HitboxType.Knight].Add(collider2D);
            }
            else if (go.GetComponent<DamageEnemies>() || go.LocateMyFSM("damages_enemy") || go.name == "Damager" && go.LocateMyFSM("Damage"))
            {
                colliders[HitboxType.Attack].Add(collider2D);
            }
            else if (collider2D.isTrigger && collider2D.GetComponent<HazardRespawnTrigger>())
            {
                colliders[HitboxType.HazardRespawn].Add(collider2D);
            }
            else if (collider2D.isTrigger && collider2D.GetComponent<TransitionPoint>())
            {
                colliders[HitboxType.Gate].Add(collider2D);
            }
            else if (collider2D.GetComponent<Breakable>())
            {
                NonBouncer? bounce = collider2D.GetComponent<NonBouncer>();
                if (bounce == null || !bounce.active)
                {
                    colliders[HitboxType.Trigger].Add(collider2D);
                }
            }
        }
    }

    private void OnGUI()
    {
        if (Event.current?.type != EventType.Repaint || Camera.main == null || GameManager.instance == null || GameManager.instance.isPaused)
        {
            return;
        }

        CleanupDestroyedCollidersIfNeeded();

        GUI.depth = int.MaxValue;
        Camera camera = Camera.main;
        float lineWidth = LineWidth;
        foreach (KeyValuePair<HitboxType, HashSet<Collider2D>> pair in colliders)
        {
            foreach (Collider2D collider2D in pair.Value)
            {
                DrawHitbox(camera, collider2D, pair.Key, lineWidth);
            }
        }

        DrawHitSource(camera, lineWidth);
        DrawUnfreezeHint();
    }

    private static readonly Color HitSourceColor = new(1f, 0.15f, 0.85f);

    private static void DrawHitSource(Camera camera, float lineWidth)
    {
        IReadOnlyList<FreezeHitSource.Shape> shapes = FreezeHitSource.Shapes;
        if (shapes.Count == 0)
        {
            return;
        }

        int origDepth = GUI.depth;
        GUI.depth = -2;
        foreach (FreezeHitSource.Shape shape in shapes)
        {
            DrawWorldShape(camera, shape, Color.black, lineWidth * 4f);
        }

        foreach (FreezeHitSource.Shape shape in shapes)
        {
            DrawWorldShape(camera, shape, HitSourceColor, lineWidth * 2.5f);
        }

        GUI.depth = origDepth;
    }

    private static void DrawWorldShape(Camera camera, FreezeHitSource.Shape shape, Color color, float width)
    {
        Vector2[] points = shape.Points;
        int count = shape.Closed ? points.Length : points.Length - 1;
        for (int i = 0; i < count; i++)
        {
            FreezeHitboxesDrawing.DrawLine(WorldToGui(camera, points[i]), WorldToGui(camera, points[(i + 1) % points.Length]), color, width, true);
        }
    }

    private static Vector2 WorldToGui(Camera camera, Vector2 world)
    {
        Vector3 screen = camera.WorldToScreenPoint(world);
        return new Vector2((int)Math.Round(screen.x), (int)Math.Round(Screen.height - screen.y));
    }

    private GUIStyle? hintStyle;

    private void DrawUnfreezeHint()
    {
        KeyCode key = FreezeHitboxes.GetUnfreezeKey();
        string text = key != KeyCode.None
            ? string.Format("FreezeHitboxes/HintKey".Localize(), key)
            : string.Format("FreezeHitboxes/HintNoKey".Localize(), QuickMenu.GetQuickMenuHotkeyLabel());

        int fontSize = Mathf.Max(14, Mathf.RoundToInt(Screen.height / 40f));
        hintStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        hintStyle.fontSize = fontSize;

        Rect rect = new(0f, Screen.height * 0.06f, Screen.width, fontSize * 3f);
        Rect shadow = new(rect.x + 2f, rect.y + 2f, rect.width, rect.height);
        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.Label(shadow, text, hintStyle);
        GUI.color = Color.white;
        GUI.Label(rect, text, hintStyle);

        string source = FreezeHitSource.Name;
        if (!string.IsNullOrEmpty(source))
        {
            string hitText = string.Format("FreezeHitboxes/HitBy".Localize(), source);
            Rect hitRect = new(0f, rect.y + (fontSize * 2.2f), Screen.width, fontSize * 2f);
            GUI.color = Color.black;
            GUI.Label(new Rect(hitRect.x + 2f, hitRect.y + 2f, hitRect.width, hitRect.height), hitText, hintStyle);
            GUI.color = HitSourceColor;
            GUI.Label(hitRect, hitText, hintStyle);
        }

        GUI.color = previous;
    }

    private void CleanupDestroyedCollidersIfNeeded()
    {
        int frame = Time.frameCount;
        if (frame == lastCleanupFrame || frame % CleanupIntervalFrames != 0)
        {
            return;
        }

        lastCleanupFrame = frame;
        foreach (HashSet<Collider2D> set in colliders.Values)
        {
            set.RemoveWhere(collider => collider == null);
        }
    }

    private static void DrawHitbox(Camera camera, Collider2D collider2D, HitboxType hitboxType, float lineWidth)
    {
        if (collider2D == null || !collider2D.isActiveAndEnabled)
        {
            return;
        }

        int origDepth = GUI.depth;
        GUI.depth = hitboxType.Depth;
        if (collider2D is BoxCollider2D or EdgeCollider2D or PolygonCollider2D)
        {
            switch (collider2D)
            {
                case BoxCollider2D boxCollider2D:
                    Vector2 halfSize = boxCollider2D.size / 2f;
                    Vector2 topLeft = new(-halfSize.x, halfSize.y);
                    Vector2 topRight = halfSize;
                    Vector2 bottomRight = new(halfSize.x, -halfSize.y);
                    Vector2 bottomLeft = -halfSize;
                    List<Vector2> boxPoints = new()
                    {
                        topLeft, topRight, bottomRight, bottomLeft, topLeft
                    };
                    DrawPointSequence(boxPoints, camera, collider2D, hitboxType, lineWidth);
                    break;
                case EdgeCollider2D edgeCollider2D:
                    DrawPointSequence(new List<Vector2>(edgeCollider2D.points), camera, collider2D, hitboxType, lineWidth);
                    break;
                case PolygonCollider2D polygonCollider2D:
                    for (int i = 0; i < polygonCollider2D.pathCount; i++)
                    {
                        List<Vector2> polygonPoints = new(polygonCollider2D.GetPath(i));
                        if (polygonPoints.Count > 0)
                        {
                            polygonPoints.Add(polygonPoints[0]);
                        }
                        DrawPointSequence(polygonPoints, camera, collider2D, hitboxType, lineWidth);
                    }
                    break;
            }
        }
        else if (collider2D is CircleCollider2D circleCollider2D)
        {
            Vector2 center = LocalToScreenPoint(camera, collider2D, Vector2.zero);
            Vector2 right = LocalToScreenPoint(camera, collider2D, Vector2.right * circleCollider2D.radius);
            int radius = (int)Math.Round(Vector2.Distance(center, right));
            FreezeHitboxesDrawing.DrawCircle(center, radius, hitboxType.Color, lineWidth, true, Mathf.Clamp(radius / 16, 4, 32));
        }

        GUI.depth = origDepth;
    }

    private static void DrawPointSequence(List<Vector2> points, Camera camera, Collider2D collider2D, HitboxType hitboxType, float lineWidth)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 pointA = LocalToScreenPoint(camera, collider2D, points[i]);
            Vector2 pointB = LocalToScreenPoint(camera, collider2D, points[i + 1]);
            FreezeHitboxesDrawing.DrawLine(pointA, pointB, hitboxType.Color, lineWidth, true);
        }
    }
}
