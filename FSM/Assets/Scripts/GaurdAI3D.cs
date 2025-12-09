using UnityEngine;
using UnityEngine.AI;

/*
===========================================
GUARD AI STATE MACHINE - DOCUMENTATION
===========================================

STATES:
-------
PATROL      - Walking between patrol points. Calm. Looking for trouble.
SUSPICIOUS  - Heard/smelled something. Stops and looks around for 2-3 seconds.
INVESTIGATE - Going to check out a specific location (sound, smell, or alert).
SEARCH      - Actively searching an area (circular pattern). High alertness.
CHASE       - Pursuing the player. Running at full speed.
ATTACK      - In combat with player. Dealing damage.

SUSPICION LEVEL (0 to 3):
-------------------------
0.0       - Calm, nothing wrong
0.5       - Slightly alerted (heard distant sound)
1.0       - Suspicious (heard close sound, weak smell)
1.5       - Concerned (found evidence, strong smell)  
2.0       - Alert (received alert from other guard)
3.0       - THREAT CONFIRMED (saw player or received critical alert)

ALERT LEVELS (sent to other guards):
------------------------------------
LOW      - "I heard something" -> Others become Suspicious
MEDIUM   - "I found something" -> Others Investigate
HIGH     - "I SEE THE INTRUDER" -> Others Search the area
CRITICAL - "I'M ENGAGING!" -> Others come to Assist immediately

STATE TRANSITIONS:
------------------
PATROL:
  -> SUSPICIOUS: heard quiet sound, weak smell
  -> INVESTIGATE: heard loud sound, strong smell, received LOW/MEDIUM alert
  -> SEARCH: received HIGH alert
  -> CHASE: saw player, received CRITICAL alert
  
SUSPICIOUS:
  -> PATROL: nothing found after timeout
  -> INVESTIGATE: suspicion increased
  -> CHASE: saw player

INVESTIGATE:
  -> SUSPICIOUS: arrived, found nothing
  -> SEARCH: arrived, found evidence (bait, strong smell)
  -> CHASE: saw player

SEARCH:
  -> PATROL: search timeout, nothing found
  -> CHASE: saw player

CHASE:
  -> ATTACK: close enough to player
  -> SEARCH: lost sight of player

ATTACK:
  -> CHASE: player moved out of range
  -> SEARCH: player escaped completely

===========================================
*/

public enum GuardState
{
    Patrol,
    Suspicious,
    Investigate,
    Search,
    Chase,
    Attack
}

[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI3D : MonoBehaviour
{
    [Header("=== VISION ===")]
    [Tooltip("Where the guard's eyes are (for raycast origin)")]
    public Transform eyes;
    [Tooltip("How far the guard can see")]
    public float sightRange = 15f;
    [Tooltip("Field of view angle (degrees)")]
    public float sightAngle = 110f;
    [Tooltip("Layers that block vision")]
    public LayerMask visionBlockers;

    [Header("=== HEARING ===")]
    [Tooltip("Multiplier for sound reaction (higher = more sensitive)")]
    public float hearingSensitivity = 1f;

    [Header("=== SMELL ===")]
    [Tooltip("How far guard can smell scent nodes")]
    public float smellRange = 8f;
    [Tooltip("How often to check for smells (seconds)")]
    public float smellCheckInterval = 0.5f;

    [Header("=== COMBAT ===")]
    [Tooltip("Distance to attack player")]
    public float attackRange = 2f;
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1.5f;
    [Tooltip("Damage per attack")]
    public float attackDamage = 10f;

    [Header("=== MOVEMENT SPEEDS ===")]
    [Tooltip("Speed when patrolling")]
    public float patrolSpeed = 2f;
    [Tooltip("Speed when investigating")]
    public float investigateSpeed = 3.5f;
    [Tooltip("Speed when chasing")]
    public float chaseSpeed = 5f;

    [Header("=== PATROL ===")]
    public Transform[] patrolPoints;
    [Tooltip("Time to wait at each patrol point")]
    public float patrolWaitTime = 2f;

    [Header("=== TIMINGS ===")]
    [Tooltip("How long to stay suspicious before returning to patrol")]
    public float suspiciousTimeout = 3f;
    [Tooltip("How long to search an area")]
    public float searchDuration = 12f;
    [Tooltip("Radius to search around a point")]
    public float searchRadius = 6f;

    [Header("=== DEBUG ===")]
    public bool showDebugLogs = true;
    public bool showVisionCone = true;

    // Components
    NavMeshAgent agent;
    Transform player;

    // Current state
    GuardState currentState = GuardState.Patrol;
    float suspicionLevel = 0f;
    GuardStateBillboard3D stateBillboard;
    // Timers
    float stateTimer = 0f;
    float attackTimer = 0f;
    float smellTimer = 0f;
    float patrolWaitTimer = 0f;

    // Navigation targets
    Vector3 investigatePosition;
    Vector3 lastKnownPlayerPos;
    int patrolIndex = 0;
    int searchPointIndex = 0;
    Vector3 searchCenter;

    // Flags
    bool subscribedToEvents = false;
    bool foundEvidence = false;

    #region Unity Lifecycle

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        stateBillboard = GetComponentInChildren<GuardStateBillboard3D>();

        // Subscribe to events
        SubscribeToEvents();

        // Start patrolling
        EnterState(GuardState.Patrol);

        Log("Initialized and patrolling");
    }

    void OnEnable()
    {
        if (!subscribedToEvents)
        {
            SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        // Update timers
        stateTimer += Time.deltaTime;
        attackTimer -= Time.deltaTime;
        smellTimer += Time.deltaTime;

        // ALWAYS check for player visibility first (highest priority)
        if (CanSeePlayer())
        {
            OnSawPlayer();
        }

        // Check smell periodically (except during combat)
        if (currentState != GuardState.Chase && currentState != GuardState.Attack)
        {
            if (smellTimer >= smellCheckInterval)
            {
                smellTimer = 0f;
                CheckForSmells();
            }
        }

        // Decay suspicion when patrolling
        if (currentState == GuardState.Patrol)
        {
            suspicionLevel = Mathf.Max(0, suspicionLevel - Time.deltaTime * 0.2f);
        }

        // State machine
        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            case GuardState.Suspicious:
                UpdateSuspicious();
                break;
            case GuardState.Investigate:
                UpdateInvestigate();
                break;
            case GuardState.Search:
                UpdateSearch();
                break;
            case GuardState.Chase:
                UpdateChase();
                break;
            case GuardState.Attack:
                UpdateAttack();
                break;
        }
    }

    #endregion

    #region State Machine

    void EnterState(GuardState newState)
    {
        if (newState == currentState) return;

        GuardState oldState = currentState;
        currentState = newState;
        stateTimer = 0f;

        Log($"State: {oldState} -> {newState} (suspicion: {suspicionLevel:F1})");
        if (stateBillboard != null) stateBillboard.SetText(currentState.ToString());

        // State entry logic
        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                foundEvidence = false;
                GoToNextPatrolPoint();
                break;

            case GuardState.Suspicious:
                agent.speed = 0f;
                agent.isStopped = true;
                // Just stand and look around
                break;

            case GuardState.Investigate:
                agent.speed = investigateSpeed;
                agent.isStopped = false;
                agent.SetDestination(investigatePosition);
                break;

            case GuardState.Search:
                agent.speed = investigateSpeed;
                agent.isStopped = false;
                searchCenter = investigatePosition;
                searchPointIndex = 0;
                GoToNextSearchPoint();
                // Alert others that we're searching (HIGH alert)
                RaiseAlert(AlertLevel.High);
                break;

            case GuardState.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                suspicionLevel = 3f; // Confirmed threat
                // Alert others that we spotted the player
                RaiseAlert(AlertLevel.High);
                break;

            case GuardState.Attack:
                agent.isStopped = true;
                // Alert others that we're engaging
                RaiseAlert(AlertLevel.Critical);
                break;
        }
    }

    #endregion

    #region Patrol State

    void UpdatePatrol()
    {
        // Wait at patrol point
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                GoToNextPatrolPoint();
            }
        }
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    #endregion

    #region Suspicious State

    void UpdateSuspicious()
    {
        // Look around (rotate slowly)
        transform.Rotate(0, 30f * Time.deltaTime, 0);

        // Timeout - go back to patrol or investigate if suspicion high enough
        if (stateTimer >= suspiciousTimeout)
        {
            if (suspicionLevel >= 1f)
            {
                EnterState(GuardState.Investigate);
            }
            else
            {
                EnterState(GuardState.Patrol);
            }
        }
    }

    #endregion

    #region Investigate State

    void UpdateInvestigate()
    {
        // Check if arrived
        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            Log("Arrived at investigation point");

            // Check for evidence at this location
            CheckForEvidenceHere();

            if (foundEvidence || suspicionLevel >= 2f)
            {
                Log("Found evidence - searching area!");
                EnterState(GuardState.Search);
            }
            else
            {
                Log("Nothing here... staying alert");
                EnterState(GuardState.Suspicious);
            }
        }
    }

    void CheckForEvidenceHere()
    {
        // Check for bait
        Collider[] nearby = Physics.OverlapSphere(transform.position, 3f);
        foreach (var col in nearby)
        {
            if (col.GetComponent<Bait3D>() != null)
            {
                Log("FOUND BAIT!");
                foundEvidence = true;
                suspicionLevel = Mathf.Max(suspicionLevel, 2f);
                return;
            }
        }

        // Check for strong smell
        float smellStrength = ScentNode3D.GetTotalStrengthAt(transform.position, 3f);
        if (smellStrength > 0.5f)
        {
            Log($"Strong smell here: {smellStrength:F2}");
            foundEvidence = true;
            suspicionLevel = Mathf.Max(suspicionLevel, 1.5f);
        }
    }

    #endregion

    #region Search State

    void UpdateSearch()
    {
        // Check if arrived at search point
        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            GoToNextSearchPoint();
        }

        // Timeout
        if (stateTimer >= searchDuration)
        {
            Log("Search complete, returning to patrol");
            suspicionLevel = Mathf.Max(0, suspicionLevel - 1f);
            EnterState(GuardState.Patrol);
        }
    }

    void GoToNextSearchPoint()
    {
        // Search in a circle pattern (5 points)
        float angle = searchPointIndex * 72f * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * searchRadius,
            0f,
            Mathf.Sin(angle) * searchRadius
        );

        Vector3 searchPoint = searchCenter + offset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(searchPoint, out hit, searchRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        searchPointIndex = (searchPointIndex + 1) % 5;
    }

    #endregion

    #region Chase State

    void UpdateChase()
    {
        if (player == null)
        {
            EnterState(GuardState.Search);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Keep chasing
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // Update last known position
        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
        }

        // Close enough to attack?
        if (distance <= attackRange)
        {
            EnterState(GuardState.Attack);
        }
        // Lost sight and too far?
        else if (!CanSeePlayer() && distance > sightRange * 1.5f)
        {
            Log("Lost the player!");
            investigatePosition = lastKnownPlayerPos;
            EnterState(GuardState.Search);
        }
    }

    #endregion

    #region Attack State

    void UpdateAttack()
    {
        if (player == null)
        {
            EnterState(GuardState.Search);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Face the player
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toPlayer),
                Time.deltaTime * 10f
            );
        }

        // Player ran away?
        if (distance > attackRange * 1.5f)
        {
            EnterState(GuardState.Chase);
            return;
        }

        // Perform attack
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }

        // Continuously update other guards about player position
        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.UpdatePlayerPosition(player.position);
        }
    }

    void PerformAttack()
    {
        Log($"ATTACK! ({attackDamage} damage)");
        // TODO: Actually damage the player
        // player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    #endregion

    #region Detection

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eyePos = eyes != null ? eyes.position : transform.position + Vector3.up * 1.6f;
        Vector3 toPlayer = player.position - eyePos;
        float distance = toPlayer.magnitude;

        // Range check
        if (distance > sightRange) return false;

        // Angle check
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > sightAngle * 0.5f) return false;

        // Raycast check (can we actually see them?)
        if (Physics.Raycast(eyePos, toPlayer.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    void OnSawPlayer()
    {
        lastKnownPlayerPos = player.position;
        suspicionLevel = 3f;

        if (currentState != GuardState.Chase && currentState != GuardState.Attack)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Log($"SPOTTED PLAYER at {distance:F1}m!");

            if (distance <= attackRange)
            {
                EnterState(GuardState.Attack);
            }
            else
            {
                EnterState(GuardState.Chase);
            }
        }
    }

    void CheckForSmells()
    {
        if (ScentNode3D.AllNodes == null || ScentNode3D.AllNodes.Count == 0) return;

        ScentNode3D strongest = ScentNode3D.FindStrongestNear(transform.position, smellRange);
        if (strongest == null) return;

        float strength = strongest.GetStrengthAtPosition(transform.position);
        if (strength < 0.2f) return;

        Log($"Smells something (strength: {strength:F2})");

        investigatePosition = strongest.transform.position;
        suspicionLevel += strength * 0.3f;

        // React based on current state
        if (currentState == GuardState.Patrol)
        {
            if (strength > 0.8f)
            {
                EnterState(GuardState.Investigate);
            }
            else
            {
                EnterState(GuardState.Suspicious);
            }
        }
    }

    #endregion

    #region Event Handlers

    void SubscribeToEvents()
    {
        if (subscribedToEvents) return;

        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundEmitted += OnHeardSound;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised += OnAlertReceived;
            AlertBus3D.Instance.OnPlayerPositionUpdated += OnPlayerPositionUpdated;
        }

        subscribedToEvents = true;
        Log("Subscribed to events");
    }

    void UnsubscribeFromEvents()
    {
        if (!subscribedToEvents) return;

        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundEmitted -= OnHeardSound;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised -= OnAlertReceived;
            AlertBus3D.Instance.OnPlayerPositionUpdated -= OnPlayerPositionUpdated;
        }

        subscribedToEvents = false;
    }

    void OnHeardSound(Vector3 position, float radius, float intensity, GameObject source)
    {
        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius) return; // Too far to hear

        // How loud is it to us?
        float loudness = (1f - distance / radius) * intensity * hearingSensitivity;

        bool isPlayer = source != null && source.CompareTag("Player");
        bool isBait = source != null && source.GetComponent<Bait3D>() != null;

        Log($"Heard sound at {position} (loudness: {loudness:F2}, player: {isPlayer}, bait: {isBait})");

        // Update suspicion
        suspicionLevel += loudness * (isPlayer ? 1f : 0.5f);
        investigatePosition = position;

        // Don't interrupt combat
        if (currentState == GuardState.Chase || currentState == GuardState.Attack)
        {
            return;
        }

        // React based on loudness
        if (loudness > 0.7f)
        {
            EnterState(GuardState.Investigate);
        }
        else if (loudness > 0.3f)
        {
            EnterState(GuardState.Suspicious);
        }
    }

    void OnAlertReceived(Vector3 position, GameObject sourceGuard, AlertLevel level)
    {
        // Don't respond to own alerts
        if (sourceGuard == gameObject) return;

        string guardName = sourceGuard != null ? sourceGuard.name : "Unknown";
        Log($"ALERT from {guardName}: {level} at {position}");

        investigatePosition = position;
        lastKnownPlayerPos = position;

        // Update suspicion based on alert level
        switch (level)
        {
            case AlertLevel.Low:
                suspicionLevel += 0.5f;
                break;
            case AlertLevel.Medium:
                suspicionLevel += 1f;
                break;
            case AlertLevel.High:
                suspicionLevel += 2f;
                break;
            case AlertLevel.Critical:
                suspicionLevel = 3f;
                break;
        }

        // Don't interrupt our own combat
        if (currentState == GuardState.Chase || currentState == GuardState.Attack)
        {
            return;
        }

        // Respond based on alert level
        switch (level)
        {
            case AlertLevel.Low:
                // Just become suspicious
                if (currentState == GuardState.Patrol)
                {
                    EnterState(GuardState.Suspicious);
                }
                break;

            case AlertLevel.Medium:
                // Go investigate
                EnterState(GuardState.Investigate);
                break;

            case AlertLevel.High:
                // Player spotted! Search the area
                Log("HIGH ALERT - Searching for intruder!");
                EnterState(GuardState.Search);
                break;

            case AlertLevel.Critical:
                // Another guard is fighting! Go help!
                Log("CRITICAL ALERT - Going to assist!");
                EnterState(GuardState.Chase); // Go directly to chase/assist
                break;
        }
    }

    void OnPlayerPositionUpdated(Vector3 position)
    {
        // Another guard is tracking the player, update our info
        lastKnownPlayerPos = position;

        // If we're searching or investigating, update destination
        if (currentState == GuardState.Search || currentState == GuardState.Investigate)
        {
            investigatePosition = position;
            agent.SetDestination(position);
        }
        // If we're in chase, update destination
        else if (currentState == GuardState.Chase)
        {
            agent.SetDestination(position);
        }
    }

    #endregion

    #region Alerts

    void RaiseAlert(AlertLevel level)
    {
        if (AlertBus3D.Instance != null)
        {
            Vector3 alertPos = player != null ? player.position : transform.position;
            AlertBus3D.Instance.RaiseAlert(alertPos, gameObject, level);
        }
    }

    #endregion

    #region Helpers

    void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Guard:{name}] {message}");
        }
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Vision cone
        if (showVisionCone)
        {
            Vector3 eyePos = eyes != null ? eyes.position : transform.position + Vector3.up * 1.6f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyePos, sightRange);

            // Draw FOV lines
            Vector3 leftDir = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.green;
            Gizmos.DrawRay(eyePos, leftDir * sightRange);
            Gizmos.DrawRay(eyePos, rightDir * sightRange);
        }

        // Smell range
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, smellRange);

        // Current target
        if (Application.isPlaying && investigatePosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, investigatePosition);
            Gizmos.DrawWireSphere(investigatePosition, 0.5f);
        }
    }

    #endregion
}