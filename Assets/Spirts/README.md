# Inventory Detail UI Framework

這組腳本用來在背包系統中加入「查看物件資訊」功能。

玩家點擊背包中的物件格子後，物件名稱、用途說明、詳細資訊和圖片會顯示在背包 UI 的資訊欄內。

## 腳本

- `InventoryItemDetailPanel.cs`：掛在背包資訊欄物件上，負責更新文字和圖片。
- `InventoryItemClickDetail.cs`：掛在每個背包物件格子或按鈕上，負責在點擊後把該物件資料送到資訊欄。

## Unity 設定

1. 把 `InventoryDetailUIFramework` 資料夾放進 Unity 專案的 `Assets` 內。
2. 在背包 UI 裡建立一個資訊欄物件，例如 `InventoryInfoPanel`。
3. 在資訊欄內準備這些 UI：
   - 物品名稱：`TextMeshProUGUI`
   - 用途說明：`TextMeshProUGUI`
   - 物件資訊：`TextMeshProUGUI`
   - 物品圖片：`Image`
   - 關閉按鈕：`Button`，可選
4. 將 `InventoryItemDetailPanel.cs` 掛到 `InventoryInfoPanel`。
5. 在 Inspector 指定：
   - `Panel Root`：資訊欄根物件，通常就是 `InventoryInfoPanel`
   - `Item Name Text`：物品名稱文字
   - `Use Description Text`：用途說明文字
   - `Item Info Text`：物件資訊文字
   - `Item Photo Image`：物品圖片
   - `Close Button`：關閉按鈕，可不填
6. 在每個背包格子或格子內的 Button 上掛 `InventoryItemClickDetail.cs`。
7. 在每個 `InventoryItemClickDetail` 指定同一個 `Detail Panel`，也就是剛剛的 `InventoryInfoPanel`。
8. 物件資料可以用兩種方式填：
   - 直接在 `Manual Item Detail` 填名稱、用途、物件資訊、圖片。
   - 指定 `Item Data Object`，腳本會自動讀取常見欄位。

## 可自動讀取的 ItemData 欄位名稱

如果你的 `ItemData` 是 ScriptableObject 或其他 Unity Object，欄位名稱符合下列其中一種就會自動顯示：

- 名稱：`itemName`、`displayName`、`title`、`name`
- 用途：`useDescription`、`itemUseDescription`、`itemDescription`、`description`、`useText`
- 資訊：`itemInfo`、`itemInformation`、`information`、`info`、`detail`、`detailText`、`flavorText`、`storyText`、`clueText`
- 圖片：`itemPhoto`、`previewSprite`、`itemImage`、`itemIcon`、`icon`、`sprite`

## 從背包程式設定資料

如果你的背包格子是用程式生成，可以在生成格子時設定：

```csharp
InventoryItemClickDetail clickDetail = slotObject.GetComponent<InventoryItemClickDetail>();
clickDetail.SetManualDetail(
    item.itemName,
    item.useDescription,
    item.itemInfo,
    item.icon
);
```

如果你的 `ItemData` 是 ScriptableObject：

```csharp
InventoryItemClickDetail clickDetail = slotObject.GetComponent<InventoryItemClickDetail>();
clickDetail.SetItemDataObject(item);
```

這樣玩家點擊背包格子時，資訊欄就會顯示該物件的資料。
