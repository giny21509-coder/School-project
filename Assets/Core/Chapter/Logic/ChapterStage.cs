namespace JianQi.Chapters
{
    /// <summary>
    /// 七章共用的章節階段，順序即流程順序（只能往前）。
    /// 對應企畫書流程：接受委託 → 現場調查 → 願者顯形 → 內心世界 → 最終互動 → 釋懷 → 完成。
    /// </summary>
    public enum ChapterStage
    {
        Commission = 0,     // S0 接受委託（事務所）
        Investigation = 1,  // S1 現場調查
        Reveal = 2,         // S2 相機顯形
        InnerWorld = 3,     // S3 內心世界
        Climax = 4,         // S4 最終互動（第六章：最終傾聽）
        Release = 5,        // S5 釋懷
        Complete = 6,       // S6 回到現實／委託完成
    }
}
