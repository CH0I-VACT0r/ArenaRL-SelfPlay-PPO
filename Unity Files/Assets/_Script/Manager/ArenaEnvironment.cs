using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;

public class ArenaEnvironment : MonoBehaviour
{
    public static ArenaEnvironment Instance { get; private set; }
    TelemetrySideChannel telemetryChannel;

    [Header("Agents")]
    public ArenaAgent warriorAgent;
    public ArenaAgent mageAgent;

    [Header("Sudden Death System")]
    private float gameTimer = 0f;
    private float dotDamageTimer = 0f;
    private const float SUDDEN_DEATH_START_TIME = 40f; // 40초 이후 활성화
    private const float DOT_DAMAGE_INTERVAL = 0.5f;    // 0.5초 주기
    private const float DAMAGE_PERCENT = 0.05f;       // 최대 체력의 5%
    private bool isEpisodeActive = false;

    [Header("Current Balance Stats (Read Only)")]
    public float warriorMaxHp;
    public float mageMaxHp;
    public float warriorDamageMultiplier;
    public float mageDamageMultiplier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        telemetryChannel = new TelemetrySideChannel();
        SideChannelManager.RegisterSideChannel(telemetryChannel);
    }

    void OnDestroy()
    {
        // 메모리 누수를 막기 위해 종료 시 채널 등록 해제
        if (telemetryChannel != null)
        {
            SideChannelManager.UnregisterSideChannel(telemetryChannel);
        }
    }

    private void Start()
    {
        // 파이썬(Optuna) 환경에서 env.reset()이 호출될 때마다 새로운 파라미터를 받아옴.
        Academy.Instance.OnEnvironmentReset += ResetEnvironment;

        // 에디터 실행 시 초기값 세팅을 위해 한 번 수동 호출
        ResetEnvironment();
    }

    private void ResetEnvironment()
    {
        UpdateBalanceParameters();
        gameTimer = 0f;
        dotDamageTimer = 0f;
        isEpisodeActive = true;
    }

    private void FixedUpdate()
    {
        if (!isEpisodeActive) return;

        gameTimer += Time.fixedDeltaTime;

        // 40초 경과 시점부터 도트 대미지
        if (gameTimer >= SUDDEN_DEATH_START_TIME)
        {
            dotDamageTimer += Time.fixedDeltaTime;
            if (dotDamageTimer >= DOT_DAMAGE_INTERVAL)
            {
                ApplySuddenDeathDamage();
                dotDamageTimer = 0f;
            }
        }
    }

    private float RoundToFive(float value)
    {
        return Mathf.Round(value / 5f) * 5f;
    }


    private void UpdateBalanceParameters()
    {
        // 플레이 테스트
        // ======================================
        // EP08 Trial 26 최적화 파라미터 고정 (8차원)
        warriorMaxHp = 300f;
        mageMaxHp = 150f;
        warriorDamageMultiplier = 1.3f;
        mageDamageMultiplier = 1.5f;

        float warriorSpeedMult = 0.85f;
        float mageSpeedMult = 1.2f;
        float warriorCdMult = 1.15f;
        float mageCdMult = 1.15f;

        if (warriorAgent != null) warriorAgent.maxHp = warriorMaxHp;
        if (mageAgent != null) mageAgent.maxHp = mageMaxHp;

        // 이동 속도 및 쿨타임 배율 주입
        if (warriorAgent != null)
        {
            warriorAgent.currentMoveSpeed = warriorAgent.moveSpeed * warriorSpeedMult;
            warriorAgent.cooldownMultiplier = warriorCdMult;
        }
        if (mageAgent != null)
        {
            mageAgent.currentMoveSpeed = mageAgent.moveSpeed * mageSpeedMult;
            mageAgent.cooldownMultiplier = mageCdMult;
        }
        // ======================================

        // 밸런스 학습
        // ======================================
        // 파이썬(Optuna)에서 전달한 밸런싱 탐색 수치 실시간 수신

        // 체력/대미지 배율 수신
        //float rawWarriorHp = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_hp", 250f);
        //float rawMageHp = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_hp", 230f);
        //float rawWarriorDmg = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_dmg_mult", 0.8f);
        //float rawMageDmg = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_dmg_mult", 1.05f);

        //// 속도 및 쿨타임 배율 수신 (기본값 1.0배)
        //float warriorSpeedMult = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_speed", 1.0f);
        //float mageSpeedMult = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_speed", 1.0f);
        //float warriorCdMult = Academy.Instance.EnvironmentParameters.GetWithDefault("warrior_cd", 1.0f);
        //float mageCdMult = Academy.Instance.EnvironmentParameters.GetWithDefault("mage_cd", 1.0f);

        //// 단위 정제 및 체력/대미지 배율 적용
        //warriorMaxHp = RoundToFive(rawWarriorHp);
        //mageMaxHp = RoundToFive(rawMageHp);
        //warriorDamageMultiplier = Mathf.Round(rawWarriorDmg * 20f) / 20f;
        //mageDamageMultiplier = Mathf.Round(rawMageDmg * 20f) / 20f;

        //if (warriorAgent != null) warriorAgent.maxHp = warriorMaxHp;
        //if (mageAgent != null) mageAgent.maxHp = mageMaxHp;

        //// 에이전트에 파라미터 적용
        //if (warriorAgent != null)
        //{
        //    warriorAgent.currentMoveSpeed = warriorAgent.moveSpeed * warriorSpeedMult;
        //    warriorAgent.cooldownMultiplier = warriorCdMult;
        //}
        //if (mageAgent != null)
        //{
        //    mageAgent.currentMoveSpeed = mageAgent.moveSpeed * mageSpeedMult;
        //    mageAgent.cooldownMultiplier = mageCdMult;
        //}
        // ======================================
    }


    private void ApplySuddenDeathDamage()
    {
        if (warriorAgent == null || mageAgent == null) return;

        // 대미지 적용 전 직전 프레임의 정확한 체력 확보
        float warriorPreHp = warriorAgent.currentHp;
        float magePreHp = mageAgent.currentHp;

        float warriorDot = warriorAgent.maxHp * DAMAGE_PERCENT;
        float mageDot = mageAgent.maxHp * DAMAGE_PERCENT;

        // 환경 대미지이므로 attacker는 null로 전달
        warriorAgent.TakeDamage(warriorDot, null);
        mageAgent.TakeDamage(mageDot, null);

        // 동일 프레임 내 동시 사망 여부 정밀 검사
        if (warriorAgent.currentHp <= 0 || mageAgent.currentHp <= 0)
        {
            isEpisodeActive = false;
            ResolveSuddenDeath(warriorPreHp, magePreHp);
        }
    }

    private void ResolveSuddenDeath(float warriorPreHp, float magePreHp)
    {
        float warriorHpRatio = warriorPreHp / warriorAgent.maxHp;
        float mageHpRatio = magePreHp / mageAgent.maxHp;

        // 도트딜을 맞기 직전 프레임에 HP가 더 많았던 쪽이 승리
        if (warriorHpRatio > mageHpRatio)
        {
            warriorAgent.AddReward(1.0f);
            mageAgent.AddReward(-1.0f);

            if (TelemetryManager.Instance != null)
            {
                TelemetryManager.Instance.RecordResult(0, true);  // 전사 승리 기록
                TelemetryManager.Instance.RecordResult(1, false); // 마법사 패배 기록
            }
        }
        else if (mageHpRatio > warriorHpRatio)
        {
            warriorAgent.AddReward(-1.0f);
            mageAgent.AddReward(1.0f);

            if (TelemetryManager.Instance != null)
            {
                TelemetryManager.Instance.RecordResult(0, false); // 전사 패배 기록
                TelemetryManager.Instance.RecordResult(1, true);  // 마법사 승리 기록
            }
        }
        else
        {
            // 동률일 경우 둘 다 패배(-1.0f) 처리
            warriorAgent.AddReward(-1.0f);
            mageAgent.AddReward(-1.0f);

            if (TelemetryManager.Instance != null)
            {
                // 텔레메트리 상으로도 양측 모두 패배로 기록하여 승률 산정에서 제외
                TelemetryManager.Instance.RecordResult(0, false);
                TelemetryManager.Instance.RecordResult(1, false);
            }
        }

        // 에피소드 종료
        SendTelemetryToPython();
        warriorAgent.EndEpisode();
        mageAgent.EndEpisode();
    }

    public void SendTelemetryToPython()
    {
        // 텔레메트리 매니저가 없으면 에러 방지
        if (TelemetryManager.Instance == null) return;

        // 실제 텔레메트리 데이터 추출
        float w_hit = TelemetryManager.Instance.GetHitRate(0);
        float m_hit = TelemetryManager.Instance.GetHitRate(1);
        float avg_dist = TelemetryManager.Instance.GetAverageDistance();
        float w_dps = TelemetryManager.Instance.GetDPS(0, gameTimer);
        float m_dps = TelemetryManager.Instance.GetDPS(1, gameTimer);

        CombatTelemetryData data = new CombatTelemetryData
        {
            warriorHitRate = float.IsNaN(w_hit) ? 0f : w_hit,
            mageHitRate = float.IsNaN(m_hit) ? 0f : m_hit,
            averageDistance = float.IsNaN(avg_dist) ? 3.2f : avg_dist, // 거리가 안 잡히면 기본 목표값 유지
            survivalTime = gameTimer, // 현재 판의 실제 진행 시간
            warriorDPS = float.IsNaN(w_dps) ? 0f : w_dps,
            mageDPS = float.IsNaN(m_dps) ? 0f : m_dps
        };

        string jsonData = JsonUtility.ToJson(data);
        if (telemetryChannel != null)
        {
            telemetryChannel.SendTelemetryData(jsonData);
        }
    }
}