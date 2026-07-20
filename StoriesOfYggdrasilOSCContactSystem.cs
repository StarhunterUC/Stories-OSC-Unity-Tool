#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
    /// one-second incoming-hit I-Frames, status gauges, and hook layers.
    /// </summary>
    public sealed class StoriesOfYggdrasilOSCContactSystem : EditorWindow
    {
        private const string Version = "0.5.3";
        private const string SenderTypeName = "VRC.SDK3.Dynamics.Contact.Components.VRCContactSender";
        private const string ReceiverTypeName = "VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver";

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
            OutgoingContacts,
            IncomingReceivers,
            AnimatorSetup,
            Help
        }

        private enum OutgoingContactKind
        {
            Attack,
            Spell,
            Blocking,
            Debuff
        }

        private enum ContactDraftKind
        {
            None,
            Attack,
            Spell,
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
        private const string FxCopyRoot = "Assets/Stories Of Yggdrasil/FX";
        private const string MenuRoot = "Assets/Stories Of Yggdrasil/Menus";
        private const string AnimationRoot = "Assets/Stories Of Yggdrasil/Animations";
        private const string BackupRoot = "Assets/Stories Of Yggdrasil/Backups/Unity Tool";
        private const string PreviewPrefix = "[TEMP] Stories Contact Preview";
        // v0.5.3 spell contact transport.
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
        private const string GitHubRepository = "StarhunterUC/Stories-OSC-Unity-Tool";
        private const string GitHubLatestReleaseApi = "https://api.github.com/repos/StarhunterUC/Stories-OSC-Unity-Tool/releases/latest";
        private const string GitHubRepositoryUrl = "https://github.com/StarhunterUC/Stories-OSC-Unity-Tool";
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

        // These are bridge hooks only. Existing Health/HP parameters and health layers are deliberately absent.
        private static readonly ParameterSpec[] BridgeParameters =
        {
            new ParameterSpec("SoY_CombatEnabled", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, true, true),
            new ParameterSpec("SoY_IsEnemy", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, true, true),
            new ParameterSpec("SoY_SpellType", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(SpellActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(SpellBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HealingSourceEnemy", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
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

        private StudioTab tab;
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
        private GitHubReleaseInfo latestRelease;
        private string updateStatus = "Not checked";
        private bool updateAvailable;
        private bool updateCheckWasManual;

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
            wantsMouseMove = true;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
            RefreshHealthAudit();
            EditorApplication.delayCall += AutoCheckForUpdates;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.update -= PollUpdateCheck;
            EditorApplication.update -= PollUpdateDownload;
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
            CancelStagedPreview(false);
        }

        private void OnInspectorUpdate()
        {
            if (stagedPreview != null)
                SceneView.RepaintAll();
        }

        private void BuildStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.95f, 0.80f, 0.32f) }
            };

            subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.72f, 0.70f, 0.82f) }
            };

            cardTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.90f, 0.88f, 0.96f) }
            };

            badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 3, 3)
            };

            wrappedLabel = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true
            };
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawHeader();
            DrawProjectFields();
            DrawTabs();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case StudioTab.Setup:
                    DrawSetup();
                    break;
                case StudioTab.OutgoingContacts:
                    DrawOutgoingContacts();
                    break;
                case StudioTab.IncomingReceivers:
                    DrawIncomingReceivers();
                    break;
                case StudioTab.AnimatorSetup:
                    DrawAnimatorSetup();
                    break;
                case StudioTab.Help:
                    DrawHelp();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var headerRect = EditorGUILayout.GetControlRect(false, 76f);
            EditorGUI.DrawRect(headerRect, new Color(0.075f, 0.045f, 0.12f));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 2f, headerRect.width, 2f), new Color(0.62f, 0.35f, 0.88f));

            var titleRect = new Rect(headerRect.x + 18f, headerRect.y + 12f, headerRect.width - 150f, 28f);
            var subRect = new Rect(headerRect.x + 18f, headerRect.y + 42f, headerRect.width - 36f, 20f);
            GUI.Label(titleRect, "Stories Of Yggdrasil — OSC Contact System", titleStyle);
            GUI.Label(subRect, "Guided contact previews • Safe FX copies • OSC bridge setup", subtitleStyle);

            var versionRect = new Rect(headerRect.xMax - 112f, headerRect.y + 18f, 88f, 24f);
            EditorGUI.DrawRect(versionRect, new Color(0.18f, 0.10f, 0.28f));
            GUI.Label(versionRect, "v" + Version, badgeStyle);
            if (updateAvailable)
            {
                var updateRect = new Rect(headerRect.xMax - 222f, headerRect.y + 18f, 102f, 24f);
                EditorGUI.DrawRect(updateRect, new Color(0.18f, 0.42f, 0.20f));
                if (GUI.Button(updateRect, "Update Ready", badgeStyle))
                    PromptForUpdate();
            }
            EditorGUILayout.Space(8f);
        }

        private void DrawProjectFields()
        {
            BeginCard("Avatar Setup");
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
            operationLog.Insert(0, "Loaded FX, menu, Expression Parameters, and avatar root from the selected Avatar Descriptor.");
        }

        private void DrawTabs()
        {
            EditorGUILayout.Space(4f);
            tab = (StudioTab)GUILayout.Toolbar((int)tab, new[]
            {
                "Setup", "Outgoing Contacts", "Incoming Contacts", "Animator", "Help"
            }, GUILayout.Height(30f));
            EditorGUILayout.Space(6f);
        }

        private void DrawSetup()
        {
            BeginCard("Quick Setup");
            EditorGUILayout.LabelField(
                "This prepares a safe copy of the avatar's FX controller, assigns it to the avatar, " +
                "and installs the missing Stories Of Yggdrasil OSC hooks.",
                wrappedLabel);

            using (new EditorGUI.DisabledScope(avatarDescriptor == null))
            {
                if (GUILayout.Button("PREPARE AVATAR FOR STORIES OSC", GUILayout.Height(42f)))
                {
                    LoadFromAvatarDescriptor();
                    if (EnsureSafeFxCopy(true))
                        InstallAllBridgeHooks();
                }
            }
            EditorGUILayout.HelpBox(
                "The original FX controller is preserved. Animator edits are made only to the assigned copy in " + FxCopyRoot + ".",
                MessageType.Info);
            EndCard();

            DrawHealthSafetyCard();

            BeginCard("Readiness");
            DrawTagRow("Avatar", avatarDescriptor != null ? "Ready" : "Missing", "Assign the VRC Avatar Descriptor above");
            DrawTagRow("FX Copy", IsSafeFxCopy(fxController) ? "Ready" : "Pending", "Created automatically before Animator changes");
            DrawTagRow("Contacts", ContactTypesAvailable() ? "SDK Ready" : "SDK Missing", "VRC Contact Sender and Receiver types");
            DrawTagRow("Target", HasUsableTargets() ? "Selected" : "Not Selected", "Select a weapon, shield, body object, or effect transform");
            EndCard();

            BeginCard("Current Runtime Features");
            DrawTagRow("Combat Menu", "Stories RP submenu", "Combat and Enemy toggles, spell pages, Mist and Curse gauges");
            DrawTagRow(
                "Spells",
                SpellDefinitions.Select(spell => spell.Id).Distinct().Count() + " IDs / " +
                Enum.GetValues(typeof(SpellSchool)).Length + " schools",
                "All schools use the compact v0.5.3 eight-bit spell contact bus without changing registry IDs");
            DrawTagRow("I-Frames", "1.0 second", "Generated incoming damage receiver cooldown");
            DrawTagRow("Updater", updateStatus, "GitHub Releases: " + GitHubRepository);
            EndCard();

            BeginCard("Next Steps");
            EditorGUILayout.LabelField("1. Prepare the avatar above.", wrappedLabel);
            EditorGUILayout.LabelField("2. Open Outgoing Contacts and create a temporary preview on a weapon, shield, or effect.", wrappedLabel);
            EditorGUILayout.LabelField("3. Move, rotate, and resize the preview in the Scene view before finalizing it.", wrappedLabel);
            EditorGUILayout.LabelField("4. Add Incoming Contacts to the avatar body only when the avatar does not already provide the needed receivers.", wrappedLabel);
            EditorGUILayout.LabelField("5. Use the generated Stories RP submenu for Combat, Enemy Mode, gauges, and spell selection.", wrappedLabel);
            EditorGUILayout.LabelField("6. Build & Test, enable OSC in VRChat, and connect the desktop program to Sam.py.", wrappedLabel);
            EndCard();
        }

        private void DrawOutgoingContacts()
        {
            outgoingKind = (OutgoingContactKind)GUILayout.Toolbar((int)outgoingKind, new[]
            {
                "Attack", "Spells", "Blocking", "Debuff"
            }, GUILayout.Height(28f));
            EditorGUILayout.Space(4f);

            switch (outgoingKind)
            {
                case OutgoingContactKind.Attack:
                    DrawAttackSenders();
                    break;
                case OutgoingContactKind.Spell:
                    DrawSpellSenders();
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
            BeginCard("Attack Contact Profile");
            attackTier = (AttackTier)EditorGUILayout.EnumPopup("Attack Tier", attackTier);

            var baseTags = GetAttackTags(attackTier).ToList();
            var tagText = string.Join(", ", baseTags);
            EditorGUILayout.HelpBox(
                attackTier == AttackTier.Critical
                    ? "Critical uses only '" + TagCritical + "'. It deliberately does NOT receive '" + TagBlockable + "'."
                    : "This sender will use: " + tagText,
                attackTier == AttackTier.Critical ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Optional debuffs on the same hit", cardTitleStyle);
            EditorGUILayout.BeginHorizontal();
            addBurnToAttack = GUILayout.Toggle(addBurnToAttack, "Burn", "Button");
            addSilenceToAttack = GUILayout.Toggle(addSilenceToAttack, "Silence", "Button");
            addFreezeToAttack = GUILayout.Toggle(addFreezeToAttack, "Freeze", "Button");
            addBindToAttack = GUILayout.Toggle(addBindToAttack, "Bind", "Button");
            addBleedToAttack = GUILayout.Toggle(addBleedToAttack, "Bleed", "Button");
            EditorGUILayout.EndHorizontal();

            DrawShapeEditor(ref attackShape, ref attackRadius, ref attackHeight, ref attackBoxSize, ref attackPosition, ref attackRotation);
            attackCreateChild = EditorGUILayout.ToggleLeft("Create a dedicated child contact object", attackCreateChild);
            attackStartsEnabled = EditorGUILayout.ToggleLeft("Start contact object enabled", attackStartsEnabled);
            if (attackStartsEnabled)
                EditorGUILayout.HelpBox("An always-enabled weapon sender can damage someone from passive touching. Animation-gated hit frames are safer.", MessageType.Warning);

            DrawDraftControls(
                ContactDraftKind.Attack,
                HasUsableTargets() && ContactTypesAvailable(),
                "Preview Attack Contact");
            EndCard();

            BeginCard("Bullet / Projectile Guidance");
            EditorGUILayout.LabelField(
                "For a real moving bullet object, select that projectile transform and create a Weak sender on it. " +
                "For hitscan-style guns, use a narrow Box contact aligned to local +Z and animate it ON for a single firing frame. " +
                "This is an approximation: avatar Contacts do not provide true networked ballistic simulation.",
                wrappedLabel);
            if (GUILayout.Button("Load Weak Hitscan Box Preset"))
            {
                attackTier = AttackTier.Weak;
                attackShape = ContactShape.Box;
                attackBoxSize = new Vector3(0.035f, 0.035f, 8f);
                attackPosition = new Vector3(0f, 0f, 4f);
                attackRotation = Vector3.zero;
                attackStartsEnabled = false;
                operationLog.Insert(0, "Loaded Weak hitscan box preset (8 m along local +Z)." );
            }
            EndCard();
        }

        private void DrawSpellSenders()
        {
            DrawHealthSafetyMini();
            BeginCard("Spell Contact Sender");
            spellSchool = (SpellSchool)EditorGUILayout.EnumPopup("Magick School", spellSchool);
            var spells = GetSpellsForSchool(spellSchool);
            if (spells.Length == 0)
            {
                EditorGUILayout.HelpBox("No spells are registered for this school.", MessageType.Warning);
                EndCard();
                return;
            }

            spellSelectionIndex = Mathf.Clamp(spellSelectionIndex, 0, spells.Length - 1);
            spellSelectionIndex = EditorGUILayout.Popup(
                "Spell",
                spellSelectionIndex,
                spells.Select(spell => spell.Id + " — " + spell.Name).ToArray());
            var selected = spells[spellSelectionIndex];
            EditorGUILayout.HelpBox(
                "Spell ID " + selected.Id + " is encoded as " + GetSpellBinary(selected.Id) + " on the v0.5.3 contact bus. " +
                "School: " + GetSpellSchoolDisplayName(selected.School) + ". Category: " + selected.Category + ". " +
                (selected.IsHealing
                    ? "Healing and revival spells include caster-alignment tags for the Enemy healing rule."
                    : "The generated sender also includes a category tag for future Sam.py routing."),
                selected.IsHealing ? MessageType.Info : MessageType.None);

            DrawShapeEditor(ref spellShape, ref spellRadius, ref spellHeight, ref spellBoxSize, ref spellPosition, ref spellRotation);
            spellStartsEnabled = EditorGUILayout.ToggleLeft("Start spell contact enabled", spellStartsEnabled);
            if (spellStartsEnabled)
                EditorGUILayout.HelpBox("Spell contacts should normally be animation-gated to the active cast or impact frames.", MessageType.Warning);

            DrawDraftControls(
                ContactDraftKind.Spell,
                HasUsableTargets() && ContactTypesAvailable(),
                "Preview Spell Contact");
            EndCard();

            BeginCard("Enemy / Ally Healing Alignment");
            EditorGUILayout.LabelField(
                "Every generated spell sender receives an Ally and Enemy alignment variant. The FX copy activates the correct variant from SoY_IsEnemy. " +
                "When the target is marked as an Enemy, Sam.py/OSC can reject healing contacts whose source is not also marked as an Enemy.",
                wrappedLabel);
            DrawTagRow("Ally caster", CasterAllyTag, "Used while SoY_IsEnemy is false");
            DrawTagRow("Enemy caster", CasterEnemyTag, "Used while SoY_IsEnemy is true");
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
            BeginCard("Debuff Contact Sender");
            EditorGUILayout.LabelField("Select one or more exact compatible tags:", wrappedLabel);
            EditorGUILayout.BeginHorizontal();
            debuffBurn = GUILayout.Toggle(debuffBurn, "Burn", "Button");
            debuffSilence = GUILayout.Toggle(debuffSilence, "Silence", "Button");
            debuffFreeze = GUILayout.Toggle(debuffFreeze, "Freeze", "Button");
            debuffBind = GUILayout.Toggle(debuffBind, "Bind", "Button");
            debuffBleed = GUILayout.Toggle(debuffBleed, "Bleed", "Button");
            EditorGUILayout.EndHorizontal();

            DrawShapeEditor(ref debuffShape, ref debuffRadius, ref debuffHeight, ref debuffBoxSize, ref debuffPosition, ref debuffRotation);
            debuffCreateChild = EditorGUILayout.ToggleLeft("Create a dedicated child debuff-contact object", debuffCreateChild);
            debuffStartsEnabled = EditorGUILayout.ToggleLeft("Start contact object enabled", debuffStartsEnabled);
            if (debuffStartsEnabled)
                EditorGUILayout.HelpBox("DOT/control senders should normally be enabled only while the effect is active.", MessageType.Warning);

            DrawDraftControls(
                ContactDraftKind.Debuff,
                HasUsableTargets() && ContactTypesAvailable() && GetSelectedDebuffs().Any(),
                "Preview Debuff Contact");
            EndCard();

            BeginCard("Expected Health-System Mappings");
            DrawTagRow("Burn", "DoT Burn", "Slow damage-over-time tick");
            DrawTagRow("Bleed", "DoT Bleed", "Slow damage-over-time tick");
            DrawTagRow("Silence", "Suppress Silence", "Disables Magick/abilities through the existing controller");
            DrawTagRow("Freeze", "Slow Freeze", "Disables or suppresses movement through the existing controller");
            DrawTagRow("Bind", "Slow Bind", "Disables or suppresses movement through the existing controller");
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
                "v0.5.3 uses one Active receiver plus eight bit receivers for every spell ID from 1-255. " +
                "This replaces the broken SDK behavior where every Constant Int receiver reported 1. " +
                "All Magick schools are supported automatically with only nine unsynced Bool parameters.",
                MessageType.Info);
            EditorGUILayout.LabelField("Receiver components", incomingSpells ? "9 spell bus + 1 caster alignment" : "Disabled");
            EditorGUILayout.LabelField("Required Desktop", "Stories Of Yggdrasil OSC v0.8.1 or newer");
            using (new EditorGUI.DisabledScope(avatarRoot == null || !ContactTypesAvailable()))
            {
                if (GUILayout.Button("REPAIR v0.5.1 / v0.5.2 SPELL CONTACTS", GUILayout.Height(34f)))
                    RepairSpellContactBus();
            }
            DrawShapeEditor(ref incomingShape, ref incomingRadius, ref incomingHeight, ref incomingBoxSize, ref incomingPosition, ref incomingRotation);
            incomingCreateChild = true;
            EditorGUILayout.HelpBox(
                "Incoming damage and spell receivers are created on separate child objects. The damage child is temporarily disabled for one second after a hit, while healing and spell receivers remain available.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            var locked = compatible && !forceIncomingOnExistingHealth;
            DrawDraftControls(
                ContactDraftKind.Incoming,
                HasUsableTargets() && ContactTypesAvailable() && !locked && (incomingHits || incomingDebuffs || incomingSpells),
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
            DrawTagRow("Caster alignment", "SoY_HealingSourceEnemy", "Enemy-tagged spell source for healing validation");
            DrawTagRow("I-Frames", "1.0 second", "Incoming damage receiver child is disabled after each accepted hit");
            EndCard();
        }

        private void DrawAnimatorSetup()
        {
            DrawHealthSafetyCard();

            BeginCard("Safe FX Copy");
            if (IsSafeFxCopy(fxController))
            {
                fxCopyPath = AssetDatabase.GetAssetPath(fxController);
                EditorGUILayout.HelpBox(
                    "Animator changes are being applied to:\n" + fxCopyPath,
                    MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select FX Copy"))
                {
                    Selection.activeObject = fxController;
                    EditorGUIUtility.PingObject(fxController);
                }
                if (GUILayout.Button("Open FX Folder"))
                {
                    var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FxCopyRoot);
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "The original FX will not be edited. Pressing an Animator install button creates and assigns a copy under " + FxCopyRoot + ".",
                    MessageType.Info);
                using (new EditorGUI.DisabledScope(avatarDescriptor == null || fxController == null))
                {
                    if (GUILayout.Button("Create and Assign Safe FX Copy", GUILayout.Height(30f)))
                        EnsureSafeFxCopy(true);
                }
            }
            EndCard();

            BeginCard("Stories Of Yggdrasil OSC Bridge Hooks");
            EditorGUILayout.HelpBox(
                "This installer adds only missing SoY_ bridge parameters and empty routing layers. " +
                "It never creates, replaces, renames, or rewrites Health/HP parameters, damage tiers, DOT logic, resistance logic, or existing health layers.",
                MessageType.Info);

            var missingAnimator = fxController == null
                ? BridgeParameters.Length
                : BridgeParameters.Count(spec => !fxController.parameters.Any(p => p.name == spec.Name));
            var missingLayers = fxController == null
                ? 4
                : new[] { CombatLayer, VitalLayer, ReactionLayer, DiablosLayer }.Count(name => !fxController.layers.Any(layer => layer.name == name));
            var missingExpression = expressionParameters == null
                ? BridgeParameters.Length
                : BridgeParameters.Count(spec => expressionParameters.parameters == null || !expressionParameters.parameters.Any(p => p != null && p.name == spec.Name));
            var compatibleAvailable = CountCompatibleAnimatorParameters(fxController);
            var missingCompatibleExpression = expressionParameters == null || fxController == null
                ? compatibleAvailable
                : CompatibleOscParameters.Count(spec =>
                    fxController.parameters.Any(p => p.name == spec.Name && p.type == spec.AnimatorType) &&
                    (expressionParameters.parameters == null || !expressionParameters.parameters.Any(p => p != null && p.name == spec.Name)));

            DrawTagRow("Animator Parameters", missingAnimator + " missing", "SoY_ contact events, combat opt-in, HP telemetry, reactions, and KO state");
            DrawTagRow("Hook Layers", missingLayers + " missing", "Combat, Vital, Reaction, and Curse warning states; contact-specific layers are rebuilt as needed");
            DrawTagRow("Expression Parameters", missingExpression + " missing", "Required for the standalone OSC program to read/write custom avatar parameters");
            DrawTagRow("Existing Avatar OSC", missingCompatibleExpression + " missing", compatibleAvailable + " existing compatible parameters can be exposed locally to OSC without editing their Animator logic");

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(fxController == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Missing Animator Parameters", GUILayout.Height(32f)))
                {
                    if (EnsureSafeFxCopy(true))
                    {
                        var added = AddMissingAnimatorParameters(fxController);
                        FinishAnimatorAssetChange("Animator parameters", added);
                    }
                }
                if (GUILayout.Button("Add Missing Hook Layers", GUILayout.Height(32f)))
                {
                    if (EnsureSafeFxCopy(true))
                    {
                        var added = EnsureHookLayers(fxController);
                        FinishAnimatorAssetChange("Animator hook layers", added);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(expressionParameters == null))
            {
                if (GUILayout.Button("Add Missing SoY Expression Parameters", GUILayout.Height(30f)))
                {
                    Undo.RecordObject(expressionParameters, "Add Stories Of Yggdrasil Expression Parameters");
                    var added = AddMissingExpressionParameters(expressionParameters);
                    EditorUtility.SetDirty(expressionParameters);
                    SaveAndLog("SoY Expression parameters", added);
                }
            }

            using (new EditorGUI.DisabledScope(expressionParameters == null || fxController == null || compatibleAvailable == 0))
            {
                if (GUILayout.Button("Register Existing Compatible Avatar Parameters for Local OSC", GUILayout.Height(32f)))
                {
                    Undo.RecordObject(expressionParameters, "Register Existing Avatar OSC Parameters");
                    var added = AddMissingCompatibleExpressionParameters(expressionParameters, fxController);
                    EditorUtility.SetDirty(expressionParameters);
                    SaveAndLog("existing avatar local OSC parameters", added);
                }
            }

            using (new EditorGUI.DisabledScope(expressionsMenu == null))
            {
                if (GUILayout.Button("Add Stories Combat Sub-Menu", GUILayout.Height(30f)))
                {
                    Undo.RecordObject(expressionsMenu, "Add Stories Of Yggdrasil Combat Sub-Menu");
                    var added = AddCombatToggle(expressionsMenu);
                    EditorUtility.SetDirty(expressionsMenu);
                    SaveAndLog("Stories combat sub-menu", added);
                }
            }

            EditorGUILayout.Space(5f);
            using (new EditorGUI.DisabledScope(fxController == null || avatarRoot == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rebuild 1s I-Frame Layer", GUILayout.Height(30f)))
                    RebuildIFrameLayer();
                if (GUILayout.Button("Rebuild Spell Alignment", GUILayout.Height(30f)))
                    RebuildSpellAlignmentLayer();
                EditorGUILayout.EndHorizontal();
            }
            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("INSTALL / REPAIR ALL MISSING OSC HOOKS", GUILayout.Height(42f)))
                    InstallAllBridgeHooks();
            }
            EndCard();

            BeginCard("Layer Purpose");
            DrawTagRow("OSC Combat Gate", "SoY_CombatEnabled", "Opt-in state only; it does not disable or alter an existing health system");
            DrawTagRow("OSC Vital State", "SoY_CriticalHP / SoY_KO", "Empty Normal, Critical, and KO states ready for avatar-specific animation clips");
            DrawTagRow("OSC Reaction Router", "SoY_DamageReaction", "Empty Weak, Average, Strong, Critical, Blocked, and Healing reaction states");
            DrawTagRow("Curse Warnings", "25 / 50 / 90 / 98", "Animator warning states driven directly by the Curse gauge");
            DrawTagRow("Hit I-Frames", "1 second", "Generated damage receiver objects are disabled between accepted hits");
            DrawTagRow("Spell Alignment", "SoY_IsEnemy", "Selects ally or enemy spell-sender tags without editing the source FX");
            EditorGUILayout.HelpBox(
                "The standalone OSC application reads exact compatible avatar parameters directly when they exist, and falls back to SoY_ incoming receivers on avatars without a health system. " +
                "Existing Health logic remains authoritative and is never rewritten.",
                MessageType.None);
            EndCard();
        }

        private void DrawHelp()
        {
            BeginCard("Five-Minute Tutorial");
            EditorGUILayout.LabelField("1. Assign the avatar's VRC Avatar Descriptor and press Load From Avatar.", wrappedLabel);
            EditorGUILayout.LabelField("2. Open Setup and press PREPARE AVATAR FOR STORIES OSC. The tool creates a safe FX copy and assigns it automatically.", wrappedLabel);
            EditorGUILayout.LabelField("3. Select a weapon, shield, body object, or effect transform. Use Contact Target when selection should remain fixed.", wrappedLabel);
            EditorGUILayout.LabelField("4. Open Outgoing Contacts or Incoming Contacts and press Preview. A temporary object appears under the selected target.", wrappedLabel);
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
                "Enemy Mode is exposed through SoY_IsEnemy. Healing validation uses the target's Enemy state plus the caster-alignment tag generated on spell senders.",
                wrappedLabel);
            EndCard();

            BeginCard("Contact Types");
            DrawTagRow("Attack", "Weak / Average / Strong / Critical", "Weapon or projectile hit volumes");
            DrawTagRow("Spell", "8-bit Contact Bus → SoY_SpellType", "Twelve Magick schools with stable IDs, caster alignment, and category tags");
            DrawTagRow("Blocking", TagBlockable + " → " + TagHitBlocked, "Shield face or guarded weapon volume");
            DrawTagRow("Debuff", string.Join(", ", DebuffTags), "Spell, projectile, aura, or effect volume");
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
            EditorGUILayout.LabelField("Current Version", Version);
            EditorGUILayout.LabelField("Status", updateStatus, wrappedLabel);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(updateRequest != null || updateDownloadRequest != null))
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
                "Updates are never installed silently. The tool checks the latest published GitHub Release, asks permission, backs up the current script, then refreshes Unity.",
                MessageType.Info);
            EndCard();
        }

        private void AutoCheckForUpdates()
        {
            var sessionKey = "StoriesOSC.UnityTool.UpdateChecked." + Version;
            if (SessionState.GetBool(sessionKey, false))
                return;
            SessionState.SetBool(sessionKey, true);
            CheckForUpdates(false);
        }

        private void CheckForUpdates(bool manual)
        {
            if (updateRequest != null || updateDownloadRequest != null)
                return;
            updateCheckWasManual = manual;
            updateStatus = "Checking GitHub Releases...";
            updateRequest = UnityWebRequest.Get(GitHubLatestReleaseApi);
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

                latestRelease = JsonUtility.FromJson<GitHubReleaseInfo>(request.downloadHandler.text);
                if (latestRelease == null || string.IsNullOrWhiteSpace(latestRelease.tag_name) || latestRelease.draft || latestRelease.prerelease)
                {
                    updateStatus = "No stable release information was returned.";
                    return;
                }

                var latestVersion = latestRelease.tag_name.Trim().TrimStart('v', 'V');
                updateAvailable = CompareVersions(latestVersion, Version) > 0;
                updateStatus = updateAvailable
                    ? "Version " + latestVersion + " is available."
                    : "Up to date — latest release is " + latestVersion + ".";
                if (updateAvailable)
                    PromptForUpdate();
                else if (updateCheckWasManual)
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
            if (latestRelease == null || latestRelease.assets == null || updateDownloadRequest != null)
            {
                EditorUtility.DisplayDialog(
                    "Stories OSC Updater",
                    "This release does not contain downloadable assets. Publish the canonical .cs file or the Unity-tool ZIP as a release asset.",
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
                    "No compatible .cs or .zip release asset was found.",
                    "OK");
                return;
            }

            updateStatus = "Downloading " + asset.name + "...";
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
                var sourceBytes = ExtractUpdatedScript(request.downloadHandler.data);
                InstallUpdatedScript(sourceBytes);
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
                        ReadCollisionTags(component).Any(tag => tag.StartsWith(LegacySpellTagPrefix, StringComparison.Ordinal)));
                var spellBusReceivers = avatarRoot.GetComponentsInChildren(receiverType, true).Cast<Component>()
                    .Count(component => ReadStringMember(component, "parameter", "Parameter") == SpellActiveParameter ||
                        ReadStringMember(component, "parameter", "Parameter").StartsWith(SpellBitParameterPrefix, StringComparison.Ordinal));
                EditorGUILayout.LabelField("Legacy broken spell receivers", legacySpellReceivers.ToString());
                EditorGUILayout.LabelField("v0.5.3 spell bus receivers", spellBusReceivers + " / 9");
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(left, GUILayout.Width(72f));
            EditorGUILayout.SelectableLabel(middle, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(260f));
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
            tags = tags.Distinct().Take(16).ToList();

            foreach (var target in GetTargets())
            {
                var host = attackCreateChild
                    ? CreateContactChild(target, "Stories Attack - " + attackTier, attackStartsEnabled)
                    : target;
                var component = EnsureContact(host, FindType(SenderTypeName), tags, null);
                if (component == null)
                    continue;

                ConfigureContact(component, attackShape, attackRadius, attackHeight, attackBoxSize, attackPosition, attackRotation, tags);
                SetBoolMember(component, false, "localOnly", "LocalOnly");
                FinishContact(component);
                Selection.activeGameObject = host;
                Log("Attack sender ready on '" + host.name + "' with tags: " + string.Join(", ", tags));
            }
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
                    tag == SpellActiveTag || tag.StartsWith(LegacySpellTagPrefix, StringComparison.Ordinal)));
            if (component == null)
                component = EnsureContact(host, senderType, tags, null);
            if (component == null)
                return;
            ConfigureContact(component, spellShape, spellRadius, spellHeight, spellBoxSize, spellPosition, spellRotation, tags);
            SetBoolMember(component, false, "localOnly", "LocalOnly");
            FinishContact(component);
        }

        private void CreateDebuffSenders()
        {
            var tags = GetSelectedDebuffs().Distinct().Take(16).ToList();
            foreach (var target in GetTargets())
            {
                var host = debuffCreateChild
                    ? CreateContactChild(target, "Stories Debuff - " + string.Join(" + ", tags), debuffStartsEnabled)
                    : target;
                var component = EnsureContact(host, FindType(SenderTypeName), tags, null);
                if (component == null)
                    continue;

                ConfigureContact(component, debuffShape, debuffRadius, debuffHeight, debuffBoxSize, debuffPosition, debuffRotation, tags);
                SetBoolMember(component, false, "localOnly", "LocalOnly");
                FinishContact(component);
                Selection.activeGameObject = host;
                Log("Debuff sender ready on '" + host.name + "' with tags: " + string.Join(", ", tags));
            }
        }

        private void CreateIncomingReceivers()
        {
            var damageMappings = new List<ReceiverMapping>();
            var spellMappings = new List<ReceiverMapping>();
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

            if (incomingSpells)
            {
                spellMappings.Add(new ReceiverMapping(SpellActiveTag, SpellActiveParameter));
                for (var bit = 0; bit < SpellBitCount; bit++)
                    spellMappings.Add(new ReceiverMapping(GetSpellBitTag(bit), GetSpellBitParameter(bit)));
                spellMappings.Add(new ReceiverMapping(CasterEnemyTag, "SoY_HealingSourceEnemy"));
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

        private static void ConfigureContact(
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
            SetTransformMember(component, component.transform, "rootTransform", "RootTransform");
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
                Undo.RecordObject(expressionParameters, "Add v0.5.3 Spell Bus Parameters");
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
                        tags.Any(tag => tag.StartsWith(LegacySpellTagPrefix, StringComparison.Ordinal)))
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
            foreach (var parameter in new[] { "SoY_HitWeak", "SoY_HitAverage", "SoY_HitStrong", "SoY_HitCritical" })
                AddBoolTransition(ready, cooldown, parameter, true);
            var back = cooldown.AddTransition(ready);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0f;
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Rebuilt 1-second incoming hit I-Frame layer for " + hosts.Count + " receiver object(s).");
        }

        private void RebuildSpellAlignmentLayer()
        {
            if (avatarRoot == null || fxController == null || !EnsureSafeFxCopy(true))
            {
                Log("Spell alignment layer was not rebuilt because the avatar root or safe FX copy is missing.");
                return;
            }

            var allyHosts = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name.StartsWith("[SoY Spell Ally]", StringComparison.Ordinal))
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToList();
            var enemyHosts = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name.StartsWith("[SoY Spell Enemy]", StringComparison.Ordinal))
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToList();
            if (allyHosts.Count == 0 && enemyHosts.Count == 0)
                return;

            EnsureAssetFolder(AnimationRoot);
            var avatarName = MakeSafeAssetName(avatarRoot.name);
            var allyClip = CreateOrReplaceAlignmentClip(
                AnimationRoot + "/" + avatarName + "_SoY_Spell_Ally.anim", allyHosts, enemyHosts, false);
            var enemyClip = CreateOrReplaceAlignmentClip(
                AnimationRoot + "/" + avatarName + "_SoY_Spell_Enemy.anim", allyHosts, enemyHosts, true);

            RemoveLayerByName(fxController, SpellAlignmentLayer);
            var layer = CreateHookLayer(fxController, SpellAlignmentLayer);
            var ally = AddHookState(layer.stateMachine, "Ally Caster", new Vector3(220f, 120f));
            var enemy = AddHookState(layer.stateMachine, "Enemy Caster", new Vector3(520f, 120f));
            ally.motion = allyClip;
            enemy.motion = enemyClip;
            layer.stateMachine.defaultState = ally;
            AddBoolTransition(ally, enemy, "SoY_IsEnemy", true);
            AddBoolTransition(enemy, ally, "SoY_IsEnemy", false);
            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log("Rebuilt spell alignment layer for " + allyHosts.Count + " spell sender pair(s).");
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

                    // Repair flags from older installer versions. This is
                    // especially important for SoY_CombatEnabled, which must
                    // be saved and network-synced so the avatar menu and OSC
                    // desktop application stay bidirectional.
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
                        existing.networkSynced = spec.NetworkSynced;
                        repaired = true;
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

            var storiesMenu = LoadOrCreateMenu(mainPath);
            var statusMenu = LoadOrCreateMenu(statusPath);
            var spellsMenu = LoadOrCreateMenu(spellsPath);
            var coreSchoolsMenu = LoadOrCreateMenu(coreSchoolsPath);
            var specializedSchoolsMenu = LoadOrCreateMenu(specializedSchoolsPath);
            var forbiddenSchoolsMenu = LoadOrCreateMenu(forbiddenSchoolsPath);

            var schoolPages = Enum.GetValues(typeof(SpellSchool))
                .Cast<SpellSchool>()
                .ToDictionary(school => school, school => BuildSpellMenuPages(avatarName, school));

            Undo.RecordObject(storiesMenu, "Build Stories RP Menu");
            storiesMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                CreateToggleControl("RP Combat", "SoY_CombatEnabled"),
                CreateToggleControl("Enemy Mode", "SoY_IsEnemy"),
                CreateSubMenuControl("Spells", spellsMenu),
                CreateSubMenuControl("Status Gauges", statusMenu)
            };
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

            Undo.RecordObject(spellsMenu, "Build Stories Spell Menu");
            spellsMenu.controls = new List<VRCExpressionsMenu.Control>
            {
                CreateSubMenuControl("Core Magick", coreSchoolsMenu),
                CreateSubMenuControl("Specialized Magick", specializedSchoolsMenu),
                CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu)
            };
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
            if (spells.Length == 0)
                return null;

            var pageCount = Mathf.CeilToInt(spells.Length / 7f);
            var pages = new List<VRCExpressionsMenu>();
            var schoolLabel = GetSpellSchoolAssetLabel(school);
            for (var page = 0; page < pageCount; page++)
            {
                var path = MenuRoot + "/" + avatarName + "_" + schoolLabel + "_Page_" + (page + 1) + ".asset";
                pages.Add(LoadOrCreateMenu(path));
            }

            for (var page = 0; page < pages.Count; page++)
            {
                var targetMenu = pages[page];
                Undo.RecordObject(targetMenu, "Build Stories Spell Page");
                targetMenu.controls = new List<VRCExpressionsMenu.Control>();
                foreach (var spell in spells.Skip(page * 7).Take(7))
                {
                    targetMenu.controls.Add(new VRCExpressionsMenu.Control
                    {
                        name = spell.Name,
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter { name = "SoY_SpellType" },
                        value = spell.Id
                    });
                }
                if (page + 1 < pages.Count)
                    targetMenu.controls.Add(CreateSubMenuControl("Next Page", pages[page + 1]));
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
            if (!target.layers.Any(layer => layer.name == VitalLayer))
            {
                AddVitalLayer(target);
                added++;
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
            var normal = AddHookState(layer.stateMachine, "Normal", new Vector3(220f, 120f));
            var critical = AddHookState(layer.stateMachine, "Critical HP", new Vector3(500f, 70f));
            var ko = AddHookState(layer.stateMachine, "KO", new Vector3(500f, 220f));
            layer.stateMachine.defaultState = normal;

            AddBoolTransition(normal, critical, "SoY_CriticalHP", true);
            AddBoolTransition(normal, ko, "SoY_KO", true);
            AddBoolTransition(critical, normal, "SoY_CriticalHP", false);
            AddBoolTransition(critical, ko, "SoY_KO", true);
            AddBoolTransition(ko, critical, "SoY_KO", false, "SoY_CriticalHP", true);
            AddBoolTransition(ko, normal, "SoY_KO", false, "SoY_CriticalHP", false);
            target.AddLayer(layer);
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
