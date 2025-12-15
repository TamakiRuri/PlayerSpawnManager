using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerSpawnManager : LibPlayerSpawnManager
{
    /*
    Modes: Whitelist Persistence Single-Session
    Other settings: white list
    Area Mode: Removed
    */
    [Header(" ")]
    [Header("Player Spawn Manager - プレイヤーのスポーン地点を設定、管理するスクリプト")]
    [Header(" ")]

    [Header("ホワイトリスト機能")]
    [Header("Whitelist")]
    [Header(" ")]

    [Header("ユーザーを入力し、場所を指定すると")]
    [Header("指定されたプレイヤーはJoinとRespawnが特定の場所にテレポートされます")]
    [Header("Input username and assign a transform here")]
    [Header("to let the players Join and Respawn to a different location")]

    [SerializeField] private string[] usernames;
    [Header(" ")]
    [Header("インスタンスオーナー(グループ以外ではインスタンスを作った人)をホワイトリストに入れる")]
    [SerializeField] private bool allowInstanceOwner = true;
    [Header(" ")]
    [Header("指定の場所")]
    [SerializeField] private Transform targetTransform;
    [Header(" ")]
    [Header("プレイヤー居場所保存機能")]
    [Header(" ")]
    [Header("指定の時間（単位：秒）ごとにプレイヤーの場所を保存し、")]
    [Header("次回Joinのときに保存された場所にテレポートされます")]
    [Header("Respawnと別のインスタンスでは動作しません")]
    [Header("Save player position so they will be teleported there when they join the same instance the next time.")]
    [Header("Will not work on Respawns")]
    [SerializeField] private bool savePlayerPosition = true;

    [Header(" ")]
    [Header("ホワイトリストに入ってる人しか使えないようにする")]
    [SerializeField] private bool isWhiteListedOnly;
    [Header(" ")]
    [Header("ホワイトリストに入ってる人を除く")]
    [Header("上のと同時に使うと実質無効になります")]
    [SerializeField] private bool isWhiteListExcluded;

    [Header(" ")]
    [Header("Persistence機能")]
    [Header(" ")]
    [Header("指定の時間（単位：秒）ごとにプレイヤーの場所を保存し、")]
    [Header("次回Joinのときに保存された場所にテレポートされます")]
    [Header("Respawnでは動作しません。別のインスタンスでも動作します")]
    [Header("好ましくない場合もございます")]
    [Header("Save player position so they will be teleported there for the next time")]
    [Header("Will not work on Respawns but will work in other instances")]
    [Header("May not be favorable in some situations")]
    [SerializeField] private bool persistence = false;

    [Header(" ")]
    [Header("保存の時間間隔(秒)")]
    [Header("時間を短くしすぎると重くなる可能性があります")]
    [Header("In seconds. This could become laggy if saving too frequently")]
    [SerializeField] private float saveInverval = 5f;
    // [Header(" ")]
    // [Header("指定のエリアでしか保存しないようにする")]
    // [Header("Respawnしない限りいつでも指定のエリアにテレポートされることになるため")]
    // [Header("好ましくない場合もございます")]
    // [Header("このプレハブにあるコライダーを調整する必要があります")]
    // [Header("Triggerなコライダーが必要です")]
    // [Header("コライダーを複数いれても大丈夫です")]
    // [Header("Persistence モードのみ有効")]
    // [Header("Save only in area set by the player")]
    // [Header("Player will always be teleported to the area")]
    // [Header("and this could be not ideal for some situations")]
    // [Header("Editing the colliders on this prefab is required to use this")]
    // [Header("Trigger must be turned on for this to work")]
    // [Header("You can have multiple colliders here")]
    // [Header("Only works on Persistence Mode")]
    // [SerializeField] private bool saveOnlyInArea = false;

    private string PlayerPositionKey = "SavedPosition";
    private string PlayerRotationKey = "SavedRotation";
    private string PlayerSavedKey = "PlayerSaved";
    private bool isPaused = false;
    private bool isSaved = false;
    //private bool isInArea = false;
    private bool shouldOn = false;
    private bool haveTeleported = false;
    private VRCPlayerApi[] players;
    [UdonSynced] private string[] savedUsers = new string[0];
    //[UdonSynced] private string[] userInArea;
    [UdonSynced] private Vector3[] savedPlayerLocation = new Vector3[0];
    [UdonSynced] private Quaternion[] savedPlayerQuaternion = new Quaternion[0];

    void Start()
    {
        if (Networking.LocalPlayer.isMaster)
        shouldOn = UserCheck(Networking.LocalPlayer);
        // if (saveOnlyInArea && !savePlayerPosition)
        //     saveOnlyInArea = false;
        if (shouldOn && targetTransform != null && !isSaved)
            Networking.LocalPlayer.TeleportTo(targetTransform.position, targetTransform.rotation);

        if (savePlayerPosition)
            SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);

    }

    //Whitelist
    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        if (shouldOn && targetTransform != null)
        {
            Networking.LocalPlayer.TeleportTo(targetTransform.position, targetTransform.rotation);
        }
        else
        {
            base.OnPlayerRespawn(player);
        }
    }


    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        Debug.Log("Player Spawn Manager: Join Debug " + PrintDebugInfo());
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        if(savePlayerPosition && !persistence)
        {
            RequestSerialization();
        }
        Debug.Log("Player Spawn Manager: Exit Debug " + PrintDebugInfo());
    }



    // Single Session
    private void SaveToSyncedObjects(VRCPlayerApi player)
    {
        int index = Array.IndexOf(savedUsers, player.displayName);
        if (Utilities.IsValid(player))
        if (index >= 0)
        {
            savedPlayerLocation[index] = player.GetPosition(); 
            savedPlayerQuaternion[index] = player.GetRotation();
        }
        else
        {
            savedUsers = Append(savedUsers, player.displayName);
            savedPlayerLocation = Append(savedPlayerLocation, player.GetPosition());
            savedPlayerQuaternion = Append(savedPlayerQuaternion, player.GetRotation());
        }
        else Debug.LogWarning("Player Spawn Manager: Save Debug: Player Object Not Present");
    }

    // Single Session
    public override void OnDeserialization()
    {
        if (!savePlayerPosition || persistence) return;
        if (!shouldOn && isWhiteListedOnly) return;
        if (shouldOn && isWhiteListExcluded) return;
        if (haveTeleported) return;
        int index = Array.IndexOf(savedUsers, Networking.LocalPlayer.displayName);
        // if (index == -1)
        // {
        //     index = Array.IndexOf(userInArea, Networking.LocalPlayer.displayName);
        // }
        if (index == -1)
        {
            haveTeleported = true;
            Debug.Log("Player Spawn Manager: Join Debug : No Saved Location");
            return;
        }
        Debug.Log("Player Spawn Manager: Joiner Debug " + PrintDebugInfo());
        Networking.LocalPlayer.TeleportTo(savedPlayerLocation[index], savedPlayerQuaternion[index]);
        haveTeleported = true;
        Debug.Log("Player Spawn Manager: Join Debug : Teleported");
    }


    
    // Persistence
    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        if (!persistence) return;
        if (!shouldOn && isWhiteListedOnly) return;
        if (shouldOn && isWhiteListExcluded) return;
        if (PlayerData.TryGetBool(player, PlayerSavedKey, out isSaved))
            if (!savePlayerPosition || !isSaved) return;
        if (PlayerData.TryGetVector3(player, PlayerPositionKey, out Vector3 l_playerPosition))
        {
            if (PlayerData.TryGetQuaternion(player, PlayerRotationKey, out Quaternion l_playerRotation))
            {
                player.TeleportTo(l_playerPosition, l_playerRotation);
            }
        }
    }

    // 定期保存
    public void SavePlayerTransform()
    {
        
        if (isPaused) return;
        if (persistence)
        {
            //if (saveOnlyInArea && !isInArea) return;
            VRCPlayerApi player = Networking.LocalPlayer;

            // 着地の時以外やり直す
            if (!player.IsPlayerGrounded())
            {
                SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), 1);
                return;
            }
            PlayerData.SetVector3(PlayerPositionKey, player.GetPosition());
            PlayerData.SetQuaternion(PlayerRotationKey, player.GetRotation());
            if (!isSaved)
            {
                isSaved = true;
                PlayerData.SetBool(PlayerSavedKey, true);
            }
        }
        else
        {
            if (players == null)
            {
                Debug.LogWarning("Player Spawn Manager: Save Debug: Not Initialized");
                SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);
                return;
            }
            int playerCount = VRCPlayerApi.GetPlayerCount();
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            for(int i = 0; i < playerCount; i++)
            {
                SaveToSyncedObjects(players[i]);
            }
            Debug.Log("Player Spawn Manager: Save Debug " + PrintDebugInfo());
        }

        //if ((saveOnlyInArea && isInArea) || !saveOnlyInArea)
        SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);
    }

    
    
    // Area Mode
    // Removed due to logic issues.
    // public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    // {
    //     if (!saveOnlyInArea) return;
    //     //if (!player.isLocal) return;
    //     if (player.isLocal) isInArea = true;
    //     else if (!persistence)
    //     {
    //         if (Array.IndexOf(userInArea, "") != -1)
    //             {
    //                 userInArea[Array.IndexOf(userInArea, "")] = player.displayName;
    //             }
    //         else userInArea = Append(userInArea, player.displayName);
    //         RequestSerialization();
    //     }
    //     SendCustomEvent(nameof(SavePlayerTransform));
    // }
    // public override void OnPlayerTriggerExit(VRCPlayerApi player)
    // {
    //     if (!saveOnlyInArea) return;
    //     //if (!player.isLocal) return;
    //     if (player.isLocal) isInArea = false;
    //     else if (!persistence)
    //     {
    //         int index = Array.IndexOf(userInArea, player.displayName);
    //         if (index != -1)
    //         {
    //             userInArea[index] = "";
    //         }
    //     }
    // }

    // Utils
    private bool UserCheck(VRCPlayerApi player)
    {
        return Array.IndexOf(usernames, player.displayName) != -1 || (allowInstanceOwner && player.isInstanceOwner);
    }

    private bool AvailabilityCheck()
    {
        if (!shouldOn && isWhiteListedOnly)
        {
            if (shouldOn && isWhiteListExcluded)
            return true;
        }
        return false;
    }

    // For Info Center
    public bool UserCheck()
    {
        return Array.IndexOf(usernames, Networking.LocalPlayer.displayName) != -1 || (allowInstanceOwner && Networking.LocalPlayer.isInstanceOwner);
    }
    public void AddCurrentUserToWhitelist()
    {
        shouldOn = true;
    }
    public void ClearSavedInfo()
    {
        if (savePlayerPosition)
        {
            isPaused = true;
            isSaved = false;
            PlayerData.SetBool(PlayerSavedKey, false);
        }
    }
    public bool GetActive()
    {
        return savePlayerPosition;
    }
    public bool GetMode()
    {
        return persistence;
    }
    public bool GetWhiteListedOnly()
    {
        return isWhiteListedOnly;
    }
    public bool GetWhiteListedExcluded()
    {
        return isWhiteListExcluded;
    }
    public Transform GetTeleportTarget()
    {
        if (targetTransform == null) return null;
        return targetTransform;
    }
    public void TeleportToSavedPosition()
    {
        int index = Array.IndexOf(savedUsers, Networking.LocalPlayer.displayName);
        if (index == -1)
        {
            Debug.Log("Player Spawn Manager: Teleport Debug : No Saved Location");
            return;
        }
        Debug.Log("Player Spawn Manager: Teleport Debug " + PrintDebugInfo());
        Networking.LocalPlayer.TeleportTo(savedPlayerLocation[index], savedPlayerQuaternion[index]);
    }
    private string PrintDebugInfo()
    {
        return JoinDebugInfo(savedUsers) + JoinDebugInfo(savedPlayerLocation);
    }
    


}
