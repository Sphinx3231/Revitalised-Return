/// <summary>
/// Quest FSM (charter Step 12) -- 5 states, `Failed` approved above the roadmap's original
/// 4-state ask (needed for missable NPC threads and the eventual Soren branch). Explicit int
/// values are load-bearing: this enum is pinned to the ints already stored in
/// PlayerData.questStates / persisted via PlayerSaveDto.QuestStateEntry.state, so reordering
/// these members would silently corrupt existing save data.
/// </summary>
public enum QuestState
{
    Unstarted = 0,
    Active = 1,
    ObjectiveComplete = 2,
    Completed = 3,
    Failed = 4,
}
