using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Config")]
    public int gridSize = 10;
    public float tickRateSeconds = 1.5f;
    public int deliveryZoneCount = 5;
    public int initialTraitCount = 12;

    [Header("UI (uGUI + TMP)")]
    public Transform gridParent;
    public CellView cellPrefab;

    public TMP_Text reputationText;
    public TMP_Text orderTimerText;
    public TMP_Text fullnessText;
    public TMP_Text statusText;

    public WantedView wantedView;
    public BufferView bufferView;

    [Header("Game Rules")]
    public int reputation = 50;
    public int minReputation = 0;

    public float orderInterval = 6f;
    public float orderLifetime = 12f;

    [Header("Sentient Tick")]
    public float sentientTickInterval = 1.0f;

    // STATE
    private BoxEntity[] grid;
    private List<Vector2Int> deliveryZones = new List<Vector2Int>();
    private Queue<ItemSubType> conveyor = new Queue<ItemSubType>();
    private Vector2Int selector = new Vector2Int(0, 0);

    private string status = "System Stable";

    private Order currentOrder;
    private float orderSpawnTimer = 0f;

    private float tickTimer = 0f;
    private float sentientTickTimer = 0f;

    public List<Ghost> ghosts = new List<Ghost>();

    private CellView[] cells;
    private System.Random rng;

    void Awake()
    {
        rng = new System.Random();
    }

    void Start()
    {
        grid = new BoxEntity[gridSize * gridSize];

        BuildGridUI();
        InitDeliveryZones();
        InitConveyor();
        InitInitialTraits();      // optional (spawns initial traits on boxes if you want, else remove)
        GenerateNewOrder();

        RenderAll();
    }

    void Update()
    {
        HandleInput();

        // Order system
        UpdateOrder(Time.deltaTime);

        // Fire expiry
        UpdateFireTimers(Time.deltaTime);

        // Ghosts: aura + movement
        UpdateGhosts(Time.deltaTime);

        // Sentient autonomous movement
        sentientTickTimer += Time.deltaTime;
        if (sentientTickTimer >= sentientTickInterval)
        {
            sentientTickTimer -= sentientTickInterval;
            SentientTick();
        }

        // Chaos tick (optional, if you still want periodic events)
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickRateSeconds)
        {
            tickTimer -= tickRateSeconds;
            WorldTick();
        }

        RenderHud();
    }

    // ----------------------------
    // INIT
    // ----------------------------
    void BuildGridUI()
    {
        cells = new CellView[gridSize * gridSize];
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            var cv = Instantiate(cellPrefab, gridParent);
            int idx = i;
            cv.Init(() => OnCellClicked(idx));
            cells[i] = cv;
        }
    }

    void InitDeliveryZones()
    {
        deliveryZones.Clear();
        while (deliveryZones.Count < deliveryZoneCount)
        {
            var p = new Vector2Int(rng.Next(0, gridSize), rng.Next(0, gridSize));
            if (!deliveryZones.Contains(p)) deliveryZones.Add(p);
        }
    }

    void InitConveyor()
    {
        conveyor.Clear();
        conveyor.Enqueue(ItemSubType.Bread);
        conveyor.Enqueue(ItemSubType.Knife);
        conveyor.Enqueue(ItemSubType.WaterBottle);
        bufferView?.Set(conveyor.ToArray());
    }

    // Keep this only if you still want some boxes pre-seeded.
    // If not needed, delete this function and its call.
    void InitInitialTraits()
    {
        // Example: do nothing (recommended for clean start)
        // Leave empty or remove entirely.
    }

    // ----------------------------
    // INPUT
    // ----------------------------
    void HandleInput()
    {
        // Selector movement (WASD)
        if (Input.GetKeyDown(KeyCode.W)) selector.y = Mathf.Max(0, selector.y - 1);
        if (Input.GetKeyDown(KeyCode.S)) selector.y = Mathf.Min(gridSize - 1, selector.y + 1);
        if (Input.GetKeyDown(KeyCode.A)) selector.x = Mathf.Max(0, selector.x - 1);
        if (Input.GetKeyDown(KeyCode.D)) selector.x = Mathf.Min(gridSize - 1, selector.x + 1);

        // Shove (Arrow keys)
        if (Input.GetKeyDown(KeyCode.UpArrow)) ShoveSelected(new Vector2Int(0, -1));
        if (Input.GetKeyDown(KeyCode.DownArrow)) ShoveSelected(new Vector2Int(0, 1));
        if (Input.GetKeyDown(KeyCode.LeftArrow)) ShoveSelected(new Vector2Int(-1, 0));
        if (Input.GetKeyDown(KeyCode.RightArrow)) ShoveSelected(new Vector2Int(1, 0));

        // Enter: spawn if empty, else submit if on delivery zone
        if (Input.GetKeyDown(KeyCode.Return))
        {
            int idx = PosToIdx(selector);
            if (grid[idx] == null) SpawnBoxAtSelector();
            else AttemptSubmit();
        }
    }

    // ----------------------------
    // MOVEMENT
    // ----------------------------
    void ShoveSelected(Vector2Int dir)
    {
        int curIdx = PosToIdx(selector);
        var box = grid[curIdx];
        if (box == null) return;

        var next = selector + dir;
        if (!InBounds(next)) return;

        int nextIdx = PosToIdx(next);

        // Collision blocks movement (GDD style)
        if (grid[nextIdx] != null)
        {
            status = "BLOCKED";
            return;
        }

        // Ice slide: slide until boundary or next occupied
        Vector2Int final = next;
        if (box.Has(TraitType.Ice))
        {
            while (true)
            {
                var cand = final + dir;
                if (!InBounds(cand)) break;

                int cIdx = PosToIdx(cand);
                if (grid[cIdx] != null) break;

                final = cand;
            }
        }

        int finalIdx = PosToIdx(final);
        grid[curIdx] = null;
        grid[finalIdx] = box;

        // Keep selector following the moved box (matches your React feel)
        selector = final;

        status = box.Has(TraitType.Ice) ? "SLID" : "MOVED";
        RenderAll();
    }

    void SpawnBoxAtSelector()
    {
        int idx = PosToIdx(selector);
        if (grid[idx] != null) return;

        var sub = conveyor.Dequeue();
        conveyor.Enqueue(RandomItemType());
        bufferView?.Set(conveyor.ToArray());

        grid[idx] = new BoxEntity(sub);

        status = "SPAWNED";
        RenderAll();
    }

    // ----------------------------
    // ORDERS + SUBMIT
    // ----------------------------
    void UpdateOrder(float dt)
    {
        if (currentOrder == null)
        {
            orderSpawnTimer += dt;
            if (orderSpawnTimer >= orderInterval)
            {
                orderSpawnTimer = 0f;
                GenerateNewOrder();
            }
            return;
        }

        currentOrder.timeLeft -= dt;
        if (currentOrder.timeLeft <= 0f)
        {
            reputation -= 10;
            status = "ORDER FAILED";
            GenerateNewOrder();

            if (reputation <= minReputation) GameOver();
        }
    }

    void GenerateNewOrder()
    {
        currentOrder = new Order
        {
            subType = RandomItemType(),
            requiredTrait = RandomRequiredTrait(),
            timeLeft = orderLifetime
        };

        wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTrait, currentOrder.timeLeft, orderLifetime);
    }

    void AttemptSubmit()
    {
        int idx = PosToIdx(selector);
        var box = grid[idx];
        if (box == null)
        {
            status = "NOTHING";
            return;
        }

        if (!IsDeliveryZone(selector))
        {
            status = "NOT DELIVERY";
            return;
        }

        if (currentOrder == null)
        {
            status = "NO ORDER";
            return;
        }

        bool okType = box.subType == currentOrder.subType;
        bool okTrait = box.traits.Contains(currentOrder.requiredTrait);

        if (okType && okTrait)
        {
            reputation += 5;
            status = "ORDER FULFILLED";
        }
        else
        {
            reputation -= 2;
            status = "WRONG DELIVERY";
            if (reputation <= minReputation) { GameOver(); return; }
        }

        grid[idx] = null;
        GenerateNewOrder();
        RenderAll();
    }

    void GameOver()
    {
        status = "GAME OVER";
        enabled = false;
        RenderHud();
    }

    // ----------------------------
    // FIRE TIMER (5s hold)
    // ----------------------------
    void UpdateFireTimers(float dt)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            var box = grid[i];
            if (box == null) continue;

            if (box.traits.Contains(TraitType.Fire))
            {
                box.fireTimer -= dt;
                if (box.fireTimer <= 0f)
                {
                    box.traits.Remove(TraitType.Fire);
                    status = "FIRE EXPIRED";
                }
            }
        }
    }

    // ----------------------------
    // GHOSTS
    // ----------------------------
    void UpdateGhosts(float dt)
    {
        foreach (var g in ghosts)
        {
            ApplyGhostAura(g);

            g.moveTimer += dt;
            if (g.moveTimer >= g.moveInterval)
            {
                g.moveTimer -= g.moveInterval;
                GhostStep(g);
            }
        }
    }

    void ApplyGhostAura(Ghost g)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var p = g.pos + new Vector2Int(dx, dy);
            if (!InBounds(p)) continue;

            int idx = PosToIdx(p);
            var box = grid[idx];
            if (box == null) continue;

            if (g.type == GhostType.Base)
            {
                box.traits.Add(TraitType.Haunted);
            }
            else if (g.type == GhostType.FireGhost)
            {
                box.AddTrait(TraitType.Fire);
            }
            else if (g.type == GhostType.IceFast)
            {
                box.traits.Add(TraitType.Ice);
            }
        }
    }

    void GhostStep(Ghost g)
    {
        var dirs = new[] {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };
        var d = dirs[rng.Next(0, dirs.Length)];
        var np = g.pos + d;
        if (!InBounds(np)) return;

        g.pos = np;
    }

    // ----------------------------
    // SENTIENT / HAUNTED MOVEMENT
    // ----------------------------
    void SentientTick()
    {
        // snapshot indices to avoid chain-move weirdness
        var indices = new List<int>();
        for (int i = 0; i < grid.Length; i++)
        {
            var b = grid[i];
            if (b == null) continue;
            if (b.Has(TraitType.Sentient) || b.Has(TraitType.Haunted)) indices.Add(i);
        }

        foreach (var i in indices.OrderBy(_ => rng.Next()))
        {
            if (grid[i] == null) continue;

            var p = IdxToPos(i);
            var dirs = new[] {
                new Vector2Int(1,0), new Vector2Int(-1,0),
                new Vector2Int(0,1), new Vector2Int(0,-1)
            };
            var d = dirs[rng.Next(0, dirs.Length)];
            var np = p + d;
            if (!InBounds(np)) continue;

            int ni = PosToIdx(np);
            if (grid[ni] != null) continue;

            grid[ni] = grid[i];
            grid[i] = null;
        }
    }

    // Optional: keep if you still want periodic status refresh or random events
    void WorldTick()
    {
        // Example: clear Haunted from boxes occasionally so it is not permanent
        for (int i = 0; i < grid.Length; i++)
        {
            var b = grid[i];
            if (b == null) continue;
            b.traits.Remove(TraitType.Haunted);
        }
    }

    // ----------------------------
    // HELPERS
    // ----------------------------
    bool InBounds(Vector2Int p) => p.x >= 0 && p.x < gridSize && p.y >= 0 && p.y < gridSize;
    int PosToIdx(Vector2Int p) => p.y * gridSize + p.x;
    Vector2Int IdxToPos(int idx) => new Vector2Int(idx % gridSize, idx / gridSize);

    bool IsDeliveryZone(Vector2Int p) => deliveryZones.Contains(p);

    ItemSubType RandomItemType()
    {
        var vals = Enum.GetValues(typeof(ItemSubType)).Cast<ItemSubType>().ToArray();
        return vals[rng.Next(0, vals.Length)];
    }

    TraitType RandomRequiredTrait()
    {
        // Only traits that an order can demand
        var candidates = new[] { TraitType.Fire, TraitType.Ice, TraitType.Sentient };
        return candidates[rng.Next(0, candidates.Length)];
    }

    void OnCellClicked(int idx)
    {
        selector = IdxToPos(idx);
        RenderAll();
    }

    // ----------------------------
    // UI RENDER
    // ----------------------------
    void RenderAll()
    {
        for (int i = 0; i < grid.Length; i++)
        {
            var p = IdxToPos(i);
            bool selected = (p == selector);
            bool zone = IsDeliveryZone(p);   
            cells[i].SetCell(grid[i], selected, zone);
        }
        RenderHud();
    }

    void RenderHud()
    {
        int filled = grid.Count(e => e != null);
        int pct = Mathf.RoundToInt(100f * filled / grid.Length);

        if (fullnessText != null) fullnessText.text = $"GRID: {pct}%";
        if (statusText != null) statusText.text = status.ToUpper();
        if (reputationText != null) reputationText.text = $"REP: {reputation}";

        if (currentOrder != null)
        {
            if (orderTimerText != null) orderTimerText.text = $"TIME: {Mathf.CeilToInt(currentOrder.timeLeft)}";
            wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTrait, currentOrder.timeLeft, orderLifetime);
        }

        bufferView?.Set(conveyor.ToArray());
    }
}
