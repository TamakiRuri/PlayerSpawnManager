# Player Spawn Manager

# プレイヤーのスポーン地点のホワイトリスト、保存システム

### 紹介

Player Spawn ManagerはVRChat ワールドでPersistenceを利用した、プレイヤーのスポーン地点のホワイトリスト、保存システムです。

ぶいすいのときにタイムアウト対策や、イベントワールドでスタッフだけ別の場所にスポーンさせるなどの場面で活躍できます。

このギミックの実装では、プレイヤーは初期のスポーン地点にスポーンしたあと、同期されたときに指定の場所にテレポートされます。（通常では、1秒間かかります）

Player Spawn Manager is a VRChat World Gimmick that teleport players to saved locations utilizing VRChat Persistence, with extra whitelist support.

It is designed so solve problems like Timeouts while sleeping (so you can get back directly to bed), and it can be used to set staffs' spawn to a different place than usual players.

What actually happen is the player is teleported to the default spawn, and then to the target position after a really short time.

### モード / Modes

#### ホワイトリストモード / Whitelist Mode

ホワイトリストにいるプレイヤー、もしくはインスタンスを作ったプレイヤー(Groupインスタンス以外)だけスポーン地点を違う場所に設定することができます。

Respawnや、Rejoinも対応しています。

Set the spawn of the players in the Whitelist (and/or instance owner) to a different location.

This also applys to Respawns and Rejoins.

#### 同期モード / Synced Mode

プレイヤーの場所を設定した時間間隔で保存し、次回ワールドに入るときに前回の場所にスポーンさせます。

同じインスタンスでのRejoinのみ動作します。

Save player's location every a few seconds, and if the player rejoins the instance, they will always appear at the place where the last time the script have auto-saved.

This mode only works on Rejoins at the same instance.

#### Persistenceモード / Persistence Mode

プレイヤーの場所を設定した時間間隔で保存し、次回ワールドに入るときに前回の場所にスポーンさせます。

同期モードと同じRejoinのみ想定していますが、同じワールドの別インスタンスにも動作します。

Save player's location every a few seconds, and if the player rejoins the instance, they will always appear at the place where the last time the script have auto-saved.

This mode works on Rejoins on all instances of the same world.

### その他設定 / Other Settings

#### ホワイトリストにいるプレイヤーに限定する / White Listed Only

定期保存機能がホワイトリストにいるプレイヤーしか使えないようになります。

This settings will limit the auto save feature to be only for whitelisted players.

#### ホワイトリストにいるプレイヤーを除く / White Listed Excluded

定期保存機能がホワイトリストにいるプレイヤーだけが使えないようになります。

この設定をオンにしない限り、Rejoinのスポーン地点が定期保存機能で上書きされます。(Respawn、(同期モードのみ)初回Joinは通常通り動作します)

This settings will make the auto save feature to be not available for whitelisted players.

If this settings isn't turned on, rejoined players will always spawn at their saved location.

Respawn and joining for the first time (only on synced mode) will still spawn at whitelisted-only spawn point.

#### 保存間隔 / Save Interval

データ保存の間隔です。

デフォルト：5秒

重くなるため、2秒以下はおすすめしません。

40人以上を想定するインスタンスでは5秒以上にしてください。

Time Interval for saving. Default: 5s.

Setting this to a value lower than 2s is not recommended.

For Instaces larger than 40 players, this value should be 5s or higher.

#### 保存制限 / Limit Save Count (Beta)

同期モードでは、同じインスタンスに入ったことがあるが、インスタンスにいないプレイヤーのデータも同期されます。

そのため、最大人数に制限を設けることで、同期によるラグを緩和できます。

Betaですが、特にパブリックなどで、インスタンスが長時間開く場合ではおすすめです。

ただし、設定値は必ずワールドのプレイヤー人数上限より大きくしてください。

また、200超えの設定はおすすめできません。

In synced mode, the datas of all players who aren't in the instance are also synced.

Setting a limit could prevent lag to an extent. Especially in public instances where the same instance could be on for a really long time.

Even though this is still in beta, it's recommended to be turned on.

This value should always be higher than the world player limit.

Also, setting this to over 200 is not recommended.

#### Debug Mode

デバッグ用のログが出力されます。ワールドにいたプレイヤーのユーザー名と場所が全員のログに保存されますので、ご了承ください。

毎回保存の時に出力されるため、一時間で1000行以上出力される場合があります。

By enabling this debug logs will be saved to all players' logs. This includes the locations and usernames of all the players who visited the instance.

This will be output every time the locations are saved. This could cause more than 1000 lines of log per hour.


### 注意事項 / Caution

複数設置できません。ほかのプレイヤーのスポーン地点（VRChat SDKのWorld Descripter除く）やリスポーン地点を変更するスクリプトと競合する恐れがあります。

There cannot be more than 1 of this script in the same scene. This can also cause issues if you use other scripts that changes player spawn / respawn locations (except World Descriptor, which is from VRChat SDK).


### 導入方法

Assets/StudioSaphir/PlayerSpawnManagerフォルダにあるプレハブをシーンにドラッグアンドドロップする

Drag and drop the prefab from StudioSaphir/PlayerSpawnManager folder

#### ホワイトリストモード / Whitelist Mode

プレイヤーのユーザー名を入力し、テレポート先に空のオブジェクトを設置したあと、このスクリプトのTarget Transformに入れると有効になります。

ホワイトリストのユーザー以外、インスタンスオーナー（インスタンスを作った人）にも設定できます。（グループインスタンスでは動作しません）

Input the username of the players, and place an empty object at a suitable location. After that, assign the object to Target Transform to enable this feature.

You can also allow instance owner (the person who created the instance) to be teleported like white listed users. (This does not work in group instances)

#### 同期・Persistenceモード / Synced / Persistence Mode

Mode で Syncedモードは同期モード、 PersistenceはPersistenceモードです。

Save Intervalは保存の時間間隔です。

In Modes, synced mode is Synced mode, and persistence mode is Persistence mode. 

Save Interval is how long it will take to before the next time the player's location is saved.