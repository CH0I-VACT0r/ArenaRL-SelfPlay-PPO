using UnityEngine;
using Unity.MLAgents.SideChannels;
using System;

public class TelemetrySideChannel : SideChannel
{
    // 고유 UUID
    public TelemetrySideChannel()
    {
        ChannelId = new Guid("11111111-2222-3333-4444-555555555555");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        // 파이썬에서 유니티로 오는 메시지 처리 (현재는 단방향 송신)
    }

    // 파이썬으로 JSON 문자열을 쏘아주는 메서드
    public void SendTelemetryData(string jsonData)
    {
        using (var msgOut = new OutgoingMessage())
        {
            msgOut.WriteString(jsonData);
            QueueMessageToSend(msgOut); // 큐에 담아 다음 통신 틱에 파이썬으로 발송
        }
    }
}

// JsonUtility가 변환할 수 있도록 직렬화 속성 부여
[Serializable] 
public class CombatTelemetryData
{
    public float warriorHitRate;
    public float mageHitRate;
    public float averageDistance;
    public float survivalTime;
    public float warriorDPS;
    public float mageDPS;
}