using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Spawner;
using SuperFantasyKingdom.UI;
using UnityEngine;

namespace DailyCaravan
{
    [BepInPlugin("sfk.dailycaravan", "Daily Caravan", "0.1.0")]
    public sealed class DailyCaravanPlugin : BaseUnityPlugin
    {
        internal static DailyCaravanPlugin Instance;
        private ConfigEntry<int> firstDay;
        private Harmony patches;
        private GameDataManager scene;
        private int day;
        private bool rewardPresent;
        private string dawnHash;
        private string directory;
        private string recordPath;
        internal Visit Visit;
        internal Guest LastGuest;
        private Arrival arrival = new Arrival();
        internal bool Launching { get; private set; }

        private void Awake()
        {
            Instance = this;
            firstDay = Config.Bind("Schedule", "FirstDay", 16, new ConfigDescription(
                "First ordinary kingdom day for supplemental caravans. Native unlocks and challenge schedules still apply.",
                new AcceptableValueRange<int>(1, 999)));
            patches = new Harmony("sfk.dailycaravan");
            try
            {
                patches.PatchAll(typeof(DailyCaravanPlugin).Assembly);
                DaytimeManager.OnMorningStart += Morning;
                Logger.LogInfo("Daily Caravan 0.1.0 ready.");
            }
            catch
            {
                patches.UnpatchSelf();
                throw;
            }
        }

        private void OnDestroy()
        {
            DaytimeManager.OnMorningStart -= Morning;
            patches?.UnpatchSelf();
            Instance = null;
        }

        internal void BeginLoad(GameData data)
        {
            LastGuest = null;
            scene = null;
            Visit = null;
            recordPath = null;
            arrival = new Arrival();
            dawnHash = Fingerprint(data);
        }

        // The fingerprint binds a replay to the actual save/backup the game loaded.
        internal static string Fingerprint(GameData data) =>
            data == null ? "new" : Helper.GenerateHash(JsonUtility.ToJson(data));

        internal void RecordGrant(string card, int added, string singleDrop)
        {
            if (!Visit.RecordDrops(card, added, singleDrop, CaravanPatches.HasGrantHooks())) return;
            rewardPresent = true;
            Persist();
        }

        // Providers call this synchronously for a native caravan card with a non-drop reward.
        public static bool TryGrantProviderReward(UICardItem card, Func<bool> grant)
        {
            var plugin = Instance;
            if (plugin == null || !CaravanPatches.CanGrant(card, out string id) ||
                !plugin.Visit.GrantProviderReward(id, grant)) return false;
            plugin.rewardPresent = true;
            plugin.Persist();
            return true;
        }

        internal void GrantFaithBonus(DroppedShardSpawner spawner, Vector3 position, int amount, ResourceType resource, bool onScreen)
        {
            if (amount <= 0) return;
            if (Visit.bonus == null || Visit.bonus.amount <= 0)
                Visit.bonus = new FaithBonus { amount = amount, resource = (int)resource,
                    x = position.x, y = position.y, z = position.z, onScreen = onScreen };
            DeliverBonus(spawner);
        }

        private void DeliverBonus(DroppedShardSpawner spawner)
        {
            if (Visit.bonus.Deliver(b => spawner.Spawn(new Vector3(b.x, b.y, b.z), b.amount, (ResourceType)b.resource, b.onScreen)))
                Persist();
        }

        private bool EnterDay()
        {
            var manager = GameDataManager.Instance;
            if (manager == null || DaytimeManager.Instance == null || string.IsNullOrEmpty(manager.GetRunID())) return false;
            int today = DaytimeManager.Instance.CurrentDay();
            if (manager == scene && day == today && Visit != null) return true;

            scene = manager;
            day = today;
            rewardPresent = false;
            arrival = new Arrival();
            directory = Path.Combine(Paths.ConfigPath, "DailyCaravan", SettingsManager.Instance.GetProfile().ToString(),
                Helper.GenerateHash(manager.GetRunID()), day.ToString());
            recordPath = Path.Combine(directory, (dawnHash ?? "new") + ".json");
            Visit = new Visit();
            try
            {
                if (File.Exists(recordPath))
                {
                    var saved = JsonUtility.FromJson<Visit>(File.ReadAllText(recordPath));
                    if (saved != null && saved.kingdom >= 0)
                    {
                        saved.items = null;
                        Visit = saved;
                        rewardPresent = saved.rewardInSave;
                        if (saved.ResumeWithoutBooking) arrival.Request(Visit);
                    }
                }
            }
            catch (Exception e) { Logger.LogWarning("Could not read caravan record: " + e.Message); }
            return true;
        }

        internal bool Request(VisitOrigin origin, Races? invitation = null)
        {
            if (!EnterDay()) return false;
            if (!arrival.Dispatched)
            {
                arrival.Request(Visit, (int?)invitation, origin);
                if (origin == VisitOrigin.Invitation) Persist();
            }
            return true;
        }

        private void Morning(int today)
        {
            if (!EnterDay()) return;
            if (DailyCaravan.Visit.AddsVisit(today, firstDay.Value,
                    RaceDataManager.Instance.HasRaceEventFlag(RaceEvent.GateOpened),
                    WorldManager.Instance.HasUnlockedCaravans(), MainManager.Instance.IsChallenge()))
                Request(VisitOrigin.Daily);
        }

        private void Update()
        {
            bool restoreBonus = Visit?.bonus?.NeedsRestore == true;
            if ((!arrival.Pending && !restoreBonus) || scene == null || scene != GameDataManager.Instance || GameManager.Instance == null ||
                !GameManager.Instance.IsGameRunning() || GameManager.Instance.IsUnloading() ||
                GameManager.Instance.IsGameOver() || day != DaytimeManager.Instance.CurrentDay()) return;

            var world = WorldManager.Instance;
            var overlay = OverlayManager.Instance;
            bool busy = world == null || world.GetCaravan() == null || world.GetGuest() != null ||
                (LastGuest != null && LastGuest.gameObject.activeInHierarchy) || overlay == null ||
                overlay.IsOpen() || overlay.HasMinimizedOverlay() || GameManager.Instance.IsForcedPause() ||
                GameCamera.Instance.IsCutscene() || DialogueManager.Instance.IsInDialogue() ||
                UIManager.Instance == null || !UIManager.Instance.IsWorldInteractable();

            if (!busy && restoreBonus) DeliverBonus(DroppedShardSpawner.Instance);
            ArrivalAction action = arrival.Take(Visit, busy);
            if (action == ArrivalAction.Wait || action == ArrivalAction.Complete) return;
            if (action == ArrivalAction.RestoreItem)
            {
                // An older dawn can restore a plain drop. A provider-owned grant needs its picker.
                if (!Visit.providerGrant && !CaravanPatches.HasGrantHooks() &&
                    AddressablesManager.Instance.GetItem(Visit.selected) != null)
                {
                    var drops = ItemManager.Instance.GetDragItems();
                    int before = drops.Count;
                    ItemManager.Instance.SpawnDragItem(Visit.selected, new Vector2(0f, 3f));
                    rewardPresent = drops.Count == before + 1;
                    return;
                }
                Visit.selected = null;
                Visit.providerGrant = false;
            }
            if (Visit.kingdom < 0)
                Visit.kingdom = DailyCaravan.Visit.VisitingKingdom(GameManager.Instance.GetSeed(), day, (int)RaceManager.Instance.GetRace());
            Persist();
            Launching = true;
            try { world.GetCaravan().TriggerManually((Races)Visit.kingdom); }
            finally { Launching = false; }
        }

        internal bool Owns(GuestCaravan guest) =>
            arrival.Dispatched && scene != null && scene == GameDataManager.Instance && day == DaytimeManager.Instance.CurrentDay() && WorldManager.Instance != null &&
            guest != null && guest == WorldManager.Instance.GetCaravan();

        internal void Persist() => WriteRecord(recordPath, Visit);

        // A native snapshot which already includes the reward must never recreate it on load.
        internal void Snapshot(GameData data)
        {
            if (Visit == null || Visit.kingdom < 0 || scene == null || scene != GameDataManager.Instance ||
                data.runID != scene.GetRunID() || data.day + 1 != day) return;
            var snapshot = JsonUtility.FromJson<Visit>(JsonUtility.ToJson(Visit));
            snapshot.rewardInSave = rewardPresent;
            if (snapshot.bonus != null) snapshot.bonus.inSave = Visit.bonus?.Present == true;
            WriteRecord(Path.Combine(directory, Fingerprint(data) + ".json"), snapshot);
        }

        private void WriteRecord(string path, Visit visit)
        {
            if (path == null || visit == null) return;
            try
            {
                Directory.CreateDirectory(directory);
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(visit));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (Exception e) { Logger.LogWarning("Could not save caravan record: " + e.Message); }
        }
    }
}
