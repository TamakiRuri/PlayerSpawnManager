using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PlayerSpawnManager : UdonSharpBehaviour
{
    [Header(" ")]
    [Header("Player Spawn Manager - プレイヤーのスポーン地点を設定、管理する")]
    [Header(" ")]

    [Header("ホワイトリスト機能")]
    [Header("Whitelist")]
    [Header(" ")]

    [Header("ユーザーを入力し、場所を指定すると")]
    [Header("指定されたプレイヤーはJoinとRespawnが特定の場所にテレポートされます")]
    [Header("Input username and assign a transform here")]
    [Header("to let the players Join and Respawn to a different location")]

    [SerializeField] private String[] usernames;
    [Header(" ")]
    [Header("インスタンスを作った人をホワイトリストに入れるかどうか")]
    [SerializeField] private bool allowInstanceOwner = true;
    [Header(" ")]
    [Header("指定の場所")]
    [SerializeField] private Transform targetTransform;
    [Header(" ")]
    [Header("Persistence機能")]
    [Header(" ")]
    [Header("指定の時間（単位：秒）ごとにプレイヤーの場所を保存し、")]
    [Header("次回Joinのときに保存された場所にテレポートされます")]
    [Header("Respawnには動作しません")]
    [Header("Save player position so they will be teleported there for the next time")]
    [Header("Will not work on Respawns")]
    [SerializeField] private bool savePlayerPosition = true;
    [Header(" ")]
    [Header("ホワイトリストにいる人しか使えないようにするかどうか")]
    [SerializeField] private bool isWhiteListedOnly;
    [Header(" ")]
    [Header("保存の時間間隔(秒)")]
    [Header("重くなる可能性があるので、時間を短くしすぎないようにお願いします")]
    [Header("In seconds. This could become laggy if saving too frequently")]
    [SerializeField] private float saveInverval = 10f;
    [Header(" ")]
    [Header("指定のエリアでしか保存しないようにするかどうか")]
    [Header("Respawnしない限りいつでも指定のエリアにテレポートされることになるため")]
    [Header("好ましくない場合もございます")]
    [Header("このプレハブにあるコライダーを調整する必要があります")]
    [Header("Triggerなコライダーが必要です")]
    [Header("コライダーを複数いれても大丈夫です")]
    [Header("Save only in area set by the player")]
    [Header("Player will always be teleported to the area")]
    [Header("and this could be not ideal for some situations")]
    [Header("Editing the colliders on this prefab is required to use this")]
    [Header("Trigger must be turned on for this to work")]
    [Header("You can have multiple colliders here")]
    [SerializeField] private bool saveOnlyInArea = false;
    private string PlayerPositionKey = "SavedPosition";
    private string PlayerRotationKey = "SavedRotation";
    private string PlayerSavedKey = "PlayerSaved";
    private bool isPaused = false;
    private bool isSaved = false;
    private bool isInArea = false;
    private bool shouldOn = false;
    void Start()
    {
        UserCheck();
        if (saveOnlyInArea && !savePlayerPosition) saveOnlyInArea = false;
        if (shouldOn && targetTransform != null && !isSaved)
        {
            Networking.LocalPlayer.TeleportTo(targetTransform.position, targetTransform.rotation);
        }
        if (savePlayerPosition)
        {
            if ((isWhiteListedOnly && shouldOn) || !isWhiteListedOnly)
            {
                if ((saveOnlyInArea && isInArea) || !saveOnlyInArea)
                SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);
            }
        }

    }
    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;
        if (shouldOn && targetTransform != null)
        {
            Networking.LocalPlayer.TeleportTo(targetTransform.position, targetTransform.rotation);
        }
        else
        {
            base.OnPlayerRespawn(player);
        }
    }
    public void SavePlayerTransform()
    {
        if (isPaused) return;
        if (saveOnlyInArea && !isInArea) return;
        VRCPlayerApi player = Networking.LocalPlayer;

        PlayerData.SetVector3(PlayerPositionKey, player.GetPosition());
        PlayerData.SetQuaternion(PlayerRotationKey, player.GetRotation());
        if (!isSaved)
        {
            isSaved = true;
            PlayerData.SetBool(PlayerSavedKey, true);
        }
        if ((saveOnlyInArea && isInArea) || !saveOnlyInArea)
        SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);
    }
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player != Networking.LocalPlayer) return;
        if (PlayerData.TryGetBool(player, PlayerSavedKey, out isSaved))
        if (savePlayerPosition && isSaved)
        {
            if ((isWhiteListedOnly && shouldOn) || !isWhiteListedOnly)
            {
                if (PlayerData.TryGetVector3(player, PlayerPositionKey, out Vector3 l_playerPosition))
                {
                    if (PlayerData.TryGetQuaternion(player, PlayerRotationKey, out Quaternion l_playerRotation))
                    {
                        player.TeleportTo(l_playerPosition, l_playerRotation);
                    }
                }
            }
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!saveOnlyInArea) return;
        if (player != Networking.LocalPlayer) return;
        isInArea = true;
        SendCustomEvent(nameof(SavePlayerTransform));
    }
    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!saveOnlyInArea) return;
        if (player != Networking.LocalPlayer) return;
        isInArea = false;
    }



    private void UserCheck()
    {
        shouldOn = Array.IndexOf(usernames, Networking.LocalPlayer.displayName) != -1 || (allowInstanceOwner && Networking.LocalPlayer.isInstanceOwner);
    }
    public void AddCurrentUserToWhitelist()
    {
        shouldOn = true;
    }
    public void ClearSavedInfo()
    {
        if (savePlayerPosition)
        {
            if ((isWhiteListedOnly && shouldOn) || !isWhiteListedOnly)
            {
                isPaused = true;
                isSaved = false;
                PlayerData.SetBool(PlayerSavedKey, false);
            }
        }
    }


}
