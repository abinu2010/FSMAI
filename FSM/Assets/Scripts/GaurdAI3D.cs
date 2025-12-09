using UnityEngine;
using UnityEngine.AI;

public enum Guard3DState
{
    Patrol,
    Suspicious,
    InvestigateSound,
    InvestigateSmell,
    Chase,
    Attack,
    Search,
    Assist
}

[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI3D : MonoBehaviour
{
    [Header("Vision")]
    public Transform eyes;
    public float sightRange = 12f;
    public float sightAngle = 90f;
    public LayerMask visionObstacles;

    [Header("Combat")]
    public float chaseRange = 16f;
    public float attackRange = 2f;
    public float attackCooldown = 1.2f;
    public float attackDamage = 10f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;

    [Header("Investigation")]
    public float suspicionDuration = 4f;
    public float searchDuration = 8f;
    public float searchRadius = 5f;

    [Header("Smell Detection")]
    public float smellCheckRadius = 6f;
    public float smellSuspicionThreshold = 0.5f;
    public float smellAlertThreshold = 1.2f;
    public float smellCheckInterval = 0.5f;

    [Header("Sound Analysis")]
    public float baitSoundConfidence = 0.3f;
    public float playerSoundConfidence = 1.0f;

    [Header("UI")]
    public GuardStateBillboard3D stateBillboard;

    NavMeshAgent agent;
    Transform player;
    Guard3DState currentState;
    int patrolIndex;
    float stateTimer;
    float attackTimer;
    float smellCheckTimer;
    Vector3 investigateTarget;
    Vector3 lastKnownPlayerPos;
    Vector3 searchStartPos;
    int searchPointIndex;
    float currentSuspicionLevel;
    bool hasDirectEvidence;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundEmitted += OnSoundHeard;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised += OnAlertReceived;
        }
    }

    void OnDisable()
    {
        if (SoundBus3D.Instance != null)
        {
            SoundBus3D.Instance.OnSoundEmitted -= OnSoundHeard;
        }

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.OnAlertRaised -= OnAlertReceived;
        }
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        SetState(Guard3DState.Patrol);
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;
        stateTimer += Time.deltaTime;
        smellCheckTimer += Time.deltaTime;

        if (currentState == Guard3DState.Patrol || currentState == Guard3DState.Suspicious)
        {
            currentSuspicionLevel = Mathf.Max(0, currentSuspicionLevel - Time.deltaTime * 0.2f);
        }

        if (CanSeePlayer())
        {
            float distance = Vector3.Distance(transform.position, player.position);
            lastKnownPlayerPos = player.position;
            hasDirectEvidence = true;
            currentSuspicionLevel = 2f;

            if (distance <= attackRange)
            {
                SetState(Guard3DState.Attack);
            }
            else
            {
                SetState(Guard3DState.Chase);
            }
        }

        switch (currentState)
        {
            case Guard3DState.Patrol:
                UpdatePatrol();
                CheckSmell();
                break;
            case Guard3DState.Suspicious:
                UpdateSuspicious();
                CheckSmell();
                break;
            case Guard3DState.InvestigateSound:
                UpdateInvestigateSound();
                CheckSmell();
                break;
            case Guard3DState.InvestigateSmell:
                UpdateInvestigateSmell();
                break;
            case Guard3DState.Chase:
                UpdateChase();
                break;
            case Guard3DState.Attack:
                UpdateAttack();
                break;
            case Guard3DState.Search:
                UpdateSearch();
                CheckSmell();
                break;
            case Guard3DState.Assist:
                UpdateAssist();
                CheckSmell();
                break;
        }
    }

    void SetState(Guard3DState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        Guard3DState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        if (stateBillboard != null)
        {
            stateBillboard.SetText(newState.ToString());
        }

        UnityEngine.Debug.Log($"[GuardAI3D] {name} state {previousState} to {newState} (suspicion: {currentSuspicionLevel:F2})");

        switch (newState)
        {
            case Guard3DState.Patrol:
                GoNextPatrolPoint();
                break;

            case Guard3DState.Chase:
                agent.speed *= 1.3f;
                if (player != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                if (AlertBus3D.Instance != null && hasDirectEvidence)
                {
                    AlertBus3D.Instance.RaiseAlert(transform.position, gameObject, AlertLevel.High);
                }
                break;

            case Guard3DState.InvestigateSound:
            case Guard3DState.InvestigateSmell:
                agent.isStopped = false;
                agent.SetDestination(investigateTarget);
                break;

            case Guard3DState.Search:
                searchStartPos = investigateTarget;
                searchPointIndex = 0;
                agent.isStopped = false;
                SetNextSearchPoint();
                break;

            case Guard3DState.Assist:
                agent.isStopped = false;
                agent.SetDestination(investigateTarget);
                break;

            case Guard3DState.Attack:
                agent.isStopped = false;
                break;

            case Guard3DState.Suspicious:
                agent.isStopped = true;
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * 1.8f;
        Vector3 toPlayer = player.position - origin;
        float distance = toPlayer.magnitude;

        if (distance > sightRange)
        {
            return false;
        }

        Vector3 dir = toPlayer.normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        if (angle > sightAngle * 0.5f)
        {
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, sightRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                UnityEngine.Debug.Log($"[GuardAI3D] {name} sees player at {distance:F1}m");
                return true;
            }
        }

        return false;
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (stateTimer >= patrolWaitTime)
            {
                GoNextPatrolPoint();
            }
        }
    }

    void GoNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        Vector3 target = patrolPoints[patrolIndex].position;
        agent.isStopped = false;
        agent.SetDestination(target);
        UnityEngine.Debug.Log($"[GuardAI3D] {name} patrol to {target}");
    }

    void UpdateSuspicious()
    {
        agent.isStopped = true;
        transform.Rotate(Vector3.up, 45f * Time.deltaTime);

        if (stateTimer >= suspicionDuration)
        {
            if (currentSuspicionLevel > 1f)
            {
                investigateTarget = transform.position;
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Patrol);
            }
        }
    }

    void UpdateInvestigateSound()
    {
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            UnityEngine.Debug.Log($"[GuardAI3D] {name} reached sound location");

            if (currentSuspicionLevel > 1f)
            {
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Suspicious);
            }
        }
    }

    void UpdateInvestigateSmell()
    {
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            UnityEngine.Debug.Log($"[GuardAI3D] {name} reached smell source");

            CheckSmellAtLocation(investigateTarget);

            if (currentSuspicionLevel > 1.5f)
            {
                if (AlertBus3D.Instance != null)
                {
                    AlertBus3D.Instance.RaiseAlert(investigateTarget, gameObject, AlertLevel.Medium);
                }
                SetState(Guard3DState.Search);
            }
            else
            {
                SetState(Guard3DState.Suspicious);
            }
        }
    }

    void UpdateChase()
    {
        if (player == null)
        {
            SetState(Guard3DState.Search);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!agent.pathPending)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (distance > chaseRange && !CanSeePlayer())
        {
            UnityEngine.Debug.Log($"[GuardAI3D] {name} lost player");
            lastKnownPlayerPos = player.position;
            investigateTarget = lastKnownPlayerPos;
            SetState(Guard3DState.Search);
        }
        else if (distance <= attackRange)
        {
            SetState(Guard3DState.Attack);
        }
    }

    void UpdateAttack()
    {
        if (player == null)
        {
            SetState(Guard3DState.Search);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange * 1.5f)
        {
            SetState(Guard3DState.Chase);
            return;
        }

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    void PerformAttack()
    {
        UnityEngine.Debug.Log($"[GuardAI3D] {name} attacks player for {attackDamage} damage!");

        // TODO: Apply damage to player
        // player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    void UpdateSearch()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            SetNextSearchPoint();
        }

        if (stateTimer >= searchDuration)
        {
            UnityEngine.Debug.Log($"[GuardAI3D] {name} search complete, returning to patrol");
            SetState(Guard3DState.Patrol);
        }
    }

    void SetNextSearchPoint()
    {
        float angle = searchPointIndex * 90f;
        float radius = searchRadius;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );

        Vector3 searchPoint = searchStartPos + offset;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(searchPoint, out navHit, radius * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
            UnityEngine.Debug.Log($"[GuardAI3D] {name} search point {searchPointIndex} to {navHit.position}");
        }

        searchPointIndex++;
    }

    void UpdateAssist()
    {
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1f)
        {
            UnityEngine.Debug.Log($"[GuardAI3D] {name} arrived to assist");
            investigateTarget = transform.position;
            SetState(Guard3DState.Search);
        }
    }

    void CheckSmell()
    {
        if (smellCheckTimer < smellCheckInterval)
        {
            return;
        }
        smellCheckTimer = 0f;

        if (ScentNode3D.AllNodes.Count == 0)
        {
            return;
        }

        ScentNode3D bestNode = null;
        float bestScore = 0f;

        foreach (ScentNode3D node in ScentNode3D.AllNodes)
        {
            if (node == null) continue;

            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance > smellCheckRadius)
            {
                continue;
            }

            float score = node.strength / Mathf.Max(distance, 0.1f);
            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        if (bestNode != null && bestScore >= smellSuspicionThreshold)
        {
            investigateTarget = bestNode.transform.position;
            currentSuspicionLevel += bestScore * 0.5f;

            UnityEngine.Debug.Log($"[GuardAI3D] {name} smells something at {investigateTarget} (score: {bestScore:F2}, suspicion: {currentSuspicionLevel:F2})");

            if (bestScore >= smellAlertThreshold)
            {
                UnityEngine.Debug.Log($"[GuardAI3D] {name} STRONG SMELL DETECTED - alerting others!");
                if (AlertBus3D.Instance != null)
                {
                    AlertBus3D.Instance.RaiseAlert(investigateTarget, gameObject, AlertLevel.Medium);
                }
            }

            if (currentState == Guard3DState.Patrol ||
                currentState == Guard3DState.Suspicious ||
                (currentState == Guard3DState.Search && stateTimer > 2f))
            {
                SetState(Guard3DState.InvestigateSmell);
            }
        }
    }

    void CheckSmellAtLocation(Vector3 location)
    {
        float strongestSmell = 0f;

        foreach (ScentNode3D node in ScentNode3D.AllNodes)
        {
            if (node == null) continue;

            float distance = Vector3.Distance(location, node.transform.position);
            if (distance < 2f)
            {
                strongestSmell = Mathf.Max(strongestSmell, node.strength);
            }
        }

        if (strongestSmell > 0f)
        {
            currentSuspicionLevel += strongestSmell;
            UnityEngine.Debug.Log($"[GuardAI3D] {name} found strong smell at location (suspicion +{strongestSmell:F2})");
        }
    }

    void OnSoundHeard(Vector3 position, float radius, float intensity, GameObject source)
    {
        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius)
        {
            return;
        }

        float urgency = (1f - distance / radius) * intensity;
        bool isPlayerSound = source != null && source.CompareTag("Player");

        float confidence = isPlayerSound ? playerSoundConfidence : baitSoundConfidence;
        currentSuspicionLevel += urgency * confidence;

        investigateTarget = position;
        UnityEngine.Debug.Log($"[GuardAI3D] {name} heard sound at {position} (dist: {distance:F1}, urgency: {urgency:F2}, player: {isPlayerSound})");

        if (isPlayerSound)
        {
            lastKnownPlayerPos = position;
        }

        if (currentState == Guard3DState.Chase || currentState == Guard3DState.Attack)
        {
            return;
        }

        SetState(Guard3DState.InvestigateSound);
    }

    void OnAlertReceived(Vector3 position, GameObject sourceGuard, AlertLevel level)
    {
        if (sourceGuard == gameObject)
        {
            return;
        }

        investigateTarget = position;

        // React more urgently to higher alert levels
        switch (level)
        {
            case AlertLevel.Low:
                currentSuspicionLevel += 0.5f;
                break;
            case AlertLevel.Medium:
                currentSuspicionLevel += 1f;
                break;
            case AlertLevel.High:
                currentSuspicionLevel += 2f;
                break;
            case AlertLevel.Critical:
                currentSuspicionLevel += 3f;
                hasDirectEvidence = true; // Treat as if we saw something
                break;
        }

        Debug.Log($"[GuardAI3D] {name} received {level} alert from {sourceGuard.name}");

        // Only respond if not in high-priority state
        if (currentState != Guard3DState.Chase && currentState != Guard3DState.Attack)
        {
            if (level >= AlertLevel.High)
            {
                SetState(Guard3DState.Search); // Skip to search for high alerts
            }
            else
            {
                SetState(Guard3DState.Assist);
            }
        }
    }
    public void OnHitByPlayer(Vector3 hitPoint)
    {
        lastKnownPlayerPos = hitPoint;
        investigateTarget = hitPoint;
        hasDirectEvidence = true;
        currentSuspicionLevel = 2f;

        UnityEngine.Debug.Log($"[GuardAI3D] {name} was hit at {hitPoint}!");
        SetState(Guard3DState.Chase);

        if (AlertBus3D.Instance != null)
        {
            AlertBus3D.Instance.RaiseAlert(hitPoint, gameObject, AlertLevel.Critical);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, smellCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward * sightRange;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward * sightRange;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}