using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("Order Traits")]
    [Range(1, 3)] public int minOrderTraits = 1;
    [Range(1, 3)] public int maxOrderTraits = 2;

    [Header("Ghosts")]
    public int maxGhosts = 4;
    public float ghostSpawnInterval = 4.0f;
    public float ghostMergeSeconds = 2.0f;

    [Header("Trait Tiles")]
    public float traitSpawnInterval = 3.0f;
    public float traitMergeSeconds = 2.0f;

    [Header("Items")]
    public float itemSpawnInterval = 5.0f;

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
    private float ghostSpawnTimer = 0f;
    private float traitSpawnTimer = 0f;
    private float itemSpawnTimer = 0f;

    public List<Ghost> ghosts = new List<Ghost>();

    private CellView[] cells;
    private System.Random rng;
    private Dictionary<string, GhostMergeState> ghostMerge = new Dictionary<string, GhostMergeState>();
    private Dictionary<string, GhostMergeState> traitMerge = new Dictionary<string, GhostMergeState>();

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

        // Timed spawns
        if (ghostSpawnInterval > 0f)
        {
            ghostSpawnTimer += Time.deltaTime;
            if (ghostSpawnTimer >= ghostSpawnInterval)
            {
                ghostSpawnTimer -= ghostSpawnInterval;
                TrySpawnGhost();
            }
        }

        if (traitSpawnInterval > 0f)
        {
            traitSpawnTimer += Time.deltaTime;
            if (traitSpawnTimer >= traitSpawnInterval)
            {
                traitSpawnTimer -= traitSpawnInterval;
                TrySpawnTraitTile();
            }
        }

        if (itemSpawnInterval > 0f)
        {
            itemSpawnTimer += Time.deltaTime;
            if (itemSpawnTimer >= itemSpawnInterval)
            {
                itemSpawnTimer -= itemSpawnInterval;
                SpawnQueuedAtSelector();
            }
        }

        // Ghost adjacency merge
        UpdateGhostMerges(Time.deltaTime);

        // Trait adjacency merge
        UpdateTraitMerges(Time.deltaTime);

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
        int spawned = 0;
        int attempts = 0;
        while (spawned < initialTraitCount && attempts < grid.Length * 4)
        {
            attempts++;
            int idx = rng.Next(0, grid.Length);
            var p = IdxToPos(idx);
            if (IsDeliveryZone(p)) continue;
            if (grid[idx] != null) continue;

            grid[idx] = BoxEntity.CreateTraitTile(RandomTraitTile());
            spawned++;
        }
    }

    // ----------------------------
    // INPUT
    // ----------------------------
    void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool moved = false;

        // Selector movement (WASD)
        if (kb.wKey.wasPressedThisFrame) { selector.y = Mathf.Max(0, selector.y - 1); moved = true; }
        if (kb.sKey.wasPressedThisFrame) { selector.y = Mathf.Min(gridSize - 1, selector.y + 1); moved = true; }
        if (kb.aKey.wasPressedThisFrame) { selector.x = Mathf.Max(0, selector.x - 1); moved = true; }
        if (kb.dKey.wasPressedThisFrame) { selector.x = Mathf.Min(gridSize - 1, selector.x + 1); moved = true; }

        // Shove (Arrow keys)
        if (kb.upArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(0, -1));
        if (kb.downArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(0, 1));
        if (kb.leftArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(-1, 0));
        if (kb.rightArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(1, 0));

        // Enter: if on delivery zone, submit selected tile; otherwise spawn if empty
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            int idx = PosToIdx(selector);
            if (IsDeliveryZone(selector))
            {
                AttemptSubmit();
            }
            else if (grid[idx] == null)
            {
                SpawnBoxAtSelector();
            }
            else
            {
                status = "NOT DELIVERY";
            }
        }

        if (moved) RenderAll();
    }

    // ----------------------------
    // MOVEMENT
    // ----------------------------
    void ShoveSelected(Vector2Int dir)
    {
        int curIdx = PosToIdx(selector);
        var box = grid[curIdx];
        if (box == null) return;

        // Haunted items invert control
        if (box.Has(TraitType.Haunted))
        {
            dir = -dir;
        }

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

        var sub = DequeueConveyor();
        bufferView?.Set(conveyor.ToArray());

        grid[idx] = new BoxEntity(sub);

        status = "SPAWNED";
        // Update just the spawned cell immediately, then refresh HUD
        if (cells != null && idx >= 0 && idx < cells.Length)
        {
            var p = IdxToPos(idx);
            cells[idx].SetCell(grid[idx], p == selector, IsDeliveryZone(p));
            RenderHud();
        }
        else
        {
            RenderAll();
        }
    }

    void SpawnQueuedAtSelector()
    {
        int idx = PosToIdx(selector);
        if (grid[idx] != null)
        {
            status = "SPAWN BLOCKED";
            return;
        }

        var sub = DequeueConveyor();
        bufferView?.Set(conveyor.ToArray());

        grid[idx] = new BoxEntity(sub);
        status = "AUTO SPAWN";
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
            SpawnQueuedAtSelector();
            currentOrder.timeLeft = orderLifetime;

            if (reputation <= minReputation) GameOver();
        }
    }

    void GenerateNewOrder()
    {
        var traits = RandomRequiredTraits();
        currentOrder = new Order
        {
            subType = RandomItemType(),
            requiredTraits = traits,
            timeLeft = orderLifetime
        };

        wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTraits, currentOrder.timeLeft, orderLifetime);
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

        if (IsTraitTile(box))
        {
            status = "NOTHING";
            return;
        }

        if (IsGhost(box))
        {
            status = "GHOST BLOCKS SUBMIT";
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
        bool okTrait = currentOrder.requiredTraits.All(t => box.traits.Contains(t));

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
        InitDeliveryZones();
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
            // Shuffle directions for better movement chances
            for (int k = 0; k < dirs.Length; k++)
            {
                int swap = rng.Next(k, dirs.Length);
                (dirs[k], dirs[swap]) = (dirs[swap], dirs[k]);
            }

            var mover = grid[i];
            bool isGhost = IsGhost(mover);
            double moveChance = isGhost ? 0.8 : 0.6;
            if (rng.NextDouble() > moveChance) continue;

            bool moved = false;
            foreach (var d in dirs)
            {
                var np = p + d;
                if (!InBounds(np)) continue;

                int ni = PosToIdx(np);
                if (grid[ni] != null)
                {
                    // Ghosts can push normal items
                    if (isGhost && IsPushable(grid[ni]))
                    {
                        var pushPos = np + d;
                        if (InBounds(pushPos))
                        {
                            int pi = PosToIdx(pushPos);
                            if (grid[pi] == null)
                            {
                                grid[pi] = grid[ni];
                                grid[ni] = mover;
                                grid[i] = null;
                                moved = true;
                                break;
                            }
                        }
                    }
                    continue;
                }

                grid[ni] = mover;
                grid[i] = null;
                moved = true;
                break;
            }

            if (moved) continue;
        }
    }

    void UpdateGhostMerges(float dt)
    {
        var aliveGhosts = new HashSet<string>();
        var dirs = new[]
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        for (int i = 0; i < grid.Length; i++)
        {
            var ghost = grid[i];
            if (!IsGhost(ghost)) continue;

            aliveGhosts.Add(ghost.id);
            var p = IdxToPos(i);

            BoxEntity target = null;
            int targetIdx = -1;
            foreach (var d in dirs)
            {
                var np = p + d;
                if (!InBounds(np)) continue;
                int ni = PosToIdx(np);
                var cand = grid[ni];
                if (cand == null || IsGhost(cand)) continue;
                target = cand;
                targetIdx = ni;
                break;
            }

            if (target == null)
            {
                if (ghostMerge.TryGetValue(ghost.id, out var st))
                {
                    st.timer = 0f;
                    st.targetId = null;
                }
                continue;
            }

            if (!ghostMerge.TryGetValue(ghost.id, out var state))
            {
                state = new GhostMergeState();
            }

            if (state.targetId == target.id)
            {
                state.timer += dt;
            }
            else
            {
                state.targetId = target.id;
                state.timer = dt;
            }

            if (state.timer >= ghostMergeSeconds)
            {
                foreach (var t in target.traits)
                {
                    ghost.AddTrait(t);
                }

                if (target.Has(TraitType.Fire))
                {
                    ghost.fireTimer = Mathf.Max(ghost.fireTimer, target.fireTimer);
                }

                grid[targetIdx] = null;
                state.timer = 0f;
                state.targetId = null;
                status = "GHOST MERGE";
            }

            ghostMerge[ghost.id] = state;
        }

        // cleanup orphaned states
        if (ghostMerge.Count > 0)
        {
            var dead = new List<string>();
            foreach (var kv in ghostMerge)
            {
                if (!aliveGhosts.Contains(kv.Key)) dead.Add(kv.Key);
            }
            foreach (var id in dead) ghostMerge.Remove(id);
        }
    }

    void UpdateTraitMerges(float dt)
    {
        var aliveBoxes = new HashSet<string>();
        var dirs = new[]
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        for (int i = 0; i < grid.Length; i++)
        {
            var box = grid[i];
            if (box == null || IsGhost(box) || IsTraitTile(box)) continue;

            aliveBoxes.Add(box.id);
            var p = IdxToPos(i);

            BoxEntity tile = null;
            int tileIdx = -1;
            foreach (var d in dirs)
            {
                var np = p + d;
                if (!InBounds(np)) continue;
                int ni = PosToIdx(np);
                var cand = grid[ni];
                if (cand == null || !IsTraitTile(cand)) continue;
                tile = cand;
                tileIdx = ni;
                break;
            }

            if (tile == null)
            {
                if (traitMerge.TryGetValue(box.id, out var st))
                {
                    st.timer = 0f;
                    st.targetId = null;
                }
                continue;
            }

            if (!traitMerge.TryGetValue(box.id, out var state))
            {
                state = new GhostMergeState();
            }

            if (state.targetId == tile.id)
            {
                state.timer += dt;
            }
            else
            {
                state.targetId = tile.id;
                state.timer = dt;
            }

            if (state.timer >= traitMergeSeconds)
            {
                if (!box.Has(tile.tileTrait))
                {
                    box.AddTrait(tile.tileTrait);
                    grid[tileIdx] = null;
                    status = $"ABSORBED {tile.tileTrait}".ToUpper();
                }

                state.timer = 0f;
                state.targetId = null;
            }

            traitMerge[box.id] = state;
        }

        if (traitMerge.Count > 0)
        {
            var dead = new List<string>();
            foreach (var kv in traitMerge)
            {
                if (!aliveBoxes.Contains(kv.Key)) dead.Add(kv.Key);
            }
            foreach (var id in dead) traitMerge.Remove(id);
        }
    }

    void TrySpawnTraitTile()
    {
        var empty = new List<int>();
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] != null) continue;
            var p = IdxToPos(i);
            if (IsDeliveryZone(p)) continue;
            empty.Add(i);
        }

        if (empty.Count == 0) return;
        int idx = empty[rng.Next(0, empty.Count)];

        grid[idx] = BoxEntity.CreateTraitTile(RandomTraitTile());
    }

    void TrySpawnGhost()
    {
        int ghostCount = grid.Count(b => IsGhost(b));
        if (ghostCount >= maxGhosts) return;

        var empty = new List<int>();
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] != null) continue;
            var p = IdxToPos(i);
            if (IsDeliveryZone(p)) continue;
            empty.Add(i);
        }

        if (empty.Count == 0) return;
        int idx = empty[rng.Next(0, empty.Count)];

        var ghost = new BoxEntity(ItemSubType.Ghost);
        ghost.AddTrait(TraitType.Sentient);
        grid[idx] = ghost;
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
    class GhostMergeState
    {
        public string targetId;
        public float timer;
    }

    bool InBounds(Vector2Int p) => p.x >= 0 && p.x < gridSize && p.y >= 0 && p.y < gridSize;
    int PosToIdx(Vector2Int p) => p.y * gridSize + p.x;
    Vector2Int IdxToPos(int idx) => new Vector2Int(idx % gridSize, idx / gridSize);

    bool IsDeliveryZone(Vector2Int p) => deliveryZones.Contains(p);
    bool IsGhost(BoxEntity b) => b != null && b.subType == ItemSubType.Ghost;
    bool IsTraitTile(BoxEntity b) => b != null && b.isTraitTile;
    bool IsPushable(BoxEntity b) => b != null && !IsGhost(b) && !IsTraitTile(b);

    ItemSubType RandomItemType()
    {
        // Only spawn real item types (exclude Ghost)
        var vals = new[] { ItemSubType.Bread, ItemSubType.Knife, ItemSubType.WaterBottle };
        return vals[rng.Next(0, vals.Length)];
    }

    ItemSubType DequeueConveyor()
    {
        var sub = conveyor.Peek();
        conveyor.Dequeue();
        conveyor.Enqueue(RandomItemType());
        return sub;
    }

    List<TraitType> RandomRequiredTraits()
    {
        // Only traits that an order can demand
        var candidates = new[] { TraitType.Fire, TraitType.Ice, TraitType.Sentient };
        int max = Mathf.Clamp(maxOrderTraits, 1, candidates.Length);
        int min = Mathf.Clamp(minOrderTraits, 1, max);
        int count = rng.Next(min, max + 1);

        var set = new HashSet<TraitType>();
        while (set.Count < count)
        {
            set.Add(candidates[rng.Next(0, candidates.Length)]);
        }

        return set.ToList();
    }

    TraitType RandomTraitTile()
    {
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
            wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTraits, currentOrder.timeLeft, orderLifetime);
        }

        bufferView?.Set(conveyor.ToArray());
    }
}
