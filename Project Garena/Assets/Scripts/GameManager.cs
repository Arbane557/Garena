using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using TMPro;
using DG.Tweening;
using Template.Audio;
using Template.Core;

public class GameManager : MonoBehaviour
{
    [Header("Config")]
    public int gridSize = 10;
    public float tickRateSeconds = 1.5f;
    public int deliveryZoneCount = 5;
    public int initialTraitCount = 15;

    [Header("UI (uGUI + TMP)")]
    public Transform gridParent;
    public CellView cellPrefab;

    public TMP_Text reputationText;
    public TMP_Text orderTimerText;
    public TMP_Text fullnessText;
    public TMP_Text statusText;
    public Image energyFill;
    public Image hpLockFill;
    public Image weightLockFill;
    public Image heatLockFill;
    public Image coldLockFill;
    public float peakBarWidth = 420f;
    public float peakBarHeight = 18f;
    public bool peakBarVertical = false;
    public GameObject peakBarPrefab;
    public RectTransform peakBarParent;
    public RectTransform peakBarRoot;

    public WantedView wantedView;
    public BufferView bufferView;
    public GameObject slashPrefab;
    public float slashLifeSeconds = 5f;
    public float slashSpinSeconds = 0.22f;
    public float slashSpinDegrees = 540f;

    [Header("Game Rules")]
    public int reputation = 50;
    public int minReputation = 0;
    public float E_max_base = 100f;
    public float E;
    public float L_hp;
    public float L_weight;
    public float L_heat;
    public float L_cold;
    public float L_hp_cap = 60f;
    public float dmgWrong = 12f;
    public float healGood = 3f;

    public float orderInterval = 6f;
    public float orderLifetime = 12f;

    [Header("Order Traits")]
    [Range(0, 3)] public int minOrderTraits = 1;
    [Range(0, 3)] public int maxOrderTraits = 2;

    [Header("Customers")]
    public List<CustomerLevel> customerLevels = new List<CustomerLevel>();
    public string tutorialSpeakerName = "Guide";
    [TextArea(2, 6)] public List<string> tutorialBeforeFirst = new List<string>
    {
        "Welcome. Keep items organized and watch the order box.",
        "Use the grid to move items and fulfill requests.",
        "You'll learn the rest the hard way."
    };
    [TextArea(2, 6)] public List<string> tutorialAfterBaker = new List<string>
    {
        "I hope this line of work is what you expected."
    };

    [Header("Progression")]
    public int ordersBeforeChaos = 7;
    public int initialItemCount = 18;
    public bool spawnInitialItems = true;
    public bool spawnInitialTraits = false;
    public bool allowTraitTilesBeforeChaos = false;
    public bool allowGhostsBeforeChaos = false;
    public bool forceFirstChaosOrder = true;
    public int initialSpawnBottomRows = 3;
    public int initialSpawnClusterRadius = 2;
    public Image chaosTint;
    public float chaosTintAlpha = 0.25f;
    public float chaosTintFlashSeconds = 0.35f;
    public float chaosTraitSpawnDelay = 8f;
    public float chaosGhostSpawnDelay = 8f;

    [Header("Ghosts")]
    public int maxGhosts = 4;
    public float ghostSpawnInterval = 2.5f;
    public float ghostMergeSeconds = 2.0f;

    [Header("Trait Tiles")]
    public float traitSpawnInterval = 5.5f;
    public float traitMergeSeconds = 2.0f;
    [Range(0f, 1f)] public float maxTraitFraction = 0.30f;
    [Range(0f, 1f)] public float minEmptyFraction = 0.40f;

    [Header("Items")]
    public float itemSpawnInterval = 5.0f;

    [Header("Sentient Tick")]
    public float sentientTickInterval = 1.0f;
    public float regenDelay = 0.35f;
    public float regenRate = 18f;
    public float costPush = 4f;
    public float costPush2x1 = 5f;
    public float costSubmit = 2f;
    public float costPerSlide = 1f;

    [Header("Weight Lock")]
    [Range(0f, 1f)] public float weightStart = 0.55f;
    [Range(0f, 1f)] public float weightEnd = 0.95f;
    public float weightMaxLock = 30f;

    [Header("Heat/Cold")]
    public float heatMax = 30f;
    public float heatBuildRate = 8f;
    public float heatRecoverRate = 10f;
    public float coldMax = 25f;
    public float coldBuildRate = 6f;
    public float coldRecoverRate = 8f;
    public float heatOverload = 24f;
    public float coldOverload = 20f;

    [Header("Fire Aura")]
    public float fireAuraDamagePerSecond = 6f;

    // STATE
    private BoxEntity[] grid;
    private List<Vector2Int> deliveryZones = new List<Vector2Int>();
    private Queue<ItemSubType> conveyor = new Queue<ItemSubType>();
    private List<ItemSubType> itemBag = new List<ItemSubType>();
    private ItemSubType? lastQueuedItem = null;
    private Vector2Int selector = new Vector2Int(0, 0);

    private string status = "System Stable";
    private string lastStatus = null;
    private int lastRep = int.MinValue;

    private Order currentOrder;
    private float orderSpawnTimer = 0f;

    private float tickTimer = 0f;
    private float sentientTickTimer = 0f;
    private float ghostSpawnTimer = 0f;
    private float traitSpawnTimer = 0f;
    private float itemSpawnTimer = 0f;
    private float ghostMoveTimer = 0f;
    private float lastActionTimer = 0f;
    private float heat = 0f;
    private float cold = 0f;
    private float fireImmuneTimer = 0f;
    private float iceImmuneTimer = 0f;
    private float frozenTimer = 0f;
    private int frozenMoves = 0;
    private bool physicsMode = false;
    public float physicsGravity = 8.5f;
    public float physicsMass = 5.0f;
    public float physicsImpulse = 14f;
    public float physicsScatterAngle = 60f;
    public float physicsModeDuration = 12f;
    private int lastScreenW = -1;
    private int lastScreenH = -1;
    public RectTransform bagBoundary;
    [Range(0.02f, 0.5f)] public float autoSubmitBand = 0.12f;
    private float glitchTimer = 0f;
    public float glitchInterval = 6f;
    [Range(0f, 1f)] public float glitchChance = 0.08f;
    private float physicsModeTimer = 0f;
    private int ordersCompleted = 0;
    private bool chaosUnlocked = false;
    private float chaosTimer = 0f;
    private int currentCustomerIndex = 0;
    private int currentCustomerOrderIndex = 0;
    private float customerFireTimer = 0f;
    private float fireSpreadTimer = 0f;
    private float customerIceTimer = 0f;
    private float customerGhostTimer = 0f;
    private string currentCustomerFlavor = null;
    private string currentCustomerName = null;
    private bool[] fireAuraCache;
    private bool[] iceAuraCache;
    private Dictionary<string, float> iceAuraTime = new Dictionary<string, float>();
    private bool awaitingDialogue = false;

    public List<Ghost> ghosts = new List<Ghost>();

    private CellView[] cells;
    private System.Random rng;
    private Dictionary<string, GhostMergeState> ghostMerge = new Dictionary<string, GhostMergeState>();
    private Dictionary<string, GhostMergeState> traitMerge = new Dictionary<string, GhostMergeState>();
    private Dictionary<string, Vector2Int> lastAnchor = new Dictionary<string, Vector2Int>();

    void Awake()
    {
        rng = new System.Random();
    }

    void Start()
    {
        grid = new BoxEntity[gridSize * gridSize];
        E = E_max_base;
        BuildPeakBarIfMissing();
        EnsureUIForDragging();

        BuildGridUI();
        InitDeliveryZones();
        InitConveyor();
        if (spawnInitialItems) InitInitialItems();
        if (spawnInitialTraits) InitInitialTraits();      // optional (spawns initial traits on boxes if you want, else remove)
        EnsureCustomerLevels();
        StartTutorialThenLevel0();
        PlayBgm("BGM");

        RenderAll();
        RefreshBagBoundary();
    }

    void Update()
    {
        HandleInput();

        // Timers
        fireImmuneTimer = Mathf.Max(0f, fireImmuneTimer - Time.deltaTime);
        iceImmuneTimer = Mathf.Max(0f, iceImmuneTimer - Time.deltaTime);
        if (frozenTimer > 0f) frozenTimer = Mathf.Max(0f, frozenTimer - Time.deltaTime);
        lastActionTimer += Time.deltaTime;

        foreach (var ent in EnumerateEntities())
        {
            if (ent == null) continue;
            ent.activeCooldown = Mathf.Max(0f, ent.activeCooldown - Time.deltaTime);
        }

        UpdateLocksAndRegen(Time.deltaTime);
        UpdateFireAuraDamage(Time.deltaTime);

        // Order system
        UpdateOrder(Time.deltaTime);

        // Customer level fire gimmick
        UpdateCustomerFire(Time.deltaTime);
        UpdateFireSpread(Time.deltaTime);
        UpdateCustomerIce(Time.deltaTime);
        UpdateIceAura(Time.deltaTime);

        // Fire expiry
        UpdateFireTimers(Time.deltaTime);

        // Ghosts: timed movement
        UpdateGhosts(Time.deltaTime);

        // Sentient movement (timed, like ghosts)
        sentientTickTimer += Time.deltaTime;
        if (sentientTickTimer >= sentientTickInterval)
        {
            sentientTickTimer -= sentientTickInterval;
            SentientTick();
        }

        // Timed spawns
        UpdateCustomerGhosts(Time.deltaTime);

        if (traitSpawnInterval > 0f && (chaosUnlocked || allowTraitTilesBeforeChaos))
        {
            traitSpawnTimer += Time.deltaTime;
            bool traitDelayOk = !chaosUnlocked || chaosTimer >= chaosTraitSpawnDelay;
            if (traitDelayOk && traitSpawnTimer >= traitSpawnInterval)
            {
                traitSpawnTimer -= traitSpawnInterval;
                TrySpawnTraitTile();
            }
        }

        // Auto drop disabled.

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

        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
        {
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            if (physicsMode) RefreshPhysicsColliders();
            RefreshBagBoundary();
        }

        if (physicsMode)
        {
            physicsModeTimer -= Time.deltaTime;
            if (physicsModeTimer <= 0f)
            {
                physicsMode = false;
                SetPhysicsMode(false);
                status = "PHYSICS ENDED";
            }
        }

        RenderHud();
    }

    // ----------------------------
    // INIT
    // ----------------------------
    void BuildGridUI()
    {
        if (gridParent == null || cellPrefab == null) return;
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(gridParent.GetChild(i).gameObject);
        }
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
    }

    void InitConveyor()
    {
        conveyor.Clear();
        conveyor.Enqueue(ItemSubType.Bread);
        conveyor.Enqueue(ItemSubType.Knife);
        conveyor.Enqueue(ItemSubType.WaterBottle);
        RefillItemBag();
        bufferView?.Set(conveyor.ToArray());
    }

    // Keep this only if you still want some boxes pre-seeded.
    // If not needed, delete this function and its call.
    void InitInitialTraits()
    {
        int spawned = 0;
        int attempts = 0;
        int maxTraits = Mathf.FloorToInt(grid.Length * maxTraitFraction);
        while (spawned < initialTraitCount && attempts < grid.Length * 4)
        {
            attempts++;
            if (CountTraitTiles() >= maxTraits) break;
            if (!HasEmptyCapacity(1)) break;
            int idx = rng.Next(0, grid.Length);
            var p = IdxToPos(idx);
            if (IsDeliveryZone(p)) continue;
            if (grid[idx] != null) continue;

            var t = BoxEntity.CreateTraitTile(RandomTraitTile());
            t.size = Vector2Int.one;
            PlaceEntity(t, p);
            spawned++;
        }
    }

    void InitInitialItems()
    {
        int spawned = 0;
        int attempts = 0;
        int bottomRows = Mathf.Clamp(initialSpawnBottomRows, 1, gridSize);
        var clusterCenter = new Vector2Int(rng.Next(0, gridSize), rng.Next(gridSize - bottomRows, gridSize));
        while (spawned < initialItemCount && attempts < grid.Length * 6)
        {
            attempts++;
            var sub = RandomItemType();
            var size = GetSizeForSubType(sub);
            int cx = clusterCenter.x + rng.Next(-initialSpawnClusterRadius, initialSpawnClusterRadius + 1);
            int cy = clusterCenter.y + rng.Next(-initialSpawnClusterRadius, initialSpawnClusterRadius + 1);
            var anchor = new Vector2Int(Mathf.Clamp(cx, 0, gridSize - 1), Mathf.Clamp(cy, gridSize - bottomRows, gridSize - 1));
            if (IsDeliveryZone(anchor)) continue;
            if (!CanPlaceAt(anchor, size)) continue;

            var e = new BoxEntity(sub);
            e.size = size;
            PlaceEntity(e, anchor);
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

        bool frozen = IsFrozen();

        // Shove (Arrow keys)
        if (!frozen)
        {
            if (kb.upArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(0, -1));
            if (kb.downArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(0, 1));
            if (kb.leftArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(-1, 0));
            if (kb.rightArrowKey.wasPressedThisFrame) ShoveSelected(new Vector2Int(1, 0));
        }
        else if (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
        {
            ConsumeFrozenAttempt();
        }

        if (!frozen && kb.eKey.wasPressedThisFrame) UseSelected();
        else if (frozen && kb.eKey.wasPressedThisFrame) ConsumeFrozenAttempt();

        if (kb.jKey.wasPressedThisFrame)
        {
            physicsMode = !physicsMode;
            SetPhysicsMode(physicsMode);
        }

        // Enter: submit if pushing above top edge; otherwise spawn if empty
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            int idx = PosToIdx(selector);
            var box = grid[idx];
            if (box != null && !IsTraitTile(box) && !IsGhost(box) && IsAtTopBoundary(box))
            {
                if (!frozen) AttemptSubmit(box, true);
                else ConsumeFrozenAttempt();
            }
            else if (grid[idx] == null)
            {
                if (!frozen) SpawnBoxAtSelector();
                else ConsumeFrozenAttempt();
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

        if (IsGhost(box))
        {
            status = "GHOSTS ARE UNCONTROLLABLE";
            return;
        }

        if (IsTraitTile(box))
        {
            status = "IMMOBILE";
            return;
        }

        // Haunted/Sentient items invert control
        if (box.Has(TraitType.Haunted) || box.Has(TraitType.Sentient))
        {
            dir = -dir;
        }

        // Submit when pushing up at the top edge
        if (dir == new Vector2Int(0, -1) && IsAtTopBoundary(box))
        {
            AttemptSubmit(box, true);
            return;
        }

        bool moved = false;
        int steps = 0;
        if (box.Has(TraitType.Ice))
        {
            while (TryMoveEntity(box, dir, allowPush: false))
            {
                moved = true;
                steps++;
            }
        }
        else
        {
            moved = TryMoveEntity(box, dir, allowPush: false);
        }
        if (!moved)
        {
            status = "BLOCKED";
            return;
        }

        // Keep selector following the moved box (anchor position)
        selector = box.anchor;
        status = box.Has(TraitType.Ice) ? "SLID" : "MOVED";
        PlaySfx("click");
        RenderAll();
        UpdateAnchorCache();
    }

    void SpawnBoxAtSelector()
    {
        int idx = PosToIdx(selector);
        var sub = DequeueConveyor();
        var size = GetSizeForSubType(sub);
        var anchor = selector;
        if (!CanPlaceAt(anchor, size))
        {
            status = "SPAWN BLOCKED";
            SpawnBlockedFeedback(size);
            return;
        }

        bufferView?.Set(conveyor.ToArray());

        var en = new BoxEntity(sub);
        en.size = size;
        en.anchor = anchor;
        PlaceEntity(en, anchor);

        status = "SPAWNED";
        PlaySfx("place");
        // Update just the spawned cell immediately, then refresh HUD
        if (cells != null && idx >= 0 && idx < cells.Length)
        {
            var p = IdxToPos(idx);
            var e = grid[idx];
            var from = (e != null && lastAnchor.TryGetValue(e.id, out var prev)) ? prev : p;
            BuildFireAuraCache();
            bool inFireAura = fireAuraCache != null && idx >= 0 && idx < fireAuraCache.Length && fireAuraCache[idx];
            BuildIceAuraCache();
            bool inIceAura = iceAuraCache != null && idx >= 0 && idx < iceAuraCache.Length && iceAuraCache[idx];
            cells[idx].SetCell(grid[idx], p == selector, false, inFireAura, inIceAura, p, from);
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
        var sub = DequeueConveyor();
        var size = GetSizeForSubType(sub);
        var anchor = selector;
        if (!CanPlaceAt(anchor, size))
        {
            status = "SPAWN BLOCKED";
            SpawnBlockedFeedback(size);
            return;
        }
        bufferView?.Set(conveyor.ToArray());

        var e = new BoxEntity(sub);
        e.size = size;
        e.anchor = anchor;
        PlaceEntity(e, anchor);
        status = "AUTO SPAWN";
        PlaySfx("place");
        RenderAll();
    }

    // ----------------------------
    // ORDERS + SUBMIT
    // ----------------------------
    void UpdateOrder(float dt)
    {
        if (awaitingDialogue) return;
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
            currentOrder.timeLeft = orderLifetime;

            if (reputation <= minReputation) GameOver();
        }
    }

    void GenerateNewOrder()
    {
        if (customerLevels == null || customerLevels.Count == 0)
        {
            currentOrder = null;
            return;
        }
        var level = customerLevels[Mathf.Clamp(currentCustomerIndex, 0, customerLevels.Count - 1)];
        if (level.orders == null || level.orders.Count == 0)
        {
            currentOrder = null;
            return;
        }

        currentCustomerOrderIndex = Mathf.Clamp(currentCustomerOrderIndex, 0, level.orders.Count - 1);
        var spec = level.orders[currentCustomerOrderIndex];
        currentOrder = new Order
        {
            subType = spec.subType,
            requiredTraits = new List<TraitType>(spec.requiredTraits ?? new List<TraitType>()),
            timeLeft = orderLifetime
        };

        wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTraits, currentOrder.timeLeft, orderLifetime, currentCustomerName, currentCustomerFlavor);
    }

    void EnsureCustomerLevels()
    {
        if (customerLevels != null && customerLevels.Count > 0) return;

        customerLevels = new List<CustomerLevel>
        {
            new CustomerLevel
            {
                id = "traveler",
                displayName = "Traveler",
                flavorLine = "I have a long road ahead. Keep it simple.",
                orders = new List<OrderSpec>
                {
                    OrderSpec.Of(ItemSubType.Bread)
                },
                enableChaosSpawns = false,
                enableFireSpawns = false
            },
            new CustomerLevel
            {
                id = "baker",
                displayName = "Baker",
                flavorLine = "Bread and blades. I burn through both.",
                orders = new List<OrderSpec>
                {
                    OrderSpec.Of(ItemSubType.Bread),
                    OrderSpec.Of(ItemSubType.Knife)
                },
                enableChaosSpawns = false,
                enableFireSpawns = false
            },
            new CustomerLevel
            {
                id = "necromancer",
                displayName = "Necromancer",
                flavorLine = "THE DEAD WANTS TOOLS THAT WON'T OBEY!",
                orders = new List<OrderSpec>
                {
                    OrderSpec.Of(ItemSubType.Knife),
                    OrderSpec.Of(ItemSubType.WaterBottle),
                    OrderSpec.Of(ItemSubType.Bread)
                },
                enableChaosSpawns = false,
                enableGhostSpawns = true,
                ghostSpawnInterval = 6f,
                ghostBurstCount = 4
            },
            new CustomerLevel
            {
                id = "fire_mage",
                displayName = "Fire Mage",
                flavorLine = "Heat it. Temper it. Bring it to me.",
                orders = new List<OrderSpec>
                {
                    OrderSpec.Of(ItemSubType.Bread, TraitType.Fire),
                    OrderSpec.Of(ItemSubType.Knife, TraitType.Fire),
                    OrderSpec.Of(ItemSubType.WaterBottle, TraitType.Fire)
                },
                enableChaosSpawns = false,
                enableFireSpawns = true,
                fireSpawnInterval = 5f,
                fireBurstCount = 3,
                enableFireSpread = true,
                fireSpreadInterval = 8f,
                fireSpreadChance = 0.25f
            },
            new CustomerLevel
            {
                id = "ice_mage",
                displayName = "Ice Mage",
                flavorLine = "Freeze it. Seal it. Let it linger.",
                orders = new List<OrderSpec>
                {
                    OrderSpec.Of(ItemSubType.Bread, TraitType.Ice),
                    OrderSpec.Of(ItemSubType.Knife, TraitType.Ice),
                    OrderSpec.Of(ItemSubType.WaterBottle, TraitType.Ice)
                },
                enableChaosSpawns = false,
                enableIceSpawns = false,
                iceSpawnInterval = 8f,
                iceBurstCount = 10,
                spawnInitialIceBurst = true,
                enableIceAura = true,
                iceToItemSeconds = 2f
            }
        };
    }

    void StartTutorialThenLevel0()
    {
        if (tutorialBeforeFirst == null || tutorialBeforeFirst.Count == 0)
        {
            StartCustomerLevel(0);
            return;
        }

        awaitingDialogue = true;
        currentOrder = null;
        orderSpawnTimer = 0f;
        PopUp.SetDialogueMode(autoAdvance: false, advanceOnEnter: true);
        PopUp.WriteSequence(tutorialSpeakerName, tutorialBeforeFirst, () =>
        {
            awaitingDialogue = false;
            PopUp.SetDialogueMode(autoAdvance: true, advanceOnEnter: false);
            StartCustomerLevel(0);
        });
    }

    void StartCustomerLevel(int levelIndex)
    {
        if (customerLevels == null || customerLevels.Count == 0) return;

        currentCustomerIndex = Mathf.Clamp(levelIndex, 0, customerLevels.Count - 1);
        currentCustomerOrderIndex = 0;
        var level = customerLevels[currentCustomerIndex];
        currentCustomerFlavor = level.flavorLine;
        currentCustomerName = level.displayName;

        chaosUnlocked = level.enableChaosSpawns;
        chaosTimer = 0f;
        customerFireTimer = 0f;
        fireSpreadTimer = 0f;
        customerIceTimer = 0f;
        customerGhostTimer = 0f;
        iceAuraTime.Clear();

        if (level.enableGhostSpawns && level.ghostBurstCount > 0)
        {
            int burst = Mathf.Max(1, level.ghostBurstCount);
            for (int i = 0; i < burst; i++)
            {
                TrySpawnGhost();
            }
        }
        else if (level.enableGhostSpawns)
        {
            // Spawn immediately on the next tick instead of waiting a full interval.
            customerGhostTimer = level.ghostSpawnInterval;
        }

        if (level.enableFireSpawns)
        {
            int burst = Mathf.Max(1, level.fireBurstCount);
            for (int i = 0; i < burst; i++)
            {
                TrySpawnTraitTile(TraitType.Fire);
            }
        }

        if (level.enableIceSpawns || level.spawnInitialIceBurst)
        {
            ClearAllFire();
            int burst = Mathf.Max(1, level.iceBurstCount);
            for (int i = 0; i < burst; i++)
            {
                TrySpawnTraitTile(TraitType.Ice);
            }
        }

        GenerateNewOrder();
    }

    void AdvanceCustomerOrder()
    {
        if (customerLevels == null || customerLevels.Count == 0)
        {
            currentOrder = null;
            return;
        }

        var level = customerLevels[currentCustomerIndex];
        currentCustomerOrderIndex++;
        if (level.orders == null || currentCustomerOrderIndex >= level.orders.Count)
        {
            int nextLevel = currentCustomerIndex + 1;
            if (nextLevel < customerLevels.Count)
            {
                if (level.id == "baker" && tutorialAfterBaker != null && tutorialAfterBaker.Count > 0)
                {
                    awaitingDialogue = true;
                    currentOrder = null;
                    orderSpawnTimer = 0f;
                    PopUp.SetDialogueMode(autoAdvance: true, advanceOnEnter: false);
                    PopUp.WriteSequence(tutorialSpeakerName, tutorialAfterBaker, () =>
                    {
                        awaitingDialogue = true;
                        PopUp.SetDialogueMode(autoAdvance: true, advanceOnEnter: false);
                        StartCustomerLevel(nextLevel);
                    });
                }
                else
                {
                    StartCustomerLevel(nextLevel);
                }
                return;
            }

            // No more customers for now.
            currentOrder = null;
            status = "NO MORE CUSTOMERS";
            RenderHud();
            return;
        }

        GenerateNewOrder();
    }

    void UpdateCustomerFire(float dt)
    {
        if (awaitingDialogue) return;
        if (customerLevels == null || customerLevels.Count == 0) return;
        var level = customerLevels[currentCustomerIndex];
        if (!level.enableFireSpawns) return;

        float interval = Mathf.Max(0.5f, level.fireSpawnInterval);
        customerFireTimer += dt;
        if (customerFireTimer >= interval)
        {
            customerFireTimer = 0f;
            int burst = Mathf.Max(1, level.fireBurstCount);
            for (int i = 0; i < burst; i++)
            {
                TrySpawnTraitTile(TraitType.Fire);
            }
        }
    }

    void UpdateCustomerIce(float dt)
    {
        if (awaitingDialogue) return;
        if (customerLevels == null || customerLevels.Count == 0) return;
        var level = customerLevels[currentCustomerIndex];
        if (!level.enableIceSpawns) return;

        float interval = Mathf.Max(0.5f, level.iceSpawnInterval);
        customerIceTimer += dt;
        if (customerIceTimer >= interval)
        {
            customerIceTimer = 0f;
            int burst = Mathf.Max(1, level.iceBurstCount);
            for (int i = 0; i < burst; i++)
            {
                TrySpawnTraitTile(TraitType.Ice);
            }
        }
    }

    void UpdateCustomerGhosts(float dt)
    {
        if (awaitingDialogue) return;
        if (customerLevels == null || customerLevels.Count == 0) return;
        var level = customerLevels[currentCustomerIndex];
        if (!level.enableGhostSpawns) return;

        float interval = Mathf.Max(0.5f, level.ghostSpawnInterval);
        customerGhostTimer += dt;
        if (customerGhostTimer >= interval)
        {
            customerGhostTimer = 0f;
            TrySpawnGhost();
        }
    }

    void UpdateIceAura(float dt)
    {
        if (awaitingDialogue) return;
        if (customerLevels == null || customerLevels.Count == 0) return;
        var level = customerLevels[currentCustomerIndex];
        if (!level.enableIceAura) return;

        BuildIceAuraCache();

        var alive = new HashSet<string>();
        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (IsTraitTile(e) || IsGhost(e)) continue;

            alive.Add(e.id);
            int idx = PosToIdx(e.anchor);
            bool inAura = iceAuraCache != null && idx >= 0 && idx < iceAuraCache.Length && iceAuraCache[idx];

            if (!inAura)
            {
                if (iceAuraTime.ContainsKey(e.id)) iceAuraTime.Remove(e.id);
                continue;
            }

            if (!iceAuraTime.TryGetValue(e.id, out float t))
            {
                t = 0f;
            }
            t += dt;
            iceAuraTime[e.id] = t;

            if (t >= level.iceToItemSeconds && !e.Has(TraitType.Ice))
            {
                e.AddTrait(TraitType.Ice);
                RenderAll();
            }
        }

        if (iceAuraTime.Count > 0)
        {
            var dead = new List<string>();
            foreach (var kv in iceAuraTime)
            {
                if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            }
            foreach (var id in dead) iceAuraTime.Remove(id);
        }
    }

    void UpdateFireSpread(float dt)
    {
        if (awaitingDialogue) return;
        if (customerLevels == null || customerLevels.Count == 0) return;
        var level = customerLevels[currentCustomerIndex];
        if (!level.enableFireSpread) return;

        float interval = Mathf.Max(0.5f, level.fireSpreadInterval);
        fireSpreadTimer += dt;
        if (fireSpreadTimer < interval) return;
        fireSpreadTimer = 0f;

        if (rng.NextDouble() > level.fireSpreadChance) return;

        var fireTiles = new List<Vector2Int>();
        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (IsTraitTile(e) && e.tileTrait == TraitType.Fire)
            {
                fireTiles.Add(e.anchor);
            }
        }

        if (fireTiles.Count == 0) return;
        var origin = fireTiles[rng.Next(0, fireTiles.Count)];
        var dirs = new[]
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };
        // shuffle
        for (int i = 0; i < dirs.Length; i++)
        {
            int j = rng.Next(i, dirs.Length);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        foreach (var d in dirs)
        {
            var p = origin + d;
            if (!InBounds(p)) continue;

            int idx = PosToIdx(p);
            var target = grid[idx];
            if (target == null)
            {
                var t = BoxEntity.CreateTraitTile(TraitType.Fire);
                t.size = Vector2Int.one;
                PlaceEntity(t, p);
                PlaySfx("burning");
                break;
            }

            if (!IsTraitTile(target) && !IsGhost(target) && !target.Has(TraitType.Fire))
            {
                target.AddTrait(TraitType.Fire);
                target.fireTimer = 2f;
                PlaySfx("burning");
                break;
            }
        }
    }

    void ExtinguishFireInArea(Vector2Int center, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            var p = center + new Vector2Int(dx, dy);
            if (!InBounds(p)) continue;
            var e = grid[PosToIdx(p)];
            if (e == null) continue;

            if (IsTraitTile(e) && e.tileTrait == TraitType.Fire)
            {
                RemoveEntity(e);
                continue;
            }

            if (e.Has(TraitType.Fire))
            {
                e.traits.Remove(TraitType.Fire);
                e.fireTimer = 0f;
            }
        }
    }

    void ClearAllFire()
    {
        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (IsTraitTile(e) && e.tileTrait == TraitType.Fire)
            {
                RemoveEntity(e);
                continue;
            }
            if (e.Has(TraitType.Fire))
            {
                e.traits.Remove(TraitType.Fire);
                e.fireTimer = 0f;
            }
        }
    }

    void BuildFireAuraCache()
    {
        if (fireAuraCache == null || fireAuraCache.Length != grid.Length)
        {
            fireAuraCache = new bool[grid.Length];
        }
        else
        {
            System.Array.Clear(fireAuraCache, 0, fireAuraCache.Length);
        }

        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (!IsTraitTile(e) || e.tileTrait != TraitType.Fire) continue;

            var center = e.anchor;
            if (InBounds(center)) fireAuraCache[PosToIdx(center)] = true;
            var dirs = new[]
            {
                new Vector2Int(1,0), new Vector2Int(-1,0),
                new Vector2Int(0,1), new Vector2Int(0,-1)
            };
            foreach (var d in dirs)
            {
                var p = center + d;
                if (!InBounds(p)) continue;
                fireAuraCache[PosToIdx(p)] = true;
            }
        }
    }

    void BuildIceAuraCache()
    {
        if (iceAuraCache == null || iceAuraCache.Length != grid.Length)
        {
            iceAuraCache = new bool[grid.Length];
        }
        else
        {
            System.Array.Clear(iceAuraCache, 0, iceAuraCache.Length);
        }

        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (!IsTraitTile(e) || e.tileTrait != TraitType.Ice) continue;

            var center = e.anchor;
            if (InBounds(center)) iceAuraCache[PosToIdx(center)] = true;
            var dirs = new[]
            {
                new Vector2Int(1,0), new Vector2Int(-1,0),
                new Vector2Int(0,1), new Vector2Int(0,-1)
            };
            foreach (var d in dirs)
            {
                var p = center + d;
                if (!InBounds(p)) continue;
                iceAuraCache[PosToIdx(p)] = true;
            }
        }
    }

    void UpdateFireAuraDamage(float dt)
    {
        if (FireImmune) return;
        BuildFireAuraCache();
        int idx = PosToIdx(selector);
        if (fireAuraCache != null && idx >= 0 && idx < fireAuraCache.Length && fireAuraCache[idx])
        {
            L_hp = Mathf.Min(L_hp_cap, L_hp + fireAuraDamagePerSecond * dt);
        }
    }

    void AttemptSubmit(BoxEntity box, bool bypassZone)
    {
        if (box == null) { status = "NOTHING"; return; }
        if (IsTraitTile(box)) { status = "NOTHING"; return; }
        if (IsGhost(box)) { status = "GHOST BLOCKS SUBMIT"; return; }
        if (!bypassZone && !IsDeliveryZone(selector)) { status = "NOT DELIVERY"; return; }

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
            L_hp = Mathf.Max(0f, L_hp - healGood);
            status = "ORDER FULFILLED";
            PlaySfx("submitsuccess");
        }
        else
        {
            reputation -= 1;
            L_hp = Mathf.Min(L_hp_cap, L_hp + dmgWrong);
            status = $"WRONG: need {currentOrder.subType}({TraitsToStr(currentOrder.requiredTraits)}) got {box.subType}({TraitsToStr(box.traits)})";
            PlaySfx("submitwrong");
            if (reputation <= minReputation) { GameOver(); return; }
        }

        bool wasHaunted = box.Has(TraitType.Haunted);
        var spawnPos = box.anchor;
        RemoveEntity(box);
        if (wasHaunted)
        {
            SpawnGhostAt(spawnPos);
        }
        AdvanceCustomerOrder();
        // no delivery zones
        RenderAll();
    }

    public void AutoCorrectSubmit(BoxEntity box)
    {
        if (box == null) return;
        if (IsTraitTile(box) || IsGhost(box)) return;

        reputation += 5;
        L_hp = Mathf.Max(0f, L_hp - healGood);
        status = "AUTO CORRECT";
        PlaySfx("submitsuccess");

        ordersCompleted++;
        if (!chaosUnlocked && ordersCompleted >= ordersBeforeChaos)
        {
            TriggerChaos();
        }

        bool wasHaunted = box.Has(TraitType.Haunted);
        var spawnPos = box.anchor;
        RemoveEntity(box);
        if (wasHaunted) SpawnGhostAt(spawnPos);
        GenerateNewOrder();

        if (physicsMode)
        {
            DisableCellsForEntity(box);
            RenderHud();
        }
        else
        {
            RenderAll();
        }
    }

    void GameOver()
    {
        // Disabled: no game over state.
        status = "GAME OVER (DISABLED)";
        RenderHud();
    }

    // ----------------------------
    // FIRE TIMER (5s hold)
    // ----------------------------
    void UpdateFireTimers(float dt)
    {
        bool changed = false;
        foreach (var box in EnumerateEntities())
        {
            if (box == null) continue;
            if (box.traits.Contains(TraitType.Fire))
            {
                box.fireTimer -= dt;
                if (box.fireTimer <= 0f)
                {
                    box.traits.Remove(TraitType.Fire);
                    status = "FIRE EXPIRED";
                    changed = true;
                }
            }
        }
        if (changed) RenderAll();
    }

    // ----------------------------
    // GHOSTS
    // ----------------------------
    void UpdateGhosts(float dt)
    {
        ghostMoveTimer += dt;
        if (ghostMoveTimer < tickRateSeconds) return;
        ghostMoveTimer -= tickRateSeconds;

        var movers = new List<BoxEntity>();
        foreach (var b in EnumerateEntities())
        {
            if (b == null) continue;
            if (IsGhost(b)) movers.Add(b);
        }

        var dirs = new[]
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1)
        };

        foreach (var ghost in movers.OrderBy(_ => rng.Next()))
        {
            // Shuffle directions
            for (int k = 0; k < dirs.Length; k++)
            {
                int swap = rng.Next(k, dirs.Length);
                (dirs[k], dirs[swap]) = (dirs[swap], dirs[k]);
            }

            bool moved = false;
            var targetDir = GetGhostChaseDir(ghost.anchor);
            if (targetDir.HasValue)
            {
                var d = targetDir.Value;
                if (TryMoveEntity(ghost, d, allowPush: false))
                {
                    moved = true;
                }
            }

            if (!moved)
            {
                foreach (var d in dirs)
                {
                    if (TryMoveEntity(ghost, d, allowPush: false))
                    {
                        moved = true;
                        break;
                    }
                }
            }

            ApplySentientAuraAt(ghost.anchor);
            if (moved) RenderAll();
        }
    }

    Vector2Int? GetGhostChaseDir(Vector2Int from)
    {
        Vector2Int? best = null;
        int bestDist = int.MaxValue;
        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            if (IsGhost(e) || IsTraitTile(e)) continue;

            var p = e.anchor;
            int dist = Mathf.Abs(p.x - from.x) + Mathf.Abs(p.y - from.y);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }

        if (!best.HasValue) return null;
        var target = best.Value;
        int dx = target.x - from.x;
        int dy = target.y - from.y;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return new Vector2Int(Mathf.Clamp(dx, -1, 1), 0);
        }
        return new Vector2Int(0, Mathf.Clamp(dy, -1, 1));
    }

    void ApplySentientAuraAt(Vector2Int center)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var p = center + new Vector2Int(dx, dy);
            if (!InBounds(p)) continue;

            int idx = PosToIdx(p);
            var box = grid[idx];
            if (box == null) continue;
            if (!IsTraitTile(box) && !IsGhost(box))
            {
                box.traits.Add(TraitType.Sentient);
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
        // snapshot entities to avoid chain-move weirdness
        var movers = new List<BoxEntity>();
        foreach (var b in EnumerateEntities())
        {
            if (b == null) continue;
            if (b.Has(TraitType.Sentient)) movers.Add(b);
        }

        if (movers.Count == 0) return;

        foreach (var mover in movers.OrderBy(_ => rng.Next()))
        {
            if (mover == null) continue;

            var p = mover.anchor;
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

            if (rng.NextDouble() > 0.5) continue;

            bool moved = false;
            foreach (var d in dirs)
            {
                var np = p + d;
                if (!InBounds(np)) continue;

                int ni = PosToIdx(np);
                if (grid[ni] != null)
                {
                    continue;
                }

                if (TryMoveEntity(mover, d, allowPush: false))
                {
                    moved = true;
                    break;
                }
            }

            if (moved)
            {
                RenderAll();
            }
        }
    }

    void UpdateGhostMerges(float dt)
    {
        // Disabled: ghosts should not merge or consume items.
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
            if (aliveBoxes.Contains(box.id)) continue;
            aliveBoxes.Add(box.id);

            BoxEntity tile = null;
            int tileIdx = -1;

            // Check adjacency for any occupied cell
            for (int dy = 0; dy < box.size.y; dy++)
            for (int dx = 0; dx < box.size.x; dx++)
            {
                var p = new Vector2Int(box.anchor.x + dx, box.anchor.y + dy);
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
                if (tile != null) break;
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
                    if (tile.tileTrait == TraitType.Fire) PlaySfx("fired");
                    else if (tile.tileTrait == TraitType.Ice) PlaySfx("freezed");
                    RenderAll();
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
        if (CountTraitTiles() >= Mathf.FloorToInt(grid.Length * maxTraitFraction)) return;
        if (!HasEmptyCapacity(1)) return;
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

        var trait = RandomTraitTile();
        var t = BoxEntity.CreateTraitTile(trait);
        t.size = Vector2Int.one;
        PlaceEntity(t, IdxToPos(idx));
        if (trait == TraitType.Fire) PlaySfx("burning");
        else if (trait == TraitType.Ice) PlaySfx("freezed");
    }

    void TrySpawnTraitTile(TraitType trait)
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

        var t = BoxEntity.CreateTraitTile(trait);
        t.size = Vector2Int.one;
        PlaceEntity(t, IdxToPos(idx));
        if (trait == TraitType.Fire) PlaySfx("burning");
        else if (trait == TraitType.Ice) PlaySfx("freezed");
    }

    void TrySpawnGhost()
    {
        int ghostCount = grid.Count(b => IsGhost(b));
        if (ghostCount >= maxGhosts) return;
        if (!HasEmptyCapacity(1)) return;

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

        SpawnGhostAt(IdxToPos(idx));
    }

    void SpawnGhostAt(Vector2Int anchor)
    {
        if (!CanPlaceAt(anchor, Vector2Int.one)) return;
        var ghost = new BoxEntity(ItemSubType.Ghost);
        ghost.size = Vector2Int.one;
        PlaceEntity(ghost, anchor);
        PlaySfx("haunted");
    }

    // Optional: keep if you still want periodic status refresh or random events
    void WorldTick()
    {

        // Glitch event: rare swap of two items
        if (glitchInterval > 0f)
        {
            glitchTimer += tickRateSeconds;
            if (glitchTimer >= glitchInterval)
            {
                glitchTimer = 0f;
                if (rng.NextDouble() < glitchChance)
                {
                    TrySwapTwoItems();
                }
            }
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

    bool IsDeliveryZone(Vector2Int p) => false;
    bool IsGhost(BoxEntity b) => b != null && b.subType == ItemSubType.Ghost;
    bool IsTraitTile(BoxEntity b) => b != null && b.isTraitTile;
    bool IsPushable(BoxEntity b) => b != null;
    bool IsFrozen() => frozenTimer > 0f || frozenMoves > 0;
    bool FireImmune => fireImmuneTimer > 0f;
    bool IceImmune => iceImmuneTimer > 0f;

    Vector2Int GetSizeForSubType(ItemSubType st)
    {
        return st switch
        {
            ItemSubType.Knife => new Vector2Int(2, 1),
            ItemSubType.WaterBottle => new Vector2Int(1, 2),
            _ => Vector2Int.one
        };
    }

    int CountTraitTiles()
    {
        int count = 0;
        for (int i = 0; i < grid.Length; i++)
        {
            if (IsTraitTile(grid[i])) count++;
        }
        return count;
    }

    bool HasEmptyCapacity(int needed)
    {
        int empty = 0;
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] == null) empty++;
        }
        int minEmpty = Mathf.CeilToInt(grid.Length * minEmptyFraction);
        return (empty - needed) >= minEmpty;
    }

    void UpdateLocksAndRegen(float dt)
    {
        // Weight lock from fullness
        float filled = grid.Count(e => e != null);
        int traitCount = 0;
        foreach (var e in EnumerateEntities())
        {
            if (e == null || IsTraitTile(e) || IsGhost(e)) continue;
            traitCount += (e.traits != null) ? e.traits.Count : 0;
        }
        filled += traitCount;
        float total = grid.Length;
        float F = (total <= 0f) ? 0f : filled / total;
        float w = Smoothstep(weightStart, weightEnd, F);
        L_weight = weightMaxLock * w;

        // Heat/cold buildup from selected item
        var sel = GetSelectedEntity();
        bool holdingFire = sel != null && sel.Has(TraitType.Fire) && !FireImmune;
        bool holdingIce = sel != null && sel.Has(TraitType.Ice) && !IceImmune;

        heat = heat + (holdingFire ? heatBuildRate : -heatRecoverRate) * dt;
        cold = cold + (holdingIce ? coldBuildRate : -coldRecoverRate) * dt;
        if (heat < 0f) heat = 0f;
        if (cold < 0f) cold = 0f;

        L_heat = heat;
        L_cold = cold;
        L_hp = Mathf.Clamp(L_hp, 0f, L_hp_cap);

        if (heat >= heatOverload) status = "HEAT OVERLOAD";
        if (cold >= coldOverload) status = "COLD OVERLOAD";

        // Regen
        float L_total = Mathf.Clamp(L_hp + L_weight + L_heat + L_cold, 0f, E_max_base);
        float E_max_eff = Mathf.Max(0f, E_max_base - L_total);
        E = Mathf.Clamp(E, 0f, E_max_eff);

        if (E_max_eff <= 0f)
        {
            status = "CAPACITY COLLAPSE";
            return;
        }

        if (lastActionTimer >= regenDelay)
        {
            E = Mathf.Min(E_max_eff, E + regenRate * dt);
        }
    }

    bool TrySpendEnergy(float cost)
    {
        if (cost <= 0f) return true;
        float L_total = Mathf.Clamp(L_hp + L_weight + L_heat + L_cold, 0f, E_max_base);
        float E_max_eff = Mathf.Max(0f, E_max_base - L_total);
        E = Mathf.Clamp(E, 0f, E_max_eff);

        if (E_max_eff <= 0f)
        {
            status = "CAPACITY COLLAPSE";
            return false;
        }
        if (E < cost)
        {
            status = "EXHAUSTED";
            return false;
        }
        E -= cost;
        lastActionTimer = 0f;
        return true;
    }

    float GetPushCost(BoxEntity b)
    {
        if (b == null) return costPush;
        int area = b.size.x * b.size.y;
        return area >= 2 ? costPush2x1 : costPush;
    }

    void ConsumeFrozenAttempt()
    {
        status = "FROZEN";
        if (frozenMoves > 0) frozenMoves--;
    }

    bool IsTraitPassable(BoxEntity mover, BoxEntity tile)
    {
        if (mover == null || tile == null || !IsTraitTile(tile)) return false;
        if (IsGhost(mover)) return false;
        if (tile.tileTrait == TraitType.Fire && FireImmune) return true;
        if (tile.tileTrait == TraitType.Ice && IceImmune) return true;
        return false;
    }

    float Smoothstep(float a, float b, float x)
    {
        if (Mathf.Approximately(a, b)) return 0f;
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    bool CanPlaceAt(Vector2Int anchor, Vector2Int size)
    {
        for (int dy = 0; dy < size.y; dy++)
        for (int dx = 0; dx < size.x; dx++)
        {
            var p = new Vector2Int(anchor.x + dx, anchor.y + dy);
            if (!InBounds(p)) return false;
            if (grid[PosToIdx(p)] != null) return false;
        }
        return true;
    }

    void PlaceEntity(BoxEntity e, Vector2Int anchor)
    {
        e.anchor = anchor;
        for (int dy = 0; dy < e.size.y; dy++)
        for (int dx = 0; dx < e.size.x; dx++)
        {
            var p = new Vector2Int(anchor.x + dx, anchor.y + dy);
            grid[PosToIdx(p)] = e;
        }
    }

    void RemoveEntity(BoxEntity e)
    {
        for (int dy = 0; dy < e.size.y; dy++)
        for (int dx = 0; dx < e.size.x; dx++)
        {
            var p = new Vector2Int(e.anchor.x + dx, e.anchor.y + dy);
            if (InBounds(p) && grid[PosToIdx(p)] == e)
            {
                grid[PosToIdx(p)] = null;
            }
        }
    }

    bool IsAtTopBoundary(BoxEntity e)
    {
        return e.anchor.y == 0;
    }

    bool AreaInBounds(Vector2Int anchor, Vector2Int size)
{
    for (int dy = 0; dy < size.y; dy++)
    for (int dx = 0; dx < size.x; dx++)
    {
        var p = new Vector2Int(anchor.x + dx, anchor.y + dy);
        if (!InBounds(p)) return false;
    }
    return true;
}

List<BoxEntity> GetUniqueBlockersInArea(Vector2Int anchor, Vector2Int size, BoxEntity ignore)
{
    var set = new HashSet<string>();
    var list = new List<BoxEntity>();

    for (int dy = 0; dy < size.y; dy++)
    for (int dx = 0; dx < size.x; dx++)
    {
        var p = new Vector2Int(anchor.x + dx, anchor.y + dy);
        var b = grid[PosToIdx(p)];
        if (b == null || b == ignore) continue;

        if (set.Add(b.id)) list.Add(b);
    }

    return list;
}

int Project(Vector2Int p, Vector2Int dir)
{
    return p.x * dir.x + p.y * dir.y;
}


   bool TryMoveEntity(BoxEntity root, Vector2Int dir, bool allowPush)
{
    if (root == null) return false;
    if (IsTraitTile(root)) return false;

    // Build push chain without changing the grid
    var queue = new Queue<BoxEntity>();
    var seen = new HashSet<string>();
    var chain = new List<BoxEntity>();

    queue.Enqueue(root);
    seen.Add(root.id);

    while (queue.Count > 0)
    {
        var e = queue.Dequeue();

        var newAnchor = e.anchor + dir;

        // must fit inside bounds
        if (!AreaInBounds(newAnchor, e.size))
            return false;

        // find blockers in destination area (dedup by id)
        var blockers = GetUniqueBlockersInArea(newAnchor, e.size, e);

        foreach (var b in blockers)
        {
            // cannot push unless allowed
            if (!allowPush) return false;

            // do not push trait tiles
            if (IsTraitTile(b))
            {
                if (IsTraitPassable(root, b))
                {
                    // consume tile so we can occupy the cell
                    RemoveEntity(b);
                    continue;
                }
                return false;
            }

            // optional: if you never want ghosts to push other ghosts
            // if (IsGhost(e) && IsGhost(b)) return false;

            if (seen.Add(b.id))
                queue.Enqueue(b);
        }

        chain.Add(e);
    }

    // Commit: move farthest-first to avoid stepping on each other
    chain = chain
        .Distinct()
        .OrderByDescending(e => Project(e.anchor, dir))
        .ToList();

    // 1) Clear all cells for all entities in the chain
    foreach (var e in chain) RemoveEntity(e);

    // 2) Place them at new anchors
    foreach (var e in chain)
    {
        var newAnchor = e.anchor + dir;
        PlaceEntity(e, newAnchor);
    }

    return true;
}


    IEnumerable<BoxEntity> EnumerateEntities()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < grid.Length; i++)
        {
            var e = grid[i];
            if (e == null) continue;
            if (seen.Add(e.id)) yield return e;
        }
    }

    string TraitsToStr(IEnumerable<TraitType> traits)
    {
        if (traits == null) return "none";
        var list = traits.Select(t => t.ToString()).ToList();
        return list.Count == 0 ? "none" : string.Join("+", list);
    }

    void UpdateAnchorCache()
    {
        var alive = new HashSet<string>();
        foreach (var e in EnumerateEntities())
        {
            if (e == null) continue;
            lastAnchor[e.id] = e.anchor;
            alive.Add(e.id);
        }

        if (lastAnchor.Count > 0)
        {
            var dead = new List<string>();
            foreach (var kv in lastAnchor)
            {
                if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            }
            foreach (var id in dead) lastAnchor.Remove(id);
        }
    }

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
        conveyor.Enqueue(NextFromBag());
        return sub;
    }

    ItemSubType NextFromBag()
    {
        if (itemBag.Count == 0) RefillItemBag();
        var item = itemBag[0];
        itemBag.RemoveAt(0);
        lastQueuedItem = item;
        return item;
    }

    void RefillItemBag()
    {
        itemBag.Clear();
        itemBag.Add(ItemSubType.Bread);
        itemBag.Add(ItemSubType.Knife);
        itemBag.Add(ItemSubType.WaterBottle);
        // Shuffle
        for (int i = 0; i < itemBag.Count; i++)
        {
            int j = rng.Next(i, itemBag.Count);
            (itemBag[i], itemBag[j]) = (itemBag[j], itemBag[i]);
        }
        // Avoid immediate repeat across bag boundaries.
        if (lastQueuedItem.HasValue && itemBag.Count > 1 && itemBag[0] == lastQueuedItem.Value)
        {
            int swapIndex = rng.Next(1, itemBag.Count);
            (itemBag[0], itemBag[swapIndex]) = (itemBag[swapIndex], itemBag[0]);
        }
    }

    List<TraitType> RandomRequiredTraits()
    {
        // Balancing like the reference:
        // 80% -> 1 trait, 20% -> 2 traits (clamped to available).
        var candidates = new[] { TraitType.Fire, TraitType.Ice };
        int count = (rng.NextDouble() > 0.8) ? 2 : 1;
        count = Mathf.Clamp(count, 0, candidates.Length);

        var set = new HashSet<TraitType>();
        while (set.Count < count)
        {
            set.Add(candidates[rng.Next(0, candidates.Length)]);
        }

        return set.ToList();
    }

    void TriggerChaos()
    {
        chaosUnlocked = true;
        chaosTimer = 0f;
        ScreenShake.Shake(0.35f, 14f, 24);
        if (chaosTint != null)
        {
            var c = chaosTint.color;
            chaosTint.DOKill();
            chaosTint.color = new Color(c.r, c.g, c.b, 0f);
            chaosTint.DOColor(new Color(c.r, c.g, c.b, chaosTintAlpha), chaosTintFlashSeconds * 0.5f)
                .SetLoops(2, LoopType.Yoyo);
        }
        TrySpawnTraitTile(TraitType.Fire);
    }

    TraitType RandomTraitTile()
    {
        var candidates = new[] { TraitType.Fire, TraitType.Ice };
        return candidates[rng.Next(0, candidates.Length)];
    }

    void OnCellClicked(int idx)
    {
        var e = grid[idx];
        selector = (e != null) ? e.anchor : IdxToPos(idx);
        RenderAll();
    }

    void SpawnBlockedFeedback(Vector2Int size)
    {
        if (size.x * size.y <= 1) return;

        ScreenShake.Shake(0.12f, 6f, 18);

        if (statusText != null)
        {
            var baseColor = statusText.color;
            statusText.DOKill();
            statusText.color = baseColor;
            statusText.DOColor(Color.red, 0.06f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => statusText.color = baseColor);
        }
    }

    void UseSelected()
    {
        var e = GetSelectedEntity();
        if (e == null) return;

        if (e.activeCooldown > 0f)
        {
            status = "ON COOLDOWN";
            return;
        }

        switch (e.kind)
        {
            case ItemKind.Sword:
                UseSword(e);
                break;
            case ItemKind.Potion:
                UsePotion(e);
                break;
            case ItemKind.Bread:
                UseBread(e);
                break;
            default:
                status = "NO USE";
                break;
        }
    }

    BoxEntity GetSelectedEntity()
    {
        int idx = PosToIdx(selector);
        var e = grid[idx];
        if (e == null) return null;
        var a = e.anchor;
        return grid[PosToIdx(a)];
    }

    void UseSword(BoxEntity sword)
    {
        var center = sword.anchor;
        SpawnSlashVfx(center);
        StartCoroutine(DoSwordEffect(sword));
    }

    System.Collections.IEnumerator DoSwordEffect(BoxEntity sword)
    {
        float delay = Mathf.Max(0f, slashSpinSeconds * 0.5f);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (sword == null) yield break;
        var center = sword.anchor;
        var targets = GetEntitiesIn3x3(center);
        bool hasFire = sword.Has(TraitType.Fire);
        bool hasIce = sword.Has(TraitType.Ice);
        bool destroyAll = hasFire && hasIce;

        foreach (var t in targets)
        {
            if (t == null) continue;
            if (t == sword) continue;

            if (destroyAll)
            {
                RemoveEntity(t);
                continue;
            }

            if (IsGhost(t))
            {
                RemoveEntity(t);
                continue;
            }

            if (IsTraitTile(t))
            {
                if (hasFire && t.tileTrait == TraitType.Fire) RemoveEntity(t);
                else if (hasIce && t.tileTrait == TraitType.Ice) RemoveEntity(t);
            }
        }

        RemoveEntity(sword);
        status = "SLASH";
        RenderAll();
    }

    void SpawnSlashVfx(Vector2Int center)
    {
        GameObject prefab = slashPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Slash");
            if (prefab == null) prefab = Resources.Load<GameObject>("Art/Slash");
        }
        if (prefab == null || cells == null) return;

        int idx = PosToIdx(center);
        if (idx < 0 || idx >= cells.Length) return;
        var cell = cells[idx];
        if (cell == null) return;

        var vfx = Instantiate(prefab, cell.transform);
        var vfxRt = vfx.GetComponent<RectTransform>();
        var cellRt = cell.GetComponent<RectTransform>();
        if (vfxRt != null && cellRt != null)
        {
            vfxRt.anchorMin = new Vector2(0.5f, 0.5f);
            vfxRt.anchorMax = new Vector2(0.5f, 0.5f);
            vfxRt.pivot = new Vector2(0.5f, 0.5f);
            vfxRt.anchoredPosition = Vector2.zero;
            vfxRt.localScale = Vector3.one;

            var grid = gridParent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                var size = grid.cellSize;
                var spacing = grid.spacing;
                float w = size.x * 3f + spacing.x * 2f;
                float h = size.y * 3f + spacing.y * 2f;
                vfxRt.sizeDelta = new Vector2(w, h);
            }
        }

        var vfxCanvas = vfx.GetComponent<Canvas>();
        if (vfxCanvas == null) vfxCanvas = vfx.AddComponent<Canvas>();
        vfxCanvas.overrideSorting = true;
        vfxCanvas.sortingOrder = 1000;

        vfx.transform.DOKill();
        vfx.transform.localRotation = Quaternion.identity;
        vfx.transform.DORotate(new Vector3(0f, 0f, slashSpinDegrees), slashSpinSeconds, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        Destroy(vfx, slashLifeSeconds);
    }

    void UsePotion(BoxEntity potion)
    {
        bool hasFire = potion.Has(TraitType.Fire);
        bool hasIce = potion.Has(TraitType.Ice);
        if (!hasFire && !hasIce)
        {
            ExtinguishFireInArea(potion.anchor, radius: 1);
            status = "SPLASH";
            RemoveEntity(potion);
            RenderAll();
            return;
        }

        float dur = 6f;
        if (hasFire) fireImmuneTimer = Mathf.Max(fireImmuneTimer, dur);
        if (hasIce) iceImmuneTimer = Mathf.Max(iceImmuneTimer, dur);

        RemoveEntity(potion);
        status = "IMMUNITY UP";
        RenderAll();
    }

    void UseBread(BoxEntity bread)
    {
        // Bread always heals regardless of traits.
        L_hp = Mathf.Max(0f, L_hp - 15f);
        E = Mathf.Min(E_max_base, E + 10f);
        status = "HEALED";

        RemoveEntity(bread);
        RenderAll();
    }

    List<BoxEntity> GetEntitiesIn3x3(Vector2Int center)
    {
        var set = new HashSet<string>();
        var list = new List<BoxEntity>();
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var p = new Vector2Int(center.x + dx, center.y + dy);
            if (!InBounds(p)) continue;
            var b = grid[PosToIdx(p)];
            if (b == null) continue;
            if (set.Add(b.id)) list.Add(b);
        }
        return list;
    }

    void ApplyFreezeMoves(int moves)
    {
        frozenMoves = Mathf.Max(frozenMoves, moves);
    }

    void AddHeatBuildup(float amount)
    {
        heat = Mathf.Clamp(heat + amount, 0f, heatMax);
    }

    void CleaveTraits(Vector2Int center)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var p = new Vector2Int(center.x + dx, center.y + dy);
            if (!InBounds(p)) continue;
            var e = grid[PosToIdx(p)];
            if (e != null && IsTraitTile(e))
            {
                grid[PosToIdx(p)] = null;
            }
        }
    }

    void TrySwapTwoItems()
    {
        var items = new List<BoxEntity>();
        foreach (var e in EnumerateEntities())
        {
            if (e == null || IsTraitTile(e) || IsGhost(e)) continue;
            items.Add(e);
        }
        if (items.Count < 2) return;
        var a = items[rng.Next(0, items.Count)];
        var b = items[rng.Next(0, items.Count)];
        if (a == b) return;

        var aAnchor = a.anchor;
        var bAnchor = b.anchor;

        RemoveEntity(a);
        RemoveEntity(b);

        if (CanPlaceAt(bAnchor, a.size) && CanPlaceAt(aAnchor, b.size))
        {
            PlaceEntity(a, bAnchor);
            PlaceEntity(b, aAnchor);
            status = "GLITCH SWAP!";
            RenderAll();
        }
        else
        {
            // put them back if swap fails
            PlaceEntity(a, aAnchor);
            PlaceEntity(b, bAnchor);
        }
    }

    // ----------------------------
    // UI RENDER
    // ----------------------------
    void RenderAll()
    {
        if (physicsMode) return;
        BuildFireAuraCache();
        BuildIceAuraCache();
        for (int i = 0; i < grid.Length; i++)
        {
            var p = IdxToPos(i);
            bool selected = (p == selector);
            var e = grid[i];
            var from = (e != null && lastAnchor.TryGetValue(e.id, out var prev)) ? prev : p;
            bool inFireAura = fireAuraCache != null && fireAuraCache[i];
            bool inIceAura = iceAuraCache != null && iceAuraCache[i];
            cells[i].SetCell(grid[i], selected, false, inFireAura, inIceAura, p, from);
        }
        RenderHud();
        UpdateAnchorCache();
    }

    void RenderHud()
    {
        int filled = grid.Count(e => e != null);
        int pct = Mathf.RoundToInt(100f * filled / grid.Length);

        if (fullnessText != null) fullnessText.text = $"GRID: {pct}%";
        if (statusText != null)
        {
            statusText.text = status.ToUpper();
            if (lastStatus != status)
            {
                var rt = statusText.rectTransform;
                rt.DOKill();
                rt.localScale = Vector3.one;
                rt.DOPunchScale(Vector3.one * 0.12f, 0.18f, 10, 0.7f);
            }
        }
        if (reputationText != null)
        {
            reputationText.text = $"REP: {reputation}";
            if (lastRep != reputation)
            {
                var rt = reputationText.rectTransform;
                rt.DOKill();
                rt.localScale = Vector3.one;
                rt.DOPunchScale(Vector3.one * 0.12f, 0.18f, 10, 0.7f);
            }
        }

        if (currentOrder != null)
        {
            if (orderTimerText != null) orderTimerText.text = $"TIME: {Mathf.CeilToInt(currentOrder.timeLeft)}";
            wantedView?.SetWanted(currentOrder.subType, currentOrder.requiredTraits, currentOrder.timeLeft, orderLifetime, currentCustomerName, currentCustomerFlavor);
        }

        bufferView?.Set(conveyor.ToArray());

        lastStatus = status;
        lastRep = reputation;
        UpdateEnergyUI();
    }

    void DisableCellsForEntity(BoxEntity e)
    {
        if (cells == null || e == null) return;
        for (int dy = 0; dy < e.size.y; dy++)
        for (int dx = 0; dx < e.size.x; dx++)
        {
            var p = new Vector2Int(e.anchor.x + dx, e.anchor.y + dy);
            int idx = PosToIdx(p);
            if (idx < 0 || idx >= cells.Length) continue;
            var cv = cells[idx];
            if (cv != null) cv.gameObject.SetActive(false);
        }
    }

    public BoxEntity GetEntityAtPos(Vector2Int p)
    {
        if (!InBounds(p)) return null;
        return grid[PosToIdx(p)];
    }

    public bool IsPhysicsMode => physicsMode;

    public bool IsInAutoSubmitZoneWorld(Vector3 worldPos)
    {
        if (bagBoundary == null) return false;
        var corners = new Vector3[4];
        bagBoundary.GetWorldCorners(corners);
        float top = corners[1].y;
        float bottom = corners[0].y;
        float band = (top - bottom) * Mathf.Clamp01(autoSubmitBand);
        return worldPos.y >= (top - band);
    }

    void SetPhysicsMode(bool on)
    {
        if (on) EnsureUIForDragging();
        var gridLayout = gridParent != null ? gridParent.GetComponent<GridLayoutGroup>() : null;
        if (gridLayout != null) gridLayout.enabled = !on;
        if (!on)
        {
            BuildGridUI();
            RenderAll();
            return;
        }

        int dragCount = 0;
        var used = new HashSet<string>();
        for (int i = 0; i < grid.Length; i++)
        {
            var cv = cells[i];
            if (cv == null) continue;

            var e = grid[i];
            bool isItem = e != null && !IsTraitTile(e) && !IsGhost(e) && e.anchor == IdxToPos(i);
            if (e != null && used.Contains(e.id))
            {
                // Always keep both cells of 2x1 items visible
                if (on)
                {
                    cv.gameObject.SetActive(true);
                    SetupDragHandle(cv, IdxToPos(i));
                    dragCount++;
                }
                continue;
            }

            if (on)
            {
                if (!isItem)
                {
                    cv.gameObject.SetActive(false);
                    continue;
                }

                cv.gameObject.SetActive(true);
                if (e != null) used.Add(e.id);
                var go = cv.gameObject;
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null) rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = physicsGravity;
                rb.mass = physicsMass;
                rb.simulated = true;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;

                var col = go.GetComponent<BoxCollider2D>();
                if (col == null) col = go.AddComponent<BoxCollider2D>();

                float angle = UnityEngine.Random.Range(-physicsScatterAngle, physicsScatterAngle);
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.down;
                rb.AddForce(dir * physicsImpulse, ForceMode2D.Impulse);
                rb.AddTorque(UnityEngine.Random.Range(-physicsImpulse, physicsImpulse) * 0.5f, ForceMode2D.Impulse);

                // Attach adjacent cells of multi-cell items to the anchor cell
                if (e != null && e.size.x * e.size.y > 1)
                {
                    for (int dy = 0; dy < e.size.y; dy++)
                    for (int dx = 0; dx < e.size.x; dx++)
                    {
                        var p = new Vector2Int(e.anchor.x + dx, e.anchor.y + dy);
                        int ci = PosToIdx(p);
                        if (ci < 0 || ci >= cells.Length) continue;
                        if (p == e.anchor) continue;
                        var child = cells[ci];
                        if (child == null) continue;
                        child.gameObject.SetActive(true);
                        child.transform.SetParent(go.transform, true);

                        var childRb = child.GetComponent<Rigidbody2D>();
                        if (childRb != null) Destroy(childRb);
                        var childCol = child.GetComponent<BoxCollider2D>();
                        if (childCol != null) Destroy(childCol);
                    }
                }

                UpdateColliderForItem(go, e);
                SetupDragHandle(cv, IdxToPos(i));
                dragCount++;
            }
            else
            {
                cv.gameObject.SetActive(true);
                var go = cv.gameObject;
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) Destroy(rb);
                var col = go.GetComponent<BoxCollider2D>();
                if (col != null) Destroy(col);
                var dh = go.GetComponent<PhysicsDragHandle>();
                if (dh != null) Destroy(dh);
            }
        }

        if (!on) RenderAll();
        if (on)
        {
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            RefreshPhysicsColliders();
            RefreshBagBoundary();
            Debug.Log($"[Drag] Physics mode ON. Drag handles attached: {dragCount}");
            physicsModeTimer = Mathf.Max(0.1f, physicsModeDuration);
        }
    }

    void SetupDragHandle(CellView cv, Vector2Int pos)
    {
        if (cv == null) return;
        var dh = cv.GetComponent<PhysicsDragHandle>();
        if (dh == null) dh = cv.gameObject.AddComponent<PhysicsDragHandle>();
        dh.Init(this, pos, bagBoundary, autoSubmitBand);
    }

    void EnsureUIForDragging()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            var gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log($"[Drag] Canvas found: {canvas.name} (renderMode={canvas.renderMode})");
        }

        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
        var sim = es.GetComponent<StandaloneInputModule>();
        if (sim != null) Destroy(sim);
        Debug.Log("[Drag] Using InputSystemUIInputModule");
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
        Debug.Log("[Drag] Using StandaloneInputModule");
#endif
    }

    void RefreshPhysicsColliders()
    {
        if (cells == null) return;
        for (int i = 0; i < grid.Length; i++)
        {
            var cv = cells[i];
            if (cv == null) continue;
            var e = grid[i];
            if (e == null || IsTraitTile(e) || IsGhost(e)) continue;
            if (e.anchor != IdxToPos(i)) continue;
            UpdateColliderForItem(cv.gameObject, e);
        }
    }

    void UpdateColliderForItem(GameObject go, BoxEntity e)
    {
        if (go == null || e == null) return;
        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();

        var bounds = new Bounds();
        bool hasBounds = false;

        for (int dy = 0; dy < e.size.y; dy++)
        for (int dx = 0; dx < e.size.x; dx++)
        {
            var p = new Vector2Int(e.anchor.x + dx, e.anchor.y + dy);
            int ci = PosToIdx(p);
            if (ci < 0 || ci >= cells.Length) continue;
            var cell = cells[ci];
            if (cell == null) continue;
            var rt = cell.GetComponent<RectTransform>();
            if (rt == null) continue;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            for (int k = 0; k < 4; k++)
            {
                var local = go.transform.InverseTransformPoint(corners[k]);
                if (!hasBounds)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        if (hasBounds)
        {
            col.offset = bounds.center;
            col.size = bounds.size;
        }
    }

    void RefreshBagBoundary()
    {
        if (bagBoundary == null) return;

        var rb = bagBoundary.GetComponent<Rigidbody2D>();
        if (rb == null) rb = bagBoundary.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var rect = bagBoundary.rect;
        var xMin = rect.xMin;
        var xMax = rect.xMax;
        var yMin = rect.yMin;
        var yMax = rect.yMax;

        SetEdge(bagBoundary, "BagEdgeTop", new Vector2(xMin, yMax), new Vector2(xMax, yMax));
        SetEdge(bagBoundary, "BagEdgeBottom", new Vector2(xMin, yMin), new Vector2(xMax, yMin));
        SetEdge(bagBoundary, "BagEdgeLeft", new Vector2(xMin, yMin), new Vector2(xMin, yMax));
        SetEdge(bagBoundary, "BagEdgeRight", new Vector2(xMax, yMin), new Vector2(xMax, yMax));
    }

    void SetEdge(RectTransform parent, string name, Vector2 a, Vector2 b)
    {
        var t = parent.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = t.gameObject;
        }

        var edge = go.GetComponent<EdgeCollider2D>();
        if (edge == null) edge = go.AddComponent<EdgeCollider2D>();
        edge.points = new[] { a, b };
    }

    void UpdateEnergyUI()
    {
        float L_total = Mathf.Clamp(L_hp + L_weight + L_heat + L_cold, 0f, E_max_base);
        float green = Mathf.Max(0f, E_max_base - L_total);

        SetSegmentSize(energyFill, green);
        SetSegmentSize(heatLockFill, L_heat);
        SetSegmentSize(coldLockFill, L_cold);
        SetSegmentSize(hpLockFill, L_hp);
        SetSegmentSize(weightLockFill, L_weight);

        if (L_total >= E_max_base)
        {
            status = "CAPACITY COLLAPSE";
            GameOver();
        }
    }

    void SetSegmentSize(Image img, float value)
    {
        if (img == null) return;
        float pct = (E_max_base <= 0f) ? 0f : Mathf.Clamp01(value / E_max_base);
        var le = img.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredWidth = peakBarVertical ? peakBarWidth : peakBarWidth * pct;
            le.preferredHeight = peakBarVertical ? peakBarHeight * pct : peakBarHeight;
            le.minWidth = 0f;
        }
        var rt = img.rectTransform;
        if (rt != null)
        {
            rt.sizeDelta = peakBarVertical
                ? new Vector2(peakBarWidth, peakBarHeight * pct)
                : new Vector2(peakBarWidth * pct, peakBarHeight);
        }
    }

    void BuildPeakBarIfMissing()
    {
        if (energyFill != null && hpLockFill != null && weightLockFill != null && heatLockFill != null && coldLockFill != null)
            return;

        if (peakBarRoot != null)
        {
            AssignPeakBarRefs(peakBarRoot);
            return;
        }

        if (peakBarPrefab != null)
        {
            var parent = peakBarParent != null ? peakBarParent : FindObjectOfType<Canvas>()?.transform as RectTransform;
            if (parent != null)
            {
                var inst = Instantiate(peakBarPrefab, parent);
                AssignPeakBarRefs(inst.transform);
                return;
            }
        }

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var root = new GameObject("PeakBar");
        root.transform.SetParent(canvas.transform, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(peakBarWidth, peakBarHeight);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

        if (peakBarVertical)
        {
            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
        else
        {
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // Order: green, hot, freeze, hp, weight
        energyFill = CreateFill(root.transform, "EnergyFill", new Color(0.2f, 0.85f, 0.35f, 1f), 0);
        heatLockFill = CreateFill(root.transform, "HeatLock", new Color(0.95f, 0.25f, 0.25f, 1f), 1);
        coldLockFill = CreateFill(root.transform, "ColdLock", new Color(0.2f, 0.6f, 1f, 1f), 2);
        hpLockFill = CreateFill(root.transform, "HpLock", new Color(1f, 0.6f, 0.2f, 1f), 3);
        weightLockFill = CreateFill(root.transform, "WeightLock", new Color(0.5f, 0.5f, 0.5f, 1f), 4);
    }

    void AssignPeakBarRefs(Transform root)
    {
        if (root == null) return;
        energyFill = FindImage(root, "Energy");
        heatLockFill = FindImage(root, "Hot");
        coldLockFill = FindImage(root, "Ice");
        hpLockFill = FindImage(root, "HP");
        weightLockFill = FindImage(root, "Weight");
    }

    Image FindImage(Transform root, string name)
    {
        if (root == null) return null;
        var t = root.Find(name);
        if (t == null) return null;
        return t.GetComponent<Image>();
    }

    Image CreateFill(Transform parent, string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(peakBarWidth, peakBarHeight);

        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = peakBarWidth;
        le.preferredHeight = peakBarHeight;

        var canvas = img.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100 + order;
        return img;
    }

    void PlaySfx(string id)
    {
        ServiceHub.Get<AudioManager>()?.PlaySfx(id);
    }

    void PlayBgm(string id)
    {
        ServiceHub.Get<AudioManager>()?.PlayBgm(id);
    }

    [Serializable]
    public class CustomerLevel
    {
        public string id;
        public string displayName;
        public string flavorLine;
        public List<OrderSpec> orders = new List<OrderSpec>();
        public bool enableChaosSpawns = false;
        public bool enableFireSpawns = false;
        public float fireSpawnInterval = 8f;
        public int fireBurstCount = 1;
        public bool enableFireSpread = false;
        public float fireSpreadInterval = 4f;
        [Range(0f, 1f)] public float fireSpreadChance = 0.4f;
        public bool enableIceSpawns = false;
        public float iceSpawnInterval = 8f;
        public int iceBurstCount = 1;
        public bool spawnInitialIceBurst = false;
        public bool enableIceAura = false;
        public float iceToItemSeconds = 2f;
        public bool enableGhostSpawns = false;
        public float ghostSpawnInterval = 6f;
        public int ghostBurstCount = 0;
    }

    [Serializable]
    public class OrderSpec
    {
        public ItemSubType subType;
        public List<TraitType> requiredTraits = new List<TraitType>();

        public static OrderSpec Of(ItemSubType subType, params TraitType[] traits)
        {
            var spec = new OrderSpec { subType = subType };
            if (traits != null && traits.Length > 0) spec.requiredTraits = new List<TraitType>(traits);
            return spec;
        }
    }
}
