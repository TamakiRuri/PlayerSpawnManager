# Player Spawn Manager

# プレイヤーのスポーン地点を設定、管理するツール

### 紹介

Player Spawn ManagerはPersistencyを利用した、VRChatワールドにおいてプレイヤーのスポーン地点を変更するツールです。

ぶいすいのときにタイムアウト対策や、イベントワールドでスタッフだけ別の場所にスポーンさせるなどの場面で活躍できます。

Player Spawn Manager is a VRChat World Gimmick that manages player's spawn locations utilizing VRChat Persistency.

It is designed so solve problems like Timeouts while sleeping (so you can get back directly to bed), and it can be used to set staffs' spawn to a different place than usual players.

### モード / Modes

#### ホワイトリストモード / Whitelist Mode

ホワイトリストにいる人、もしくはインスタンスを作った人だけスポーン地点を違う場所に設定する*ことができます。

Respawnや、Rejoinも対応しています。

Set the spawn* of the players in the Whitelist (and/or instance owner) to a different location.

This also applys to Respawns and Rejoins.

#### Persistenceモード / Persistence Mode

プレイヤーの場所をある設定可能な時間間隔で保存し、次回ワールドに入るときに前回の場所にスポーンさせます*。

PersistenceモードはRejoinのみ想定しています。

Save player's location every a few seconds, and if the player rejoins the instance, they will always appear at the place where the last time the script have auto-saved*.

This mode only work on Rejoins.

#### エリアモード / Area Mode

Persistenceモードの拡張モードです。指定のエリアでしか保存しなくなります。

プレイヤーの場所はエリアにはいったすぐに一回保存され、あとは設定の時間間隔で保存されます。

An extension of Persistence mode. This will let the script only save when the player is in a certain area.

When Player get into an area, the position will be saved once, and the script will save the position every few seconds.

#### サポーター（有料）限定機能 / Supporter Only (Paid) Functions

サポーター版ではウェイポイントに特化した、Player Waypoint Managerを同封します。

Player Waypoint Managerはエリア機能が強化され、エリアを複数設置することや、ウェイポイントの番号管理がより簡単になっています。

ただし、関数の競合問題により、Player Waypoint ManagerとPlayer Spawn Managerを同時に使用できません。

For the supporter version, the waypoint specialized Player Waypoint Manager is included.

Player Waypoint Manager is specialized in waypoints. Placing multiple areas, and waypoint numbering management are easier in this version.

However, Player Waypoint Manager and Player Spawn Manager cannot be used at the same time due to function override issues.

### 注意事項 / Caution

プレイヤーの場所の保存間隔が短すぎると重くなる恐れがあります。

複数設置できません。ほかのプレイヤーのスポーン地点（VRChat SDKのWorld Descripter除く）やリスポーン地点を変更するスクリプトと競合する恐れがあります。

*実際には、一回デフォルトのスポーン地点にリスポーン、またはスポーンしたあと、ほぼ一瞬で指定した場所にテレポートします。

Do not set the time interval of saving to less than a few seconds, or this could cause lags.

There cannot be more than 1 of this script in the same scene. This can also cause issues if you use other scripts that changes player spawn / respawn locations (except World Descriptor, which is from VRChat SDK).

*What actually happen is the player is teleported to the default spawn, and then to the target position after a really short time.

### 導入方法

StudioSaphir/PlayerSpawnManagerフォルダにあるプレハブをシーンにドラッグアンドドロップする

Drag and drop the prefab from StudioSaphir/PlayerSpawnManager folder

#### ホワイトリストモード / Whitelist Mode

プレイヤーのユーザー名を入力し、テレポート先に空のオブジェクトを設置し、このスクリプトのTarget Transformに入れると有効になります。

ホワイトリストのユーザー以外、インスタンスオーナー（インスタンスを作った人）をも同じ動作するかも選択できます。

Input the username of the players, and place an empty object at a suitable location. After that, assign the object to Target Transform to enable this feature.

You can also allow instance owner (the person who created the instance) to be teleported like white listed users.

#### Persistenceモード / Persistence Mode

Save Player Positionをチェック入れると使えます。

Save Intervalはプレイヤーの場所の保存の時間間隔です。

Enable Save Player Position and then you're good to go.

Save Interval is how long it will take to before the next time the player's location is saved.

#### エリアモード / Area Mode

Persistenceモードを有効にしたあと、Save Only In Areaを有効にすると有効になります。

このスクリプトにアタッチしているTriggerが有効になっているコライダーを編集すれば、エリアの指定ができます。

なお、複数コライダーを同時につかっても問題なく動作します。ただし、仕様上、子オブジェクトのコライダーは動作しません。

Triggerを有効にしないとプレイヤーがエリアに入れないのでご注意ください。

Enabling Save Only In Area after enabling Persistence mode to enable this feature.

Then you can edit the collider with Trigger attached with this script to mark the area.

Having multiple colliders on the same object will also work. But all of them should be on this object. Having them on child objects will not work.

If you don't have Trigge on, the colliders may prevent the player from getting into the designed area.