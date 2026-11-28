# 第六章「暴食」：程式骨架與場景接線

> 配合 `Docs/Ch06_Gluttony_Design.md`。這份說明程式放在哪裡，以及在場景裡怎麼接線。

## 檔案

```
Assets/Core/Chapter/                      七章共用（Assembly-CSharp）
  ChapterDefinition / MemoryFragmentDefinition / DraftMessageDefinition   ScriptableObject
  ChapterProgress    進度 key：ch{n}/...（寫入 GameProgressState，存檔沿用 SaveManager）
  ChapterRunner      每個章節場景一個：階段 → 啟用物件群組；Fungus 用 Call Method 呼叫 AdvanceTo(int)
  Logic/  (JianQi.Chapter.Logic.asmdef，純 C#)  ChapterStage、ChapterStateMachine

Assets/Chapters/Ch06_Gluttony/
  Logic/  (JianQi.Ch06.Logic.asmdef，純 C#)     EmptinessModel、ListenModel、ShadeBossModel
  Scripts/                                     MonoBehaviour（見下表）

Assets/Tests/EditMode/Ch06/  (JianQi.Ch06.Tests.asmdef)   NUnit 測試 22 個
```

## 對既有程式的修改（都只是新增掛勾，預設行為不變）

| 檔案 | 新增 | 用途 |
|---|---|---|
| `Spirts/PlayerController.cs` | `speedMultiplier`（預設 1）、`AddExternalVelocity(Vector2)` | 失控減速、失控與嘴的吸力 |
| `Spirts/CameraShootingEncounter.cs` | `HitGate`、`FreezeMovement` | S2 只有停頓時拍攝才算命中 |
| `Spirts/ScreenEdgeFog.cs` | `SetProgress(float)` | 由空虛值控制黑霧 |

## 資料（Project 視窗右鍵 → Create → JianQi/...）

1. `Ch06`（Chapter Definition）：`chapterNumber = 6`，放入 4 個 Fragment、3 個 Draft，以及執念物、紀念品、相冊的 ItemData。
2. `Fragment_1..4`：`order` 填 1–4，`cgItem` 指向 CG 的 ItemData，`echoLines` 填 3 句。
3. `Draft_D1..D3`：`draftId` 填 D1–D3；`unlockedByClueId` 分別填 C2、C3、C5。
4. `GluttonyConfig`：數值先用預設值，試玩後再調整。

## 場景接線

**Ch06_Apartment（S1 調查、S2 拍攝）**
- `ChapterRunner`：`sceneEntryStage = Investigation`。
- 每條線索掛 `ClueInspectable`（Collider2D 勾 Is Trigger，填 `clueId` 與 Fungus block）。
- `InvestigationTracker`：放入 5 條線索，`requiredClues = 3`，`onThresholdReached` → 打開浴室門。
- `PauseWindowEncounter`：`CameraShootingEncounter.onEncounterStarted` → `Begin()`、`onEncounterCompleted` → `End()`；`onPauseStarted` / `onEatingStarted` 接抬頭動畫與雜訊淡入淡出。

**Ch06_InnerWorld（S3 內心世界、S4 最終戰）**
- 一個空物件放 `EmptinessMeter`（指定 GluttonyConfig、ScreenEdgeFog、咀嚼聲 AudioSource、空碗 Image）。
- 玩家：另外加上 `PlayerListenAbility`、`BingeController`、`GluttonyFoodPouch`（Health 用 TopDownEngine 的）。
- 每個 Zone：一個 `ZoneGate`（入口 Trigger、該 Zone 的回音、`FillerSpawner`、出口黑霧牆、`drifting`：A 不勾，B、C 勾）。
- 回音：`EchoListenTarget`（指定 Ch06 與 Fragment），`onLineShown` 接字幕 UI。
- 填充怪 Prefab：TopDownEngine 的 Health 加 DamageOnTouch；雜訊怪另外加 `NoiseFillerAI`；嘴加 `MawHazard`；空盒的 Health 死亡時生成 `FoodPickup`。
- 最終戰：`GluttonyShadeBoss`（指定 Ch06、ChapterRunner、EmptinessMeter、`DraftCardMenu`）；`onAttackActive` 開關三種招式；食物雨生成的 `FoodPickup` 勾 `eatOnTouch`。
- 死亡重試：PlayerDeathUI 重試時呼叫目前 Zone 的 `ZoneGate.RestartZone()`，或 `GluttonyShadeBoss.RestartRound()`。

## 按鍵（暫定，見 `Ch06Input.cs`）
傾聽 Q／LB（按住）・互動、吃 R／X・吃袋子裡的食物 H。之後改成 Input System actions（設計文件 §9.6）。

## 還沒做的（骨架之外）
- 最終戰三種招式本體（吞嚥、食物雨、污漬區）、填充怪的移動 AI（雜訊怪以外）。
- 回音字幕 UI、空碗 UI、草稿匣（手機）UI、內容警示畫面。
- `ZoneGate.RestartZone` 與既有 `PlayerDeathUI` 的串接。
- 兩個場景本身與所有美術、音效。

## 執行測試
Unity：Window → General → Test Runner → EditMode → Run All。
命令列：
```
Unity.exe -batchmode -projectPath <Schoolproject> -runTests -testPlatform EditMode -assemblyNames JianQi.Ch06.Tests -testResults results.xml
```
