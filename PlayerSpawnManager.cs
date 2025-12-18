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
    Modes: Whitelist Synced Persistence
    Other settings: white list
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
    [Header("同期モードでは別のインスタンスで動作しません")]
    [Header("Save player position so they will be teleported there when they join the same instance the next time.")]
    [Header("Will not work on Respawns")]
    [SerializeField] private bool savePlayerPosition = true;

    [Header(" ")]
    [Header("同期/保存の時間間隔(秒)")]
    [Header("時間を短くしすぎると重くなる可能性があります")]
    [Header("In seconds. This could become laggy if saving too frequently")]
    [SerializeField] private float saveInverval = 5f;

    [Header(" ")]
    [Header("ホワイトリストに入ってるプレイヤーしか使えないようにする")]
    [SerializeField] private bool isWhiteListedOnly;
    [Header(" ")]
    [Header("ホワイトリストに入ってるプレイヤーを除く")]
    [Header("上のと同時に使うと実質無効になります")]
    [SerializeField] private bool isWhiteListExcluded;

    [Header(" ")]
    [Header("同期モード専用")]
    [Header("同期する最大人数を制限する")]
    [Header("制限を超えると、いまインスタンスにいないプレイヤーのデータが上書きされます")]
    [Header("インスタンスの最大人数より大きく設定してください")]
    [Header("重くなりますのでできるだけ100以下でお願いします")]
    [Header("Synced Mode Only")]
    [Header("Limit maximum amout of people being saved")]
    [Header("Exceeding the limit will cause data of players who aren't in the instance to be overwritten")]
    [Header("Don't set it to over 100 or the game could be laggy when there are a lot of people")]
    [Header("It should be bigger than the maximum allowed player in the instance")]
    [SerializeField] private bool limitSaveCount = true;
    [SerializeField] private int limitThrehold = 60;

    [Header(" ")]
    [Header("Persistenceモード")]
    [Header(" ")]
    [Header("ローカルで保存するため同期を行いません")]
    [Header("同期モードと違い別のインスタンスでも動作します")]
    [Header("好ましくない場合もございます")]
    [Header("オンにしない場合では同期モードになります")]
    [Header("Save player position so they will be teleported there for the next time")]
    [Header("Will not work on Respawns but will work in other instances")]
    [Header("May not be favorable in some situations")]
    [Header("This script will work as Synced Mode if this option is not on")]
    [SerializeField] private bool persistence = false;

    
    
    [Header(" ")]
    [Header("Debug Mode")]
    [Header("保存間隔でログが生成されます。プレイヤー人数が多いほど長くなるため、通常使用ではおすすめしません。")]
    [Header("Generate Log every time player location is saved. Could be really long and frequent")]
    [Header("Not recommended on normal use")]
    [SerializeField] private bool debugMode = false;

    private string PlayerPositionKey = "SavedPosition";
    private string PlayerRotationKey = "SavedRotation";
    private string PlayerSavedKey = "PlayerSaved";
    [UdonSynced] private bool isPaused = false;
    private bool isSaved = false;
    private bool shouldOn = false;
    private bool haveTeleported = false;
    private VRCPlayerApi[] players;
    [UdonSynced] private string[] savedUsers = new string[0];
    [UdonSynced] private Vector3[] savedPlayerLocation = new Vector3[0];
    [UdonSynced] private Quaternion[] savedPlayerQuaternion = new Quaternion[0];

    void Start()
    {
        shouldOn = UserCheck(Networking.LocalPlayer);
        if (shouldOn && targetTransform != null && !isSaved)
            Networking.LocalPlayer.TeleportTo(targetTransform.position, targetTransform.rotation);

        if (savePlayerPosition)
            SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);

    }

    // Whitelist
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

    // Events

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        PrintDebugInfo("Join Debug ");
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        if(savePlayerPosition && !persistence)
        {
            RequestSerialization();
        }
        PrintDebugInfo("Exit Debug ");
    }



    // Synced
    private void SaveToSyncedObjects(VRCPlayerApi player)
    {
        int index = Array.IndexOf(savedUsers, player.displayName);
        if (Utilities.IsValid(player))
        if (index >= 0)
        {
            savedPlayerLocation[index] = player.GetPosition(); 
            savedPlayerQuaternion[index] = player.GetRotation();
        }
        else if (savedUsers.Length >= limitThrehold)
        {
            ReplaceInfo(player);
        }
        else
        {
            savedUsers = Append(savedUsers, player.displayName);
            savedPlayerLocation = Append(savedPlayerLocation, player.GetPosition());
            savedPlayerQuaternion = Append(savedPlayerQuaternion, player.GetRotation());
        }
        else Debug.LogWarning("Player Spawn Manager: Save: Player Object Not Valid");
    }

    // Synced
    public override void OnDeserialization()
    {
        if (!savePlayerPosition || persistence) return;
        if (isPaused) return;
        if (!shouldOn && isWhiteListedOnly) return;
        if (shouldOn && isWhiteListExcluded) return;
        if (haveTeleported) return;
        int index = Array.IndexOf(savedUsers, Networking.LocalPlayer.displayName);
        if (index == -1)
        {
            haveTeleported = true;
            Debug.Log("Player Spawn Manager: Join : No Saved Location");
            return;
        }
        PrintDebugInfo("Joiner Debug ");
        Networking.LocalPlayer.TeleportTo(savedPlayerLocation[index], savedPlayerQuaternion[index]);
        haveTeleported = true;
        Debug.Log("Player Spawn Manager: Join : Teleported");
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
                Debug.LogWarning("Player Spawn Manager: Save: Not Initialized");
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
            PrintDebugInfo("Save Debug ");
        }

        SendCustomEventDelayedSeconds(nameof(SavePlayerTransform), saveInverval);
    }

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
    private void PrintDebugInfo(string mode)
    {
        if (debugMode)
        {
            Debug.Log("Player Spawn Manager: " + mode + JoinDebugInfo(savedUsers) + JoinDebugInfo(savedPlayerLocation));
        }
    }
    private void ReplaceInfo(VRCPlayerApi player)
    {
        for(int i = 0; i<savedUsers.Length; i++)
        {
            if (Array.IndexOf(players, savedUsers[i]) == -1)
            {
                savedUsers[i] = player.displayName;
                savedPlayerLocation[i] = player.GetPosition(); 
                savedPlayerQuaternion[i] = player.GetRotation();
                RequestSerialization();
                return;
            }
        }
        if (limitThrehold < VRCPlayerApi.GetPlayerCount() && limitThrehold < 200)
        {
            Debug.Log("Player Spawn Manager: Player Count Higher than threhold. Changing array limit automatically.");
            limitThrehold = VRCPlayerApi.GetPlayerCount() + 10;
            ReplaceInfo(player);
        }
    }

    // Info Center
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
            RequestSerialization();
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
        PrintDebugInfo("Teleport Debug ");
        Networking.LocalPlayer.TeleportTo(savedPlayerLocation[index], savedPlayerQuaternion[index]);
    }
    


}
