using UnityEngine;
using UnityEngine.AI;

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
    public Transform eyes;
    [Tooltip("How far the guard can see")]
    public float sightRange = 15f;
    [Tooltip("Field of view angle (degrees)")]
    public float sightAngle = 110f;
    public LayerMask visionBlockers;
    public float hearingSensitivity = 1f;
    public float smellRange = 8f;
    public float smellCheckInterval = 0.5f;
    [Tooltip("Distance to attack player")]
    public float attackRange = 2f;
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1.5f;
    [Tooltip("Damage per attack")]
    public float attackDamage = 10f;
    [Tooltip("Speed when patrolling")]
    public float patrolSpeed = 2f;
    [Tooltip("Speed when investigating")]
    public float investigateSpeed = 3.5f;
    [Tooltip("Speed when chasing")]
    public float chaseSpeed = 5f;
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    public float suspiciousTimeout = 3f;
    [Tooltip("How long to search an area")]
    public float searchDuration = 12f;
    [Tooltip("Radius to search around a point")]
    public float searchRadius = 6f;
    public bool showDebugLogs = true;
    public bool showVisionCone = true;
    NavMeshAgent agent;
    Transform player;
    GuardState currentState = GuardState.Patrol;
    float suspicionLevel = 0f;
    GuardStateBillboard3D stateBillboard;
    float stateTimer = 0f;
    float attackTimer = 0f;
    float smellTimer = 0f;
    float patrolWaitTimer = 0f;
    Vector3 investigatePosition;
    Vector3 lastKnownPlayerPos;
    int patrolIndex = 0;
    int searchPointIndex = 0;
    Vector3 searchCenter;
    bool subscribedToEvents = false;
    bool foundEvidence = false;
    public string DebugStateName
    {
        get { return currentState.ToString(); }
    }
    #region Unity Lifecycle

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        stateBillboard = GetComponentInChildren<GuardStateBillboard3D>();
        SubscribeToEvents();
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
        stateTimer += Time.deltaTime;
        attackTimer -= Time.deltaTime;
        smellTimer += Time.deltaTime;
        if (CanSeePlayer())
        {
            OnSawPlayer();
        }
        if (currentState != GuardState.Chase && currentState != GuardState.Attack)
        {
            if (smellTimer >= smellCheckInterval)
            {
                smellTimer = 0f;
                CheckForSmells();
            }
        }
        if (currentState == GuardState.Patrol)
        {
            suspicionLevel = Mathf.Max(0, suspicionLevel - Time.deltaTime * 0.2f);
        }
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
                RaiseAlert(AlertLevel.High);
                break;
            case GuardState.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                suspicionLevel = 3f; // Confirmed threat
                RaiseAlert(AlertLevel.High);
                break;
            case GuardState.Attack:
                agent.isStopped = true;
                RaiseAlert(AlertLevel.Critical);
                break;
        }
    }
    #endregion
    #region Patrol State
    void UpdatePatrol()
    {
        if (AlertBus3D.Instance != null &&
            AlertBus3D.Instance.CurrentGlobalAlert >= AlertLevel.High)
        {
            investigatePosition = AlertBus3D.Instance.LastKnownPlayerPosition;
            Log("Global alert high, switching from patrol to search");
            EnterState(GuardState.Search);
            return;
        }
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
        transform.Rotate(0, 30f * Time.deltaTime, 0);
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
        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            Log("Arrived at investigation point");
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
        if (!agent.pathPending && agent.remainingDistance <= 1.5f)
        {
            GoToNextSearchPoint();
        }
        if (stateTimer >= searchDuration)
        {
            bool combatOngoing = AlertBus3D.Instance != null && AlertBus3D.Instance.CurrentGlobalAlert >= AlertLevel.High;
            if (!combatOngoing)
            {
                Log("Search complete, returning to patrol");
                suspicionLevel = Mathf.Max(0, suspicionLevel - 1f);
                EnterState(GuardState.Patrol);
            }
            else
            {
                stateTimer = 0f;
                Log("Search extended because global alert is high");
            }
        }
    }
    void GoToNextSearchPoint()
    {
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
        agent.isStopped = false;
        agent.SetDestination(player.position);
        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
        }
        if (distance <= attackRange)
        {
            EnterState(GuardState.Attack);
        }
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
        if (distance > attackRange * 1.5f)
        {
            EnterState(GuardState.Chase);
            return;
        }
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.UpdatePlayerPosition(player.position);
        }
    }
    void PerformAttack()
    {
        if (player == null)
        {
            return;
        }
        Log("Attack triggered with damage " + attackDamage);
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        if (health != null)
        {
            health.TakeDamage(attackDamage);
        }
        else
        {
            Log("Tried to attack but player has no PlayerHealth");
        }
    }
    #endregion
    #region Detection
    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eyePos = eyes != null ? eyes.position : transform.position + Vector3.up * 1.6f;
        Vector3 toPlayer = player.position - eyePos;
        float distance = toPlayer.magnitude;
        if (distance > sightRange) return false;
        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > sightAngle * 0.5f) return false;
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
        investigatePosition = strongest.transform.position;
        suspicionLevel += strength * 0.3f;
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
        float loudness = (1f - distance / radius) * intensity * hearingSensitivity;
        bool isPlayer = source != null && source.CompareTag("Player");
        bool isBait = source != null && source.GetComponent<Bait3D>() != null;
        suspicionLevel += loudness * (isPlayer ? 1f : 0.5f);
        investigatePosition = position;
        if (currentState == GuardState.Chase || currentState == GuardState.Attack)
        {
            return;
        }
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
        if (sourceGuard == gameObject) return;
        string guardName = sourceGuard != null ? sourceGuard.name : "Unknown";
        investigatePosition = position;
        lastKnownPlayerPos = position;
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
        if (currentState == GuardState.Chase || currentState == GuardState.Attack)
        {
            return;
        }
        switch (level)
        {
            case AlertLevel.Low:
                if (currentState == GuardState.Patrol)
                {
                    EnterState(GuardState.Suspicious);
                }
                break;
            case AlertLevel.Medium:
                EnterState(GuardState.Investigate);
                break;
            case AlertLevel.High:
                EnterState(GuardState.Search);
                break;
            case AlertLevel.Critical:
                EnterState(GuardState.Chase); // Go directly to chase/assist
                break;
        }
    }
    void OnPlayerPositionUpdated(Vector3 position)
    {
        lastKnownPlayerPos = position;
        if (currentState == GuardState.Search || currentState == GuardState.Investigate)
        {
            investigatePosition = position;
            agent.SetDestination(position);
        }
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