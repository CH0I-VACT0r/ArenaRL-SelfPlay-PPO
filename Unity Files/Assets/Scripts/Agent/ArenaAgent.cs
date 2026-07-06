using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SkillManager))]
public class ArenaAgent : Agent
{
    [Header("Agent Identity")]
    [Tooltip("0 = 전사(Warrior), 1 = 마법사(Mage)")]
    public int classId = 0; // 식별 데이터 추가

    [Header("Movement & Physics")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb { get; private set; }
    private Vector2 lastFacingDirection = Vector2.right;

    [Header("Environment References")]
    public UnityEngine.Transform enemyTransform;
    public ArenaAgent enemyAgent;
    public UnityEngine.Transform spawnPoint;

    [Header("Agent Stats")]
    public float maxHp = 200f;
    public float currentHp { get; private set; }

    [Header("Skill Deck System")]
    public int[] initialSkillIds = new int[4] { 1, 2, 3, 6 };
    private SkillManager skillManager;

    public ISkillVisualizer Visualizer { get; private set; }
    public AgentUI agentUI;

    public bool isStunned { get; private set; }
    public bool isInvincible { get; private set; }
    public bool isCcImmune { get; private set; }
    public bool isCharging { get; private set; }

    private float maxChargeTimer;
    private float stunTimer;
    private float buffTimer;
    private float chargeTimer;

    private SkillBase activeCastSkill;
    public bool isDashing { get; private set; }
    private float dashTimer;
    private Vector2 dashDirection;
    private float dashSpeed;

    private void Update()
    {
        if (agentUI == null) return;

        if (skillManager != null)
        {
            for (int i = 0; i < 4; i++)
            {
                SkillBase skill = skillManager.equippedSkills[i];
                if (skill != null) agentUI.UpdateCooldown(i, skill.CurrentCooldown, skill.MaxCooldown);
            }
        }

        if (isStunned) agentUI.UpdateStatusText("Stunned", Color.black);
        else if (isInvincible) agentUI.UpdateStatusText("Invincible", Color.yellow);
        else if (isCcImmune) agentUI.UpdateStatusText("CC Immune", Color.blue);
        else agentUI.UpdateStatusText("", Color.white);

        if (isCharging && maxChargeTimer > 0f)
        {
            float progress = 1f - (chargeTimer / maxChargeTimer);
            agentUI.UpdateChargeBar(progress);
        }
        else
        {
            agentUI.UpdateChargeBar(0f);
        }
    }

    private void FixedUpdate()
    {
        UpdateTimers();

        // 생존 시간 및 상대와의 거리 통계 누적
        if (enemyTransform != null && TelemetryManager.Instance != null)
        {
            float distance = Vector2.Distance(transform.position, enemyTransform.position);
            TelemetryManager.Instance.RecordStep(classId, distance, Time.fixedDeltaTime);
        }
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        skillManager = GetComponent<SkillManager>();

#if UNITY_EDITOR
        Visualizer = new EditorSkillVisualizer();
#else
        Visualizer = new NullSkillVisualizer();   
#endif
    }

    public override void OnEpisodeBegin()
    {
        // 이전 판의 데이터를 CSV로 저장
        if (classId == 0 && TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.ExportEpisodeData();
        }

        // 에피소드 초기화
        foreach (var proj in FindObjectsOfType<ProjectileHelper>())
        {
            if (proj.caster == this || proj.caster == enemyAgent) proj.gameObject.SetActive(false);
        }
        foreach (var field in FindObjectsOfType<DamageFieldHelper>())
        {
            if (field.caster == this || field.caster == enemyAgent) field.gameObject.SetActive(false);
        }
        foreach (var orb in FindObjectsOfType<OrbitingHelper>())
        {
            if (orb.caster == this || orb.caster == enemyAgent) orb.gameObject.SetActive(false);
        }

        transform.position = spawnPoint.position;
        rb.velocity = Vector2.zero;
        lastFacingDirection = (enemyTransform.position - transform.position).normalized;

        currentHp = maxHp;

        isStunned = false;
        isInvincible = false;
        isCcImmune = false;
        isCharging = false;
        stunTimer = 0f; buffTimer = 0f; chargeTimer = 0f;

        skillManager.Initialize(this, initialSkillIds);

        if (agentUI != null)
        {
            agentUI.UpdateHealth(currentHp, maxHp);
            for (int i = 0; i < 4; i++)
            {
                SkillBase skill = skillManager.equippedSkills[i];
                if (skill != null)
                {
                    agentUI.SetupSkillIcon(i, skill.SkillIcon);
                }
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- 1. 본인 상태 (13차원) ---
        sensor.AddObservation((float)classId); // [NEW] 직업 식별자 추가
        sensor.AddObservation(transform.localPosition.x / 5f);
        sensor.AddObservation(transform.localPosition.y / 5f);
        sensor.AddObservation(rb.velocity.normalized.x);
        sensor.AddObservation(rb.velocity.normalized.y);
        sensor.AddObservation(currentHp / maxHp);

        sensor.AddObservation(skillManager.GetSkillReadyStatus(0));
        sensor.AddObservation(skillManager.GetSkillReadyStatus(1));
        sensor.AddObservation(skillManager.GetSkillReadyStatus(2));
        sensor.AddObservation(skillManager.GetSkillReadyStatus(3));

        sensor.AddObservation(isStunned ? 1f : 0f);
        sensor.AddObservation(isInvincible ? 1f : 0f);
        sensor.AddObservation(isCcImmune ? 1f : 0f);

        // --- 2. 적의 상태 (10차원) ---
        if (enemyTransform != null && enemyAgent != null)
        {
            Vector2 relativePos = enemyTransform.localPosition - transform.localPosition;
            sensor.AddObservation(relativePos.x / 10f);
            sensor.AddObservation(relativePos.y / 10f);
            sensor.AddObservation(enemyAgent.rb.velocity.normalized.x);
            sensor.AddObservation(enemyAgent.rb.velocity.normalized.y);
            sensor.AddObservation(enemyAgent.currentHp / enemyAgent.maxHp);
            sensor.AddObservation(Mathf.Clamp01(relativePos.magnitude / 10f));
            sensor.AddObservation(enemyAgent.isStunned ? 1f : 0f);
            sensor.AddObservation(enemyAgent.isInvincible ? 1f : 0f);
            sensor.AddObservation(enemyAgent.isCcImmune ? 1f : 0f);
            sensor.AddObservation(enemyAgent.isCharging ? 1f : 0f);
        }
        else
        {
            for (int i = 0; i < 10; i++) sensor.AddObservation(0f);
        }

        // --- 3. 환경 위협 레이더 (7차원 패딩) ---
        Vector2 closestHazard = GetClosestHazardRelativePosition();

        // 가장 가까운 위험물의 X, Y 좌표 (2차원 사용)
        sensor.AddObservation(closestHazard.x / 10f);
        sensor.AddObservation(closestHazard.y / 10f);

        // 나머지 임시 차원 : 5  (0으로 패딩)
        for (int i = 0; i < 5; i++) sensor.AddObservation(0f);

        // 최종 크기: 13 + 10 + 7 = 30차원
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (isStunned)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        int moveAction = actionBuffers.DiscreteActions[0];
        int skillAction = actionBuffers.DiscreteActions[1];

        if (!isCharging && !isDashing)
        {
            Vector2 moveVector = Vector2.zero;
            switch (moveAction)
            {
                case 1: moveVector = Vector2.up; break;
                case 2: moveVector = new Vector2(1, 1).normalized; break;
                case 3: moveVector = Vector2.right; break;
                case 4: moveVector = new Vector2(1, -1).normalized; break;
                case 5: moveVector = Vector2.down; break;
                case 6: moveVector = new Vector2(-1, -1).normalized; break;
                case 7: moveVector = Vector2.left; break;
                case 8: moveVector = new Vector2(-1, 1).normalized; break;
            }

            if (moveVector != Vector2.zero) lastFacingDirection = moveVector;

            float currentSpeed = isCcImmune ? moveSpeed * 0.6f : moveSpeed;
            rb.velocity = moveVector * currentSpeed;
        }

        if (skillAction != 0 && !isCharging)
        {
            skillManager.TryExecuteSkill(skillAction - 1);
        }

        AddReward(-0.0005f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;

        if (gameObject.name.Contains("Warrior"))
        {
            if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 2;
            else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 4;
            else if (Input.GetKey(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 6;
            else if (Input.GetKey(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 8;
            else if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1;
            else if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 3;
            else if (Input.GetKey(KeyCode.DownArrow)) discreteActionsOut[0] = 5;
            else if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 7;

            if (Input.GetKey(KeyCode.Z)) discreteActionsOut[1] = 1;
            else if (Input.GetKey(KeyCode.X)) discreteActionsOut[1] = 2;
            else if (Input.GetKey(KeyCode.C)) discreteActionsOut[1] = 3;
            else if (Input.GetKey(KeyCode.V)) discreteActionsOut[1] = 4;
        }
        else if (gameObject.name.Contains("Mage"))
        {
            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 2;
            else if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 4;
            else if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 6;
            else if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 8;
            else if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
            else if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 3;
            else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 5;
            else if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 7;

            if (Input.GetKey(KeyCode.J)) discreteActionsOut[1] = 1;
            else if (Input.GetKey(KeyCode.K)) discreteActionsOut[1] = 2;
            else if (Input.GetKey(KeyCode.L)) discreteActionsOut[1] = 3;
            else if (Input.GetKey(KeyCode.Semicolon)) discreteActionsOut[1] = 4;
        }
    }

    private void UpdateTimers()
    {
        float dt = Time.fixedDeltaTime;
        skillManager.UpdateCooldowns(dt);

        if (isStunned)
        {
            stunTimer -= dt;
            if (stunTimer <= 0f) isStunned = false;
        }

        if (buffTimer > 0f)
        {
            buffTimer -= dt;
            if (buffTimer <= 0f) { isInvincible = false; isCcImmune = false; }
        }

        if (isCharging)
        {
            chargeTimer -= dt;
            if (chargeTimer <= 0f)
            {
                isCharging = false;
                if (activeCastSkill != null)
                {
                    activeCastSkill.OnCastComplete(this);
                    activeCastSkill = null;
                }
            }
        }

        if (isDashing)
        {
            dashTimer -= dt;
            rb.velocity = dashDirection * dashSpeed;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.velocity = Vector2.zero;
            }
        }
    }

    public Vector2 GetFacingDirection()
    {
        if (rb.velocity.sqrMagnitude > 0.01f) return rb.velocity.normalized;
        return lastFacingDirection;
    }

    public void StartCasting(float time, SkillBase skillRef)
    {
        isCharging = true;
        maxChargeTimer = time;
        chargeTimer = time;
        activeCastSkill = skillRef;
        rb.velocity = Vector2.zero;
    }

    public void ActivateParry(float time)
    {
        isInvincible = true;
        buffTimer = time;
    }

    public void ActivateCcImmune(float time)
    {
        isCcImmune = true;
        buffTimer = time;
    }

    public void ApplyStun(float duration)
    {
        if (isCcImmune || isInvincible) return;

        isStunned = true;
        stunTimer = duration;
        rb.velocity = Vector2.zero;

        if (isCharging)
        {
            isCharging = false;
            activeCastSkill = null;
        }

        if (isDashing)
        {
            isDashing = false;
        }
    }

    public void TakeDamage(float damage, ArenaAgent attacker)
    {
        if (isInvincible)
        {
            if (buffTimer > 0f && attacker != null)
            {
                attacker.ApplyStun(1.0f);
                attacker.TakeDamage(damage * 1.5f, this);
                AddReward(0.2f);
            }
            return;
        }

        float finalDamage = isCcImmune ? damage * 0.5f : damage;
        currentHp -= finalDamage;

        if (TelemetryManager.Instance != null)
            TelemetryManager.Instance.RecordDamageTaken(classId, finalDamage);

        if (agentUI != null) agentUI.UpdateHealth(currentHp, maxHp);
        if (attacker != null) attacker.AddReward(0.01f * finalDamage);

        if (currentHp <= 0)
        {
            currentHp = 0;
            AddReward(-1.0f);

            if (TelemetryManager.Instance != null)
            {
                TelemetryManager.Instance.RecordResult(this.classId, false);
                if (attacker != null) TelemetryManager.Instance.RecordResult(attacker.classId, true);
            }

            if (attacker != null) attacker.AddReward(1.0f);
            EndEpisode();
            if (attacker != null) attacker.EndEpisode();
        }
    }

    public void StartDash(float duration, float speed, Vector2 direction)
    {
        isDashing = true;
        dashTimer = duration;
        dashSpeed = speed;
        dashDirection = direction.normalized;
        rb.velocity = dashDirection * dashSpeed;
    }

    private Vector2 GetClosestHazardRelativePosition()
    {
        Vector2 closestPos = Vector2.zero;
        float minDistance = float.MaxValue;
        bool found = false;

        // 적 스킬 투사체 탐지
        var projectiles = FindObjectsOfType<ProjectileHelper>();
        foreach (var proj in projectiles)
        {
            if (proj.gameObject.activeInHierarchy && proj.caster == enemyAgent)
            {
                float dist = Vector2.Distance(transform.position, proj.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPos = proj.transform.position - transform.position;
                    found = true;
                }
            }
        }

        // 적 스킬 장판 탐지
        var fields = FindObjectsOfType<DamageFieldHelper>();
        foreach (var field in fields)
        {
            if (field.gameObject.activeInHierarchy && field.caster == enemyAgent)
            {
                float dist = Vector2.Distance(transform.position, field.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPos = field.transform.position - transform.position;
                    found = true;
                }
            }
        }

        // 위험물이 존재하면 상대 좌표를, 없으면 0,0을 반환
        return found ? closestPos : Vector2.zero;
    }
}