using System;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;

namespace com.rurinya.spawnmanager
{
    public enum PlayerSpawnManagerMode
    {
        Synced,
        Persistence,
    }
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerSpawnManager : LibPlayerSpawnManager
    {
        /*
        Modes: Whitelist Synced Persistence
        Other settings: white list
        */
        [Header(" ")]
        [Header("Player Spawn Manager - プレイヤーのスポーン地点のホワイトリスト、保存システム")]
        [Header(" ")]

        [Header("ホワイトリスト機能")]
        [Header("Whitelist")]
        [Space(8)]

        [Header("指定されたプレイヤーがJoinとRespawnのときに特定の場所にテレポートされます")]
        [Header("Players here will Join and Respawn to a different location")]

        [SerializeField] private string[] usernames;
        [Space(8)]
        [Header("インスタンスオーナー(グループ以外ではインスタンスを作った人)をホワイトリストに入れる")]
        [SerializeField] private bool allowInstanceOwner = true;
        [Space(8)]
        [Header("指定の場所")]
        [SerializeField] private Transform targetTransform;
        [Header(" ")]
        [Header("プレイヤー居場所保存機能")]
        [Space(8)]
        [Header("指定の時間（単位：秒）ごとにプレイヤーの場所を保存し、")]
        [Header("次回Joinのときに保存された場所にテレポートされます")]
        [Header("リスポーンでは動作しません")]
        [Header("Save player position so they will be teleported there when they join the same instance the next time.")]
        [Header("Will not work on Respawns")]
        [SerializeField] private bool savePlayerPosition = true;
        [Space(8)]
        [Header("Mode: 同期モード: Synced. Persistenceモード: Persistence")]
        [Header("同期モードでは別のインスタンスで動作しません")]
        [Header("Synced Mode will not work in other instances")]
        [SerializeField] private PlayerSpawnManagerMode mode = PlayerSpawnManagerMode.Synced;

        [Space(8)]
        [Header("同期/保存の時間間隔(秒)")]
        [Header("時間を短くしすぎると重くなる可能性があります")]
        [Header("In seconds. This could become laggy if saving too frequently")]
        [SerializeField] private float saveInverval = 5f;

        [Space(8)]
        [Header("ホワイトリストに入ってるプレイヤーしか保存されないようにする")]
        [SerializeField] private bool isWhiteListedOnly;
        [Space(8)]
        [Header("ホワイトリストに入ってるプレイヤーを除く")]
        [Header("ホワイトリストに入ってるプレイヤーの居場所が保存されなくなります")]
        [Header("上のと同時に使うと実質無効になります")]
        [SerializeField] private bool isWhiteListExcluded;

        [Space(8)]
        [Header("同期モード専用")]
        [Header("同期する最大人数を制限する")]
        [Header("制限を超えると、いまインスタンスにいないプレイヤーのデータが上書きされます")]
        [Header("インスタンスの最大人数より大きく設定してください")]
        [Header("重くなりますのでできるだけ100以下にしてください")]
        [Header("Synced Mode Only")]
        [Header("Limit maximum amout of people being saved")]
        [Header("Exceeding the limit will cause data of players who aren't in the instance to be overwritten")]
        [Header("Don't set it to over 100 or the game could be laggy when there are a lot of people")]
        [Header("It should be bigger than the maximum allowed player in the instance")]
        [SerializeField] private bool limitSaveCount = true;
        [SerializeField] private int limitThrehold = 60;

        // NOT IMPLEMENTED
        private GameObject teleportToInstances;

        
        
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
        [UdonSynced] private bool spawnToInstances = false;

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
            if(savePlayerPosition && (int)mode == 0)
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
            if (!savePlayerPosition) return;
            if (isPaused) return;
            if (!shouldOn && isWhiteListedOnly) return;
            if (shouldOn && isWhiteListExcluded) return;
            if (haveTeleported) return;

            // Spawn To instances
            if (spawnToInstances)
            {
                haveTeleported = true;
                TeleportToInstances();
                return;
            }
            if ((int)mode == 1) return;

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
            if ((int)mode == 0) return;
            if (!shouldOn && isWhiteListedOnly) return;
            if (shouldOn && isWhiteListExcluded) return;
            if (spawnToInstances) return;
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
            if ((int)mode == 1)
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

        #region ManagerPanel

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
                savedPlayerLocation = new Vector3[0];
                savedPlayerQuaternion = new Quaternion[0];
                savedUsers = new string[0];
                RequestSerialization();
            }
        }
        public bool GetActive()
        {
            return savePlayerPosition;
        }
        public bool GetMode()
        {
            return (int)mode == 1;
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
            if (savePlayerPosition)
            if ((int)mode == 0)
            {
                int index = Array.IndexOf(savedUsers, Networking.LocalPlayer.displayName);
                if (index == -1)
                {
                    Debug.LogWarning("Player Spawn Manager: Teleport Debug : No Saved Location");
                    return;
                }
                PrintDebugInfo("Teleport Debug ");
                Networking.LocalPlayer.TeleportTo(savedPlayerLocation[index], savedPlayerQuaternion[index]);
            }
            else
            {
                Vector3 l_targetPosition = PlayerData.GetVector3(Networking.LocalPlayer, PlayerPositionKey);
                Quaternion l_targetRotation = PlayerData.GetQuaternion(Networking.LocalPlayer,PlayerRotationKey);
                if (l_targetPosition!= null && l_targetRotation != null)
                Networking.LocalPlayer.TeleportTo(l_targetPosition,l_targetRotation);
                else
                Debug.LogWarning("Player Spawn Manager: Teleport Debug : No Saved Location");
            }
            
        }
        #endregion

        #region SpawnToInstances

        public void SwitchSpawnToInstancesOn()
        {
            spawnToInstances = true;
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            RequestSerialization();
        }
        public void SwitchSpawnToInstancesOff()
        {
            spawnToInstances = false;
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            RequestSerialization();
        }
        public void TeleportToInstances()
        {
            teleportToInstances.GetComponent<UdonBehaviour>().SendCustomEvent("TeleportToTarget");
        }
        public void SwitchTargetPlayer()
        {
            teleportToInstances.GetComponent<UdonBehaviour>().SendCustomEvent("SwitchTargetPlayer");
        }
        public void SwitchTargetObject()
        {
            teleportToInstances.GetComponent<UdonBehaviour>().SendCustomEvent("SwitchTargetObject");
        }
        #endregion


    }
}


