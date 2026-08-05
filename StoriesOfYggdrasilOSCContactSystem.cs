#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Networking;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;

namespace StoriesOfYggdrasil.OSC
{
    /// <summary>
    /// Stories Of Yggdrasil OSC Contact System.
    ///
    /// Safety rule: this tool NEVER edits or replaces an existing health Animator system.
    /// It audits the selected FX controller, creates compatible Contact Senders/Receivers,
    /// and can add the missing Stories Of Yggdrasil OSC bridge parameters, spell menus,
    /// one-second incoming-hit I-Frames, status gauges, hook layers, background update checks, and managed-system repair tools.
    /// </summary>
    public sealed class StoriesOfYggdrasilOSCContactSystem : EditorWindow
    {
        private const string Version = "0.5.9";
        private const string BuildNumber = "TB3";
        private const string BuildLabel = "Test Build 3 — Spell Bus Audit Classification Fix";
        private const string SenderTypeName = "VRC.SDK3.Dynamics.Contact.Components.VRCContactSender";
        private const string ReceiverTypeName = "VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver";
        private static readonly string[] RaycastTypeNames =
        {
            "VRC.SDK3.Dynamics.Raycast.Components.VRCRaycast",
            "VRC.SDK3.Dynamics.Raycast.VRCRaycast",
            "VRC.SDK3.Avatars.Components.VRCRaycast"
        };
        private static readonly string[] ParentConstraintTypeNames =
        {
            "VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint",
            "VRC.SDK3.Dynamics.Constraint.VRCParentConstraint",
            "VRC.SDK3.Avatars.Components.VRCParentConstraint"
        };

        private const string TagWeak = "Hit By Weak Attack";
        private const string TagAverage = "Hit By Average Attack";
        private const string TagStrong = "Hit By Strong Attack";
        private const string TagCritical = "Hit By Critical Attack";
        private const string TagBlockable = "Blockable";
        private const string TagHitBlocked = "Hit Blocked";

        private static readonly string[] DebuffTags = { "Burn", "Silence", "Freeze", "Bind", "Bleed" };

        private enum StudioTab
        {
            Setup,
            Contacts,
            AnimatorSetup,
            MenuBuilder,
            Tools,
            Help,

            // Legacy internal destinations retained so older navigation calls
            // remain harmless while the visible UI stays compact.
            OutgoingContacts,
            IncomingReceivers,
            Diagnostics,
            Accessibility,
            Backups
        }

        private enum ContactsPage
        {
            Outgoing,
            Incoming
        }

        private enum ToolsPage
        {
            Status,
            Repair,
            Display,
            Backups
        }

        private enum ManagedRepairState
        {
            Healthy,
            Outdated,
            Repairable,
            Broken,
            Defunct,
            Foreign
        }

        private enum ManagedRepairKind
        {
            None,
            LegacySpellReceiver,
            AttackTagContract,
            IncomingReceiverMapping,
            DuplicateManagedContact,
            MissingManagedComponent
        }

        private enum DeliveryMode
        {
            Contact,
            Raycast
        }

        private enum ContactAttachmentMode
        {
            ContactObject,
            WeaponRootTransform,
            VRCParentConstraint
        }

        private enum AnimationPage
        {
            Health,
            Spells,
            Technicks,
            Items,
            Setup
        }

        private enum WizardStep
        {
            Avatar,
            Audit,
            SafeFx,
            Parameters,
            Menus,
            Animations,
            Contacts,
            Validate
        }

        private enum ActionAnimationKind
        {
            Spell,
            Technick,
            Item
        }

        private enum ContactPreset
        {
            Custom,
            SwordBlade,
            ShieldFace,
            HandSpell,
            HealingOrb,
            BodyReceiver,
            Aura,
            LargeAoe,
            RaycastImpact
        }

        private enum RaycastCollisionTarget
        {
            Players,
            WorldsAndPlayers
        }

        private enum UpdateChannel
        {
            Stable,
            TestBuilds
        }

        private enum TextScaleMode
        {
            Normal,
            Large,
            ExtraLarge
        }

        private enum ColorVisionMode
        {
            Standard,
            HighContrast,
            ProtanopiaFriendly,
            DeuteranopiaFriendly,
            TritanopiaFriendly,
            Monochrome
        }

        private enum MenuNavigationMode
        {
            Combined,
            SchoolFirst,
            PurposeFirst,
            FavoritesFirst,
            CompactCombat
        }

        private enum OutgoingContactKind
        {
            Attack,
            Spell,
            Technick,
            Item,
            Blocking,
            Debuff
        }

        private enum ContactDraftKind
        {
            None,
            Attack,
            Spell,
            Technick,
            Item,
            Blocking,
            Debuff,
            Incoming
        }

        private enum SpellSchool
        {
            WhiteMagick,
            BlackMagick,
            GreenMagick,
            TimeMagick,
            ArcaneMagick,
            SynergistMagick,
            IllusionMagick,
            DreamMagick,
            NatureMagick,
            ChaosMagick,
            AbyssalCurses,
            YggdrasilLightMagick
        }

        private enum SpellCategory
        {
            Offensive,
            Healing,
            Revival,
            Cleanse,
            Support,
            Status,
            Utility
        }

        private enum AttackTier
        {
            Weak,
            Average,
            Strong,
            Critical
        }

        private enum ContactShape
        {
            Sphere,
            Capsule,
            Box
        }

        private enum HealthSystemKind
        {
            None,
            Generic,
            Compatible
        }

        private const string CombatLayer = "Stories Of Yggdrasil | OSC Combat Gate";
        private const string VitalLayer = "Stories Of Yggdrasil | OSC Vital State";
        private const string ReactionLayer = "Stories Of Yggdrasil | OSC Reaction Router";
        private const string DiablosLayer = "Stories Of Yggdrasil | Curse Of Diablos Warnings";
        private const string IFrameLayer = "Stories Of Yggdrasil | Incoming Hit I-Frames";
        private const string SpellAlignmentLayer = "Stories Of Yggdrasil | Spell Alignment";
        private const string SpellCastLayer = "Stories Of Yggdrasil | Spell Cast Animations";
        private const string TechnickCastLayer = "Stories Of Yggdrasil | Technick Animations";
        private const string ItemUseLayer = "Stories Of Yggdrasil | Item Use Animations";
        private const string RaycastLayerPrefix = "Stories Of Yggdrasil | Raycast Gate | ";
        private const string RaycastFireParameter = "SoY_RaycastFire";
        private const string FxCopyRoot = "Assets/Stories Of Yggdrasil/FX";
        private const string MenuRoot = "Assets/Stories Of Yggdrasil/Menus";
        private const string AnimationRoot = "Assets/Stories Of Yggdrasil/Animations";
        private const string ProfileRoot = "Assets/Stories Of Yggdrasil/Profiles";
        private const string BackupRoot = "Assets/Stories Of Yggdrasil/Backups/Unity Tool";
        private const string ManifestRoot = "Assets/Stories Of Yggdrasil/Backups/Manifests";
        private const string RepairSnapshotRoot = "Assets/Stories Of Yggdrasil/Backups/Migrations";
        private const string RaycastRootName = "Stories Raycast Results";
        private const string PreviewPrefix = "[TEMP] Stories Contact Preview";
        // Compact spell contact transport.
        // Older VRChat SDKs force Constant receivers to write only 1, so spell IDs
        // are transmitted as an eight-bit contact bus and reconstructed by the Desktop app.
        private const string LegacySpellTagPrefix = "SoY Spell ";
        private const string SpellActiveTag = "SoY Spell Active";
        private const string SpellBitTagPrefix = "SoY Spell Bit ";
        private const string SpellActiveParameter = "SoY_SpellActive";
        private const string SpellBitParameterPrefix = "SoY_SpellBit";
        private const int SpellBitCount = 8;
        private const string CasterAllyTag = "SoY Caster Ally";
        private const string CasterEnemyTag = "SoY Caster Enemy";
        private const string DamageSourceEnemyParameter = "SoY_DamageSourceEnemy";
        private const string TechnickActiveTag = "SoY Technick Active";
        private const string TechnickBitTagPrefix = "SoY Technick Bit ";
        private const string TechnickActiveParameter = "SoY_TechnickActive";
        private const string TechnickBitParameterPrefix = "SoY_TechnickBit";
        private const string ItemActiveTag = "SoY Item Active";
        private const string ItemBitTagPrefix = "SoY Item Bit ";
        private const string ItemActiveParameter = "SoY_ItemActive";
        private const string ItemBitParameterPrefix = "SoY_ItemBit";
        private const int ActionBitCount = 8;
        private const string GitHubRepository = "StarhunterUC/Stories-OSC-Unity-Tool";
        private const string GitHubLatestReleaseApi = "https://api.github.com/repos/StarhunterUC/Stories-OSC-Unity-Tool/releases/latest";
        private const string GitHubRepositoryUrl = "https://github.com/StarhunterUC/Stories-OSC-Unity-Tool";
        private const double BackgroundUpdateIntervalSeconds = 6d * 60d * 60d;
        private const float HitIFrameSeconds = 1f;

        private struct ParameterSpec
        {
            public string Name;
            public AnimatorControllerParameterType AnimatorType;
            public VRCExpressionParameters.ValueType ExpressionType;
            public float DefaultValue;
            public bool Saved;
            public bool NetworkSynced;

            public ParameterSpec(
                string name,
                AnimatorControllerParameterType animatorType,
                VRCExpressionParameters.ValueType expressionType,
                float defaultValue = 0f,
                bool saved = false,
                bool networkSynced = false)
            {
                Name = name;
                AnimatorType = animatorType;
                ExpressionType = expressionType;
                DefaultValue = defaultValue;
                Saved = saved;
                NetworkSynced = networkSynced;
            }
        }

        private struct SpellDefinition
        {
            public int Id;
            public string Name;
            public SpellSchool School;
            public SpellCategory Category;

            public bool IsHealing
            {
                get { return Category == SpellCategory.Healing || Category == SpellCategory.Revival; }
            }

            public SpellDefinition(int id, string name, SpellSchool school, SpellCategory category)
            {
                Id = id;
                Name = name;
                School = school;
                Category = category;
            }
        }

        private struct ActionDefinition
        {
            public int Id;
            public string Name;
            public string Description;

            public ActionDefinition(int id, string name, string description)
            {
                Id = id;
                Name = name;
                Description = description;
            }
        }

        private struct ReceiverMapping
        {
            public string Tag;
            public string Parameter;
            public float Value;
            public string ReceiverType;

            public ReceiverMapping(string tag, string parameter, float value = 1f, string receiverType = "Constant")
            {
                Tag = tag;
                Parameter = parameter;
                Value = value;
                ReceiverType = receiverType;
            }
        }

        [Serializable]
        private sealed class GitHubReleaseAsset
        {
            public string name;
            public string browser_download_url;
        }

        [Serializable]
        private sealed class GitHubReleaseInfo
        {
            public string tag_name;
            public string name;
            public string body;
            public string html_url;
            public bool draft;
            public bool prerelease;
            public GitHubReleaseAsset[] assets;
        }

        [Serializable]
        private sealed class GitHubReleaseList
        {
            public GitHubReleaseInfo[] items;
        }


        private static readonly SpellDefinition[] SpellDefinitions =
        {
            // White Magick
            new SpellDefinition(1, "Cure", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(2, "Cura", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(3, "Curaga", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(4, "Curaja", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(5, "Raise", SpellSchool.WhiteMagick, SpellCategory.Revival),
            new SpellDefinition(6, "Arise", SpellSchool.WhiteMagick, SpellCategory.Revival),
            new SpellDefinition(7, "Renew", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(8, "Regen", SpellSchool.WhiteMagick, SpellCategory.Healing),
            new SpellDefinition(9, "Poisona", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(10, "Blindna", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(11, "Vox", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(12, "Stona", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(13, "Esuna", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(14, "Esunaga", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(15, "Cleanse", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(16, "Protect", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(17, "Protectga", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(18, "Shell", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(19, "Shellga", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(20, "Dispel", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(21, "Dispelga", SpellSchool.WhiteMagick, SpellCategory.Cleanse),
            new SpellDefinition(22, "Bravery", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(23, "Faith", SpellSchool.WhiteMagick, SpellCategory.Support),
            new SpellDefinition(24, "Holy", SpellSchool.WhiteMagick, SpellCategory.Offensive),
            new SpellDefinition(25, "Confuse", SpellSchool.WhiteMagick, SpellCategory.Status),
            new SpellDefinition(100, "I Am... Recovery Atomic", SpellSchool.WhiteMagick, SpellCategory.Healing),

            // Green Magick
            new SpellDefinition(26, "Decoy", SpellSchool.GreenMagick, SpellCategory.Support),
            new SpellDefinition(27, "Oil", SpellSchool.GreenMagick, SpellCategory.Status),
            new SpellDefinition(28, "Reverse", SpellSchool.GreenMagick, SpellCategory.Support),
            new SpellDefinition(29, "Drain", SpellSchool.GreenMagick, SpellCategory.Offensive),
            new SpellDefinition(30, "Bubble", SpellSchool.GreenMagick, SpellCategory.Support),
            new SpellDefinition(31, "Syphon", SpellSchool.GreenMagick, SpellCategory.Offensive),
            new SpellDefinition(32, "Disablega", SpellSchool.GreenMagick, SpellCategory.Status),
            new SpellDefinition(122, "Sleep", SpellSchool.GreenMagick, SpellCategory.Status),

            // Time Magick
            new SpellDefinition(33, "Slow", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(34, "Immobilize", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(35, "Reflect", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(36, "Disable", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(37, "Vanish", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(38, "Balance", SpellSchool.TimeMagick, SpellCategory.Offensive),
            new SpellDefinition(39, "Gravity", SpellSchool.TimeMagick, SpellCategory.Offensive),
            new SpellDefinition(40, "Haste", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(41, "Stop", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(42, "Bleed", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(43, "Break", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(44, "Countdown", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(45, "Float", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(46, "Berserk", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(47, "Vanishga", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(48, "Warp", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(49, "Reflectga", SpellSchool.TimeMagick, SpellCategory.Support),
            new SpellDefinition(50, "Slowga", SpellSchool.TimeMagick, SpellCategory.Status),
            new SpellDefinition(51, "Graviga", SpellSchool.TimeMagick, SpellCategory.Offensive),
            new SpellDefinition(52, "Hastega", SpellSchool.TimeMagick, SpellCategory.Support),

            // Synergist Magick
            new SpellDefinition(53, "Boon", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(54, "Veil", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(55, "Vigilance", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(40, "Haste", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(56, "Barfire", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(57, "Barfrost", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(58, "Barthunder", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(59, "Barwater", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(22, "Bravery", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(23, "Faith", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(60, "Enfire", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(61, "Enfrost", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(62, "Enthunder", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(63, "Enwater", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(16, "Protect", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(18, "Shell", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(64, "Protectra", SpellSchool.SynergistMagick, SpellCategory.Support),
            new SpellDefinition(65, "Shellra", SpellSchool.SynergistMagick, SpellCategory.Support),

            // Illusion Magick
            new SpellDefinition(66, "Mindmaze", SpellSchool.IllusionMagick, SpellCategory.Status),
            new SpellDefinition(67, "Veil of the Unseen", SpellSchool.IllusionMagick, SpellCategory.Support),
            new SpellDefinition(68, "Mirror Walk", SpellSchool.IllusionMagick, SpellCategory.Support),
            new SpellDefinition(69, "Flicker", SpellSchool.IllusionMagick, SpellCategory.Support),
            new SpellDefinition(70, "Doppelgeist", SpellSchool.IllusionMagick, SpellCategory.Offensive),
            new SpellDefinition(71, "Glamour Veil", SpellSchool.IllusionMagick, SpellCategory.Status),
            new SpellDefinition(72, "False Terrain", SpellSchool.IllusionMagick, SpellCategory.Status),
            new SpellDefinition(73, "Unmake", SpellSchool.IllusionMagick, SpellCategory.Offensive),
            new SpellDefinition(74, "Phantom Army", SpellSchool.IllusionMagick, SpellCategory.Offensive),
            new SpellDefinition(75, "Phantom Atomic", SpellSchool.IllusionMagick, SpellCategory.Offensive),

            // Arcane Magick
            new SpellDefinition(76, "Dark", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(77, "Darka", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(78, "Darkra", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(79, "Darkga", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(80, "Death", SpellSchool.ArcaneMagick, SpellCategory.Status),
            new SpellDefinition(81, "Ardor", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(82, "Soul Rend", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(83, "Ex Nihilo", SpellSchool.ArcaneMagick, SpellCategory.Offensive),
            new SpellDefinition(84, "Atomic", SpellSchool.ArcaneMagick, SpellCategory.Offensive),

            // Chaos Magick
            new SpellDefinition(85, "Chaos Lance", SpellSchool.ChaosMagick, SpellCategory.Offensive),
            new SpellDefinition(86, "Fracture", SpellSchool.ChaosMagick, SpellCategory.Offensive),
            new SpellDefinition(87, "Chaos Imbuement", SpellSchool.ChaosMagick, SpellCategory.Support),
            new SpellDefinition(88, "Cataclysm", SpellSchool.ChaosMagick, SpellCategory.Offensive),
            new SpellDefinition(89, "Balefire", SpellSchool.ChaosMagick, SpellCategory.Offensive),
            new SpellDefinition(90, "Event Horizon", SpellSchool.ChaosMagick, SpellCategory.Offensive),
            new SpellDefinition(91, "Chaos Atomic", SpellSchool.ChaosMagick, SpellCategory.Offensive),

            // Abyssal Curses
            new SpellDefinition(92, "Madness", SpellSchool.AbyssalCurses, SpellCategory.Status),
            new SpellDefinition(93, "Wither", SpellSchool.AbyssalCurses, SpellCategory.Status),
            new SpellDefinition(94, "Silence of Thought", SpellSchool.AbyssalCurses, SpellCategory.Status),
            new SpellDefinition(95, "Null Pulse", SpellSchool.AbyssalCurses, SpellCategory.Offensive),
            new SpellDefinition(96, "Worldrend", SpellSchool.AbyssalCurses, SpellCategory.Offensive),
            new SpellDefinition(97, "Memory Bleed", SpellSchool.AbyssalCurses, SpellCategory.Status),

            // Yggdrasil Light Magick
            new SpellDefinition(98, "Chakra Heal", SpellSchool.YggdrasilLightMagick, SpellCategory.Healing),
            new SpellDefinition(99, "Aura Shielding", SpellSchool.YggdrasilLightMagick, SpellCategory.Support),

            // Black Magick
            new SpellDefinition(101, "Fire", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(102, "Fira", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(103, "Firaga", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(104, "Blizzard", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(105, "Blizzara", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(106, "Blizzaga", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(107, "Thunder", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(108, "Thundara", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(109, "Thundaga", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(110, "Water", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(111, "Waterga", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(112, "Aqua", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(113, "Aero", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(114, "Aeroga", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(115, "Bio", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(116, "Poison", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(117, "Toxify", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(118, "Blind", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(119, "Blindga", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(120, "Silence", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(121, "Silencega", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(122, "Sleep", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(123, "Sleepga", SpellSchool.BlackMagick, SpellCategory.Status),
            new SpellDefinition(124, "Shock", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(125, "Scourge", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(126, "Flare", SpellSchool.BlackMagick, SpellCategory.Offensive),
            new SpellDefinition(127, "Scathe", SpellSchool.BlackMagick, SpellCategory.Offensive),

            // Dream Magick
            new SpellDefinition(128, "Spirit Vision", SpellSchool.DreamMagick, SpellCategory.Support),
            new SpellDefinition(122, "Sleep", SpellSchool.DreamMagick, SpellCategory.Status),
            new SpellDefinition(129, "Whisper", SpellSchool.DreamMagick, SpellCategory.Status),
            new SpellDefinition(130, "Foresight", SpellSchool.DreamMagick, SpellCategory.Support),
            new SpellDefinition(131, "Spirit Chains", SpellSchool.DreamMagick, SpellCategory.Status),
            new SpellDefinition(132, "Soul Step", SpellSchool.DreamMagick, SpellCategory.Support),
            new SpellDefinition(133, "Bind & Banish", SpellSchool.DreamMagick, SpellCategory.Offensive),
            new SpellDefinition(134, "Memoryweaving", SpellSchool.DreamMagick, SpellCategory.Status),

            // Nature Magick
            new SpellDefinition(135, "Thornbind", SpellSchool.NatureMagick, SpellCategory.Status),
            new SpellDefinition(136, "Verdant Ward", SpellSchool.NatureMagick, SpellCategory.Support),
            new SpellDefinition(137, "Purifying Spores", SpellSchool.NatureMagick, SpellCategory.Cleanse),
            new SpellDefinition(138, "Briar Cage", SpellSchool.NatureMagick, SpellCategory.Status),
            new SpellDefinition(139, "Sylvan Blessing", SpellSchool.NatureMagick, SpellCategory.Healing),
            new SpellDefinition(140, "Stormwhisper", SpellSchool.NatureMagick, SpellCategory.Offensive),
            new SpellDefinition(141, "Wild Growth", SpellSchool.NatureMagick, SpellCategory.Status),
            new SpellDefinition(142, "Living Bulwark", SpellSchool.NatureMagick, SpellCategory.Support),
            new SpellDefinition(143, "Grovecall", SpellSchool.NatureMagick, SpellCategory.Offensive),
            new SpellDefinition(144, "Rebirth Bloom", SpellSchool.NatureMagick, SpellCategory.Revival),
        };

        private static readonly ActionDefinition[] TechnickDefinitions =
        {
            new ActionDefinition(1, "1000 Needles", "Deal 1,000 damage to one foe."),
            new ActionDefinition(2, "Achilles", "Render one foe vulnerable to an additional element."),
            new ActionDefinition(3, "Addle", "Lowers magick power."),
            new ActionDefinition(4, "Aegis Step", "Defensive repositioning move inspired by shield-and-bow combat. Grants brief evasion and block focus."),
            new ActionDefinition(5, "Arcane Guard", "Reduces magickal damage taken for 2 turns."),
            new ActionDefinition(6, "Arsenal Quip", "Temporarily copy or manifest the weapon of an owned summon for 7 minutes. Cooldown: 30 seconds."),
            new ActionDefinition(7, "Aurora", "Apply a regeneration effect to self or one ally."),
            new ActionDefinition(8, "Baleful Echo", "Recasts the last spell with halved MP cost, but with a chance to misfire or backfire."),
            new ActionDefinition(9, "Black Box Revival", "If the user would be knocked unconscious or killed, one active summon may sacrifice itself to prevent the final blow. Cooldown: 1 week."),
            new ActionDefinition(10, "Blasting Zone", "Fire a concentrated aether blast from the gunblade for heavy damage."),
            new ActionDefinition(11, "Blink Step", "Evade next attack and reposition instantly."),
            new ActionDefinition(12, "Blitz", "Deal physical damage to targets in range."),
            new ActionDefinition(13, "Bloodfest", "Instantly load Powder Cartridges for burst damage windows."),
            new ActionDefinition(14, "Bonecrusher", "Consume HP to reduce the HP of one foe to 0."),
            new ActionDefinition(15, "Bow Shock", "Release an explosive shockwave that damages enemies over time."),
            new ActionDefinition(16, "Brutal Shell", "Follow Keen Edge with a guarded blast, dealing damage and granting a small barrier."),
            new ActionDefinition(17, "Burst Strike", "Spend one Powder Cartridge to deliver a heavy gunblade explosion."),
            new ActionDefinition(18, "Camouflage", "Increase evasion and reduce incoming physical damage for several turns."),
            new ActionDefinition(19, "Challenge", "Force a single enemy to attack you for a short time."),
            new ActionDefinition(20, "Charge", "Restores MP but may fail."),
            new ActionDefinition(21, "Charm", "Cause one foe to Confuse friend with foe."),
            new ActionDefinition(22, "Continuation", "Follow certain cartridge attacks with an extra precision blast."),
            new ActionDefinition(23, "Corpse Relay", "Use an active summon as a battlefield relay point for targeting, pressure, and tactical awareness."),
            new ActionDefinition(24, "Counterblow", "Take an enemy attack and return it upon the attacker as physical damage."),
            new ActionDefinition(25, "Counterspell", "Take an enemy attack and return it upon the attacker as magick damage."),
            new ActionDefinition(26, "Cover", "Intercept damage meant for one ally."),
            new ActionDefinition(27, "Cover Breach", "Overload deployed cover to create pushback, smoke, stagger, or heated shrapnel."),
            new ActionDefinition(28, "Covering Fire", "Allies gain Dodge advantage; interrupts enemy charges."),
            new ActionDefinition(29, "Dead Man's Trigger", "When a Loyal Summon is destroyed, it may perform one final action before vanishing."),
            new ActionDefinition(30, "Deathblow", "Attempt to instantly slay a target at low HP."),
            new ActionDefinition(31, "Demon Slaughter", "Finish the area combo and load one Powder Cartridge."),
            new ActionDefinition(32, "Demon Slice", "Perform a sweeping gunblade strike against multiple enemies."),
            new ActionDefinition(33, "Despair Gaze", "Inflicts Fear and causes target to randomly skip turns."),
            new ActionDefinition(34, "Double Down", "Spend two Powder Cartridges to unleash a devastating point-blank detonation."),
            new ActionDefinition(35, "Echo Mark", "Mark one enemy, improving summon tracking and granting a small bonus to Extraction if the marked target dies."),
            new ActionDefinition(36, "Elude", "Defend with a high probability of evading enemy attacks."),
            new ActionDefinition(37, "Entrench", "Defend and prepare a counterattack, dealing more damage the longer you defend."),
            new ActionDefinition(38, "Erasure", "Nullifies all active buffs on one enemy, including barrier, regen, and reflect effects."),
            new ActionDefinition(39, "Evade", "Attempt to evade enemy attacks. Certain attacks cannot be evaded."),
            new ActionDefinition(40, "Expose", "Lower one foe's defense."),
            new ActionDefinition(41, "Extraction", "When an enemy or NPC is dead, the user gains three D100 attempts to bind the corpse as a Loyal Summon. Cooldown is 1-2 weeks depending on summon size and DM approval."),
            new ActionDefinition(42, "First Aid", "Works on a living ally below 15% HP; restores a random 0–20% of maximum HP."),
            new ActionDefinition(43, "Foresight", "Reveal the target’s intended action next turn, increasing evasion."),
            new ActionDefinition(44, "Gil Toss", "Throw gil to damage all foes in range."),
            new ActionDefinition(45, "Gnashing Fang", "Spend one Powder Cartridge to begin a brutal three-part gunblade chain."),
            new ActionDefinition(46, "Gravebound Reload", "Reload or recharge one firearm by drawing residual energy from a corpse, destroyed summon, or battlefield remnant."),
            new ActionDefinition(47, "Guard", "Reduces physical and magick damage taken from enemy attacks while guarding."),
            new ActionDefinition(48, "Guard Counter", "Enter a guarding stance, then retaliate after blocking a hit."),
            new ActionDefinition(49, "Heart of Corundum", "Upgrade Heart of Stone with stronger mitigation and emergency healing."),
            new ActionDefinition(50, "Heart of Light", "Reduce magical damage taken by the party for several turns."),
            new ActionDefinition(51, "Heart of Stone", "Grant a defensive barrier to self or one ally."),
            new ActionDefinition(52, "Heavy Guard", "Greatly reduces physical and magick damage taken while guarding, but at a higher ATB cost."),
            new ActionDefinition(53, "Heroic Guard", "Massively reduces physical and magick damage taken while guarding, but at a very high ATB cost."),
            new ActionDefinition(54, "Highwind", "Release a catastrophic jump strike, clearing chain gauges. Damage varies by situation."),
            new ActionDefinition(55, "Hollowpoint Round", "Fires armor-piercing shot. Ignores partial Defense."),
            new ActionDefinition(56, "Horology", "Deals time-based damage."),
            new ActionDefinition(57, "Infuse", "Fully consume user's MP, changing one ally's HP to 10 times that amount."),
            new ActionDefinition(58, "Keen Edge", "Strike with the opening cut of the gunblade combo."),
            new ActionDefinition(59, "Launch", "Attack and launch a staggered target into the air."),
            new ActionDefinition(60, "Libra", "Reveal detailed information about targets."),
            new ActionDefinition(61, "Libra II", "Reveal detailed information about Chaos Inflicted targets."),
            new ActionDefinition(62, "Light Guard", "Slightly reduces physical and magick damage taken while guarding. Low ATB cost."),
            new ActionDefinition(63, "Magick Counter", "Chance to counter with a random spell when hit by magick."),
            new ActionDefinition(64, "Mediguard", "Defend while gradually recovering HP."),
            new ActionDefinition(65, "Nebula", "Greatly reduce incoming damage for a short time."),
            new ActionDefinition(66, "NulAll Guard", "Reduces damage taken while guarding and greatly reduces elemental-attribute damage."),
            new ActionDefinition(67, "Numerology", "Damage increases with hits."),
            new ActionDefinition(68, "Poach", "Capture weakened foes for loot."),
            new ActionDefinition(69, "Power Focus", "Increases next magick's power by 50%."),
            new ActionDefinition(70, "Provoke", "Entices enemies to attack you instead of allies."),
            new ActionDefinition(71, "Revive", "Fully consume user's HP, reviving and fully restoring HP of one KO'd ally."),
            new ActionDefinition(72, "Royal Guard", "Assume a tank stance, increasing threat generated and improving defensive posture."),
            new ActionDefinition(73, "Ruin", "Deal non-elemental magick damage to one target."),
            new ActionDefinition(74, "Ruinga", "Deal non-elemental magick damage to targets within a wide radius."),
            new ActionDefinition(75, "Savage Claw", "Second strike of the Gnashing Fang chain."),
            new ActionDefinition(76, "Shades of Black", "Cast a random black magick on one foe."),
            new ActionDefinition(77, "Shadow Dance", "Swap places with an owned active summon. Cooldown: 50 seconds."),
            new ActionDefinition(78, "Shear", "Lowers magick resistance."),
            new ActionDefinition(79, "Sight Unseeing", "Unleash an attack only available when blind."),
            new ActionDefinition(80, "Smite", "Deal heavy damage to a launched target about to recover from stagger."),
            new ActionDefinition(81, "Solid Barrel", "Complete the core combo and load one Powder Cartridge."),
            new ActionDefinition(82, "Souleater", "Consume HP to deal damage to one foe."),
            new ActionDefinition(83, "Soulshred", "Deals high non-elemental damage and reduces MP of target based on caster's missing HP."),
            new ActionDefinition(84, "Sovereign Fist", "Crush foes with a ruinous blow, clearing chain gauges. Damage varies by situation."),
            new ActionDefinition(85, "Spellblade", "Imbue weapon with a selected element for 3 turns."),
            new ActionDefinition(86, "Stamp", "Transfers status effects."),
            new ActionDefinition(87, "Steal", "Steal from one foe."),
            new ActionDefinition(88, "Steelguard", "Reduces damage taken while guarding. Guard becomes more effective each time damage is taken."),
            new ActionDefinition(89, "Summon Fusion", "Temporarily resonate with an owned Loyal Summon to use part of that summon’s abilities. Cooldown: at least 4 days."),
            new ActionDefinition(90, "Superbolide", "Drop to critical HP to become nearly immune to damage for a short duration."),
            new ActionDefinition(91, "Suppressive Burst", "Reduces enemy movement and disables reactions for 1 turn."),
            new ActionDefinition(92, "Telekinesis", "Allow melee weapons to hit distant targets."),
            new ActionDefinition(93, "Traveler", "deals fixed damage to an enemy and enemies in range in its vicinity based on the number of steps currently taken. After being used, or after 1000 steps, the counter resets.."),
            new ActionDefinition(94, "Trick Reload", "Reloads and dodges simultaneously. Grants temporary Evasion."),
            new ActionDefinition(95, "Vein Tap", "Restores MP equal to the number of active illusions when cast."),
            new ActionDefinition(96, "Vendetta", "Counterattack after defending, dealing more damage the more you are attacked."),
            new ActionDefinition(97, "Vital Shot", "Target weak point to deal bonus damage and apply Bleed."),
            new ActionDefinition(98, "Wicked Talon", "Final strike of the Gnashing Fang chain."),
            new ActionDefinition(99, "Wither", "Lower one foe's strength."),
        };

        private static readonly ActionDefinition[] ItemDefinitions =
        {
            new ActionDefinition(1, "Potion", "Restores 120 HP and removes 1 wound; amount increases with Potion Lore."),
            new ActionDefinition(2, "Hi-Potion", "Restores 450 HP; amount increases with Potion Lore."),
            new ActionDefinition(3, "X-Potion", "Restores 1,500 HP and clears all wounds; amount increases with Potion Lore."),
            new ActionDefinition(4, "Ether", "Restores 50 MP; amount increases with Ether Lore."),
            new ActionDefinition(5, "Hi-Ether", "Restores 200 MP; amount increases with Ether Lore."),
            new ActionDefinition(6, "Elixir", "Completely restores HP and MP."),
            new ActionDefinition(7, "Megalixir", "Completely restores HP and MP of the party within a given radius."),
            new ActionDefinition(8, "Phoenix Down", "Revives a character; HP restored increases with Phoenix Lore."),
            new ActionDefinition(9, "Black Phoenix Feather", "Revives a character at Full HP and regrows missing limbs, Inflicts Berserk & Bubble Will ALWAYS trigger a boss fight; A Black Phoenix feather with a transparent black mist emitting from it"),
            new ActionDefinition(10, "Rainbow Phoenix Feather", "Revives a character, 4m Timer is ignored."),
            new ActionDefinition(11, "Antidote", "Removes Poison."),
            new ActionDefinition(12, "Eye Drops", "Removes Blind."),
            new ActionDefinition(13, "Echo Herbs", "Removes Silence."),
            new ActionDefinition(14, "Gold Needle", "Removes Petrify and Stone."),
            new ActionDefinition(15, "Prince's Kiss", "Removes Sleep."),
            new ActionDefinition(16, "Chronos Tear", "Removes Stop and Slow."),
            new ActionDefinition(17, "Handkerchief", "Removes Oil."),
            new ActionDefinition(18, "Remedy", "Removes Poison, Slow, Blind, and Silence; more with Remedy Lore."),
            new ActionDefinition(19, "Vaccine", "Removes Disease."),
            new ActionDefinition(20, "Serum", "Removes Disease."),
            new ActionDefinition(21, "Diablos Stabilizer", "A rare stabilizing agent that suppresses the Curse of Diablos but does not fully remove it."),
            new ActionDefinition(22, "Librascope", "Automatically performs a full Libra II scan, revealing advanced statistics, affinities, abilities, traits, and Chaos-obscured data. Consumed on use."),
            new ActionDefinition(23, "Aero Mote", "Deals wind-aspected mote damage to one target."),
            new ActionDefinition(24, "Aeroga Mote", "A mote containing the power of Aeroga. Deals wind damage to all foes in range and ignores Reflect."),
            new ActionDefinition(25, "Aquara Mote", "A mote containing the power of Water. Deals water damage to all foes in range and ignores Reflect."),
            new ActionDefinition(26, "Bacchus's Wine", "Inflicts Berserk on one target."),
            new ActionDefinition(27, "Balance Mote", "A mote containing the power of Balance. Casts Balance on all foes in range and ignores Reflect."),
            new ActionDefinition(28, "Baltoro Seed", "Grants Protect, Shell, Haste, Bravery, Faith, Invisible, Regen, Float, Bubble, and Libra to one target."),
            new ActionDefinition(29, "Bio Mote", "A mote containing the power of Sap. Inflicts Sap and deals non-elemental damage to all foes in range, ignoring Reflect."),
            new ActionDefinition(30, "Blue Herb", "Haults Poison."),
            new ActionDefinition(31, "Bubble Mote", "A mote containing the power of Bubble. Grants Bubble to a single target and ignores Reflect."),
            new ActionDefinition(32, "Cura Mote", "A mote containing the power of Cura. Restores HP to all allies in range and ignores Reflect."),
            new ActionDefinition(33, "Dark Energy", "Deals massive non-elemental damage to all enemies in range."),
            new ActionDefinition(34, "Dark Matter", "Deals damage to all foes in range. Its damage scales with the number of Knots of Rust used."),
            new ActionDefinition(35, "Dark Mote", "Deals dark-aspected mote damage to one target."),
            new ActionDefinition(36, "Dispel Mote", "Casts Dispel when used."),
            new ActionDefinition(37, "Domaine Calvados", "Grants Bravery to one target."),
            new ActionDefinition(38, "Float Mote", "A mote containing the power of Float. Grants Float to all targets in range and ignores Reflect."),
            new ActionDefinition(39, "Gravity Mote", "Deals damage to all foes in range. Deals non-elemental damage equal to a portion of each target’s maximum HP and ignores Reflect."),
            new ActionDefinition(40, "Green Herb", "heals a small amount of health"),
            new ActionDefinition(41, "Hastega Mote", "A mote containing the power of Hastega. Grants Haste to all allies in range and ignores Reflect."),
            new ActionDefinition(42, "Holy Mote", "A mote containing the power of Holy. Deals holy damage to one foe and ignores Reflect."),
            new ActionDefinition(43, "Hypo Spray", "Restores 800 HP and removes 1 wound; Hypo sprays deliver stimulants, analgesics and active neuro-biological reinforcing elements through a sub-dermal injector. The solution takes time to act on the body: further trauma while it is active reduce the hypo's maximum healing potential. Even a few hits will cancel the effect altogether. "),
            new ActionDefinition(44, "Knot of Rust", "Deals random non-elemental damage to one foe."),
            new ActionDefinition(45, "Lightning Fang", "Deals Lightning-element damage to one foe."),
            new ActionDefinition(46, "Meteorite (A)", "My hopes rose, but 'twas naught but an ordinary stone. Inflicts Sap on one enemy."),
            new ActionDefinition(47, "Meteorite (B)", "A chondrite, a species of meteorite found in plenty. Inflicts Disease on one enemy."),
            new ActionDefinition(48, "Meteorite (C)", "A malleable specimen, teeming with fine veins of iron withal. Deals random physical damage to one target based on the user’s maximum HP."),
            new ActionDefinition(49, "Meteorite (D)", "At last, the elusive achondrite is mine! The Widmanstatten pattern is simply stunning! Deals powerful random physical damage to one target."),
            new ActionDefinition(50, "Nu Khai Sand", "Removes Confuse."),
            new ActionDefinition(51, "Overture MK II", "A master-crafted evolution of Overture, imbued with Ice Magicite. Its blade leaves spectral afterimages that repeat successful strikes moments later."),
            new ActionDefinition(52, "Reflectga Mote", "Casts Reflectga when used."),
            new ActionDefinition(53, "Reverse Mote", "A mote containing the power of Reverse. Grants Reverse to a single target and ignores Reflect."),
            new ActionDefinition(54, "Rime Fang", "Deals Ice-element damage to one foe."),
            new ActionDefinition(55, "Scathe Mote", "A mote containing the power of Scathe. Casts Scathe on all foes in range and ignores Reflect."),
            new ActionDefinition(56, "Shock Mote", "A mote containing the power of Shock. Deals heavy non-elemental damage to all foes in range and ignores Reflect."),
            new ActionDefinition(57, "Soleil Fang", "Deals Fire-element damage to one foe."),
            new ActionDefinition(58, "Vanishga Mote", "A mote containing the power of Vanishga. Grants Invisible to all allies in range and ignores Reflect."),
            new ActionDefinition(59, "Vyrrah’s Lament", "The Spear of the Breaking Dawn — a soul-locked divine spear born when Lumina Farron refused erasure during Father Farron’s Final Ascension Ritual. It is the shape of her refusal, her will made manifest."),
            new ActionDefinition(60, "Warp Mote", "Attempts to displace a target with unstable space magick."),
            new ActionDefinition(61, "Water Mote", "Deals water-aspected mote damage to one target."),
        };

        // These are bridge hooks only. Existing Health/HP parameters and health layers are deliberately absent.
        //
        // Remote-visible action animation contract:
        // - SoY_SpellType, SoY_TechnickType, and SoY_ItemType are synchronized Int selectors.
        //   Expression-menu button presses therefore reach remote avatar instances and can drive FX transitions.
        // - The Active/Bit receiver buses remain UNSYNCED. They are local collision telemetry used by the
        //   Desktop bridge to reconstruct incoming action IDs and must not be rebroadcast to other clients.
        private static readonly ParameterSpec[] BridgeParameters =
        {
            new ParameterSpec("SoY_CombatEnabled", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, true, true),
            new ParameterSpec("SoY_IsEnemy", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, true, true),
            new ParameterSpec(RaycastFireParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec("SoY_SpellType", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec(SpellActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_TechnickType", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec(TechnickActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(TechnickBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_ItemType", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec(ItemActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HealingSourceEnemy", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(DamageSourceEnemyParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HealingRejected", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_MistCharge", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec("SoY_MistMax", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 3f, false, false),
            new ParameterSpec("SoY_MistPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 0f, false, false),
            new ParameterSpec("SoY_DiablosApplicable", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_DiablosPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 0f, false, false),
            new ParameterSpec("SoY_OSCProbe", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HitWeak", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_HitAverage", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_HitStrong", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_HitCritical", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_HitBlocked", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_DebuffBurn", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_DebuffSilence", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_DebuffFreeze", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_DebuffBind", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_DebuffBleed", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_HPPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 1f, false, true),
            new ParameterSpec("SoY_HPStage", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 10f, false, true),
            new ParameterSpec("SoY_DamageReaction", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec("SoY_Damaged", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec("SoY_Healing", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec("SoY_CriticalHP", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec("SoY_KO", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec("SoY_Invulnerable", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_Blocked", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_BurnActive", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_Silenced", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_Frozen", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_Bound", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_Bleeding", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_MagicLocked", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
            new ParameterSpec("SoY_MovementLocked", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool)
        };

        // Existing compatible avatar parameters registered for local OSC observation only.
        // These specs NEVER add or alter Animator parameters; they are added to
        // the selected Expression Parameters asset only when the same correctly
        // typed parameter already exists in the selected FX controller.
        private static readonly ParameterSpec[] CompatibleOscParameters = BuildCompatibleOscParameters();

        private static ParameterSpec[] BuildCompatibleOscParameters()
        {
            var list = new List<ParameterSpec>
            {
                new ParameterSpec("Health", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float),
                new ParameterSpec("Healthbar", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("Hit Blocked", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("DoT Burn", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("DoT Bleed", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("Suppress Silence", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("Slow Freeze", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool),
                new ParameterSpec("Slow Bind", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool)
            };
            foreach (var family in new[]
            {
                "Hit By Weak Attack T", "Hit By Average Attack T",
                "Hit By Strong Attack T", "Hit By Critical Attack T"
            })
            {
                for (var i = 0; i < 4; i++)
                    list.Add(new ParameterSpec(family + i, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool));
            }
            return list.ToArray();
        }

        [Serializable]
        private sealed class HealthAudit
        {
            public HealthSystemKind Kind;
            public readonly List<string> Found = new List<string>();
            public readonly List<string> Missing = new List<string>();
            public readonly List<string> Layers = new List<string>();
            public bool HasLegacyPrototypeHooks;
            public string Summary;
        }

        [Serializable]
        private sealed class SpellAnimationBinding
        {
            public int id;
            public string name;
            public string clipPath;
            public bool enabled = true;
        }

        [Serializable]
        private sealed class ActionAnimationBinding
        {
            public int id;
            public string name;
            public string clipPath;
            public bool enabled = true;
        }

        [Serializable]
        private sealed class MenuFavorite
        {
            public string kind;
            public int id;
            public string name;
        }

        [Serializable]
        private sealed class BackupManifest
        {
            public string version;
            public string build;
            public string avatar;
            public string timestamp;
            public string originalFxPath;
            public string workingFxPath;
            public List<string> createdAssets = new List<string>();
            public List<string> layers = new List<string>();
            public List<string> parameters = new List<string>();
        }

        [Serializable]
        private sealed class RepairTransformSnapshot
        {
            public string hierarchyPath;
            public string parentPath;
            public int siblingIndex;
            public bool activeSelf;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
        }

        [Serializable]
        private sealed class RepairConstraintSnapshot
        {
            public string hierarchyPath;
            public string componentType;
            public string serializedJson;
        }

        [Serializable]
        private sealed class RepairTransactionManifest
        {
            public string version;
            public string build;
            public string avatar;
            public string timestamp;
            public string reason;
            public List<RepairTransformSnapshot> transforms = new List<RepairTransformSnapshot>();
            public List<RepairConstraintSnapshot> constraints = new List<RepairConstraintSnapshot>();
            public List<string> plannedActions = new List<string>();
        }

        private sealed class ManagedRepairFinding
        {
            public GameObject Host;
            public Component Component;
            public ManagedRepairState State;
            public ManagedRepairKind Kind;
            public string Role;
            public string Summary;
            public string Action;

            public bool CanAutoRepair
            {
                get
                {
                    return State == ManagedRepairState.Outdated ||
                           State == ManagedRepairState.Repairable ||
                           State == ManagedRepairState.Defunct;
                }
            }
        }

        [Serializable]
        private sealed class AvatarAnimationProfile
        {
            public string avatarName;
            public List<SpellAnimationBinding> spellAnimations = new List<SpellAnimationBinding>();
            public List<ActionAnimationBinding> technickAnimations = new List<ActionAnimationBinding>();
            public List<ActionAnimationBinding> itemAnimations = new List<ActionAnimationBinding>();
            public List<MenuFavorite> favorites = new List<MenuFavorite>();
            public string healthFullClipPath;
            public string healthHalfClipPath;
            public string healthCriticalClipPath;
            public string healthKoClipPath;
        }

        private StudioTab tab;
        private ContactsPage contactsPage;
        private ToolsPage toolsPage;
        private DeliveryMode deliveryMode = DeliveryMode.Contact;
        private AnimationPage animationPage = AnimationPage.Health;
        private WizardStep wizardStep;
        private string globalSearch = string.Empty;
        private string parameterSearch = string.Empty;
        private string menuBuilderSearch = string.Empty;
        private string backupStatus = "No backup action performed.";
        private ContactPreset contactPreset = ContactPreset.Custom;
        private RaycastCollisionTarget raycastCollisionTarget = RaycastCollisionTarget.Players;
        private bool raycastApplyRotation;
        private bool raycastCreateLineRenderer;
        private float raycastDistance = 25f;
        private Vector3 raycastDirection = Vector3.forward;
        private string raycastParameterPrefix = "SoY_Raycast";
        private float raycastImpactRadius = 0.12f;
        private bool raycastUseCustomPrefix;
        private bool showRaycastAdvanced;
        private bool showContactAdvanced;
        private ContactAttachmentMode contactAttachmentMode = ContactAttachmentMode.ContactObject;
        private GameObject weaponAttachmentTarget;
        private bool constraintMaintainOffset = true;
        private float constraintWeight = 1f;
        private bool suppressContactAttachment;
        private bool showSetupGuide;
        private bool showSetupDiagnostics;
        private bool showActionDetails;
        private bool showContactUtilities;
        private bool showAnimationAdvanced;
        private float runtimeTestHpPercent = 1f;
        private string runtimeTestSummary = "Not tested.";
        private VRCAvatarDescriptor avatarDescriptor;
        private AnimatorController fxController;
        private VRCExpressionParameters expressionParameters;
        private VRCExpressionsMenu expressionsMenu;
        private GameObject avatarRoot;
        private GameObject explicitTarget;

        private OutgoingContactKind outgoingKind;
        private GameObject stagedPreview;
        private GameObject stagedTarget;
        private ContactDraftKind stagedKind;
        private bool advancedContextFoldout;
        private bool diagnosticsFoldout;
        private string fxCopyPath = string.Empty;
        private UnityWebRequest updateRequest;
        private UnityWebRequest updateDownloadRequest;
        private UnityWebRequest updateChecksumRequest;
        private byte[] updatePendingSourceBytes;
        private string updateDownloadAssetName = string.Empty;
        private GitHubReleaseInfo latestRelease;
        private string updateStatus = "Waiting for background check";
        private bool updateAvailable;
        private bool updateCheckWasManual;
        private bool autoCheckUpdates = true;
        private UpdateChannel updateChannel = UpdateChannel.Stable;
        private string updateLastChecked = "Never";
        private double nextBackgroundUpdateCheckAt;

        // TB2 left-side category navigation. Categories collapse like Admin Panel groups.
        private const float SidebarWidth = 218f;
        private Vector2 sidebarScroll;
        private bool showAvatarContext = true;
        private bool navAvatarExpanded = true;
        private bool navContactsExpanded = true;
        private bool navAnimationsExpanded = true;
        private bool navMaintenanceExpanded = true;
        private bool navSupportExpanded = true;

        // v0.5.9 accessibility, navigation, and managed-repair preferences.
        private TextScaleMode textScaleMode = TextScaleMode.Normal;
        private ColorVisionMode colorVisionMode = ColorVisionMode.Standard;
        private MenuNavigationMode menuNavigationMode = MenuNavigationMode.Combined;
        private bool largeControls;
        private bool shortVrchatLabels;

        // Search and animation authoring state.
        private string spellSearch = string.Empty;
        private string technickSearch = string.Empty;
        private string itemSearch = string.Empty;
        private string animationSpellSearch = string.Empty;
        private string animationTechnickSearch = string.Empty;
        private string animationItemSearch = string.Empty;
        private int animationSpellSelectionIndex;
        private int animationTechnickSelectionIndex;
        private int animationItemSelectionIndex;
        private AnimationClip animationSpellClip;
        private AnimationClip animationTechnickClip;
        private AnimationClip animationItemClip;
        private AvatarAnimationProfile animationProfile = new AvatarAnimationProfile();
        private string animationProfileAssetPath = string.Empty;

        private AttackTier attackTier = AttackTier.Average;
        private ContactShape attackShape = ContactShape.Capsule;
        private float attackRadius = 0.06f;
        private float attackHeight = 0.75f;
        private Vector3 attackBoxSize = new Vector3(0.10f, 0.10f, 0.80f);
        private Vector3 attackPosition;
        private Vector3 attackRotation;
        private bool attackCreateChild = true;
        private bool attackStartsEnabled;
        private bool addBurnToAttack;
        private bool addSilenceToAttack;
        private bool addFreezeToAttack;
        private bool addBindToAttack;
        private bool addBleedToAttack;

        private SpellSchool spellSchool = SpellSchool.WhiteMagick;
        private int spellSelectionIndex;
        private ContactShape spellShape = ContactShape.Sphere;
        private float spellRadius = 0.14f;
        private float spellHeight = 0.40f;
        private Vector3 spellBoxSize = new Vector3(0.25f, 0.25f, 0.50f);
        private Vector3 spellPosition;
        private Vector3 spellRotation;
        private bool spellStartsEnabled;

        private int technickSelectionIndex;
        private ContactShape technickShape = ContactShape.Sphere;
        private float technickRadius = 0.14f;
        private float technickHeight = 0.40f;
        private Vector3 technickBoxSize = new Vector3(0.25f, 0.25f, 0.50f);
        private Vector3 technickPosition;
        private Vector3 technickRotation;
        private bool technickStartsEnabled;

        private int itemSelectionIndex;
        private ContactShape itemShape = ContactShape.Sphere;
        private float itemRadius = 0.14f;
        private float itemHeight = 0.40f;
        private Vector3 itemBoxSize = new Vector3(0.25f, 0.25f, 0.50f);
        private Vector3 itemPosition;
        private Vector3 itemRotation;
        private bool itemStartsEnabled;

        private ContactShape blockShape = ContactShape.Box;
        private float blockRadius = 0.18f;
        private float blockHeight = 0.65f;
        private Vector3 blockBoxSize = new Vector3(0.45f, 0.65f, 0.08f);
        private Vector3 blockPosition;
        private Vector3 blockRotation;
        private bool blockCreateChild = true;
        private bool addLegacyBlockedSender = true;

        private ContactShape debuffShape = ContactShape.Sphere;
        private float debuffRadius = 0.12f;
        private float debuffHeight = 0.40f;
        private Vector3 debuffBoxSize = new Vector3(0.20f, 0.20f, 0.40f);
        private Vector3 debuffPosition;
        private Vector3 debuffRotation;
        private bool debuffCreateChild = true;
        private bool debuffStartsEnabled;
        private bool debuffBurn = true;
        private bool debuffSilence;
        private bool debuffFreeze;
        private bool debuffBind;
        private bool debuffBleed;

        private ContactShape incomingShape = ContactShape.Capsule;
        private float incomingRadius = 0.22f;
        private float incomingHeight = 0.65f;
        private Vector3 incomingBoxSize = new Vector3(0.38f, 0.65f, 0.25f);
        private Vector3 incomingPosition;
        private Vector3 incomingRotation;
        private bool incomingCreateChild = true;
        private bool incomingHits = true;
        private bool incomingDebuffs = true;
        private bool incomingSpells = true;
        private bool incomingTechnicks = true;
        private bool incomingItems = true;
        private bool incomingWhiteSpells = true;
        private bool incomingBlackSpells = true;
        private bool incomingGreenSpells;
        private bool incomingTimeSpells;
        private bool incomingArcaneSpells;
        private bool incomingSynergistSpells;
        private bool incomingIllusionSpells;
        private bool incomingDreamSpells;
        private bool incomingNatureSpells;
        private bool incomingChaosSpells;
        private bool incomingAbyssalSpells;
        private bool incomingYggdrasilLightSpells;
        private bool forceIncomingOnExistingHealth;
        private bool bridgeBlockToOsc = true;

        private Vector2 scroll;
        private readonly List<string> operationLog = new List<string>();
        private readonly List<ManagedRepairFinding> managedRepairFindings = new List<ManagedRepairFinding>();
        private string managedRepairSummary = "Run an audit to inspect Stories-managed contacts.";
        private string managedRepairPreview = "No repair preview generated.";
        private string lastRepairSnapshotPath = string.Empty;
        private bool managedRepairAuditReady;
        private bool managedRepairShowHealthy;
        private bool managedRepairShowForeign;
        private bool managedRepairConfirmDestructive = true;
        private int lastRepairUndoGroup = -1;
        private HealthAudit cachedAudit;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle wrappedLabel;

        [MenuItem("Tools/Stories Of Yggdrasil/OSC Contact System")]
        private static void Open()
        {
            var window = GetWindow<StoriesOfYggdrasilOSCContactSystem>("SoY OSC Contacts");
            window.minSize = new Vector2(720f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            Debug.Log("[Stories OSC Unity Tool] Loaded v" + Version + " — " + BuildLabel);
            LoadEditorPreferences();
            LoadCachedUpdateState();
            if (avatarDescriptor != null)
                LoadAnimationProfile();
            wantsMouseMove = true;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
            EditorApplication.update -= BackgroundMonitorTick;
            EditorApplication.update += BackgroundMonitorTick;
            RefreshHealthAudit();
            nextBackgroundUpdateCheckAt = EditorApplication.timeSinceStartup + 1.0d;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.update -= BackgroundMonitorTick;
            EditorApplication.update -= PollUpdateCheck;
            EditorApplication.update -= PollUpdateDownload;
            EditorApplication.update -= PollUpdateChecksum;
            if (updateRequest != null)
            {
                updateRequest.Abort();
                updateRequest.Dispose();
                updateRequest = null;
            }
            if (updateDownloadRequest != null)
            {
                updateDownloadRequest.Abort();
                updateDownloadRequest.Dispose();
                updateDownloadRequest = null;
            }
            if (updateChecksumRequest != null)
            {
                updateChecksumRequest.Abort();
                updateChecksumRequest.Dispose();
                updateChecksumRequest = null;
            }
            CancelStagedPreview(false);
        }

        private void OnInspectorUpdate()
        {
            if (stagedPreview != null)
                SceneView.RepaintAll();
        }

        private void BackgroundMonitorTick()
        {
            var now = EditorApplication.timeSinceStartup;
            if (autoCheckUpdates && now >= nextBackgroundUpdateCheckAt && updateRequest == null && updateDownloadRequest == null && updateChecksumRequest == null)
            {
                nextBackgroundUpdateCheckAt = now + BackgroundUpdateIntervalSeconds;
                CheckForUpdates(false);
            }
        }

        private float AccessibilityScale
        {
            get
            {
                switch (textScaleMode)
                {
                    case TextScaleMode.Large: return 1.25f;
                    case TextScaleMode.ExtraLarge: return 1.50f;
                    default: return 1f;
                }
            }
        }

        private Color HeaderBackgroundColor
        {
            get
            {
                switch (colorVisionMode)
                {
                    case ColorVisionMode.HighContrast: return new Color(0.02f, 0.02f, 0.02f);
                    case ColorVisionMode.ProtanopiaFriendly: return new Color(0.03f, 0.16f, 0.24f);
                    case ColorVisionMode.DeuteranopiaFriendly: return new Color(0.10f, 0.08f, 0.22f);
                    case ColorVisionMode.TritanopiaFriendly: return new Color(0.22f, 0.07f, 0.10f);
                    case ColorVisionMode.Monochrome: return new Color(0.10f, 0.10f, 0.10f);
                    default: return new Color(0.075f, 0.045f, 0.12f);
                }
            }
        }

        private Color AccentColor
        {
            get
            {
                switch (colorVisionMode)
                {
                    case ColorVisionMode.HighContrast: return Color.white;
                    case ColorVisionMode.ProtanopiaFriendly: return new Color(0.15f, 0.75f, 1f);
                    case ColorVisionMode.DeuteranopiaFriendly: return new Color(0.95f, 0.65f, 0.15f);
                    case ColorVisionMode.TritanopiaFriendly: return new Color(1f, 0.35f, 0.45f);
                    case ColorVisionMode.Monochrome: return new Color(0.85f, 0.85f, 0.85f);
                    default: return new Color(0.62f, 0.35f, 0.88f);
                }
            }
        }

        private void InvalidateStyles()
        {
            titleStyle = null;
            subtitleStyle = null;
            cardTitleStyle = null;
            badgeStyle = null;
            wrappedLabel = null;
        }

        private void BuildStyles()
        {
            if (titleStyle != null)
                return;

            var scale = AccessibilityScale;
            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(20 * scale),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = colorVisionMode == ColorVisionMode.HighContrast ? Color.white : new Color(0.95f, 0.80f, 0.32f) }
            };

            subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = Mathf.RoundToInt(11 * scale),
                normal = { textColor = colorVisionMode == ColorVisionMode.HighContrast ? Color.white : new Color(0.72f, 0.70f, 0.82f) }
            };

            cardTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.RoundToInt(12 * scale),
                normal = { textColor = colorVisionMode == ColorVisionMode.HighContrast ? Color.white : new Color(0.90f, 0.88f, 0.96f) }
            };

            badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 3, 3)
            };

            wrappedLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = Mathf.RoundToInt(11 * scale),
                wordWrap = true,
                richText = true
            };
        }

        private void ApplyAccessibilitySkin()
        {
            var scale = AccessibilityScale;
            GUI.skin.label.fontSize = Mathf.RoundToInt(11 * scale);
            GUI.skin.button.fontSize = Mathf.RoundToInt(11 * scale);
            GUI.skin.textField.fontSize = Mathf.RoundToInt(11 * scale);
            GUI.skin.textArea.fontSize = Mathf.RoundToInt(11 * scale);
            GUI.skin.toggle.fontSize = Mathf.RoundToInt(11 * scale);
        }

        private void OnGUI()
        {
            BuildStyles();
            ApplyAccessibilitySkin();
            DrawHeader();

            EditorGUILayout.BeginHorizontal();
            DrawSidebarNavigation();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawProjectFields();
            DrawActiveWorkspace();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActiveWorkspace()
        {
            switch (tab)
            {
                case StudioTab.Setup:
                    DrawSetup();
                    break;
                case StudioTab.Contacts:
                    DrawContactsWorkspace();
                    break;
                case StudioTab.AnimatorSetup:
                    DrawAnimatorSetup();
                    break;
                case StudioTab.MenuBuilder:
                    DrawMenuBuilder();
                    break;
                case StudioTab.Tools:
                    DrawToolsWorkspace();
                    break;
                case StudioTab.Help:
                    DrawHelp();
                    break;

                // Legacy routes are folded into the sidebar destinations.
                case StudioTab.OutgoingContacts:
                    contactsPage = ContactsPage.Outgoing;
                    tab = StudioTab.Contacts;
                    DrawContactsWorkspace();
                    break;
                case StudioTab.IncomingReceivers:
                    contactsPage = ContactsPage.Incoming;
                    tab = StudioTab.Contacts;
                    DrawContactsWorkspace();
                    break;
                case StudioTab.Diagnostics:
                    toolsPage = ToolsPage.Status;
                    tab = StudioTab.Tools;
                    DrawToolsWorkspace();
                    break;
                case StudioTab.Accessibility:
                    toolsPage = ToolsPage.Display;
                    tab = StudioTab.Tools;
                    DrawToolsWorkspace();
                    break;
                case StudioTab.Backups:
                    toolsPage = ToolsPage.Backups;
                    tab = StudioTab.Tools;
                    DrawToolsWorkspace();
                    break;
            }
        }

        private void DrawHeader()
        {
            var headerHeight = largeControls ? 84f : 70f;
            var headerRect = EditorGUILayout.GetControlRect(false, headerHeight);
            EditorGUI.DrawRect(headerRect, HeaderBackgroundColor);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 3f, headerRect.width, 3f), AccentColor);

            var titleRect = new Rect(headerRect.x + 18f, headerRect.y + 10f, headerRect.width - 160f, 28f);
            var subRect = new Rect(headerRect.x + 18f, headerRect.y + 39f, headerRect.width - 160f, 20f);
            GUI.Label(titleRect, "Stories Of Yggdrasil — OSC Contact System", titleStyle);
            GUI.Label(subRect, "Avatar setup • Contact authoring • FX animations • Safe repair", subtitleStyle);

            var versionRect = new Rect(headerRect.xMax - 112f, headerRect.y + 12f, 88f, 24f);
            EditorGUI.DrawRect(versionRect, colorVisionMode == ColorVisionMode.HighContrast ? Color.black : new Color(0.18f, 0.10f, 0.28f));
            GUI.Label(versionRect, "v" + Version + " • " + BuildNumber, badgeStyle);

            if (updateAvailable)
            {
                var updateRect = new Rect(headerRect.xMax - 222f, headerRect.y + 12f, 102f, 24f);
                EditorGUI.DrawRect(updateRect, new Color(0.18f, 0.42f, 0.20f));
                if (GUI.Button(updateRect, "Update Ready", badgeStyle))
                    PromptForUpdate();
            }

        }

        private void DrawProjectFields()
        {
            showAvatarContext = EditorGUILayout.Foldout(showAvatarContext, "Avatar Context", true);
            if (!showAvatarContext)
            {
                EditorGUILayout.Space(4f);
                return;
            }

            BeginCard("Avatar Context");
            EditorGUI.BeginChangeCheck();
            avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Avatar Descriptor", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
            explicitTarget = (GameObject)EditorGUILayout.ObjectField(
                "Contact Target", explicitTarget, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                RefreshHealthAudit();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load From Avatar", GUILayout.Height(28f)))
            {
                LoadFromAvatarDescriptor();
                Repaint();
            }
            if (GUILayout.Button("Use Selected Object as Target", GUILayout.Height(28f)))
            {
                explicitTarget = Selection.activeGameObject;
                Repaint();
            }
            if (GUILayout.Button("Help", GUILayout.Width(72f), GUILayout.Height(28f)))
                tab = StudioTab.Help;
            EditorGUILayout.EndHorizontal();

            if (avatarDescriptor == null)
            {
                EditorGUILayout.HelpBox(
                    "Start by assigning the avatar's VRC Avatar Descriptor, then press Load From Avatar.",
                    MessageType.Info);
            }
            else if (fxController == null)
            {
                EditorGUILayout.HelpBox(
                    "No FX Animator Controller is currently assigned to this avatar.",
                    MessageType.Warning);
            }
            else if (IsSafeFxCopy(fxController))
            {
                fxCopyPath = AssetDatabase.GetAssetPath(fxController);
                EditorGUILayout.HelpBox(
                    "Safe FX copy active:\n" + fxCopyPath,
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "The avatar's original FX controller will not be edited. The first Animator setup action creates a copy under:\n" +
                    FxCopyRoot + "/<Avatar>_<FX>_SoY_FX.controller\nand assigns that copy to the avatar.",
                    MessageType.Info);
                if (GUILayout.Button("Create and Assign Safe FX Copy", GUILayout.Height(30f)))
                    EnsureSafeFxCopy(true);
            }

            advancedContextFoldout = EditorGUILayout.Foldout(
                advancedContextFoldout, "Advanced Asset Fields", true);
            if (advancedContextFoldout)
            {
                EditorGUI.indentLevel++;
                fxController = (AnimatorController)EditorGUILayout.ObjectField(
                    "Working FX Controller", fxController, typeof(AnimatorController), false);
                expressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField(
                    "Expression Parameters", expressionParameters, typeof(VRCExpressionParameters), false);
                expressionsMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(
                    "Expressions Menu", expressionsMenu, typeof(VRCExpressionsMenu), false);
                avatarRoot = (GameObject)EditorGUILayout.ObjectField(
                    "Avatar Root", avatarRoot, typeof(GameObject), true);
                EditorGUI.indentLevel--;

                if (avatarDescriptor != null && expressionParameters != null &&
                    avatarDescriptor.expressionParameters != expressionParameters)
                {
                    EditorGUILayout.HelpBox(
                        "The selected Expression Parameters asset is not the one assigned to this avatar. " +
                        "Press Load From Avatar to restore the assigned asset.",
                        MessageType.Warning);
                }
            }
            EndCard();
        }

        private void LoadFromAvatarDescriptor()
        {
            if (avatarDescriptor == null)
                return;

            avatarRoot = avatarDescriptor.gameObject;
            expressionParameters = avatarDescriptor.expressionParameters;
            expressionsMenu = avatarDescriptor.expressionsMenu;

            try
            {
                var fxLayer = avatarDescriptor.baseAnimationLayers
                    .FirstOrDefault(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX);
                fxController = fxLayer.animatorController as AnimatorController;
                fxCopyPath = IsSafeFxCopy(fxController) ? AssetDatabase.GetAssetPath(fxController) : string.Empty;
            }
            catch (Exception exception)
            {
                operationLog.Insert(0, "Could not read FX Controller from Avatar Descriptor: " + exception.Message);
            }

            RefreshHealthAudit();
            LoadAnimationProfile();
            operationLog.Insert(0, "Loaded FX, menu, Expression Parameters, avatar root, and the v0.5.9 animation/repair profile.");
        }

        private void DrawSidebarNavigation()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(SidebarWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("CATEGORIES", cardTitleStyle);
            EditorGUILayout.Space(3f);

            sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll, false, false, GUILayout.Width(SidebarWidth - 8f));

            DrawSidebarCategory("AVATAR", ref navAvatarExpanded);
            if (navAvatarExpanded)
                DrawSidebarButton("Setup & Preflight", tab == StudioTab.Setup, () => tab = StudioTab.Setup);

            DrawSidebarCategory("COMBAT CONTACTS", ref navContactsExpanded);
            if (navContactsExpanded)
            {
                DrawSidebarButton("Outgoing", tab == StudioTab.Contacts && contactsPage == ContactsPage.Outgoing, () =>
                {
                    contactsPage = ContactsPage.Outgoing;
                    tab = StudioTab.Contacts;
                });
                DrawSidebarButton("Incoming", tab == StudioTab.Contacts && contactsPage == ContactsPage.Incoming, () =>
                {
                    contactsPage = ContactsPage.Incoming;
                    tab = StudioTab.Contacts;
                });
            }

            DrawSidebarCategory("AVATAR FX", ref navAnimationsExpanded);
            if (navAnimationsExpanded)
            {
                DrawSidebarButton("Health States", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Health, () =>
                {
                    animationPage = AnimationPage.Health;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Spell Animations", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Spells, () =>
                {
                    animationPage = AnimationPage.Spells;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Technick Animations", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Technicks, () =>
                {
                    animationPage = AnimationPage.Technicks;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Item Animations", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Items, () =>
                {
                    animationPage = AnimationPage.Items;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Layer Setup", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Setup, () =>
                {
                    animationPage = AnimationPage.Setup;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Menu Builder", tab == StudioTab.MenuBuilder, () => tab = StudioTab.MenuBuilder);
            }

            DrawSidebarCategory("MAINTENANCE", ref navMaintenanceExpanded);
            if (navMaintenanceExpanded)
            {
                DrawSidebarButton("Diagnostics", tab == StudioTab.Tools && toolsPage == ToolsPage.Status, () =>
                {
                    toolsPage = ToolsPage.Status;
                    tab = StudioTab.Tools;
                });
                DrawSidebarButton("Managed Repair", tab == StudioTab.Tools && toolsPage == ToolsPage.Repair, () =>
                {
                    toolsPage = ToolsPage.Repair;
                    tab = StudioTab.Tools;
                });
                DrawSidebarButton("Display", tab == StudioTab.Tools && toolsPage == ToolsPage.Display, () =>
                {
                    toolsPage = ToolsPage.Display;
                    tab = StudioTab.Tools;
                });
                DrawSidebarButton("Backups", tab == StudioTab.Tools && toolsPage == ToolsPage.Backups, () =>
                {
                    toolsPage = ToolsPage.Backups;
                    tab = StudioTab.Tools;
                });
            }

            DrawSidebarCategory("SUPPORT", ref navSupportExpanded);
            if (navSupportExpanded)
                DrawSidebarButton("Help", tab == StudioTab.Help, () => tab = StudioTab.Help);

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("v" + Version + " • " + BuildNumber, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawSidebarCategory(string label, ref bool expanded)
        {
            var oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = colorVisionMode == ColorVisionMode.HighContrast
                ? Color.black
                : new Color(0.14f, 0.09f, 0.20f);
            var marker = expanded ? "▼  " : "▶  ";
            if (GUILayout.Button(marker + label, EditorStyles.toolbarButton, GUILayout.Height(largeControls ? 34f : 26f)))
                expanded = !expanded;
            GUI.backgroundColor = oldBackground;
        }

        private void DrawSidebarButton(string label, bool selected, Action activate)
        {
            var oldBackground = GUI.backgroundColor;
            var oldContent = GUI.contentColor;
            if (selected)
            {
                GUI.backgroundColor = AccentColor;
                GUI.contentColor = colorVisionMode == ColorVisionMode.HighContrast ? Color.black : Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.11f, 0.11f, 0.14f);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10f);
            if (GUILayout.Button(label, GUILayout.Height(largeControls ? 38f : 29f), GUILayout.ExpandWidth(true)))
            {
                activate?.Invoke();
                scroll = Vector2.zero;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = oldBackground;
            GUI.contentColor = oldContent;
        }

        private void DrawContactsWorkspace()
        {
            if (contactsPage == ContactsPage.Outgoing)
                DrawOutgoingContacts();
            else
                DrawIncomingReceivers();
        }

        private void DrawToolsWorkspace()
        {
            switch (toolsPage)
            {
                case ToolsPage.Status:
                    DrawDiagnosticsWorkspace();
                    break;
                case ToolsPage.Repair:
                    DrawManagedRepairCenter();
                    break;
                case ToolsPage.Display:
                    DrawAccessibilitySettings();
                    break;
                case ToolsPage.Backups:
                    DrawBackupCenter();
                    break;
            }
        }

        private void DrawSetup()
        {
            BeginCard("Avatar Setup");
            var ready = avatarDescriptor != null && fxController != null && expressionParameters != null;
            EditorGUILayout.LabelField(
                ready ? "Avatar assets loaded. Safe setup and repair actions are available." : "Assign the Avatar Descriptor above, then load its assets.",
                wrappedLabel);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(avatarDescriptor == null))
            {
                if (GUILayout.Button("Prepare / Repair", GUILayout.Height(largeControls ? 44f : 34f)))
                {
                    LoadFromAvatarDescriptor();
                    if (EnsureSafeFxCopy(true))
                        InstallAllBridgeHooks();
                }
            }
            if (GUILayout.Button("Run Preflight", GUILayout.Height(largeControls ? 44f : 34f)))
                RefreshHealthAudit();
            EditorGUILayout.EndHorizontal();

            DrawTagRow("Avatar", avatarDescriptor != null ? "✓ Ready" : "✕ Missing", "Descriptor");
            DrawTagRow("FX", fxController != null ? (IsSafeFxCopy(fxController) ? "✓ Safe Copy" : "! Original") : "✕ Missing", "Working controller");
            DrawTagRow("Parameters", expressionParameters != null ? "✓ Assigned" : "✕ Missing", "Expression Parameters");
            DrawTagRow("Menus", expressionsMenu != null ? "✓ Assigned" : "! Optional", "Expressions Menu");
            DrawTagRow("Raycast", RaycastTypeAvailable() ? "✓ Available" : "! SDK Update", "VRCRaycast");
            EndCard();

            showSetupGuide = EditorGUILayout.Foldout(showSetupGuide, "Guided Setup", true);
            if (showSetupGuide)
                DrawGuidedSetupWizard();

            DrawPreflightCard();

            showSetupDiagnostics = EditorGUILayout.Foldout(showSetupDiagnostics, "Health & Safety Diagnostics", true);
            if (showSetupDiagnostics)
            {
                DrawCriticalHpDiagnosticsCard();
                DrawHealthSafetyCard();
            }
        }

        private void DrawCriticalHpDiagnosticsCard()
        {
            BeginCard("Critical HP Runtime Diagnostic");
            EditorGUILayout.LabelField(
                "Canonical HP Critical is living HP strictly below 15% of effective Max HP. The tool-owned Full / Half / Critical / KO layer uses SoY_HPPercent directly, so a stale SoY_CriticalHP Bool cannot force that managed layer into Critical.",
                wrappedLabel);
            DrawTagRow("Threshold", "HP > 0 and < 15%", "15.00% is not Critical; 0 HP is KO");
            DrawTagRow("Managed Layer", "SoY_HPPercent + SoY_KO", "Does not depend on SoY_CriticalHP");
            DrawTagRow("Desktop Bool", "SoY_CriticalHP", "Must be set False again whenever HP returns to 15% or higher");
            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("REBUILD MANAGED HEALTH LAYER AT 15%", GUILayout.Height(largeControls ? 46f : 34f)))
                    RebuildHealthAnimationLayer();
            }
            EditorGUILayout.HelpBox(
                "If Avatar Parameters shows SoY_CriticalHP = True while SoY_HPPercent is 0.15 or higher, the incorrect value is being sent by the Desktop OSC runtime—not generated by this Unity layer. Use the current Desktop Critical-15 audit to repair its authoritative reset path.",
                MessageType.Warning);
            EndCard();
        }

        private void DrawPreflightCard()
        {
            BeginCard("Avatar Preflight");
            var checks = new List<KeyValuePair<string, bool>>
            {
                new KeyValuePair<string, bool>("Avatar Descriptor", avatarDescriptor != null),
                new KeyValuePair<string, bool>("FX Controller", fxController != null),
                new KeyValuePair<string, bool>("Expression Parameters", expressionParameters != null),
                new KeyValuePair<string, bool>("Expressions Menu", expressionsMenu != null),
                new KeyValuePair<string, bool>("Safe FX Copy", IsSafeFxCopy(fxController)),
                new KeyValuePair<string, bool>("VRChat Contacts SDK", ContactTypesAvailable()),
                new KeyValuePair<string, bool>("VRCRaycast SDK", RaycastTypeAvailable()),
                new KeyValuePair<string, bool>("Single Tool Script", AssetDatabase.FindAssets("StoriesOfYggdrasilOSCContactSystem t:MonoScript").Length == 1)
            };
            var passed = checks.Count(pair => pair.Value);
            var readiness = checks.Count == 0 ? 0f : passed / (float)checks.Count;
            var rect = EditorGUILayout.GetControlRect(false, largeControls ? 28f : 20f);
            EditorGUI.ProgressBar(rect, readiness, "Readiness " + Mathf.RoundToInt(readiness * 100f) + "% — " + passed + "/" + checks.Count);
            foreach (var check in checks)
                EditorGUILayout.LabelField((check.Value ? "✓ " : "✕ ") + check.Key, wrappedLabel);

            if (expressionParameters != null)
            {
                var used = SyncedExpressionCost(expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>());
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Synchronized Parameter Memory", cardTitleStyle);
                var budgetRect = EditorGUILayout.GetControlRect(false, largeControls ? 28f : 20f);
                EditorGUI.ProgressBar(budgetRect, Mathf.Clamp01(used / 256f), used + " / 256 bits used");
                EditorGUILayout.LabelField(used <= 179 ? "✓ Safe budget" : used <= 230 ? "! Caution: budget is becoming crowded" : "✕ Critical: very little synchronized memory remains", wrappedLabel);
            }

            using (new EditorGUI.DisabledScope(avatarDescriptor == null))
            {
                if (GUILayout.Button("REPAIR ALL SAFE STORIES OSC ITEMS", GUILayout.Height(largeControls ? 48f : 36f)))
                {
                    LoadFromAvatarDescriptor();
                    if (EnsureSafeFxCopy(true))
                    {
                        InstallAllBridgeHooks();
                        AuditManagedSystems();
                        RunManagedRepair(false);
                    }
                }
            }
            EditorGUILayout.HelpBox("This repair only manages Stories Of Yggdrasil assets and parameters. Existing third-party health logic remains locked and untouched.", MessageType.Info);
            EndCard();
        }

        private void DrawManagedRepairCenter()
        {
            BeginCard("Managed-System Repair Center");
            EditorGUILayout.LabelField(
                "Audits only Stories Of Yggdrasil-owned Contacts. Repairs preserve the existing GameObject, parent, sibling order, local/world transform, active state, prefab overrides, and every detected constraint component. Foreign avatar systems are reported but never changed.",
                wrappedLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("AUDIT AVATAR", GUILayout.Height(largeControls ? 44f : 32f)))
                AuditManagedSystems();
            if (GUILayout.Button("PREVIEW REPAIR", GUILayout.Height(largeControls ? 44f : 32f)))
                PreviewManagedRepair();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(avatarRoot == null || !managedRepairAuditReady))
            {
                if (GUILayout.Button("REPAIR SELECTED", GUILayout.Height(largeControls ? 44f : 32f)))
                    RunManagedRepair(true);
                if (GUILayout.Button("REPAIR ALL SAFE", GUILayout.Height(largeControls ? 44f : 32f)))
                    RunManagedRepair(false);
            }
            using (new EditorGUI.DisabledScope(lastRepairUndoGroup < 0))
            {
                if (GUILayout.Button("ROLL BACK LAST REPAIR", GUILayout.Height(largeControls ? 44f : 32f)))
                    RollBackLastManagedRepair();
            }
            EditorGUILayout.EndHorizontal();

            managedRepairConfirmDestructive = EditorGUILayout.ToggleLeft(
                "Require confirmation before removing defunct or duplicate managed components",
                managedRepairConfirmDestructive);
            managedRepairShowHealthy = EditorGUILayout.ToggleLeft("Show healthy managed entries", managedRepairShowHealthy);
            managedRepairShowForeign = EditorGUILayout.ToggleLeft("Show foreign / untouched entries", managedRepairShowForeign);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(managedRepairSummary, wrappedLabel);
            if (!string.IsNullOrWhiteSpace(lastRepairSnapshotPath))
                EditorGUILayout.SelectableLabel("Last snapshot: " + lastRepairSnapshotPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EndCard();

            BeginCard("Repair Preview");
            EditorGUILayout.LabelField(managedRepairPreview, wrappedLabel);
            EndCard();

            BeginCard("Audit Findings");
            if (!managedRepairAuditReady)
            {
                EditorGUILayout.HelpBox("Run Audit Avatar first. Audit mode does not modify the avatar.", MessageType.Info);
                EndCard();
                return;
            }

            var visible = managedRepairFindings
                .Where(finding => managedRepairShowHealthy || finding.State != ManagedRepairState.Healthy)
                .Where(finding => managedRepairShowForeign || finding.State != ManagedRepairState.Foreign)
                .OrderByDescending(finding => RepairStatePriority(finding.State))
                .ThenBy(finding => finding.Host != null ? GetHierarchyPath(finding.Host.transform) : string.Empty)
                .ToList();

            if (visible.Count == 0)
                EditorGUILayout.HelpBox("No findings match the current filters.", MessageType.Info);

            foreach (var finding in visible)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(RepairStateLabel(finding.State), GUILayout.Width(110f));
                EditorGUILayout.LabelField(finding.Role ?? "Managed Item", cardTitleStyle);
                if (finding.Host != null && GUILayout.Button("Select", GUILayout.Width(58f)))
                {
                    Selection.activeGameObject = finding.Host;
                    EditorGUIUtility.PingObject(finding.Host);
                }
                EditorGUILayout.EndHorizontal();
                if (finding.Host != null)
                    EditorGUILayout.SelectableLabel(GetHierarchyPath(finding.Host.transform), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.LabelField(finding.Summary ?? string.Empty, wrappedLabel);
                if (!string.IsNullOrWhiteSpace(finding.Action))
                    EditorGUILayout.LabelField("Repair: " + finding.Action, wrappedLabel);
                EditorGUILayout.EndVertical();
            }
            EndCard();
        }

        private static int RepairStatePriority(ManagedRepairState state)
        {
            switch (state)
            {
                case ManagedRepairState.Broken: return 6;
                case ManagedRepairState.Defunct: return 5;
                case ManagedRepairState.Repairable: return 4;
                case ManagedRepairState.Outdated: return 3;
                case ManagedRepairState.Healthy: return 2;
                default: return 1;
            }
        }

        private static string RepairStateLabel(ManagedRepairState state)
        {
            switch (state)
            {
                case ManagedRepairState.Healthy: return "✓ Healthy";
                case ManagedRepairState.Outdated: return "! Outdated";
                case ManagedRepairState.Repairable: return "! Repairable";
                case ManagedRepairState.Broken: return "✕ Broken";
                case ManagedRepairState.Defunct: return "✕ Defunct";
                default: return "— Foreign";
            }
        }

        private void AuditManagedSystems()
        {
            managedRepairFindings.Clear();
            managedRepairAuditReady = false;
            if (avatarRoot == null)
            {
                managedRepairSummary = "Assign an Avatar Descriptor or Avatar Root before auditing.";
                return;
            }

            var senderType = FindType(SenderTypeName);
            var receiverType = FindType(ReceiverTypeName);
            if (senderType == null || receiverType == null)
            {
                managedRepairSummary = "VRChat Contact component types are unavailable. Update the SDK before repairing.";
                return;
            }

            var allTransforms = avatarRoot.GetComponentsInChildren<Transform>(true);
            foreach (var transform in allTransforms)
            {
                var host = transform.gameObject;
                var components = host.GetComponents<Component>();
                if (components.Any(component => component == null) && IsStrictlyStoriesManagedHost(host))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        State = ManagedRepairState.Broken,
                        Kind = ManagedRepairKind.MissingManagedComponent,
                        Role = "Missing Script",
                        Summary = "A Stories-managed object contains a missing component. Its original type cannot be recovered automatically from Unity's missing-script placeholder.",
                        Action = "Restore the missing package/script, then rerun the audit. The object and its transform will not be deleted."
                    });
                }

                foreach (var receiver in host.GetComponents(receiverType).Cast<Component>())
                    AuditManagedReceiver(receiver);
                foreach (var sender in host.GetComponents(senderType).Cast<Component>())
                    AuditManagedSender(sender);

                AuditDuplicateManagedContacts(host, senderType, false);
                AuditDuplicateManagedContacts(host, receiverType, true);

                if (!IsStrictlyStoriesManagedHost(host) &&
                    (host.GetComponents(senderType).Length > 0 || host.GetComponents(receiverType).Length > 0))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        State = ManagedRepairState.Foreign,
                        Kind = ManagedRepairKind.None,
                        Role = "Foreign Contact Object",
                        Summary = "Contact components exist, but the object does not match the strict Stories ownership signature.",
                        Action = "No action. This object is intentionally untouched."
                    });
                }
            }

            managedRepairAuditReady = true;
            var healthy = managedRepairFindings.Count(finding => finding.State == ManagedRepairState.Healthy);
            var repairable = managedRepairFindings.Count(finding => finding.CanAutoRepair);
            var broken = managedRepairFindings.Count(finding => finding.State == ManagedRepairState.Broken);
            var foreign = managedRepairFindings.Count(finding => finding.State == ManagedRepairState.Foreign);
            managedRepairSummary = "Audit complete — Healthy: " + healthy +
                                   " • Safe repairs: " + repairable +
                                   " • Manual review: " + broken +
                                   " • Foreign / untouched: " + foreign + ".";
            managedRepairPreview = "Audit complete. Press Preview Repair to list the exact safe actions.";
            Log(managedRepairSummary);
        }

        private static bool IsLegacySpellReceiverTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) ||
                !tag.StartsWith(LegacySpellTagPrefix, StringComparison.Ordinal))
                return false;

            // The current compact transport deliberately uses tags beginning with
            // "SoY Spell" too: "SoY Spell Active" and "SoY Spell Bit 0-7".
            // Only the retired numeric tags (for example "SoY Spell 24") are legacy.
            var suffix = tag.Substring(LegacySpellTagPrefix.Length).Trim();
            int spellId;
            return int.TryParse(suffix, out spellId) && spellId >= 1 && spellId <= 255;
        }

        private void AuditManagedReceiver(Component receiver)
        {
            if (receiver == null)
                return;
            var host = receiver.gameObject;
            var tags = ReadCollisionTags(receiver).ToList();
            var parameter = ReadStringMember(receiver, "parameter", "Parameter");
            var isLegacySpell = parameter == "SoY_SpellType" || tags.Any(IsLegacySpellReceiverTag);
            if (isLegacySpell && IsStrictlyStoriesManagedComponent(receiver))
            {
                // A legacy host can contain many retired receiver components. Report
                // and repair it once so the audit does not flood the UI with the same
                // GameObject and the migration runs only one host-level transaction.
                if (!managedRepairFindings.Any(finding =>
                    finding.Host == host && finding.Kind == ManagedRepairKind.LegacySpellReceiver))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = ManagedRepairState.Defunct,
                        Kind = ManagedRepairKind.LegacySpellReceiver,
                        Role = "Legacy Spell Receiver",
                        Summary = "The old Constant Int spell receiver cannot transmit IDs correctly on current SDK behavior.",
                        Action = "Remove only the defunct receiver and install the compact Active + 8-bit spell bus on this same GameObject."
                    });
                }
                return;
            }

            string expectedParameter;
            if (TryGetCanonicalIncomingParameter(tags, out expectedParameter))
            {
                if (!string.Equals(parameter, expectedParameter, StringComparison.Ordinal))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = ManagedRepairState.Repairable,
                        Kind = ManagedRepairKind.IncomingReceiverMapping,
                        Role = "Incoming Receiver Mapping",
                        Summary = "Canonical tag maps to '" + expectedParameter + "', but this receiver currently writes '" + parameter + "'.",
                        Action = "Correct the receiver parameter in place without replacing its GameObject or constraint."
                    });
                }
                else if (IsStrictlyStoriesManagedComponent(receiver))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = ManagedRepairState.Healthy,
                        Kind = ManagedRepairKind.None,
                        Role = "Incoming Receiver",
                        Summary = "Canonical tag and parameter mapping are valid.",
                        Action = string.Empty
                    });
                }
            }
        }

        private void AuditManagedSender(Component sender)
        {
            if (sender == null)
                return;
            var tags = ReadCollisionTags(sender).ToList();
            AttackTier tier;
            if (!TryGetAttackTier(tags, out tier))
                return;
            if (!IsStrictlyStoriesManagedComponent(sender))
                return;

            var shouldBlock = tier != AttackTier.Critical;
            var hasBlockable = tags.Contains(TagBlockable);
            if (shouldBlock != hasBlockable)
            {
                managedRepairFindings.Add(new ManagedRepairFinding
                {
                    Host = sender.gameObject,
                    Component = sender,
                    State = ManagedRepairState.Repairable,
                    Kind = ManagedRepairKind.AttackTagContract,
                    Role = tier + " Attack Sender",
                    Summary = shouldBlock
                        ? "Weak, Average, and Strong attacks must include Blockable."
                        : "Critical attacks must never include Blockable.",
                    Action = "Repair only the canonical attack tags while preserving all other sender settings."
                });
            }
            else
            {
                managedRepairFindings.Add(new ManagedRepairFinding
                {
                    Host = sender.gameObject,
                    Component = sender,
                    State = ManagedRepairState.Healthy,
                    Kind = ManagedRepairKind.None,
                    Role = tier + " Attack Sender",
                    Summary = "Canonical attack and blocking tags are valid.",
                    Action = string.Empty
                });
            }
        }

        private void AuditDuplicateManagedContacts(GameObject host, Type componentType, bool receiver)
        {
            if (host == null || componentType == null || !IsStrictlyStoriesManagedHost(host))
                return;
            var components = host.GetComponents(componentType).Cast<Component>().ToArray();
            var groups = components.GroupBy(component =>
            {
                var tags = string.Join("|", ReadCollisionTags(component).OrderBy(value => value, StringComparer.Ordinal).ToArray());
                var parameter = receiver ? ReadStringMember(component, "parameter", "Parameter") : string.Empty;
                return tags + "::" + parameter;
            });
            foreach (var group in groups.Where(group => group.Count() > 1))
            {
                foreach (var duplicate in group.Skip(1))
                {
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = duplicate,
                        State = ManagedRepairState.Outdated,
                        Kind = ManagedRepairKind.DuplicateManagedContact,
                        Role = "Duplicate " + componentType.Name,
                        Summary = "An identical Stories-managed Contact component already exists on this object.",
                        Action = "Remove only this duplicate component."
                    });
                }
            }
        }

        private static bool TryGetCanonicalIncomingParameter(IEnumerable<string> tags, out string parameter)
        {
            var set = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (set.Contains(TagWeak)) { parameter = "SoY_HitWeak"; return true; }
            if (set.Contains(TagAverage)) { parameter = "SoY_HitAverage"; return true; }
            if (set.Contains(TagStrong)) { parameter = "SoY_HitStrong"; return true; }
            if (set.Contains(TagCritical)) { parameter = "SoY_HitCritical"; return true; }
            if (set.Contains("Burn")) { parameter = "SoY_DebuffBurn"; return true; }
            if (set.Contains("Silence")) { parameter = "SoY_DebuffSilence"; return true; }
            if (set.Contains("Freeze")) { parameter = "SoY_DebuffFreeze"; return true; }
            if (set.Contains("Bind")) { parameter = "SoY_DebuffBind"; return true; }
            if (set.Contains("Bleed")) { parameter = "SoY_DebuffBleed"; return true; }
            if (set.Contains(SpellActiveTag)) { parameter = SpellActiveParameter; return true; }
            for (var bit = 0; bit < SpellBitCount; bit++)
                if (set.Contains(GetSpellBitTag(bit))) { parameter = GetSpellBitParameter(bit); return true; }
            if (set.Contains(TechnickActiveTag)) { parameter = TechnickActiveParameter; return true; }
            for (var bit = 0; bit < ActionBitCount; bit++)
                if (set.Contains(TechnickBitTagPrefix + bit)) { parameter = TechnickBitParameterPrefix + bit; return true; }
            if (set.Contains(ItemActiveTag)) { parameter = ItemActiveParameter; return true; }
            for (var bit = 0; bit < ActionBitCount; bit++)
                if (set.Contains(ItemBitTagPrefix + bit)) { parameter = ItemBitParameterPrefix + bit; return true; }
            parameter = string.Empty;
            return false;
        }

        private static bool TryGetAttackTier(IEnumerable<string> tags, out AttackTier tier)
        {
            var set = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (set.Contains(TagWeak)) { tier = AttackTier.Weak; return true; }
            if (set.Contains(TagAverage)) { tier = AttackTier.Average; return true; }
            if (set.Contains(TagStrong)) { tier = AttackTier.Strong; return true; }
            if (set.Contains(TagCritical)) { tier = AttackTier.Critical; return true; }
            tier = AttackTier.Average;
            return false;
        }

        private static bool IsStrictlyStoriesManagedHost(GameObject host)
        {
            if (host == null)
                return false;
            var name = host.name ?? string.Empty;
            if (name.StartsWith("Stories ", StringComparison.Ordinal) ||
                name.StartsWith("[SoY ", StringComparison.Ordinal) ||
                name == RaycastRootName)
                return true;
            return host.GetComponents<Component>()
                .Where(component => component != null)
                .Any(IsStrictlyStoriesManagedComponent);
        }

        private static bool IsStrictlyStoriesManagedComponent(Component component)
        {
            if (component == null)
                return false;
            var tags = ReadCollisionTags(component);
            if (tags.Any(tag =>
                tag == TagWeak || tag == TagAverage || tag == TagStrong || tag == TagCritical ||
                tag == TagBlockable || tag == TagHitBlocked || tag == SpellActiveTag ||
                tag.StartsWith(SpellBitTagPrefix, StringComparison.Ordinal) ||
                IsLegacySpellReceiverTag(tag) ||
                tag == TechnickActiveTag || tag.StartsWith(TechnickBitTagPrefix, StringComparison.Ordinal) ||
                tag == ItemActiveTag || tag.StartsWith(ItemBitTagPrefix, StringComparison.Ordinal) ||
                tag == CasterAllyTag || tag == CasterEnemyTag))
                return true;
            var parameter = ReadStringMember(component, "parameter", "Parameter");
            return !string.IsNullOrEmpty(parameter) &&
                   (parameter.StartsWith("SoY_", StringComparison.Ordinal) || parameter == TagHitBlocked);
        }

        private void PreviewManagedRepair()
        {
            if (!managedRepairAuditReady)
                AuditManagedSystems();
            var actions = managedRepairFindings.Where(finding => finding.CanAutoRepair).ToList();
            if (actions.Count == 0)
            {
                managedRepairPreview = "No safe automatic repairs are currently required.";
                return;
            }
            var builder = new StringBuilder();
            builder.AppendLine(actions.Count + " safe repair action(s) will be performed:");
            foreach (var finding in actions.Take(30))
                builder.AppendLine("• " + GetHierarchyPath(finding.Host.transform) + " — " + finding.Action);
            if (actions.Count > 30)
                builder.AppendLine("• …and " + (actions.Count - 30) + " additional action(s).");
            builder.AppendLine();
            builder.AppendLine("Every affected object is snapshotted before changes. Parent, sibling index, transforms, active state, constraints, and prefab modifications are preserved.");
            managedRepairPreview = builder.ToString();
        }

        private void RunManagedRepair(bool selectedOnly)
        {
            if (!managedRepairAuditReady)
                AuditManagedSystems();
            if (avatarRoot == null)
                return;

            var selected = Selection.activeGameObject;
            var findings = managedRepairFindings
                .Where(finding => finding.CanAutoRepair)
                .Where(finding => !selectedOnly || (selected != null && finding.Host == selected))
                .ToList();
            if (findings.Count == 0)
            {
                EditorUtility.DisplayDialog("Stories OSC Repair", selectedOnly
                    ? "The selected object has no safe automatic repair actions."
                    : "No safe automatic repairs are required.", "OK");
                return;
            }

            if (managedRepairConfirmDestructive && findings.Any(finding =>
                finding.Kind == ManagedRepairKind.LegacySpellReceiver ||
                finding.Kind == ManagedRepairKind.DuplicateManagedContact))
            {
                var confirmed = EditorUtility.DisplayDialog(
                    "Confirm Managed Repair",
                    "The repair will remove " + findings.Count(finding => finding.Kind == ManagedRepairKind.LegacySpellReceiver || finding.Kind == ManagedRepairKind.DuplicateManagedContact) +
                    " defunct or duplicate Stories-managed component(s) after creating a migration snapshot. Unrelated components and objects will not be changed.",
                    "Create Snapshot & Repair",
                    "Cancel");
                if (!confirmed)
                    return;
            }

            var snapshots = CaptureRepairRuntimeSnapshots(findings.Select(finding => finding.Host).Distinct());
            lastRepairSnapshotPath = WriteRepairTransactionManifest(findings, snapshots);
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Stories OSC Managed-System Repair");
            lastRepairUndoGroup = group;

            try
            {
                foreach (var snapshot in snapshots)
                    RegisterRepairUndo(snapshot.Host);
                foreach (var finding in findings)
                    ApplyManagedRepairFinding(finding);
                foreach (var snapshot in snapshots)
                {
                    RestoreRepairRuntimeSnapshot(snapshot);
                    ValidateRepairRuntimeSnapshot(snapshot);
                }

                AssetDatabase.SaveAssets();
                PrefabUtility.RecordPrefabInstancePropertyModifications(avatarRoot.transform);
                Undo.CollapseUndoOperations(group);
                AuditManagedSystems();
                managedRepairPreview = "Repair committed successfully. Use Roll Back Last Repair to undo the complete transaction during this Unity session.";
                Log("Managed-system repair completed: " + findings.Count + " action(s). Snapshot: " + lastRepairSnapshotPath);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                lastRepairUndoGroup = -1;
                managedRepairPreview = "Repair failed and was rolled back: " + exception.Message;
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Stories OSC Repair Rolled Back", managedRepairPreview, "OK");
            }
        }

        private sealed class ContactGeometrySnapshot
        {
            public ContactShape Shape = ContactShape.Sphere;
            public float Radius = 0.1f;
            public float Height = 0.2f;
            public Vector3 Size = Vector3.one * 0.1f;
            public Vector3 Position = Vector3.zero;
            public Vector3 EulerRotation = Vector3.zero;
            public Transform RootTransform;
        }

        private sealed class RepairRuntimeSnapshot
        {
            public GameObject Host;
            public Transform Parent;
            public int SiblingIndex;
            public bool ActiveSelf;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public readonly List<KeyValuePair<Component, string>> Constraints = new List<KeyValuePair<Component, string>>();
        }

        private static List<RepairRuntimeSnapshot> CaptureRepairRuntimeSnapshots(IEnumerable<GameObject> hosts)
        {
            var result = new List<RepairRuntimeSnapshot>();
            foreach (var host in (hosts ?? Enumerable.Empty<GameObject>()).Where(value => value != null).Distinct())
            {
                var transform = host.transform;
                var snapshot = new RepairRuntimeSnapshot
                {
                    Host = host,
                    Parent = transform.parent,
                    SiblingIndex = transform.GetSiblingIndex(),
                    ActiveSelf = host.activeSelf,
                    LocalPosition = transform.localPosition,
                    LocalRotation = transform.localRotation,
                    LocalScale = transform.localScale,
                    WorldPosition = transform.position,
                    WorldRotation = transform.rotation
                };
                foreach (var component in host.GetComponents<Component>().Where(IsConstraintComponent))
                {
                    try
                    {
                        snapshot.Constraints.Add(new KeyValuePair<Component, string>(component, EditorJsonUtility.ToJson(component, true)));
                    }
                    catch
                    {
                        // Undo still protects unsupported SDK constraint serializers.
                    }
                }
                result.Add(snapshot);
            }
            return result;
        }

        private static bool IsConstraintComponent(Component component)
        {
            if (component == null)
                return false;
            var name = component.GetType().Name;
            return name.IndexOf("Constraint", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RegisterRepairUndo(GameObject host)
        {
            if (host == null)
                return;
            Undo.RegisterFullObjectHierarchyUndo(host, "Preserve Stories Managed Object");
            Undo.RecordObject(host, "Preserve Stories Managed Object State");
            Undo.RecordObject(host.transform, "Preserve Stories Managed Transform");
            foreach (var component in host.GetComponents<Component>().Where(component => component != null))
                Undo.RecordObject(component, "Preserve Stories Managed Component");
        }

        private static void RestoreRepairRuntimeSnapshot(RepairRuntimeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Host == null)
                return;
            var transform = snapshot.Host.transform;
            if (transform.parent != snapshot.Parent)
                Undo.SetTransformParent(transform, snapshot.Parent, "Restore Stories Managed Parent");
            transform.SetSiblingIndex(Mathf.Clamp(snapshot.SiblingIndex, 0, transform.parent != null ? transform.parent.childCount - 1 : snapshot.SiblingIndex));
            transform.localPosition = snapshot.LocalPosition;
            transform.localRotation = snapshot.LocalRotation;
            transform.localScale = snapshot.LocalScale;
            snapshot.Host.SetActive(snapshot.ActiveSelf);
            foreach (var pair in snapshot.Constraints)
            {
                if (pair.Key == null || string.IsNullOrEmpty(pair.Value))
                    continue;
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
                    InvokeNoArg(pair.Key, "ApplyConfigurationChanges");
                    EditorUtility.SetDirty(pair.Key);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pair.Key);
                }
                catch
                {
                    // Unity Undo remains the authoritative rollback if an SDK constraint refuses JSON restore.
                }
            }
            EditorUtility.SetDirty(snapshot.Host);
            PrefabUtility.RecordPrefabInstancePropertyModifications(snapshot.Host);
            PrefabUtility.RecordPrefabInstancePropertyModifications(snapshot.Host.transform);
        }

        private static void ValidateRepairRuntimeSnapshot(RepairRuntimeSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Host == null)
                throw new InvalidOperationException("An affected managed object was deleted during repair.");
            var transform = snapshot.Host.transform;
            if (transform.parent != snapshot.Parent)
                throw new InvalidOperationException("Repair changed the parent of '" + snapshot.Host.name + "'.");
            if ((transform.localPosition - snapshot.LocalPosition).sqrMagnitude > 0.0000001f)
                throw new InvalidOperationException("Repair changed the local position of '" + snapshot.Host.name + "'.");
            if (Quaternion.Angle(transform.localRotation, snapshot.LocalRotation) > 0.001f)
                throw new InvalidOperationException("Repair changed the local rotation of '" + snapshot.Host.name + "'.");
            if ((transform.localScale - snapshot.LocalScale).sqrMagnitude > 0.0000001f)
                throw new InvalidOperationException("Repair changed the local scale of '" + snapshot.Host.name + "'.");
            if (snapshot.Host.activeSelf != snapshot.ActiveSelf)
                throw new InvalidOperationException("Repair changed the active state of '" + snapshot.Host.name + "'.");
        }

        private void ApplyManagedRepairFinding(ManagedRepairFinding finding)
        {
            if (finding == null || finding.Host == null)
                return;
            switch (finding.Kind)
            {
                case ManagedRepairKind.LegacySpellReceiver:
                    RepairLegacySpellReceiverInPlace(finding);
                    break;
                case ManagedRepairKind.AttackTagContract:
                    RepairAttackTagContract(finding.Component);
                    break;
                case ManagedRepairKind.IncomingReceiverMapping:
                    RepairIncomingReceiverMapping(finding.Component);
                    break;
                case ManagedRepairKind.DuplicateManagedContact:
                    if (finding.Component != null)
                        Undo.DestroyObjectImmediate(finding.Component);
                    break;
            }
        }

        private void RepairLegacySpellReceiverInPlace(ManagedRepairFinding finding)
        {
            var host = finding.Host;
            var receiverType = FindType(ReceiverTypeName);
            if (receiverType == null)
                throw new InvalidOperationException("VRChat receiver type is unavailable.");

            var legacyReceivers = host.GetComponents(receiverType).Cast<Component>()
                .Where(receiver =>
                {
                    var tags = ReadCollisionTags(receiver);
                    var parameter = ReadStringMember(receiver, "parameter", "Parameter");
                    return parameter == "SoY_SpellType" || tags.Any(IsLegacySpellReceiverTag);
                })
                .ToArray();
            var geometry = legacyReceivers.Length > 0
                ? CaptureContactGeometry(legacyReceivers[0])
                : new ContactGeometrySnapshot
                {
                    Shape = incomingShape,
                    Radius = incomingRadius,
                    Height = incomingHeight,
                    Size = incomingBoxSize,
                    Position = incomingPosition,
                    EulerRotation = incomingRotation,
                    RootTransform = host.transform
                };

            foreach (var receiver in legacyReceivers)
                Undo.DestroyObjectImmediate(receiver);

            if (host.name == "Stories Incoming Spell Contacts")
            {
                var canonicalSibling = host.transform.parent != null
                    ? host.transform.parent.Cast<Transform>().FirstOrDefault(child => child != host.transform && child.name == "Stories Incoming Spell Bus Contacts")
                    : null;
                if (canonicalSibling == null)
                {
                    Undo.RecordObject(host, "Rename Stories Spell Bus Host");
                    host.name = "Stories Incoming Spell Bus Contacts";
                }
            }

            ConfigureIncomingReceiverPreservingGeometry(host, new ReceiverMapping(SpellActiveTag, SpellActiveParameter), geometry);
            for (var bit = 0; bit < SpellBitCount; bit++)
                ConfigureIncomingReceiverPreservingGeometry(host, new ReceiverMapping(GetSpellBitTag(bit), GetSpellBitParameter(bit)), geometry);
            ConfigureIncomingReceiverPreservingGeometry(host, new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"), geometry);
        }

        private static ContactGeometrySnapshot CaptureContactGeometry(Component component)
        {
            var snapshot = new ContactGeometrySnapshot();
            if (component == null)
                return snapshot;

            var shapeName = ReadStringMember(component, "shapeType", "ShapeType");
            ContactShape shape;
            if (Enum.TryParse(shapeName, true, out shape))
                snapshot.Shape = shape;

            var radius = ReadMember(component, "radius", "Radius");
            if (radius is float) snapshot.Radius = Mathf.Max(0.001f, (float)radius);
            var height = ReadMember(component, "height", "Height");
            if (height is float) snapshot.Height = Mathf.Max(snapshot.Radius * 2f, (float)height);
            var size = ReadMember(component, "size", "Size");
            if (size is Vector3) snapshot.Size = ClampPositive((Vector3)size);
            var position = ReadMember(component, "position", "Position");
            if (position is Vector3) snapshot.Position = (Vector3)position;
            var rotation = ReadMember(component, "rotation", "Rotation");
            if (rotation is Quaternion) snapshot.EulerRotation = ((Quaternion)rotation).eulerAngles;
            else if (rotation is Vector3) snapshot.EulerRotation = (Vector3)rotation;
            snapshot.RootTransform = ReadMember(component, "rootTransform", "RootTransform") as Transform ?? component.transform;
            return snapshot;
        }

        private void ConfigureIncomingReceiverPreservingGeometry(GameObject host, ReceiverMapping mapping, ContactGeometrySnapshot geometry)
        {
            var receiver = EnsureContact(host, FindType(ReceiverTypeName), new[] { mapping.Tag }, mapping.Parameter);
            if (receiver == null)
                throw new InvalidOperationException("Could not create repaired receiver for " + mapping.Tag + ".");

            var oldSuppress = suppressContactAttachment;
            suppressContactAttachment = true;
            try
            {
                ConfigureContact(
                    receiver,
                    geometry.Shape,
                    geometry.Radius,
                    geometry.Height,
                    geometry.Size,
                    geometry.Position,
                    geometry.EulerRotation,
                    new[] { mapping.Tag });
                SetTransformMember(receiver, geometry.RootTransform != null ? geometry.RootTransform : host.transform, "rootTransform", "RootTransform");
                SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
                SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
                SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                SetStringMember(receiver, mapping.Parameter, "parameter", "Parameter");
                SetEnumMember(receiver, mapping.ReceiverType, "receiverType", "ReceiverType");
                SetFloatMember(receiver, mapping.Value, "value", "Value");
                SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
                FinishContact(receiver);
            }
            finally
            {
                suppressContactAttachment = oldSuppress;
            }
        }

        private static void RepairAttackTagContract(Component sender)
        {
            if (sender == null)
                return;
            var tags = ReadCollisionTags(sender).ToList();
            AttackTier tier;
            if (!TryGetAttackTier(tags, out tier))
                return;
            tags.RemoveAll(tag => tag == TagBlockable);
            if (tier != AttackTier.Critical)
                tags.Add(TagBlockable);
            SetCollisionTags(sender, tags.Distinct().Take(16).ToArray());
            FinishContact(sender);
        }

        private static void RepairIncomingReceiverMapping(Component receiver)
        {
            if (receiver == null)
                return;
            string parameter;
            if (!TryGetCanonicalIncomingParameter(ReadCollisionTags(receiver), out parameter))
                return;
            SetStringMember(receiver, parameter, "parameter", "Parameter");
            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            FinishContact(receiver);
        }

        private string WriteRepairTransactionManifest(List<ManagedRepairFinding> findings, List<RepairRuntimeSnapshot> snapshots)
        {
            EnsureAssetFolder(RepairSnapshotRoot);
            var manifest = new RepairTransactionManifest
            {
                version = Version,
                build = BuildLabel,
                avatar = avatarRoot != null ? avatarRoot.name : "Unknown",
                timestamp = DateTime.Now.ToString("o"),
                reason = "Managed-system self-healing repair"
            };
            foreach (var finding in findings)
                manifest.plannedActions.Add(GetHierarchyPath(finding.Host.transform) + " — " + finding.Action);
            foreach (var snapshot in snapshots)
            {
                var transform = snapshot.Host.transform;
                manifest.transforms.Add(new RepairTransformSnapshot
                {
                    hierarchyPath = GetHierarchyPath(transform),
                    parentPath = snapshot.Parent != null ? GetHierarchyPath(snapshot.Parent) : string.Empty,
                    siblingIndex = snapshot.SiblingIndex,
                    activeSelf = snapshot.ActiveSelf,
                    localPosition = snapshot.LocalPosition,
                    localRotation = snapshot.LocalRotation,
                    localScale = snapshot.LocalScale,
                    worldPosition = snapshot.WorldPosition,
                    worldRotation = snapshot.WorldRotation
                });
                foreach (var pair in snapshot.Constraints)
                {
                    manifest.constraints.Add(new RepairConstraintSnapshot
                    {
                        hierarchyPath = GetHierarchyPath(transform),
                        componentType = pair.Key != null ? pair.Key.GetType().AssemblyQualifiedName : string.Empty,
                        serializedJson = pair.Value
                    });
                }
            }
            var path = RepairSnapshotRoot + "/" + MakeSafeAssetName(manifest.avatar) + "_v" + Version + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            File.WriteAllText(Path.GetFullPath(path), JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();
            return path;
        }

        private void RollBackLastManagedRepair()
        {
            if (lastRepairUndoGroup < 0)
                return;
            var confirmed = EditorUtility.DisplayDialog(
                "Roll Back Managed Repair",
                "Undo the most recent Stories OSC managed-system repair transaction? This uses Unity's complete Undo snapshot and restores affected components, transforms, constraints, and prefab overrides.",
                "Roll Back",
                "Cancel");
            if (!confirmed)
                return;
            Undo.PerformUndo();
            lastRepairUndoGroup = -1;
            AuditManagedSystems();
            managedRepairPreview = "The most recent repair transaction was rolled back.";
            Log("Rolled back the most recent managed-system repair transaction.");
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;
            var names = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names.ToArray());
        }

        private void DrawAccessibilitySettings()
        {
            BeginCard("Accessibility & Display");
            EditorGUILayout.LabelField("These settings affect this Unity Editor tool. Every status uses text and symbols as well as color.", wrappedLabel);
            EditorGUI.BeginChangeCheck();
            textScaleMode = (TextScaleMode)EditorGUILayout.EnumPopup("Text Size", textScaleMode);
            colorVisionMode = (ColorVisionMode)EditorGUILayout.EnumPopup("Color Mode", colorVisionMode);
            largeControls = EditorGUILayout.ToggleLeft("Larger buttons and navigation controls", largeControls);
            shortVrchatLabels = EditorGUILayout.ToggleLeft("Short, icon-assisted VRChat menu labels", shortVrchatLabels);
            menuNavigationMode = (MenuNavigationMode)EditorGUILayout.EnumPopup("Generated Spell Menu", menuNavigationMode);
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorPreferences();
                InvalidateStyles();
                Repaint();
            }
            EditorGUILayout.Space(5f);
            DrawTagRow("Ready", "Ready", "Uses ✓ plus readable text");
            DrawTagRow("Attention", "Warning", "Uses ! plus readable text");
            DrawTagRow("Missing", "Missing", "Uses ✕ plus readable text");
            EditorGUILayout.HelpBox("VRChat controls the Expressions Menu font size. Large-label mode therefore shortens labels and adds category symbols rather than attempting to change VRChat's fixed font.", MessageType.Info);
            EndCard();

            BeginCard("Generated Menu Navigation");
            EditorGUILayout.LabelField("Combined: purpose shortcuts plus school groups. School First: traditional Magick groups. Purpose First: Healing, Damage, Support, Status, Cleanse, and Utility first.", wrappedLabel);
            using (new EditorGUI.DisabledScope(expressionsMenu == null))
            {
                if (GUILayout.Button("REBUILD STORIES RP MENUS", GUILayout.Height(largeControls ? 46f : 34f)))
                {
                    Undo.RecordObject(expressionsMenu, "Rebuild Accessible Stories RP Menus");
                    AddCombatToggle(expressionsMenu);
                    EditorUtility.SetDirty(expressionsMenu);
                    AssetDatabase.SaveAssets();
                }
            }
            EndCard();
        }

        private void LoadEditorPreferences()
        {
            textScaleMode = (TextScaleMode)EditorPrefs.GetInt("StoriesOSC.v058.TB5.TextScale", (int)TextScaleMode.Normal);
            colorVisionMode = (ColorVisionMode)EditorPrefs.GetInt("StoriesOSC.v058.TB5.ColorVision", (int)ColorVisionMode.Standard);
            menuNavigationMode = (MenuNavigationMode)EditorPrefs.GetInt("StoriesOSC.v058.TB5.MenuNavigation", (int)MenuNavigationMode.Combined);
            largeControls = EditorPrefs.GetBool("StoriesOSC.v058.TB5.LargeControls", false);
            shortVrchatLabels = EditorPrefs.GetBool("StoriesOSC.v058.TB5.ShortMenuLabels", false);
            autoCheckUpdates = EditorPrefs.GetBool("StoriesOSC.v058.TB5.AutoCheckUpdates", true);
            updateChannel = (UpdateChannel)EditorPrefs.GetInt("StoriesOSC.v058.TB5.UpdateChannel", (int)UpdateChannel.Stable);
            deliveryMode = (DeliveryMode)EditorPrefs.GetInt("StoriesOSC.v058.TB5.DeliveryMode", (int)DeliveryMode.Contact);
            raycastUseCustomPrefix = EditorPrefs.GetBool("StoriesOSC.v058.TB5.RaycastCustomPrefix", false);
            contactAttachmentMode = (ContactAttachmentMode)EditorPrefs.GetInt("StoriesOSC.v058.TB6.ContactAttachmentMode", (int)ContactAttachmentMode.ContactObject);
            constraintMaintainOffset = EditorPrefs.GetBool("StoriesOSC.v058.TB6.ConstraintMaintainOffset", true);
            constraintWeight = EditorPrefs.GetFloat("StoriesOSC.v058.TB6.ConstraintWeight", 1f);
        }

        private void SaveEditorPreferences()
        {
            EditorPrefs.SetInt("StoriesOSC.v058.TB5.TextScale", (int)textScaleMode);
            EditorPrefs.SetInt("StoriesOSC.v058.TB5.ColorVision", (int)colorVisionMode);
            EditorPrefs.SetInt("StoriesOSC.v058.TB5.MenuNavigation", (int)menuNavigationMode);
            EditorPrefs.SetBool("StoriesOSC.v058.TB5.LargeControls", largeControls);
            EditorPrefs.SetBool("StoriesOSC.v058.TB5.ShortMenuLabels", shortVrchatLabels);
            EditorPrefs.SetBool("StoriesOSC.v058.TB5.AutoCheckUpdates", autoCheckUpdates);
            EditorPrefs.SetInt("StoriesOSC.v058.TB5.UpdateChannel", (int)updateChannel);
            EditorPrefs.SetInt("StoriesOSC.v058.TB5.DeliveryMode", (int)deliveryMode);
            EditorPrefs.SetBool("StoriesOSC.v058.TB5.RaycastCustomPrefix", raycastUseCustomPrefix);
            EditorPrefs.SetInt("StoriesOSC.v058.TB6.ContactAttachmentMode", (int)contactAttachmentMode);
            EditorPrefs.SetBool("StoriesOSC.v058.TB6.ConstraintMaintainOffset", constraintMaintainOffset);
            EditorPrefs.SetFloat("StoriesOSC.v058.TB6.ConstraintWeight", constraintWeight);
        }

        private string BuildSpellMenuLabel(SpellDefinition spell)
        {
            if (!shortVrchatLabels)
                return spell.Name;
            string prefix;
            switch (spell.Category)
            {
                case SpellCategory.Healing:
                case SpellCategory.Revival: prefix = "✚ "; break;
                case SpellCategory.Offensive: prefix = "◆ "; break;
                case SpellCategory.Support: prefix = "▲ "; break;
                case SpellCategory.Status: prefix = "● "; break;
                case SpellCategory.Cleanse: prefix = "◇ "; break;
                default: prefix = "■ "; break;
            }
            return prefix + ShortenMenuLabel(spell.Name);
        }

        private string BuildActionMenuLabel(string value)
        {
            return shortVrchatLabels ? ShortenMenuLabel(value) : value;
        }

        private static string ShortenMenuLabel(string value)
        {
            var text = (value ?? string.Empty)
                .Replace("Technick: ", string.Empty)
                .Replace("Magick: ", string.Empty)
                .Trim();
            return text.Length <= 20 ? text : text.Substring(0, 19) + "…";
        }

        private static string GetSpellCategoryDisplayName(SpellCategory category)
        {
            switch (category)
            {
                case SpellCategory.Offensive: return "Damage";
                case SpellCategory.Healing: return "Healing";
                case SpellCategory.Revival: return "Revival";
                case SpellCategory.Cleanse: return "Cleanse";
                case SpellCategory.Support: return "Support";
                case SpellCategory.Status: return "Status & Control";
                default: return "Utility";
            }
        }

        private string CurrentAnimationProfilePath()
        {
            if (avatarDescriptor == null)
                return string.Empty;
            EnsureAssetFolder(ProfileRoot);
            return ProfileRoot + "/" + MakeSafeAssetName(avatarDescriptor.gameObject.name) + "_StoriesOSC_AnimationProfile.json";
        }

        private void LoadAnimationProfile()
        {
            animationProfileAssetPath = CurrentAnimationProfilePath();
            animationProfile = new AvatarAnimationProfile
            {
                avatarName = avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar",
                spellAnimations = new List<SpellAnimationBinding>(),
                technickAnimations = new List<ActionAnimationBinding>(),
                itemAnimations = new List<ActionAnimationBinding>(),
                favorites = new List<MenuFavorite>()
            };
            if (string.IsNullOrWhiteSpace(animationProfileAssetPath))
                return;
            try
            {
                var fullPath = Path.GetFullPath(animationProfileAssetPath);
                if (File.Exists(fullPath))
                {
                    var loaded = JsonUtility.FromJson<AvatarAnimationProfile>(File.ReadAllText(fullPath));
                    if (loaded != null)
                        animationProfile = loaded;
                }
                if (animationProfile.spellAnimations == null)
                    animationProfile.spellAnimations = new List<SpellAnimationBinding>();
                if (animationProfile.technickAnimations == null)
                    animationProfile.technickAnimations = new List<ActionAnimationBinding>();
                if (animationProfile.itemAnimations == null)
                    animationProfile.itemAnimations = new List<ActionAnimationBinding>();
                if (animationProfile.favorites == null)
                    animationProfile.favorites = new List<MenuFavorite>();
            }
            catch (Exception exception)
            {
                Log("Could not load animation profile: " + exception.Message);
            }
        }

        private void SaveAnimationProfile()
        {
            animationProfileAssetPath = CurrentAnimationProfilePath();
            if (string.IsNullOrWhiteSpace(animationProfileAssetPath))
                return;
            try
            {
                animationProfile.avatarName = avatarDescriptor != null ? avatarDescriptor.gameObject.name : animationProfile.avatarName;
                File.WriteAllText(Path.GetFullPath(animationProfileAssetPath), JsonUtility.ToJson(animationProfile, true));
                AssetDatabase.Refresh();
                Log("Saved animation profile: " + animationProfileAssetPath);
            }
            catch (Exception exception)
            {
                Log("Could not save animation profile: " + exception.Message);
            }
        }

        private static AnimationClip LoadClipFromPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static string ClipPath(AnimationClip clip)
        {
            return clip == null ? string.Empty : AssetDatabase.GetAssetPath(clip);
        }

        private void DrawSpellAnimationBuilder()
        {
            BeginCard("Spell Animation Layer Builder");
            EditorGUILayout.LabelField("Builds one managed FX layer with one state per selected spell. Each state can use its own AnimationClip. This is more efficient and easier to maintain than creating a separate FX layer for every spell.", wrappedLabel);
            animationSpellSearch = EditorGUILayout.TextField("Search Spell", animationSpellSearch);
            var available = SpellDefinitions
                .GroupBy(spell => spell.Id)
                .Select(group => group.First())
                .Where(spell => string.IsNullOrWhiteSpace(animationSpellSearch) || spell.Name.IndexOf(animationSpellSearch, StringComparison.OrdinalIgnoreCase) >= 0 || spell.Id.ToString().Contains(animationSpellSearch.Trim()))
                .OrderBy(spell => spell.Id)
                .ToArray();
            if (available.Length > 0)
            {
                animationSpellSelectionIndex = Mathf.Clamp(animationSpellSelectionIndex, 0, available.Length - 1);
                animationSpellSelectionIndex = EditorGUILayout.Popup("Spell", animationSpellSelectionIndex, available.Select(spell => spell.Id + " — " + spell.Name).ToArray());
                animationSpellClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationSpellClip, typeof(AnimationClip), false);
                if (GUILayout.Button("ADD / UPDATE SELECTED SPELL", GUILayout.Height(largeControls ? 42f : 30f)))
                {
                    var selected = available[animationSpellSelectionIndex];
                    var binding = animationProfile.spellAnimations.FirstOrDefault(entry => entry.id == selected.Id);
                    if (binding == null)
                    {
                        binding = new SpellAnimationBinding { id = selected.Id, name = selected.Name, enabled = true };
                        animationProfile.spellAnimations.Add(binding);
                    }
                    binding.name = selected.Name;
                    binding.clipPath = ClipPath(animationSpellClip);
                    binding.enabled = true;
                    SaveAnimationProfile();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No spell matches the search.", MessageType.Info);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Selected Spell States", cardTitleStyle);
            foreach (var binding in animationProfile.spellAnimations.OrderBy(entry => entry.id).ToList())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUI.BeginChangeCheck();
                binding.enabled = EditorGUILayout.Toggle(binding.enabled, GUILayout.Width(20f));
                if (EditorGUI.EndChangeCheck())
                    SaveAnimationProfile();
                EditorGUILayout.LabelField(binding.id + " — " + binding.name, GUILayout.Width(AccessibilityScale > 1f ? 260f : 210f));
                var clip = LoadClipFromPath(binding.clipPath);
                var nextClip = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                if (nextClip != clip)
                {
                    binding.clipPath = ClipPath(nextClip);
                    SaveAnimationProfile();
                }
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    animationProfile.spellAnimations.Remove(binding);
                    SaveAnimationProfile();
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(fxController == null || animationProfile.spellAnimations.Count(entry => entry.enabled) == 0))
            {
                if (GUILayout.Button("BUILD / REPAIR SPELL CAST ANIMATION LAYER", GUILayout.Height(largeControls ? 50f : 38f)))
                    RebuildSpellCastAnimationLayer();
            }
            EditorGUILayout.HelpBox("Missing clips receive safe empty placeholders. Open the generated layer and replace or edit each state's Motion whenever the avatar-specific animation is ready.", MessageType.Info);
            EndCard();
        }

        private void DrawHealthAnimationBuilder()
        {
            BeginCard("Health Animation States");
            EditorGUILayout.LabelField("The managed OSC Vital State layer uses synchronized SoY_HPPercent and SoY_KO values. It creates Full, Half, Critical, and KO states without editing an existing third-party health layer.", wrappedLabel);
            var full = LoadClipFromPath(animationProfile.healthFullClipPath);
            var half = LoadClipFromPath(animationProfile.healthHalfClipPath);
            var critical = LoadClipFromPath(animationProfile.healthCriticalClipPath);
            var ko = LoadClipFromPath(animationProfile.healthKoClipPath);
            var nextFull = (AnimationClip)EditorGUILayout.ObjectField("Full Health Clip", full, typeof(AnimationClip), false);
            var nextHalf = (AnimationClip)EditorGUILayout.ObjectField("Half Health Clip", half, typeof(AnimationClip), false);
            var nextCritical = (AnimationClip)EditorGUILayout.ObjectField("Critical HP Clip", critical, typeof(AnimationClip), false);
            var nextKo = (AnimationClip)EditorGUILayout.ObjectField("KO Clip", ko, typeof(AnimationClip), false);
            if (nextFull != full || nextHalf != half || nextCritical != critical || nextKo != ko)
            {
                animationProfile.healthFullClipPath = ClipPath(nextFull);
                animationProfile.healthHalfClipPath = ClipPath(nextHalf);
                animationProfile.healthCriticalClipPath = ClipPath(nextCritical);
                animationProfile.healthKoClipPath = ClipPath(nextKo);
                SaveAnimationProfile();
            }
            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("BUILD / REPAIR FULL • HALF • CRITICAL • KO LAYER", GUILayout.Height(largeControls ? 50f : 38f)))
                    RebuildHealthAnimationLayer();
            }
            EditorGUILayout.LabelField("Thresholds: Full above 50% • Half from 15% to 50% • Critical below 15% • KO when SoY_KO is true", wrappedLabel);
            EndCard();
        }

        private AnimationClip GetOrCreatePlaceholderClip(string folder, string fileName)
        {
            EnsureAssetFolder(folder);
            var path = folder + "/" + MakeSafeAssetName(fileName) + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                return clip;
            clip = new AnimationClip { frameRate = 60f, name = fileName };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private void RebuildSpellCastAnimationLayer()
        {
            if (fxController == null || !EnsureSafeFxCopy(true))
                return;
            var bindings = animationProfile.spellAnimations.Where(entry => entry.enabled).OrderBy(entry => entry.id).ToList();
            if (bindings.Count == 0)
                return;

            RemoveLayerByName(fxController, SpellCastLayer);
            var layer = CreateHookLayer(fxController, SpellCastLayer);
            var idle = AddHookState(layer.stateMachine, "Idle", new Vector3(120f, 140f));
            var waitForRelease = AddHookState(layer.stateMachine, "Wait For Menu Release", new Vector3(410f, 140f));
            layer.stateMachine.defaultState = idle;
            var release = waitForRelease.AddTransition(idle);
            release.hasExitTime = false;
            release.duration = 0f;
            release.AddCondition(AnimatorConditionMode.Equals, 0f, "SoY_SpellType");

            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AnimationRoot + "/" + avatarName + "/Spell Casts";
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                var state = AddHookState(layer.stateMachine, binding.id + " — " + binding.name,
                    new Vector3(720f + (index % 3) * 280f, 40f + (index / 3) * 110f));
                state.motion = LoadClipFromPath(binding.clipPath) ?? GetOrCreatePlaceholderClip(folder, binding.id + "_" + binding.name);

                var enter = layer.stateMachine.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Equals, binding.id, "SoY_SpellType");

                var finish = state.AddTransition(waitForRelease);
                finish.hasExitTime = true;
                finish.exitTime = 1f;
                finish.duration = 0f;
            }
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Built spell cast animation layer with " + bindings.Count + " selected spell state(s).");
        }

        private void RebuildHealthAnimationLayer()
        {
            if (fxController == null || !EnsureSafeFxCopy(true))
                return;
            RemoveLayerByName(fxController, VitalLayer);
            AddVitalLayer(fxController);
            var layer = fxController.layers.FirstOrDefault(entry => entry.name == VitalLayer);
            if (layer == null || layer.stateMachine == null)
                return;
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AnimationRoot + "/" + avatarName + "/Health";
            foreach (var child in layer.stateMachine.states)
            {
                var state = child.state;
                if (state == null)
                    continue;
                if (state.name == "Full Health")
                    state.motion = LoadClipFromPath(animationProfile.healthFullClipPath) ?? GetOrCreatePlaceholderClip(folder, "Full Health");
                else if (state.name == "Half Health")
                    state.motion = LoadClipFromPath(animationProfile.healthHalfClipPath) ?? GetOrCreatePlaceholderClip(folder, "Half Health");
                else if (state.name == "Critical HP")
                    state.motion = LoadClipFromPath(animationProfile.healthCriticalClipPath) ?? GetOrCreatePlaceholderClip(folder, "Critical HP");
                else if (state.name == "KO")
                    state.motion = LoadClipFromPath(animationProfile.healthKoClipPath) ?? GetOrCreatePlaceholderClip(folder, "KO");
                EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Rebuilt Full, Half, Critical, and KO health animation states.");
        }

        private void DrawGlobalSearchBar()
        {
            BeginCard("Global Search");
            globalSearch = EditorGUILayout.TextField("Find anything", globalSearch);
            if (string.IsNullOrWhiteSpace(globalSearch))
            {
                EditorGUILayout.LabelField("Search spells, Technicks, items, parameters, layers, and generated assets by name or ID.", wrappedLabel);
                EndCard();
                return;
            }

            var query = globalSearch.Trim();
            var shown = 0;
            foreach (var spell in SpellDefinitions.Where(x => x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == query).GroupBy(x => x.Id).Select(x => x.First()).Take(4))
            {
                if (GUILayout.Button("Spell " + spell.Id + " — " + spell.Name))
                {
                    tab = StudioTab.AnimatorSetup;
                    animationSpellSearch = spell.Id.ToString();
                }
                shown++;
            }
            foreach (var action in TechnickDefinitions.Where(x => x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == query).Take(Math.Max(0, 4 - shown)))
            {
                if (GUILayout.Button("Technick " + action.Id + " — " + action.Name))
                {
                    tab = StudioTab.AnimatorSetup;
                    animationTechnickSearch = action.Id.ToString();
                }
                shown++;
            }
            foreach (var action in ItemDefinitions.Where(x => x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == query).Take(Math.Max(0, 4 - shown)))
            {
                if (GUILayout.Button("Item " + action.Id + " — " + action.Name))
                {
                    tab = StudioTab.AnimatorSetup;
                    animationItemSearch = action.Id.ToString();
                }
                shown++;
            }
            foreach (var spec in BridgeParameters.Where(x => x.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).Take(Math.Max(0, 6 - shown)))
            {
                if (GUILayout.Button("Parameter — " + spec.Name))
                {
                    toolsPage = ToolsPage.Status; tab = StudioTab.Tools;
                    parameterSearch = spec.Name;
                }
                shown++;
            }
            if (shown == 0)
                EditorGUILayout.HelpBox("No registered Stories OSC entry matches that search.", MessageType.Info);
            EndCard();
        }

        private void DrawGuidedSetupWizard()
        {
            BeginCard("Guided Setup Wizard");
            EditorGUILayout.LabelField("Step " + ((int)wizardStep + 1) + " of 8 — " + wizardStep, cardTitleStyle);
            var progress = ((int)wizardStep + 1) / 8f;
            var rect = EditorGUILayout.GetControlRect(false, largeControls ? 28f : 20f);
            EditorGUI.ProgressBar(rect, progress, Mathf.RoundToInt(progress * 100f) + "%");

            switch (wizardStep)
            {
                case WizardStep.Avatar:
                    EditorGUILayout.LabelField("Assign the Avatar Descriptor and load its FX, Expression Parameters, menu, and root.", wrappedLabel);
                    using (new EditorGUI.DisabledScope(avatarDescriptor == null))
                        if (GUILayout.Button("LOAD AVATAR CONTEXT")) LoadFromAvatarDescriptor();
                    break;
                case WizardStep.Audit:
                    EditorGUILayout.LabelField("Run preflight, duplicate-script detection, health-system safety checks, parameter budget checks, and SDK capability checks.", wrappedLabel);
                    DrawPreflightCard();
                    break;
                case WizardStep.SafeFx:
                    EditorGUILayout.LabelField("Create and assign a safe FX copy. The original controller remains untouched.", wrappedLabel);
                    using (new EditorGUI.DisabledScope(avatarDescriptor == null || fxController == null))
                        if (GUILayout.Button("CREATE / CONFIRM SAFE FX COPY")) EnsureSafeFxCopy(true);
                    break;
                case WizardStep.Parameters:
                    EditorGUILayout.LabelField("Install and repair the Stories OSC parameter contract and managed hook layers.", wrappedLabel);
                    using (new EditorGUI.DisabledScope(fxController == null))
                        if (GUILayout.Button("INSTALL / REPAIR OSC HOOKS")) InstallAllBridgeHooks();
                    break;
                case WizardStep.Menus:
                    EditorGUILayout.LabelField("Choose a menu layout, configure Quick Access favorites, then generate the Stories RP menu tree.", wrappedLabel);
                    if (GUILayout.Button("OPEN MENU BUILDER")) tab = StudioTab.MenuBuilder;
                    break;
                case WizardStep.Animations:
                    EditorGUILayout.LabelField("Bind avatar-specific Spell, Technick, Item, and Health clips to managed FX layers.", wrappedLabel);
                    if (GUILayout.Button("OPEN ANIMATION BINDINGS")) tab = StudioTab.AnimatorSetup;
                    break;
                case WizardStep.Contacts:
                    EditorGUILayout.LabelField("Create outgoing and incoming Contacts. For ranged attacks use the official VRCRaycast delivery builder instead of a stretched or animated fake projectile.", wrappedLabel);
                    if (GUILayout.Button("OPEN CONTACT AUTHORING"))
                    {
                        contactsPage = ContactsPage.Outgoing;
                        tab = StudioTab.Contacts;
                    }
                    break;
                case WizardStep.Validate:
                    EditorGUILayout.LabelField("Check services, parameter consistency, duplicate scripts, generated layers, menu limits, raycast support, and runtime health-state routing.", wrappedLabel);
                    if (GUILayout.Button("OPEN DIAGNOSTICS"))
                    {
                        toolsPage = ToolsPage.Status;
                        tab = StudioTab.Tools;
                    }
                    break;
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(wizardStep == WizardStep.Avatar))
                if (GUILayout.Button("◀ Back")) wizardStep = (WizardStep)Mathf.Max(0, (int)wizardStep - 1);
            using (new EditorGUI.DisabledScope(wizardStep == WizardStep.Validate))
                if (GUILayout.Button("Continue ▶")) wizardStep = (WizardStep)Mathf.Min((int)WizardStep.Validate, (int)wizardStep + 1);
            EditorGUILayout.EndHorizontal();
            EndCard();
        }

        private void DrawAnimationBindingAudit()
        {
            BeginCard("Animation Binding Audit");
            DrawTagRow("Spell Selector", ParameterStatus("SoY_SpellType", AnimatorControllerParameterType.Int, true), "Remote-visible spell animation routing");
            DrawTagRow("Technick Selector", ParameterStatus("SoY_TechnickType", AnimatorControllerParameterType.Int, true), "Remote-visible Technick animation routing");
            DrawTagRow("Item Selector", ParameterStatus("SoY_ItemType", AnimatorControllerParameterType.Int, true), "Remote-visible Item animation routing");
            DrawTagRow("Health Percent", ParameterStatus("SoY_HPPercent", AnimatorControllerParameterType.Float, true), "Full / Half / Critical routing");
            DrawTagRow("KO", ParameterStatus("SoY_KO", AnimatorControllerParameterType.Bool, true), "KO routing");
            EditorGUILayout.LabelField("The assistant creates only Stories-owned layers and reports local-only selectors, missing expression entries, type mismatches, and duplicate managed layers.", wrappedLabel);
            EndCard();
        }

        private string ParameterStatus(string name, AnimatorControllerParameterType type, bool shouldSync)
        {
            var animator = fxController != null ? fxController.parameters.FirstOrDefault(x => x.name == name) : null;
            var expression = expressionParameters != null && expressionParameters.parameters != null
                ? expressionParameters.parameters.FirstOrDefault(x => x != null && x.name == name)
                : null;
            if (animator == null) return "✕ Missing Animator";
            if (animator.type != type) return "✕ Type " + animator.type;
            if (expression == null) return "! Missing Expression";
            if (shouldSync && !expression.networkSynced) return "! Local Only";
            return "✓ Ready";
        }

        private void DrawTechnickAnimationBuilder()
        {
            DrawActionAnimationBuilder(ActionAnimationKind.Technick, ref animationTechnickSearch, ref animationTechnickSelectionIndex,
                ref animationTechnickClip, animationProfile.technickAnimations, TechnickDefinitions, "SoY_TechnickType", TechnickCastLayer);
        }

        private void DrawItemAnimationBuilder()
        {
            DrawActionAnimationBuilder(ActionAnimationKind.Item, ref animationItemSearch, ref animationItemSelectionIndex,
                ref animationItemClip, animationProfile.itemAnimations, ItemDefinitions, "SoY_ItemType", ItemUseLayer);
        }

        private void DrawActionAnimationBuilder(
            ActionAnimationKind kind,
            ref string search,
            ref int selectionIndex,
            ref AnimationClip selectedClip,
            List<ActionAnimationBinding> bindings,
            ActionDefinition[] definitions,
            string parameterName,
            string layerName)
        {
            BeginCard(kind + " Animation Layer Builder");
            EditorGUILayout.LabelField("Builds one managed layer with one state per selected " + kind + ". Menu release returns the layer to Idle without repeating the animation.", wrappedLabel);
            search = EditorGUILayout.TextField("Search", search);
            // C# does not allow ref parameters to be captured by lambdas.
            // Copy the current value into an ordinary local before filtering.
            var searchText = search ?? string.Empty;
            var trimmedSearch = searchText.Trim();
            var available = definitions.Where(x =>
                string.IsNullOrWhiteSpace(searchText) ||
                x.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.Id.ToString().Contains(trimmedSearch)).ToArray();
            if (available.Length > 0)
            {
                selectionIndex = Mathf.Clamp(selectionIndex, 0, available.Length - 1);
                selectionIndex = EditorGUILayout.Popup(kind.ToString(), selectionIndex, available.Select(x => x.Id + " — " + x.Name).ToArray());
                selectedClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", selectedClip, typeof(AnimationClip), false);
                if (GUILayout.Button("ADD / UPDATE SELECTED " + kind.ToString().ToUpperInvariant()))
                {
                    var selected = available[selectionIndex];
                    var binding = bindings.FirstOrDefault(x => x.id == selected.Id);
                    if (binding == null)
                    {
                        binding = new ActionAnimationBinding { id = selected.Id, name = selected.Name, enabled = true };
                        bindings.Add(binding);
                    }
                    binding.name = selected.Name;
                    binding.clipPath = ClipPath(selectedClip);
                    binding.enabled = true;
                    SaveAnimationProfile();
                }
            }
            foreach (var binding in bindings.OrderBy(x => x.id).ToList())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUI.BeginChangeCheck();
                binding.enabled = EditorGUILayout.Toggle(binding.enabled, GUILayout.Width(20f));
                if (EditorGUI.EndChangeCheck()) SaveAnimationProfile();
                EditorGUILayout.LabelField(binding.id + " — " + binding.name, GUILayout.Width(AccessibilityScale > 1f ? 260f : 210f));
                var clip = LoadClipFromPath(binding.clipPath);
                var nextClip = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                if (nextClip != clip) { binding.clipPath = ClipPath(nextClip); SaveAnimationProfile(); }
                if (GUILayout.Button("Open", GUILayout.Width(55f)) && clip != null) { Selection.activeObject = clip; EditorGUIUtility.PingObject(clip); }
                if (GUILayout.Button("Remove", GUILayout.Width(70f))) { bindings.Remove(binding); SaveAnimationProfile(); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            using (new EditorGUI.DisabledScope(fxController == null || bindings.Count(x => x.enabled) == 0))
                if (GUILayout.Button("BUILD / REPAIR " + kind.ToString().ToUpperInvariant() + " ANIMATION LAYER", GUILayout.Height(largeControls ? 50f : 38f)))
                    RebuildActionAnimationLayer(kind, parameterName, layerName, bindings);
            EndCard();
        }

        private void RebuildActionAnimationLayer(ActionAnimationKind kind, string parameterName, string layerName, List<ActionAnimationBinding> sourceBindings)
        {
            if (fxController == null || !EnsureSafeFxCopy(true)) return;
            var bindings = sourceBindings.Where(x => x.enabled).OrderBy(x => x.id).ToList();
            if (bindings.Count == 0) return;
            RemoveLayerByName(fxController, layerName);
            var layer = CreateHookLayer(fxController, layerName);
            var idle = AddHookState(layer.stateMachine, "Idle", new Vector3(120f, 140f));
            var wait = AddHookState(layer.stateMachine, "Wait For Menu Release", new Vector3(410f, 140f));
            layer.stateMachine.defaultState = idle;
            var release = wait.AddTransition(idle);
            release.hasExitTime = false; release.duration = 0f;
            release.AddCondition(AnimatorConditionMode.Equals, 0f, parameterName);
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AnimationRoot + "/" + avatarName + "/" + kind + "s";
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                var state = AddHookState(layer.stateMachine, binding.id + " — " + binding.name, new Vector3(720f + (index % 3) * 280f, 40f + (index / 3) * 110f));
                state.motion = LoadClipFromPath(binding.clipPath) ?? GetOrCreatePlaceholderClip(folder, binding.id + "_" + binding.name);
                var enter = layer.stateMachine.AddAnyStateTransition(state);
                enter.hasExitTime = false; enter.duration = 0f; enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Equals, binding.id, parameterName);
                var finish = state.AddTransition(wait);
                finish.hasExitTime = true; finish.exitTime = 1f; finish.duration = 0f;
            }
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Built " + kind + " animation layer with " + bindings.Count + " state(s).");
        }

        private void DrawMenuBuilder()
        {
            BeginCard("VRChat Menu Builder");
            menuNavigationMode = (MenuNavigationMode)EditorGUILayout.EnumPopup("Navigation Layout", menuNavigationMode);
            shortVrchatLabels = EditorGUILayout.ToggleLeft("Use short accessibility labels", shortVrchatLabels);
            EditorGUILayout.LabelField("Layouts: Combined, School First, Purpose First, Favorites First, and Compact Combat. Generated pages reserve Previous/Next slots and never exceed eight controls.", wrappedLabel);
            EndCard();

            BeginCard("Quick Access Favorites");
            menuBuilderSearch = EditorGUILayout.TextField("Search actions", menuBuilderSearch);
            if (string.IsNullOrWhiteSpace(menuBuilderSearch))
            {
                EditorGUILayout.LabelField("Type a name or numeric ID to add an action to Quick Access.", wrappedLabel);
            }
            else
            {
                var spells = SpellDefinitions.Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).GroupBy(x => x.Id).Select(x => x.First()).Take(5).ToArray();
                foreach (var spell in spells)
                    if (GUILayout.Button("+ Spell " + spell.Id + " — " + spell.Name)) AddFavorite("spell", spell.Id, spell.Name);
                foreach (var action in TechnickDefinitions.Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).Take(5))
                    if (GUILayout.Button("+ Technick " + action.Id + " — " + action.Name)) AddFavorite("technick", action.Id, action.Name);
                foreach (var action in ItemDefinitions.Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).Take(5))
                    if (GUILayout.Button("+ Item " + action.Id + " — " + action.Name)) AddFavorite("item", action.Id, action.Name);
            }

            EditorGUILayout.LabelField("Current Quick Access (maximum seven actions)", cardTitleStyle);
            foreach (var favorite in animationProfile.favorites.Take(7).ToList())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(favorite.kind.ToUpperInvariant() + " " + favorite.id + " — " + favorite.name);
                if (GUILayout.Button("▲", GUILayout.Width(28f))) MoveFavorite(favorite, -1);
                if (GUILayout.Button("▼", GUILayout.Width(28f))) MoveFavorite(favorite, 1);
                if (GUILayout.Button("Remove", GUILayout.Width(70f))) { animationProfile.favorites.Remove(favorite); SaveAnimationProfile(); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (animationProfile.favorites.Count > 7)
                EditorGUILayout.HelpBox("Only the first seven favorites are generated. Move or remove entries to change the live menu.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(expressionsMenu == null))
                if (GUILayout.Button("GENERATE / REPAIR STORIES RP MENUS", GUILayout.Height(largeControls ? 50f : 38f)))
                {
                    AddCombatToggle(expressionsMenu);
                    SaveEditorPreferences();
                }
            DrawMenuPreview();
            EndCard();
        }

        private void AddFavorite(string kind, int id, string name)
        {
            if (animationProfile.favorites.Any(x => x.kind == kind && x.id == id)) return;
            animationProfile.favorites.Add(new MenuFavorite { kind = kind, id = id, name = name });
            SaveAnimationProfile();
        }

        private void MoveFavorite(MenuFavorite favorite, int delta)
        {
            var index = animationProfile.favorites.IndexOf(favorite);
            var next = Mathf.Clamp(index + delta, 0, animationProfile.favorites.Count - 1);
            if (index == next) return;
            animationProfile.favorites.RemoveAt(index);
            animationProfile.favorites.Insert(next, favorite);
            SaveAnimationProfile();
        }

        private void DrawMenuPreview()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Eight-slot Preview", cardTitleStyle);
            var labels = new List<string> { "RP Combat", "Enemy Mode", "Raycast Fire" };
            if (menuNavigationMode == MenuNavigationMode.FavoritesFirst && animationProfile.favorites.Count > 0) labels.Add("Quick Access");
            labels.Add("Spells");
            labels.Add(menuNavigationMode == MenuNavigationMode.CompactCombat ? "Actions" : "Technicks");
            if (menuNavigationMode != MenuNavigationMode.CompactCombat) labels.Add("Items");
            labels.Add("Status Gauges");
            if (menuNavigationMode != MenuNavigationMode.FavoritesFirst && animationProfile.favorites.Count > 0) labels.Add("Quick Access");
            for (var i = 0; i < 8; i += 2)
            {
                EditorGUILayout.BeginHorizontal();
                for (var j = 0; j < 2; j++)
                {
                    var index = i + j;
                    EditorGUILayout.HelpBox(index < labels.Count ? labels[index] : "— Empty —", MessageType.None);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void BuildQuickAccessMenu(VRCExpressionsMenu quickMenu)
        {
            Undo.RecordObject(quickMenu, "Build Stories Quick Access");
            quickMenu.controls = new List<VRCExpressionsMenu.Control>();
            foreach (var favorite in animationProfile.favorites.Take(7))
            {
                var parameter = favorite.kind == "spell" ? "SoY_SpellType" : favorite.kind == "technick" ? "SoY_TechnickType" : "SoY_ItemType";
                quickMenu.controls.Add(new VRCExpressionsMenu.Control
                {
                    name = BuildActionMenuLabel(favorite.name),
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter },
                    value = favorite.id
                });
            }
            EditorUtility.SetDirty(quickMenu);
        }

        private void DrawDiagnosticsWorkspace()
        {
            DrawParameterSyncInspector();
            DrawManagedHealthAudit();
            DrawRuntimeTestPanel();
            DrawDuplicateScriptAudit();
            DrawUpdaterCard();
            DrawDiagnostics();
        }

        private void DrawParameterSyncInspector()
        {
            BeginCard("Parameter Sync Inspector");
            parameterSearch = EditorGUILayout.TextField("Filter Parameters", parameterSearch);
            var used = expressionParameters == null ? 0 : SyncedExpressionCost(expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>());
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(rect, used / 256f, used + " / 256 synchronized bits");
            foreach (var spec in BridgeParameters.Where(x => string.IsNullOrWhiteSpace(parameterSearch) || x.Name.IndexOf(parameterSearch, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var animator = fxController != null ? fxController.parameters.FirstOrDefault(x => x.name == spec.Name) : null;
                var expression = expressionParameters != null && expressionParameters.parameters != null ? expressionParameters.parameters.FirstOrDefault(x => x != null && x.name == spec.Name) : null;
                var role = ClassifyParameter(spec.Name);
                var state = animator == null ? "✕ Animator" : animator.type != spec.AnimatorType ? "✕ Type" : expression == null ? "! Expression" : expression.networkSynced != spec.NetworkSynced ? "! Sync Flag" : "✓";
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(state, GUILayout.Width(80f));
                EditorGUILayout.LabelField(spec.Name, GUILayout.Width(220f));
                EditorGUILayout.LabelField(spec.AnimatorType.ToString(), GUILayout.Width(70f));
                EditorGUILayout.LabelField(role, GUILayout.Width(150f));
                EditorGUILayout.LabelField(spec.NetworkSynced ? ExpressionParameterCost(spec.ExpressionType) + " bits" : "Local", GUILayout.Width(60f));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(fxController == null))
                if (GUILayout.Button("REPAIR ANIMATOR PARAMETERS")) { AddMissingAnimatorParameters(fxController); AssetDatabase.SaveAssets(); }
            using (new EditorGUI.DisabledScope(expressionParameters == null))
                if (GUILayout.Button("REPAIR EXPRESSION FLAGS")) { Undo.RecordObject(expressionParameters, "Repair Stories OSC Parameters"); AddMissingExpressionParameters(expressionParameters); EditorUtility.SetDirty(expressionParameters); AssetDatabase.SaveAssets(); }
            EditorGUILayout.EndHorizontal();
            EndCard();
        }

        private string ClassifyParameter(string name)
        {
            if (name == "SoY_CombatEnabled" || name == "SoY_IsEnemy") return "Saved Toggle";
            if (name.EndsWith("Type", StringComparison.Ordinal) || name == "SoY_HPPercent" || name == "SoY_CriticalHP" || name == "SoY_KO") return "Networked Animation";
            if (name.Contains("Bit") || name.EndsWith("Active", StringComparison.Ordinal)) return "Incoming Contact Bus";
            if (name.StartsWith("SoY_Hit") || name.StartsWith("SoY_Debuff")) return "Local OSC Input";
            return "Runtime Output";
        }

        private void DrawManagedHealthAudit()
        {
            BeginCard("Managed Health Layer Audit");
            var layer = fxController != null ? fxController.layers.FirstOrDefault(x => x.name == VitalLayer) : null;
            if (layer == null)
            {
                EditorGUILayout.HelpBox("The Stories-managed Vital layer is missing.", MessageType.Warning);
            }
            else
            {
                var conditions = layer.stateMachine.anyStateTransitions.SelectMany(x => x.conditions).Where(x => x.parameter == "SoY_HPPercent").ToArray();
                var has15 = conditions.Any(x => Mathf.Abs(x.threshold - 0.15f) < 0.0001f);
                var has50 = conditions.Any(x => Mathf.Abs(x.threshold - 0.50f) < 0.002f);
                DrawTagRow("Critical Boundary", has15 ? "✓ 15%" : "✕ Not 15%", "Living HP strictly below 15%");
                DrawTagRow("Half Boundary", has50 ? "✓ 50%" : "✕ Not 50%", "Half state from 15% through 50%");
                DrawTagRow("States", string.Join(", ", layer.stateMachine.states.Select(x => x.state.name).ToArray()), "Expected: Full Health, Half Health, Critical HP, KO");
            }
            using (new EditorGUI.DisabledScope(fxController == null))
                if (GUILayout.Button("REBUILD STORIES HEALTH LAYER")) RebuildHealthAnimationLayer();
            EndCard();
        }

        private void DrawRuntimeTestPanel()
        {
            BeginCard("Runtime Health & Action Test Panel");
            EditorGUILayout.LabelField("Enter Play Mode to write temporary parameters to the avatar Animator. Nothing is saved to the controller by these test buttons.", wrappedLabel);
            runtimeTestHpPercent = EditorGUILayout.Slider("HP Percent", runtimeTestHpPercent, 0f, 1f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("100%")) runtimeTestHpPercent = 1f;
            if (GUILayout.Button("50%")) runtimeTestHpPercent = 0.5f;
            if (GUILayout.Button("15%")) runtimeTestHpPercent = 0.15f;
            if (GUILayout.Button("14.9%")) runtimeTestHpPercent = 0.149f;
            if (GUILayout.Button("0%")) runtimeTestHpPercent = 0f;
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("APPLY HEALTH TEST")) ApplyRuntimeHealthTest();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Weak Hit")) SetRuntimeParameter("SoY_DamageReaction", 1);
            if (GUILayout.Button("Healing")) SetRuntimeParameter("SoY_Healing", true);
            if (GUILayout.Button("Reset")) ResetRuntimeTestParameters();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(runtimeTestSummary, wrappedLabel);
            EndCard();
        }

        private Animator RuntimeAnimator()
        {
            return avatarRoot != null ? avatarRoot.GetComponent<Animator>() : null;
        }

        private void ApplyRuntimeHealthTest()
        {
            var animator = RuntimeAnimator();
            var expected = runtimeTestHpPercent <= 0f ? "KO" : runtimeTestHpPercent < 0.15f ? "Critical HP" : runtimeTestHpPercent <= 0.5f ? "Half Health" : "Full Health";
            runtimeTestSummary = "Expected state: " + expected + " at " + (runtimeTestHpPercent * 100f).ToString("0.0") + "% HP.";
            if (!EditorApplication.isPlaying || animator == null)
            {
                runtimeTestSummary += " Enter Play Mode with an Animator on the avatar root to write the parameters.";
                return;
            }
            animator.SetFloat("SoY_HPPercent", runtimeTestHpPercent);
            animator.SetBool("SoY_KO", runtimeTestHpPercent <= 0f);
            animator.SetBool("SoY_CriticalHP", runtimeTestHpPercent > 0f && runtimeTestHpPercent < 0.15f);
        }

        private void SetRuntimeParameter(string name, bool value)
        {
            var animator = RuntimeAnimator();
            if (EditorApplication.isPlaying && animator != null) animator.SetBool(name, value);
        }

        private void SetRuntimeParameter(string name, int value)
        {
            var animator = RuntimeAnimator();
            if (EditorApplication.isPlaying && animator != null) animator.SetInteger(name, value);
        }

        private void ResetRuntimeTestParameters()
        {
            var animator = RuntimeAnimator();
            if (EditorApplication.isPlaying && animator != null)
            {
                animator.SetFloat("SoY_HPPercent", 1f);
                animator.SetBool("SoY_KO", false);
                animator.SetBool("SoY_CriticalHP", false);
                animator.SetBool("SoY_Healing", false);
                animator.SetInteger("SoY_DamageReaction", 0);
                animator.SetInteger("SoY_SpellType", 0);
                animator.SetInteger("SoY_TechnickType", 0);
                animator.SetInteger("SoY_ItemType", 0);
            }
            runtimeTestSummary = "Runtime test parameters reset.";
        }

        private void DrawDuplicateScriptAudit()
        {
            BeginCard("Duplicate Unity Tool Audit");
            var guids = AssetDatabase.FindAssets("StoriesOfYggdrasilOSCContactSystem t:MonoScript");
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
            DrawTagRow("Active scripts", paths.Length == 1 ? "✓ One" : "✕ " + paths.Length, "Unity must compile only one copy of this EditorWindow class");
            foreach (var path in paths) EditorGUILayout.SelectableLabel(path, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (paths.Length > 1) EditorGUILayout.HelpBox("Move numbered copies such as (1).cs or (9).cs outside the project. Do not delete the canonical file or its .meta.", MessageType.Error);
            EndCard();
        }

        private void DrawBackupCenter()
        {
            BeginCard("Backup & Rollback Center");
            EditorGUILayout.LabelField("Major installer actions create manifests for FX and managed-system migrations. Repair snapshots include hierarchy, transforms, active state, constraint serialization, and planned component changes. Restores never modify third-party health layers.", wrappedLabel);
            EditorGUILayout.LabelField(backupStatus, wrappedLabel);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(avatarDescriptor == null || fxController == null))
                if (GUILayout.Button("CREATE BACKUP MANIFEST")) CreateBackupManifest();
            if (GUILayout.Button("OPEN BACKUP FOLDER"))
            {
                EnsureAssetFolder(BackupRoot);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BackupRoot);
                Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("OPEN MIGRATION SNAPSHOT FOLDER"))
            {
                EnsureAssetFolder(RepairSnapshotRoot);
                var migrationAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RepairSnapshotRoot);
                Selection.activeObject = migrationAsset;
                EditorGUIUtility.PingObject(migrationAsset);
            }
            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("REMOVE GENERATED SPELL / TECHNICK / ITEM LAYERS"))
                {
                    RemoveLayerByName(fxController, SpellCastLayer); RemoveLayerByName(fxController, TechnickCastLayer); RemoveLayerByName(fxController, ItemUseLayer);
                    EditorUtility.SetDirty(fxController); AssetDatabase.SaveAssets(); backupStatus = "Removed generated action animation layers.";
                }
                if (GUILayout.Button("REMOVE GENERATED HEALTH LAYER"))
                {
                    RemoveLayerByName(fxController, VitalLayer); EditorUtility.SetDirty(fxController); AssetDatabase.SaveAssets(); backupStatus = "Removed the Stories-managed Vital layer.";
                }
            }
            if (avatarDescriptor != null && GUILayout.Button("RESTORE ORIGINAL FX ASSIGNMENT FROM LATEST MANIFEST")) RestoreOriginalFxFromLatestManifest();
            EndCard();
        }

        private void CreateBackupManifest()
        {
            EnsureAssetFolder(ManifestRoot);
            var manifest = new BackupManifest
            {
                version = Version,
                build = BuildLabel,
                avatar = avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Unknown",
                timestamp = DateTime.Now.ToString("o"),
                workingFxPath = fxController != null ? AssetDatabase.GetAssetPath(fxController) : "",
                originalFxPath = FindOriginalFxPath()
            };
            if (fxController != null)
            {
                manifest.layers.AddRange(fxController.layers.Where(x => x.name.StartsWith("Stories Of Yggdrasil |", StringComparison.Ordinal)).Select(x => x.name));
                manifest.parameters.AddRange(fxController.parameters.Where(x => x.name.StartsWith("SoY_", StringComparison.Ordinal)).Select(x => x.name));
            }
            var path = ManifestRoot + "/" + MakeSafeAssetName(manifest.avatar) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            File.WriteAllText(Path.GetFullPath(path), JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh();
            backupStatus = "Created manifest: " + path;
        }

        private string FindOriginalFxPath()
        {
            if (fxController == null) return "";
            var current = AssetDatabase.GetAssetPath(fxController);
            if (!IsSafeFxCopy(fxController)) return current;
            var name = fxController.name.Replace("_SoY_FX", "");
            var candidates = AssetDatabase.FindAssets(name + " t:AnimatorController").Select(AssetDatabase.GUIDToAssetPath).Where(x => x != current).ToArray();
            return candidates.FirstOrDefault() ?? "";
        }

        private void RestoreOriginalFxFromLatestManifest()
        {
            var guids = AssetDatabase.FindAssets("", new[] { ManifestRoot });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x).ToArray();
            if (paths.Length == 0) { backupStatus = "No backup manifest exists."; return; }
            var manifest = JsonUtility.FromJson<BackupManifest>(File.ReadAllText(Path.GetFullPath(paths[0])));
            var original = manifest != null ? AssetDatabase.LoadAssetAtPath<AnimatorController>(manifest.originalFxPath) : null;
            if (original == null) { backupStatus = "Latest manifest does not reference a valid original FX controller."; return; }
            var layers = avatarDescriptor.baseAnimationLayers;
            var index = Array.FindIndex(layers, x => x.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (index < 0) return;
            Undo.RecordObject(avatarDescriptor, "Restore Original FX Controller");
            var layer = layers[index]; layer.isDefault = false; layer.animatorController = original; layers[index] = layer;
            avatarDescriptor.baseAnimationLayers = layers; EditorUtility.SetDirty(avatarDescriptor); AssetDatabase.SaveAssets();
            fxController = original; backupStatus = "Restored original FX: " + manifest.originalFxPath;
        }

        private void DrawRaycastDeliveryCard()
        {
            BeginCard("Official VRChat Raycast Contact Delivery");
            EditorGUILayout.LabelField("VRCRaycast moves a separate result transform to the player or world hit point. v0.5.9 preserves the TB6.2 delivery model: the selected Stories Contact sender is placed on that result transform and enabled only while the ray is hitting and the matching action is actively fired. This replaces stretched hitscan boxes and fake animated projectiles.", wrappedLabel);
            DrawTagRow("SDK", RaycastTypeAvailable() ? "✓ VRCRaycast Available" : "✕ Requires SDK 3.10.3+", "Official avatar raycast component");
            raycastDistance = Mathf.Clamp(EditorGUILayout.FloatField("Maximum Distance", raycastDistance), 0.1f, 1000f);
            raycastDirection = EditorGUILayout.Vector3Field("Local Direction", raycastDirection);
            raycastImpactRadius = Mathf.Clamp(EditorGUILayout.FloatField("Impact Contact Radius", raycastImpactRadius), 0.01f, 1f);
            raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Collision Target", raycastCollisionTarget);
            raycastApplyRotation = EditorGUILayout.ToggleLeft("Align result to the hit surface", raycastApplyRotation);
            raycastCreateLineRenderer = EditorGUILayout.ToggleLeft("Create optional LineRenderer object (visual setup remains manual)", raycastCreateLineRenderer);
            raycastParameterPrefix = EditorGUILayout.TextField("Animator Parameter Prefix", raycastParameterPrefix);
            using (new EditorGUI.DisabledScope(!RaycastTypeAvailable() || !HasUsableTargets() || avatarRoot == null))
                if (GUILayout.Button("CREATE RAYCAST DELIVERY FOR CURRENT ACTION", GUILayout.Height(largeControls ? 50f : 38f))) CreateRaycastDeliveryForCurrentAction();
            EditorGUILayout.HelpBox("VRCRaycast detects player colliders and positions the impact Contact. Spell, Technick, and Item impacts are gated by their existing selector ID. Attack and Debuff impacts use the shared momentary SoY_RaycastFire button. The Contact still carries the exact canonical tags; Critical attacks remain unblockable.", MessageType.Info);
            EndCard();

            BeginCard("Contact Presets & Utilities");
            contactPreset = (ContactPreset)EditorGUILayout.EnumPopup("Preset", contactPreset);
            if (GUILayout.Button("APPLY PRESET TO CURRENT CONTACT EDITOR")) ApplyContactPreset(contactPreset);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate Selected")) DuplicateSelectedContactObject();
            if (GUILayout.Button("Mirror X")) MirrorSelectedContactObject();
            if (GUILayout.Button("Copy Collider Shape")) CopySelectedColliderToCurrentGeometry();
            EditorGUILayout.EndHorizontal();
            EndCard();
        }

        private Type FindRaycastType()
        {
            foreach (var name in RaycastTypeNames)
            {
                var type = FindType(name);
                if (type != null) return type;
            }
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => { try { return x.GetTypes(); } catch { return Type.EmptyTypes; } }).FirstOrDefault(x => x.Name == "VRCRaycast");
        }

        private bool RaycastTypeAvailable() { return FindRaycastType() != null; }

        private void GetCurrentRaycastActionGate(out string parameter, out int value, out bool integerGate)
        {
            parameter = RaycastFireParameter;
            value = 1;
            integerGate = false;
            if (outgoingKind == OutgoingContactKind.Spell)
            {
                var rows = GetSpellsForSchool(spellSchool)
                    .Where(entry => string.IsNullOrWhiteSpace(spellSearch) ||
                        entry.Name.IndexOf(spellSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(spellSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    parameter = "SoY_SpellType";
                    value = rows[Mathf.Clamp(spellSelectionIndex, 0, rows.Length - 1)].Id;
                    integerGate = true;
                }
            }
            else if (outgoingKind == OutgoingContactKind.Technick)
            {
                var rows = TechnickDefinitions
                    .Where(entry => string.IsNullOrWhiteSpace(technickSearch) ||
                        entry.Name.IndexOf(technickSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(technickSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    parameter = "SoY_TechnickType";
                    value = rows[Mathf.Clamp(technickSelectionIndex, 0, rows.Length - 1)].Id;
                    integerGate = true;
                }
            }
            else if (outgoingKind == OutgoingContactKind.Item)
            {
                var rows = ItemDefinitions
                    .Where(entry => string.IsNullOrWhiteSpace(itemSearch) ||
                        entry.Name.IndexOf(itemSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(itemSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    parameter = "SoY_ItemType";
                    value = rows[Mathf.Clamp(itemSelectionIndex, 0, rows.Length - 1)].Id;
                    integerGate = true;
                }
            }
        }

        private void CreateRaycastDeliveryForCurrentAction()
        {
            var originTarget = explicitTarget != null ? explicitTarget : Selection.activeGameObject;
            var raycastType = FindRaycastType();
            if (originTarget == null || avatarRoot == null || raycastType == null) return;
            var sourcePrefix = raycastUseCustomPrefix ? raycastParameterPrefix : CurrentRaycastSuggestedPrefix();
            var safePrefix = RegexSafeParameter(sourcePrefix);
            string actionGateParameter;
            int actionGateValue;
            bool actionGateIsInteger;
            GetCurrentRaycastActionGate(out actionGateParameter, out actionGateValue, out actionGateIsInteger);
            var origin = CreateContactChild(originTarget, "[SoY Raycast Origin] " + safePrefix, true);
            ApplyRaycastOriginAttachment(origin);
            var resultRoot = avatarRoot.transform.Cast<Transform>().FirstOrDefault(x => x.name == RaycastRootName)?.gameObject ?? CreateContactChild(avatarRoot, RaycastRootName, true);
            var result = CreateContactChild(resultRoot, "[SoY Raycast Result] " + safePrefix, true);
            result.transform.localPosition = Vector3.zero;

            var component = origin.GetComponent(raycastType) as Component ?? Undo.AddComponent(origin, raycastType) as Component;
            if (component == null) return;
            Undo.RecordObject(component, "Configure Stories VRCRaycast");
            SetVector3Member(component, raycastDirection.sqrMagnitude > 0.0001f ? raycastDirection.normalized : Vector3.forward, "raycastDirection", "RaycastDirection", "direction", "Direction");
            SetFloatMember(component, raycastDistance, "distance", "Distance", "maxDistance", "MaxDistance");
            SetBoolMember(component, false, "applyTransformScale", "ApplyTransformScale");
            SetBoolMember(component, raycastApplyRotation, "applyRotation", "ApplyRotation");
            SetTransformMember(component, result.transform, "resultTransform", "ResultTransform");
            SetStringMember(component, safePrefix, "parameter", "Parameter", "parameterName", "ParameterName");
            SetEnumMemberByKeywords(component, raycastCollisionTarget == RaycastCollisionTarget.Players ? new[] { "player" } : new[] { "world", "player" }, "collisionMode", "CollisionMode");
            SetEnumMemberByKeywords(component, new[] { "start" }, "behaviorOnMiss", "BehaviorOnMiss", "positioningOnMiss", "PositioningOnMiss");
            FinishContact(component);

            EnsureAnimatorParameter(fxController, safePrefix + "_Hit", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, safePrefix + "_Ratio", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(fxController, safePrefix + "_Distance", AnimatorControllerParameterType.Float);

            var oldTarget = explicitTarget;
            explicitTarget = result;
            var oldSpellRadius = spellRadius; var oldTechnickRadius = technickRadius; var oldItemRadius = itemRadius; var oldAttackRadius = attackRadius;
            var oldAttackEnabled = attackStartsEnabled; var oldSpellEnabled = spellStartsEnabled; var oldTechnickEnabled = technickStartsEnabled; var oldItemEnabled = itemStartsEnabled; var oldDebuffEnabled = debuffStartsEnabled;
            spellRadius = technickRadius = itemRadius = attackRadius = raycastImpactRadius;
            attackStartsEnabled = spellStartsEnabled = technickStartsEnabled = itemStartsEnabled = debuffStartsEnabled = true;
            suppressContactAttachment = true; // Impact Contact stays on the raycast result transform.
            try
            {
                switch (outgoingKind)
                {
                    case OutgoingContactKind.Attack: CreateAttackSenders(); break;
                    case OutgoingContactKind.Spell: CreateSpellSenders(); break;
                    case OutgoingContactKind.Technick: CreateTechnickSenders(); break;
                    case OutgoingContactKind.Item: CreateItemSenders(); break;
                    case OutgoingContactKind.Debuff: CreateDebuffSenders(); break;
                    default: EditorUtility.DisplayDialog("Raycast Delivery", "Select Attack, Spell, Technick, Item, or Debuff before creating a raycast delivery.", "OK"); break;
                }
            }
            finally
            {
                spellRadius = oldSpellRadius; technickRadius = oldTechnickRadius; itemRadius = oldItemRadius; attackRadius = oldAttackRadius;
                attackStartsEnabled = oldAttackEnabled; spellStartsEnabled = oldSpellEnabled; technickStartsEnabled = oldTechnickEnabled; itemStartsEnabled = oldItemEnabled; debuffStartsEnabled = oldDebuffEnabled;
                suppressContactAttachment = false;
                explicitTarget = oldTarget;
            }
            RebuildRaycastGateLayer(safePrefix, result, actionGateParameter, actionGateValue, actionGateIsInteger);
            if (raycastCreateLineRenderer && origin.GetComponent<LineRenderer>() == null)
            {
                var line = Undo.AddComponent<LineRenderer>(origin);
                line.positionCount = 2; line.useWorldSpace = true; line.widthMultiplier = 0.01f; line.enabled = false;
            }
            Selection.activeGameObject = origin;
            Log("Created VRCRaycast delivery '" + safePrefix + "' with result Contact at " + raycastDistance + "m.");
        }

        private string RegexSafeParameter(string value)
        {
            var cleaned = new string((value ?? "SoY_Raycast").Where(x => char.IsLetterOrDigit(x) || x == '_').ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "SoY_Raycast" : cleaned;
        }

        private void RebuildRaycastGateLayer(string prefix, GameObject result, string actionParameter, int actionValue, bool integerGate)
        {
            if (fxController == null || result == null) return;
            var layerName = RaycastLayerPrefix + prefix;
            RemoveLayerByName(fxController, layerName);
            var layer = CreateHookLayer(fxController, layerName);
            var off = AddHookState(layer.stateMachine, "Miss / Disabled", new Vector3(180f, 120f));
            var on = AddHookState(layer.stateMachine, "Hit / Contact Enabled", new Vector3(500f, 120f));
            layer.stateMachine.defaultState = off;
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AnimationRoot + "/" + avatarName + "/Raycasts";
            EnsureAssetFolder(folder);
            off.motion = CreateOrReplaceActiveClip(folder + "/" + prefix + "_Off.anim", new[] { result }, false, 1f / 60f);
            on.motion = CreateOrReplaceActiveClip(folder + "/" + prefix + "_On.anim", new[] { result }, true, 1f / 60f);
            var activate = off.AddTransition(on);
            activate.hasExitTime = false;
            activate.duration = 0f;
            activate.AddCondition(AnimatorConditionMode.If, 0f, prefix + "_Hit");
            activate.AddCondition(integerGate ? AnimatorConditionMode.Equals : AnimatorConditionMode.If, integerGate ? actionValue : 0f, actionParameter);
            activate.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

            var miss = on.AddTransition(off);
            miss.hasExitTime = false;
            miss.duration = 0f;
            miss.AddCondition(AnimatorConditionMode.IfNot, 0f, prefix + "_Hit");

            var released = on.AddTransition(off);
            released.hasExitTime = false;
            released.duration = 0f;
            released.AddCondition(integerGate ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.IfNot, integerGate ? actionValue : 0f, actionParameter);

            var koLock = on.AddTransition(off);
            koLock.hasExitTime = false;
            koLock.duration = 0f;
            koLock.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");
            fxController.AddLayer(layer); EditorUtility.SetDirty(fxController); AssetDatabase.SaveAssets();
        }

        private void EnsureAnimatorParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            if (controller == null || controller.parameters.Any(x => x.name == name)) return;
            controller.AddParameter(name, type); EditorUtility.SetDirty(controller);
        }

        private void SetEnumMemberByKeywords(Component component, string[] keywords, params string[] names)
        {
            var member = FindMember(component.GetType(), names);
            var enumType = member is FieldInfo ? ((FieldInfo)member).FieldType : member is PropertyInfo ? ((PropertyInfo)member).PropertyType : null;
            if (enumType == null || !enumType.IsEnum) return;
            var values = Enum.GetValues(enumType).Cast<object>().ToArray();
            var match = values.FirstOrDefault(value => keywords.All(keyword => value.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0));
            if (match == null && keywords.Length > 0) match = values.FirstOrDefault(value => value.ToString().IndexOf(keywords[0], StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null) WriteMember(component, match, names);
        }

        private void ApplyContactPreset(ContactPreset preset)
        {
            switch (preset)
            {
                case ContactPreset.SwordBlade: attackShape = ContactShape.Capsule; attackRadius = 0.05f; attackHeight = 0.8f; attackPosition = Vector3.zero; attackRotation = Vector3.zero; break;
                case ContactPreset.ShieldFace: blockShape = ContactShape.Box; blockBoxSize = new Vector3(0.5f, 0.7f, 0.08f); break;
                case ContactPreset.HandSpell: spellShape = ContactShape.Sphere; spellRadius = 0.12f; spellPosition = Vector3.zero; break;
                case ContactPreset.HealingOrb: spellShape = ContactShape.Sphere; spellRadius = 0.2f; break;
                case ContactPreset.BodyReceiver: incomingShape = ContactShape.Capsule; incomingRadius = 0.22f; incomingHeight = 0.65f; break;
                case ContactPreset.Aura: debuffShape = ContactShape.Sphere; debuffRadius = 0.65f; break;
                case ContactPreset.LargeAoe: spellShape = ContactShape.Sphere; spellRadius = 1.5f; break;
                case ContactPreset.RaycastImpact: attackShape = spellShape = technickShape = itemShape = ContactShape.Sphere; attackRadius = spellRadius = technickRadius = itemRadius = raycastImpactRadius; break;
            }
            Log("Applied contact preset: " + preset);
        }

        private void DuplicateSelectedContactObject()
        {
            if (Selection.activeGameObject == null) return;
            var clone = Instantiate(Selection.activeGameObject, Selection.activeGameObject.transform.parent);
            clone.name = Selection.activeGameObject.name + " Copy";
            Undo.RegisterCreatedObjectUndo(clone, "Duplicate Stories Contact");
            Selection.activeGameObject = clone;
        }

        private void MirrorSelectedContactObject()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return;
            Undo.RecordObject(selected.transform, "Mirror Stories Contact");
            var pos = selected.transform.localPosition; pos.x = -pos.x; selected.transform.localPosition = pos;
            var rot = selected.transform.localEulerAngles; rot.y = -rot.y; rot.z = -rot.z; selected.transform.localEulerAngles = rot;
        }

        private ContactDraftKind CurrentDraftKind()
        {
            switch (outgoingKind)
            {
                case OutgoingContactKind.Spell: return ContactDraftKind.Spell;
                case OutgoingContactKind.Technick: return ContactDraftKind.Technick;
                case OutgoingContactKind.Item: return ContactDraftKind.Item;
                case OutgoingContactKind.Blocking: return ContactDraftKind.Blocking;
                case OutgoingContactKind.Debuff: return ContactDraftKind.Debuff;
                default: return ContactDraftKind.Attack;
            }
        }

        private void CopySelectedColliderToCurrentGeometry()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) return;
            var sphere = selected.GetComponent<SphereCollider>();
            var capsule = selected.GetComponent<CapsuleCollider>();
            var box = selected.GetComponent<BoxCollider>();
            var kind = CurrentDraftKind();
            if (sphere != null) SetGeometry(kind, ContactShape.Sphere, sphere.radius, sphere.radius * 2f, Vector3.one, selected.transform.localPosition, selected.transform.localEulerAngles);
            else if (capsule != null) SetGeometry(kind, ContactShape.Capsule, capsule.radius, capsule.height, Vector3.one, selected.transform.localPosition, selected.transform.localEulerAngles);
            else if (box != null) SetGeometry(kind, ContactShape.Box, 0.1f, 0.2f, box.size, selected.transform.localPosition, selected.transform.localEulerAngles);
            else EditorUtility.DisplayDialog("Copy Collider", "The selected object has no Sphere, Capsule, or Box Collider.", "OK");
        }

        private string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(x => x.ToString("x2")).ToArray());
        }


        private bool DrawDeliverySelector(string actionLabel)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delivery", GUILayout.Width(70f));
            EditorGUI.BeginChangeCheck();
            deliveryMode = (DeliveryMode)GUILayout.Toolbar(
                (int)deliveryMode,
                new[] { "Contact", "Raycast" },
                GUILayout.Height(largeControls ? 34f : 24f));
            if (EditorGUI.EndChangeCheck())
                SaveEditorPreferences();
            EditorGUILayout.EndHorizontal();

            if (deliveryMode == DeliveryMode.Raycast)
            {
                var gate = CurrentRaycastGateSummary();
                EditorGUILayout.LabelField(actionLabel + " raycast • " + gate, wrappedLabel);
                DrawCompactRaycastSettings();
                return true;
            }
            return false;
        }

        private string CurrentRaycastGateSummary()
        {
            string parameter;
            int value;
            bool integerGate;
            GetCurrentRaycastActionGate(out parameter, out value, out integerGate);
            return integerGate ? parameter + " = " + value : RaycastFireParameter + " button";
        }

        private string CurrentRaycastSuggestedPrefix()
        {
            if (outgoingKind == OutgoingContactKind.Spell)
            {
                var rows = GetSpellsForSchool(spellSchool)
                    .Where(entry => string.IsNullOrWhiteSpace(spellSearch) ||
                        entry.Name.IndexOf(spellSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(spellSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    var selected = rows[Mathf.Clamp(spellSelectionIndex, 0, rows.Length - 1)];
                    return "Spell_" + selected.Id + "_" + MakeSafeAssetName(selected.Name);
                }
            }
            if (outgoingKind == OutgoingContactKind.Technick)
            {
                var rows = TechnickDefinitions
                    .Where(entry => string.IsNullOrWhiteSpace(technickSearch) ||
                        entry.Name.IndexOf(technickSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(technickSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    var selected = rows[Mathf.Clamp(technickSelectionIndex, 0, rows.Length - 1)];
                    return "Technick_" + selected.Id + "_" + MakeSafeAssetName(selected.Name);
                }
            }
            if (outgoingKind == OutgoingContactKind.Item)
            {
                var rows = ItemDefinitions
                    .Where(entry => string.IsNullOrWhiteSpace(itemSearch) ||
                        entry.Name.IndexOf(itemSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        entry.Id.ToString().Contains(itemSearch.Trim()))
                    .ToArray();
                if (rows.Length > 0)
                {
                    var selected = rows[Mathf.Clamp(itemSelectionIndex, 0, rows.Length - 1)];
                    return "Item_" + selected.Id + "_" + MakeSafeAssetName(selected.Name);
                }
            }
            if (outgoingKind == OutgoingContactKind.Attack)
                return "Attack_" + attackTier;
            if (outgoingKind == OutgoingContactKind.Debuff)
                return "Debuff_" + string.Join("_", GetSelectedDebuffs().DefaultIfEmpty("Effect").ToArray());
            return "SoY_Raycast";
        }

        private void DrawCompactRaycastSettings()
        {
            DrawTagRow("SDK", RaycastTypeAvailable() ? "✓ Ready" : "✕ Missing", "VRChat SDK 3.10.3+");
            raycastDistance = Mathf.Clamp(EditorGUILayout.FloatField("Range", raycastDistance), 0.1f, 1000f);
            raycastImpactRadius = Mathf.Clamp(EditorGUILayout.FloatField("Impact Radius", raycastImpactRadius), 0.01f, 1f);
            raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Targets", raycastCollisionTarget);

            showRaycastAdvanced = EditorGUILayout.Foldout(showRaycastAdvanced, "Advanced Raycast", true);
            if (showRaycastAdvanced)
            {
                raycastDirection = EditorGUILayout.Vector3Field("Local Direction", raycastDirection);
                raycastApplyRotation = EditorGUILayout.ToggleLeft("Align result to hit surface", raycastApplyRotation);
                raycastCreateLineRenderer = EditorGUILayout.ToggleLeft("Create visual LineRenderer holder", raycastCreateLineRenderer);
                raycastUseCustomPrefix = EditorGUILayout.ToggleLeft("Use custom asset prefix", raycastUseCustomPrefix);
                if (raycastUseCustomPrefix)
                    raycastParameterPrefix = EditorGUILayout.TextField("Custom Prefix", raycastParameterPrefix);
            }

            var suggested = CurrentRaycastSuggestedPrefix();
            EditorGUILayout.LabelField("Creates: " + suggested, EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!RaycastTypeAvailable() || !HasUsableTargets() || avatarRoot == null))
            {
                if (GUILayout.Button("Create Raycast Delivery", GUILayout.Height(largeControls ? 42f : 32f)))
                    CreateRaycastDeliveryForCurrentAction();
            }
        }

        private void DrawCompactContactSettings(
            ContactDraftKind kind,
            ref ContactShape shape,
            ref float radius,
            ref float height,
            ref Vector3 boxSize,
            ref Vector3 position,
            ref Vector3 rotation,
            bool canCreate,
            string previewLabel)
        {
            DrawShapeEditor(ref shape, ref radius, ref height, ref boxSize, ref position, ref rotation);
            showContactAdvanced = EditorGUILayout.Foldout(showContactAdvanced, "Advanced Contact Utilities", true);
            if (showContactAdvanced)
            {
                contactPreset = (ContactPreset)EditorGUILayout.EnumPopup("Preset", contactPreset);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Preset")) ApplyContactPreset(contactPreset);
                if (GUILayout.Button("Copy Collider")) CopySelectedColliderToCurrentGeometry();
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                contactAttachmentMode = (ContactAttachmentMode)EditorGUILayout.EnumPopup("Attachment", contactAttachmentMode);
                if (contactAttachmentMode != ContactAttachmentMode.ContactObject)
                    weaponAttachmentTarget = (GameObject)EditorGUILayout.ObjectField("Weapon / Prop", weaponAttachmentTarget, typeof(GameObject), true);
                if (contactAttachmentMode == ContactAttachmentMode.VRCParentConstraint)
                {
                    constraintMaintainOffset = EditorGUILayout.Toggle("Keep Current Offset", constraintMaintainOffset);
                    constraintWeight = EditorGUILayout.Slider("Constraint Weight", constraintWeight, 0f, 1f);
                }
                if (EditorGUI.EndChangeCheck()) SaveEditorPreferences();
                if (contactAttachmentMode != ContactAttachmentMode.ContactObject && weaponAttachmentTarget == null)
                    EditorGUILayout.HelpBox("Assign the weapon or prop transform used by this Contact.", MessageType.Warning);
            }
            DrawDraftControls(kind, canCreate, previewLabel);
        }

        private void DrawOutgoingContacts()
        {
            outgoingKind = (OutgoingContactKind)GUILayout.Toolbar((int)outgoingKind, new[]
            {
                "Attack", "Spell", "Technick", "Item", "Block", "Debuff"
            }, GUILayout.Height(largeControls ? 36f : 26f));
            EditorGUILayout.Space(4f);

            switch (outgoingKind)
            {
                case OutgoingContactKind.Attack:
                    DrawAttackSenders();
                    break;
                case OutgoingContactKind.Spell:
                    DrawSpellSenders();
                    break;
                case OutgoingContactKind.Technick:
                    DrawTechnickSenders();
                    break;
                case OutgoingContactKind.Item:
                    DrawItemSenders();
                    break;
                case OutgoingContactKind.Blocking:
                    DrawBlocking();
                    break;
                case OutgoingContactKind.Debuff:
                    DrawDebuffs();
                    break;
            }
        }

        private void DrawAttackSenders()
        {
            DrawHealthSafetyMini();
            BeginCard("Attack");
            attackTier = (AttackTier)EditorGUILayout.EnumPopup("Tier", attackTier);

            EditorGUILayout.BeginHorizontal();
            addBurnToAttack = GUILayout.Toggle(addBurnToAttack, "Burn", "Button");
            addSilenceToAttack = GUILayout.Toggle(addSilenceToAttack, "Silence", "Button");
            addFreezeToAttack = GUILayout.Toggle(addFreezeToAttack, "Freeze", "Button");
            addBindToAttack = GUILayout.Toggle(addBindToAttack, "Bind", "Button");
            addBleedToAttack = GUILayout.Toggle(addBleedToAttack, "Bleed", "Button");
            EditorGUILayout.EndHorizontal();

            var isRaycast = DrawDeliverySelector(attackTier + " attack");
            if (!isRaycast)
            {
                DrawCompactContactSettings(
                    ContactDraftKind.Attack,
                    ref attackShape, ref attackRadius, ref attackHeight, ref attackBoxSize,
                    ref attackPosition, ref attackRotation,
                    HasUsableTargets() && ContactTypesAvailable(),
                    "Preview Attack");
                attackStartsEnabled = EditorGUILayout.ToggleLeft("Start enabled", attackStartsEnabled);
            }

            if (attackTier == AttackTier.Critical)
                EditorGUILayout.LabelField("Critical attacks are not Blockable.", EditorStyles.miniLabel);
            EndCard();
        }

        private void DrawSpellSenders()
        {
            DrawHealthSafetyMini();
            BeginCard("Spell");
            spellSchool = (SpellSchool)EditorGUILayout.EnumPopup("School", spellSchool);
            spellSearch = EditorGUILayout.TextField("Search", spellSearch);
            var spells = GetSpellsForSchool(spellSchool)
                .Where(entry => string.IsNullOrWhiteSpace(spellSearch) ||
                    entry.Name.IndexOf(spellSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(spellSearch.Trim()))
                .ToArray();
            if (spells.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching spell.", MessageType.Info);
                EndCard();
                return;
            }

            spellSelectionIndex = Mathf.Clamp(spellSelectionIndex, 0, spells.Length - 1);
            spellSelectionIndex = EditorGUILayout.Popup(
                "Spell",
                spellSelectionIndex,
                spells.Select(spell => spell.Id + " — " + spell.Name).ToArray());
            var selected = spells[spellSelectionIndex];

            EditorGUILayout.LabelField(
                "ID " + selected.Id + " • " + GetSpellSchoolDisplayName(selected.School) + " • " + selected.Category,
                EditorStyles.miniLabel);

            var isRaycast = DrawDeliverySelector(selected.Name);
            if (!isRaycast)
            {
                DrawCompactContactSettings(
                    ContactDraftKind.Spell,
                    ref spellShape, ref spellRadius, ref spellHeight, ref spellBoxSize,
                    ref spellPosition, ref spellRotation,
                    HasUsableTargets() && ContactTypesAvailable(),
                    "Preview Spell");
                spellStartsEnabled = EditorGUILayout.ToggleLeft("Start enabled", spellStartsEnabled);
            }

            showActionDetails = EditorGUILayout.Foldout(showActionDetails, "Details", true);
            if (showActionDetails)
                EditorGUILayout.LabelField(
                    "Outgoing bus: " + GetSpellBinary(selected.Id) +
                    " • Alignment: " + CasterAllyTag + " / " + CasterEnemyTag,
                    wrappedLabel);
            EndCard();
        }


        private void DrawTechnickSenders()
        {
            DrawHealthSafetyMini();
            BeginCard("Technick");
            technickSearch = EditorGUILayout.TextField("Search", technickSearch);
            var rows = TechnickDefinitions
                .Where(entry => string.IsNullOrWhiteSpace(technickSearch) ||
                    entry.Name.IndexOf(technickSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(technickSearch.Trim()))
                .ToArray();
            if (rows.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching Technick.", MessageType.Info);
                EndCard();
                return;
            }

            technickSelectionIndex = Mathf.Clamp(technickSelectionIndex, 0, rows.Length - 1);
            technickSelectionIndex = EditorGUILayout.Popup(
                "Technick",
                technickSelectionIndex,
                rows.Select(entry => entry.Id + " — " + entry.Name).ToArray());
            var selected = rows[technickSelectionIndex];

            EditorGUILayout.LabelField("ID " + selected.Id + " • Gate: SoY_TechnickType", EditorStyles.miniLabel);
            var isRaycast = DrawDeliverySelector(selected.Name);
            if (!isRaycast)
            {
                DrawCompactContactSettings(
                    ContactDraftKind.Technick,
                    ref technickShape, ref technickRadius, ref technickHeight, ref technickBoxSize,
                    ref technickPosition, ref technickRotation,
                    HasUsableTargets() && ContactTypesAvailable(),
                    "Preview Technick");
                technickStartsEnabled = EditorGUILayout.ToggleLeft("Start enabled", technickStartsEnabled);
            }

            showActionDetails = EditorGUILayout.Foldout(showActionDetails, "Details", true);
            if (showActionDetails && !string.IsNullOrWhiteSpace(selected.Description))
                EditorGUILayout.LabelField(selected.Description, wrappedLabel);
            EndCard();
        }

        private void DrawItemSenders()
        {
            DrawHealthSafetyMini();
            BeginCard("Item");
            itemSearch = EditorGUILayout.TextField("Search", itemSearch);
            var rows = ItemDefinitions
                .Where(entry => string.IsNullOrWhiteSpace(itemSearch) ||
                    entry.Name.IndexOf(itemSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(itemSearch.Trim()))
                .ToArray();
            if (rows.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching item.", MessageType.Info);
                EndCard();
                return;
            }

            itemSelectionIndex = Mathf.Clamp(itemSelectionIndex, 0, rows.Length - 1);
            itemSelectionIndex = EditorGUILayout.Popup(
                "Item",
                itemSelectionIndex,
                rows.Select(entry => entry.Id + " — " + entry.Name).ToArray());
            var selected = rows[itemSelectionIndex];

            EditorGUILayout.LabelField("ID " + selected.Id + " • Gate: SoY_ItemType", EditorStyles.miniLabel);
            var isRaycast = DrawDeliverySelector(selected.Name);
            if (!isRaycast)
            {
                DrawCompactContactSettings(
                    ContactDraftKind.Item,
                    ref itemShape, ref itemRadius, ref itemHeight, ref itemBoxSize,
                    ref itemPosition, ref itemRotation,
                    HasUsableTargets() && ContactTypesAvailable(),
                    "Preview Item");
                itemStartsEnabled = EditorGUILayout.ToggleLeft("Start enabled", itemStartsEnabled);
            }

            showActionDetails = EditorGUILayout.Foldout(showActionDetails, "Details", true);
            if (showActionDetails && !string.IsNullOrWhiteSpace(selected.Description))
                EditorGUILayout.LabelField(selected.Description, wrappedLabel);
            EndCard();
        }

        private void DrawBlocking()
        {
            DrawHealthSafetyMini();
            BeginCard("Block Surface");
            EditorGUILayout.HelpBox(
                "Creates a receiver that listens for '" + TagBlockable + "' and drives the existing Bool parameter '" + TagHitBlocked + "'. " +
                "It can also create a legacy sender using the exact '" + TagHitBlocked + "' tag for wider compatibility.",
                MessageType.Info);

            DrawShapeEditor(ref blockShape, ref blockRadius, ref blockHeight, ref blockBoxSize, ref blockPosition, ref blockRotation);
            blockCreateChild = EditorGUILayout.ToggleLeft("Create a dedicated child block-contact object", blockCreateChild);
            addLegacyBlockedSender = EditorGUILayout.ToggleLeft("Also emit legacy 'Hit Blocked' sender tag", addLegacyBlockedSender);
            bridgeBlockToOsc = EditorGUILayout.ToggleLeft("Also mirror the block into SoY_HitBlocked for the OSC program", bridgeBlockToOsc);

            DrawDraftControls(
                ContactDraftKind.Blocking,
                HasUsableTargets() && ContactTypesAvailable(),
                "Preview Block Surface");
            EndCard();

            BeginCard("Blocking Rules");
            EditorGUILayout.LabelField("• Weak, Average, and Strong attacks carry 'Blockable'.", wrappedLabel);
            EditorGUILayout.LabelField("• Critical attacks never carry 'Blockable' and bypass this receiver.", wrappedLabel);
            EditorGUILayout.LabelField("• Put the block volume on the physical shield face or guarded sword blade.", wrappedLabel);
            EditorGUILayout.LabelField("• Leave the receiver enabled while the item is actively guarding; disable it when not guarding if the animation requires that behavior.", wrappedLabel);
            EndCard();
        }

        private void DrawDebuffs()
        {
            DrawHealthSafetyMini();
            BeginCard("Debuff");
            EditorGUILayout.BeginHorizontal();
            debuffBurn = GUILayout.Toggle(debuffBurn, "Burn", "Button");
            debuffSilence = GUILayout.Toggle(debuffSilence, "Silence", "Button");
            debuffFreeze = GUILayout.Toggle(debuffFreeze, "Freeze", "Button");
            debuffBind = GUILayout.Toggle(debuffBind, "Bind", "Button");
            debuffBleed = GUILayout.Toggle(debuffBleed, "Bleed", "Button");
            EditorGUILayout.EndHorizontal();

            var isRaycast = DrawDeliverySelector("Debuff");
            if (!isRaycast)
            {
                DrawCompactContactSettings(
                    ContactDraftKind.Debuff,
                    ref debuffShape, ref debuffRadius, ref debuffHeight, ref debuffBoxSize,
                    ref debuffPosition, ref debuffRotation,
                    HasUsableTargets() && ContactTypesAvailable() && GetSelectedDebuffs().Any(),
                    "Preview Debuff");
                debuffStartsEnabled = EditorGUILayout.ToggleLeft("Start enabled", debuffStartsEnabled);
            }
            EndCard();
        }

        private void DrawIncomingReceivers()
        {
            DrawHealthSafetyMini();
            BeginCard("Incoming Contact Receivers");
            EditorGUILayout.HelpBox(
                "These receivers listen for the standard combat tags and write only to SoY_ OSC input parameters. " +
                "They are intended for avatars without an existing body receiver set. If an existing health system is detected, " +
                "the safer path is to register its existing parameters for OSC instead of adding another body receiver set.",
                MessageType.Info);

            var compatible = cachedAudit != null && cachedAudit.Kind == HealthSystemKind.Compatible;
            if (compatible)
            {
                EditorGUILayout.HelpBox(
                    "A compatible health system is detected. Incoming SoY receiver creation is locked by default so the existing health setup remains untouched.",
                    MessageType.Warning);
                forceIncomingOnExistingHealth = EditorGUILayout.ToggleLeft(
                    "Advanced: allow a separate SoY monitor receiver set anyway",
                    forceIncomingOnExistingHealth);
            }

            incomingHits = EditorGUILayout.ToggleLeft("Create hit receivers: Weak, Average, Strong, Critical", incomingHits);
            incomingDebuffs = EditorGUILayout.ToggleLeft("Create debuff receivers: Burn, Silence, Freeze, Bind, Bleed", incomingDebuffs);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Incoming Spell Identification", cardTitleStyle);
            incomingSpells = EditorGUILayout.ToggleLeft("Create compact binary spell receiver bus", incomingSpells);
            EditorGUILayout.HelpBox(
                "The compact receiver bus uses one Active receiver plus eight bit receivers for every spell ID from 1-255. " +
                "This replaces the broken SDK behavior where every Constant Int receiver reported 1. " +
                "All Magick schools are supported automatically with only nine unsynced Bool parameters.",
                MessageType.Info);
            EditorGUILayout.LabelField("Receiver components", incomingSpells ? "9 spell bus + 1 caster alignment" : "Disabled");
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Incoming Technick / Item Identification", cardTitleStyle);
            incomingTechnicks = EditorGUILayout.ToggleLeft("Create compact binary Technick receiver bus", incomingTechnicks);
            incomingItems = EditorGUILayout.ToggleLeft("Create compact binary Item receiver bus", incomingItems);
            EditorGUILayout.HelpBox(
                "Technicks and Items each use the same compact 1 Active + 8 bit pattern. " +
                "The incoming Active/Bit bus parameters are unsynced and consume no synchronized parameter memory. " +
                "The separate SoY_TechnickType and SoY_ItemType menu selectors are synced so remote players can see action FX.",
                MessageType.Info);
            EditorGUILayout.LabelField("Required Desktop", "Stories Of Yggdrasil OSC v0.8.4 or newer");
            DrawShapeEditor(ref incomingShape, ref incomingRadius, ref incomingHeight, ref incomingBoxSize, ref incomingPosition, ref incomingRotation);
            incomingCreateChild = true;
            EditorGUILayout.HelpBox(
                "Incoming damage and action receivers are created on separate child objects. The damage child is temporarily disabled for one second after a hit, while spell, Technick, and Item buses remain available.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            var locked = compatible && !forceIncomingOnExistingHealth;
            DrawDraftControls(
                ContactDraftKind.Incoming,
                HasUsableTargets() && ContactTypesAvailable() && !locked && (incomingHits || incomingDebuffs || incomingSpells || incomingTechnicks || incomingItems),
                "Preview Incoming Receiver Volume");
            EndCard();

            BeginCard("Exact Incoming Mapping");
            DrawTagRow(TagWeak, "SoY_HitWeak", "Exact collision tag; Constant mode held for OSC rising-edge detection");
            DrawTagRow(TagAverage, "SoY_HitAverage", "Exact collision tag; Constant mode held for OSC rising-edge detection");
            DrawTagRow(TagStrong, "SoY_HitStrong", "Exact collision tag; Constant mode held for OSC rising-edge detection");
            DrawTagRow(TagCritical, "SoY_HitCritical", "Exact collision tag; Critical remains unblockable");
            DrawTagRow("Burn / Silence", "SoY_DebuffBurn / Silence", "Exact debuff tags; Constant mode held for OSC rising-edge detection");
            DrawTagRow("Freeze / Bind / Bleed", "SoY_DebuffFreeze / Bind / Bleed", "Exact debuff tags; Constant mode held for OSC rising-edge detection");
            DrawTagRow("Spell Active", SpellActiveParameter, "True while any encoded spell sender is inside the receiver");
            DrawTagRow("Spell ID Bits", SpellBitParameterPrefix + "0-7", "Eight Bool values reconstruct the stable 1-255 spell ID in Desktop v0.8.1+");
            DrawTagRow("Resolved Spell", "SoY_SpellType (Int)", "Desktop reconstructs the bit bus and sends the normal registry ID to Sam.py");
            DrawTagRow("Action alignment", "SoY_HealingSourceEnemy / SoY_DamageSourceEnemy", "Enemy-tagged action and damage sources");
            DrawTagRow("Technick Bus", "SoY_TechnickActive + Bit0-7", "Desktop reconstructs TECHNICK_ID_REGISTRY_v1");
            DrawTagRow("Item Bus", "SoY_ItemActive + Bit0-7", "Desktop reconstructs ITEM_ID_REGISTRY_v1; Sam.py verifies inventory");
            DrawTagRow("I-Frames", "1.0 second", "Incoming damage receiver child is disabled after each accepted hit");
            EndCard();
        }

        private void DrawAnimatorSetup()
        {
            switch (animationPage)
            {
                case AnimationPage.Health:
                    DrawHealthAnimationBuilder();
                    break;
                case AnimationPage.Spells:
                    DrawSpellAnimationBuilder();
                    break;
                case AnimationPage.Technicks:
                    DrawTechnickAnimationBuilder();
                    break;
                case AnimationPage.Items:
                    DrawItemAnimationBuilder();
                    break;
                case AnimationPage.Setup:
                    DrawAnimationMaintenance();
                    break;
            }
        }

        private void DrawAnimationMaintenance()
        {
            BeginCard("Animation Setup");
            var safeFx = IsSafeFxCopy(fxController);
            var missingAnimator = fxController == null
                ? BridgeParameters.Length
                : BridgeParameters.Count(spec => !fxController.parameters.Any(p => p.name == spec.Name));
            var missingExpression = expressionParameters == null
                ? BridgeParameters.Length
                : BridgeParameters.Count(spec =>
                    expressionParameters.parameters == null ||
                    !expressionParameters.parameters.Any(p => p != null && p.name == spec.Name));

            DrawTagRow("FX", safeFx ? "✓ Safe Copy" : fxController != null ? "! Original" : "✕ Missing", "Working controller");
            DrawTagRow("Animator", missingAnimator == 0 ? "✓ Ready" : "! " + missingAnimator + " missing", "SoY parameters");
            DrawTagRow("Expressions", missingExpression == 0 ? "✓ Ready" : "! " + missingExpression + " missing", "OSC parameters");

            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("Install / Repair Animation Hooks", GUILayout.Height(largeControls ? 44f : 34f)))
                    InstallAllBridgeHooks();
            }

            showAnimationAdvanced = EditorGUILayout.Foldout(showAnimationAdvanced, "Advanced Maintenance", true);
            if (showAnimationAdvanced)
            {
                DrawHealthSafetyCard();
                DrawAnimationBindingAudit();

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(fxController == null || avatarRoot == null))
                {
                    if (GUILayout.Button("Rebuild I-Frames")) RebuildIFrameLayer();
                    if (GUILayout.Button("Rebuild Alignment")) RebuildSpellAlignmentLayer();
                }
                EditorGUILayout.EndHorizontal();

                if (safeFx && GUILayout.Button("Select FX Copy"))
                {
                    Selection.activeObject = fxController;
                    EditorGUIUtility.PingObject(fxController);
                }
            }
            EndCard();
        }

        private void DrawHelp()
        {
            BeginCard("Accessibility");
            EditorGUILayout.LabelField("Open Maintenance → Display for Normal, Large, and Extra Large text; larger controls; high-contrast and color-vision-friendly palettes; short VRChat labels; and purpose-first spell navigation.", wrappedLabel);
            EndCard();

            BeginCard("Five-Minute Tutorial");
            EditorGUILayout.LabelField("1. Assign the avatar's VRC Avatar Descriptor and press Load From Avatar.", wrappedLabel);
            EditorGUILayout.LabelField("2. Open Setup and press PREPARE AVATAR FOR STORIES OSC. The tool creates a safe FX copy and assigns it automatically.", wrappedLabel);
            EditorGUILayout.LabelField("3. Select a weapon, ray origin, shield, body object, or effect transform. Use Contact Target when selection should remain fixed.", wrappedLabel);
            EditorGUILayout.LabelField("4. Open Contacts and choose Outgoing or Incoming and press Preview. A temporary object appears under the selected target.", wrappedLabel);
            EditorGUILayout.LabelField("5. Move and rotate the temporary object with Unity's Transform tools. Resize it through its Sphere, Capsule, or Box Collider.", wrappedLabel);
            EditorGUILayout.LabelField("6. Return to this window and press Finalize & Create Contact. The temporary object deletes itself after the real contact is created.", wrappedLabel);
            EditorGUILayout.LabelField("7. Use the generated Stories RP submenu to enable combat, set Enemy Mode, view Mist/Curse gauges, and select spell IDs.", wrappedLabel);
            EditorGUILayout.LabelField("8. Animate attack, spell, and debuff contact objects ON only during valid hit frames, then Build & Test.", wrappedLabel);
            EndCard();

            BeginCard("Temporary Contact Preview");
            EditorGUILayout.LabelField(
                "The preview is not a VRChat Contact. It uses a normal trigger Collider so it can be positioned and resized safely before anything permanent is added. " +
                "Finalizing copies the preview's local position, rotation, and dimensions into the real Contact, then removes the preview object.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Preview objects are marked as editor-only and are also removed when this tool window closes.",
                wrappedLabel);
            EndCard();

            BeginCard("Safe FX Copy Workflow");
            EditorGUILayout.LabelField(
                "The tool never edits the avatar's original FX controller. Before adding Animator parameters or layers it duplicates the controller, stores it under:",
                wrappedLabel);
            EditorGUILayout.SelectableLabel(FxCopyRoot + "/<Avatar>_<FX>_SoY_FX.controller", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField(
                "The copy is automatically assigned to the avatar's FX layer. A confirmation dialog shows the exact asset path.",
                wrappedLabel);
            EndCard();

            BeginCard("Stories RP Sub-Menu");
            EditorGUILayout.LabelField(
                "The root Expressions Menu receives one Stories RP submenu instead of a direct combat toggle. The generated submenu contains RP Combat, Enemy Mode, spell pages, Mist Charge, and Curse Of Diablos gauges.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Enemy Mode is exposed through SoY_IsEnemy. Spell, Technick, and Item senders now use Ally/Enemy pairs so target-side alignment checks receive the caster state.",
                wrappedLabel);
            EndCard();

            BeginCard("Contact Types");
            DrawTagRow("Attack", "Weak / Average / Strong / Critical", "Weapon contacts or VRCRaycast impact contacts");
            DrawTagRow("Spell", "8-bit Contact Bus → SoY_SpellType", "Twelve Magick schools with stable IDs, caster alignment, and category tags");
            DrawTagRow("Blocking", TagBlockable + " → " + TagHitBlocked, "Shield face or guarded weapon volume");
            DrawTagRow("Debuff", string.Join(", ", DebuffTags), "Spell, raycast impact, aura, or effect volume");
            DrawTagRow("Incoming", "SoY_Hit* / SoY_Debuff*", "Avatar body receiver volume for Sam.py synchronization");
            EndCard();

            DrawUpdaterCard();

            diagnosticsFoldout = EditorGUILayout.Foldout(diagnosticsFoldout, "Diagnostics and Recent Operations", true);
            if (diagnosticsFoldout)
                DrawDiagnostics();
        }

        private void DrawUpdaterCard()
        {
            BeginCard("Unity Tool Updater");
            EditorGUILayout.LabelField("Repository", GitHubRepository);
            EditorGUILayout.LabelField("Current Version", Version + " — " + BuildLabel);
            EditorGUILayout.LabelField("Status", updateStatus, wrappedLabel);
            EditorGUILayout.LabelField("Last checked", updateLastChecked, wrappedLabel);
            EditorGUI.BeginChangeCheck();
            updateChannel = (UpdateChannel)EditorGUILayout.EnumPopup("Update Channel", updateChannel);
            autoCheckUpdates = EditorGUILayout.ToggleLeft("Automatically check GitHub Releases every 6 hours while this window is open", autoCheckUpdates);
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorPreferences();
                nextBackgroundUpdateCheckAt = EditorApplication.timeSinceStartup + 0.1d;
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(updateRequest != null || updateDownloadRequest != null || updateChecksumRequest != null))
            {
                if (GUILayout.Button("Check For Updates", GUILayout.Height(30f)))
                    CheckForUpdates(true);
            }
            if (GUILayout.Button("Open GitHub", GUILayout.Height(30f)))
                Application.OpenURL(GitHubRepositoryUrl);
            if (updateAvailable && GUILayout.Button("Install Update", GUILayout.Height(30f)))
                PromptForUpdate();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Background checks are silent. Stable uses the latest published release; Test Builds may include prereleases. Installation requires confirmation, verifies the release SHA256 asset, backs up the current script, and refreshes Unity.",
                MessageType.Info);
            EndCard();
        }

        private void LoadCachedUpdateState()
        {
            updateStatus = EditorPrefs.GetString("StoriesOSC.v058.TB5.UpdateStatus", "Waiting for background check");
            updateLastChecked = EditorPrefs.GetString("StoriesOSC.v058.TB5.UpdateLastChecked", "Never");
            updateAvailable = EditorPrefs.GetBool("StoriesOSC.v058.TB5.UpdateAvailable", false);
        }

        private void SaveCachedUpdateState()
        {
            EditorPrefs.SetString("StoriesOSC.v058.TB5.UpdateStatus", updateStatus ?? string.Empty);
            EditorPrefs.SetString("StoriesOSC.v058.TB5.UpdateLastChecked", updateLastChecked ?? "Never");
            EditorPrefs.SetBool("StoriesOSC.v058.TB5.UpdateAvailable", updateAvailable);
        }

        private void CheckForUpdates(bool manual)
        {
            if (updateRequest != null || updateDownloadRequest != null || updateChecksumRequest != null)
                return;
            updateCheckWasManual = manual;
            updateStatus = "Checking GitHub Releases...";
            var apiUrl = updateChannel == UpdateChannel.Stable
                ? GitHubLatestReleaseApi
                : "https://api.github.com/repos/" + GitHubRepository + "/releases?per_page=10";
            updateRequest = UnityWebRequest.Get(apiUrl);
            updateRequest.SetRequestHeader("Accept", "application/vnd.github+json");
            updateRequest.SetRequestHeader("User-Agent", "Stories-Of-Yggdrasil-OSC-Unity-Tool/" + Version);
            updateRequest.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
            updateRequest.SendWebRequest();
            EditorApplication.update -= PollUpdateCheck;
            EditorApplication.update += PollUpdateCheck;
            Repaint();
        }

        private void PollUpdateCheck()
        {
            if (updateRequest == null || !updateRequest.isDone)
                return;
            EditorApplication.update -= PollUpdateCheck;
            var request = updateRequest;
            updateRequest = null;

            try
            {
                if (request.responseCode == 404)
                {
                    updateStatus = "No published GitHub Release exists yet.";
                    if (updateCheckWasManual)
                        EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
                    return;
                }
                if (request.result != UnityWebRequest.Result.Success)
                {
                    updateStatus = "Update check failed: " + request.error;
                    if (updateCheckWasManual)
                        EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
                    return;
                }

                if (updateChannel == UpdateChannel.Stable)
                {
                    latestRelease = JsonUtility.FromJson<GitHubReleaseInfo>(request.downloadHandler.text);
                }
                else
                {
                    var wrapper = JsonUtility.FromJson<GitHubReleaseList>("{\"items\":" + request.downloadHandler.text + "}");
                    latestRelease = wrapper != null && wrapper.items != null
                        ? wrapper.items.FirstOrDefault(item => item != null && !item.draft)
                        : null;
                }
                if (latestRelease == null || string.IsNullOrWhiteSpace(latestRelease.tag_name) || latestRelease.draft || (updateChannel == UpdateChannel.Stable && latestRelease.prerelease))
                {
                    updateStatus = "No compatible release information was returned.";
                    return;
                }

                var latestVersion = latestRelease.tag_name.Trim().TrimStart('v', 'V');
                updateAvailable = updateChannel == UpdateChannel.Stable
                    ? CompareVersions(latestVersion, Version) > 0
                    : CompareReleaseTags(latestRelease.tag_name, "v" + Version + "-" + BuildNumber) > 0;
                updateStatus = updateAvailable
                    ? "Update available — version " + latestVersion + "."
                    : "Up to date — latest release is " + latestVersion + ".";
                updateLastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveCachedUpdateState();
                if (updateCheckWasManual)
                    EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
            }
            catch (Exception exception)
            {
                updateStatus = "Could not parse the GitHub release: " + exception.Message;
                if (updateCheckWasManual)
                    EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
            }
            finally
            {
                if (updateLastChecked == "Never")
                    updateLastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveCachedUpdateState();
                request.Dispose();
                Repaint();
            }
        }

        private void PromptForUpdate()
        {
            if (!updateAvailable || latestRelease == null)
                return;
            var latestVersion = latestRelease.tag_name.Trim().TrimStart('v', 'V');
            var notes = string.IsNullOrWhiteSpace(latestRelease.body)
                ? "No release notes were provided."
                : latestRelease.body;
            if (notes.Length > 1200)
                notes = notes.Substring(0, 1200) + "...";
            var choice = EditorUtility.DisplayDialogComplex(
                "Stories OSC Unity Tool Update",
                "Version " + latestVersion + " is available.\n\n" + notes + "\n\nDownload and install it now?",
                "Download & Install",
                "Later",
                "Open Release");
            if (choice == 0)
                BeginUpdateDownload();
            else if (choice == 2 && !string.IsNullOrWhiteSpace(latestRelease.html_url))
                Application.OpenURL(latestRelease.html_url);
        }

        private void BeginUpdateDownload()
        {
            if (latestRelease == null || latestRelease.assets == null || updateDownloadRequest != null || updateChecksumRequest != null)
            {
                EditorUtility.DisplayDialog(
                    "Stories OSC Updater",
                    "This release does not contain downloadable assets. Publish the .unitypackage plus the canonical .cs updater asset.",
                    "OK");
                return;
            }

            var asset = latestRelease.assets.FirstOrDefault(item =>
                item != null && item.name != null &&
                item.name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                item.name.IndexOf("StoriesOfYggdrasilOSCContactSystem", StringComparison.OrdinalIgnoreCase) >= 0);
            if (asset == null)
            {
                asset = latestRelease.assets.FirstOrDefault(item =>
                    item != null && item.name != null &&
                    item.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            }
            if (asset == null || string.IsNullOrWhiteSpace(asset.browser_download_url))
            {
                EditorUtility.DisplayDialog(
                    "Stories OSC Updater",
                    "No compatible updater payload was found. Keep the canonical .cs release asset alongside the .unitypackage.",
                    "OK");
                return;
            }

            updateStatus = "Downloading " + asset.name + "...";
            updateDownloadAssetName = asset.name;
            updateDownloadRequest = UnityWebRequest.Get(asset.browser_download_url);
            updateDownloadRequest.SetRequestHeader("User-Agent", "Stories-Of-Yggdrasil-OSC-Unity-Tool/" + Version);
            updateDownloadRequest.SendWebRequest();
            EditorApplication.update -= PollUpdateDownload;
            EditorApplication.update += PollUpdateDownload;
            Repaint();
        }

        private void PollUpdateDownload()
        {
            if (updateDownloadRequest == null || !updateDownloadRequest.isDone)
                return;
            EditorApplication.update -= PollUpdateDownload;
            var request = updateDownloadRequest;
            updateDownloadRequest = null;
            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException(request.error);
                updatePendingSourceBytes = ExtractUpdatedScript(request.downloadHandler.data);
                if (updatePendingSourceBytes == null || updatePendingSourceBytes.Length == 0)
                    throw new InvalidDataException("The updater downloaded an empty source payload.");
                var checksumAsset = latestRelease != null && latestRelease.assets != null
                    ? latestRelease.assets.FirstOrDefault(item => item != null && item.name != null &&
                        (item.name.Equals(updateDownloadAssetName + ".sha256", StringComparison.OrdinalIgnoreCase) ||
                         (item.name.StartsWith(Path.GetFileNameWithoutExtension(updateDownloadAssetName), StringComparison.OrdinalIgnoreCase) &&
                          item.name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))))
                    : null;
                if (checksumAsset == null || string.IsNullOrWhiteSpace(checksumAsset.browser_download_url))
                    throw new InvalidDataException("The release does not contain the required SHA256 asset for " + updateDownloadAssetName + ".");
                updateStatus = "Verifying SHA256...";
                updateChecksumRequest = UnityWebRequest.Get(checksumAsset.browser_download_url);
                updateChecksumRequest.SetRequestHeader("User-Agent", "Stories-Of-Yggdrasil-OSC-Unity-Tool/" + Version + "-" + BuildNumber);
                updateChecksumRequest.SendWebRequest();
                EditorApplication.update -= PollUpdateChecksum;
                EditorApplication.update += PollUpdateChecksum;
            }
            catch (Exception exception)
            {
                updateStatus = "Update failed: " + exception.Message;
                EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
            }
            finally
            {
                request.Dispose();
                Repaint();
            }
        }

        private void PollUpdateChecksum()
        {
            if (updateChecksumRequest == null || !updateChecksumRequest.isDone)
                return;
            EditorApplication.update -= PollUpdateChecksum;
            var request = updateChecksumRequest;
            updateChecksumRequest = null;
            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException(request.error);
                var checksumText = request.downloadHandler.text ?? string.Empty;
                var checksumMatch = System.Text.RegularExpressions.Regex.Match(checksumText, @"[A-Fa-f0-9]{64}");
                var expected = checksumMatch.Success ? checksumMatch.Value.ToLowerInvariant() : string.Empty;
                var actual = ComputeSha256(updatePendingSourceBytes).ToLowerInvariant();
                if (expected.Length != 64 || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA256 mismatch. Expected " + expected + " but received " + actual + ".");
                Log("Updater SHA256 verified: " + actual);
                var bytes = updatePendingSourceBytes;
                updatePendingSourceBytes = null;
                InstallUpdatedScript(bytes);
            }
            catch (Exception exception)
            {
                updatePendingSourceBytes = null;
                updateStatus = "Update verification failed: " + exception.Message;
                EditorUtility.DisplayDialog("Stories OSC Updater", updateStatus, "OK");
            }
            finally
            {
                request.Dispose();
                Repaint();
            }
        }

        private byte[] ExtractUpdatedScript(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new InvalidDataException("The downloaded release asset was empty.");
            var isZip = payload.Length >= 4 && payload[0] == 0x50 && payload[1] == 0x4B;
            if (!isZip)
                return payload;

            using (var memory = new MemoryStream(payload))
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Read))
            {
                var entry = archive.Entries.FirstOrDefault(candidate =>
                    candidate.FullName.EndsWith("StoriesOfYggdrasilOSCContactSystem.cs", StringComparison.OrdinalIgnoreCase))
                    ?? archive.Entries.FirstOrDefault(candidate =>
                        candidate.FullName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                        candidate.FullName.IndexOf("StoriesOfYggdrasilOSCContactSystem", StringComparison.OrdinalIgnoreCase) >= 0);
                if (entry == null)
                    throw new InvalidDataException("The ZIP does not contain the Unity Editor script.");
                using (var stream = entry.Open())
                using (var output = new MemoryStream())
                {
                    stream.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        private void InstallUpdatedScript(byte[] sourceBytes)
        {
            var sourceText = System.Text.Encoding.UTF8.GetString(sourceBytes);
            if (sourceText.IndexOf("class StoriesOfYggdrasilOSCContactSystem", StringComparison.Ordinal) < 0 ||
                sourceText.IndexOf("private const string Version", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("The downloaded file is not a valid Stories OSC Unity tool script.");

            var monoScript = MonoScript.FromScriptableObject(this);
            var currentAssetPath = AssetDatabase.GetAssetPath(monoScript);
            if (string.IsNullOrWhiteSpace(currentAssetPath))
                throw new InvalidOperationException("Unity could not locate the current tool script.");

            EnsureAssetFolder(BackupRoot);
            var backupPath = AssetDatabase.GenerateUniqueAssetPath(
                BackupRoot + "/StoriesOfYggdrasilOSCContactSystem_v" + Version + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            File.WriteAllBytes(Path.GetFullPath(backupPath), File.ReadAllBytes(Path.GetFullPath(currentAssetPath)));
            File.WriteAllBytes(Path.GetFullPath(currentAssetPath), sourceBytes);
            updateStatus = "Update installed. Unity is recompiling the tool.";
            Debug.Log("[Stories OSC Updater] Installed update. Backup: " + backupPath);
            AssetDatabase.Refresh();
        }

        private static int CompareVersions(string left, string right)
        {
            var leftParts = ParseVersion(left);
            var rightParts = ParseVersion(right);
            var count = Math.Max(leftParts.Length, rightParts.Length);
            for (var index = 0; index < count; index++)
            {
                var a = index < leftParts.Length ? leftParts[index] : 0;
                var b = index < rightParts.Length ? rightParts[index] : 0;
                if (a != b)
                    return a.CompareTo(b);
            }
            return 0;
        }

        private static int CompareReleaseTags(string left, string right)
        {
            var versionCompare = CompareVersions(left, right);
            if (versionCompare != 0)
                return versionCompare;
            return TestBuildNumber(left).CompareTo(TestBuildNumber(right));
        }

        private static int TestBuildNumber(string value)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                value ?? string.Empty,
                @"(?:TB|Test[ _-]*Build)[._ -]*(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int number;
            return match.Success && int.TryParse(match.Groups[1].Value, out number) ? number : 0;
        }

        private static int[] ParseVersion(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .TrimStart('v', 'V')
                .Split('.')
                .Select(part =>
                {
                    var digits = new string(part.TakeWhile(char.IsDigit).ToArray());
                    return int.TryParse(digits, out var number) ? number : 0;
                })
                .ToArray();
        }

        private void DrawDiagnostics()
        {
            DrawHealthSafetyCard();
            BeginCard("SDK / Contact Diagnostics");
            var senderType = FindType(SenderTypeName);
            var receiverType = FindType(ReceiverTypeName);
            EditorGUILayout.LabelField("VRCContactSender", senderType != null ? "Available" : "NOT FOUND");
            EditorGUILayout.LabelField("VRCContactReceiver", receiverType != null ? "Available" : "NOT FOUND");

            if (avatarRoot != null && senderType != null && receiverType != null)
            {
                var senderCount = avatarRoot.GetComponentsInChildren(senderType, true).Length;
                var receiverCount = avatarRoot.GetComponentsInChildren(receiverType, true).Length;
                EditorGUILayout.LabelField("Contact Senders under avatar", senderCount.ToString());
                EditorGUILayout.LabelField("Contact Receivers under avatar", receiverCount.ToString());
                EditorGUILayout.LabelField("Total Contacts", (senderCount + receiverCount).ToString());
                var legacySpellReceivers = avatarRoot.GetComponentsInChildren(receiverType, true).Cast<Component>()
                    .Count(component => ReadStringMember(component, "parameter", "Parameter") == "SoY_SpellType" &&
                        ReadCollisionTags(component).Any(IsLegacySpellReceiverTag));
                var spellBusReceivers = avatarRoot.GetComponentsInChildren(receiverType, true).Cast<Component>()
                    .Count(component => ReadStringMember(component, "parameter", "Parameter") == SpellActiveParameter ||
                        ReadStringMember(component, "parameter", "Parameter").StartsWith(SpellBitParameterPrefix, StringComparison.Ordinal));
                EditorGUILayout.LabelField("Legacy broken spell receivers", legacySpellReceivers.ToString());
                var technickBusReceivers = avatarRoot.GetComponentsInChildren(receiverType, true).Cast<Component>()
                    .Count(component => ReadStringMember(component, "parameter", "Parameter") == TechnickActiveParameter ||
                        ReadStringMember(component, "parameter", "Parameter").StartsWith(TechnickBitParameterPrefix, StringComparison.Ordinal));
                var itemBusReceivers = avatarRoot.GetComponentsInChildren(receiverType, true).Cast<Component>()
                    .Count(component => ReadStringMember(component, "parameter", "Parameter") == ItemActiveParameter ||
                        ReadStringMember(component, "parameter", "Parameter").StartsWith(ItemBitParameterPrefix, StringComparison.Ordinal));
                EditorGUILayout.LabelField("Spell bus receivers", spellBusReceivers + " / 9");
                EditorGUILayout.LabelField("Technick bus receivers", technickBusReceivers + " / 10");
                EditorGUILayout.LabelField("Item bus receivers", itemBusReceivers + " / 10");
            }
            EditorGUILayout.HelpBox(
                "VRChat custom collision tags are case-sensitive. This tool uses the exact spacing/capitalization requested and keeps every generated contact below the 16-tag limit.",
                MessageType.Info);
            EndCard();

            BeginCard("Recent Tool Operations");
            if (operationLog.Count == 0)
                EditorGUILayout.LabelField("No operations yet.", wrappedLabel);
            foreach (var line in operationLog.Take(20))
                EditorGUILayout.LabelField("• " + line, wrappedLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Log"))
                operationLog.Clear();
            if (GUILayout.Button("Select Created / Reused Object") && Selection.activeGameObject != null)
                EditorGUIUtility.PingObject(Selection.activeGameObject);
            EditorGUILayout.EndHorizontal();
            EndCard();
        }

        private void DrawHealthSafetyCard()
        {
            BeginCard("Health-System Safety Lock");
            RefreshHealthAuditIfNeeded();

            if (fxController == null)
            {
                EditorGUILayout.HelpBox("Assign the avatar's FX Controller to audit its health system. Contact creation remains available without it.", MessageType.Info);
                EndCard();
                return;
            }

            var audit = cachedAudit ?? BuildHealthAudit(fxController);
            switch (audit.Kind)
            {
                case HealthSystemKind.Compatible:
                    EditorGUILayout.HelpBox(
                        "Existing compatible health system detected. SAFE MODE ACTIVE: Health, Healthbar, damage tiers, resistances, blocking logic, debuffs, layers, and transitions will not be edited.",
                        MessageType.Info);
                    break;
                case HealthSystemKind.Generic:
                    EditorGUILayout.HelpBox(
                        "An existing health-style system was detected. SAFE MODE ACTIVE: this tool will not add, replace, rename, or rewire health parameters/layers.",
                        MessageType.Warning);
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "No recognized health system was found. This version creates compatible outgoing contacts only; it does not silently install a replacement health engine.",
                        MessageType.Warning);
                    break;
            }

            EditorGUILayout.LabelField(audit.Summary, wrappedLabel);
            if (audit.HasLegacyPrototypeHooks)
                EditorGUILayout.HelpBox("Legacy prototype hook layers were detected. They are left untouched; the new Stories Of Yggdrasil hooks use the SoY_ parameter contract.", MessageType.Warning);
            EndCard();
        }

        private void DrawHealthSafetyMini()
        {
            RefreshHealthAuditIfNeeded();
            if (fxController == null)
                return;

            var kind = cachedAudit != null ? cachedAudit.Kind : HealthSystemKind.None;
            var text = kind == HealthSystemKind.Compatible
                ? "Compatible health system detected — Animator health logic is locked and untouched."
                : kind == HealthSystemKind.Generic
                    ? "Existing health logic detected — Animator health logic is locked and untouched."
                    : "No health logic detected — contacts can still be created, but this tool will not install health automatically.";
            EditorGUILayout.HelpBox(text, kind == HealthSystemKind.Compatible ? MessageType.Info : MessageType.Warning);
        }

        private void DrawTagRow(string left, string middle, string right)
        {
            var status = middle ?? string.Empty;
            var lower = status.ToLowerInvariant();
            if (!status.StartsWith("✓") && !status.StartsWith("!") && !status.StartsWith("✕"))
            {
                if (lower.Contains("ready") || lower.Contains("active") || lower.Contains("available") || lower.Contains("up to date"))
                    status = "✓ " + status;
                else if (lower.Contains("missing") || lower.Contains("not found") || lower.Contains("blocked") || lower.Contains("error"))
                    status = "✕ " + status;
                else if (lower.Contains("pending") || lower.Contains("warning") || lower.Contains("caution"))
                    status = "! " + status;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(left, GUILayout.Width(AccessibilityScale > 1f ? 105f : 82f));
            EditorGUILayout.SelectableLabel(status, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight * AccessibilityScale), GUILayout.Width(AccessibilityScale > 1f ? 300f : 260f));
            EditorGUILayout.LabelField(right, wrappedLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawShapeEditor(
            ref ContactShape shape,
            ref float radius,
            ref float height,
            ref Vector3 boxSize,
            ref Vector3 position,
            ref Vector3 rotation)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Contact Volume", cardTitleStyle);
            shape = (ContactShape)EditorGUILayout.EnumPopup("Shape", shape);
            switch (shape)
            {
                case ContactShape.Sphere:
                    radius = Mathf.Max(0.001f, EditorGUILayout.FloatField("Radius", radius));
                    break;
                case ContactShape.Capsule:
                    radius = Mathf.Max(0.001f, EditorGUILayout.FloatField("Radius", radius));
                    height = Mathf.Max(radius * 2f, EditorGUILayout.FloatField("Height", height));
                    break;
                case ContactShape.Box:
                    boxSize = ClampPositive(EditorGUILayout.Vector3Field("Size", boxSize));
                    break;
            }
            position = EditorGUILayout.Vector3Field("Local Position", position);
            rotation = EditorGUILayout.Vector3Field("Local Rotation", rotation);
        }

        private void DrawDraftControls(ContactDraftKind kind, bool canCreate, string previewLabel)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview, Place, Then Create", cardTitleStyle);

            var activeForThisKind = stagedPreview != null && stagedKind == kind;
            if (!activeForThisKind)
            {
                EditorGUILayout.HelpBox(
                    "Create a temporary collider preview first. Move, rotate, and resize it in the Scene view, then return here to finalize the real Contact.",
                    MessageType.Info);
                using (new EditorGUI.DisabledScope(!canCreate))
                {
                    if (GUILayout.Button(previewLabel, GUILayout.Height(34f)))
                        CreateStagedPreview(kind);
                }
                return;
            }

            EditorGUILayout.HelpBox(
                "Temporary preview active on '" + (stagedTarget != null ? stagedTarget.name : "Unknown") +
                "'. Use Unity's Move/Rotate tools and the Collider's Edit Collider handles before finalizing.",
                MessageType.Info);
            EditorGUILayout.ObjectField("Temporary Preview", stagedPreview, typeof(GameObject), true);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Preview", GUILayout.Height(30f)))
            {
                Selection.activeGameObject = stagedPreview;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            if (GUILayout.Button("Reset From Current Values", GUILayout.Height(30f)))
                ApplyCurrentGeometryToPreview(kind);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!canCreate || stagedPreview == null || stagedTarget == null))
            {
                if (GUILayout.Button("FINALIZE & CREATE CONTACT", GUILayout.Height(36f)))
                    FinalizeStagedPreview();
            }
            if (GUILayout.Button("Cancel Preview", GUILayout.Width(140f), GUILayout.Height(36f)))
                CancelStagedPreview(true);
            EditorGUILayout.EndHorizontal();
        }

        private void CreateStagedPreview(ContactDraftKind kind)
        {
            var target = explicitTarget != null ? explicitTarget : Selection.activeGameObject;
            if (target == null)
            {
                EditorUtility.DisplayDialog("Stories Of Yggdrasil OSC", "Select a target object first.", "OK");
                return;
            }

            CancelStagedPreview(false);
            stagedKind = kind;
            stagedTarget = target;
            stagedPreview = new GameObject(PreviewPrefix + " - " + kind);
            Undo.RegisterCreatedObjectUndo(stagedPreview, "Create Stories Contact Preview");
            stagedPreview.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            stagedPreview.transform.SetParent(target.transform, false);
            stagedPreview.transform.localScale = Vector3.one;
            ApplyCurrentGeometryToPreview(kind);

            Selection.activeGameObject = stagedPreview;
            SceneView.lastActiveSceneView?.FrameSelected();
            Log("Temporary " + kind + " preview created under '" + target.name + "'.");
        }

        private void ApplyCurrentGeometryToPreview(ContactDraftKind kind)
        {
            if (stagedPreview == null)
                return;

            ContactShape shape;
            float radius;
            float height;
            Vector3 boxSize;
            Vector3 position;
            Vector3 rotation;
            GetGeometry(kind, out shape, out radius, out height, out boxSize, out position, out rotation);

            Undo.RecordObject(stagedPreview.transform, "Update Stories Contact Preview");
            stagedPreview.transform.localPosition = position;
            stagedPreview.transform.localRotation = Quaternion.Euler(rotation);
            stagedPreview.transform.localScale = Vector3.one;

            foreach (var collider in stagedPreview.GetComponents<Collider>())
                Undo.DestroyObjectImmediate(collider);

            switch (shape)
            {
                case ContactShape.Sphere:
                    var sphere = Undo.AddComponent<SphereCollider>(stagedPreview);
                    sphere.isTrigger = true;
                    sphere.radius = Mathf.Max(0.001f, radius);
                    break;
                case ContactShape.Capsule:
                    var capsule = Undo.AddComponent<CapsuleCollider>(stagedPreview);
                    capsule.isTrigger = true;
                    capsule.direction = 1;
                    capsule.radius = Mathf.Max(0.001f, radius);
                    capsule.height = Mathf.Max(capsule.radius * 2f, height);
                    break;
                case ContactShape.Box:
                    var box = Undo.AddComponent<BoxCollider>(stagedPreview);
                    box.isTrigger = true;
                    box.size = ClampPositive(boxSize);
                    break;
            }

            EditorUtility.SetDirty(stagedPreview);
            SceneView.RepaintAll();
        }

        private void FinalizeStagedPreview()
        {
            if (stagedPreview == null || stagedTarget == null || stagedKind == ContactDraftKind.None)
                return;

            SyncGeometryFromPreview(stagedKind);
            var originalTarget = explicitTarget;
            var target = stagedTarget;
            var kind = stagedKind;
            explicitTarget = target;

            try
            {
                switch (kind)
                {
                    case ContactDraftKind.Attack:
                        CreateAttackSenders();
                        break;
                    case ContactDraftKind.Spell:
                        CreateSpellSenders();
                        break;
                    case ContactDraftKind.Technick:
                        CreateTechnickSenders();
                        break;
                    case ContactDraftKind.Item:
                        CreateItemSenders();
                        break;
                    case ContactDraftKind.Blocking:
                        CreateBlockSurfaces();
                        break;
                    case ContactDraftKind.Debuff:
                        CreateDebuffSenders();
                        break;
                    case ContactDraftKind.Incoming:
                        CreateIncomingReceivers();
                        break;
                }
            }
            finally
            {
                explicitTarget = originalTarget;
                CancelStagedPreview(false);
            }

            Log(kind + " contact finalized on '" + target.name + "'; temporary preview removed.");
        }

        private void CancelStagedPreview(bool log)
        {
            if (stagedPreview != null)
            {
                var name = stagedPreview.name;
                DestroyImmediate(stagedPreview);
                if (log)
                    Log("Cancelled and removed temporary preview '" + name + "'.");
            }
            stagedPreview = null;
            stagedTarget = null;
            stagedKind = ContactDraftKind.None;
            SceneView.RepaintAll();
        }

        private void SyncGeometryFromPreview(ContactDraftKind kind)
        {
            if (stagedPreview == null)
                return;

            var position = stagedPreview.transform.localPosition;
            var rotation = stagedPreview.transform.localEulerAngles;
            var scale = stagedPreview.transform.localScale;
            var absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            var sphere = stagedPreview.GetComponent<SphereCollider>();
            var capsule = stagedPreview.GetComponent<CapsuleCollider>();
            var box = stagedPreview.GetComponent<BoxCollider>();

            ContactShape shape = ContactShape.Sphere;
            float radius = 0.1f;
            float height = 0.2f;
            Vector3 boxSize = Vector3.one * 0.1f;

            if (sphere != null)
            {
                shape = ContactShape.Sphere;
                radius = sphere.radius * Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));
            }
            else if (capsule != null)
            {
                shape = ContactShape.Capsule;
                var axisScale = capsule.direction == 0 ? absScale.x : capsule.direction == 2 ? absScale.z : absScale.y;
                var radialScale = capsule.direction == 0
                    ? Mathf.Max(absScale.y, absScale.z)
                    : capsule.direction == 2
                        ? Mathf.Max(absScale.x, absScale.y)
                        : Mathf.Max(absScale.x, absScale.z);
                radius = capsule.radius * radialScale;
                height = capsule.height * axisScale;
            }
            else if (box != null)
            {
                shape = ContactShape.Box;
                boxSize = Vector3.Scale(box.size, absScale);
            }

            SetGeometry(kind, shape, radius, height, boxSize, position, rotation);
        }

        private void GetGeometry(
            ContactDraftKind kind,
            out ContactShape shape,
            out float radius,
            out float height,
            out Vector3 boxSize,
            out Vector3 position,
            out Vector3 rotation)
        {
            switch (kind)
            {
                case ContactDraftKind.Attack:
                    shape = attackShape; radius = attackRadius; height = attackHeight; boxSize = attackBoxSize;
                    position = attackPosition; rotation = attackRotation; return;
                case ContactDraftKind.Spell:
                    shape = spellShape; radius = spellRadius; height = spellHeight; boxSize = spellBoxSize;
                    position = spellPosition; rotation = spellRotation; return;
                case ContactDraftKind.Technick:
                    shape = technickShape; radius = technickRadius; height = technickHeight; boxSize = technickBoxSize;
                    position = technickPosition; rotation = technickRotation; return;
                case ContactDraftKind.Item:
                    shape = itemShape; radius = itemRadius; height = itemHeight; boxSize = itemBoxSize;
                    position = itemPosition; rotation = itemRotation; return;
                case ContactDraftKind.Blocking:
                    shape = blockShape; radius = blockRadius; height = blockHeight; boxSize = blockBoxSize;
                    position = blockPosition; rotation = blockRotation; return;
                case ContactDraftKind.Debuff:
                    shape = debuffShape; radius = debuffRadius; height = debuffHeight; boxSize = debuffBoxSize;
                    position = debuffPosition; rotation = debuffRotation; return;
                case ContactDraftKind.Incoming:
                    shape = incomingShape; radius = incomingRadius; height = incomingHeight; boxSize = incomingBoxSize;
                    position = incomingPosition; rotation = incomingRotation; return;
                default:
                    shape = ContactShape.Sphere; radius = 0.1f; height = 0.2f; boxSize = Vector3.one * 0.1f;
                    position = Vector3.zero; rotation = Vector3.zero; return;
            }
        }

        private void SetGeometry(
            ContactDraftKind kind,
            ContactShape shape,
            float radius,
            float height,
            Vector3 boxSize,
            Vector3 position,
            Vector3 rotation)
        {
            switch (kind)
            {
                case ContactDraftKind.Attack:
                    attackShape = shape; attackRadius = radius; attackHeight = height; attackBoxSize = boxSize;
                    attackPosition = position; attackRotation = rotation; break;
                case ContactDraftKind.Spell:
                    spellShape = shape; spellRadius = radius; spellHeight = height; spellBoxSize = boxSize;
                    spellPosition = position; spellRotation = rotation; break;
                case ContactDraftKind.Technick:
                    technickShape = shape; technickRadius = radius; technickHeight = height; technickBoxSize = boxSize;
                    technickPosition = position; technickRotation = rotation; break;
                case ContactDraftKind.Item:
                    itemShape = shape; itemRadius = radius; itemHeight = height; itemBoxSize = boxSize;
                    itemPosition = position; itemRotation = rotation; break;
                case ContactDraftKind.Blocking:
                    blockShape = shape; blockRadius = radius; blockHeight = height; blockBoxSize = boxSize;
                    blockPosition = position; blockRotation = rotation; break;
                case ContactDraftKind.Debuff:
                    debuffShape = shape; debuffRadius = radius; debuffHeight = height; debuffBoxSize = boxSize;
                    debuffPosition = position; debuffRotation = rotation; break;
                case ContactDraftKind.Incoming:
                    incomingShape = shape; incomingRadius = radius; incomingHeight = height; incomingBoxSize = boxSize;
                    incomingPosition = position; incomingRotation = rotation; break;
            }
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (stagedPreview == null)
                return;

            Handles.color = new Color(0.95f, 0.72f, 0.20f, 1f);
            var previousMatrix = Handles.matrix;
            Handles.matrix = stagedPreview.transform.localToWorldMatrix;

            var sphere = stagedPreview.GetComponent<SphereCollider>();
            var capsule = stagedPreview.GetComponent<CapsuleCollider>();
            var box = stagedPreview.GetComponent<BoxCollider>();

            if (sphere != null)
            {
                Handles.DrawWireDisc(sphere.center, Vector3.right, sphere.radius);
                Handles.DrawWireDisc(sphere.center, Vector3.up, sphere.radius);
                Handles.DrawWireDisc(sphere.center, Vector3.forward, sphere.radius);
            }
            else if (box != null)
            {
                Handles.DrawWireCube(box.center, box.size);
            }
            else if (capsule != null)
            {
                var radius = capsule.radius;
                var halfLine = Mathf.Max(0f, capsule.height * 0.5f - radius);
                var top = capsule.center + Vector3.up * halfLine;
                var bottom = capsule.center - Vector3.up * halfLine;
                Handles.DrawWireDisc(top, Vector3.up, radius);
                Handles.DrawWireDisc(bottom, Vector3.up, radius);
                Handles.DrawWireDisc(top, Vector3.right, radius);
                Handles.DrawWireDisc(bottom, Vector3.right, radius);
                Handles.DrawWireDisc(top, Vector3.forward, radius);
                Handles.DrawWireDisc(bottom, Vector3.forward, radius);
                Handles.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
                Handles.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
                Handles.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
                Handles.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
            }

            Handles.matrix = previousMatrix;
            Handles.Label(
                stagedPreview.transform.position,
                "TEMP " + stagedKind + " PREVIEW\nMove / Rotate / Edit Collider, then Finalize",
                EditorStyles.helpBox);
        }

        private static bool IsSafeFxCopy(AnimatorController controller)
        {
            if (controller == null)
                return false;
            var path = AssetDatabase.GetAssetPath(controller).Replace('\\', '/');
            return path.StartsWith(FxCopyRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        private bool EnsureSafeFxCopy(bool notify)
        {
            if (avatarDescriptor == null)
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "Assign the Avatar Descriptor first. The tool needs it to assign the safe FX copy.",
                    "OK");
                return false;
            }

            if (fxController == null)
                LoadFromAvatarDescriptor();
            if (fxController == null)
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "This avatar does not have an FX Animator Controller assigned.",
                    "OK");
                return false;
            }

            if (IsSafeFxCopy(fxController))
            {
                fxCopyPath = AssetDatabase.GetAssetPath(fxController);
                return true;
            }

            var sourcePath = AssetDatabase.GetAssetPath(fxController);
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "The selected FX controller is not a saved project asset and cannot be copied safely.",
                    "OK");
                return false;
            }

            EnsureAssetFolder(FxCopyRoot);
            var avatarName = MakeSafeAssetName(avatarDescriptor.gameObject.name);
            var controllerName = MakeSafeAssetName(fxController.name);
            var destination = AssetDatabase.GenerateUniqueAssetPath(
                FxCopyRoot + "/" + avatarName + "_" + controllerName + "_SoY_FX.controller");

            if (!AssetDatabase.CopyAsset(sourcePath, destination))
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "Unity could not copy the FX controller. No Animator changes were made.",
                    "OK");
                return false;
            }

            AssetDatabase.ImportAsset(destination);
            var copy = AssetDatabase.LoadAssetAtPath<AnimatorController>(destination);
            if (copy == null)
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "The FX copy was created but could not be loaded. No controller was assigned.",
                    "OK");
                return false;
            }

            var layers = avatarDescriptor.baseAnimationLayers;
            var fxIndex = Array.FindIndex(layers, layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX);
            if (fxIndex < 0)
            {
                EditorUtility.DisplayDialog(
                    "Stories Of Yggdrasil OSC",
                    "The Avatar Descriptor does not expose an FX animation layer.",
                    "OK");
                return false;
            }

            Undo.RecordObject(avatarDescriptor, "Assign Stories Of Yggdrasil FX Copy");
            var fxLayer = layers[fxIndex];
            fxLayer.isDefault = false;
            fxLayer.animatorController = copy;
            layers[fxIndex] = fxLayer;
            avatarDescriptor.baseAnimationLayers = layers;
            EditorUtility.SetDirty(avatarDescriptor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(avatarDescriptor);

            fxController = copy;
            fxCopyPath = destination;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshHealthAudit();
            Log("Created and assigned safe FX copy: " + destination);

            if (notify)
            {
                EditorUtility.DisplayDialog(
                    "Safe FX Copy Assigned",
                    "The original FX controller was not edited.\n\nWorking copy:\n" + destination +
                    "\n\nThis copy is now assigned to the avatar's FX layer.",
                    "OK");
            }
            return true;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            var normalized = folderPath.Replace('\\', '/');
            var parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string MakeSafeAssetName(string value)
        {
            var raw = string.IsNullOrWhiteSpace(value) ? "Avatar" : value.Trim();
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var cleaned = new string(raw.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "Avatar" : cleaned;
        }

        private void CreateAttackSenders()
        {
            var tags = GetAttackTags(attackTier).ToList();
            if (addBurnToAttack) tags.Add("Burn");
            if (addSilenceToAttack) tags.Add("Silence");
            if (addFreezeToAttack) tags.Add("Freeze");
            if (addBindToAttack) tags.Add("Bind");
            if (addBleedToAttack) tags.Add("Bleed");
            tags = tags.Distinct().Take(15).ToList();

            foreach (var target in GetTargets())
            {
                var host = attackCreateChild
                    ? CreateContactChild(target, "Stories Attack - " + attackTier, attackStartsEnabled)
                    : target;
                var allyHost = CreateContactChild(host, "[SoY Attack Ally] " + attackTier, true);
                var enemyHost = CreateContactChild(host, "[SoY Attack Enemy] " + attackTier, false);

                ConfigureAlignedDamageSender(allyHost, tags, CasterAllyTag,
                    attackShape, attackRadius, attackHeight, attackBoxSize, attackPosition, attackRotation);
                ConfigureAlignedDamageSender(enemyHost, tags, CasterEnemyTag,
                    attackShape, attackRadius, attackHeight, attackBoxSize, attackPosition, attackRotation);

                Selection.activeGameObject = host;
                Log("Attack sender ready on '" + host.name + "' with Ally/Enemy alignment and tags: " + string.Join(", ", tags));
            }
            RebuildSpellAlignmentLayer();
        }

        private void ConfigureAlignedDamageSender(
            GameObject host,
            IEnumerable<string> baseTags,
            string alignmentTag,
            ContactShape shape,
            float radius,
            float height,
            Vector3 boxSize,
            Vector3 position,
            Vector3 rotation)
        {
            var tags = (baseTags ?? Enumerable.Empty<string>())
                .Concat(new[] { alignmentTag })
                .Distinct()
                .Take(16)
                .ToList();
            var component = EnsureContact(host, FindType(SenderTypeName), tags, null);
            if (component == null)
                return;
            ConfigureContact(component, shape, radius, height, boxSize, position, rotation, tags);
            SetBoolMember(component, false, "localOnly", "LocalOnly");
            FinishContact(component);
        }

        private void CreateBlockSurfaces()
        {
            foreach (var target in GetTargets())
            {
                var host = blockCreateChild
                    ? CreateContactChild(target, "Compatible Block Surface", true)
                    : target;

                var receiver = EnsureContact(host, FindType(ReceiverTypeName), new[] { TagBlockable }, TagHitBlocked);
                if (receiver != null)
                {
                    ConfigureContact(receiver, blockShape, blockRadius, blockHeight, blockBoxSize, blockPosition, blockRotation, new[] { TagBlockable });
                    SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
                    SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
                    SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                    SetStringMember(receiver, TagHitBlocked, "parameter", "Parameter");
                    SetEnumMember(receiver, "Constant", "receiverType", "ReceiverType");
                    SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
                    FinishContact(receiver);
                    Log("Block receiver ready on '" + host.name + "': " + TagBlockable + " → " + TagHitBlocked);
                }

                if (bridgeBlockToOsc)
                {
                    var oscReceiver = EnsureContact(host, FindType(ReceiverTypeName), new[] { TagBlockable }, "SoY_HitBlocked");
                    if (oscReceiver != null)
                    {
                        ConfigureContact(oscReceiver, blockShape, blockRadius, blockHeight, blockBoxSize, blockPosition, blockRotation, new[] { TagBlockable });
                        SetBoolMember(oscReceiver, false, "allowSelf", "AllowSelf");
                        SetBoolMember(oscReceiver, true, "allowOthers", "AllowOthers");
                        SetBoolMember(oscReceiver, true, "localOnly", "LocalOnly");
                        SetStringMember(oscReceiver, "SoY_HitBlocked", "parameter", "Parameter");
                        SetEnumMember(oscReceiver, "Constant", "receiverType", "ReceiverType");
                        SetFloatMember(oscReceiver, 0f, "minVelocity", "MinVelocity");
                        FinishContact(oscReceiver);
                        Log("OSC block mirror ready on '" + host.name + "': " + TagBlockable + " → SoY_HitBlocked");
                    }
                }

                if (addLegacyBlockedSender)
                {
                    var sender = EnsureContact(host, FindType(SenderTypeName), new[] { TagHitBlocked }, null);
                    if (sender != null)
                    {
                        ConfigureContact(sender, blockShape, blockRadius, blockHeight, blockBoxSize, blockPosition, blockRotation, new[] { TagHitBlocked });
                        SetBoolMember(sender, false, "localOnly", "LocalOnly");
                        FinishContact(sender);
                        Log("Legacy block sender ready on '" + host.name + "' with tag: " + TagHitBlocked);
                    }
                }

                Selection.activeGameObject = host;
            }
        }

        private void CreateSpellSenders()
        {
            var spells = GetSpellsForSchool(spellSchool);
            if (spells.Length == 0)
                return;
            spellSelectionIndex = Mathf.Clamp(spellSelectionIndex, 0, spells.Length - 1);
            var spell = spells[spellSelectionIndex];

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Spell - " + spell.Id + " " + spell.Name, spellStartsEnabled);
                var allyHost = CreateContactChild(host, "[SoY Spell Ally] " + spell.Id + " " + spell.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Spell Enemy] " + spell.Id + " " + spell.Name, false);

                ConfigureSpellSender(allyHost, spell, CasterAllyTag);
                ConfigureSpellSender(enemyHost, spell, CasterEnemyTag);

                Selection.activeGameObject = host;
                Log("Spell sender ready on '" + host.name + "': " + spell.Name + " (ID " + spell.Id + ", bits " + GetSpellBinary(spell.Id) + ").");
            }

            RebuildSpellAlignmentLayer();
        }

        private void ConfigureSpellSender(GameObject host, SpellDefinition spell, string alignmentTag)
        {
            var tags = GetSpellBusTags(spell.Id).ToList();
            tags.Add(alignmentTag);
            tags.Add(GetSpellCategoryTag(spell.Category));

            var senderType = FindType(SenderTypeName);
            var component = host.GetComponents(senderType).Cast<Component>()
                .FirstOrDefault(existing => ReadCollisionTags(existing).Any(tag =>
                    tag == SpellActiveTag || IsLegacySpellReceiverTag(tag)));
            if (component == null)
                component = EnsureContact(host, senderType, tags, null);
            if (component == null)
                return;
            ConfigureContact(component, spellShape, spellRadius, spellHeight, spellBoxSize, spellPosition, spellRotation, tags);
            SetBoolMember(component, false, "localOnly", "LocalOnly");
            FinishContact(component);
        }


        private void CreateTechnickSenders()
        {
            if (TechnickDefinitions.Length == 0)
                return;
            technickSelectionIndex = Mathf.Clamp(technickSelectionIndex, 0, TechnickDefinitions.Length - 1);
            var technick = TechnickDefinitions[technickSelectionIndex];

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Technick - " + technick.Id + " " + technick.Name, technickStartsEnabled);
                var allyHost = CreateContactChild(host, "[SoY Technick Ally] " + technick.Id + " " + technick.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Technick Enemy] " + technick.Id + " " + technick.Name, false);
                ConfigureActionSender(allyHost, technick.Id, TechnickActiveTag, TechnickBitTagPrefix, "SoY Technick", CasterAllyTag,
                    technickShape, technickRadius, technickHeight, technickBoxSize, technickPosition, technickRotation);
                ConfigureActionSender(enemyHost, technick.Id, TechnickActiveTag, TechnickBitTagPrefix, "SoY Technick", CasterEnemyTag,
                    technickShape, technickRadius, technickHeight, technickBoxSize, technickPosition, technickRotation);
                Selection.activeGameObject = host;
                Log("Technick sender ready on '" + host.name + "': " + technick.Name + " (ID " + technick.Id + ", bits " + GetActionBinary(technick.Id) + ").");
            }
            RebuildSpellAlignmentLayer();
        }

        private void CreateItemSenders()
        {
            if (ItemDefinitions.Length == 0)
                return;
            itemSelectionIndex = Mathf.Clamp(itemSelectionIndex, 0, ItemDefinitions.Length - 1);
            var item = ItemDefinitions[itemSelectionIndex];

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Item - " + item.Id + " " + item.Name, itemStartsEnabled);
                var allyHost = CreateContactChild(host, "[SoY Item Ally] " + item.Id + " " + item.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Item Enemy] " + item.Id + " " + item.Name, false);
                ConfigureActionSender(allyHost, item.Id, ItemActiveTag, ItemBitTagPrefix, "SoY Item", CasterAllyTag,
                    itemShape, itemRadius, itemHeight, itemBoxSize, itemPosition, itemRotation);
                ConfigureActionSender(enemyHost, item.Id, ItemActiveTag, ItemBitTagPrefix, "SoY Item", CasterEnemyTag,
                    itemShape, itemRadius, itemHeight, itemBoxSize, itemPosition, itemRotation);
                Selection.activeGameObject = host;
                Log("Item sender ready on '" + host.name + "': " + item.Name + " (ID " + item.Id + ", bits " + GetActionBinary(item.Id) + ").");
            }
            RebuildSpellAlignmentLayer();
        }

        private void ConfigureActionSender(
            GameObject host, int actionId, string activeTag, string bitPrefix, string kindTag, string alignmentTag,
            ContactShape shape, float radius, float height, Vector3 boxSize, Vector3 position, Vector3 rotation)
        {
            var tags = GetActionBusTags(actionId, activeTag, bitPrefix).ToList();
            tags.Add(kindTag);
            tags.Add(alignmentTag);
            var component = EnsureContact(host, FindType(SenderTypeName), tags, null);
            if (component == null)
                return;
            ConfigureContact(component, shape, radius, height, boxSize, position, rotation, tags);
            SetBoolMember(component, false, "localOnly", "LocalOnly");
            FinishContact(component);
        }

        private void CreateDebuffSenders()
        {
            var tags = GetSelectedDebuffs().Distinct().Take(15).ToList();
            foreach (var target in GetTargets())
            {
                var host = debuffCreateChild
                    ? CreateContactChild(target, "Stories Debuff - " + string.Join(" + ", tags), debuffStartsEnabled)
                    : target;
                var label = string.Join(" + ", tags);
                var allyHost = CreateContactChild(host, "[SoY Debuff Ally] " + label, true);
                var enemyHost = CreateContactChild(host, "[SoY Debuff Enemy] " + label, false);
                ConfigureAlignedDamageSender(allyHost, tags, CasterAllyTag,
                    debuffShape, debuffRadius, debuffHeight, debuffBoxSize, debuffPosition, debuffRotation);
                ConfigureAlignedDamageSender(enemyHost, tags, CasterEnemyTag,
                    debuffShape, debuffRadius, debuffHeight, debuffBoxSize, debuffPosition, debuffRotation);
                Selection.activeGameObject = host;
                Log("Debuff sender ready on '" + host.name + "' with Ally/Enemy alignment and tags: " + label);
            }
            RebuildSpellAlignmentLayer();
        }

        private void CreateIncomingReceivers()
        {
            var damageMappings = new List<ReceiverMapping>();
            var spellMappings = new List<ReceiverMapping>();
            var technickMappings = new List<ReceiverMapping>();
            var itemMappings = new List<ReceiverMapping>();
            if (incomingHits)
            {
                damageMappings.Add(new ReceiverMapping(TagWeak, "SoY_HitWeak"));
                damageMappings.Add(new ReceiverMapping(TagAverage, "SoY_HitAverage"));
                damageMappings.Add(new ReceiverMapping(TagStrong, "SoY_HitStrong"));
                damageMappings.Add(new ReceiverMapping(TagCritical, "SoY_HitCritical"));
            }
            if (incomingDebuffs)
            {
                damageMappings.Add(new ReceiverMapping("Burn", "SoY_DebuffBurn"));
                damageMappings.Add(new ReceiverMapping("Silence", "SoY_DebuffSilence"));
                damageMappings.Add(new ReceiverMapping("Freeze", "SoY_DebuffFreeze"));
                damageMappings.Add(new ReceiverMapping("Bind", "SoY_DebuffBind"));
                damageMappings.Add(new ReceiverMapping("Bleed", "SoY_DebuffBleed"));
            }
            if (incomingHits || incomingDebuffs)
                damageMappings.Add(new ReceiverMapping(CasterEnemyTag, DamageSourceEnemyParameter));

            if (incomingSpells)
            {
                spellMappings.Add(new ReceiverMapping(SpellActiveTag, SpellActiveParameter));
                for (var bit = 0; bit < SpellBitCount; bit++)
                    spellMappings.Add(new ReceiverMapping(GetSpellBitTag(bit), GetSpellBitParameter(bit)));
                spellMappings.Add(new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
            }
            if (incomingTechnicks)
            {
                technickMappings.Add(new ReceiverMapping(TechnickActiveTag, TechnickActiveParameter));
                for (var bit = 0; bit < ActionBitCount; bit++)
                    technickMappings.Add(new ReceiverMapping(TechnickBitTagPrefix + bit, TechnickBitParameterPrefix + bit));
                technickMappings.Add(new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
            }
            if (incomingItems)
            {
                itemMappings.Add(new ReceiverMapping(ItemActiveTag, ItemActiveParameter));
                for (var bit = 0; bit < ActionBitCount; bit++)
                    itemMappings.Add(new ReceiverMapping(ItemBitTagPrefix + bit, ItemBitParameterPrefix + bit));
                itemMappings.Add(new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
            }

            foreach (var target in GetTargets())
            {
                GameObject lastHost = null;
                if (damageMappings.Count > 0)
                {
                    var damageHost = CreateContactChild(target, "Stories Incoming Damage Contacts", true);
                    foreach (var mapping in damageMappings)
                        ConfigureIncomingReceiver(damageHost, mapping);
                    lastHost = damageHost;
                }

                if (spellMappings.Count > 0)
                {
                    var spellHost = CreateContactChild(target, "Stories Incoming Spell Bus Contacts", true);
                    foreach (var mapping in spellMappings)
                        ConfigureIncomingReceiver(spellHost, mapping);
                    lastHost = spellHost;
                }

                if (technickMappings.Count > 0)
                {
                    var technickHost = CreateContactChild(target, "Stories Incoming Technick Bus Contacts", true);
                    foreach (var mapping in technickMappings)
                        ConfigureIncomingReceiver(technickHost, mapping);
                    lastHost = technickHost;
                }

                if (itemMappings.Count > 0)
                {
                    var itemHost = CreateContactChild(target, "Stories Incoming Item Bus Contacts", true);
                    foreach (var mapping in itemMappings)
                        ConfigureIncomingReceiver(itemHost, mapping);
                    lastHost = itemHost;
                }

                if (lastHost != null)
                    Selection.activeGameObject = lastHost;
            }

            if (incomingHits)
                RebuildIFrameLayer();
        }

        private void ConfigureIncomingReceiver(GameObject host, ReceiverMapping mapping)
        {
            var receiver = EnsureContact(host, FindType(ReceiverTypeName), new[] { mapping.Tag }, mapping.Parameter);
            if (receiver == null)
                return;
            ConfigureContact(receiver, incomingShape, incomingRadius, incomingHeight, incomingBoxSize, incomingPosition, incomingRotation, new[] { mapping.Tag });
            SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
            SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            SetStringMember(receiver, mapping.Parameter, "parameter", "Parameter");
            SetEnumMember(receiver, mapping.ReceiverType, "receiverType", "ReceiverType");
            SetFloatMember(receiver, mapping.Value, "value", "Value");
            SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
            FinishContact(receiver);
            Log("Incoming receiver ready on '" + host.name + "': " + mapping.Tag + " → " + mapping.Parameter + " = " + mapping.Value);
        }

        private Component EnsureContact(GameObject host, Type componentType, IEnumerable<string> tags, string receiverParameter)
        {
            if (host == null || componentType == null)
                return null;

            var expected = new HashSet<string>(tags ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            foreach (var existing in host.GetComponents(componentType).Cast<Component>())
            {
                var existingTags = new HashSet<string>(ReadCollisionTags(existing), StringComparer.Ordinal);
                var parameterMatches = string.IsNullOrEmpty(receiverParameter) ||
                                       string.Equals(ReadStringMember(existing, "parameter", "Parameter"), receiverParameter, StringComparison.Ordinal);
                if (expected.SetEquals(existingTags) && parameterMatches)
                {
                    Log("Reused existing " + componentType.Name + " on '" + host.name + "'.");
                    return existing;
                }
            }

            Undo.RecordObject(host, "Add Stories Of Yggdrasil Contact");
            var added = Undo.AddComponent(host, componentType) as Component;
            if (added == null)
            {
                Log("ERROR: Could not add " + componentType.FullName + " to '" + host.name + "'.");
                return null;
            }
            return added;
        }

        private Type FindParentConstraintType()
        {
            foreach (var name in ParentConstraintTypeNames)
            {
                var found = FindType(name);
                if (found != null) return found;
            }
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => { try { return assembly.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(type => type.Name == "VRCParentConstraint");
        }

        private void ApplyRaycastOriginAttachment(GameObject origin)
        {
            if (origin == null || weaponAttachmentTarget == null || contactAttachmentMode == ContactAttachmentMode.ContactObject)
                return;
            if (contactAttachmentMode == ContactAttachmentMode.WeaponRootTransform)
            {
                Undo.SetTransformParent(origin.transform, weaponAttachmentTarget.transform, "Attach Stories Raycast Origin To Weapon");
                if (!constraintMaintainOffset)
                {
                    Undo.RecordObject(origin.transform, "Reset Stories Raycast Origin Offset");
                    origin.transform.localPosition = Vector3.zero;
                    origin.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                ConfigureVRCParentConstraint(origin, weaponAttachmentTarget.transform);
            }
        }

        private bool ConfigureVRCParentConstraint(GameObject host, Transform source)
        {
            if (host == null || source == null) return false;
            var constraintType = FindParentConstraintType();
            if (constraintType == null)
            {
                Log("VRC Parent Constraint is unavailable. Update the VRChat SDK or use Weapon Root Transform mode.");
                return false;
            }

            var component = host.GetComponent(constraintType) as Component ?? Undo.AddComponent(host, constraintType) as Component;
            if (component == null) return false;
            Undo.RecordObject(component, "Configure Stories VRC Parent Constraint");

            var serialized = new SerializedObject(component);
            serialized.Update();
            SerializedProperty sources = null;
            var iterator = serialized.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String &&
                    iterator.name.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sources = iterator.Copy();
                    break;
                }
            }

            var sourceAssigned = false;
            if (sources != null)
            {
                sources.arraySize = 1;
                var element = sources.GetArrayElementAtIndex(0);
                if (element.propertyType == SerializedPropertyType.ObjectReference)
                {
                    element.objectReferenceValue = source;
                    sourceAssigned = true;
                }
                else
                {
                    var cursor = element.Copy();
                    var end = cursor.GetEndProperty();
                    var descend = true;
                    while (cursor.NextVisible(descend) && !SerializedProperty.EqualContents(cursor, end))
                    {
                        descend = false;
                        if (cursor.propertyType == SerializedPropertyType.ObjectReference &&
                            (cursor.name.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             cursor.displayName.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            cursor.objectReferenceValue = source;
                            sourceAssigned = true;
                        }
                        else if (cursor.propertyType == SerializedPropertyType.Float &&
                                 cursor.name.IndexOf("weight", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cursor.floatValue = Mathf.Clamp01(constraintWeight);
                        }
                    }
                }
            }

            serialized.ApplyModifiedProperties();
            SetFloatMember(component, Mathf.Clamp01(constraintWeight), "weight", "Weight", "globalWeight", "GlobalWeight");
            SetBoolMember(component, true, "isActive", "IsActive", "active", "Active");
            SetBoolMember(component, constraintMaintainOffset, "isLocked", "IsLocked", "locked", "Locked");
            InvokeNoArg(component, "ApplyConfigurationChanges");
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);

            if (!sourceAssigned)
                Log("VRC Parent Constraint was added, but its source list could not be configured automatically. Assign '" + source.name + "' in the Inspector.");
            return sourceAssigned;
        }

        private void ConfigureContact(
            Component component,
            ContactShape shape,
            float radius,
            float height,
            Vector3 boxSize,
            Vector3 position,
            Vector3 eulerRotation,
            IEnumerable<string> tags)
        {
            Undo.RecordObject(component, "Configure Stories Of Yggdrasil Contact");
            var contactRoot = component.transform;
            if (!suppressContactAttachment && weaponAttachmentTarget != null)
            {
                if (contactAttachmentMode == ContactAttachmentMode.WeaponRootTransform)
                    contactRoot = weaponAttachmentTarget.transform;
                else if (contactAttachmentMode == ContactAttachmentMode.VRCParentConstraint)
                    ConfigureVRCParentConstraint(component.gameObject, weaponAttachmentTarget.transform);
            }
            SetTransformMember(component, contactRoot, "rootTransform", "RootTransform");
            SetEnumMember(component, shape.ToString(), "shapeType", "ShapeType");
            SetFloatMember(component, Mathf.Clamp(radius, 0.001f, 3f), "radius", "Radius");
            SetFloatMember(component, Mathf.Clamp(height, 0.002f, 6f), "height", "Height");
            SetVector3Member(component, ClampSize(boxSize), "size", "Size");
            SetVector3Member(component, position, "position", "Position");
            SetQuaternionMember(component, Quaternion.Euler(eulerRotation), "rotation", "Rotation");
            SetCollisionTags(component, tags.Distinct().Take(16).ToArray());
        }

        private static void FinishContact(Component component)
        {
            InvokeNoArg(component, "ApplyConfigurationChanges");
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static GameObject CreateContactChild(GameObject parent, string baseName, bool enabled)
        {
            var existing = parent.transform.Cast<Transform>()
                .FirstOrDefault(t => t.name == baseName);
            if (existing != null)
            {
                if (existing.gameObject.activeSelf != enabled)
                {
                    Undo.RecordObject(existing.gameObject, "Set Contact Active State");
                    existing.gameObject.SetActive(enabled);
                }
                return existing.gameObject;
            }

            var child = new GameObject(baseName);
            Undo.RegisterCreatedObjectUndo(child, "Create Stories Of Yggdrasil Contact Object");
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            child.SetActive(enabled);
            return child;
        }

        private IEnumerable<GameObject> GetTargets()
        {
            if (explicitTarget != null)
                return new[] { explicitTarget };

            return Selection.gameObjects
                .Where(go => go != null)
                .Distinct()
                .ToArray();
        }

        private bool HasUsableTargets()
        {
            return explicitTarget != null || Selection.gameObjects.Any(go => go != null);
        }

        private static GameObject FindSelectionRoot()
        {
            var current = Selection.activeGameObject;
            if (current == null)
                return null;
            while (current.transform.parent != null)
                current = current.transform.parent.gameObject;
            return current;
        }

        private static SpellDefinition[] GetSpellsForSchool(SpellSchool school)
        {
            return SpellDefinitions
                .Where(spell => spell.School == school)
                .OrderBy(spell => spell.Id)
                .ToArray();
        }

        private static string GetSpellSchoolDisplayName(SpellSchool school)
        {
            switch (school)
            {
                case SpellSchool.WhiteMagick: return "White Magick";
                case SpellSchool.BlackMagick: return "Black Magick";
                case SpellSchool.GreenMagick: return "Green Magick";
                case SpellSchool.TimeMagick: return "Time Magick";
                case SpellSchool.ArcaneMagick: return "Arcane Magick";
                case SpellSchool.SynergistMagick: return "Synergist Magick";
                case SpellSchool.IllusionMagick: return "Illusion Magick";
                case SpellSchool.DreamMagick: return "Dream Magick";
                case SpellSchool.NatureMagick: return "Nature Magick";
                case SpellSchool.ChaosMagick: return "Chaos Magick";
                case SpellSchool.AbyssalCurses: return "Abyssal Curses";
                case SpellSchool.YggdrasilLightMagick: return "Yggdrasil Light";
                default: return school.ToString();
            }
        }

        private static string GetSpellSchoolAssetLabel(SpellSchool school)
        {
            return GetSpellSchoolDisplayName(school).Replace(" ", "_");
        }

        private bool IsIncomingSchoolEnabled(SpellSchool school)
        {
            switch (school)
            {
                case SpellSchool.WhiteMagick: return incomingWhiteSpells;
                case SpellSchool.BlackMagick: return incomingBlackSpells;
                case SpellSchool.GreenMagick: return incomingGreenSpells;
                case SpellSchool.TimeMagick: return incomingTimeSpells;
                case SpellSchool.ArcaneMagick: return incomingArcaneSpells;
                case SpellSchool.SynergistMagick: return incomingSynergistSpells;
                case SpellSchool.IllusionMagick: return incomingIllusionSpells;
                case SpellSchool.DreamMagick: return incomingDreamSpells;
                case SpellSchool.NatureMagick: return incomingNatureSpells;
                case SpellSchool.ChaosMagick: return incomingChaosSpells;
                case SpellSchool.AbyssalCurses: return incomingAbyssalSpells;
                case SpellSchool.YggdrasilLightMagick: return incomingYggdrasilLightSpells;
                default: return false;
            }
        }

        private bool AnyIncomingSpellSchoolEnabled()
        {
            return Enum.GetValues(typeof(SpellSchool))
                .Cast<SpellSchool>()
                .Any(IsIncomingSchoolEnabled);
        }

        private int GetIncomingSpellReceiverCount()
        {
            return SpellDefinitions
                .Where(spell => IsIncomingSchoolEnabled(spell.School))
                .Select(spell => spell.Id)
                .Distinct()
                .Count();
        }

        private void SetAllIncomingSpellSchools(bool value)
        {
            incomingWhiteSpells = value;
            incomingBlackSpells = value;
            incomingGreenSpells = value;
            incomingTimeSpells = value;
            incomingArcaneSpells = value;
            incomingSynergistSpells = value;
            incomingIllusionSpells = value;
            incomingDreamSpells = value;
            incomingNatureSpells = value;
            incomingChaosSpells = value;
            incomingAbyssalSpells = value;
            incomingYggdrasilLightSpells = value;
        }


        private static IEnumerable<string> GetActionBusTags(int actionId, string activeTag, string bitTagPrefix)
        {
            var safeId = Mathf.Clamp(actionId, 1, 255);
            yield return activeTag;
            for (var bit = 0; bit < ActionBitCount; bit++)
            {
                if ((safeId & (1 << bit)) != 0)
                    yield return bitTagPrefix + bit;
            }
        }

        private static string GetActionBinary(int actionId)
        {
            return Convert.ToString(Mathf.Clamp(actionId, 0, 255), 2).PadLeft(ActionBitCount, '0');
        }

        private static string GetSpellBitTag(int bit)
        {
            return SpellBitTagPrefix + bit;
        }

        private static string GetSpellBitParameter(int bit)
        {
            return SpellBitParameterPrefix + bit;
        }

        private static IEnumerable<string> GetSpellBusTags(int spellId)
        {
            var safeId = Mathf.Clamp(spellId, 1, 255);
            yield return SpellActiveTag;
            for (var bit = 0; bit < SpellBitCount; bit++)
            {
                if ((safeId & (1 << bit)) != 0)
                    yield return GetSpellBitTag(bit);
            }
        }

        private static string GetSpellBinary(int spellId)
        {
            return Convert.ToString(Mathf.Clamp(spellId, 0, 255), 2).PadLeft(SpellBitCount, '0');
        }

        private static string GetSpellCategoryTag(SpellCategory category)
        {
            switch (category)
            {
                case SpellCategory.Healing:
                case SpellCategory.Revival:
                    return "SoY Healing Spell";
                case SpellCategory.Cleanse:
                    return "SoY Cleanse Spell";
                case SpellCategory.Support:
                    return "SoY Support Spell";
                case SpellCategory.Status:
                    return "SoY Status Spell";
                case SpellCategory.Utility:
                    return "SoY Utility Spell";
                default:
                    return "SoY Offensive Spell";
            }
        }

        private static bool TryReadLegacySpellId(IEnumerable<string> tags, GameObject host, out int spellId)
        {
            foreach (var tag in tags ?? Enumerable.Empty<string>())
            {
                if (!tag.StartsWith(LegacySpellTagPrefix, StringComparison.Ordinal))
                    continue;
                if (int.TryParse(tag.Substring(LegacySpellTagPrefix.Length).Trim(), out spellId))
                    return spellId >= 1 && spellId <= 255;
            }

            var current = host != null ? host.transform : null;
            while (current != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(current.name, @"(?:Stories Spell -|\[SoY Spell (?:Ally|Enemy)\])\s*(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out spellId))
                    return spellId >= 1 && spellId <= 255;
                current = current.parent;
            }
            spellId = 0;
            return false;
        }

        private static SpellDefinition? FindSpellById(int spellId)
        {
            foreach (var spell in SpellDefinitions)
            {
                if (spell.Id == spellId)
                    return spell;
            }
            return null;
        }

        private void RepairSpellContactBus()
        {
            if (avatarRoot == null || !ContactTypesAvailable())
            {
                EditorUtility.DisplayDialog("Stories Of Yggdrasil OSC", "Assign the Avatar Root and make sure the VRChat Contacts SDK is available.", "OK");
                return;
            }
            if (fxController != null && !EnsureSafeFxCopy(true))
                return;

            if (fxController != null)
                AddMissingAnimatorParameters(fxController);
            if (expressionParameters != null)
            {
                Undo.RecordObject(expressionParameters, "Add Spell Bus Parameters");
                AddMissingExpressionParameters(expressionParameters);
                EditorUtility.SetDirty(expressionParameters);
            }

            var senderType = FindType(SenderTypeName);
            var receiverType = FindType(ReceiverTypeName);
            var repairedSenders = 0;
            var removedReceivers = 0;
            var busHosts = 0;

            foreach (var sender in avatarRoot.GetComponentsInChildren(senderType, true).Cast<Component>())
            {
                var oldTags = ReadCollisionTags(sender).ToList();
                if (!TryReadLegacySpellId(oldTags, sender.gameObject, out var spellId))
                    continue;

                var tags = GetSpellBusTags(spellId).ToList();
                var alignment = oldTags.FirstOrDefault(tag => tag == CasterAllyTag || tag == CasterEnemyTag);
                tags.Add(string.IsNullOrEmpty(alignment) ? CasterAllyTag : alignment);
                var category = oldTags.FirstOrDefault(tag => tag.StartsWith("SoY ", StringComparison.Ordinal) && tag.EndsWith(" Spell", StringComparison.Ordinal) && tag != SpellActiveTag);
                var definition = FindSpellById(spellId);
                tags.Add(!string.IsNullOrEmpty(category)
                    ? category
                    : definition.HasValue ? GetSpellCategoryTag(definition.Value.Category) : "SoY Offensive Spell");

                Undo.RecordObject(sender, "Repair Stories Spell Sender");
                SetCollisionTags(sender, tags.Distinct().Take(16).ToArray());
                FinishContact(sender);
                repairedSenders++;
            }

            var oldHosts = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name == "Stories Incoming Spell Contacts")
                .ToList();
            foreach (var oldHost in oldHosts)
            {
                foreach (var receiver in oldHost.GetComponents(receiverType).Cast<Component>().ToArray())
                {
                    var tags = ReadCollisionTags(receiver).ToList();
                    var parameter = ReadStringMember(receiver, "parameter", "Parameter");
                    if (parameter == "SoY_SpellType" ||
                        parameter == "SoY_HealingSourceEnemy" ||
                        tags.Any(IsLegacySpellReceiverTag))
                    {
                        Undo.DestroyObjectImmediate(receiver);
                        removedReceivers++;
                    }
                }

                var parent = oldHost.parent != null ? oldHost.parent.gameObject : avatarRoot;
                var busHost = CreateContactChild(parent, "Stories Incoming Spell Bus Contacts", true);
                ConfigureIncomingReceiver(busHost, new ReceiverMapping(SpellActiveTag, SpellActiveParameter));
                for (var bit = 0; bit < SpellBitCount; bit++)
                    ConfigureIncomingReceiver(busHost, new ReceiverMapping(GetSpellBitTag(bit), GetSpellBitParameter(bit)));
                ConfigureIncomingReceiver(busHost, new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
                busHosts++;
            }

            if (oldHosts.Count == 0)
            {
                var busHost = CreateContactChild(avatarRoot, "Stories Incoming Spell Bus Contacts", true);
                ConfigureIncomingReceiver(busHost, new ReceiverMapping(SpellActiveTag, SpellActiveParameter));
                for (var bit = 0; bit < SpellBitCount; bit++)
                    ConfigureIncomingReceiver(busHost, new ReceiverMapping(GetSpellBitTag(bit), GetSpellBitParameter(bit)));
                ConfigureIncomingReceiver(busHost, new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
                busHosts = 1;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var message = "Repaired " + repairedSenders + " spell sender(s), removed " + removedReceivers +
                          " legacy Int receiver(s), and prepared " + busHosts + " compact spell bus receiver host(s).";
            Log(message);
            EditorUtility.DisplayDialog(
                "Stories OSC Spell Bus Repair Complete",
                message + "\n\nDesktop v0.8.1 or newer is required to reconstruct the eight-bit spell ID.",
                "OK");
        }

        private IEnumerable<string> GetSelectedDebuffs()
        {
            if (debuffBurn) yield return "Burn";
            if (debuffSilence) yield return "Silence";
            if (debuffFreeze) yield return "Freeze";
            if (debuffBind) yield return "Bind";
            if (debuffBleed) yield return "Bleed";
        }

        private static IEnumerable<string> GetAttackTags(AttackTier tier)
        {
            switch (tier)
            {
                case AttackTier.Weak:
                    return new[] { TagWeak, TagBlockable };
                case AttackTier.Average:
                    return new[] { TagAverage, TagBlockable };
                case AttackTier.Strong:
                    return new[] { TagStrong, TagBlockable };
                case AttackTier.Critical:
                    return new[] { TagCritical };
                default:
                    return Array.Empty<string>();
            }
        }

        private void RebuildIFrameLayer()
        {
            if (avatarRoot == null || fxController == null || !EnsureSafeFxCopy(true))
            {
                Log("I-Frame layer was not rebuilt because the avatar root or safe FX copy is missing.");
                return;
            }

            var receiverType = FindType(ReceiverTypeName);
            if (receiverType == null)
                return;
            var hosts = avatarRoot.GetComponentsInChildren(receiverType, true)
                .Cast<Component>()
                .Where(component => ReadStringMember(component, "parameter", "Parameter").StartsWith("SoY_Hit", StringComparison.Ordinal))
                .Select(component => component.gameObject)
                .Distinct()
                .ToList();
            if (hosts.Count == 0)
                return;

            EnsureAssetFolder(AnimationRoot);
            var avatarName = MakeSafeAssetName(avatarRoot.name);
            var readyClip = CreateOrReplaceActiveClip(
                AnimationRoot + "/" + avatarName + "_SoY_IFrames_Ready.anim", hosts, true, 1f / 60f);
            var cooldownClip = CreateOrReplaceActiveClip(
                AnimationRoot + "/" + avatarName + "_SoY_IFrames_1s.anim", hosts, false, HitIFrameSeconds);

            RemoveLayerByName(fxController, IFrameLayer);
            var layer = CreateHookLayer(fxController, IFrameLayer);
            var ready = AddHookState(layer.stateMachine, "Ready", new Vector3(220f, 120f));
            var cooldown = AddHookState(layer.stateMachine, "Invincibility Frames (1s)", new Vector3(520f, 120f));
            ready.motion = readyClip;
            cooldown.motion = cooldownClip;
            layer.stateMachine.defaultState = ready;
            // Start local I-Frames only after Desktop/Sam.py accepts the hit.
            // Rejected Friendly fire never pulses SoY_Damaged.
            AddBoolTransition(ready, cooldown, "SoY_Damaged", true);
            var back = cooldown.AddTransition(ready);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0f;
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Rebuilt 1-second incoming hit I-Frame layer for " + hosts.Count + " receiver object(s).");
        }

        private void RepairExistingActionAlignment()
        {
            if (avatarRoot == null)
            {
                EditorUtility.DisplayDialog("Stories Of Yggdrasil OSC", "Select an avatar root first.", "OK");
                return;
            }
            var senderType = FindType(SenderTypeName);
            if (senderType == null)
                return;
            var repaired = 0;
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true).ToArray())
            {
                var isTechnick = transform.name.StartsWith("Stories Technick - ", StringComparison.Ordinal);
                var isItem = transform.name.StartsWith("Stories Item - ", StringComparison.Ordinal);
                var isAttack = transform.name.StartsWith("Stories Attack - ", StringComparison.Ordinal);
                var isDebuff = transform.name.StartsWith("Stories Debuff - ", StringComparison.Ordinal);
                if (!isTechnick && !isItem && !isAttack && !isDebuff)
                    continue;
                var directSenders = transform.GetComponents(senderType).Cast<Component>().ToArray();
                foreach (var sender in directSenders)
                {
                    var oldTags = ReadCollisionTags(sender).ToList();
                    if (oldTags.Contains(CasterAllyTag) || oldTags.Contains(CasterEnemyTag))
                        continue;
                    var label = transform.name.Substring(transform.name.IndexOf(" - ", StringComparison.Ordinal) + 3);
                    var kind = isTechnick ? "Technick" : isItem ? "Item" : isAttack ? "Attack" : "Debuff";
                    var allyHost = CreateContactChild(transform.gameObject, "[SoY " + kind + " Ally] " + label, true);
                    var enemyHost = CreateContactChild(transform.gameObject, "[SoY " + kind + " Enemy] " + label, false);
                    UnityEditorInternal.ComponentUtility.CopyComponent(sender);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(allyHost);
                    var allySender = allyHost.GetComponents(senderType).Cast<Component>().LastOrDefault();
                    UnityEditorInternal.ComponentUtility.CopyComponent(sender);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(enemyHost);
                    var enemySender = enemyHost.GetComponents(senderType).Cast<Component>().LastOrDefault();
                    if (allySender != null)
                    {
                        SetCollisionTags(allySender, oldTags.Concat(new[] { CasterAllyTag }).Distinct().Take(16).ToArray());
                        FinishContact(allySender);
                    }
                    if (enemySender != null)
                    {
                        SetCollisionTags(enemySender, oldTags.Concat(new[] { CasterEnemyTag }).Distinct().Take(16).ToArray());
                        FinishContact(enemySender);
                    }
                    Undo.DestroyObjectImmediate(sender);
                    repaired++;
                }
            }
            foreach (var host in avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Stories Incoming Technick Bus Contacts" || t.name == "Stories Incoming Item Bus Contacts"))
            {
                ConfigureIncomingReceiver(host.gameObject, new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
            }
            foreach (var host in avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Stories Incoming Damage Contacts"))
            {
                ConfigureIncomingReceiver(host.gameObject, new ReceiverMapping(CasterEnemyTag, DamageSourceEnemyParameter));
            }
            // Existing v0.5.5 and earlier avatars may still start I-Frames from raw hit
            // receivers. Rebuild the layer so only Sam.py-accepted SoY_Damaged pulses
            // can begin the one-second protection window.
            RebuildIFrameLayer();
            RebuildSpellAlignmentLayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Stories OSC Action Alignment Repair",
                "Repaired " + repaired + " legacy Attack/Debuff/Technick/Item sender(s). Ally and Enemy variants now follow SoY_IsEnemy.",
                "OK");
        }

        private void RebuildSpellAlignmentLayer()
        {
            if (avatarRoot == null || fxController == null || !EnsureSafeFxCopy(true))
            {
                Log("Action alignment layer was not rebuilt because the avatar root or safe FX copy is missing.");
                return;
            }

            var allyHosts = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                    transform.name.StartsWith("[SoY Spell Ally]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Technick Ally]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Item Ally]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Attack Ally]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Debuff Ally]", StringComparison.Ordinal))
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToList();
            var enemyHosts = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                    transform.name.StartsWith("[SoY Spell Enemy]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Technick Enemy]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Item Enemy]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Attack Enemy]", StringComparison.Ordinal) ||
                    transform.name.StartsWith("[SoY Debuff Enemy]", StringComparison.Ordinal))
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToList();
            if (allyHosts.Count == 0 && enemyHosts.Count == 0)
                return;

            EnsureAssetFolder(AnimationRoot);
            var avatarName = MakeSafeAssetName(avatarRoot.name);
            var allyClip = CreateOrReplaceAlignmentClip(
                AnimationRoot + "/" + avatarName + "_SoY_Action_Ally.anim", allyHosts, enemyHosts, false);
            var enemyClip = CreateOrReplaceAlignmentClip(
                AnimationRoot + "/" + avatarName + "_SoY_Action_Enemy.anim", allyHosts, enemyHosts, true);
            var koClip = CreateOrReplaceActiveClip(
                AnimationRoot + "/" + avatarName + "_SoY_Action_KO_Lock.anim", allyHosts.Concat(enemyHosts), false, 1f / 60f);

            RemoveLayerByName(fxController, SpellAlignmentLayer);
            var layer = CreateHookLayer(fxController, SpellAlignmentLayer);
            var ally = AddHookState(layer.stateMachine, "Ally Caster", new Vector3(220f, 120f));
            var enemy = AddHookState(layer.stateMachine, "Enemy Caster", new Vector3(520f, 120f));
            var koLocked = AddHookState(layer.stateMachine, "KO — Outgoing Contacts Disabled", new Vector3(370f, 300f));
            ally.motion = allyClip;
            enemy.motion = enemyClip;
            koLocked.motion = koClip;
            layer.stateMachine.defaultState = ally;
            AddBoolTransition(ally, enemy, "SoY_IsEnemy", true);
            AddBoolTransition(enemy, ally, "SoY_IsEnemy", false);
            AddBoolTransition(ally, koLocked, "SoY_KO", true);
            AddBoolTransition(enemy, koLocked, "SoY_KO", true);

            var reviveToAlly = koLocked.AddTransition(ally);
            reviveToAlly.hasExitTime = false; reviveToAlly.duration = 0f;
            reviveToAlly.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");
            reviveToAlly.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_IsEnemy");
            var reviveToEnemy = koLocked.AddTransition(enemy);
            reviveToEnemy.hasExitTime = false; reviveToEnemy.duration = 0f;
            reviveToEnemy.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");
            reviveToEnemy.AddCondition(AnimatorConditionMode.If, 0f, "SoY_IsEnemy");
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Rebuilt action alignment + KO lock layer for " + allyHosts.Count + " Ally/Enemy sender object(s).");
        }

        private AnimationClip CreateOrReplaceActiveClip(string path, IEnumerable<GameObject> objects, bool active, float length)
        {
            var clip = LoadOrCreateClip(path);
            clip.ClearCurves();
            foreach (var obj in objects)
            {
                var objectPath = GetRelativePath(avatarRoot.transform, obj.transform);
                if (objectPath == null)
                    continue;
                var binding = EditorCurveBinding.FloatCurve(objectPath, typeof(GameObject), "m_IsActive");
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(
                    new Keyframe(0f, active ? 1f : 0f),
                    new Keyframe(Mathf.Max(1f / 60f, length), active ? 1f : 0f)));
            }
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private AnimationClip CreateOrReplaceAlignmentClip(
            string path,
            IEnumerable<GameObject> allyObjects,
            IEnumerable<GameObject> enemyObjects,
            bool enemyMode)
        {
            var clip = LoadOrCreateClip(path);
            clip.ClearCurves();
            foreach (var pair in allyObjects.Select(obj => new KeyValuePair<GameObject, bool>(obj, !enemyMode))
                .Concat(enemyObjects.Select(obj => new KeyValuePair<GameObject, bool>(obj, enemyMode))))
            {
                var objectPath = GetRelativePath(avatarRoot.transform, pair.Key.transform);
                if (objectPath == null)
                    continue;
                var binding = EditorCurveBinding.FloatCurve(objectPath, typeof(GameObject), "m_IsActive");
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(
                    new Keyframe(0f, pair.Value ? 1f : 0f),
                    new Keyframe(1f / 60f, pair.Value ? 1f : 0f)));
            }
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip LoadOrCreateClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                return clip;
            clip = new AnimationClip { frameRate = 60f };
            AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(path));
            return clip;
        }

        private static void RemoveLayerByName(AnimatorController controller, string layerName)
        {
            for (var index = controller.layers.Length - 1; index >= 0; index--)
            {
                if (controller.layers[index].name == layerName)
                    controller.RemoveLayer(index);
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null)
                return null;
            if (root == target)
                return string.Empty;
            var names = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return current == root ? string.Join("/", names.ToArray()) : null;
        }

        private void InstallAllBridgeHooks()
        {
            if (fxController == null)
            {
                EditorUtility.DisplayDialog("Stories Of Yggdrasil OSC", "Select an FX Animator Controller first.", "OK");
                return;
            }
            if (!EnsureSafeFxCopy(true))
                return;

            Undo.RecordObject(fxController, "Install Stories Of Yggdrasil OSC Hooks");
            var parameterCount = AddMissingAnimatorParameters(fxController);
            var layerCount = EnsureHookLayers(fxController);
            EditorUtility.SetDirty(fxController);

            var expressionCount = 0;
            var compatibleExpressionCount = 0;
            if (expressionParameters != null)
            {
                Undo.RecordObject(expressionParameters, "Install Stories Of Yggdrasil Expression Parameters");
                compatibleExpressionCount = AddMissingCompatibleExpressionParameters(expressionParameters, fxController);
                expressionCount = AddMissingExpressionParameters(expressionParameters);
                EditorUtility.SetDirty(expressionParameters);
            }

            var menuCount = 0;
            if (expressionsMenu != null)
            {
                Undo.RecordObject(expressionsMenu, "Install Stories Of Yggdrasil Combat Toggle");
                menuCount = AddCombatToggle(expressionsMenu);
                EditorUtility.SetDirty(expressionsMenu);
            }

            if (avatarRoot != null)
            {
                RebuildIFrameLayer();
                RebuildSpellAlignmentLayer();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshHealthAudit();

            var summary = "Added " + parameterCount + " Animator parameter(s), " +
                          layerCount + " hook layer(s), " +
                          expressionCount + " SoY Expression parameter(s), " +
                          compatibleExpressionCount + " existing OSC binding(s), and " +
                          menuCount + " Stories RP sub-menu link(s).";
            operationLog.Insert(0, summary);
            EditorUtility.DisplayDialog(
                "Stories Of Yggdrasil OSC",
                summary + "\n\nOriginal FX preserved. Working copy:\n" + AssetDatabase.GetAssetPath(fxController),
                "OK");
        }

        private int AddMissingAnimatorParameters(AnimatorController target)
        {
            if (target == null)
                return 0;

            Undo.RecordObject(target, "Add Stories Of Yggdrasil Animator Parameters");
            var existing = target.parameters.ToDictionary(p => p.name, p => p.type, StringComparer.Ordinal);
            var added = 0;

            foreach (var spec in BridgeParameters)
            {
                AnimatorControllerParameterType existingType;
                if (existing.TryGetValue(spec.Name, out existingType))
                {
                    if (existingType != spec.AnimatorType)
                    {
                        operationLog.Insert(0,
                            "Skipped parameter '" + spec.Name + "': existing type is " + existingType +
                            ", expected " + spec.AnimatorType + ". Existing data was preserved.");
                    }
                    continue;
                }

                target.AddParameter(new AnimatorControllerParameter
                {
                    name = spec.Name,
                    type = spec.AnimatorType,
                    defaultBool = spec.DefaultValue > 0.5f,
                    defaultFloat = spec.DefaultValue,
                    defaultInt = Mathf.RoundToInt(spec.DefaultValue)
                });
                existing[spec.Name] = spec.AnimatorType;
                added++;
            }

            EditorUtility.SetDirty(target);
            return added;
        }

        private int AddMissingExpressionParameters(VRCExpressionParameters target)
        {
            if (target == null)
                return 0;

            var list = target.parameters != null
                ? target.parameters.ToList()
                : new List<VRCExpressionParameters.Parameter>();
            var added = 0;

            foreach (var spec in BridgeParameters)
            {
                var existing = list.FirstOrDefault(p => p != null && p.name == spec.Name);
                if (existing != null)
                {
                    if (existing.valueType != spec.ExpressionType)
                    {
                        operationLog.Insert(0,
                            "Skipped Expression parameter '" + spec.Name + "': existing type is " +
                            existing.valueType + ", expected " + spec.ExpressionType + ".");
                        continue;
                    }

                    // Repair flags from older installer versions.
                    // SoY_CombatEnabled and SoY_IsEnemy remain saved/synced.
                    // The installer also repairs the three action selector Ints to Synced so
                    // remote avatar instances can enter the same cast/action FX states.
                    var repaired = false;
                    if (!Mathf.Approximately(existing.defaultValue, spec.DefaultValue))
                    {
                        existing.defaultValue = spec.DefaultValue;
                        repaired = true;
                    }
                    if (existing.saved != spec.Saved)
                    {
                        existing.saved = spec.Saved;
                        repaired = true;
                    }
                    if (existing.networkSynced != spec.NetworkSynced)
                    {
                        if (spec.NetworkSynced && !existing.networkSynced &&
                            SyncedExpressionCost(list) + ExpressionParameterCost(existing.valueType) > 256)
                        {
                            operationLog.Insert(0,
                                "Skipped synchronizing Expression parameter '" + spec.Name +
                                "': enabling it would exceed VRChat's 256-bit parameter budget.");
                        }
                        else
                        {
                            existing.networkSynced = spec.NetworkSynced;
                            repaired = true;
                        }
                    }
                    if (repaired)
                        added++;
                    continue;
                }

                if (spec.NetworkSynced && SyncedExpressionCost(list) + ExpressionParameterCost(spec.ExpressionType) > 256)
                {
                    operationLog.Insert(0,
                        "Skipped Expression parameter '" + spec.Name + "': adding it would exceed VRChat's 256-bit parameter budget.");
                    continue;
                }

                list.Add(new VRCExpressionParameters.Parameter
                {
                    name = spec.Name,
                    valueType = spec.ExpressionType,
                    defaultValue = spec.DefaultValue,
                    saved = spec.Saved,
                    networkSynced = spec.NetworkSynced
                });
                added++;
            }

            target.parameters = list.ToArray();
            return added;
        }


        private static int CountCompatibleAnimatorParameters(AnimatorController controller)
        {
            if (controller == null)
                return 0;
            var parameters = controller.parameters.ToDictionary(p => p.name, p => p.type, StringComparer.Ordinal);
            return CompatibleOscParameters.Count(spec =>
                parameters.TryGetValue(spec.Name, out var type) && type == spec.AnimatorType);
        }

        private int AddMissingCompatibleExpressionParameters(VRCExpressionParameters target, AnimatorController controller)
        {
            if (target == null || controller == null)
                return 0;

            var animatorParameters = controller.parameters.ToDictionary(p => p.name, p => p.type, StringComparer.Ordinal);
            var list = target.parameters != null
                ? target.parameters.ToList()
                : new List<VRCExpressionParameters.Parameter>();
            var added = 0;

            foreach (var spec in CompatibleOscParameters)
            {
                if (!animatorParameters.TryGetValue(spec.Name, out var animatorType) || animatorType != spec.AnimatorType)
                    continue;

                var existing = list.FirstOrDefault(p => p != null && p.name == spec.Name);
                if (existing != null)
                {
                    if (existing.valueType != spec.ExpressionType)
                    {
                        operationLog.Insert(0,
                            "Skipped existing existing avatar Expression parameter '" + spec.Name +
                            "': type is " + existing.valueType + ", expected " + spec.ExpressionType + ".");
                    }
                    continue;
                }

                if (spec.NetworkSynced && SyncedExpressionCost(list) + ExpressionParameterCost(spec.ExpressionType) > 256)
                {
                    operationLog.Insert(0,
                        "Skipped existing avatar OSC parameter '" + spec.Name + "': adding it would exceed VRChat's 256-bit parameter budget.");
                    continue;
                }

                list.Add(new VRCExpressionParameters.Parameter
                {
                    name = spec.Name,
                    valueType = spec.ExpressionType,
                    defaultValue = spec.DefaultValue,
                    saved = false,
                    networkSynced = false
                });
                added++;
            }

            target.parameters = list.ToArray();
            return added;
        }

        private static int ExpressionParameterCost(VRCExpressionParameters.ValueType type)
        {
            return type == VRCExpressionParameters.ValueType.Bool ? 1 : 8;
        }

        private static int SyncedExpressionCost(IEnumerable<VRCExpressionParameters.Parameter> parameters)
        {
            // VRChat's 256-bit budget applies only to parameters marked Synced.
            // Unsynced local parameters still belong in the Expression Parameters
            // asset so OSC can read/write them, but they do not consume sync memory.
            return (parameters ?? Enumerable.Empty<VRCExpressionParameters.Parameter>())
                .Where(parameter => parameter != null && parameter.networkSynced)
                .Sum(parameter => ExpressionParameterCost(parameter.valueType));
        }

        private int AddCombatToggle(VRCExpressionsMenu menu)
        {
            if (menu == null)
                return 0;

            EnsureAssetFolder(MenuRoot);
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var mainPath = MenuRoot + "/" + avatarName + "_Stories_RP_Menu.asset";
            var statusPath = MenuRoot + "/" + avatarName + "_Stories_Status_Menu.asset";
            var spellsPath = MenuRoot + "/" + avatarName + "_Stories_Spells_Menu.asset";
            var coreSchoolsPath = MenuRoot + "/" + avatarName + "_Stories_Core_Schools.asset";
            var specializedSchoolsPath = MenuRoot + "/" + avatarName + "_Stories_Specialized_Schools.asset";
            var forbiddenSchoolsPath = MenuRoot + "/" + avatarName + "_Stories_Forbidden_Schools.asset";
            var schoolBrowsePath = MenuRoot + "/" + avatarName + "_Stories_Browse_By_School.asset";
            var purposePath = MenuRoot + "/" + avatarName + "_Stories_By_Purpose.asset";
            var quickPath = MenuRoot + "/" + avatarName + "_Stories_Quick_Access.asset";

            var storiesMenu = LoadOrCreateMenu(mainPath);
            var statusMenu = LoadOrCreateMenu(statusPath);
            var spellsMenu = LoadOrCreateMenu(spellsPath);
            var technicksMenu = BuildActionMenuPages(avatarName, "Technicks", "SoY_TechnickType", TechnickDefinitions);
            var itemsMenu = BuildActionMenuPages(avatarName, "Items", "SoY_ItemType", ItemDefinitions);
            var coreSchoolsMenu = LoadOrCreateMenu(coreSchoolsPath);
            var specializedSchoolsMenu = LoadOrCreateMenu(specializedSchoolsPath);
            var forbiddenSchoolsMenu = LoadOrCreateMenu(forbiddenSchoolsPath);
            var schoolBrowseMenu = LoadOrCreateMenu(schoolBrowsePath);
            var purposeMenu = LoadOrCreateMenu(purposePath);
            var quickMenu = LoadOrCreateMenu(quickPath);

            var schoolPages = Enum.GetValues(typeof(SpellSchool))
                .Cast<SpellSchool>()
                .ToDictionary(school => school, school => BuildSpellMenuPages(avatarName, school));
            var purposePages = Enum.GetValues(typeof(SpellCategory))
                .Cast<SpellCategory>()
                .ToDictionary(category => category, category => BuildSpellCategoryMenuPages(avatarName, category));

            BuildQuickAccessMenu(quickMenu);

            Undo.RecordObject(storiesMenu, "Build Stories RP Menu");
            storiesMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                CreateToggleControl("RP Combat", "SoY_CombatEnabled"),
                CreateToggleControl("Enemy Mode", "SoY_IsEnemy"),
                CreateButtonControl("Raycast Fire", RaycastFireParameter, 1f)
            };
            if (menuNavigationMode == MenuNavigationMode.FavoritesFirst && quickMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Quick Access", quickMenu));
            storiesMenu.controls.Add(CreateSubMenuControl("Spells", spellsMenu));
            if (menuNavigationMode != MenuNavigationMode.CompactCombat)
            {
                storiesMenu.controls.Add(CreateSubMenuControl("Technicks", technicksMenu));
                storiesMenu.controls.Add(CreateSubMenuControl("Items", itemsMenu));
            }
            else
            {
                var actionsMenu = LoadOrCreateMenu(MenuRoot + "/" + avatarName + "_Stories_Actions.asset");
                Undo.RecordObject(actionsMenu, "Build Stories Action Menu");
                actionsMenu.controls = new List<VRCExpressionsMenu.Control>
                {
                    CreateSubMenuControl("Technicks", technicksMenu),
                    CreateSubMenuControl("Items", itemsMenu)
                };
                EditorUtility.SetDirty(actionsMenu);
                storiesMenu.controls.Add(CreateSubMenuControl("Actions", actionsMenu));
            }
            storiesMenu.controls.Add(CreateSubMenuControl("Status Gauges", statusMenu));
            if (menuNavigationMode != MenuNavigationMode.FavoritesFirst && quickMenu.controls.Count > 0 && storiesMenu.controls.Count < 8)
                storiesMenu.controls.Add(CreateSubMenuControl("Quick Access", quickMenu));
            EditorUtility.SetDirty(storiesMenu);

            Undo.RecordObject(statusMenu, "Build Stories Status Menu");
            statusMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                CreateRadialControl("Mist Charge", "SoY_MistPercent"),
                CreateRadialControl("Curse Of Diablos", "SoY_DiablosPercent")
            };
            EditorUtility.SetDirty(statusMenu);

            BuildSchoolGroupMenu(
                coreSchoolsMenu,
                "Build Stories Core Spell Schools",
                schoolPages,
                new[]
                {
                    SpellSchool.WhiteMagick,
                    SpellSchool.BlackMagick,
                    SpellSchool.GreenMagick,
                    SpellSchool.TimeMagick,
                    SpellSchool.ArcaneMagick
                });

            BuildSchoolGroupMenu(
                specializedSchoolsMenu,
                "Build Stories Specialized Spell Schools",
                schoolPages,
                new[]
                {
                    SpellSchool.SynergistMagick,
                    SpellSchool.IllusionMagick,
                    SpellSchool.DreamMagick,
                    SpellSchool.NatureMagick
                });

            BuildSchoolGroupMenu(
                forbiddenSchoolsMenu,
                "Build Stories Forbidden Spell Schools",
                schoolPages,
                new[]
                {
                    SpellSchool.ChaosMagick,
                    SpellSchool.AbyssalCurses,
                    SpellSchool.YggdrasilLightMagick
                });

            Undo.RecordObject(schoolBrowseMenu, "Build Stories School Browser");
            schoolBrowseMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                CreateSubMenuControl("Core Magick", coreSchoolsMenu),
                CreateSubMenuControl("Specialized", specializedSchoolsMenu),
                CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu)
            };
            EditorUtility.SetDirty(schoolBrowseMenu);

            Undo.RecordObject(purposeMenu, "Build Stories Purpose Browser");
            purposeMenu.controls = new List<VRCExpressionsMenu.Control>();
            foreach (var category in Enum.GetValues(typeof(SpellCategory)).Cast<SpellCategory>())
            {
                VRCExpressionsMenu categoryPage;
                if (purposePages.TryGetValue(category, out categoryPage) && categoryPage != null)
                    purposeMenu.controls.Add(CreateSubMenuControl(GetSpellCategoryDisplayName(category), categoryPage));
            }
            EditorUtility.SetDirty(purposeMenu);

            Undo.RecordObject(spellsMenu, "Build Stories Spell Menu");
            spellsMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (menuNavigationMode == MenuNavigationMode.PurposeFirst)
            {
                spellsMenu.controls.Add(CreateSubMenuControl("By Purpose", purposeMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("By School", schoolBrowseMenu));
            }
            else if (menuNavigationMode == MenuNavigationMode.SchoolFirst)
            {
                spellsMenu.controls.Add(CreateSubMenuControl("Core Magick", coreSchoolsMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("Specialized", specializedSchoolsMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu));
            }
            else
            {
                spellsMenu.controls.Add(CreateSubMenuControl("By Purpose", purposeMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("Core Magick", coreSchoolsMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("Specialized", specializedSchoolsMenu));
                spellsMenu.controls.Add(CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu));
            }
            EditorUtility.SetDirty(spellsMenu);

            Undo.RecordObject(menu, "Add Stories RP Sub-Menu");
            if (menu.controls == null)
                menu.controls = new List<VRCExpressionsMenu.Control>();

            // Remove the old direct toggle created by v0.5.0 and earlier.
            menu.controls.RemoveAll(control =>
                control != null &&
                control.type == VRCExpressionsMenu.Control.ControlType.Toggle &&
                control.parameter != null &&
                control.parameter.name == "SoY_CombatEnabled");

            var existing = menu.controls.FirstOrDefault(control =>
                control != null && control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu == storiesMenu);
            if (existing == null)
            {
                if (menu.controls.Count >= 8)
                {
                    operationLog.Insert(0, "Stories RP sub-menu was not added because the selected root menu already has 8 controls.");
                    AssetDatabase.SaveAssets();
                    return 0;
                }
                menu.controls.Add(CreateSubMenuControl("Stories RP", storiesMenu));
            }
            else
            {
                existing.name = "Stories RP";
            }

            EditorUtility.SetDirty(menu);
            AssetDatabase.SaveAssets();
            operationLog.Insert(0, "Stories RP sub-menu created at " + mainPath + ".");
            return 1;
        }

        private static void BuildSchoolGroupMenu(
            VRCExpressionsMenu menu,
            string undoLabel,
            IDictionary<SpellSchool, VRCExpressionsMenu> schoolPages,
            IEnumerable<SpellSchool> schools)
        {
            if (menu == null)
                return;

            Undo.RecordObject(menu, undoLabel);
            menu.controls = new List<VRCExpressionsMenu.Control>();
            foreach (var school in schools)
            {
                VRCExpressionsMenu firstPage;
                if (schoolPages.TryGetValue(school, out firstPage) && firstPage != null)
                    menu.controls.Add(CreateSubMenuControl(GetSpellSchoolDisplayName(school), firstPage));
            }
            EditorUtility.SetDirty(menu);
        }

        private VRCExpressionsMenu BuildSpellMenuPages(string avatarName, SpellSchool school)
        {
            var spells = GetSpellsForSchool(school);
            return BuildSpellDefinitionPages(avatarName, GetSpellSchoolAssetLabel(school), spells);
        }

        private VRCExpressionsMenu BuildSpellCategoryMenuPages(string avatarName, SpellCategory category)
        {
            var spells = SpellDefinitions
                .Where(spell => spell.Category == category)
                .GroupBy(spell => spell.Id)
                .Select(group => group.First())
                .OrderBy(spell => spell.Id)
                .ToArray();
            return BuildSpellDefinitionPages(avatarName, "Purpose_" + category, spells);
        }

        private VRCExpressionsMenu BuildSpellDefinitionPages(string avatarName, string assetLabel, SpellDefinition[] spells)
        {
            if (spells == null || spells.Length == 0)
                return null;

            var pageSize = spells.Length > 8 ? 6 : 8;
            var pageCount = Mathf.CeilToInt(spells.Length / (float)pageSize);
            var pages = new List<VRCExpressionsMenu>();
            for (var page = 0; page < pageCount; page++)
            {
                var path = MenuRoot + "/" + avatarName + "_" + assetLabel + "_Page_" + (page + 1) + ".asset";
                pages.Add(LoadOrCreateMenu(path));
            }

            for (var page = 0; page < pages.Count; page++)
            {
                var targetMenu = pages[page];
                Undo.RecordObject(targetMenu, "Build Stories Spell Page");
                targetMenu.controls = new List<VRCExpressionsMenu.Control>();
                if (page > 0)
                    targetMenu.controls.Add(CreateSubMenuControl("◀ Prev " + page + "/" + pageCount, pages[page - 1]));
                foreach (var spell in spells.Skip(page * pageSize).Take(pageSize))
                {
                    targetMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = BuildSpellMenuLabel(spell),
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = "SoY_SpellType" },
                        value = spell.Id
                    });
                }
                if (page + 1 < pages.Count)
                    targetMenu.controls.Add(CreateSubMenuControl("Next " + (page + 2) + "/" + pageCount + " ▶", pages[page + 1]));
                EditorUtility.SetDirty(targetMenu);
            }

            return pages[0];
        }

        private VRCExpressionsMenu BuildActionMenuPages(
            string avatarName,
            string actionLabel,
            string parameterName,
            ActionDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
                return null;

            var safeLabel = MakeSafeAssetName(actionLabel);
            var pageSize = definitions.Length > 8 ? 6 : 8;
            var pageCount = Mathf.CeilToInt(definitions.Length / (float)pageSize);
            var pages = new List<VRCExpressionsMenu>();
            for (var page = 0; page < pageCount; page++)
            {
                var path = MenuRoot + "/" + avatarName + "_Stories_" + safeLabel + "_Page_" + (page + 1) + ".asset";
                pages.Add(LoadOrCreateMenu(path));
            }

            for (var page = 0; page < pages.Count; page++)
            {
                var targetMenu = pages[page];
                Undo.RecordObject(targetMenu, "Build Stories " + actionLabel + " Page");
                targetMenu.controls = new List<VRCExpressionsMenu.Control>();
                if (page > 0)
                    targetMenu.controls.Add(CreateSubMenuControl("◀ Prev " + page + "/" + pageCount, pages[page - 1]));
                foreach (var definition in definitions.Skip(page * pageSize).Take(pageSize))
                {
                    targetMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = BuildActionMenuLabel(definition.Name),
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = parameterName },
                        value = definition.Id
                    });
                }
                if (page + 1 < pages.Count)
                    targetMenu.controls.Add(CreateSubMenuControl("Next " + (page + 2) + "/" + pageCount + " ▶", pages[page + 1]));
                EditorUtility.SetDirty(targetMenu);
            }
            return pages[0];
        }

        private static VRCExpressionsMenu LoadOrCreateMenu(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
            if (existing != null)
                return existing;
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.controls = new List<VRCExpressionsMenu.Control>();
            AssetDatabase.CreateAsset(menu, AssetDatabase.GenerateUniqueAssetPath(path));
            return menu;
        }

        private static VRCExpressionsMenu.Control CreateButtonControl(string label, string parameter, float value)
        {
            return new VRCExpressionsMenu.Control
            {
                name = label,
                type = VRCExpressionsMenu.Control.ControlType.Button,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter },
                value = value
            };
        }

        private static VRCExpressionsMenu.Control CreateToggleControl(string name, string parameter)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter },
                value = 1f
            };
        }

        private static VRCExpressionsMenu.Control CreateSubMenuControl(string name, VRCExpressionsMenu subMenu)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = subMenu
            };
        }

        private static VRCExpressionsMenu.Control CreateRadialControl(string name, string parameter)
        {
            return new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                subParameters = new[]
                {
                    new VRCExpressionsMenu.Control.Parameter { name = parameter }
                }
            };
        }

        private int EnsureHookLayers(AnimatorController target)
        {
            if (target == null)
                return 0;

            Undo.RecordObject(target, "Add Stories Of Yggdrasil OSC Hook Layers");
            var added = 0;
            if (!target.layers.Any(layer => layer.name == CombatLayer))
            {
                AddCombatLayer(target);
                added++;
            }
            var existingVital = target.layers.FirstOrDefault(layer => layer.name == VitalLayer);
            var vitalNeedsUpgrade = existingVital == null || existingVital.stateMachine == null ||
                !existingVital.stateMachine.states.Any(child => child.state != null && child.state.name == "Full Health") ||
                !existingVital.stateMachine.states.Any(child => child.state != null && child.state.name == "Half Health");
            if (vitalNeedsUpgrade)
            {
                RemoveLayerByName(target, VitalLayer);
                AddVitalLayer(target);
                added++;
                operationLog.Insert(0, "Installed or upgraded the managed Full / Half / Critical / KO health-state layer.");
            }
            if (!target.layers.Any(layer => layer.name == ReactionLayer))
            {
                AddReactionLayer(target);
                added++;
            }
            if (!target.layers.Any(layer => layer.name == DiablosLayer))
            {
                AddDiablosLayer(target);
                added++;
            }
            EditorUtility.SetDirty(target);
            return added;
        }

        private static AnimatorControllerLayer CreateHookLayer(AnimatorController target, string name)
        {
            var machine = new AnimatorStateMachine
            {
                name = name,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(machine, target);
            return new AnimatorControllerLayer
            {
                name = name,
                defaultWeight = 1f,
                stateMachine = machine
            };
        }

        private static AnimatorState AddHookState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddCombatLayer(AnimatorController target)
        {
            var layer = CreateHookLayer(target, CombatLayer);
            var disabled = AddHookState(layer.stateMachine, "Combat Disabled", new Vector3(220f, 120f));
            var enabled = AddHookState(layer.stateMachine, "Combat Enabled", new Vector3(500f, 120f));
            layer.stateMachine.defaultState = disabled;
            AddBoolTransition(disabled, enabled, "SoY_CombatEnabled", true);
            AddBoolTransition(enabled, disabled, "SoY_CombatEnabled", false);
            target.AddLayer(layer);
        }

        private static void AddVitalLayer(AnimatorController target)
        {
            var layer = CreateHookLayer(target, VitalLayer);
            var full = AddHookState(layer.stateMachine, "Full Health", new Vector3(220f, 80f));
            var half = AddHookState(layer.stateMachine, "Half Health", new Vector3(500f, 80f));
            var critical = AddHookState(layer.stateMachine, "Critical HP", new Vector3(500f, 220f));
            var ko = AddHookState(layer.stateMachine, "KO", new Vector3(500f, 360f));
            layer.stateMachine.defaultState = full;

            AddAnyVitalTransition(layer.stateMachine, ko, true, null, null);
            AddAnyVitalTransition(layer.stateMachine, critical, false, null, 0.15f);
            AddAnyVitalTransition(layer.stateMachine, half, false, 0.1499f, 0.501f);
            AddAnyVitalTransition(layer.stateMachine, full, false, 0.499f, null);
            target.AddLayer(layer);
        }

        private static void AddAnyVitalTransition(
            AnimatorStateMachine machine,
            AnimatorState destination,
            bool ko,
            float? greaterThan,
            float? lessThan)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(ko ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "SoY_KO");
            if (greaterThan.HasValue)
                transition.AddCondition(AnimatorConditionMode.Greater, greaterThan.Value, "SoY_HPPercent");
            if (lessThan.HasValue)
                transition.AddCondition(AnimatorConditionMode.Less, lessThan.Value, "SoY_HPPercent");
        }

        private static void AddReactionLayer(AnimatorController target)
        {
            var layer = CreateHookLayer(target, ReactionLayer);
            var idle = AddHookState(layer.stateMachine, "Idle", new Vector3(150f, 210f));
            var weak = AddHookState(layer.stateMachine, "Weak Hit", new Vector3(470f, 10f));
            var average = AddHookState(layer.stateMachine, "Average Hit", new Vector3(470f, 90f));
            var strong = AddHookState(layer.stateMachine, "Strong Hit", new Vector3(470f, 170f));
            var critical = AddHookState(layer.stateMachine, "Critical Hit", new Vector3(470f, 250f));
            var blocked = AddHookState(layer.stateMachine, "Blocked", new Vector3(470f, 330f));
            var healing = AddHookState(layer.stateMachine, "Healing", new Vector3(470f, 410f));
            layer.stateMachine.defaultState = idle;

            AddReactionTransition(idle, weak, 1);
            AddReactionTransition(idle, average, 2);
            AddReactionTransition(idle, strong, 3);
            AddReactionTransition(idle, critical, 4);
            AddBoolTransition(idle, blocked, "SoY_Blocked", true);
            AddBoolTransition(idle, healing, "SoY_Healing", true);

            AddBoolTransition(weak, idle, "SoY_Damaged", false);
            AddBoolTransition(average, idle, "SoY_Damaged", false);
            AddBoolTransition(strong, idle, "SoY_Damaged", false);
            AddBoolTransition(critical, idle, "SoY_Damaged", false);
            AddBoolTransition(blocked, idle, "SoY_Blocked", false);
            AddBoolTransition(healing, idle, "SoY_Healing", false);
            target.AddLayer(layer);
        }

        private static void AddDiablosLayer(AnimatorController target)
        {
            var layer = CreateHookLayer(target, DiablosLayer);
            var clear = AddHookState(layer.stateMachine, "No Warning", new Vector3(150f, 200f));
            var warning25 = AddHookState(layer.stateMachine, "Warning 25%", new Vector3(470f, 40f));
            var warning50 = AddHookState(layer.stateMachine, "Warning 50%", new Vector3(470f, 130f));
            var warning90 = AddHookState(layer.stateMachine, "Warning 90%", new Vector3(470f, 220f));
            var warning98 = AddHookState(layer.stateMachine, "Warning 98%", new Vector3(470f, 310f));
            layer.stateMachine.defaultState = clear;

            AddAnyDiablosTransition(layer.stateMachine, clear, false, null, null);
            AddAnyDiablosTransition(layer.stateMachine, clear, true, null, 0.25f);
            AddAnyDiablosTransition(layer.stateMachine, warning98, true, 0.979f, null);
            AddAnyDiablosTransition(layer.stateMachine, warning90, true, 0.899f, 0.98f);
            AddAnyDiablosTransition(layer.stateMachine, warning50, true, 0.499f, 0.90f);
            AddAnyDiablosTransition(layer.stateMachine, warning25, true, 0.249f, 0.50f);
            target.AddLayer(layer);
        }

        private static void AddAnyDiablosTransition(
            AnimatorStateMachine machine,
            AnimatorState destination,
            bool applicable,
            float? greaterThan,
            float? lessThan)
        {
            var transition = machine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(applicable ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "SoY_DiablosApplicable");
            if (greaterThan.HasValue)
                transition.AddCondition(AnimatorConditionMode.Greater, greaterThan.Value, "SoY_DiablosPercent");
            if (lessThan.HasValue)
                transition.AddCondition(AnimatorConditionMode.Less, lessThan.Value, "SoY_DiablosPercent");
        }

        private static void AddReactionTransition(AnimatorState source, AnimatorState destination, int reaction)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "SoY_Damaged");
            transition.AddCondition(AnimatorConditionMode.Equals, reaction, "SoY_DamageReaction");
        }

        private static void AddBoolTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
            bool value,
            string secondParameter = null,
            bool secondValue = false)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
            if (!string.IsNullOrEmpty(secondParameter))
            {
                transition.AddCondition(secondValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, secondParameter);
            }
        }

        private void FinishAnimatorAssetChange(string label, int added)
        {
            if (fxController != null)
                EditorUtility.SetDirty(fxController);
            SaveAndLog(label, added);
            RefreshHealthAudit();
        }

        private void SaveAndLog(string label, int added)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            operationLog.Insert(0, label + ": added " + added + " missing item(s). Existing items were preserved.");
            Repaint();
        }

        private void RefreshHealthAuditIfNeeded()
        {
            if (cachedAudit == null)
                RefreshHealthAudit();
        }

        private void RefreshHealthAudit()
        {
            cachedAudit = fxController != null ? BuildHealthAudit(fxController) : null;
            Repaint();
        }

        private static HealthAudit BuildHealthAudit(AnimatorController controller)
        {
            var audit = new HealthAudit();
            if (controller == null)
            {
                audit.Kind = HealthSystemKind.None;
                audit.Summary = "No FX controller assigned.";
                return audit;
            }

            var parameters = controller.parameters.ToDictionary(p => p.name, p => p.type, StringComparer.Ordinal);
            var layers = controller.layers.Select(l => l.name).ToList();
            audit.Layers.AddRange(layers);

            var compatibleCore = new Dictionary<string, AnimatorControllerParameterType>
            {
                { "Health", AnimatorControllerParameterType.Float },
                { "Healthbar", AnimatorControllerParameterType.Bool },
                { "Damage Value", AnimatorControllerParameterType.Int },
                { "Hit Blocked", AnimatorControllerParameterType.Bool },
                { "DoT Burn", AnimatorControllerParameterType.Bool },
                { "DoT Bleed", AnimatorControllerParameterType.Bool },
                { "Suppress Silence", AnimatorControllerParameterType.Bool },
                { "Slow Freeze", AnimatorControllerParameterType.Bool },
                { "Slow Bind", AnimatorControllerParameterType.Bool }
            };

            foreach (var pair in compatibleCore)
            {
                if (parameters.TryGetValue(pair.Key, out var type) && type == pair.Value)
                    audit.Found.Add(pair.Key);
                else
                    audit.Missing.Add(pair.Key);
            }

            var tierFamilies = new[]
            {
                "Hit By Weak Attack T", "Hit By Average Attack T", "Hit By Strong Attack T", "Hit By Critical Attack T"
            };
            var tierHits = 0;
            foreach (var family in tierFamilies)
            {
                var familyFound = Enumerable.Range(0, 4).All(i => parameters.ContainsKey(family + i));
                if (familyFound)
                {
                    audit.Found.Add(family + "0-3");
                    tierHits++;
                }
                else
                {
                    audit.Missing.Add(family + "0-3");
                }
            }

            var compatibleScore = audit.Found.Count + tierHits * 2;
            var genericHealth = parameters.Keys.Any(n =>
                n.Equals("Health", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("HP", StringComparison.OrdinalIgnoreCase) ||
                n.IndexOf("Healthbar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("CurrentHealth", StringComparison.OrdinalIgnoreCase) >= 0);

            if (compatibleScore >= 9 && tierHits >= 3)
                audit.Kind = HealthSystemKind.Compatible;
            else if (genericHealth)
                audit.Kind = HealthSystemKind.Generic;
            else
                audit.Kind = HealthSystemKind.None;

            audit.HasLegacyPrototypeHooks = layers.Any(n => n.StartsWith("OoY OSC |", StringComparison.Ordinal)) ||
                                      parameters.Keys.Any(n => n.StartsWith("OoY_", StringComparison.Ordinal));

            audit.Summary = audit.Kind == HealthSystemKind.Compatible
                ? "Detected compatible health-system signatures: " + string.Join(", ", audit.Found.Take(12)) + "."
                : audit.Kind == HealthSystemKind.Generic
                    ? "Detected a health-style parameter set, but not the full compatible signature."
                    : "No recognized Health/HP signature detected in this controller.";
            return audit;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Ignore reflection-only or partially loaded assemblies.
                }
            }
            return Type.GetType(fullName, false);
        }

        private static bool ContactTypesAvailable()
        {
            return FindType(SenderTypeName) != null && FindType(ReceiverTypeName) != null;
        }

        private static MemberInfo FindMember(Type type, params string[] names)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var name in names)
            {
                var field = type.GetField(name, flags);
                if (field != null)
                    return field;
                var property = type.GetProperty(name, flags);
                if (property != null && property.CanWrite)
                    return property;
            }
            return null;
        }

        private static object ReadMember(Component component, params string[] names)
        {
            if (component == null)
                return null;
            var member = FindMember(component.GetType(), names);
            if (member is FieldInfo field)
                return field.GetValue(component);
            if (member is PropertyInfo property && property.CanRead)
                return property.GetValue(component, null);
            return null;
        }

        private static bool WriteMember(Component component, object value, params string[] names)
        {
            if (component == null)
                return false;
            var member = FindMember(component.GetType(), names);
            try
            {
                if (member is FieldInfo field)
                {
                    field.SetValue(component, ConvertValue(value, field.FieldType));
                    return true;
                }
                if (member is PropertyInfo property && property.CanWrite)
                {
                    property.SetValue(component, ConvertValue(value, property.PropertyType), null);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Could not set member on " + component.GetType().Name + ": " + exception.Message);
            }
            return false;
        }

        private static object ConvertValue(object value, Type destinationType)
        {
            if (value == null)
                return null;
            if (destinationType.IsInstanceOfType(value))
                return value;
            if (destinationType.IsEnum)
                return Enum.Parse(destinationType, value.ToString(), true);
            return Convert.ChangeType(value, destinationType);
        }

        private static void SetEnumMember(Component component, string enumName, params string[] names)
        {
            var member = FindMember(component.GetType(), names);
            var enumType = member is FieldInfo field ? field.FieldType : (member as PropertyInfo)?.PropertyType;
            if (enumType == null || !enumType.IsEnum)
                return;
            try
            {
                WriteMember(component, Enum.Parse(enumType, enumName, true), names);
            }
            catch
            {
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Enum value '" + enumName + "' is not available on " + component.GetType().Name + ".");
            }
        }

        private static void SetCollisionTags(Component component, string[] tags)
        {
            var member = FindMember(component.GetType(), "collisionTags", "CollisionTags");
            if (member is FieldInfo field)
            {
                field.SetValue(component, BuildStringCollection(field.FieldType, tags));
            }
            else if (member is PropertyInfo property && property.CanWrite)
            {
                property.SetValue(component, BuildStringCollection(property.PropertyType, tags), null);
            }

            var update = component.GetType().GetMethod(
                "UpdateCollisionTags",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string[]) },
                null);
            if (update != null)
            {
                try { update.Invoke(component, new object[] { tags }); }
                catch { /* Editor serialization above is still retained. */ }
            }
        }

        private static object BuildStringCollection(Type type, string[] tags)
        {
            if (type == typeof(string[]))
                return tags;
            if (typeof(IList).IsAssignableFrom(type))
            {
                var list = Activator.CreateInstance(type) as IList;
                if (list != null)
                {
                    foreach (var tag in tags)
                        list.Add(tag);
                    return list;
                }
            }
            return tags.ToList();
        }

        private static IEnumerable<string> ReadCollisionTags(Component component)
        {
            var raw = ReadMember(component, "collisionTags", "CollisionTags");
            if (raw is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                        yield return item.ToString();
                }
            }
        }

        private static string ReadStringMember(Component component, params string[] names)
        {
            return ReadMember(component, names)?.ToString() ?? string.Empty;
        }

        private static void SetBoolMember(Component component, bool value, params string[] names) => WriteMember(component, value, names);
        private static void SetFloatMember(Component component, float value, params string[] names) => WriteMember(component, value, names);
        private static void SetStringMember(Component component, string value, params string[] names) => WriteMember(component, value, names);
        private static void SetVector3Member(Component component, Vector3 value, params string[] names) => WriteMember(component, value, names);
        private static void SetQuaternionMember(Component component, Quaternion value, params string[] names) => WriteMember(component, value, names);
        private static void SetTransformMember(Component component, Transform value, params string[] names) => WriteMember(component, value, names);

        private static void InvokeNoArg(Component component, string methodName)
        {
            if (component == null)
                return;
            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method == null)
                return;
            try { method.Invoke(component, null); }
            catch { /* Safe to ignore in editor; serialized fields remain set. */ }
        }

        private static Vector3 ClampPositive(Vector3 value)
        {
            return new Vector3(Mathf.Max(0.001f, value.x), Mathf.Max(0.001f, value.y), Mathf.Max(0.001f, value.z));
        }

        private static Vector3 ClampSize(Vector3 value)
        {
            return new Vector3(
                Mathf.Clamp(Mathf.Abs(value.x), 0.001f, 6f),
                Mathf.Clamp(Mathf.Abs(value.y), 0.001f, 6f),
                Mathf.Clamp(Mathf.Abs(value.z), 0.001f, 6f));
        }

        private void Log(string message)
        {
            operationLog.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            Debug.Log("[Stories Of Yggdrasil OSC Contact System] " + message);
            Repaint();
        }

        private static void BeginCard(string title)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);
        }

        private static void EndCard()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }
    }
}
#endif
