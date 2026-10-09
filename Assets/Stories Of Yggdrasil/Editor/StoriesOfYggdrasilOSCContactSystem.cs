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
using VRC.SDKBase;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.Dynamics;

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
        private const string Version = "0.5.10";
        private const string BuildNumber = "TB18";
        private const string BuildLabel = "Test Build 18 — Physical Helpful Items";
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

        // TB12 external-contact compatibility aliases.
        // These are exact, case-sensitive VRChat Contact Sender tags used by outside avatar systems.
        // They are received and normalized into the existing Stories OSC parameters so Desktop/Sam.py
        // continue to see the canonical Weak/Average/Blocked contract.
        private const string TagExternalSword = "Sword";
        private const string TagExternalWeapon = "Weapon";
        private const string TagExternalHands = "Hands";
        private const string TagExternalParryDetect = "Parry_Detect";

        private static readonly string[] IncomingWeakContactTags =
        {
            TagWeak,
            TagExternalHands
        };

        private static readonly string[] IncomingAverageContactTags =
        {
            TagAverage,
            TagExternalSword,
            TagExternalWeapon
        };

        private static readonly string[] ExternalDamageContactTags =
        {
            TagExternalSword,
            TagExternalWeapon,
            TagExternalHands
        };

        private static readonly string[] CompatibleBlockContactTags =
        {
            TagBlockable,
            TagHitBlocked,
            TagExternalParryDetect
        };

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
            MissingManagedComponent,
            UnityCompatibilityMarker
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
            Resources,
            Spells,
            Technicks,
            Items,
            Evasion,
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

        private enum HelpfulItemHand
        {
            Left,
            Right
        }

        private enum HelpfulItemGesture
        {
            Neutral = 0,
            Fist = 1,
            HandOpen = 2,
            FingerPoint = 3,
            Victory = 4,
            RockNRoll = 5,
            HandGun = 6,
            ThumbsUp = 7
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
            RemotePlayersOnly = 0,
            WorldsAndPlayers = 1,
            Worlds = 2
        }

        private enum RaycastDeliveryStyle
        {
            DirectImpact,
            WorldGroundPlacement
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
            LightMagick
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

        private enum ResourceGaugeKind
        {
            Health,
            MP,
            Mist,
            Diablos,
            Arousal
        }

        private enum ResourceVisibilityMode
        {
            Always,
            CombatOnly,
            ApplicableOnly,
            Never
        }

        private enum GaugeClipGeneratorMode
        {
            TransformScale,
            BlendShape,
            MaterialFloat
        }

        private enum GaugeScaleAxis
        {
            X,
            Y,
            Z
        }

        private const string CombatLayer = "Stories Of Yggdrasil | OSC Combat Gate";
        private const string VitalLayer = "Stories Of Yggdrasil | OSC Vital State";
        private const string MpGaugeLayer = "Stories Of Yggdrasil | MP Gauge";
        private const string MistGaugeLayer = "Stories Of Yggdrasil | Mist Gauge";
        private const string ReactionLayer = "Stories Of Yggdrasil | OSC Reaction Router";
        private const string DiablosLayer = "Stories Of Yggdrasil | Curse Of Diablos Warnings";
        private const string ArousalLayer = "Stories Of Yggdrasil | Arousal Discharge State";
        private const string IFrameLayer = "Stories Of Yggdrasil | Incoming Hit I-Frames";
        private const string SpellAlignmentLayer = "Stories Of Yggdrasil | Spell Alignment";
        private const string SpellCastLayer = "Stories Of Yggdrasil | Spell Cast Animations";
        private const string TechnickCastLayer = "Stories Of Yggdrasil | Technick Animations";
        private const string ItemUseLayer = "Stories Of Yggdrasil | Item Use Animations";
        private const string SpellContactGateLayer = "Stories Of Yggdrasil | Spell Contact Gate";
        private const string TechnickContactGateLayer = "Stories Of Yggdrasil | Technick Contact Gate";
        private const string ItemContactGateLayer = "Stories Of Yggdrasil | Item Contact Gate";
        private const string SpellApprovedParameter = "SoY_SpellApproved";
        private const string TechnickApprovedParameter = "SoY_TechnickApproved";
        private const string ItemApprovedParameter = "SoY_ItemApproved";
        private const string RaycastApprovedParameter = "SoY_RaycastApproved";
        private const float DefaultSpellRecoverySeconds = 8f;
        private const float DefaultTechnickRecoverySeconds = 15f;
        private const float DefaultItemRecoverySeconds = 5f;
        private const float DefaultContactWindowSeconds = 0.85f;
        private const float DefaultRaycastRecoverySeconds = 1f;
        private const float DefaultAttackRecoverySeconds = 5f;
        private const string RaycastLayerPrefix = "Stories Of Yggdrasil | Raycast Gate | ";
        private const string RaycastTargetLayerPrefix = "Stories Of Yggdrasil | Raycast Target | ";
        private const string RaycastFireParameter = "SoY_RaycastFire";
        private const string RaycastTargetingParameter = "SoY_RaycastTargeting";
        private const string GeneratedAssetRoot = "Assets/Stories Of Yggdrasil";
        // Legacy output folders remain recognized so existing avatars are never broken or migrated implicitly.
        private const string LegacyFxCopyRoot = "Assets/Stories Of Yggdrasil/FX";
        private const string LegacyMenuRoot = "Assets/Stories Of Yggdrasil/Menus";
        private const string LegacyAnimationRoot = "Assets/Stories Of Yggdrasil/Animations";
        private const string LegacyProfileRoot = "Assets/Stories Of Yggdrasil/Profiles";
        private const string LegacyBackupRoot = "Assets/Stories Of Yggdrasil/Backups/Unity Tool";
        private const string LegacyManifestRoot = "Assets/Stories Of Yggdrasil/Backups/Manifests";
        private const string LegacyRepairSnapshotRoot = "Assets/Stories Of Yggdrasil/Backups/Migrations";
        private const string RaycastRootName = "Stories Raycast Systems";
        private const string LegacyRaycastRootName = "Stories Raycast Results";
        private const string LegacyRaycastOriginPrefix = "[SoY Raycast Origin] ";
        private const string LegacyRaycastResultPrefix = "[SoY Raycast Result] ";
        private const string SpellPlacementRigName = "Spell Ground Placement";
        private const string SpellTargetPrefix = "SoY_SpellTarget";
        private const string SpellGroundPrefix = "SoY_SpellGround";
        private const string SpellWorldDropPrefix = "[SoY Spell World Drop] ";
        private const string TechnickPlacementRigName = "Technick Ground Placement";
        private const string TechnickTargetPrefix = "SoY_TechnickTarget";
        private const string TechnickGroundPrefix = "SoY_TechnickGround";
        private const string TechnickWorldDropPrefix = "[SoY Technick World Drop] ";
        private const float SpellGroundProbeHeight = 1.75f;
        private const float SpellGroundProbeDistance = 6f;
        private const float RaycastActionArmSeconds = 1.25f;
        private const float RaycastActionPulseSeconds = 0.85f;
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
        // Protocol 20 separates canonical Stories alignment from legacy/external aliases.
        // Sword / Weapon / Hands must never be allowed to force a friendly Stories sender
        // into Enemy/NPC attribution merely because the same body receiver sees both tags.
        private const string ExternalDamageSourceParameter = "SoY_ExternalDamageSource";
        private const string TechnickActiveTag = "SoY Technick Active";
        private const string TechnickBitTagPrefix = "SoY Technick Bit ";
        private const string TechnickActiveParameter = "SoY_TechnickActive";
        private const string TechnickBitParameterPrefix = "SoY_TechnickBit";
        private const string ItemActiveTag = "SoY Item Active";
        private const string ItemBitTagPrefix = "SoY Item Bit ";
        private const string ItemActiveParameter = "SoY_ItemActive";
        private const string ItemBitParameterPrefix = "SoY_ItemBit";
        private const string HelpfulItemActiveTag = "SoY Help Item Active";
        private const string HelpfulItemBitTagPrefix = "SoY Help Item Bit ";
        private const string HelpfulItemActiveParameter = "SoY_HelpItemActive";
        private const string HelpfulItemBitParameterPrefix = "SoY_HelpItemBit";
        private const string HelpfulItemSelfTouchParameter = "SoY_HelpItemSelfTouch";
        private const string HelpfulItemOtherTouchParameter = "SoY_HelpItemOtherTouch";
        private const string HelpfulItemUseResultParameter = "SoY_ItemUseResult";
        private const string HelpfulItemReceiveResultParameter = "SoY_ItemReceiveResult";
        private const string PvpAttemptWeakParameter = "SoY_PvPAttemptWeak";
        private const string PvpAttemptAverageParameter = "SoY_PvPAttemptAverage";
        private const string PvpAttemptStrongParameter = "SoY_PvPAttemptStrong";
        private const string PvpAttemptCriticalParameter = "SoY_PvPAttemptCritical";
        private static readonly string[] PvpRemoteBodyTags =
        {
            "Head", "Torso", "Hand", "HandL", "HandR", "Foot", "FootL", "FootR",
            "Finger", "FingerL", "FingerR"
        };
        private const string HelpfulItemLayerPrefix = "Stories Of Yggdrasil | Helpful Item ";
        private const string HelpfulItemHeadReceiverHost = "Stories Helpful Item Head Receiver";
        private const int ActionBitCount = 8;
        private const string GitHubRepository = "StarhunterUC/Stories-OSC-Unity-Tool";
        private const string GitHubLatestReleaseApi = "https://api.github.com/repos/StarhunterUC/Stories-OSC-Unity-Tool/releases/latest";
        private const string GitHubRepositoryUrl = "https://github.com/StarhunterUC/Stories-OSC-Unity-Tool";
        private const double BackgroundUpdateIntervalSeconds = 6d * 60d * 60d;
        private const float HitIFrameSeconds = 1f;

        // TB16 avatar/runtime compatibility contract. The Desktop OSC runtime reads these
        // local, unsynced avatar parameters and refuses Stories-generated gameplay input
        // from legacy/invalid schemas. OSC contract v19 is the first Unity-enforced marker.
        private const int OscProtocolVersion = 21;
        private const string UnityMarkerLayer = "Stories Of Yggdrasil | Unity Tool Marker";
        private const string UnityMarkerStateA = "SoY Marker Beacon A";
        private const string UnityMarkerStateB = "SoY Marker Beacon B";
        private const string UnityMarkerStateInvalid = "SoY Marker INVALID";
        private const string UnityToolPresentParameter = "SoY_UnityToolPresent";
        private const string UnityToolMajorParameter = "SoY_UnityToolMajor";
        private const string UnityToolMinorParameter = "SoY_UnityToolMinor";
        private const string UnityToolPatchParameter = "SoY_UnityToolPatch";
        private const string UnityToolTbParameter = "SoY_UnityToolTB";
        private const string UnityToolTbRevisionParameter = "SoY_UnityToolTBRevision";
        private const string ProtocolVersionParameter = "SoY_ProtocolVersion";
        private const string UnitySchemaValidParameter = "SoY_UnitySchemaValid";
        private const string UnityMarkerBeaconParameter = "SoY_UnityMarkerBeacon";
        private const string EvadeTypeParameter = "SoY_EvadeType";
        private const string EvadingParameter = "SoY_Evading";
        private const string EvasionLayer = "Stories Of Yggdrasil | Evasion Animations";

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
            public string[] Tags;
            public string Parameter;
            public float Value;
            public string ReceiverType;

            public IEnumerable<string> CollisionTags
            {
                get
                {
                    if (Tags != null && Tags.Length > 0)
                        return Tags;
                    return string.IsNullOrWhiteSpace(Tag)
                        ? Enumerable.Empty<string>()
                        : new[] { Tag };
                }
            }

            public string DisplayTags
            {
                get { return string.Join(" / ", CollisionTags.ToArray()); }
            }

            public ReceiverMapping(string tag, string parameter, float value = 1f, string receiverType = "Constant")
            {
                Tag = tag;
                Tags = string.IsNullOrWhiteSpace(tag) ? Array.Empty<string>() : new[] { tag };
                Parameter = parameter;
                Value = value;
                ReceiverType = receiverType;
            }

            public ReceiverMapping(IEnumerable<string> tags, string parameter, float value = 1f, string receiverType = "Constant")
            {
                Tags = (tags ?? Enumerable.Empty<string>())
                    .Where(valueTag => !string.IsNullOrWhiteSpace(valueTag))
                    .Distinct(StringComparer.Ordinal)
                    .Take(16)
                    .ToArray();
                Tag = Tags.FirstOrDefault() ?? string.Empty;
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

            // Light Bearer — Light Magick (new IDs; legacy 98/99 retired)
            new SpellDefinition(145, "Light Lance", SpellSchool.LightMagick, SpellCategory.Offensive),
            new SpellDefinition(146, "Radiant Fracture", SpellSchool.LightMagick, SpellCategory.Offensive),
            new SpellDefinition(147, "Light Imbuement", SpellSchool.LightMagick, SpellCategory.Support),
            new SpellDefinition(148, "Dawnfall", SpellSchool.LightMagick, SpellCategory.Offensive),
            new SpellDefinition(149, "Sacred Flame", SpellSchool.LightMagick, SpellCategory.Offensive),
            new SpellDefinition(150, "Radiant Horizon", SpellSchool.LightMagick, SpellCategory.Offensive),
            new SpellDefinition(151, "Light Atomic", SpellSchool.LightMagick, SpellCategory.Offensive),

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

        private static readonly HashSet<int> HelpfulPhysicalItemIds = new HashSet<int>
        {
            1, 2, 3, 4, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
            28, 30, 31, 37, 40, 43, 50
        };

        private static bool IsHelpfulPhysicalItem(int id)
        {
            return HelpfulPhysicalItemIds.Contains(id);
        }

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

        private static readonly ActionDefinition[] EvasionDefinitions =
        {
            new ActionDefinition(1, "Evade Forward", "Directional forward evade animation."),
            new ActionDefinition(2, "Evade Backward", "Directional backward evade animation."),
            new ActionDefinition(3, "Evade Left", "Directional left evade animation."),
            new ActionDefinition(4, "Evade Right", "Directional right evade animation."),
            new ActionDefinition(5, "Roll Forward", "Forward combat roll animation."),
            new ActionDefinition(6, "Roll Backward", "Backward combat roll animation."),
            new ActionDefinition(7, "Roll Left", "Left combat roll animation."),
            new ActionDefinition(8, "Roll Right", "Right combat roll animation."),
            new ActionDefinition(9, "Generic Evade", "Fallback evade animation when direction is not important."),
        };

        // Canonical bridge parameters used by Stories-managed Contacts and Resource FX. Sam.py/Desktop remain authoritative for gameplay values.
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
            // TB16 tool/protocol markers are local-only and cost no synchronized parameter memory.
            new ParameterSpec(UnityToolPresentParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(UnityToolMajorParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(UnityToolMinorParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(UnityToolPatchParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(UnityToolTbParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(UnityToolTbRevisionParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(ProtocolVersionParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(UnitySchemaValidParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(UnityMarkerBeaconParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            // Evasion selector is synced for remote animation playback; the active flag is local OSC telemetry.
            new ParameterSpec(EvadeTypeParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec(EvadingParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(RaycastFireParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, true),
            new ParameterSpec(RaycastApprovedParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(RaycastTargetingParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_SpellType", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, true),
            new ParameterSpec(SpellApprovedParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
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
            new ParameterSpec(TechnickApprovedParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
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
            new ParameterSpec(ItemApprovedParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ItemBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemSelfTouchParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemOtherTouchParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemActiveParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "0", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "1", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "2", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "3", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "4", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "5", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "6", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemBitParameterPrefix + "7", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(HelpfulItemUseResultParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(HelpfulItemReceiveResultParameter, AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec(PvpAttemptWeakParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(PvpAttemptAverageParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(PvpAttemptStrongParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(PvpAttemptCriticalParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HealingSourceEnemy", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(DamageSourceEnemyParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec(ExternalDamageSourceParameter, AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_HealingRejected", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_MistCharge", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 0f, false, false),
            new ParameterSpec("SoY_MistMax", AnimatorControllerParameterType.Int, VRCExpressionParameters.ValueType.Int, 3f, false, false),
            new ParameterSpec("SoY_MistPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 0f, false, false),
            new ParameterSpec("SoY_DiablosApplicable", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_DiablosPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 0f, false, false),
            new ParameterSpec("SoY_ArousalApplicable", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
            new ParameterSpec("SoY_ArousalPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 0f, false, false),
            new ParameterSpec("SoY_ArousalDischargeReady", AnimatorControllerParameterType.Bool, VRCExpressionParameters.ValueType.Bool, 0f, false, false),
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
            new ParameterSpec("SoY_MPPercent", AnimatorControllerParameterType.Float, VRCExpressionParameters.ValueType.Float, 1f, false, false),
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
            public float recoverySeconds = DefaultSpellRecoverySeconds;
            public float contactWindowSeconds = DefaultContactWindowSeconds;
        }

        [Serializable]
        private sealed class ActionAnimationBinding
        {
            public int id;
            public string name;
            public string clipPath;
            public bool enabled = true;
            public float recoverySeconds;
            public float contactWindowSeconds = DefaultContactWindowSeconds;

            // TB18 physical helpful-item authoring. Ignored for Technicks.
            public bool physicalHelpful;
            public string physicalPropPath;
            public HelpfulItemHand helpfulHand = HelpfulItemHand.Right;
            public HelpfulItemGesture grabGesture = HelpfulItemGesture.Fist;
            public HelpfulItemGesture useGesture = HelpfulItemGesture.HandOpen;
            public bool helpfulAllowSelf = true;
            public bool helpfulAllowOthers = true;
            public string helpfulSuccessClipPath;
            public string helpfulFailureClipPath;
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
        private sealed class HealthBlendPoint
        {
            // Legacy TB13 structure retained for one-way Resource FX migration.
            public bool enabled = true;
            public string label;
            public float threshold;
            public string clipPath;
        }

        [Serializable]
        private sealed class ResourceBlendPoint
        {
            public bool enabled = true;
            public string label;
            public float threshold;
            public string clipPath;
        }

        [Serializable]
        private sealed class ResourceGaugeProfile
        {
            public string id;
            public bool enabled = true;
            public bool networkSynced;
            public ResourceVisibilityMode visibility = ResourceVisibilityMode.Always;
            public List<ResourceBlendPoint> blendPoints = new List<ResourceBlendPoint>();
            public string specialClipPath;
        }

        [Serializable]
        private sealed class AnimationPresetBinding
        {
            public string key;
            public string clipPath;
        }

        [Serializable]
        private sealed class AvatarAnimationProfile
        {
            public string avatarName;
            public List<SpellAnimationBinding> spellAnimations = new List<SpellAnimationBinding>();
            public List<ActionAnimationBinding> technickAnimations = new List<ActionAnimationBinding>();
            public List<ActionAnimationBinding> itemAnimations = new List<ActionAnimationBinding>();
            public List<ActionAnimationBinding> evasionAnimations = new List<ActionAnimationBinding>();
            public List<MenuFavorite> favorites = new List<MenuFavorite>();
            public List<ResourceGaugeProfile> resourceGauges = new List<ResourceGaugeProfile>();

            // TB17 presentation presets. Functional Contacts/Raycasts are always generated
            // independently; these clips are optional avatar-presentation motions only.
            public string defaultSpellPresentationClipPath;
            public string defaultTechnickPresentationClipPath;
            public string defaultItemPresentationClipPath;
            public bool useGenericEvadeFallback = true;
            public List<AnimationPresetBinding> spellSchoolPresentationClips = new List<AnimationPresetBinding>();
            public List<AnimationPresetBinding> spellCategoryPresentationClips = new List<AnimationPresetBinding>();

            // Legacy TB13/TB12 fields are retained for one-way profile migration.
            public List<HealthBlendPoint> healthBlendPoints = new List<HealthBlendPoint>();
            public string healthFullClipPath;
            public string healthHalfClipPath;
            public string healthCriticalClipPath;
            public string healthKoClipPath;
        }

        [Serializable]
        private sealed class AvatarContextSnapshot
        {
            public string descriptorGlobalId;
            public string descriptorName;
            public string fxControllerGuid;
            public string expressionParametersGuid;
            public string expressionsMenuGuid;
            public string avatarRootGlobalId;
            public string contactTargetGlobalId;
            public long savedUtcTicks;
        }

        [Serializable]
        private sealed class AvatarContextStore
        {
            public string lastDescriptorGlobalId;
            public List<AvatarContextSnapshot> contexts = new List<AvatarContextSnapshot>();
        }

        private StudioTab tab;
        private ContactsPage contactsPage;
        private ToolsPage toolsPage;
        private DeliveryMode deliveryMode = DeliveryMode.Contact;
        private AnimationPage animationPage = AnimationPage.Resources;
        private WizardStep wizardStep;
        private string globalSearch = string.Empty;
        private string parameterSearch = string.Empty;
        private string menuBuilderSearch = string.Empty;
        private string backupStatus = "No backup action performed.";
        private ContactPreset contactPreset = ContactPreset.Custom;
        private RaycastCollisionTarget raycastCollisionTarget = RaycastCollisionTarget.RemotePlayersOnly;
        private RaycastDeliveryStyle raycastDeliveryStyle = RaycastDeliveryStyle.DirectImpact;
        private RaycastDeliveryStyle technickRaycastDeliveryStyle = RaycastDeliveryStyle.DirectImpact;
        private bool raycastApplyRotation;
        private bool raycastCreateLineRenderer;
        private float raycastDistance = 25f;
        private Vector3 raycastDirection = Vector3.forward;
        private string raycastParameterPrefix = "SoY_Raycast";
        private float raycastImpactRadius = 0.12f;
        private bool raycastUseCustomPrefix;
        private bool showRaycastAdvanced;
        private bool showRaycastInstructions = true;
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
        private float runtimeTestMpPercent = 1f;
        private float runtimeTestMistPercent;
        private float runtimeTestDiablosPercent;
        private float runtimeTestArousalPercent;
        private bool runtimeTestDiablosApplicable;
        private bool runtimeTestArousalApplicable;
        private bool runtimeTestArousalDischargeReady;
        private string runtimeTestSummary = "Not tested.";
        private VRCAvatarDescriptor avatarDescriptor;
        private AnimatorController fxController;
        private VRCExpressionParameters expressionParameters;
        private VRCExpressionsMenu expressionsMenu;
        private GameObject avatarRoot;
        private GameObject explicitTarget;
        private AvatarContextStore avatarContextStore = new AvatarContextStore();
        private bool avatarContextLocked;
        private string avatarContextStatus = "No saved avatar context loaded.";
        private const int MaxRecentAvatarContexts = 8;

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

        // v0.5.10 accessibility, navigation, raycast, and managed-repair preferences.
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
        private string animationEvasionSearch = string.Empty;
        private int animationEvasionSelectionIndex;
        private AnimationClip animationEvasionClip;
        private bool showSpellPresentationPresets;
        private AvatarAnimationProfile animationProfile = new AvatarAnimationProfile();
        private string animationProfileAssetPath = string.Empty;
        private int selectedResourceGaugeIndex;
        private GaugeClipGeneratorMode gaugeClipGeneratorMode = GaugeClipGeneratorMode.TransformScale;
        private GameObject gaugeGeneratorTarget;
        private GaugeScaleAxis gaugeScaleAxis = GaugeScaleAxis.X;
        private float gaugeEmptyValue;
        private float gaugeFullValue = 1f;
        private SkinnedMeshRenderer gaugeBlendShapeRenderer;
        private int gaugeBlendShapeIndex;
        private Renderer gaugeMaterialRenderer;
        private string gaugeMaterialProperty = "_FillAmount";

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
        private bool incomingLightSpells;
        private bool forceIncomingOnExistingHealth;
        private bool bridgeBlockToOsc = true;

        private Vector2 scroll;
        private readonly List<string> operationLog = new List<string>();
        private readonly List<ManagedRepairFinding> managedRepairFindings = new List<ManagedRepairFinding>();
        private string managedRepairSummary = "Run an audit to inspect Stories-managed contacts.";
        private string managedRepairPreview = "No repair preview generated.";
        private string lastRepairSnapshotPath = string.Empty;
        private string lastAnimatorIntegrityBackupPath = string.Empty;
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
            LoadAvatarContextStore();
            RestoreLastAvatarContext();
            EditorApplication.delayCall -= DelayedRestoreAvatarContext;
            EditorApplication.delayCall += DelayedRestoreAvatarContext;
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
            SaveCurrentAvatarContext();
            SaveAvatarContextStore();
            EditorApplication.delayCall -= DelayedRestoreAvatarContext;
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

            EditorGUILayout.BeginHorizontal();
            var newLock = EditorGUILayout.ToggleLeft("Lock Avatar Context", avatarContextLocked, GUILayout.Width(150f));
            if (newLock != avatarContextLocked)
            {
                avatarContextLocked = newLock;
                EditorPrefs.SetBool(AvatarContextLockPrefsKey(), avatarContextLocked);
                avatarContextStatus = avatarContextLocked
                    ? "Avatar context locked. Automatic/context-switch controls are protected."
                    : "Avatar context unlocked.";
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(avatarContextLocked ? "LOCKED" : "Editable", EditorStyles.miniBoldLabel, GUILayout.Width(62f));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            VRCAvatarDescriptor requestedDescriptor;
            using (new EditorGUI.DisabledScope(avatarContextLocked))
            {
                requestedDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                    "Avatar Descriptor", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
            }
            if (EditorGUI.EndChangeCheck() && requestedDescriptor != avatarDescriptor)
                SelectAvatarDescriptor(requestedDescriptor, true);

            EditorGUI.BeginChangeCheck();
            var requestedTarget = (GameObject)EditorGUILayout.ObjectField(
                "Contact Target", explicitTarget, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                explicitTarget = requestedTarget;
                SaveCurrentAvatarContext();
                RefreshHealthAudit();
            }

            DrawAvatarContextHealthRow();

            var recentContexts = GetRecentAvatarContexts();
            if (recentContexts.Count > 0)
            {
                var labels = new List<string> { "Recent Avatars…" };
                labels.AddRange(recentContexts.Select(context =>
                    string.IsNullOrWhiteSpace(context.descriptorName) ? "Unnamed Avatar" : context.descriptorName));
                using (new EditorGUI.DisabledScope(avatarContextLocked))
                {
                    var chosen = EditorGUILayout.Popup("Recent Avatar", 0, labels.ToArray());
                    if (chosen > 0)
                        RestoreAvatarContext(recentContexts[chosen - 1], true);
                }
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(avatarContextLocked || avatarDescriptor == null))
            {
                if (GUILayout.Button("Load From Avatar", GUILayout.Height(28f)))
                {
                    LoadFromAvatarDescriptor();
                    Repaint();
                }
            }
            using (new EditorGUI.DisabledScope(avatarContextLocked))
            {
                if (GUILayout.Button("Use Selected Avatar", GUILayout.Height(28f)))
                {
                    UseSelectedAvatarAsContext();
                    Repaint();
                }
            }
            if (GUILayout.Button("Use Selected Object as Target", GUILayout.Height(28f)))
            {
                explicitTarget = Selection.activeGameObject;
                SaveCurrentAvatarContext();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(avatarContextLocked || avatarDescriptor == null))
            {
                if (GUILayout.Button("Clear Context", GUILayout.Height(24f)))
                    ClearAvatarContext();
            }
            if (GUILayout.Button("Help", GUILayout.Width(72f), GUILayout.Height(24f)))
                tab = StudioTab.Help;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrWhiteSpace(avatarContextStatus))
                EditorGUILayout.HelpBox(avatarContextStatus, MessageType.None);

            if (avatarDescriptor == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a VRC Avatar Descriptor or use Use Selected Avatar. TB17 retains per-avatar context across recompiles and Unity restarts.",
                    MessageType.Info);
            }
            else if (fxController == null)
            {
                EditorGUILayout.HelpBox(
                    "No FX Animator Controller is currently assigned/restored for this avatar.",
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
                    GeneratedAssetRoot + "/<Avatar>/FX/<FX>_SoY_FX.controller\nand assigns that copy to the avatar.",
                    MessageType.Info);
                if (GUILayout.Button("Create and Assign Safe FX Copy", GUILayout.Height(30f)))
                {
                    EnsureSafeFxCopy(true);
                    SaveCurrentAvatarContext();
                }
            }

            advancedContextFoldout = EditorGUILayout.Foldout(
                advancedContextFoldout, "Advanced Asset Fields", true);
            if (advancedContextFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(avatarContextLocked))
                {
                    fxController = (AnimatorController)EditorGUILayout.ObjectField(
                        "Working FX Controller", fxController, typeof(AnimatorController), false);
                    expressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField(
                        "Expression Parameters", expressionParameters, typeof(VRCExpressionParameters), false);
                    expressionsMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(
                        "Expressions Menu", expressionsMenu, typeof(VRCExpressionsMenu), false);
                    avatarRoot = (GameObject)EditorGUILayout.ObjectField(
                        "Avatar Root", avatarRoot, typeof(GameObject), true);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    fxCopyPath = IsSafeFxCopy(fxController) ? AssetDatabase.GetAssetPath(fxController) : string.Empty;
                    SaveCurrentAvatarContext();
                    RefreshHealthAudit();
                }
                EditorGUI.indentLevel--;

                if (avatarDescriptor != null && expressionParameters != null &&
                    avatarDescriptor.expressionParameters != expressionParameters)
                {
                    EditorGUILayout.HelpBox(
                        "The selected Expression Parameters asset is not the one assigned to this avatar. " +
                        "Press Load From Avatar to intentionally restore the descriptor-assigned asset.",
                        MessageType.Warning);
                }
            }
            EndCard();
        }

        private static string AvatarContextProjectToken()
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath ?? string.Empty));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).Substring(0, 12);
            }
        }

        private static string AvatarContextStorePrefsKey()
        {
            return "StoriesOSC.v0510.TB15.AvatarContextStore." + AvatarContextProjectToken();
        }

        private static string AvatarContextLockPrefsKey()
        {
            return "StoriesOSC.v0510.TB15.AvatarContextLock." + AvatarContextProjectToken();
        }

        private void LoadAvatarContextStore()
        {
            avatarContextLocked = EditorPrefs.GetBool(AvatarContextLockPrefsKey(), false);
            var json = EditorPrefs.GetString(AvatarContextStorePrefsKey(), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                avatarContextStore = new AvatarContextStore();
                return;
            }

            try
            {
                avatarContextStore = JsonUtility.FromJson<AvatarContextStore>(json) ?? new AvatarContextStore();
                if (avatarContextStore.contexts == null)
                    avatarContextStore.contexts = new List<AvatarContextSnapshot>();
            }
            catch (Exception exception)
            {
                avatarContextStore = new AvatarContextStore();
                avatarContextStatus = "Saved avatar context could not be read: " + exception.Message;
            }
        }

        private void SaveAvatarContextStore()
        {
            if (avatarContextStore == null)
                avatarContextStore = new AvatarContextStore();
            if (avatarContextStore.contexts == null)
                avatarContextStore.contexts = new List<AvatarContextSnapshot>();
            try
            {
                EditorPrefs.SetString(AvatarContextStorePrefsKey(), JsonUtility.ToJson(avatarContextStore));
            }
            catch (Exception exception)
            {
                operationLog.Insert(0, "Could not save Avatar Context store: " + exception.Message);
            }
        }

        private static string GlobalIdForObject(UnityEngine.Object value)
        {
            if (value == null)
                return string.Empty;
            try
            {
                return GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static T ResolveGlobalObject<T>(string globalId) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(globalId))
                return null;
            try
            {
                GlobalObjectId parsed;
                if (!GlobalObjectId.TryParse(globalId, out parsed))
                    return null;
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsed) as T;
            }
            catch
            {
                return null;
            }
        }

        private static string AssetGuidForObject(UnityEngine.Object value)
        {
            if (value == null)
                return string.Empty;
            var path = AssetDatabase.GetAssetPath(value);
            return string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static T ResolveAssetGuid<T>(string guid) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(guid))
                return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private List<AvatarContextSnapshot> GetRecentAvatarContexts()
        {
            if (avatarContextStore == null || avatarContextStore.contexts == null)
                return new List<AvatarContextSnapshot>();
            return avatarContextStore.contexts
                .Where(context => context != null && !string.IsNullOrWhiteSpace(context.descriptorGlobalId))
                .Take(MaxRecentAvatarContexts)
                .ToList();
        }

        private void SaveCurrentAvatarContext()
        {
            if (avatarDescriptor == null)
                return;

            if (avatarContextStore == null)
                avatarContextStore = new AvatarContextStore();
            if (avatarContextStore.contexts == null)
                avatarContextStore.contexts = new List<AvatarContextSnapshot>();

            var descriptorId = GlobalIdForObject(avatarDescriptor);
            if (string.IsNullOrWhiteSpace(descriptorId))
                return;

            var snapshot = new AvatarContextSnapshot
            {
                descriptorGlobalId = descriptorId,
                descriptorName = avatarDescriptor.gameObject != null ? avatarDescriptor.gameObject.name : avatarDescriptor.name,
                fxControllerGuid = AssetGuidForObject(fxController),
                expressionParametersGuid = AssetGuidForObject(expressionParameters),
                expressionsMenuGuid = AssetGuidForObject(expressionsMenu),
                avatarRootGlobalId = GlobalIdForObject(avatarRoot),
                contactTargetGlobalId = GlobalIdForObject(explicitTarget),
                savedUtcTicks = DateTime.UtcNow.Ticks
            };

            avatarContextStore.contexts.RemoveAll(context =>
                context != null && string.Equals(context.descriptorGlobalId, descriptorId, StringComparison.Ordinal));
            avatarContextStore.contexts.Insert(0, snapshot);
            if (avatarContextStore.contexts.Count > MaxRecentAvatarContexts)
                avatarContextStore.contexts.RemoveRange(MaxRecentAvatarContexts, avatarContextStore.contexts.Count - MaxRecentAvatarContexts);
            avatarContextStore.lastDescriptorGlobalId = descriptorId;
            SaveAvatarContextStore();
        }

        private void DelayedRestoreAvatarContext()
        {
            if (avatarDescriptor == null)
                RestoreLastAvatarContext();
            Repaint();
        }

        private void RestoreLastAvatarContext()
        {
            if (avatarContextStore == null || string.IsNullOrWhiteSpace(avatarContextStore.lastDescriptorGlobalId))
                return;

            var snapshot = avatarContextStore.contexts != null
                ? avatarContextStore.contexts.FirstOrDefault(context =>
                    context != null && string.Equals(
                        context.descriptorGlobalId,
                        avatarContextStore.lastDescriptorGlobalId,
                        StringComparison.Ordinal))
                : null;
            if (snapshot == null)
                return;

            if (!RestoreAvatarContext(snapshot, false))
                avatarContextStatus = "Saved avatar context exists, but its scene/prefab is not currently loaded. The entry remains available in Recent Avatars.";
        }

        private bool RestoreAvatarContext(AvatarContextSnapshot snapshot, bool makeCurrent)
        {
            if (snapshot == null)
                return false;

            var descriptor = ResolveGlobalObject<VRCAvatarDescriptor>(snapshot.descriptorGlobalId);
            if (descriptor == null)
            {
                avatarContextStatus = "Could not restore '" + (snapshot.descriptorName ?? "avatar") + "'. Load its scene/prefab and try again.";
                return false;
            }

            if (avatarDescriptor != null && avatarDescriptor != descriptor)
                SaveCurrentAvatarContext();

            avatarDescriptor = descriptor;
            avatarRoot = ResolveGlobalObject<GameObject>(snapshot.avatarRootGlobalId) ?? descriptor.gameObject;
            explicitTarget = ResolveGlobalObject<GameObject>(snapshot.contactTargetGlobalId);

            fxController = ResolveAssetGuid<AnimatorController>(snapshot.fxControllerGuid);
            expressionParameters = ResolveAssetGuid<VRCExpressionParameters>(snapshot.expressionParametersGuid);
            expressionsMenu = ResolveAssetGuid<VRCExpressionsMenu>(snapshot.expressionsMenuGuid);

            var recovered = new List<string>();
            if (!string.IsNullOrWhiteSpace(snapshot.fxControllerGuid) && fxController == null)
            {
                fxController = GetFxControllerFromDescriptor(descriptor);
                recovered.Add("FX");
            }
            if (!string.IsNullOrWhiteSpace(snapshot.expressionParametersGuid) && expressionParameters == null)
            {
                expressionParameters = descriptor.expressionParameters;
                recovered.Add("Parameters");
            }
            if (!string.IsNullOrWhiteSpace(snapshot.expressionsMenuGuid) && expressionsMenu == null)
            {
                expressionsMenu = descriptor.expressionsMenu;
                recovered.Add("Menu");
            }

            fxCopyPath = IsSafeFxCopy(fxController) ? AssetDatabase.GetAssetPath(fxController) : string.Empty;
            if (makeCurrent && avatarContextStore != null)
                avatarContextStore.lastDescriptorGlobalId = snapshot.descriptorGlobalId;

            RefreshHealthAudit();
            LoadAnimationProfile();
            SaveCurrentAvatarContext();

            avatarContextStatus = recovered.Count == 0
                ? "Restored saved context for " + descriptor.gameObject.name + "."
                : "Restored " + descriptor.gameObject.name + "; recovered stale " + string.Join(", ", recovered) + " reference(s) from its Avatar Descriptor.";
            return true;
        }

        private AnimatorController GetFxControllerFromDescriptor(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null)
                return null;
            try
            {
                var fxLayer = descriptor.baseAnimationLayers
                    .FirstOrDefault(layer => layer.type == VRCAvatarDescriptor.AnimLayerType.FX);
                return fxLayer.animatorController as AnimatorController;
            }
            catch
            {
                return null;
            }
        }

        private void SelectAvatarDescriptor(VRCAvatarDescriptor descriptor, bool autoLoad)
        {
            if (avatarDescriptor == descriptor)
                return;

            SaveCurrentAvatarContext();
            avatarDescriptor = descriptor;
            fxController = null;
            expressionParameters = null;
            expressionsMenu = null;
            avatarRoot = descriptor != null ? descriptor.gameObject : null;
            explicitTarget = null;
            fxCopyPath = string.Empty;

            if (descriptor == null)
            {
                if (avatarContextStore != null)
                    avatarContextStore.lastDescriptorGlobalId = string.Empty;
                SaveAvatarContextStore();
                avatarContextStatus = "Avatar context cleared.";
                RefreshHealthAudit();
                return;
            }

            var descriptorId = GlobalIdForObject(descriptor);
            var snapshot = avatarContextStore != null && avatarContextStore.contexts != null
                ? avatarContextStore.contexts.FirstOrDefault(context =>
                    context != null && string.Equals(context.descriptorGlobalId, descriptorId, StringComparison.Ordinal))
                : null;

            if (snapshot != null && RestoreAvatarContext(snapshot, true))
                return;

            if (autoLoad)
                LoadFromAvatarDescriptor();
            else
                SaveCurrentAvatarContext();
        }

        private void UseSelectedAvatarAsContext()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                avatarContextStatus = "No Hierarchy object is selected.";
                return;
            }

            var descriptor = selected.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null)
                descriptor = selected.GetComponentInParent<VRCAvatarDescriptor>();
            if (descriptor == null)
                descriptor = selected.GetComponentInChildren<VRCAvatarDescriptor>(true);

            if (descriptor == null)
            {
                avatarContextStatus = "The selected object is not inside an avatar with a VRC Avatar Descriptor.";
                return;
            }

            SelectAvatarDescriptor(descriptor, true);
        }

        private void ClearAvatarContext()
        {
            SaveCurrentAvatarContext();
            avatarDescriptor = null;
            fxController = null;
            expressionParameters = null;
            expressionsMenu = null;
            avatarRoot = null;
            explicitTarget = null;
            fxCopyPath = string.Empty;
            if (avatarContextStore != null)
                avatarContextStore.lastDescriptorGlobalId = string.Empty;
            SaveAvatarContextStore();
            avatarContextStatus = "Current fields cleared. Saved per-avatar contexts remain in Recent Avatars.";
            RefreshHealthAudit();
        }

        private void DrawAvatarContextHealthRow()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            DrawContextHealthBadge("Descriptor", avatarDescriptor != null);
            DrawContextHealthBadge("FX", fxController != null);
            DrawContextHealthBadge("Params", expressionParameters != null);
            DrawContextHealthBadge("Menu", expressionsMenu != null);
            DrawContextHealthBadge("Root", avatarRoot != null);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawContextHealthBadge(string label, bool ready)
        {
            GUILayout.Label((ready ? "✓ " : "✕ ") + label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUILayout.Space(8f);
        }

        private void LoadFromAvatarDescriptor()
        {
            if (avatarDescriptor == null)
                return;

            avatarRoot = avatarDescriptor.gameObject;
            expressionParameters = avatarDescriptor.expressionParameters;
            expressionsMenu = avatarDescriptor.expressionsMenu;
            fxController = GetFxControllerFromDescriptor(avatarDescriptor);
            fxCopyPath = IsSafeFxCopy(fxController) ? AssetDatabase.GetAssetPath(fxController) : string.Empty;

            RefreshHealthAudit();
            LoadAnimationProfile();
            SaveCurrentAvatarContext();
            avatarContextStatus = "Loaded descriptor-assigned FX, menu, Expression Parameters, and avatar root for " + avatarDescriptor.gameObject.name + ".";
            operationLog.Insert(0, "Loaded FX, menu, Expression Parameters, avatar root, and the v0.5.10 animation/repair profile.");
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
                DrawSidebarButton("Resource FX", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Resources, () =>
                {
                    animationPage = AnimationPage.Resources;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Spell Automation", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Spells, () =>
                {
                    animationPage = AnimationPage.Spells;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Technick Automation", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Technicks, () =>
                {
                    animationPage = AnimationPage.Technicks;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Item Automation", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Items, () =>
                {
                    animationPage = AnimationPage.Items;
                    tab = StudioTab.AnimatorSetup;
                });
                DrawSidebarButton("Evasion Animations", tab == StudioTab.AnimatorSetup && animationPage == AnimationPage.Evasion, () =>
                {
                    animationPage = AnimationPage.Evasion;
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
            DrawProtocolCompatibilityCard();

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
                "Canonical HP Critical is living HP strictly below 15% of effective Max HP. Health visuals now use the Resource FX SoY_HPPercent Blend Tree, while SoY_CriticalHP remains a Desktop/Sam.py gameplay-status signal and cannot force the Unity Health FX layer into a fixed state.",
                wrappedLabel);
            DrawTagRow("Threshold", "HP > 0 and < 15%", "15.00% is not Critical; 0 HP is KO");
            DrawTagRow("Managed Layer", "Resource FX Framework", "HP / MP / Mist / Diablos / Arousal Blend Trees");
            DrawTagRow("Desktop Bool", "SoY_CriticalHP", "Must be set False again whenever HP returns to 15% or higher");
            if (GUILayout.Button("OPEN RESOURCE FX EDITOR", GUILayout.Height(largeControls ? 46f : 34f)))
                OpenHealthFxEditor();
            EditorGUILayout.HelpBox(
                "If Avatar Parameters shows SoY_CriticalHP = True while SoY_HPPercent is 0.15 or higher, the incorrect gameplay-status value is being sent by the Desktop OSC runtime—not generated by the Unity Blend Tree. The visual layer continues to follow SoY_HPPercent directly.",
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

            if (RaycastTypeAvailable() && avatarRoot != null)
            {
                var raycastCount = CountRaycastComponents();
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("VRCRaycast / FinalIK Shared Budget", cardTitleStyle);
                var raycastRect = EditorGUILayout.GetControlRect(false, largeControls ? 28f : 20f);
                EditorGUI.ProgressBar(raycastRect, Mathf.Clamp01(raycastCount / 80f), raycastCount + " / 80 Raycast components");
                EditorGUILayout.LabelField(
                    raycastCount < 64 ? "✓ Healthy Raycast headroom" :
                    raycastCount < 72 ? "! Caution: Raycast count is becoming crowded" :
                    "✕ Critical: close to VRChat's shared Raycast/FinalIK component limit",
                    wrappedLabel);
            }

            if (GUILayout.Button("OPEN MANAGED REPAIR CENTER", GUILayout.Height(largeControls ? 48f : 36f)))
            {
                toolsPage = ToolsPage.Repair;
                tab = StudioTab.Tools;
            }
            EditorGUILayout.HelpBox("Avatar Setup is the primary install/repair path. Managed Repair is kept separate for contact-by-contact audits and targeted recovery. Existing third-party health logic remains locked and untouched.", MessageType.Info);
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

            using (new EditorGUI.DisabledScope(fxController == null))
            {
                if (GUILayout.Button("REPAIR ANIMATOR INTEGRITY", GUILayout.Height(largeControls ? 44f : 32f)))
                    RepairAnimatorControllerIntegrity();
            }
            EditorGUILayout.HelpBox(
                "TB17.2 safety repair: removes only unreachable Animator transition subassets, validates every live layer/state transition, and refuses to save if the live controller graph is still broken. A full controller backup is created before cleanup.",
                MessageType.Info);

            managedRepairConfirmDestructive = EditorGUILayout.ToggleLeft(
                "Require confirmation before removing defunct or duplicate managed components",
                managedRepairConfirmDestructive);
            managedRepairShowHealthy = EditorGUILayout.ToggleLeft("Show healthy managed entries", managedRepairShowHealthy);
            managedRepairShowForeign = EditorGUILayout.ToggleLeft("Show foreign / untouched entries", managedRepairShowForeign);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(managedRepairSummary, wrappedLabel);
            if (!string.IsNullOrWhiteSpace(lastRepairSnapshotPath))
                EditorGUILayout.SelectableLabel("Last snapshot: " + lastRepairSnapshotPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (!string.IsNullOrWhiteSpace(lastAnimatorIntegrityBackupPath))
                EditorGUILayout.SelectableLabel("Animator backup: " + lastAnimatorIntegrityBackupPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
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

            if (fxController != null && expressionParameters != null && !HasCurrentCompatibilityMarkerStructure())
            {
                managedRepairFindings.Add(new ManagedRepairFinding
                {
                    Host = avatarRoot,
                    State = ManagedRepairState.Repairable,
                    Kind = ManagedRepairKind.UnityCompatibilityMarker,
                    Role = "Unity Tool Compatibility Marker",
                    Summary = "The avatar does not contain the current " + BuildNumber + " periodic Protocol " + OscProtocolVersion + " marker beacon, or one of its local Expression/Animator parameters is missing.",
                    Action = "Rebuild the marker parameters/layer and install the periodic compatibility beacon so Desktop can rediscover the avatar after either program restarts."
                });
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
            foreach (var finding in managedRepairFindings.Where(finding => finding.CanAutoRepair).Take(12))
                Log("Repairable — " + (finding.Role ?? finding.Kind.ToString()) + " — " +
                    GetHierarchyPath(finding.Host != null ? finding.Host.transform : null) + " — " +
                    (finding.Action ?? finding.Summary ?? string.Empty));
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

            var strictlyManaged = IsStrictlyStoriesManagedComponent(receiver);

            // Protocol 20 alignment contract: canonical Stories Enemy alignment and
            // outside Sword / Weapon / Hands compatibility MUST be separate receivers.
            // Mixing them is the legacy bug that made friendly Stories attacks look like
            // NPC attacks whenever an external alias was also present on the sender.
            if (string.Equals(parameter, DamageSourceEnemyParameter, StringComparison.Ordinal) &&
                (tags.Contains(CasterEnemyTag) || tags.Any(tag => ExternalDamageContactTags.Contains(tag))))
            {
                if (strictlyManaged)
                {
                    var externalTags = tags.Where(tag => ExternalDamageContactTags.Contains(tag)).Distinct(StringComparer.Ordinal).ToArray();
                    var unexpected = tags.Where(tag => !string.Equals(tag, CasterEnemyTag, StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).ToArray();
                    var healthy = tags.Count == 1 && tags.Contains(CasterEnemyTag);
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = healthy ? ManagedRepairState.Healthy : ManagedRepairState.Repairable,
                        Kind = healthy ? ManagedRepairKind.None : ManagedRepairKind.IncomingReceiverMapping,
                        Role = "Canonical Stories Enemy Alignment",
                        Summary = healthy
                            ? DamageSourceEnemyParameter + " listens only to '" + CasterEnemyTag + "'."
                            : DamageSourceEnemyParameter + " is mixed with legacy/external tag(s): " +
                              string.Join(", ", unexpected.Length > 0 ? unexpected : externalTags) + ". This can misclassify Friendly Player attacks as Enemy/NPC.",
                        Action = healthy
                            ? string.Empty
                            : "Restrict this receiver to '" + CasterEnemyTag + "' and create/update a separate " + ExternalDamageSourceParameter + " receiver for Sword / Weapon / Hands."
                    });
                }
                return;
            }

            if (string.Equals(parameter, ExternalDamageSourceParameter, StringComparison.Ordinal))
            {
                if (strictlyManaged)
                {
                    var missing = ExternalDamageContactTags.Except(tags, StringComparer.Ordinal).ToArray();
                    var unexpected = tags.Except(ExternalDamageContactTags, StringComparer.Ordinal).ToArray();
                    var healthy = missing.Length == 0 && unexpected.Length == 0;
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = healthy ? ManagedRepairState.Healthy : ManagedRepairState.Repairable,
                        Kind = healthy ? ManagedRepairKind.None : ManagedRepairKind.IncomingReceiverMapping,
                        Role = "External Damage Source Compatibility",
                        Summary = healthy
                            ? ExternalDamageSourceParameter + " owns Sword / Weapon / Hands without changing canonical Stories alignment."
                            : "External source receiver tags are incomplete or mixed with canonical alignment tags.",
                        Action = healthy
                            ? string.Empty
                            : "Set this receiver to exactly Sword / Weapon / Hands while preserving its geometry and transform."
                    });
                }
                return;
            }

            // TB12 block compatibility: both the legacy compatible-health Bool and the
            // Stories OSC mirror may listen to Blockable / Hit Blocked / Parry_Detect.
            if ((string.Equals(parameter, TagHitBlocked, StringComparison.Ordinal) ||
                 string.Equals(parameter, "SoY_HitBlocked", StringComparison.Ordinal)) &&
                tags.Any(tag => CompatibleBlockContactTags.Contains(tag)))
            {
                if (strictlyManaged)
                {
                    var missing = CompatibleBlockContactTags.Except(tags, StringComparer.Ordinal).ToArray();
                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = missing.Length == 0 ? ManagedRepairState.Healthy : ManagedRepairState.Repairable,
                        Kind = missing.Length == 0 ? ManagedRepairKind.None : ManagedRepairKind.IncomingReceiverMapping,
                        Role = "Compatible Block / Parry Receiver",
                        Summary = missing.Length == 0
                            ? "Blockable, Hit Blocked, and Parry_Detect are all accepted by this active guard receiver."
                            : "External block/parry aliases are missing from this managed receiver: " + string.Join(", ", missing) + ".",
                        Action = missing.Length == 0
                            ? string.Empty
                            : "Expand the existing block receiver tag list in place without changing its geometry."
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
                        Summary = "Canonical/compatible tag maps to '" + expectedParameter + "', but this receiver currently writes '" + parameter + "'.",
                        Action = "Correct the receiver parameter in place without replacing its GameObject or constraint."
                    });
                }
                else if (strictlyManaged)
                {
                    string[] requiredTags = null;
                    if (string.Equals(parameter, "SoY_HitWeak", StringComparison.Ordinal))
                        requiredTags = IncomingWeakContactTags;
                    else if (string.Equals(parameter, "SoY_HitAverage", StringComparison.Ordinal))
                        requiredTags = IncomingAverageContactTags;

                    var missing = requiredTags == null
                        ? Array.Empty<string>()
                        : requiredTags.Except(tags, StringComparer.Ordinal).ToArray();

                    managedRepairFindings.Add(new ManagedRepairFinding
                    {
                        Host = host,
                        Component = receiver,
                        State = missing.Length == 0 ? ManagedRepairState.Healthy : ManagedRepairState.Repairable,
                        Kind = missing.Length == 0 ? ManagedRepairKind.None : ManagedRepairKind.IncomingReceiverMapping,
                        Role = missing.Length == 0 ? "Incoming Receiver" : "Incoming External Alias Receiver",
                        Summary = missing.Length == 0
                            ? "Canonical tag and parameter mapping are valid."
                            : "The receiver is correctly mapped but is missing TB12 compatibility tag(s): " + string.Join(", ", missing) + ".",
                        Action = missing.Length == 0
                            ? string.Empty
                            : "Expand the existing receiver collision-tag list in place."
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
            if (set.Contains(TagWeak) || set.Contains(TagExternalHands)) { parameter = "SoY_HitWeak"; return true; }
            if (set.Contains(TagAverage) || set.Contains(TagExternalSword) || set.Contains(TagExternalWeapon)) { parameter = "SoY_HitAverage"; return true; }
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
            if (set.Contains(TagWeak) || set.Contains(TagExternalHands)) { tier = AttackTier.Weak; return true; }
            if (set.Contains(TagAverage) || set.Contains(TagExternalSword) || set.Contains(TagExternalWeapon)) { tier = AttackTier.Average; return true; }
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
            if (fxController != null)
                lastAnimatorIntegrityBackupPath = CreateAnimatorIntegrityBackup("managed_repair");

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Stories OSC Managed-System Repair");
            lastRepairUndoGroup = group;

            try
            {
                foreach (var snapshot in snapshots)
                    RegisterRepairUndo(snapshot.Host);

                if (fxController != null)
                    Undo.RecordObject(fxController, "Preserve Stories FX Controller");

                foreach (var finding in findings)
                    ApplyManagedRepairFinding(finding);

                foreach (var snapshot in snapshots)
                {
                    RestoreRepairRuntimeSnapshot(snapshot);
                    ValidateRepairRuntimeSnapshot(snapshot);
                }

                if (fxController != null)
                {
                    var preMarkerOrphans = CleanupOrphanedAnimatorTransitions(fxController);
                    if (preMarkerOrphans > 0)
                        operationLog.Insert(0, "Removed " + preMarkerOrphans + " unreachable Animator transition subasset(s) before marker publication.");

                    // Re-audit the repaired contact/parameter state before deciding whether
                    // the published schema marker is valid. The compatibility marker itself
                    // is ignored by ManagedSchemaCoreIsValid().
                    AuditManagedSystems();
                    var coreSchemaValid = ManagedSchemaCoreIsValid();
                    if (!coreSchemaValid)
                        operationLog.Insert(0, "Marker remains schema INVALID: " + ManagedSchemaCoreFailureSummary());
                    RebuildUnityToolMarkerLayer(coreSchemaValid);

                    var postMarkerOrphans = CleanupOrphanedAnimatorTransitions(fxController);
                    if (postMarkerOrphans > 0)
                        operationLog.Insert(0, "Removed " + postMarkerOrphans + " unreachable Animator transition subasset(s) after marker publication.");

                    var integrity = InspectAnimatorControllerIntegrity(fxController);
                    if (!integrity.IsValid)
                        throw new InvalidOperationException("Animator integrity validation failed before save: " + integrity.Summary);
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(avatarRoot.transform);
                AssetDatabase.SaveAssets();

                // Disk write occurs only after graph validation. If SaveAssets itself throws,
                // the catch path restores the Undo group and writes the restored controller.
                Undo.CollapseUndoOperations(group);
                AuditManagedSystems();

                var remainingRepairs = managedRepairFindings.Where(finding => finding.CanAutoRepair).ToList();
                if (remainingRepairs.Count > 0)
                {
                    var details = string.Join(" | ", remainingRepairs.Take(8)
                        .Select(finding => (finding.Role ?? finding.Kind.ToString()) + ": " + (finding.Action ?? finding.Summary ?? "repair still required"))
                        .ToArray());
                    managedRepairPreview = "Repair transaction committed, but " + remainingRepairs.Count +
                        " safe repair(s) still remain: " + details;
                    Log("Managed-system repair left " + remainingRepairs.Count + " repairable finding(s): " + details);
                    EditorUtility.DisplayDialog(
                        "Stories OSC Repair Needs Another Look",
                        managedRepairPreview + "\n\nSnapshot: " + lastRepairSnapshotPath,
                        "OK");
                }
                else
                {
                    managedRepairPreview = "Repair committed successfully. Animator graph validation passed before save. Use Roll Back Last Repair to undo the complete transaction during this Unity session.";
                    Log("Managed-system repair completed cleanly: " + findings.Count + " action(s). Snapshot: " + lastRepairSnapshotPath);
                }
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                lastRepairUndoGroup = -1;
                try
                {
                    if (fxController != null)
                        EditorUtility.SetDirty(fxController);
                    AssetDatabase.SaveAssets();
                }
                catch (Exception saveException)
                {
                    Debug.LogException(saveException);
                }
                AuditManagedSystems();
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
                case ManagedRepairKind.UnityCompatibilityMarker:
                    var repairedMarkerParameters = RepairUnityMarkerParameterContract();
                    var restoredAnimatorParameters = fxController != null ? AddMissingAnimatorParameters(fxController) : 0;
                    var restoredExpressionParameters = expressionParameters != null ? AddMissingExpressionParameters(expressionParameters) : 0;
                    operationLog.Insert(0,
                        "Repaired Unity compatibility marker contract (" + repairedMarkerParameters +
                        " marker setting change(s), " + restoredAnimatorParameters +
                        " missing Animator parameter(s), " + restoredExpressionParameters +
                        " missing/repaired Expression parameter setting(s)). Marker publication is deferred until the complete repair transaction passes its core audit.");
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
            var tags = mapping.CollisionTags.Distinct(StringComparer.Ordinal).Take(16).ToArray();
            var receiver = EnsureReceiverForTagsAndParameter(host, FindType(ReceiverTypeName), tags, mapping.Parameter);
            if (receiver == null)
                throw new InvalidOperationException("Could not create repaired receiver for " + mapping.DisplayTags + ".");

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
                    tags);
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

        private void RepairIncomingReceiverMapping(Component receiver)
        {
            if (receiver == null)
                return;

            var tags = ReadCollisionTags(receiver).ToList();
            var currentParameter = ReadStringMember(receiver, "parameter", "Parameter");
            var parameter = currentParameter;

            // Protocol 20 migration: split the legacy mixed alignment receiver into
            // canonical Stories Enemy alignment plus a separate external compatibility
            // receiver. Preserve the original volume geometry on both components.
            if (string.Equals(currentParameter, DamageSourceEnemyParameter, StringComparison.Ordinal) &&
                (tags.Contains(CasterEnemyTag) || tags.Any(tag => ExternalDamageContactTags.Contains(tag))))
            {
                var geometry = CaptureContactGeometry(receiver);
                SetStringMember(receiver, DamageSourceEnemyParameter, "parameter", "Parameter");
                SetCollisionTags(receiver, new[] { CasterEnemyTag });
                SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                FinishContact(receiver);

                ConfigureIncomingReceiverPreservingGeometry(
                    receiver.gameObject,
                    new ReceiverMapping(ExternalDamageContactTags, ExternalDamageSourceParameter),
                    geometry);
                return;
            }

            if (string.Equals(currentParameter, ExternalDamageSourceParameter, StringComparison.Ordinal))
            {
                SetStringMember(receiver, ExternalDamageSourceParameter, "parameter", "Parameter");
                SetCollisionTags(receiver, ExternalDamageContactTags);
                SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                FinishContact(receiver);
                return;
            }

            var isBlockCompatibility =
                (string.Equals(currentParameter, TagHitBlocked, StringComparison.Ordinal) ||
                 string.Equals(currentParameter, "SoY_HitBlocked", StringComparison.Ordinal)) &&
                tags.Any(tag => CompatibleBlockContactTags.Contains(tag));

            if (!isBlockCompatibility)
            {
                string canonicalParameter;
                if (TryGetCanonicalIncomingParameter(tags, out canonicalParameter))
                    parameter = canonicalParameter;
            }

            SetStringMember(receiver, parameter, "parameter", "Parameter");

            if (string.Equals(parameter, "SoY_HitWeak", StringComparison.Ordinal))
                SetCollisionTags(receiver, IncomingWeakContactTags);
            else if (string.Equals(parameter, "SoY_HitAverage", StringComparison.Ordinal))
                SetCollisionTags(receiver, IncomingAverageContactTags);
            else if ((string.Equals(parameter, TagHitBlocked, StringComparison.Ordinal) ||
                      string.Equals(parameter, "SoY_HitBlocked", StringComparison.Ordinal)) &&
                     tags.Any(tag => CompatibleBlockContactTags.Contains(tag)))
                SetCollisionTags(receiver, CompatibleBlockContactTags);

            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            FinishContact(receiver);
        }

        private string WriteRepairTransactionManifest(List<ManagedRepairFinding> findings, List<RepairRuntimeSnapshot> snapshots)
        {
            EnsureAssetFolder(CurrentAvatarGeneratedFolder("Backups/Migrations"));
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
            var path = AvatarGeneratedFolder(manifest.avatar, "Backups/Migrations") + "/Migration_v" + Version + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
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
            if (fxController != null)
                RebuildUnityToolMarkerLayer(ManagedSchemaCoreIsValid());
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
            technickRaycastDeliveryStyle = (RaycastDeliveryStyle)EditorPrefs.GetInt("StoriesOSC.v0510.TB9.TechnickRaycastMode", (int)RaycastDeliveryStyle.DirectImpact);
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
            EditorPrefs.SetInt("StoriesOSC.v0510.TB9.TechnickRaycastMode", (int)technickRaycastDeliveryStyle);
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
            var folder = CurrentAvatarGeneratedFolder("Profiles");
            EnsureAssetFolder(folder);
            return folder + "/StoriesOSC_AnimationProfile.json";
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
                evasionAnimations = new List<ActionAnimationBinding>(),
                favorites = new List<MenuFavorite>(),
                resourceGauges = new List<ResourceGaugeProfile>(),
                spellSchoolPresentationClips = new List<AnimationPresetBinding>(),
                spellCategoryPresentationClips = new List<AnimationPresetBinding>(),
                healthBlendPoints = new List<HealthBlendPoint>()
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
                if (animationProfile.evasionAnimations == null)
                    animationProfile.evasionAnimations = new List<ActionAnimationBinding>();
                if (animationProfile.spellSchoolPresentationClips == null)
                    animationProfile.spellSchoolPresentationClips = new List<AnimationPresetBinding>();
                if (animationProfile.spellCategoryPresentationClips == null)
                    animationProfile.spellCategoryPresentationClips = new List<AnimationPresetBinding>();
                foreach (var binding in animationProfile.spellAnimations)
                {
                    if (binding == null) continue;
                    if (binding.recoverySeconds <= 0f) binding.recoverySeconds = DefaultSpellRecoverySeconds;
                    if (binding.contactWindowSeconds <= 0f) binding.contactWindowSeconds = DefaultContactWindowSeconds;
                }
                foreach (var binding in animationProfile.technickAnimations)
                {
                    if (binding == null) continue;
                    if (binding.recoverySeconds <= 0f) binding.recoverySeconds = DefaultTechnickRecoverySeconds;
                    if (binding.contactWindowSeconds <= 0f) binding.contactWindowSeconds = DefaultContactWindowSeconds;
                }
                foreach (var binding in animationProfile.itemAnimations)
                {
                    if (binding == null) continue;
                    if (binding.recoverySeconds <= 0f) binding.recoverySeconds = DefaultItemRecoverySeconds;
                    if (binding.contactWindowSeconds <= 0f) binding.contactWindowSeconds = DefaultContactWindowSeconds;
                }
                if (animationProfile.favorites == null)
                    animationProfile.favorites = new List<MenuFavorite>();
                EnsureHealthBlendProfileDefaults();
            }
            catch (Exception exception)
            {
                Log("Could not load animation profile: " + exception.Message);
            }
        }

        private static IEnumerable<ResourceGaugeKind> AllResourceGaugeKinds()
        {
            return Enum.GetValues(typeof(ResourceGaugeKind)).Cast<ResourceGaugeKind>();
        }

        private static string ResourceId(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return "health";
                case ResourceGaugeKind.MP: return "mp";
                case ResourceGaugeKind.Mist: return "mist";
                case ResourceGaugeKind.Diablos: return "diablos";
                case ResourceGaugeKind.Arousal: return "arousal";
                default: return kind.ToString().ToLowerInvariant();
            }
        }

        private static string ResourceDisplayName(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return "Health";
                case ResourceGaugeKind.MP: return "MP";
                case ResourceGaugeKind.Mist: return "Mist";
                case ResourceGaugeKind.Diablos: return "Curse of Diablos";
                case ResourceGaugeKind.Arousal: return "Arousal";
                default: return kind.ToString();
            }
        }

        private static string ResourceParameter(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return "SoY_HPPercent";
                case ResourceGaugeKind.MP: return "SoY_MPPercent";
                case ResourceGaugeKind.Mist: return "SoY_MistPercent";
                case ResourceGaugeKind.Diablos: return "SoY_DiablosPercent";
                case ResourceGaugeKind.Arousal: return "SoY_ArousalPercent";
                default: return string.Empty;
            }
        }

        private static string ResourceLayerName(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return VitalLayer;
                case ResourceGaugeKind.MP: return MpGaugeLayer;
                case ResourceGaugeKind.Mist: return MistGaugeLayer;
                case ResourceGaugeKind.Diablos: return DiablosLayer;
                case ResourceGaugeKind.Arousal: return ArousalLayer;
                default: return "Stories Of Yggdrasil | Resource " + kind;
            }
        }

        private static string ResourceApplicabilityParameter(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Diablos: return "SoY_DiablosApplicable";
                case ResourceGaugeKind.Arousal: return "SoY_ArousalApplicable";
                default: return string.Empty;
            }
        }

        private static string ResourceSpecialBoolParameter(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return "SoY_KO";
                case ResourceGaugeKind.Arousal: return "SoY_ArousalDischargeReady";
                default: return string.Empty;
            }
        }

        private static string ResourceSpecialStateName(ResourceGaugeKind kind)
        {
            switch (kind)
            {
                case ResourceGaugeKind.Health: return "KO";
                case ResourceGaugeKind.Arousal: return "Discharge Ready";
                default: return string.Empty;
            }
        }

        private static bool ResourceDefaultNetworkSync(ResourceGaugeKind kind)
        {
            return kind == ResourceGaugeKind.Health;
        }

        private static ResourceVisibilityMode ResourceDefaultVisibility(ResourceGaugeKind kind)
        {
            if (kind == ResourceGaugeKind.Diablos || kind == ResourceGaugeKind.Arousal)
                return ResourceVisibilityMode.ApplicableOnly;
            return ResourceVisibilityMode.Always;
        }

        private static List<ResourceBlendPoint> DefaultResourceBlendPoints(ResourceGaugeKind kind)
        {
            var points = new List<ResourceBlendPoint>();
            switch (kind)
            {
                case ResourceGaugeKind.Health:
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Critical", threshold = 0.149f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Half", threshold = 0.50f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Full", threshold = 1.00f });
                    break;
                case ResourceGaugeKind.Diablos:
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Clear", threshold = 0f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Warning 25%", threshold = 0.25f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Warning 50%", threshold = 0.50f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Warning 80%", threshold = 0.80f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Warning 98%", threshold = 0.98f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Maximum", threshold = 1.00f });
                    break;
                default:
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Empty", threshold = 0f });
                    points.Add(new ResourceBlendPoint { enabled = true, label = "Full", threshold = 1f });
                    break;
            }
            return points;
        }

        private ResourceGaugeProfile FindResourceGaugeProfile(ResourceGaugeKind kind)
        {
            if (animationProfile == null || animationProfile.resourceGauges == null)
                return null;
            var id = ResourceId(kind);
            return animationProfile.resourceGauges.FirstOrDefault(x => x != null && string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
        }

        private ResourceGaugeProfile GetResourceGaugeProfile(ResourceGaugeKind kind)
        {
            EnsureResourceGaugeProfileDefaults();
            return FindResourceGaugeProfile(kind);
        }

        private void EnsureResourceGaugeProfileDefaults()
        {
            if (animationProfile == null)
                animationProfile = new AvatarAnimationProfile();
            if (animationProfile.resourceGauges == null)
                animationProfile.resourceGauges = new List<ResourceGaugeProfile>();
            if (animationProfile.healthBlendPoints == null)
                animationProfile.healthBlendPoints = new List<HealthBlendPoint>();

            foreach (var kind in AllResourceGaugeKinds())
            {
                var config = FindResourceGaugeProfile(kind);
                if (config == null)
                {
                    config = new ResourceGaugeProfile
                    {
                        id = ResourceId(kind),
                        enabled = true,
                        networkSynced = ResourceDefaultNetworkSync(kind),
                        visibility = ResourceDefaultVisibility(kind),
                        blendPoints = DefaultResourceBlendPoints(kind)
                    };

                    // One-way TB13/TB12 Health FX migration.
                    if (kind == ResourceGaugeKind.Health)
                    {
                        if (animationProfile.healthBlendPoints.Count > 0)
                        {
                            config.blendPoints = animationProfile.healthBlendPoints
                                .Where(point => point != null)
                                .Select(point => new ResourceBlendPoint
                                {
                                    enabled = point.enabled,
                                    label = point.label,
                                    threshold = point.threshold,
                                    clipPath = point.clipPath
                                }).ToList();
                        }
                        else
                        {
                            config.blendPoints = new List<ResourceBlendPoint>
                            {
                                new ResourceBlendPoint { enabled = true, label = "Critical", threshold = 0.149f, clipPath = animationProfile.healthCriticalClipPath },
                                new ResourceBlendPoint { enabled = true, label = "Half", threshold = 0.50f, clipPath = animationProfile.healthHalfClipPath },
                                new ResourceBlendPoint { enabled = true, label = "Full", threshold = 1.00f, clipPath = animationProfile.healthFullClipPath }
                            };
                        }
                        config.specialClipPath = animationProfile.healthKoClipPath;
                    }
                    animationProfile.resourceGauges.Add(config);
                }

                if (config.blendPoints == null || config.blendPoints.Count == 0)
                    config.blendPoints = DefaultResourceBlendPoints(kind);
                for (var i = 0; i < config.blendPoints.Count; i++)
                {
                    var point = config.blendPoints[i];
                    if (point == null)
                    {
                        point = new ResourceBlendPoint { enabled = true, label = "FX " + (i + 1), threshold = Mathf.Clamp01(i / Mathf.Max(1f, config.blendPoints.Count - 1f)) };
                        config.blendPoints[i] = point;
                    }
                    point.threshold = Mathf.Clamp01(point.threshold);
                    if (string.IsNullOrWhiteSpace(point.label))
                        point.label = ResourceDisplayName(kind) + " FX " + (i + 1);
                }
                if (config.visibility == ResourceVisibilityMode.ApplicableOnly && string.IsNullOrWhiteSpace(ResourceApplicabilityParameter(kind)))
                    config.visibility = ResourceVisibilityMode.Always;
            }
        }

        private void EnsureHealthBlendProfileDefaults()
        {
            // Compatibility wrapper for TB13 call sites; TB14 uses Resource FX profiles.
            EnsureResourceGaugeProfileDefaults();
        }

        private void OpenHealthFxEditor()
        {
            selectedResourceGaugeIndex = 0;
            animationPage = AnimationPage.Resources;
            tab = StudioTab.AnimatorSetup;
            Repaint();
        }

        private void OpenResourceFxEditor(ResourceGaugeKind kind)
        {
            selectedResourceGaugeIndex = Mathf.Clamp((int)kind, 0, AllResourceGaugeKinds().Count() - 1);
            animationPage = AnimationPage.Resources;
            tab = StudioTab.AnimatorSetup;
            Repaint();
        }

        private bool TryGetResourceKindForParameter(string parameterName, out ResourceGaugeKind kind)
        {
            foreach (var candidate in AllResourceGaugeKinds())
            {
                if (parameterName == ResourceParameter(candidate) ||
                    parameterName == ResourceApplicabilityParameter(candidate) ||
                    parameterName == ResourceSpecialBoolParameter(candidate))
                {
                    kind = candidate;
                    return true;
                }
            }
            kind = ResourceGaugeKind.Health;
            return false;
        }

        private bool DesiredNetworkSync(string parameterName, bool fallback)
        {
            ResourceGaugeKind kind;
            if (!TryGetResourceKindForParameter(parameterName, out kind))
                return fallback;
            var profile = FindResourceGaugeProfile(kind);
            return profile != null ? profile.networkSynced : ResourceDefaultNetworkSync(kind);
        }

        private void SaveAnimationProfile()
        {
            animationProfileAssetPath = CurrentAnimationProfilePath();
            if (string.IsNullOrWhiteSpace(animationProfileAssetPath))
                return;
            try
            {
                EnsureHealthBlendProfileDefaults();
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

        private static bool IsAnimationClipEmpty(AnimationClip clip)
        {
            if (clip == null)
                return true;
            return AnimationUtility.GetCurveBindings(clip).Length == 0 &&
                   AnimationUtility.GetObjectReferenceCurveBindings(clip).Length == 0;
        }

        private bool IsLegacyGeneratedEmptyPresentation(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(GeneratedAssetRoot + "/", StringComparison.OrdinalIgnoreCase))
                return false;
            var fileName = Path.GetFileName(normalized);
            var looksLikeLegacyPlaceholder =
                fileName.EndsWith("__Cast.anim", StringComparison.OrdinalIgnoreCase) ||
                (fileName.StartsWith("SOY_Spell_", StringComparison.OrdinalIgnoreCase) &&
                 fileName.IndexOf("_Wait", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_Recovery", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_AutoPresentation", StringComparison.OrdinalIgnoreCase) < 0) ||
                (fileName.StartsWith("SOY_Technick_", StringComparison.OrdinalIgnoreCase) &&
                 fileName.IndexOf("_Wait", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_Recovery", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_AutoPresentation", StringComparison.OrdinalIgnoreCase) < 0) ||
                (fileName.StartsWith("SOY_Item_", StringComparison.OrdinalIgnoreCase) &&
                 fileName.IndexOf("_Wait", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_Recovery", StringComparison.OrdinalIgnoreCase) < 0 &&
                 fileName.IndexOf("_AutoPresentation", StringComparison.OrdinalIgnoreCase) < 0);
            if (!looksLikeLegacyPlaceholder)
                return false;
            return IsAnimationClipEmpty(LoadClipFromPath(path));
        }

        private static AnimationPresetBinding FindAnimationPreset(List<AnimationPresetBinding> presets, string key)
        {
            if (presets == null || string.IsNullOrWhiteSpace(key))
                return null;
            return presets.FirstOrDefault(x => x != null && string.Equals(x.key, key, StringComparison.Ordinal));
        }

        private static AnimationPresetBinding GetOrCreateAnimationPreset(List<AnimationPresetBinding> presets, string key)
        {
            if (presets == null || string.IsNullOrWhiteSpace(key))
                return null;
            var existing = FindAnimationPreset(presets, key);
            if (existing != null)
                return existing;
            existing = new AnimationPresetBinding { key = key, clipPath = string.Empty };
            presets.Add(existing);
            return existing;
        }

        private static bool TryGetSpellDefinitionById(int id, out SpellDefinition spell)
        {
            foreach (var candidate in SpellDefinitions)
            {
                if (candidate.Id != id)
                    continue;
                spell = candidate;
                return true;
            }
            spell = default(SpellDefinition);
            return false;
        }

        private AnimationClip ResolveSpellPresentationClip(SpellAnimationBinding binding, out string source)
        {
            source = "Auto Functional";
            if (binding == null)
                return null;

            var custom = LoadClipFromPath(binding.clipPath);
            if (custom != null && !IsLegacyGeneratedEmptyPresentation(binding.clipPath))
            {
                source = "Per-spell override";
                return custom;
            }

            SpellDefinition spell;
            if (TryGetSpellDefinitionById(binding.id, out spell))
            {
                var school = FindAnimationPreset(animationProfile.spellSchoolPresentationClips, spell.School.ToString());
                var schoolClip = school != null ? LoadClipFromPath(school.clipPath) : null;
                if (schoolClip != null)
                {
                    source = spell.School + " preset";
                    return schoolClip;
                }

                var category = FindAnimationPreset(animationProfile.spellCategoryPresentationClips, spell.Category.ToString());
                var categoryClip = category != null ? LoadClipFromPath(category.clipPath) : null;
                if (categoryClip != null)
                {
                    source = GetSpellCategoryDisplayName(spell.Category) + " preset";
                    return categoryClip;
                }
            }

            var shared = LoadClipFromPath(animationProfile.defaultSpellPresentationClipPath);
            if (shared != null)
            {
                source = "Shared Spell default";
                return shared;
            }
            return null;
        }

        private AnimationClip ResolveActionPresentationClip(ActionAnimationKind kind, ActionAnimationBinding binding, out string source)
        {
            source = "Auto Functional";
            if (binding == null)
                return null;
            var custom = LoadClipFromPath(binding.clipPath);
            if (custom != null && !IsLegacyGeneratedEmptyPresentation(binding.clipPath))
            {
                source = "Per-action override";
                return custom;
            }
            var sharedPath = kind == ActionAnimationKind.Technick
                ? animationProfile.defaultTechnickPresentationClipPath
                : animationProfile.defaultItemPresentationClipPath;
            var shared = LoadClipFromPath(sharedPath);
            if (shared != null)
            {
                source = "Shared " + kind + " default";
                return shared;
            }
            return null;
        }

        private int SyncInstalledActionAnimationProfile(ActionAnimationKind kind, bool saveProfile = true)
        {
            if (animationProfile == null)
                animationProfile = new AvatarAnimationProfile();
            if (animationProfile.spellAnimations == null) animationProfile.spellAnimations = new List<SpellAnimationBinding>();
            if (animationProfile.technickAnimations == null) animationProfile.technickAnimations = new List<ActionAnimationBinding>();
            if (animationProfile.itemAnimations == null) animationProfile.itemAnimations = new List<ActionAnimationBinding>();
            if (animationProfile.spellSchoolPresentationClips == null) animationProfile.spellSchoolPresentationClips = new List<AnimationPresetBinding>();
            if (animationProfile.spellCategoryPresentationClips == null) animationProfile.spellCategoryPresentationClips = new List<AnimationPresetBinding>();

            var changed = 0;
            if (kind == ActionAnimationKind.Spell)
            {
                foreach (var installed in GetInstalledSpellDefinitions())
                {
                    var binding = animationProfile.spellAnimations.FirstOrDefault(x => x != null && x.id == installed.Id);
                    if (binding == null)
                    {
                        binding = new SpellAnimationBinding
                        {
                            id = installed.Id,
                            name = installed.Name,
                            enabled = true,
                            recoverySeconds = DefaultSpellRecoverySeconds,
                            contactWindowSeconds = DefaultContactWindowSeconds
                        };
                        animationProfile.spellAnimations.Add(binding);
                        changed++;
                    }
                    if (binding.name != installed.Name) { binding.name = installed.Name; changed++; }
                    if (binding.recoverySeconds <= 0f) { binding.recoverySeconds = DefaultSpellRecoverySeconds; changed++; }
                    if (binding.contactWindowSeconds <= 0f) { binding.contactWindowSeconds = DefaultContactWindowSeconds; changed++; }
                    if (IsLegacyGeneratedEmptyPresentation(binding.clipPath)) { binding.clipPath = string.Empty; changed++; }
                }
            }
            else
            {
                var definitions = kind == ActionAnimationKind.Technick ? GetInstalledTechnickDefinitions() : GetInstalledItemDefinitions();
                var bindings = kind == ActionAnimationKind.Technick ? animationProfile.technickAnimations : animationProfile.itemAnimations;
                var fallbackRecovery = kind == ActionAnimationKind.Technick ? DefaultTechnickRecoverySeconds : DefaultItemRecoverySeconds;
                foreach (var installed in definitions)
                {
                    var binding = bindings.FirstOrDefault(x => x != null && x.id == installed.Id);
                    if (binding == null)
                    {
                        binding = new ActionAnimationBinding
                        {
                            id = installed.Id,
                            name = installed.Name,
                            enabled = true,
                            recoverySeconds = fallbackRecovery,
                            contactWindowSeconds = DefaultContactWindowSeconds
                        };
                        bindings.Add(binding);
                        changed++;
                    }
                    if (binding.name != installed.Name) { binding.name = installed.Name; changed++; }
                    if (binding.recoverySeconds <= 0f) { binding.recoverySeconds = fallbackRecovery; changed++; }
                    if (binding.contactWindowSeconds <= 0f) { binding.contactWindowSeconds = DefaultContactWindowSeconds; changed++; }
                    if (IsLegacyGeneratedEmptyPresentation(binding.clipPath)) { binding.clipPath = string.Empty; changed++; }
                }
            }

            if (changed > 0 && saveProfile)
                SaveAnimationProfile();
            return changed;
        }

        private void RebuildAllAutomatedActionLayers()
        {
            if (avatarRoot == null || fxController == null)
                return;
            var changes = 0;
            changes += SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell, false);
            changes += SyncInstalledActionAnimationProfile(ActionAnimationKind.Technick, false);
            changes += SyncInstalledActionAnimationProfile(ActionAnimationKind.Item, false);
            if (changes > 0)
                SaveAnimationProfile();

            RebuildSpellCastAnimationLayer();
            RebuildActionAnimationLayer(ActionAnimationKind.Technick, "SoY_TechnickType", TechnickCastLayer, animationProfile.technickAnimations);
            RebuildActionAnimationLayer(ActionAnimationKind.Item, "SoY_ItemType", ItemUseLayer, animationProfile.itemAnimations);
            RebuildAllHelpfulItemInteractions();
            RebuildSpellAlignmentLayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log(BuildNumber + " automated all installed Spell/Technick/Item presentation + functional action layers. No per-action animation clip is required.");
        }

        private void DrawSpellAnimationBuilder()
        {
            BeginCard("Spell Automation & Presentation");
            var installed = GetInstalledSpellDefinitions();
            EditorGUILayout.LabelField(
                "TB17 separates functional spell authoring from optional avatar presentation. Installed spell Contacts/Raycasts are discovered automatically; the tool generates their gates, active windows, reset clips, and recovery timing without requiring one AnimationClip per spell.",
                wrappedLabel);
            EditorGUILayout.HelpBox(
                "Presentation clips only animate the avatar (pose, hands, face, staff, etc.). Leaving every presentation field blank is valid: the spell still works and TB17 supplies a deterministic generated timer motion while the functional Contact/Raycast layers do the real toggling.",
                MessageType.Info);
            DrawTagRow("Installed Spells", installed.Length.ToString(), installed.Length == 0 ? "Create/repair spell Contacts or Raycasts first" : "Automatically discovered from managed action objects");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("SYNC INSTALLED SPELLS", GUILayout.Height(largeControls ? 38f : 28f)))
            {
                var changed = SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell);
                Log(BuildNumber + " spell automation sync updated " + changed + " profile field(s).");
            }
            using (new EditorGUI.DisabledScope(fxController == null || installed.Length == 0))
            {
                if (GUILayout.Button("AUTOMATE / REPAIR INSTALLED SPELLS", GUILayout.Height(largeControls ? 38f : 28f)))
                {
                    SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell);
                    RebuildSpellCastAnimationLayer();
                }
            }
            EditorGUILayout.EndHorizontal();

            var shared = LoadClipFromPath(animationProfile.defaultSpellPresentationClipPath);
            var nextShared = (AnimationClip)EditorGUILayout.ObjectField("Shared Spell Cast", shared, typeof(AnimationClip), false);
            if (nextShared != shared)
            {
                animationProfile.defaultSpellPresentationClipPath = ClipPath(nextShared);
                SaveAnimationProfile();
            }
            EditorGUILayout.LabelField("Resolution order: per-spell override → school preset → purpose/category preset → shared default → generated automatic timer.", EditorStyles.miniLabel);

            showSpellPresentationPresets = EditorGUILayout.Foldout(showSpellPresentationPresets, "Optional Shared Spell Presets", true);
            if (showSpellPresentationPresets)
            {
                EditorGUILayout.LabelField("School Presets", cardTitleStyle);
                foreach (SpellSchool school in Enum.GetValues(typeof(SpellSchool)))
                {
                    var preset = GetOrCreateAnimationPreset(animationProfile.spellSchoolPresentationClips, school.ToString());
                    var clip = LoadClipFromPath(preset.clipPath);
                    var next = (AnimationClip)EditorGUILayout.ObjectField(school.ToString(), clip, typeof(AnimationClip), false);
                    if (next != clip)
                    {
                        preset.clipPath = ClipPath(next);
                        SaveAnimationProfile();
                    }
                }
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Purpose / Category Presets", cardTitleStyle);
                foreach (SpellCategory category in Enum.GetValues(typeof(SpellCategory)))
                {
                    var preset = GetOrCreateAnimationPreset(animationProfile.spellCategoryPresentationClips, category.ToString());
                    var clip = LoadClipFromPath(preset.clipPath);
                    var next = (AnimationClip)EditorGUILayout.ObjectField(GetSpellCategoryDisplayName(category), clip, typeof(AnimationClip), false);
                    if (next != clip)
                    {
                        preset.clipPath = ClipPath(next);
                        SaveAnimationProfile();
                    }
                }
            }

            SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell, false);
            animationSpellSearch = EditorGUILayout.TextField("Filter Installed", animationSpellSearch);
            var installedIds = new HashSet<int>(installed.Select(x => x.Id));
            var visible = animationProfile.spellAnimations
                .Where(binding => binding != null && installedIds.Contains(binding.id))
                .Where(binding => string.IsNullOrWhiteSpace(animationSpellSearch) ||
                    binding.name.IndexOf(animationSpellSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    binding.id.ToString().Contains(animationSpellSearch.Trim()))
                .OrderBy(binding => binding.id)
                .ToList();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Installed Spell Presentation Overrides", cardTitleStyle);
            foreach (var binding in visible)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                binding.enabled = EditorGUILayout.Toggle(binding.enabled, GUILayout.Width(20f));
                EditorGUILayout.LabelField(binding.id + " — " + binding.name, GUILayout.Width(AccessibilityScale > 1f ? 300f : 235f));
                string source;
                var resolved = ResolveSpellPresentationClip(binding, out source);
                EditorGUILayout.LabelField(source, EditorStyles.miniLabel);
                if (EditorGUI.EndChangeCheck()) SaveAnimationProfile();
                EditorGUILayout.EndHorizontal();

                var custom = LoadClipFromPath(binding.clipPath);
                if (IsLegacyGeneratedEmptyPresentation(binding.clipPath))
                    custom = null;
                var nextCustom = (AnimationClip)EditorGUILayout.ObjectField("Per-spell Override", custom, typeof(AnimationClip), false);
                if (nextCustom != custom)
                {
                    binding.clipPath = ClipPath(nextCustom);
                    SaveAnimationProfile();
                }
                if (custom == null && resolved != null)
                    EditorGUILayout.LabelField("Resolved presentation: " + AssetDatabase.GetAssetPath(resolved), EditorStyles.miniLabel);
                else if (custom == null)
                    EditorGUILayout.LabelField("Resolved presentation: generated automatic timer (no manual animation required)", EditorStyles.miniLabel);

                var oldRecovery = binding.recoverySeconds;
                var oldWindow = binding.contactWindowSeconds;
                EditorGUILayout.BeginHorizontal();
                binding.recoverySeconds = Mathf.Max(0.1f, EditorGUILayout.FloatField("Recovery", binding.recoverySeconds));
                binding.contactWindowSeconds = Mathf.Max(0.05f, EditorGUILayout.FloatField("Contact Window", binding.contactWindowSeconds));
                EditorGUILayout.EndHorizontal();
                if (!Mathf.Approximately(oldRecovery, binding.recoverySeconds) || !Mathf.Approximately(oldWindow, binding.contactWindowSeconds))
                    SaveAnimationProfile();
                EditorGUILayout.EndVertical();
            }

            using (new EditorGUI.DisabledScope(fxController == null || installed.Length == 0))
            {
                if (GUILayout.Button("BUILD / REPAIR AUTOMATED SPELL LAYERS", GUILayout.Height(largeControls ? 50f : 38f)))
                {
                    SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell);
                    RebuildSpellCastAnimationLayer();
                }
            }
            EditorGUILayout.HelpBox(
                "TB17 no longer writes empty Raycast Cast clips into a spell as if they were required custom animations. Existing empty TB16-generated Cast placeholders are treated as legacy placeholders; non-empty clips are preserved as deliberate overrides.",
                MessageType.None);
            EndCard();
        }

        private void DrawResourceFxBuilder()
        {
            EnsureResourceGaugeProfileDefaults();
            BeginCard("Resource FX Framework");
            EditorGUILayout.LabelField(
                "Health, MP, Mist, Curse of Diablos, and Arousal now share one managed Resource FX authoring path. Sam.py/Desktop remain authoritative for gameplay values; Unity only visualizes normalized 0.0–1.0 floats.",
                wrappedLabel);

            var kinds = AllResourceGaugeKinds().ToArray();
            selectedResourceGaugeIndex = Mathf.Clamp(selectedResourceGaugeIndex, 0, kinds.Length - 1);
            selectedResourceGaugeIndex = EditorGUILayout.Popup("Resource", selectedResourceGaugeIndex, kinds.Select(ResourceDisplayName).ToArray());
            var kind = kinds[selectedResourceGaugeIndex];
            var config = GetResourceGaugeProfile(kind);
            var changed = false;

            EditorGUILayout.Space(4f);
            DrawTagRow("OSC Driver", ResourceParameter(kind), "Normalized 0.0–1.0 value; Sam.py/Desktop authority");
            if (kind == ResourceGaugeKind.MP)
                EditorGUILayout.HelpBox("Desktop should publish SoY_MPPercent = current MP / max MP. Sam.py already tracks both mp and max_mp; Unity must not recalculate or own MP gameplay state.", MessageType.Info);
            EditorGUILayout.HelpBox("For smoother visible bars, the Desktop bridge may ease transmitted resource percentages over roughly 0.15–0.40 seconds. Sam.py values and combat logic remain instantaneous and authoritative; smoothing is presentation-only.", MessageType.None);

            var nextEnabled = EditorGUILayout.Toggle("Enabled", config.enabled);
            if (nextEnabled != config.enabled) { config.enabled = nextEnabled; changed = true; }

            var nextVisibility = (ResourceVisibilityMode)EditorGUILayout.EnumPopup("Visibility", config.visibility);
            if (nextVisibility == ResourceVisibilityMode.ApplicableOnly && string.IsNullOrWhiteSpace(ResourceApplicabilityParameter(kind)))
            {
                EditorGUILayout.HelpBox("Applicable Only is available only for resources with an applicability parameter. Falling back to Always.", MessageType.Warning);
                nextVisibility = ResourceVisibilityMode.Always;
            }
            if (nextVisibility != config.visibility) { config.visibility = nextVisibility; changed = true; }

            var nextNetworked = EditorGUILayout.Toggle("Visible To Remote Players", config.networkSynced);
            if (nextNetworked != config.networkSynced) { config.networkSynced = nextNetworked; changed = true; }
            var associatedBits = 8;
            if (!string.IsNullOrWhiteSpace(ResourceApplicabilityParameter(kind))) associatedBits += 1;
            if (!string.IsNullOrWhiteSpace(ResourceSpecialBoolParameter(kind))) associatedBits += 1;
            DrawTagRow("Parameter Cost", config.networkSynced ? associatedBits + " synced bits" : "Local only", config.networkSynced ? "Counts against VRChat's 256-bit budget" : "Does not consume synchronized parameter memory");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Blend Points", cardTitleStyle);
            EditorGUILayout.HelpBox(
                "Blend points are continuous. Two 0%/100% clips are enough for a smooth bar; add intermediate clips for staged poses, warnings, emission changes, or other avatar-specific effects.",
                MessageType.Info);

            var removeIndex = -1;
            for (var i = 0; i < config.blendPoints.Count; i++)
            {
                var point = config.blendPoints[i];
                if (point == null) continue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                var pEnabled = EditorGUILayout.Toggle(point.enabled, GUILayout.Width(20f));
                var pLabel = EditorGUILayout.TextField(point.label ?? (ResourceDisplayName(kind) + " FX " + (i + 1)));
                if (GUILayout.Button("Remove", GUILayout.Width(70f))) removeIndex = i;
                EditorGUILayout.EndHorizontal();
                var pThreshold = EditorGUILayout.Slider("Percent", point.threshold, 0f, 1f);
                var clip = LoadClipFromPath(point.clipPath);
                var pClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
                EditorGUILayout.LabelField("Blend position: " + (pThreshold * 100f).ToString("0.0") + "%", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                if (pEnabled != point.enabled || pLabel != point.label || !Mathf.Approximately(pThreshold, point.threshold) || pClip != clip)
                {
                    point.enabled = pEnabled;
                    point.label = pLabel;
                    point.threshold = pThreshold;
                    point.clipPath = ClipPath(pClip);
                    changed = true;
                }
            }
            if (removeIndex >= 0 && removeIndex < config.blendPoints.Count)
            {
                config.blendPoints.RemoveAt(removeIndex);
                changed = true;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ ADD BLEND POINT"))
            {
                config.blendPoints.Add(new ResourceBlendPoint
                {
                    enabled = true,
                    label = ResourceDisplayName(kind) + " FX " + (config.blendPoints.Count + 1),
                    threshold = config.blendPoints.Count == 0 ? 1f : 0.75f
                });
                changed = true;
            }
            if (GUILayout.Button("RESTORE RESOURCE DEFAULTS"))
            {
                config.blendPoints = DefaultResourceBlendPoints(kind);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();

            var specialParameter = ResourceSpecialBoolParameter(kind);
            if (!string.IsNullOrWhiteSpace(specialParameter))
            {
                var special = LoadClipFromPath(config.specialClipPath);
                var nextSpecial = (AnimationClip)EditorGUILayout.ObjectField(ResourceSpecialStateName(kind) + " Animation", special, typeof(AnimationClip), false);
                if (nextSpecial != special) { config.specialClipPath = ClipPath(nextSpecial); changed = true; }
                DrawTagRow("Special State", specialParameter, ResourceSpecialStateName(kind) + " remains discrete and never blends with the gauge");
            }

            if (changed)
                SaveAnimationProfile();

            DrawQuickGaugeClipGenerator(kind, config);
            DrawResourceFxValidation(kind, config);

            var enabledPoints = config.blendPoints.Count(point => point != null && point.enabled);
            using (new EditorGUI.DisabledScope(fxController == null || !config.enabled || config.visibility == ResourceVisibilityMode.Never || enabledPoints == 0))
            {
                if (GUILayout.Button("APPLY / REPAIR RESOURCE FX", GUILayout.Height(largeControls ? 50f : 38f)))
                    RebuildAllResourceFxLayers();
            }
            EditorGUILayout.LabelField("One repair action rebuilds every enabled Stories-managed resource layer. Diagnostics only reports problems and links back here.", wrappedLabel);
            EndCard();
        }

        private void DrawQuickGaugeClipGenerator(ResourceGaugeKind kind, ResourceGaugeProfile config)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Quick Gauge Clip Generator", cardTitleStyle);
            EditorGUILayout.LabelField("Generate Empty/Full endpoint clips for common avatar-bar setups, then edit or replace the clips normally if you need something more complex.", wrappedLabel);
            gaugeClipGeneratorMode = (GaugeClipGeneratorMode)EditorGUILayout.EnumPopup("Generator", gaugeClipGeneratorMode);
            gaugeEmptyValue = EditorGUILayout.FloatField("Empty Value", gaugeEmptyValue);
            gaugeFullValue = EditorGUILayout.FloatField("Full Value", gaugeFullValue);

            switch (gaugeClipGeneratorMode)
            {
                case GaugeClipGeneratorMode.TransformScale:
                    gaugeGeneratorTarget = (GameObject)EditorGUILayout.ObjectField("Target Object", gaugeGeneratorTarget, typeof(GameObject), true);
                    gaugeScaleAxis = (GaugeScaleAxis)EditorGUILayout.EnumPopup("Scale Axis", gaugeScaleAxis);
                    break;
                case GaugeClipGeneratorMode.BlendShape:
                    gaugeBlendShapeRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh", gaugeBlendShapeRenderer, typeof(SkinnedMeshRenderer), true);
                    if (gaugeBlendShapeRenderer != null && gaugeBlendShapeRenderer.sharedMesh != null && gaugeBlendShapeRenderer.sharedMesh.blendShapeCount > 0)
                    {
                        gaugeBlendShapeIndex = Mathf.Clamp(gaugeBlendShapeIndex, 0, gaugeBlendShapeRenderer.sharedMesh.blendShapeCount - 1);
                        gaugeBlendShapeIndex = EditorGUILayout.Popup("BlendShape", gaugeBlendShapeIndex,
                            Enumerable.Range(0, gaugeBlendShapeRenderer.sharedMesh.blendShapeCount)
                                .Select(i => gaugeBlendShapeRenderer.sharedMesh.GetBlendShapeName(i)).ToArray());
                    }
                    break;
                case GaugeClipGeneratorMode.MaterialFloat:
                    gaugeMaterialRenderer = (Renderer)EditorGUILayout.ObjectField("Renderer", gaugeMaterialRenderer, typeof(Renderer), true);
                    gaugeMaterialProperty = EditorGUILayout.TextField("Material Float", gaugeMaterialProperty);
                    EditorGUILayout.LabelField("Example: _FillAmount or another animatable shader float. The clip binds material.<property> on the selected Renderer.", wrappedLabel);
                    break;
            }

            if (GUILayout.Button("GENERATE EMPTY / FULL GAUGE CLIPS"))
                GenerateGaugeEndpointClips(kind, config);
        }

        private void GenerateGaugeEndpointClips(ResourceGaugeKind kind, ResourceGaugeProfile config)
        {
            if (avatarRoot == null)
            {
                EditorUtility.DisplayDialog("Stories Resource FX", "Assign an avatar root first.", "OK");
                return;
            }

            Component component = null;
            string propertyName = null;
            string relativePath = null;
            Type bindingType = null;

            if (gaugeClipGeneratorMode == GaugeClipGeneratorMode.TransformScale)
            {
                if (gaugeGeneratorTarget == null) { EditorUtility.DisplayDialog("Stories Resource FX", "Choose a target object.", "OK"); return; }
                relativePath = AnimationUtility.CalculateTransformPath(gaugeGeneratorTarget.transform, avatarRoot.transform);
                bindingType = typeof(Transform);
                propertyName = gaugeScaleAxis == GaugeScaleAxis.X ? "m_LocalScale.x" : gaugeScaleAxis == GaugeScaleAxis.Y ? "m_LocalScale.y" : "m_LocalScale.z";
            }
            else if (gaugeClipGeneratorMode == GaugeClipGeneratorMode.BlendShape)
            {
                component = gaugeBlendShapeRenderer;
                if (gaugeBlendShapeRenderer == null || gaugeBlendShapeRenderer.sharedMesh == null || gaugeBlendShapeRenderer.sharedMesh.blendShapeCount == 0)
                { EditorUtility.DisplayDialog("Stories Resource FX", "Choose a SkinnedMeshRenderer with at least one BlendShape.", "OK"); return; }
                gaugeBlendShapeIndex = Mathf.Clamp(gaugeBlendShapeIndex, 0, gaugeBlendShapeRenderer.sharedMesh.blendShapeCount - 1);
                relativePath = AnimationUtility.CalculateTransformPath(gaugeBlendShapeRenderer.transform, avatarRoot.transform);
                bindingType = typeof(SkinnedMeshRenderer);
                propertyName = "blendShape." + gaugeBlendShapeRenderer.sharedMesh.GetBlendShapeName(gaugeBlendShapeIndex);
            }
            else
            {
                component = gaugeMaterialRenderer;
                if (gaugeMaterialRenderer == null || string.IsNullOrWhiteSpace(gaugeMaterialProperty))
                { EditorUtility.DisplayDialog("Stories Resource FX", "Choose a Renderer and material float property.", "OK"); return; }
                relativePath = AnimationUtility.CalculateTransformPath(gaugeMaterialRenderer.transform, avatarRoot.transform);
                bindingType = gaugeMaterialRenderer.GetType();
                var cleanProperty = gaugeMaterialProperty.Trim();
                if (cleanProperty.StartsWith("material.", StringComparison.Ordinal)) cleanProperty = cleanProperty.Substring("material.".Length);
                propertyName = "material." + cleanProperty;
            }

            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : avatarRoot.name);
            var folder = AvatarGeneratedFolder(avatarName, "Animations/Resources/" + MakeSafeAssetName(ResourceId(kind)));
            EnsureAssetFolder(folder);
            var emptyPath = folder + "/SOY_" + MakeSafeAssetName(ResourceId(kind)) + "_Gauge_Empty.anim";
            var fullPath = folder + "/SOY_" + MakeSafeAssetName(ResourceId(kind)) + "_Gauge_Full.anim";
            var empty = CreateOrReplaceGaugeClip(emptyPath, relativePath, bindingType, propertyName, gaugeEmptyValue);
            var full = CreateOrReplaceGaugeClip(fullPath, relativePath, bindingType, propertyName, gaugeFullValue);
            if (empty == null || full == null) return;

            AssignResourceEndpointClip(config, 0f, "Empty", emptyPath);
            AssignResourceEndpointClip(config, 1f, "Full", fullPath);
            SaveAnimationProfile();
            AssetDatabase.SaveAssets();
            operationLog.Insert(0, "Generated Empty/Full " + ResourceDisplayName(kind) + " gauge clips using " + gaugeClipGeneratorMode + ".");
        }

        private static AnimationClip CreateOrReplaceGaugeClip(string path, string relativePath, Type bindingType, string propertyName, float value)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            var clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path), frameRate = 60f };
            var binding = EditorCurveBinding.FloatCurve(relativePath ?? string.Empty, bindingType, propertyName);
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, 1f / 60f, value));
            AssetDatabase.CreateAsset(clip, path);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AssignResourceEndpointClip(ResourceGaugeProfile config, float threshold, string label, string path)
        {
            var point = config.blendPoints.FirstOrDefault(p => p != null && Mathf.Abs(p.threshold - threshold) < 0.0001f);
            if (point == null)
            {
                point = new ResourceBlendPoint { enabled = true, label = label, threshold = threshold };
                config.blendPoints.Add(point);
            }
            point.enabled = true;
            point.label = label;
            point.threshold = threshold;
            point.clipPath = path;
        }

        private void DrawResourceFxValidation(ResourceGaugeKind kind, ResourceGaugeProfile config)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Resource Validation", cardTitleStyle);
            var enabled = config.blendPoints.Where(p => p != null && p.enabled).OrderBy(p => p.threshold).ToList();
            var duplicate = enabled.GroupBy(p => Mathf.RoundToInt(Mathf.Clamp01(p.threshold) * 10000f)).Any(g => g.Count() > 1);
            var missingClipCount = enabled.Count(p => LoadClipFromPath(p.clipPath) == null);
            var layerCount = fxController == null ? 0 : fxController.layers.Count(layer => layer.name == ResourceLayerName(kind));
            var expectedSync = config.networkSynced;
            var expression = expressionParameters != null && expressionParameters.parameters != null
                ? expressionParameters.parameters.FirstOrDefault(p => p != null && p.name == ResourceParameter(kind))
                : null;
            DrawTagRow("Blend Thresholds", duplicate ? "! Duplicates" : "✓ Unique", duplicate ? "Repair nudges duplicates safely, but explicit unique positions are clearer" : enabled.Count + " enabled point(s)");
            DrawTagRow("Animation Clips", missingClipCount == 0 ? "✓ Assigned" : "! " + missingClipCount + " placeholder(s)", "Missing clips receive generated safe empty placeholders");
            DrawTagRow("Managed Layer", layerCount == 1 ? "✓ One" : layerCount == 0 ? "! Missing" : "✕ Duplicate", ResourceLayerName(kind));
            DrawTagRow("Expression Sync", expression == null ? "! Missing" : expression.networkSynced == expectedSync ? "✓ Matches" : "! Needs Repair", expectedSync ? "Remote-visible" : "Local only");
        }

        private AnimationClip GetOrCreatePlaceholderClip(string folder, string fileName)
        {
            EnsureAssetFolder(folder);
            var path = folder + "/" + MakeSafeAssetName(fileName) + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var expectedName = Path.GetFileNameWithoutExtension(path);
            if (clip != null)
            {
                if (clip.name != expectedName)
                {
                    clip.name = expectedName;
                    EditorUtility.SetDirty(clip);
                }
                return clip;
            }
            clip = new AnimationClip { frameRate = 60f, name = expectedName };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private GameObject GetOrCreateActionGateTimerAnchor()
        {
            if (avatarRoot == null)
                return null;
            var existing = avatarRoot.transform.Cast<Transform>()
                .FirstOrDefault(child => child != null && child.name == "[SoY System] Action Gate Timer");
            if (existing != null)
                return existing.gameObject;
            var anchor = new GameObject("[SoY System] Action Gate Timer");
            Undo.RegisterCreatedObjectUndo(anchor, "Create Stories Action Gate Timer");
            anchor.transform.SetParent(avatarRoot.transform, false);
            anchor.transform.localPosition = Vector3.zero;
            anchor.transform.localRotation = Quaternion.identity;
            anchor.transform.localScale = Vector3.one;
            return anchor;
        }

        private AnimationClip CreateOrReplaceTimerClip(string path, float seconds)
        {
            var anchor = GetOrCreateActionGateTimerAnchor();
            if (anchor == null)
                return GetOrCreatePlaceholderClip(Path.GetDirectoryName(path)?.Replace('\\', '/') ?? CurrentAvatarGeneratedFolder("Animations/System"), Path.GetFileNameWithoutExtension(path));
            EnsureAssetFolder(Path.GetDirectoryName(path)?.Replace('\\', '/') ?? CurrentAvatarGeneratedFolder("Animations/System"));
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            var clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path), frameRate = 60f };
            var duration = Mathf.Max(1f / 60f, seconds);
            var relativePath = GetRelativePath(avatarRoot.transform, anchor.transform) ?? string.Empty;
            var binding = EditorCurveBinding.FloatCurve(relativePath, typeof(Transform), "m_LocalPosition.x");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, duration, anchor.transform.localPosition.x));
            AssetDatabase.CreateAsset(clip, path);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void DriveValueOnStateEnter(AnimatorState state, string parameterName, float value)
        {
            if (state == null || string.IsNullOrWhiteSpace(parameterName))
                return;
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.localOnly = true;
            if (driver.parameters == null)
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>();
            driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                name = parameterName,
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                value = value
            });
        }

        private static void DriveBoolOnStateEnter(AnimatorState state, string parameterName, bool value)
        {
            DriveValueOnStateEnter(state, parameterName, value ? 1f : 0f);
        }

        private static string ApprovedParameterForKind(ActionAnimationKind kind)
        {
            switch (kind)
            {
                case ActionAnimationKind.Spell: return SpellApprovedParameter;
                case ActionAnimationKind.Technick: return TechnickApprovedParameter;
                case ActionAnimationKind.Item: return ItemApprovedParameter;
                default: return string.Empty;
            }
        }

        private static string SelectorParameterForKind(ActionAnimationKind kind)
        {
            switch (kind)
            {
                case ActionAnimationKind.Spell: return "SoY_SpellType";
                case ActionAnimationKind.Technick: return "SoY_TechnickType";
                case ActionAnimationKind.Item: return "SoY_ItemType";
                default: return string.Empty;
            }
        }

        private static string ContactGateLayerForKind(ActionAnimationKind kind)
        {
            switch (kind)
            {
                case ActionAnimationKind.Spell: return SpellContactGateLayer;
                case ActionAnimationKind.Technick: return TechnickContactGateLayer;
                case ActionAnimationKind.Item: return ItemContactGateLayer;
                default: return "Stories Of Yggdrasil | Action Contact Gate";
            }
        }

        private static string ContactHostPrefixForKind(ActionAnimationKind kind)
        {
            switch (kind)
            {
                case ActionAnimationKind.Spell: return "Stories Spell - ";
                case ActionAnimationKind.Technick: return "Stories Technick - ";
                case ActionAnimationKind.Item: return "Stories Item - ";
                default: return string.Empty;
            }
        }

        private static string VisualHolderNameForKind(ActionAnimationKind kind)
        {
            switch (kind)
            {
                case ActionAnimationKind.Spell: return "FX — Spell Visuals (Place Here)";
                case ActionAnimationKind.Technick: return "FX — Technick Visuals (Place Here)";
                case ActionAnimationKind.Item: return "FX — Item Visuals (Place Here)";
                default: return "FX — Action Visuals (Place Here)";
            }
        }

        private int EnsureManagedActionVisualHolders(ActionAnimationKind kind)
        {
            if (avatarRoot == null)
                return 0;
            var prefix = ContactHostPrefixForKind(kind);
            var holderName = VisualHolderNameForKind(kind);
            var created = 0;
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(x => x != null && x.name.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray())
            {
                var existing = transform.Cast<Transform>().FirstOrDefault(child => child != null && child.name == holderName);
                if (existing != null)
                    continue;
                CreateContactChild(transform.gameObject, holderName, true);
                created++;
            }
            if (created > 0)
                Log(BuildNumber + " created " + created + " managed " + kind + " visual holder(s). Place optional action VFX under these holders; the generated functional gate toggles their parent action automatically.");
            return created;
        }

        private bool IsInsideRaycastManagedPayload(Transform transform)
        {
            for (var current = transform != null ? transform.parent : null; current != null; current = current.parent)
            {
                if (current.name.StartsWith("[SoY Raycast Action] ", StringComparison.Ordinal) ||
                    current.name.StartsWith("[SoY Spell Placement] ", StringComparison.Ordinal) ||
                    current.name.StartsWith("[SoY Technick Placement] ", StringComparison.Ordinal))
                    return true;
                if (avatarRoot != null && current == avatarRoot.transform)
                    break;
            }
            return false;
        }

        private Dictionary<int, List<GameObject>> GetManagedActionContactHosts(ActionAnimationKind kind)
        {
            var result = new Dictionary<int, List<GameObject>>();
            if (avatarRoot == null)
                return result;
            var prefix = ContactHostPrefixForKind(kind);
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == null || !transform.name.StartsWith(prefix, StringComparison.Ordinal) || IsInsideRaycastManagedPayload(transform))
                    continue;
                var match = System.Text.RegularExpressions.Regex.Match(transform.name, @"^" + System.Text.RegularExpressions.Regex.Escape(prefix) + @"(\d+)\s");
                int id;
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out id) || id <= 0)
                    continue;
                List<GameObject> hosts;
                if (!result.TryGetValue(id, out hosts))
                {
                    hosts = new List<GameObject>();
                    result[id] = hosts;
                }
                if (!hosts.Contains(transform.gameObject))
                    hosts.Add(transform.gameObject);
            }
            return result;
        }

        private float GetRecoverySeconds(ActionAnimationKind kind, int actionId)
        {
            if (animationProfile != null)
            {
                if (kind == ActionAnimationKind.Spell)
                {
                    var binding = animationProfile.spellAnimations?.FirstOrDefault(x => x != null && x.id == actionId);
                    if (binding != null && binding.recoverySeconds > 0f) return binding.recoverySeconds;
                }
                else
                {
                    var list = kind == ActionAnimationKind.Technick ? animationProfile.technickAnimations : animationProfile.itemAnimations;
                    var binding = list?.FirstOrDefault(x => x != null && x.id == actionId);
                    if (binding != null && binding.recoverySeconds > 0f) return binding.recoverySeconds;
                }
            }
            return kind == ActionAnimationKind.Spell ? DefaultSpellRecoverySeconds :
                   kind == ActionAnimationKind.Technick ? DefaultTechnickRecoverySeconds : DefaultItemRecoverySeconds;
        }

        private float GetContactWindowSeconds(ActionAnimationKind kind, int actionId)
        {
            if (animationProfile != null)
            {
                if (kind == ActionAnimationKind.Spell)
                {
                    var binding = animationProfile.spellAnimations?.FirstOrDefault(x => x != null && x.id == actionId);
                    if (binding != null && binding.contactWindowSeconds > 0f) return binding.contactWindowSeconds;
                }
                else
                {
                    var list = kind == ActionAnimationKind.Technick ? animationProfile.technickAnimations : animationProfile.itemAnimations;
                    var binding = list?.FirstOrDefault(x => x != null && x.id == actionId);
                    if (binding != null && binding.contactWindowSeconds > 0f) return binding.contactWindowSeconds;
                }
            }
            return DefaultContactWindowSeconds;
        }

        private AnimationClip CreateOrReplaceSelectiveActiveClip(
            string path,
            IEnumerable<GameObject> allObjects,
            IEnumerable<GameObject> activeObjects,
            float length)
        {
            var clip = LoadOrCreateClip(path);
            clip.ClearCurves();
            var activeSet = new HashSet<GameObject>((activeObjects ?? Enumerable.Empty<GameObject>()).Where(x => x != null));
            var endTime = Mathf.Max(1f / 60f, length);
            foreach (var obj in (allObjects ?? Enumerable.Empty<GameObject>()).Where(x => x != null).Distinct())
            {
                var objectPath = GetRelativePath(avatarRoot.transform, obj.transform);
                if (objectPath == null) continue;
                var binding = EditorCurveBinding.FloatCurve(objectPath, typeof(GameObject), "m_IsActive");
                var value = activeSet.Contains(obj) ? 1f : 0f;
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, endTime, value));
            }
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private void RebuildManagedActionContactGate(ActionAnimationKind kind)
        {
            if (avatarRoot == null || fxController == null || !EnsureSafeFxCopy(false))
                return;
            var hostsById = GetManagedActionContactHosts(kind);
            var layerName = ContactGateLayerForKind(kind);
            RemoveLayerByName(fxController, layerName);
            if (hostsById.Count == 0)
                return;

            var selector = SelectorParameterForKind(kind);
            var approved = ApprovedParameterForKind(kind);
            EnsureAnimatorParameter(fxController, selector, AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, approved, AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);

            var allHosts = hostsById.Values.SelectMany(x => x).Where(x => x != null).Distinct().ToList();
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : avatarRoot.name);
            var folder = AvatarGeneratedFolder(avatarName, "Animations/Action Gates/" + kind);
            EnsureAssetFolder(folder);

            var layer = CreateHookLayer(fxController, layerName);
            var idle = AddHookState(layer.stateMachine, "Ready / Contacts Hidden", new Vector3(120f, 140f));
            idle.motion = CreateOrReplaceSelectiveActiveClip(folder + "/SOY_" + kind + "_Gate_Idle.anim", allHosts, Array.Empty<GameObject>(), 1f / 60f);
            DriveBoolOnStateEnter(idle, approved, false);
            layer.stateMachine.defaultState = idle;

            var index = 0;
            foreach (var pair in hostsById.OrderBy(x => x.Key))
            {
                var actionId = pair.Key;
                var window = Mathf.Max(0.05f, GetContactWindowSeconds(kind, actionId));
                var recoverySeconds = Mathf.Max(0.1f, GetRecoverySeconds(kind, actionId));
                var x = 480f + (index % 3) * 300f;
                var y = 40f + (index / 3) * 230f;
                var active = AddHookState(layer.stateMachine, actionId + " — Approved Contact Window", new Vector3(x, y));
                var wait = AddHookState(layer.stateMachine, actionId + " — Wait For Release", new Vector3(x, y + 75f));
                var recovery = AddHookState(layer.stateMachine, actionId + " — Recovery " + recoverySeconds.ToString("0.##") + "s", new Vector3(x, y + 150f));

                active.motion = CreateOrReplaceSelectiveActiveClip(folder + "/SOY_" + kind + "_Gate_" + actionId + "_Active.anim", allHosts, pair.Value, window);
                wait.motion = CreateOrReplaceSelectiveActiveClip(folder + "/SOY_" + kind + "_Gate_" + actionId + "_Wait.anim", allHosts, Array.Empty<GameObject>(), 1f / 60f);
                recovery.motion = CreateOrReplaceSelectiveActiveClip(folder + "/SOY_" + kind + "_Gate_" + actionId + "_Recovery.anim", allHosts, Array.Empty<GameObject>(), recoverySeconds);
                DriveBoolOnStateEnter(active, approved, true);
                DriveBoolOnStateEnter(wait, approved, false);
                DriveBoolOnStateEnter(recovery, approved, false);

                var enter = idle.AddTransition(active);
                enter.hasExitTime = false; enter.duration = 0f;
                enter.AddCondition(AnimatorConditionMode.Equals, actionId, selector);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

                var finish = active.AddTransition(wait);
                finish.hasExitTime = true; finish.exitTime = 1f; finish.duration = 0f;
                var ko = active.AddTransition(wait);
                ko.hasExitTime = false; ko.duration = 0f;
                ko.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

                var released = wait.AddTransition(recovery);
                released.hasExitTime = false; released.duration = 0f;
                released.AddCondition(AnimatorConditionMode.NotEqual, actionId, selector);

                var recovered = recovery.AddTransition(idle);
                recovered.hasExitTime = true; recovered.exitTime = 1f; recovered.duration = 0f;
                index++;
            }

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log(BuildNumber + " rebuilt " + kind + " contact gate for " + hostsById.Count + " action ID(s). Managed action Contacts now default hidden and open only during approved windows.");
        }

        private void RebuildSpellCastAnimationLayer()
        {
            if (fxController == null || !EnsureSafeFxCopy(true))
                return;

            EnsureManagedActionVisualHolders(ActionAnimationKind.Spell);
            SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell, false);
            var installedIds = new HashSet<int>(GetInstalledSpellDefinitions().Select(x => x.Id));
            var bindings = animationProfile.spellAnimations
                .Where(entry => entry != null && entry.enabled && installedIds.Contains(entry.id))
                .OrderBy(entry => entry.id)
                .ToList();

            RemoveLayerByName(fxController, SpellCastLayer);
            if (bindings.Count == 0)
            {
                RebuildManagedActionContactGate(ActionAnimationKind.Spell);
                EditorUtility.SetDirty(fxController);
                AssetDatabase.SaveAssets();
                return;
            }

            EnsureAnimatorParameter(fxController, "SoY_SpellType", AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);
            var layer = CreateHookLayer(fxController, SpellCastLayer);
            var idle = AddHookState(layer.stateMachine, "Ready / Idle", new Vector3(120f, 140f));
            layer.stateMachine.defaultState = idle;

            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AvatarGeneratedFolder(avatarName, "Animations/Spell Casts");
            EnsureAssetFolder(folder);
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                var recoverySeconds = Mathf.Max(0.1f, binding.recoverySeconds > 0f ? binding.recoverySeconds : DefaultSpellRecoverySeconds);
                var presentationSeconds = Mathf.Max(0.05f, binding.contactWindowSeconds > 0f ? binding.contactWindowSeconds : DefaultContactWindowSeconds);
                var x = 520f + (index % 3) * 310f;
                var y = 30f + (index / 3) * 240f;
                string source;
                var presentation = ResolveSpellPresentationClip(binding, out source);
                var state = AddHookState(layer.stateMachine, binding.id + " — " + binding.name + " [" + source + "]", new Vector3(x, y));
                var waitForRelease = AddHookState(layer.stateMachine, binding.id + " — Wait For Menu Release", new Vector3(x, y + 80f));
                var recovery = AddHookState(layer.stateMachine, binding.id + " — Recovery " + recoverySeconds.ToString("0.##") + "s", new Vector3(x, y + 160f));
                state.motion = presentation ?? CreateOrReplaceTimerClip(
                    folder + "/SOY_Spell_" + binding.id + "_AutoPresentation.anim",
                    presentationSeconds);
                waitForRelease.motion = CreateOrReplaceTimerClip(folder + "/SOY_Spell_" + binding.id + "_Wait.anim", 1f / 60f);
                recovery.motion = CreateOrReplaceTimerClip(folder + "/SOY_Spell_" + binding.id + "_Recovery.anim", recoverySeconds);

                // TB17 presentation layers do not write SoY_SpellApproved. Approval is owned
                // exclusively by the functional Contact/Raycast gate so optional pose clips
                // cannot fight the gameplay-facing gate parameter across Animator layers.
                var enter = idle.AddTransition(state);
                enter.hasExitTime = false; enter.duration = 0f;
                enter.AddCondition(AnimatorConditionMode.Equals, binding.id, "SoY_SpellType");
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

                var finish = state.AddTransition(waitForRelease);
                finish.hasExitTime = true; finish.exitTime = 1f; finish.duration = 0f;
                var ko = state.AddTransition(waitForRelease);
                ko.hasExitTime = false; ko.duration = 0f;
                ko.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

                var release = waitForRelease.AddTransition(recovery);
                release.hasExitTime = false; release.duration = 0f;
                release.AddCondition(AnimatorConditionMode.NotEqual, binding.id, "SoY_SpellType");

                var recovered = recovery.AddTransition(idle);
                recovered.hasExitTime = true; recovered.exitTime = 1f; recovered.duration = 0f;
            }
            fxController.AddLayer(layer);
            RebuildManagedActionContactGate(ActionAnimationKind.Spell);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log(BuildNumber + " built automated Spell presentation for " + bindings.Count + " installed spell(s). Functional approval remains owned by Contact/Raycast gates; no per-spell clip is required.");
        }

        private static void RemoveManagedResourceLayer(AnimatorController controller, ResourceGaugeKind kind)
        {
            if (controller == null) return;
            var layerName = ResourceLayerName(kind);
            for (var index = controller.layers.Length - 1; index >= 0; index--)
            {
                var layer = controller.layers[index];
                if (layer == null || layer.name != layerName) continue;
                var trees = layer.stateMachine != null
                    ? layer.stateMachine.states.Select(child => child.state != null ? child.state.motion as BlendTree : null).Where(tree => tree != null).Distinct().ToList()
                    : new List<BlendTree>();
                controller.RemoveLayer(index);
                foreach (var tree in trees)
                    if (tree != null && AssetDatabase.IsSubAsset(tree)) UnityEngine.Object.DestroyImmediate(tree, true);
            }
        }

        private static void RemoveManagedVitalLayer(AnimatorController controller)
        {
            RemoveManagedResourceLayer(controller, ResourceGaugeKind.Health);
        }

        private void RebuildAllResourceFxLayers()
        {
            if (fxController == null || !EnsureSafeFxCopy(true)) return;
            EnsureResourceGaugeProfileDefaults();
            Undo.RecordObject(fxController, "Rebuild Stories Resource FX");
            var built = 0;
            foreach (var kind in AllResourceGaugeKinds())
            {
                RemoveManagedResourceLayer(fxController, kind);
                var config = GetResourceGaugeProfile(kind);
                if (config != null && config.enabled && config.visibility != ResourceVisibilityMode.Never && config.blendPoints.Any(point => point != null && point.enabled))
                {
                    AddResourceGaugeLayer(fxController, kind);
                    built++;
                }
            }
            AddMissingAnimatorParameters(fxController);
            if (expressionParameters != null)
            {
                Undo.RecordObject(expressionParameters, "Repair Resource FX Expression Parameters");
                AddMissingExpressionParameters(expressionParameters);
                EditorUtility.SetDirty(expressionParameters);
            }
            CleanupOrphanedManagedResourceBlendTrees(fxController);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            RefreshHealthAudit();
            Log("Rebuilt Resource FX framework with " + built + " enabled gauge layer(s).");
        }

        private void RebuildHealthAnimationLayer()
        {
            RebuildAllResourceFxLayers();
        }

        private static void CleanupOrphanedManagedResourceBlendTrees(AnimatorController controller)
        {
            if (controller == null) return;
            var path = AssetDatabase.GetAssetPath(controller);
            if (string.IsNullOrWhiteSpace(path)) return;
            var referenced = new HashSet<BlendTree>();
            foreach (var layer in controller.layers)
            {
                if (layer == null || layer.stateMachine == null) continue;
                foreach (var child in layer.stateMachine.states)
                {
                    var tree = child.state != null ? child.state.motion as BlendTree : null;
                    if (tree != null) referenced.Add(tree);
                }
            }
            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>().ToList())
            {
                if (tree == null || referenced.Contains(tree)) continue;
                if (tree.name.StartsWith("SOY ", StringComparison.Ordinal) && tree.name.IndexOf("Blend Tree", StringComparison.OrdinalIgnoreCase) >= 0)
                    UnityEngine.Object.DestroyImmediate(tree, true);
            }
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
                    EditorGUILayout.LabelField("Spell/Technick/Item functional layers are auto-authored from installed actions. Optionally assign shared or per-action presentation clips, then configure Evasion and Resource FX.", wrappedLabel);
                    if (GUILayout.Button("OPEN ANIMATION BINDINGS")) tab = StudioTab.AnimatorSetup;
                    break;
                case WizardStep.Contacts:
                    EditorGUILayout.LabelField("Create outgoing and incoming Contacts. Use Raycast Studio for bullets/arrows/projectiles and for spells that should be placed on the floor beneath a remote player.", wrappedLabel);
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
            DrawTagRow("Evade Selector", ParameterStatus(EvadeTypeParameter, AnimatorControllerParameterType.Int, true), "TB17 remote-visible directional evade animation routing");
            DrawTagRow("Evading", ParameterStatus(EvadingParameter, AnimatorControllerParameterType.Bool, false), "Local animation-active telemetry; not gameplay invulnerability");
            DrawTagRow("Spell Approved", ParameterStatus(SpellApprovedParameter, AnimatorControllerParameterType.Bool, false), "TB17 functional-gate approval pulse; presentation layers do not write this parameter");
            DrawTagRow("Technick Approved", ParameterStatus(TechnickApprovedParameter, AnimatorControllerParameterType.Bool, false), "TB17 functional-gate approval pulse; presentation layers do not write this parameter");
            DrawTagRow("Item Approved", ParameterStatus(ItemApprovedParameter, AnimatorControllerParameterType.Bool, false), "TB17 functional-gate approval pulse");
            DrawTagRow("Raycast Approved", ParameterStatus(RaycastApprovedParameter, AnimatorControllerParameterType.Bool, false), "TB17 local raycast approval pulse");
            DrawTagRow("Health Percent", ParameterStatus("SoY_HPPercent", AnimatorControllerParameterType.Float, DesiredNetworkSync("SoY_HPPercent", true)), "Resource FX Health driver");
            DrawTagRow("MP Percent", ParameterStatus("SoY_MPPercent", AnimatorControllerParameterType.Float, DesiredNetworkSync("SoY_MPPercent", false)), "Sam.py mp / max_mp visualization");
            DrawTagRow("KO", ParameterStatus("SoY_KO", AnimatorControllerParameterType.Bool, DesiredNetworkSync("SoY_KO", true)), "Health special-state routing");
            DrawTagRow("Mist", ParameterStatus("SoY_MistPercent", AnimatorControllerParameterType.Float, DesiredNetworkSync("SoY_MistPercent", false)), "Resource FX Mist driver");
            DrawTagRow("Curse of Diablos", ParameterStatus("SoY_DiablosPercent", AnimatorControllerParameterType.Float, DesiredNetworkSync("SoY_DiablosPercent", false)), "Resource FX Curse driver");
            DrawTagRow("Arousal", ParameterStatus("SoY_ArousalPercent", AnimatorControllerParameterType.Float, DesiredNetworkSync("SoY_ArousalPercent", false)), "Resource FX Arousal driver");
            DrawTagRow("Arousal Discharge", ParameterStatus("SoY_ArousalDischargeReady", AnimatorControllerParameterType.Bool, DesiredNetworkSync("SoY_ArousalDischargeReady", false)), "Discharge-ready special state");
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
            DrawActionAnimationBuilder(ActionAnimationKind.Technick, ref animationTechnickSearch,
                animationProfile.technickAnimations, "SoY_TechnickType", TechnickCastLayer);
        }

        private void DrawItemAnimationBuilder()
        {
            DrawActionAnimationBuilder(ActionAnimationKind.Item, ref animationItemSearch,
                animationProfile.itemAnimations, "SoY_ItemType", ItemUseLayer);
        }

        private void DrawEvasionAnimationBuilder()
        {
            BeginCard("Evasion Animation Layer Builder");
            EditorGUILayout.LabelField(
                "TB17 retains the directional Evasion layer introduced in TB16. SoY_EvadeType is a synced selector for remote animation playback; SoY_Evading is local OSC telemetry only and does not grant gameplay invulnerability by itself.",
                wrappedLabel);
            EditorGUILayout.HelpBox(
                "Use this for sidesteps, dashes, rolls, or a generic fallback. The animation plays to completion, then waits for the menu/button selector to release before returning to Ready. Sam.py/Desktop remain authoritative for whether an incoming attack actually misses.",
                MessageType.Info);
            var nextGenericFallback = EditorGUILayout.ToggleLeft("Use Generic Evade for every unassigned direction/roll", animationProfile.useGenericEvadeFallback);
            if (nextGenericFallback != animationProfile.useGenericEvadeFallback)
            {
                animationProfile.useGenericEvadeFallback = nextGenericFallback;
                SaveAnimationProfile();
            }

            animationEvasionSearch = EditorGUILayout.TextField("Search Evade", animationEvasionSearch);
            var searchText = animationEvasionSearch ?? string.Empty;
            var trimmedSearch = searchText.Trim();
            var available = EvasionDefinitions.Where(x =>
                string.IsNullOrWhiteSpace(searchText) ||
                x.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.Id.ToString().Contains(trimmedSearch)).ToArray();
            if (available.Length > 0)
            {
                animationEvasionSelectionIndex = Mathf.Clamp(animationEvasionSelectionIndex, 0, available.Length - 1);
                animationEvasionSelectionIndex = EditorGUILayout.Popup("Evade", animationEvasionSelectionIndex, available.Select(x => x.Id + " — " + x.Name).ToArray());
                animationEvasionClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationEvasionClip, typeof(AnimationClip), false);
                if (GUILayout.Button("ADD / UPDATE SELECTED EVADE", GUILayout.Height(largeControls ? 42f : 30f)))
                {
                    var selected = available[animationEvasionSelectionIndex];
                    var binding = animationProfile.evasionAnimations.FirstOrDefault(x => x.id == selected.Id);
                    if (binding == null)
                    {
                        binding = new ActionAnimationBinding
                        {
                            id = selected.Id,
                            name = selected.Name,
                            enabled = true,
                            recoverySeconds = 0f,
                            contactWindowSeconds = 0f
                        };
                        animationProfile.evasionAnimations.Add(binding);
                    }
                    binding.name = selected.Name;
                    binding.clipPath = ClipPath(animationEvasionClip);
                    binding.enabled = true;
                    SaveAnimationProfile();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No evade option matches the search.", MessageType.Info);
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Selected Evasion States", cardTitleStyle);
            foreach (var binding in animationProfile.evasionAnimations.OrderBy(x => x.id).ToList())
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUI.BeginChangeCheck();
                binding.enabled = EditorGUILayout.Toggle(binding.enabled, GUILayout.Width(20f));
                if (EditorGUI.EndChangeCheck()) SaveAnimationProfile();
                EditorGUILayout.LabelField(binding.id + " — " + binding.name, GUILayout.Width(AccessibilityScale > 1f ? 260f : 210f));
                var clip = LoadClipFromPath(binding.clipPath);
                var nextClip = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                if (nextClip != clip)
                {
                    binding.clipPath = ClipPath(nextClip);
                    SaveAnimationProfile();
                }
                if (GUILayout.Button("Open", GUILayout.Width(55f)) && clip != null)
                {
                    Selection.activeObject = clip;
                    EditorGUIUtility.PingObject(clip);
                }
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    animationProfile.evasionAnimations.Remove(binding);
                    SaveAnimationProfile();
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(fxController == null || animationProfile.evasionAnimations.Count(x => x != null && x.enabled) == 0))
            {
                if (GUILayout.Button("BUILD / REPAIR EVASION ANIMATION LAYER", GUILayout.Height(largeControls ? 50f : 38f)))
                    RebuildEvasionAnimationLayer();
            }
            EndCard();
        }

        private void RebuildEvasionAnimationLayer()
        {
            if (fxController == null || !EnsureSafeFxCopy(true))
                return;
            var explicitBindings = animationProfile.evasionAnimations
                .Where(x => x != null && x.enabled)
                .OrderBy(x => x.id)
                .ToList();
            var explicitIds = new HashSet<int>(explicitBindings.Select(x => x.id));
            var bindings = explicitBindings.ToList();
            var generic = explicitBindings.FirstOrDefault(x => x.id == 9 && LoadClipFromPath(x.clipPath) != null);
            if (animationProfile.useGenericEvadeFallback && generic != null)
            {
                foreach (var definition in EvasionDefinitions.Where(x => x.Id != 9 && !explicitIds.Contains(x.Id)))
                {
                    bindings.Add(new ActionAnimationBinding
                    {
                        id = definition.Id,
                        name = definition.Name,
                        enabled = true,
                        clipPath = generic.clipPath
                    });
                }
            }
            bindings = bindings.OrderBy(x => x.id).ToList();
            if (bindings.Count == 0)
                return;

            EnsureAnimatorParameter(fxController, EvadeTypeParameter, AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, EvadingParameter, AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);
            RemoveLayerByName(fxController, EvasionLayer);
            var layer = CreateHookLayer(fxController, EvasionLayer);
            var idle = AddHookState(layer.stateMachine, "Ready / Idle", new Vector3(120f, 140f));
            DriveBoolOnStateEnter(idle, EvadingParameter, false);
            layer.stateMachine.defaultState = idle;

            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AvatarGeneratedFolder(avatarName, "Animations/Evasion");
            EnsureAssetFolder(folder);

            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                var x = 520f + (index % 3) * 310f;
                var y = 30f + (index / 3) * 170f;
                var fallbackLabel = !explicitIds.Contains(binding.id) ? " [Generic Fallback]" : string.Empty;
                var state = AddHookState(layer.stateMachine, binding.id + " — " + binding.name + fallbackLabel, new Vector3(x, y));
                var wait = AddHookState(layer.stateMachine, binding.id + " — Wait For Release", new Vector3(x, y + 80f));
                state.motion = LoadClipFromPath(binding.clipPath) ?? GetOrCreatePlaceholderClip(folder, "SOY_Evade_" + binding.id + "_" + binding.name);
                wait.motion = CreateOrReplaceTimerClip(folder + "/SOY_Evade_" + binding.id + "_Wait.anim", 1f / 60f);
                DriveBoolOnStateEnter(state, EvadingParameter, true);
                DriveBoolOnStateEnter(wait, EvadingParameter, false);

                var enter = idle.AddTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Equals, binding.id, EvadeTypeParameter);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

                var finish = state.AddTransition(wait);
                finish.hasExitTime = true;
                finish.exitTime = 1f;
                finish.duration = 0f;

                var release = wait.AddTransition(idle);
                release.hasExitTime = false;
                release.duration = 0f;
                release.AddCondition(AnimatorConditionMode.Equals, 0f, EvadeTypeParameter);
            }

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            operationLog.Insert(0, "TB17 rebuilt Evasion animation layer with " + bindings.Count + " directional/fallback state(s).");
        }

        private ActionDefinition[] GetInstalledEvasionDefinitions()
        {
            if (animationProfile == null || animationProfile.evasionAnimations == null)
                return Array.Empty<ActionDefinition>();
            var enabled = animationProfile.evasionAnimations.Where(x => x != null && x.enabled).ToList();
            var ids = new HashSet<int>(enabled.Select(x => x.id));
            var generic = enabled.FirstOrDefault(x => x.id == 9 && LoadClipFromPath(x.clipPath) != null);
            if (animationProfile.useGenericEvadeFallback && generic != null)
                return EvasionDefinitions.OrderBy(x => x.Id).ToArray();
            return EvasionDefinitions.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToArray();
        }

        private GameObject FindAvatarObjectByRelativePath(string relativePath)
        {
            if (avatarRoot == null || string.IsNullOrWhiteSpace(relativePath))
                return null;
            var found = avatarRoot.transform.Find(relativePath);
            return found != null ? found.gameObject : null;
        }

        private bool TryStoreHelpfulPropPath(ActionAnimationBinding binding, GameObject prop)
        {
            if (binding == null)
                return false;
            if (prop == null)
            {
                binding.physicalPropPath = string.Empty;
                return true;
            }
            if (avatarRoot == null)
                return false;
            var relative = GetRelativePath(avatarRoot.transform, prop.transform);
            if (relative == null)
            {
                EditorUtility.DisplayDialog(
                    "Helpful Item Prop",
                    "The selected object must live under the currently loaded avatar root.",
                    "OK");
                return false;
            }
            binding.physicalPropPath = relative;
            return true;
        }

        private static string GestureParameterForHand(HelpfulItemHand hand)
        {
            return hand == HelpfulItemHand.Left ? "GestureLeft" : "GestureRight";
        }

        private void DrawActionAnimationBuilder(
            ActionAnimationKind kind,
            ref string search,
            List<ActionAnimationBinding> bindings,
            string parameterName,
            string layerName)
        {
            BeginCard(kind + " Automation & Presentation");
            var installed = kind == ActionAnimationKind.Technick ? GetInstalledTechnickDefinitions() : GetInstalledItemDefinitions();
            EditorGUILayout.LabelField(
                "TB17 discovers installed " + kind + " actions automatically. Functional Contacts, approval windows, release handling, and recovery are generated by the tool; presentation animation is optional.",
                wrappedLabel);
            DrawTagRow("Installed " + kind + "s", installed.Length.ToString(), installed.Length == 0 ? "Create/repair managed actions first" : "Auto-discovered from the avatar hierarchy");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("SYNC INSTALLED", GUILayout.Height(largeControls ? 38f : 28f)))
            {
                var changed = SyncInstalledActionAnimationProfile(kind);
                Log(BuildNumber + " " + kind + " automation sync updated " + changed + " profile field(s).");
            }
            using (new EditorGUI.DisabledScope(fxController == null || installed.Length == 0))
            {
                if (GUILayout.Button("AUTOMATE / REPAIR", GUILayout.Height(largeControls ? 38f : 28f)))
                {
                    SyncInstalledActionAnimationProfile(kind);
                    RebuildActionAnimationLayer(kind, parameterName, layerName, bindings);
                }
            }
            EditorGUILayout.EndHorizontal();

            var defaultPath = kind == ActionAnimationKind.Technick
                ? animationProfile.defaultTechnickPresentationClipPath
                : animationProfile.defaultItemPresentationClipPath;
            var shared = LoadClipFromPath(defaultPath);
            var nextShared = (AnimationClip)EditorGUILayout.ObjectField("Shared " + kind + " Animation", shared, typeof(AnimationClip), false);
            if (nextShared != shared)
            {
                if (kind == ActionAnimationKind.Technick)
                    animationProfile.defaultTechnickPresentationClipPath = ClipPath(nextShared);
                else
                    animationProfile.defaultItemPresentationClipPath = ClipPath(nextShared);
                SaveAnimationProfile();
            }
            EditorGUILayout.LabelField("Resolution order: per-action override → shared default → generated automatic timer.", EditorStyles.miniLabel);

            SyncInstalledActionAnimationProfile(kind, false);
            search = EditorGUILayout.TextField("Filter Installed", search);
            var searchText = search ?? string.Empty;
            var installedIds = new HashSet<int>(installed.Select(x => x.Id));
            var visible = bindings
                .Where(binding => binding != null && installedIds.Contains(binding.id))
                .Where(binding => string.IsNullOrWhiteSpace(searchText) ||
                    binding.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    binding.id.ToString().Contains(searchText.Trim()))
                .OrderBy(binding => binding.id)
                .ToList();

            foreach (var binding in visible)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                binding.enabled = EditorGUILayout.Toggle(binding.enabled, GUILayout.Width(20f));
                EditorGUILayout.LabelField(binding.id + " — " + binding.name, GUILayout.Width(AccessibilityScale > 1f ? 300f : 235f));
                string source;
                var resolved = ResolveActionPresentationClip(kind, binding, out source);
                EditorGUILayout.LabelField(source, EditorStyles.miniLabel);
                if (EditorGUI.EndChangeCheck()) SaveAnimationProfile();
                EditorGUILayout.EndHorizontal();

                var custom = LoadClipFromPath(binding.clipPath);
                if (IsLegacyGeneratedEmptyPresentation(binding.clipPath))
                    custom = null;
                var nextCustom = (AnimationClip)EditorGUILayout.ObjectField("Per-action Override", custom, typeof(AnimationClip), false);
                if (nextCustom != custom)
                {
                    binding.clipPath = ClipPath(nextCustom);
                    SaveAnimationProfile();
                }
                if (custom == null && resolved != null)
                    EditorGUILayout.LabelField("Resolved presentation: " + AssetDatabase.GetAssetPath(resolved), EditorStyles.miniLabel);
                else if (custom == null)
                    EditorGUILayout.LabelField("Resolved presentation: generated automatic timer (no manual animation required)", EditorStyles.miniLabel);

                var oldRecovery = binding.recoverySeconds;
                var oldWindow = binding.contactWindowSeconds;
                EditorGUILayout.BeginHorizontal();
                binding.recoverySeconds = Mathf.Max(0.1f, EditorGUILayout.FloatField("Recovery", binding.recoverySeconds));
                binding.contactWindowSeconds = Mathf.Max(0.05f, EditorGUILayout.FloatField("Contact Window", binding.contactWindowSeconds));
                EditorGUILayout.EndHorizontal();
                if (!Mathf.Approximately(oldRecovery, binding.recoverySeconds) || !Mathf.Approximately(oldWindow, binding.contactWindowSeconds))
                    SaveAnimationProfile();
                EditorGUILayout.EndVertical();
            }

            using (new EditorGUI.DisabledScope(fxController == null || installed.Length == 0))
                if (GUILayout.Button("BUILD / REPAIR AUTOMATED " + kind.ToString().ToUpperInvariant() + " LAYERS", GUILayout.Height(largeControls ? 50f : 38f)))
                {
                    SyncInstalledActionAnimationProfile(kind);
                    RebuildActionAnimationLayer(kind, parameterName, layerName, bindings);
                }
            EndCard();
        }

        private void RebuildActionAnimationLayer(ActionAnimationKind kind, string parameterName, string layerName, List<ActionAnimationBinding> sourceBindings)
        {
            if (fxController == null || !EnsureSafeFxCopy(true)) return;
            EnsureManagedActionVisualHolders(kind);
            SyncInstalledActionAnimationProfile(kind, false);
            var installedIds = new HashSet<int>((kind == ActionAnimationKind.Technick ? GetInstalledTechnickDefinitions() : GetInstalledItemDefinitions()).Select(x => x.Id));
            var bindings = sourceBindings
                .Where(x => x != null && x.enabled && installedIds.Contains(x.id))
                .OrderBy(x => x.id)
                .ToList();

            RemoveLayerByName(fxController, layerName);
            if (bindings.Count == 0)
            {
                RebuildManagedActionContactGate(kind);
                EditorUtility.SetDirty(fxController);
                AssetDatabase.SaveAssets();
                return;
            }

            EnsureAnimatorParameter(fxController, parameterName, AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);
            var layer = CreateHookLayer(fxController, layerName);
            var idle = AddHookState(layer.stateMachine, "Ready / Idle", new Vector3(120f, 140f));
            layer.stateMachine.defaultState = idle;
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AvatarGeneratedFolder(avatarName, "Animations/" + kind + "s");
            EnsureAssetFolder(folder);
            for (var index = 0; index < bindings.Count; index++)
            {
                var binding = bindings[index];
                var fallbackRecovery = kind == ActionAnimationKind.Technick ? DefaultTechnickRecoverySeconds : DefaultItemRecoverySeconds;
                var recoverySeconds = Mathf.Max(0.1f, binding.recoverySeconds > 0f ? binding.recoverySeconds : fallbackRecovery);
                var presentationSeconds = Mathf.Max(0.05f, binding.contactWindowSeconds > 0f ? binding.contactWindowSeconds : DefaultContactWindowSeconds);
                var x = 520f + (index % 3) * 310f;
                var y = 30f + (index / 3) * 240f;
                string source;
                var presentation = ResolveActionPresentationClip(kind, binding, out source);
                var state = AddHookState(layer.stateMachine, binding.id + " — " + binding.name + " [" + source + "]", new Vector3(x, y));
                var wait = AddHookState(layer.stateMachine, binding.id + " — Wait For Menu Release", new Vector3(x, y + 80f));
                var recovery = AddHookState(layer.stateMachine, binding.id + " — Recovery " + recoverySeconds.ToString("0.##") + "s", new Vector3(x, y + 160f));
                state.motion = presentation ?? CreateOrReplaceTimerClip(
                    folder + "/SOY_" + kind + "_" + binding.id + "_AutoPresentation.anim",
                    presentationSeconds);
                wait.motion = CreateOrReplaceTimerClip(folder + "/SOY_" + kind + "_" + binding.id + "_Wait.anim", 1f / 60f);
                recovery.motion = CreateOrReplaceTimerClip(folder + "/SOY_" + kind + "_" + binding.id + "_Recovery.anim", recoverySeconds);

                // TB17: presentation does not own SoY_*Approved. The functional gate is the
                // sole writer, preventing an optional animation layer from opening/closing Contacts.
                var enter = idle.AddTransition(state);
                enter.hasExitTime = false; enter.duration = 0f;
                enter.AddCondition(AnimatorConditionMode.Equals, binding.id, parameterName);
                enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

                var finish = state.AddTransition(wait);
                finish.hasExitTime = true; finish.exitTime = 1f; finish.duration = 0f;
                var ko = state.AddTransition(wait);
                ko.hasExitTime = false; ko.duration = 0f;
                ko.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

                var release = wait.AddTransition(recovery);
                release.hasExitTime = false; release.duration = 0f;
                release.AddCondition(AnimatorConditionMode.NotEqual, binding.id, parameterName);

                var recovered = recovery.AddTransition(idle);
                recovered.hasExitTime = true; recovered.exitTime = 1f; recovered.duration = 0f;
            }
            fxController.AddLayer(layer);
            RebuildManagedActionContactGate(kind);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log(BuildNumber + " built automated " + kind + " presentation for " + bindings.Count + " installed action(s). Functional approval remains owned by the Contact/Raycast gate.");
        }

        private void DrawMenuBuilder()
        {
            BeginCard("VRChat Menu Builder");
            menuNavigationMode = (MenuNavigationMode)EditorGUILayout.EnumPopup("Navigation Layout", menuNavigationMode);
            shortVrchatLabels = EditorGUILayout.ToggleLeft("Use short accessibility labels", shortVrchatLabels);
            var installedSpells = GetInstalledSpellDefinitions();
            var installedTechnicks = GetInstalledTechnickDefinitions();
            var installedItems = GetInstalledItemDefinitions();
            DrawTagRow("Installed Contacts", installedSpells.Length + " Spells • " + installedTechnicks.Length + " Technicks • " + installedItems.Length + " Items", "Only these managed Contact actions are added to generated menus");
            EditorGUILayout.LabelField("Menus are install-aware: action buttons are generated only when matching Stories-managed Contacts exist on this avatar. Empty action categories are omitted instead of filling the menu with catalog entries the model cannot use.", wrappedLabel);
            EditorGUILayout.LabelField("Layouts: Combined, School First, Purpose First, Favorites First, and Compact Combat. Generated pages reserve Previous/Next slots and never exceed eight controls.", wrappedLabel);
            EndCard();

            BeginCard("Quick Access Favorites");
            menuBuilderSearch = EditorGUILayout.TextField("Search actions", menuBuilderSearch);
            if (string.IsNullOrWhiteSpace(menuBuilderSearch))
            {
                EditorGUILayout.LabelField("Type a name or numeric ID from the actions currently installed as managed Contacts on this avatar.", wrappedLabel);
            }
            else
            {
                var spells = GetInstalledSpellDefinitions().Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).Take(5).ToArray();
                foreach (var spell in spells)
                    if (GUILayout.Button("+ Spell " + spell.Id + " — " + spell.Name)) AddFavorite("spell", spell.Id, spell.Name);
                foreach (var action in GetInstalledTechnickDefinitions().Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).Take(5))
                    if (GUILayout.Button("+ Technick " + action.Id + " — " + action.Name)) AddFavorite("technick", action.Id, action.Name);
                foreach (var action in GetInstalledItemDefinitions().Where(x => x.Name.IndexOf(menuBuilderSearch, StringComparison.OrdinalIgnoreCase) >= 0 || x.Id.ToString() == menuBuilderSearch.Trim()).Take(5))
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
            EditorGUILayout.LabelField("Installed Menu Preview", cardTitleStyle);
            var labels = new List<string>();
            if (HasExpressionParameter("SoY_CombatEnabled") || HasExpressionParameter("SoY_IsEnemy")) labels.Add("Combat");
            if (GetInstalledSpellDefinitions().Length > 0) labels.Add("Spells");
            if (GetInstalledTechnickDefinitions().Length > 0 || GetInstalledItemDefinitions().Length > 0) labels.Add("Actions");
            if (HasInstalledManagedRaycast()) labels.Add("Targeting");
            if (HasExpressionParameter("SoY_MPPercent") || HasExpressionParameter("SoY_MistPercent") || HasExpressionParameter("SoY_DiablosPercent") || HasExpressionParameter("SoY_ArousalPercent")) labels.Add("Status");
            var installedFavorites = animationProfile.favorites.Count(favorite =>
                (favorite.kind == "spell" && GetInstalledSpellDefinitions().Any(entry => entry.Id == favorite.id)) ||
                (favorite.kind == "technick" && GetInstalledTechnickDefinitions().Any(entry => entry.Id == favorite.id)) ||
                (favorite.kind == "item" && GetInstalledItemDefinitions().Any(entry => entry.Id == favorite.id)));
            if (installedFavorites > 0) labels.Add("Quick Access");
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
            var installedSpellIds = new HashSet<int>(GetInstalledSpellDefinitions().Select(entry => entry.Id));
            var installedTechnickIds = new HashSet<int>(GetInstalledTechnickDefinitions().Select(entry => entry.Id));
            var installedItemIds = new HashSet<int>(GetInstalledItemDefinitions().Select(entry => entry.Id));
            foreach (var favorite in animationProfile.favorites.Where(favorite =>
                (favorite.kind == "spell" && installedSpellIds.Contains(favorite.id)) ||
                (favorite.kind == "technick" && installedTechnickIds.Contains(favorite.id)) ||
                (favorite.kind == "item" && installedItemIds.Contains(favorite.id))).Take(7))
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
                var desiredSync = DesiredNetworkSync(spec.Name, spec.NetworkSynced);
                var state = animator == null ? "✕ Animator" : animator.type != spec.AnimatorType ? "✕ Type" : expression == null ? "! Expression" : expression.networkSynced != desiredSync ? "! Sync Flag" : "✓";
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(state, GUILayout.Width(80f));
                EditorGUILayout.LabelField(spec.Name, GUILayout.Width(220f));
                EditorGUILayout.LabelField(spec.AnimatorType.ToString(), GUILayout.Width(70f));
                EditorGUILayout.LabelField(role, GUILayout.Width(150f));
                EditorGUILayout.LabelField(desiredSync ? ExpressionParameterCost(spec.ExpressionType) + " bits" : "Local", GUILayout.Width(60f));
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
            if (name.EndsWith("Type", StringComparison.Ordinal) || name == "SoY_HPPercent" || name == "SoY_MPPercent" || name == "SoY_CriticalHP" || name == "SoY_KO") return "Networked Animation";
            if (name == "SoY_MistPercent" || name == "SoY_DiablosPercent" || name == "SoY_ArousalPercent" || name == "SoY_DiablosApplicable" || name == "SoY_ArousalApplicable" || name == "SoY_ArousalDischargeReady") return "Resource FX";
            if (name.Contains("Bit") || name.EndsWith("Active", StringComparison.Ordinal)) return "Incoming Contact Bus";
            if (name.StartsWith("SoY_Hit") || name.StartsWith("SoY_Debuff")) return "Local OSC Input";
            return "Runtime Output";
        }

        private void DrawManagedResourceFxAudit()
        {
            EnsureResourceGaugeProfileDefaults();
            BeginCard("Managed Resource FX Audit");
            foreach (var kind in AllResourceGaugeKinds())
            {
                var config = GetResourceGaugeProfile(kind);
                var layers = fxController == null ? Array.Empty<AnimatorControllerLayer>() : fxController.layers.Where(x => x.name == ResourceLayerName(kind)).ToArray();
                var current = layers.Length == 1 && ResourceLayerIsCurrent(layers[0], kind);
                var expected = config != null && config.enabled && config.visibility != ResourceVisibilityMode.Never;
                DrawTagRow(ResourceDisplayName(kind), !expected ? "Disabled" : current ? "✓ Ready" : layers.Length > 1 ? "✕ Duplicate" : "! Missing / Legacy", ResourceParameter(kind));
            }
            if (GUILayout.Button("OPEN RESOURCE FX EDITOR"))
            {
                animationPage = AnimationPage.Resources;
                tab = StudioTab.AnimatorSetup;
            }
            EndCard();
        }

        private void DrawManagedHealthAudit()
        {
            DrawManagedResourceFxAudit();
        }

        private void DrawRuntimeTestPanel()
        {
            BeginCard("Runtime Resource & Action Test Panel");
            EditorGUILayout.LabelField("Enter Play Mode to write temporary resource parameters to the avatar Animator. Nothing is saved to the controller.", wrappedLabel);
            runtimeTestHpPercent = EditorGUILayout.Slider("HP Percent", runtimeTestHpPercent, 0f, 1f);
            runtimeTestMpPercent = EditorGUILayout.Slider("MP Percent", runtimeTestMpPercent, 0f, 1f);
            runtimeTestMistPercent = EditorGUILayout.Slider("Mist Percent", runtimeTestMistPercent, 0f, 1f);
            runtimeTestDiablosApplicable = EditorGUILayout.Toggle("Diablos Applicable", runtimeTestDiablosApplicable);
            runtimeTestDiablosPercent = EditorGUILayout.Slider("Diablos Percent", runtimeTestDiablosPercent, 0f, 1f);
            runtimeTestArousalApplicable = EditorGUILayout.Toggle("Arousal Applicable", runtimeTestArousalApplicable);
            runtimeTestArousalPercent = EditorGUILayout.Slider("Arousal Percent", runtimeTestArousalPercent, 0f, 1f);
            runtimeTestArousalDischargeReady = EditorGUILayout.Toggle("Arousal Discharge Ready", runtimeTestArousalDischargeReady);
            if (GUILayout.Button("APPLY RESOURCE TEST")) ApplyRuntimeResourceTest();
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

        private void ApplyRuntimeResourceTest()
        {
            var animator = RuntimeAnimator();
            runtimeTestSummary = "Resource preview: HP " + (runtimeTestHpPercent * 100f).ToString("0.0") +
                "% | MP " + (runtimeTestMpPercent * 100f).ToString("0.0") +
                "% | Mist " + (runtimeTestMistPercent * 100f).ToString("0.0") +
                "% | Diablos " + (runtimeTestDiablosPercent * 100f).ToString("0.0") +
                "% | Arousal " + (runtimeTestArousalPercent * 100f).ToString("0.0") + "%";
            if (!EditorApplication.isPlaying || animator == null)
            {
                runtimeTestSummary += ". Enter Play Mode with an Animator on the avatar root to write the parameters.";
                return;
            }
            animator.SetFloat("SoY_HPPercent", runtimeTestHpPercent);
            animator.SetFloat("SoY_MPPercent", runtimeTestMpPercent);
            animator.SetFloat("SoY_MistPercent", runtimeTestMistPercent);
            animator.SetFloat("SoY_DiablosPercent", runtimeTestDiablosPercent);
            animator.SetFloat("SoY_ArousalPercent", runtimeTestArousalPercent);
            animator.SetBool("SoY_KO", runtimeTestHpPercent <= 0f);
            animator.SetBool("SoY_CriticalHP", runtimeTestHpPercent > 0f && runtimeTestHpPercent < 0.15f);
            animator.SetBool("SoY_DiablosApplicable", runtimeTestDiablosApplicable);
            animator.SetBool("SoY_ArousalApplicable", runtimeTestArousalApplicable);
            animator.SetBool("SoY_ArousalDischargeReady", runtimeTestArousalDischargeReady);
        }

        private void ApplyRuntimeHealthTest()
        {
            ApplyRuntimeResourceTest();
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
                animator.SetFloat("SoY_MPPercent", 1f);
                animator.SetFloat("SoY_MistPercent", 0f);
                animator.SetFloat("SoY_DiablosPercent", 0f);
                animator.SetFloat("SoY_ArousalPercent", 0f);
                animator.SetBool("SoY_DiablosApplicable", false);
                animator.SetBool("SoY_ArousalApplicable", false);
                animator.SetBool("SoY_ArousalDischargeReady", false);
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
                EnsureAssetFolder(CurrentAvatarGeneratedFolder("Backups/Unity Tool"));
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CurrentAvatarGeneratedFolder("Backups/Unity Tool"));
                Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("OPEN MIGRATION SNAPSHOT FOLDER"))
            {
                EnsureAssetFolder(CurrentAvatarGeneratedFolder("Backups/Migrations"));
                var migrationAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CurrentAvatarGeneratedFolder("Backups/Migrations"));
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
            EnsureAssetFolder(CurrentAvatarGeneratedFolder("Backups/Manifests"));
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
            var path = AvatarGeneratedFolder(manifest.avatar, "Backups/Manifests") + "/Manifest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
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
            var guids = AssetDatabase.FindAssets("", new[] { CurrentAvatarGeneratedFolder("Backups/Manifests"), LegacyManifestRoot });
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
            fxController = original; SaveCurrentAvatarContext(); backupStatus = "Restored original FX: " + manifest.originalFxPath;
        }

        private void DrawRaycastDeliveryCard()
        {
            BeginCard("VRChat Raycast Studio");
            EditorGUILayout.LabelField(
                "v0.5.10 uses the official VRCRaycast component in two main ways: Direct Impact for bullets/arrows/projectiles, and World / Ground Placement for Spells or Technicks that need to appear beneath a targeted player.",
                wrappedLabel);
            DrawTagRow("SDK", RaycastTypeAvailable() ? "✓ VRCRaycast Available" : "✕ Requires VRChat Avatars SDK 3.10.3+", "Official avatar raycast component");
            var currentRaycastCount = CountRaycastComponents();
            DrawTagRow("Raycast budget", currentRaycastCount + " / 80", currentRaycastCount >= 72 ? "Near VRChat's avatar Raycast/FinalIK shared limit" : "Spell placement reuses two shared rays");
            var legacyRaycastCount = CountLegacyRaycastObjects();
            if (legacyRaycastCount > 0)
                DrawTagRow("Legacy TB3 objects", legacyRaycastCount.ToString(), "Create / Repair disables the matching old managed origin/result before rebuilding");
            DrawTagRow("Runtime params", "_Hit / _Ratio / _Distance", "VRCRaycast output prefix is created in the FX Animator");
            DrawTagRow("Ground fire gate", "Latched", "Button press arms the spell for 1.25s; valid player + floor hit then pulses placement for 0.85s");
            DrawTagRow("Self-hit protection", "Remote Player layer only", "Player (9) is targeted; PlayerLocal (10) is excluded");
            DrawTagRow("World placement", "Target → downward floor probe", "Final +Y follows the floor normal; the generated FX holder faces down");
            DrawTagRow("Target icon", "Local + toggleable", "Enable Targeting in the generated menu; it hides as soon as the cast/fire action is pressed");

            showRaycastInstructions = EditorGUILayout.Foldout(showRaycastInstructions, "How to use Raycasting", true);
            if (showRaycastInstructions)
            {
                EditorGUILayout.HelpBox(
                    "SPELLS / WORLD TECHNICKS: Select the hand/focus/weapon object that should aim. Spells use World / Ground Placement automatically; Technicks can choose Direct Impact or World / Ground Placement, then create it. Generate/repair the Stories menu, turn Targeting ON, aim until the local crosshair appears, then press/hold that installed Spell or Technick button. Once placement resolves, TB10 World Drops the action with VRChat Freeze To World and keeps it locked until the button is released (or a future toggle is turned off).",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    "BULLETS / ARROWS / PROJECTILES: Select the muzzle or projectile origin, choose Attack/Technick/Item/Debuff and Direct Impact Raycast delivery, then create it. Generate/repair the Stories menu and turn Targeting ON. Technick/Item buttons fire selector raycasts; Projectile Fire appears only when an installed Attack/Debuff raycast needs the manual fire trigger.",
                    MessageType.Info);
                EditorGUILayout.HelpBox(
                    "WORLD DROP VS FOLLOW: Before firing, the ground result follows the targeted player. After a valid cast/action, the placed Spell or World-placed Technick freezes in world space until the action is released. If you intentionally need an effect to keep following a moving target after cast, that remains a separate tracker-style system.",
                    MessageType.None);
                EditorGUILayout.HelpBox(
                    "ANIMATIONS: TB10 keeps every generated Raycast clip directly in <Avatar>/Animations/Raycasts with globally unique SOY_Raycast_* asset and clip names. Create / Repair automatically flattens legacy Raycast animation subfolders while preserving Unity GUID references. Selector actions (Spell/Technick/Item) are automatically added to the managed cast-animation layer.",
                    MessageType.None);
                EditorGUILayout.HelpBox(
                    "TESTING: Select the generated VRCRaycast to see its Scene-view gizmo. VRChat's avatar Raycast can also be exercised in Unity Play Mode before an avatar upload. Watch the generated _Hit, _Ratio, and _Distance Animator parameters while testing.",
                    MessageType.None);
            }

            raycastDistance = Mathf.Clamp(EditorGUILayout.FloatField("Maximum Aim Distance", raycastDistance), 0.1f, 1000f);
            raycastImpactRadius = Mathf.Clamp(EditorGUILayout.FloatField("Impact Contact Radius", raycastImpactRadius), 0.01f, 1f);

            if (outgoingKind == OutgoingContactKind.Spell)
            {
                raycastDeliveryStyle = RaycastDeliveryStyle.WorldGroundPlacement;
                EditorGUILayout.LabelField("Mode", "World / Ground Placement (Spell)");
                EditorGUILayout.LabelField("Aim Direction", raycastDirection.ToString(), EditorStyles.miniLabel);
            }
            else if (outgoingKind == OutgoingContactKind.Technick)
            {
                var technickMode = technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement ? 1 : 0;
                technickMode = EditorGUILayout.Popup(
                    "Mode",
                    technickMode,
                    new[] { "Direct Impact", "World / Ground Placement" });
                technickRaycastDeliveryStyle = technickMode == 1
                    ? RaycastDeliveryStyle.WorldGroundPlacement
                    : RaycastDeliveryStyle.DirectImpact;
                raycastDeliveryStyle = technickRaycastDeliveryStyle;

                if (technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement)
                    EditorGUILayout.LabelField("Targets", "Remote player → floor beneath target", EditorStyles.miniLabel);
                else
                    raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Collision Target", raycastCollisionTarget);
            }
            else
            {
                raycastDeliveryStyle = RaycastDeliveryStyle.DirectImpact;
                EditorGUILayout.LabelField("Mode", "Direct Impact");
                raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Collision Target", raycastCollisionTarget);
            }

            showRaycastAdvanced = EditorGUILayout.Foldout(showRaycastAdvanced, "Advanced Raycast Settings", true);
            if (showRaycastAdvanced)
            {
                raycastDirection = EditorGUILayout.Vector3Field("Local Aim Direction", raycastDirection);
                using (new EditorGUI.DisabledScope(CurrentRaycastUsesWorldGroundPlacement()))
                    raycastApplyRotation = EditorGUILayout.ToggleLeft("Direct impact: align result to hit surface", raycastApplyRotation);
                raycastCreateLineRenderer = EditorGUILayout.ToggleLeft("Create optional LineRenderer holder", raycastCreateLineRenderer);
                raycastUseCustomPrefix = EditorGUILayout.ToggleLeft("Use custom asset prefix", raycastUseCustomPrefix);
                if (raycastUseCustomPrefix)
                    raycastParameterPrefix = EditorGUILayout.TextField("Custom Prefix", raycastParameterPrefix);
            }

            var suggested = CurrentRaycastSuggestedPrefix();
            EditorGUILayout.LabelField("Generated action: " + suggested, EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!RaycastTypeAvailable() || !HasUsableTargets() || avatarRoot == null))
            {
                if (GUILayout.Button("CREATE / REPAIR RAYCAST FOR CURRENT ACTION", GUILayout.Height(largeControls ? 50f : 38f)))
                    CreateRaycastDeliveryForCurrentAction();
            }
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

        private int CountRaycastComponents()
        {
            var raycastType = FindRaycastType();
            if (avatarRoot == null || raycastType == null)
                return 0;
            return avatarRoot.GetComponentsInChildren<Component>(true)
                .Count(component => component != null && raycastType.IsInstanceOfType(component));
        }

        private static bool TryParseManagedActionId(string objectName, string prefix, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(prefix) ||
                !objectName.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            var suffix = objectName.Substring(prefix.Length).TrimStart();
            var digits = new string(suffix.TakeWhile(char.IsDigit).ToArray());
            return !string.IsNullOrWhiteSpace(digits) && int.TryParse(digits, out id) && id > 0;
        }

        private HashSet<int> GetInstalledManagedActionIds(string managedPrefix)
        {
            var result = new HashSet<int>();
            if (avatarRoot == null)
                return result;

            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                int id;
                if (transform != null && TryParseManagedActionId(transform.name, managedPrefix, out id))
                    result.Add(id);
            }
            return result;
        }

        private SpellDefinition[] GetInstalledSpellDefinitions()
        {
            var ids = GetInstalledManagedActionIds("Stories Spell - ");
            return SpellDefinitions
                .Where(spell => ids.Contains(spell.Id))
                .GroupBy(spell => spell.Id)
                .Select(group => group.First())
                .OrderBy(spell => spell.Id)
                .ToArray();
        }

        private ActionDefinition[] GetInstalledTechnickDefinitions()
        {
            var ids = GetInstalledManagedActionIds("Stories Technick - ");
            return TechnickDefinitions.Where(action => ids.Contains(action.Id)).OrderBy(action => action.Id).ToArray();
        }

        private ActionDefinition[] GetInstalledItemDefinitions()
        {
            var ids = GetInstalledManagedActionIds("Stories Item - ");
            return ItemDefinitions.Where(action => ids.Contains(action.Id)).OrderBy(action => action.Id).ToArray();
        }

        private bool HasInstalledManagedRaycast()
        {
            if (avatarRoot == null)
                return false;
            var raycastType = FindRaycastType();
            return raycastType != null && avatarRoot.GetComponentsInChildren<Component>(true)
                .Any(component => component != null && raycastType.IsInstanceOfType(component));
        }

        private bool HasManualRaycastFireAction()
        {
            if (avatarRoot == null)
                return false;
            return avatarRoot.GetComponentsInChildren<Transform>(true).Any(transform =>
                transform != null &&
                (transform.name.StartsWith("[SoY Direct Raycast Result] Attack_", StringComparison.Ordinal) ||
                 transform.name.StartsWith("[SoY Direct Raycast Result] Debuff_", StringComparison.Ordinal)));
        }

        private bool HasExpressionParameter(string parameterName)
        {
            return expressionParameters != null && expressionParameters.parameters != null &&
                expressionParameters.parameters.Any(parameter => parameter != null && parameter.name == parameterName);
        }

        private void EnsureLocalRaycastTargetingExpressionParameter()
        {
            if (expressionParameters == null || HasExpressionParameter(RaycastTargetingParameter))
                return;

            Undo.RecordObject(expressionParameters, "Add Local Stories Raycast Targeting Parameter");
            var list = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>()).ToList();
            list.Add(new VRCExpressionParameters.Parameter
            {
                name = RaycastTargetingParameter,
                valueType = VRCExpressionParameters.ValueType.Bool,
                defaultValue = 0f,
                saved = false,
                networkSynced = false
            });
            expressionParameters.parameters = list.ToArray();
            EditorUtility.SetDirty(expressionParameters);
        }

        private string RaycastAnimationRoot()
        {
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var root = AvatarGeneratedFolder(avatarName, "Animations/Raycasts");
            EnsureAssetFolder(root);
            return root;
        }

        private static string ShortStableAssetToken(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).Substring(0, 8);
            }
        }

        private static string CompactAnimationAssetToken(string value)
        {
            var safe = MakeSafeAssetName(value).Replace(' ', '_');
            while (safe.Contains("__"))
                safe = safe.Replace("__", "_");
            const int maxLength = 96;
            if (safe.Length <= maxLength)
                return safe;
            return safe.Substring(0, maxLength - 9) + "_" + ShortStableAssetToken(safe);
        }

        private string RaycastAnimationClipPath(string identity, string purpose)
        {
            var stem = "SOY_Raycast_" +
                CompactAnimationAssetToken(identity) + "__" +
                CompactAnimationAssetToken(purpose);
            return RaycastAnimationRoot() + "/" + stem + ".anim";
        }

        private static string RaycastSelectorKindToken(OutgoingContactKind kind)
        {
            switch (kind)
            {
                case OutgoingContactKind.Spell: return "Spell";
                case OutgoingContactKind.Technick: return "Technick";
                case OutgoingContactKind.Item: return "Item";
                case OutgoingContactKind.Attack: return "Attack";
                case OutgoingContactKind.Debuff: return "Debuff";
                case OutgoingContactKind.Blocking: return "Block";
                default: return "Action";
            }
        }

        private static string RaycastGateAnimationIdentity(
            string layerKey,
            string actionParameter,
            int actionValue,
            bool integerGate)
        {
            var category = "Manual";
            if (actionParameter == "SoY_SpellType") category = "Spell";
            else if (actionParameter == "SoY_TechnickType") category = "Technick";
            else if (actionParameter == "SoY_ItemType") category = "Item";

            return integerGate
                ? category + "_" + actionValue + "_" + MakeSafeAssetName(layerKey)
                : category + "_" + MakeSafeAssetName(layerKey);
        }

        private static string ExtractLegacyRaycastAnimationIdentity(string assetPath)
        {
            var normalized = (assetPath ?? string.Empty).Replace('\\', '/');
            var segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var prefixes = new[] { "Spell_", "Technick_", "Item_", "Attack_", "Debuff_" };
            for (var segmentIndex = segments.Length - 1; segmentIndex >= 0; segmentIndex--)
            {
                var segment = segments[segmentIndex];
                foreach (var prefix in prefixes)
                {
                    var marker = segment.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                    if (marker >= 0)
                        return CompactAnimationAssetToken(segment.Substring(marker));
                }
            }

            if (normalized.IndexOf("SpellGroundPlacement", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SpellGroundPlacement";
            if (normalized.IndexOf("TechnickGroundPlacement", StringComparison.OrdinalIgnoreCase) >= 0)
                return "TechnickGroundPlacement";
            return "Legacy_" + ShortStableAssetToken(normalized);
        }

        private void UpdateAnimationProfilePathAfterMove(string oldPath, string newPath)
        {
            var changed = false;
            foreach (var binding in animationProfile.spellAnimations)
            {
                if (string.Equals(binding.clipPath, oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    binding.clipPath = newPath;
                    changed = true;
                }
            }
            foreach (var binding in animationProfile.technickAnimations)
            {
                if (string.Equals(binding.clipPath, oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    binding.clipPath = newPath;
                    changed = true;
                }
            }
            foreach (var binding in animationProfile.itemAnimations)
            {
                if (string.Equals(binding.clipPath, oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    binding.clipPath = newPath;
                    changed = true;
                }
            }
            if (changed)
                SaveAnimationProfile();
        }

        private void NormalizeGeneratedAnimationClipNames()
        {
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var animationsRoot = AvatarGeneratedFolder(avatarName, "Animations");
            if (!AssetDatabase.IsValidFolder(animationsRoot))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { animationsRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                    continue;

                var relative = path.StartsWith(animationsRoot + "/", StringComparison.OrdinalIgnoreCase)
                    ? path.Substring(animationsRoot.Length + 1)
                    : Path.GetFileName(path);
                var raycastRoot = RaycastAnimationRoot();
                var pathDirectory = (Path.GetDirectoryName(path) ?? string.Empty).Replace('\\', '/');
                var fileStem = Path.GetFileNameWithoutExtension(path);
                var uniqueName = pathDirectory == raycastRoot && fileStem.StartsWith("SOY_Raycast_", StringComparison.Ordinal)
                    ? fileStem
                    : "SOY_" + CompactAnimationAssetToken(Path.ChangeExtension(relative, null).Replace('/', '_').Replace('\\', '_'));
                if (clip.name == uniqueName)
                    continue;
                clip.name = uniqueName;
                EditorUtility.SetDirty(clip);
            }
        }

        private void RemoveEmptyLegacyRaycastAnimationFolders()
        {
            var root = RaycastAnimationRoot();
            var fullRoot = Path.GetFullPath(root);
            if (!Directory.Exists(fullRoot))
                return;

            var directories = Directory.GetDirectories(fullRoot, "*", SearchOption.AllDirectories)
                .OrderByDescending(path => path.Length)
                .ToArray();
            foreach (var directory in directories)
            {
                if (Directory.EnumerateFileSystemEntries(directory).Any())
                    continue;
                var relative = directory.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
                var assetFolder = string.IsNullOrWhiteSpace(relative) ? root : root + "/" + relative;
                AssetDatabase.DeleteAsset(assetFolder);
            }
        }

        private void MigrateLegacyRaycastAnimationAssetsToFlatFolder()
        {
            var root = RaycastAnimationRoot();
            var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { root });
            var moved = 0;
            foreach (var guid in guids)
            {
                var oldPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                var directory = (Path.GetDirectoryName(oldPath) ?? string.Empty).Replace('\\', '/');
                var oldStem = Path.GetFileNameWithoutExtension(oldPath);
                if (directory == root && oldStem.StartsWith("SOY_Raycast_", StringComparison.Ordinal))
                    continue;

                var identity = ExtractLegacyRaycastAnimationIdentity(oldPath);
                var purpose = CompactAnimationAssetToken(oldStem);
                var newPath = RaycastAnimationClipPath(identity, purpose);
                if (AssetDatabase.LoadMainAssetAtPath(newPath) != null &&
                    !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    newPath = RaycastAnimationClipPath(
                        identity,
                        purpose + "_Migrated_" + ShortStableAssetToken(oldPath));
                }

                if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    var moveError = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (!string.IsNullOrWhiteSpace(moveError))
                    {
                        Log("TB10 could not flatten legacy Raycast animation '" + oldPath + "': " + moveError);
                        continue;
                    }
                    UpdateAnimationProfilePathAfterMove(oldPath, newPath);
                    moved++;
                }
            }

            RemoveEmptyLegacyRaycastAnimationFolders();
            NormalizeGeneratedAnimationClipNames();
            AssetDatabase.SaveAssets();
            if (moved > 0)
                Log("TB10 flattened " + moved + " legacy Raycast animation asset(s) into: " + root);
        }

        private AnimationClip EnsureRaycastSelectorAnimationBinding(
            OutgoingContactKind kind,
            int actionId,
            string actionName,
            GameObject hierarchyObject)
        {
            if (actionId <= 0 || string.IsNullOrWhiteSpace(actionName))
                return null;

            // TB17 no longer creates an empty Raycast Cast clip and records it as though
            // every action required a bespoke animation. Functional Raycast/Contact clips
            // are generated by their own gate layers; this method only ensures an optional
            // presentation binding exists and returns the resolved shared/custom clip.
            if (kind == OutgoingContactKind.Spell)
            {
                var binding = animationProfile.spellAnimations.FirstOrDefault(entry => entry.id == actionId);
                if (binding == null)
                {
                    binding = new SpellAnimationBinding
                    {
                        id = actionId,
                        name = actionName,
                        enabled = true,
                        recoverySeconds = DefaultSpellRecoverySeconds,
                        contactWindowSeconds = DefaultContactWindowSeconds
                    };
                    animationProfile.spellAnimations.Add(binding);
                }
                binding.name = actionName;
                binding.enabled = true;
                if (binding.recoverySeconds <= 0f) binding.recoverySeconds = DefaultSpellRecoverySeconds;
                if (binding.contactWindowSeconds <= 0f) binding.contactWindowSeconds = DefaultContactWindowSeconds;
                if (IsLegacyGeneratedEmptyPresentation(binding.clipPath)) binding.clipPath = string.Empty;
                SaveAnimationProfile();
                if (fxController != null)
                    RebuildSpellCastAnimationLayer();
                string source;
                return ResolveSpellPresentationClip(binding, out source);
            }

            if (kind == OutgoingContactKind.Technick || kind == OutgoingContactKind.Item)
            {
                var animationKind = kind == OutgoingContactKind.Technick ? ActionAnimationKind.Technick : ActionAnimationKind.Item;
                var bindings = kind == OutgoingContactKind.Technick ? animationProfile.technickAnimations : animationProfile.itemAnimations;
                var binding = bindings.FirstOrDefault(entry => entry.id == actionId);
                if (binding == null)
                {
                    binding = new ActionAnimationBinding
                    {
                        id = actionId,
                        name = actionName,
                        enabled = true,
                        recoverySeconds = kind == OutgoingContactKind.Technick ? DefaultTechnickRecoverySeconds : DefaultItemRecoverySeconds,
                        contactWindowSeconds = DefaultContactWindowSeconds
                    };
                    bindings.Add(binding);
                }
                binding.name = actionName;
                binding.enabled = true;
                if (binding.recoverySeconds <= 0f) binding.recoverySeconds = kind == OutgoingContactKind.Technick ? DefaultTechnickRecoverySeconds : DefaultItemRecoverySeconds;
                if (binding.contactWindowSeconds <= 0f) binding.contactWindowSeconds = DefaultContactWindowSeconds;
                if (IsLegacyGeneratedEmptyPresentation(binding.clipPath)) binding.clipPath = string.Empty;
                SaveAnimationProfile();
                if (fxController != null)
                {
                    if (kind == OutgoingContactKind.Technick)
                        RebuildActionAnimationLayer(ActionAnimationKind.Technick, "SoY_TechnickType", TechnickCastLayer, animationProfile.technickAnimations);
                    else
                        RebuildActionAnimationLayer(ActionAnimationKind.Item, "SoY_ItemType", ItemUseLayer, animationProfile.itemAnimations);
                }
                string source;
                return ResolveActionPresentationClip(animationKind, binding, out source);
            }

            return null;
        }

        private SpellDefinition[] GetVisibleSpellDefinitions()
        {
            return GetSpellsForSchool(spellSchool)
                .Where(entry => string.IsNullOrWhiteSpace(spellSearch) ||
                    entry.Name.IndexOf(spellSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(spellSearch.Trim()))
                .ToArray();
        }

        private ActionDefinition[] GetVisibleTechnickDefinitions()
        {
            return TechnickDefinitions
                .Where(entry => string.IsNullOrWhiteSpace(technickSearch) ||
                    entry.Name.IndexOf(technickSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(technickSearch.Trim()))
                .ToArray();
        }

        private ActionDefinition[] GetVisibleItemDefinitions()
        {
            return ItemDefinitions
                .Where(entry => string.IsNullOrWhiteSpace(itemSearch) ||
                    entry.Name.IndexOf(itemSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    entry.Id.ToString().Contains(itemSearch.Trim()))
                .ToArray();
        }

        private bool TryGetSelectedSpell(out SpellDefinition spell)
        {
            var rows = GetVisibleSpellDefinitions();
            if (rows.Length == 0)
            {
                spell = default(SpellDefinition);
                return false;
            }

            spellSelectionIndex = Mathf.Clamp(spellSelectionIndex, 0, rows.Length - 1);
            spell = rows[spellSelectionIndex];
            return true;
        }

        private bool TryGetSelectedTechnick(out ActionDefinition technick)
        {
            var rows = GetVisibleTechnickDefinitions();
            if (rows.Length == 0)
            {
                technick = default(ActionDefinition);
                return false;
            }

            technickSelectionIndex = Mathf.Clamp(technickSelectionIndex, 0, rows.Length - 1);
            technick = rows[technickSelectionIndex];
            return true;
        }

        private bool TryGetSelectedItem(out ActionDefinition item)
        {
            var rows = GetVisibleItemDefinitions();
            if (rows.Length == 0)
            {
                item = default(ActionDefinition);
                return false;
            }

            itemSelectionIndex = Mathf.Clamp(itemSelectionIndex, 0, rows.Length - 1);
            item = rows[itemSelectionIndex];
            return true;
        }

        private void GetCurrentRaycastActionGate(out string parameter, out int value, out bool integerGate)
        {
            parameter = RaycastFireParameter;
            value = 1;
            integerGate = false;

            SpellDefinition spell;
            ActionDefinition action;
            if (outgoingKind == OutgoingContactKind.Spell && TryGetSelectedSpell(out spell))
            {
                parameter = "SoY_SpellType";
                value = spell.Id;
                integerGate = true;
            }
            else if (outgoingKind == OutgoingContactKind.Technick && TryGetSelectedTechnick(out action))
            {
                parameter = "SoY_TechnickType";
                value = action.Id;
                integerGate = true;
            }
            else if (outgoingKind == OutgoingContactKind.Item && TryGetSelectedItem(out action))
            {
                parameter = "SoY_ItemType";
                value = action.Id;
                integerGate = true;
            }
        }

        private bool CurrentRaycastUsesWorldGroundPlacement()
        {
            if (outgoingKind == OutgoingContactKind.Spell)
                return true;
            if (outgoingKind == OutgoingContactKind.Technick)
                return technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement;
            return false;
        }

        private void CreateRaycastDeliveryForCurrentAction()
        {
            MigrateLegacyRaycastAnimationAssetsToFlatFolder();

            if (outgoingKind == OutgoingContactKind.Spell)
            {
                CreateSpellGroundPlacementRaycastDelivery();
                return;
            }

            if (outgoingKind == OutgoingContactKind.Technick &&
                technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement)
            {
                CreateTechnickGroundPlacementRaycastDelivery();
                return;
            }

            CreateDirectRaycastDelivery();
        }

        private GameObject GetOrCreateRaycastRoot()
        {
            if (avatarRoot == null)
                return null;
            var existing = avatarRoot.transform.Cast<Transform>().FirstOrDefault(x => x.name == RaycastRootName);
            return existing != null ? existing.gameObject : CreateContactChild(avatarRoot, RaycastRootName, true);
        }

        private static bool IsInsideManagedRaycastHierarchy(GameObject target)
        {
            if (target == null)
                return false;
            var current = target.transform;
            while (current != null)
            {
                if (current.name == RaycastRootName || current.name == LegacyRaycastRootName)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private bool ValidateRaycastOrigin(GameObject originTarget)
        {
            if (originTarget == null)
                return false;
            if (!IsInsideManagedRaycastHierarchy(originTarget))
                return true;

            EditorUtility.DisplayDialog(
                "Invalid Raycast Origin",
                "Select a hand, weapon muzzle, staff/focus, or another avatar object outside the generated '" +
                RaycastRootName +
                "' hierarchy. VRChat warns against using a Raycast Result Transform or one of its parents as the Raycast origin.",
                "OK");
            return false;
        }

        private int DisableManagedDirectRaycastDelivery(string safePrefix)
        {
            if (avatarRoot == null || string.IsNullOrWhiteSpace(safePrefix))
                return 0;

            var names = new HashSet<string>(StringComparer.Ordinal)
            {
                "[SoY Direct Raycast Origin] " + safePrefix,
                "[SoY Direct Raycast Result] " + safePrefix
            };

            var disabled = 0;
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == null || !names.Contains(transform.name) || !transform.gameObject.activeSelf)
                    continue;
                Undo.RecordObject(transform.gameObject, "Disable Stories Direct Raycast While Switching Mode");
                transform.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                disabled++;
            }

            if (disabled > 0)
                Log("Disabled " + disabled + " matching Direct Impact object(s) while switching '" + safePrefix + "' to World / Ground Placement.");
            return disabled;
        }

        private int DisableManagedTechnickGroundPlacement(string safePrefix)
        {
            if (avatarRoot == null || string.IsNullOrWhiteSpace(safePrefix))
                return 0;

            var targetName = TechnickWorldDropPrefix + safePrefix;
            var disabled = 0;
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == null || transform.name != targetName || !transform.gameObject.activeSelf)
                    continue;
                Undo.RecordObject(transform.gameObject, "Disable Stories Technick Ground Placement While Switching Mode");
                transform.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                disabled++;
            }

            if (disabled > 0)
                Log("Disabled " + disabled + " matching Technick World Drop carrier(s) while switching '" + safePrefix + "' to Direct Impact.");
            return disabled;
        }

        private int DisableLegacyRaycastDelivery(string safePrefix)
        {
            if (avatarRoot == null || string.IsNullOrWhiteSpace(safePrefix))
                return 0;

            var legacyNames = new HashSet<string>(StringComparer.Ordinal)
            {
                LegacyRaycastOriginPrefix + safePrefix,
                LegacyRaycastResultPrefix + safePrefix
            };

            var disabled = 0;
            foreach (var transform in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == null || !legacyNames.Contains(transform.name))
                    continue;

                // These exact names were generated by Stories OSC v0.5.9 TB3 and earlier.
                // Disable rather than destroy so the migration remains reversible through Undo
                // and never deletes user-authored children/materials placed beneath the result.
                if (transform.gameObject.activeSelf)
                {
                    Undo.RecordObject(transform.gameObject, "Disable Legacy Stories Raycast");
                    transform.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                    disabled++;
                }
            }

            if (disabled > 0)
                Log("Disabled " + disabled + " legacy TB3 Raycast object(s) for '" + safePrefix + "' before creating the v0.5.10 delivery.");

            return disabled;
        }

        private int CountLegacyRaycastObjects()
        {
            if (avatarRoot == null)
                return 0;
            return avatarRoot.GetComponentsInChildren<Transform>(true)
                .Count(transform => transform != null &&
                    (transform.name.StartsWith(LegacyRaycastOriginPrefix, StringComparison.Ordinal) ||
                     transform.name.StartsWith(LegacyRaycastResultPrefix, StringComparison.Ordinal)));
        }

        private void ConfigureRaycastCommon(
            Component component,
            Vector3 direction,
            float distance,
            Transform result,
            string parameterPrefix,
            RaycastCollisionTarget collisionTarget,
            bool applyRotation,
            bool alignPositiveY)
        {
            if (component == null)
                return;
            Undo.RecordObject(component, "Configure Stories VRCRaycast");
            SetVector3Member(component,
                direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward,
                "raycastDirection", "RaycastDirection", "direction", "Direction");
            SetFloatMember(component, distance, "distance", "Distance", "maxDistance", "MaxDistance");
            SetBoolMember(component, false, "applyTransformScale", "ApplyTransformScale");
            SetBoolMember(component, applyRotation, "applyRotation", "ApplyRotation");
            SetTransformMember(component, result, "resultTransform", "ResultTransform");
            SetStringMember(component, parameterPrefix, "parameter", "Parameter", "parameterName", "ParameterName");
            ConfigureRaycastCollision(component, collisionTarget);
            if (applyRotation && alignPositiveY)
                SetEnumMemberByKeywords(component, new[] { "positive", "y" }, "alignmentAxis", "AlignmentAxis");
            SetEnumMemberByKeywords(component, new[] { "start" }, "behaviorOnMiss", "BehaviorOnMiss", "positioningOnMiss", "PositioningOnMiss");
            FinishContact(component);
        }

        private void ConfigureRaycastCollision(Component component, RaycastCollisionTarget collisionTarget)
        {
            if (component == null)
                return;

            if (collisionTarget == RaycastCollisionTarget.RemotePlayersOnly)
            {
                // Preferred path: Hit Custom Layers with Player (9) only.
                // VRChat reserves PlayerLocal (10) for the local avatar, so excluding it prevents
                // the ray from immediately colliding with the caster when its origin is near the body.
                SetEnumMemberByKeywords(component, new[] { "custom" }, "collisionMode", "CollisionMode");
                var customMaskApplied = SetLayerMaskMember(
                    component,
                    1 << 9,
                    "customCollisionLayers", "CustomCollisionLayers",
                    "customLayers", "CustomLayers",
                    "customLayerMask", "CustomLayerMask",
                    "collisionLayers", "CollisionLayers",
                    "collisionLayerMask", "CollisionLayerMask",
                    "layerMask", "LayerMask");

                if (!customMaskApplied)
                {
                    // SDK internals can move between releases. Never leave a VRCRaycast in
                    // Hit Custom Layers with an unknown/empty mask: fall back to Hit Players.
                    // This includes PlayerLocal, so the UI warns that self-hit protection is degraded,
                    // but the ray still functions instead of silently targeting nothing.
                    SetEnumMemberByKeywords(component, new[] { "player" }, "collisionMode", "CollisionMode");
                    var warning = "Raycast warning: this SDK build did not expose the custom layer-mask member. Fell back to Hit Players; update the VRChat Avatars SDK for remote-player-only self-hit protection.";
                    operationLog.Insert(0, warning);
                    Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] " + warning);
                }
            }
            else if (collisionTarget == RaycastCollisionTarget.Worlds)
            {
                SetEnumMemberByKeywords(component, new[] { "world" }, "collisionMode", "CollisionMode");
            }
            else
            {
                SetEnumMemberByKeywords(component, new[] { "world", "player" }, "collisionMode", "CollisionMode");
            }
        }

        private void EnsureRaycastAnimatorParameters(string prefix)
        {
            if (fxController == null)
                return;
            EnsureAnimatorParameter(fxController, prefix + "_Hit", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, prefix + "_Ratio", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(fxController, prefix + "_Distance", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(fxController, "IsLocal", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, RaycastFireParameter, AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, RaycastTargetingParameter, AnimatorControllerParameterType.Bool);
        }

        private GameObject GetOrCreateSharedSpellAimOrigin(GameObject originTarget)
        {
            if (avatarRoot == null || originTarget == null)
                return null;

            var matches = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t != null && t.name == "[SoY Spell Aim Origin]")
                .ToArray();

            GameObject aimOrigin = null;
            if (matches.Length > 0)
            {
                aimOrigin = matches[0].gameObject;
                if (!aimOrigin.activeSelf)
                {
                    Undo.RecordObject(aimOrigin, "Enable Stories Shared Spell Aim Origin");
                    aimOrigin.SetActive(true);
                }

                if (aimOrigin.transform.parent != originTarget.transform)
                {
                    Undo.SetTransformParent(
                        aimOrigin.transform,
                        originTarget.transform,
                        "Move Stories Shared Spell Aim Origin");
                    Undo.RecordObject(aimOrigin.transform, "Reset Stories Shared Spell Aim Origin");
                    aimOrigin.transform.localPosition = Vector3.zero;
                    aimOrigin.transform.localRotation = Quaternion.identity;
                    aimOrigin.transform.localScale = Vector3.one;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(aimOrigin.transform);
                    Log("Moved the shared Spell Aim Origin to '" + originTarget.name + "'. All spell ground placement now uses this single aim source.");
                }

                // Older/test-build repairs may have produced more than one shared writer.
                // Disable only exact Stories-managed duplicates so a single parameter prefix
                // has a single VRCRaycast authority.
                for (var i = 1; i < matches.Length; i++)
                {
                    var duplicate = matches[i] != null ? matches[i].gameObject : null;
                    if (duplicate == null || !duplicate.activeSelf)
                        continue;
                    Undo.RecordObject(duplicate, "Disable Duplicate Stories Spell Aim Origin");
                    duplicate.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(duplicate);
                    Log("Disabled duplicate managed Spell Aim Origin '" + GetHierarchyPath(duplicate.transform) + "'.");
                }
            }
            else
            {
                aimOrigin = CreateContactChild(originTarget, "[SoY Spell Aim Origin]", true);
            }

            return aimOrigin;
        }

        private GameObject GetOrCreateSharedTechnickAimOrigin(GameObject originTarget)
        {
            if (avatarRoot == null || originTarget == null)
                return null;

            var matches = avatarRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t != null && t.name == "[SoY Technick Aim Origin]")
                .ToArray();

            GameObject aimOrigin = null;
            if (matches.Length > 0)
            {
                aimOrigin = matches[0].gameObject;
                if (!aimOrigin.activeSelf)
                {
                    Undo.RecordObject(aimOrigin, "Enable Stories Shared Technick Aim Origin");
                    aimOrigin.SetActive(true);
                }

                if (aimOrigin.transform.parent != originTarget.transform)
                {
                    Undo.SetTransformParent(
                        aimOrigin.transform,
                        originTarget.transform,
                        "Move Stories Shared Technick Aim Origin");
                    Undo.RecordObject(aimOrigin.transform, "Reset Stories Shared Technick Aim Origin");
                    aimOrigin.transform.localPosition = Vector3.zero;
                    aimOrigin.transform.localRotation = Quaternion.identity;
                    aimOrigin.transform.localScale = Vector3.one;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(aimOrigin.transform);
                    Log("Moved the shared Technick Aim Origin to '" + originTarget.name + "'. All Technick ground placement now uses this single aim source.");
                }

                for (var i = 1; i < matches.Length; i++)
                {
                    var duplicate = matches[i] != null ? matches[i].gameObject : null;
                    if (duplicate == null || !duplicate.activeSelf)
                        continue;
                    Undo.RecordObject(duplicate, "Disable Duplicate Stories Technick Aim Origin");
                    duplicate.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(duplicate);
                    Log("Disabled duplicate managed Technick Aim Origin '" + GetHierarchyPath(duplicate.transform) + "'.");
                }
            }
            else
            {
                aimOrigin = CreateContactChild(originTarget, "[SoY Technick Aim Origin]", true);
            }

            return aimOrigin;
        }

        private GameObject CreateLocalTargetingIcon(GameObject parent, string name)
        {
            var icon = CreateContactChild(parent, name, false);
            if (icon.transform.childCount > 0)
                return icon;

            // A material-less primitive uses Unity's default renderer material. Keeping the
            // icon as simple geometry avoids introducing shader/package dependencies.
            CreateTargetingIconBar(icon, "Horizontal", new Vector3(0.14f, 0.012f, 0.012f), Vector3.zero);
            CreateTargetingIconBar(icon, "Vertical", new Vector3(0.012f, 0.14f, 0.012f), Vector3.zero);
            CreateTargetingIconBar(icon, "Left Tick", new Vector3(0.05f, 0.012f, 0.012f), new Vector3(-0.115f, 0f, 0f));
            CreateTargetingIconBar(icon, "Right Tick", new Vector3(0.05f, 0.012f, 0.012f), new Vector3(0.115f, 0f, 0f));
            return icon;
        }

        private static void CreateTargetingIconBar(GameObject parent, string name, Vector3 scale, Vector3 localPosition)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            Undo.RegisterCreatedObjectUndo(bar, "Create Stories Local Targeting Icon");
            bar.transform.SetParent(parent.transform, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = scale;
            var collider = bar.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);
        }

        private void RemoveMismatchedManagedRaycastActionChildren(
            GameObject actionHost,
            OutgoingContactKind kind,
            int expectedId)
        {
            if (actionHost == null || expectedId <= 0)
                return;

            string prefix;
            switch (kind)
            {
                case OutgoingContactKind.Spell: prefix = "Stories Spell - "; break;
                case OutgoingContactKind.Technick: prefix = "Stories Technick - "; break;
                case OutgoingContactKind.Item: prefix = "Stories Item - "; break;
                default: return;
            }

            var expectedPrefix = prefix + expectedId + " ";
            var stale = actionHost.transform.Cast<Transform>()
                .Where(child => child != null &&
                    child.name.StartsWith(prefix, StringComparison.Ordinal) &&
                    !child.name.StartsWith(expectedPrefix, StringComparison.Ordinal))
                .Select(child => child.gameObject)
                .ToArray();

            foreach (var child in stale)
            {
                Log("Removed stale managed Raycast action child '" + child.name +
                    "' while repairing expected action ID " + expectedId + ".");
                Undo.DestroyObjectImmediate(child);
            }
        }

        private void CreateDirectRaycastDelivery()
        {
            var originTarget = explicitTarget != null ? explicitTarget : Selection.activeGameObject;
            var raycastType = FindRaycastType();
            if (originTarget == null || avatarRoot == null || raycastType == null)
                return;
            if (!ValidateRaycastOrigin(originTarget))
                return;
            if (fxController != null && !EnsureSafeFxCopy(true))
                return;

            var sourcePrefix = raycastUseCustomPrefix ? raycastParameterPrefix : CurrentRaycastSuggestedPrefix();
            var safePrefix = RegexSafeParameter(sourcePrefix);
            DisableLegacyRaycastDelivery(safePrefix);
            if (outgoingKind == OutgoingContactKind.Technick)
                DisableManagedTechnickGroundPlacement(safePrefix);
            string actionGateParameter;
            int actionGateValue;
            bool actionGateIsInteger;
            GetCurrentRaycastActionGate(out actionGateParameter, out actionGateValue, out actionGateIsInteger);

            var origin = CreateContactChild(originTarget, "[SoY Direct Raycast Origin] " + safePrefix, true);
            ApplyRaycastOriginAttachment(origin);
            var resultRoot = GetOrCreateRaycastRoot();
            var result = CreateContactChild(resultRoot, "[SoY Direct Raycast Result] " + safePrefix, true);
            result.transform.localPosition = Vector3.zero;

            var component = origin.GetComponent(raycastType) as Component ?? Undo.AddComponent(origin, raycastType) as Component;
            ConfigureRaycastCommon(
                component,
                raycastDirection,
                raycastDistance,
                result.transform,
                safePrefix,
                raycastCollisionTarget,
                raycastApplyRotation,
                true);
            EnsureRaycastAnimatorParameters(safePrefix);
            EnsureLocalRaycastTargetingExpressionParameter();

            var targetIcon = CreateLocalTargetingIcon(result, "[LOCAL ONLY] Targeting Icon");
            var actionHost = CreateContactChild(result, "[SoY Raycast Action] " + safePrefix, false);
            RemoveMismatchedManagedRaycastActionChildren(actionHost, outgoingKind, actionGateValue);

            var oldTarget = explicitTarget;
            explicitTarget = actionHost;
            var oldSpellRadius = spellRadius; var oldTechnickRadius = technickRadius; var oldItemRadius = itemRadius; var oldAttackRadius = attackRadius;
            var oldAttackEnabled = attackStartsEnabled; var oldSpellEnabled = spellStartsEnabled; var oldTechnickEnabled = technickStartsEnabled; var oldItemEnabled = itemStartsEnabled; var oldDebuffEnabled = debuffStartsEnabled;
            spellRadius = technickRadius = itemRadius = attackRadius = raycastImpactRadius;
            attackStartsEnabled = spellStartsEnabled = technickStartsEnabled = itemStartsEnabled = debuffStartsEnabled = true;
            suppressContactAttachment = true;
            try
            {
                switch (outgoingKind)
                {
                    case OutgoingContactKind.Attack: CreateAttackSenders(); break;
                    case OutgoingContactKind.Technick: CreateTechnickSenders(); break;
                    case OutgoingContactKind.Item: CreateItemSenders(); break;
                    case OutgoingContactKind.Debuff: CreateDebuffSenders(); break;
                    default:
                        EditorUtility.DisplayDialog("Raycast Delivery", "Direct Impact supports Attack, Technick, Item, or Debuff. Spells use World / Ground Placement; Technicks may use either mode.", "OK");
                        break;
                }
            }
            finally
            {
                spellRadius = oldSpellRadius; technickRadius = oldTechnickRadius; itemRadius = oldItemRadius; attackRadius = oldAttackRadius;
                attackStartsEnabled = oldAttackEnabled; spellStartsEnabled = oldSpellEnabled; technickStartsEnabled = oldTechnickEnabled; itemStartsEnabled = oldItemEnabled; debuffStartsEnabled = oldDebuffEnabled;
                suppressContactAttachment = false;
                explicitTarget = oldTarget;
            }

            RebuildRaycastGateLayer(safePrefix, actionHost, safePrefix, actionGateParameter, actionGateValue, actionGateIsInteger, null);
            RebuildRaycastTargetingLayer(safePrefix, targetIcon, safePrefix, actionGateParameter, actionGateValue, actionGateIsInteger, false);

            if (actionGateIsInteger)
            {
                ActionDefinition selectorAction;
                if (outgoingKind == OutgoingContactKind.Technick && TryGetSelectedTechnick(out selectorAction))
                    EnsureRaycastSelectorAnimationBinding(OutgoingContactKind.Technick, selectorAction.Id, selectorAction.Name, actionHost);
                else if (outgoingKind == OutgoingContactKind.Item && TryGetSelectedItem(out selectorAction))
                    EnsureRaycastSelectorAnimationBinding(OutgoingContactKind.Item, selectorAction.Id, selectorAction.Name, actionHost);
            }

            if (raycastCreateLineRenderer && origin.GetComponent<LineRenderer>() == null)
            {
                var line = Undo.AddComponent<LineRenderer>(origin);
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.widthMultiplier = 0.01f;
                line.enabled = false;
            }

            Selection.activeGameObject = origin;
            Log("Created/repaired Direct Impact VRCRaycast '" + safePrefix + "'.");
        }

        private void CreateSpellGroundPlacementRaycastDelivery()
        {
            var originTarget = explicitTarget != null ? explicitTarget : Selection.activeGameObject;
            var raycastType = FindRaycastType();
            if (originTarget == null || avatarRoot == null || raycastType == null)
                return;
            if (!ValidateRaycastOrigin(originTarget))
                return;
            if (fxController != null && !EnsureSafeFxCopy(true))
                return;

            string actionGateParameter;
            int spellId;
            bool integerGate;
            GetCurrentRaycastActionGate(out actionGateParameter, out spellId, out integerGate);
            SpellDefinition selectedSpell;
            if (!integerGate || actionGateParameter != "SoY_SpellType" || spellId <= 0 ||
                !TryGetSelectedSpell(out selectedSpell) || selectedSpell.Id != spellId)
            {
                EditorUtility.DisplayDialog("Spell Ground Placement", "Select a valid Spell before creating the ground-placement raycast.", "OK");
                return;
            }

            var sourcePrefix = raycastUseCustomPrefix ? raycastParameterPrefix : CurrentRaycastSuggestedPrefix();
            var safePrefix = RegexSafeParameter(sourcePrefix);
            DisableLegacyRaycastDelivery(safePrefix);
            var resultRoot = GetOrCreateRaycastRoot();
            var rig = CreateContactChild(resultRoot, SpellPlacementRigName, true);

            // One reusable player-targeting ray for the whole spell placement system.
            // Repairing from a different hand/focus moves this exact Stories-managed origin
            // instead of creating a second ray that writes to the same target parameters.
            var aimOrigin = GetOrCreateSharedSpellAimOrigin(originTarget);
            if (aimOrigin == null)
                return;
            ApplyRaycastOriginAttachment(aimOrigin);
            var targetAnchor = CreateContactChild(rig, "[SoY Spell Target Anchor]", true);
            targetAnchor.transform.localPosition = Vector3.zero;
            var aimRay = aimOrigin.GetComponent(raycastType) as Component ?? Undo.AddComponent(aimOrigin, raycastType) as Component;
            ConfigureRaycastCommon(
                aimRay,
                raycastDirection,
                raycastDistance,
                targetAnchor.transform,
                SpellTargetPrefix,
                RaycastCollisionTarget.RemotePlayersOnly,
                false,
                false);
            EnsureRaycastAnimatorParameters(SpellTargetPrefix);
            EnsureLocalRaycastTargetingExpressionParameter();

            // The second ray starts above the tracked player hit and always casts straight down.
            // It collides with world surfaces only, then rotates the result so local +Y follows
            // the surface normal. A generated child turns conventional +Z-facing VFX downward.
            var groundOrigin = CreateContactChild(targetAnchor, "[SoY Downward Ground Probe]", true);
            groundOrigin.transform.localPosition = Vector3.up * SpellGroundProbeHeight;
            groundOrigin.transform.localRotation = Quaternion.identity;
            var groundResult = CreateContactChild(rig, "[SoY Spell Ground Result]", true);
            groundResult.transform.localPosition = Vector3.zero;
            var groundRay = groundOrigin.GetComponent(raycastType) as Component ?? Undo.AddComponent(groundOrigin, raycastType) as Component;
            ConfigureRaycastCommon(
                groundRay,
                Vector3.down,
                SpellGroundProbeDistance,
                groundResult.transform,
                SpellGroundPrefix,
                RaycastCollisionTarget.Worlds,
                true,
                true);
            EnsureRaycastAnimatorParameters(SpellGroundPrefix);

            var targetIcon = CreateLocalTargetingIcon(targetAnchor, "[LOCAL ONLY] Spell Targeting Icon");

            // TB5: the visible/contact payload lives under a dedicated world-drop carrier.
            // While unfrozen, the carrier follows the native Raycast ground result through a
            // VRCParentConstraint. Once the spell is fired and placement is valid, the FX
            // Animator toggles FreezeToWorld so the spell remains fixed in world space until
            // the menu button is released (or a future toggle is disabled).
            var worldDropCarrier = CreateContactChild(rig, SpellWorldDropPrefix + safePrefix, true);
            worldDropCarrier.transform.localPosition = Vector3.zero;
            worldDropCarrier.transform.localRotation = Quaternion.identity;
            worldDropCarrier.transform.localScale = Vector3.one;
            var worldDropConstraint = ConfigureWorldDropParentConstraint(worldDropCarrier, groundResult.transform);
            if (worldDropConstraint == null)
            {
                EditorUtility.DisplayDialog(
                    "Spell Ground Placement",
                    "TB10 could not configure the VRChat Parent Constraint required for World Drop. Check the Stories tool log for the direct SDK Sources API diagnostic, then run Create / Repair again.",
                    "OK");
                return;
            }

            var actionName = "[SoY Spell Placement] " + safePrefix;
            var legacyAction = groundResult.transform.Cast<Transform>().FirstOrDefault(child => child.name == actionName);
            if (legacyAction != null && legacyAction.parent != worldDropCarrier.transform)
            {
                Undo.SetTransformParent(legacyAction, worldDropCarrier.transform, "Migrate Stories Spell Placement To World Drop");
                Undo.RecordObject(legacyAction, "Reset Stories Spell Placement World Drop Offset");
                legacyAction.localPosition = Vector3.zero;
                legacyAction.localRotation = Quaternion.identity;
                legacyAction.localScale = Vector3.one;
                Log("Migrated the existing TB4 spell placement host into the TB5 World Drop carrier without replacing its children.");
            }

            var actionHost = CreateContactChild(worldDropCarrier, actionName, false);
            RemoveMismatchedManagedRaycastActionChildren(actionHost, OutgoingContactKind.Spell, selectedSpell.Id);
            var effectHolder = CreateContactChild(actionHost, "FX — Faces Down (Place Particle Here)", true);
            effectHolder.transform.localPosition = Vector3.zero;
            effectHolder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            effectHolder.transform.localScale = Vector3.one;

            var oldTarget = explicitTarget;
            explicitTarget = actionHost;
            var oldSpellRadius = spellRadius;
            var oldSpellEnabled = spellStartsEnabled;
            spellRadius = raycastImpactRadius;
            spellStartsEnabled = true;
            suppressContactAttachment = true;
            try
            {
                CreateSpellSenders();
            }
            finally
            {
                spellRadius = oldSpellRadius;
                spellStartsEnabled = oldSpellEnabled;
                suppressContactAttachment = false;
                explicitTarget = oldTarget;
            }

            RebuildRaycastGateLayer(
                safePrefix,
                actionHost,
                SpellGroundPrefix,
                actionGateParameter,
                spellId,
                true,
                SpellTargetPrefix,
                worldDropConstraint);
            RebuildRaycastTargetingLayer(
                "SpellGroundPlacement",
                targetIcon,
                SpellTargetPrefix,
                "SoY_SpellType",
                0,
                true,
                true,
                SpellGroundPrefix);

            var castClip = EnsureRaycastSelectorAnimationBinding(OutgoingContactKind.Spell, selectedSpell.Id, selectedSpell.Name, actionHost);
            if (castClip != null)
                Log("Raycast cast animation is bound to the same Spell menu parameter. Edit: " + AssetDatabase.GetAssetPath(castClip));

            Selection.activeGameObject = aimOrigin;
            Log("Created/repaired Spell Ground Placement for spell ID " + spellId + ". The Spell button arms placement, then World Drops the spell at the valid floor hit and keeps it frozen until the button/toggle releases. The targeting icon only appears when both player + floor hits are valid.");
        }

        private void CreateTechnickGroundPlacementRaycastDelivery()
        {
            var originTarget = explicitTarget != null ? explicitTarget : Selection.activeGameObject;
            var raycastType = FindRaycastType();
            if (originTarget == null || avatarRoot == null || raycastType == null)
                return;
            if (!ValidateRaycastOrigin(originTarget))
                return;
            if (fxController != null && !EnsureSafeFxCopy(true))
                return;

            string actionGateParameter;
            int technickId;
            bool integerGate;
            GetCurrentRaycastActionGate(out actionGateParameter, out technickId, out integerGate);
            ActionDefinition selectedTechnick;
            if (!integerGate || actionGateParameter != "SoY_TechnickType" || technickId <= 0 ||
                !TryGetSelectedTechnick(out selectedTechnick) || selectedTechnick.Id != technickId)
            {
                EditorUtility.DisplayDialog(
                    "Technick World / Ground Placement",
                    "Select a valid Technick before creating the world/ground-placement Raycast.",
                    "OK");
                return;
            }

            var sourcePrefix = raycastUseCustomPrefix ? raycastParameterPrefix : CurrentRaycastSuggestedPrefix();
            var safePrefix = RegexSafeParameter(sourcePrefix);
            DisableLegacyRaycastDelivery(safePrefix);
            DisableManagedDirectRaycastDelivery(safePrefix);
            var resultRoot = GetOrCreateRaycastRoot();
            var rig = CreateContactChild(resultRoot, TechnickPlacementRigName, true);

            // TB10: Technicks can now use the same two-stage player -> floor targeting
            // architecture as Spells while retaining Direct Impact as a separate mode.
            // One shared aim writer is used by all placed Technicks so each Technick
            // does not consume its own pair of VRCRaycast components.
            var aimOrigin = GetOrCreateSharedTechnickAimOrigin(originTarget);
            if (aimOrigin == null)
                return;
            ApplyRaycastOriginAttachment(aimOrigin);

            var targetAnchor = CreateContactChild(rig, "[SoY Technick Target Anchor]", true);
            targetAnchor.transform.localPosition = Vector3.zero;
            targetAnchor.transform.localRotation = Quaternion.identity;
            targetAnchor.transform.localScale = Vector3.one;

            var aimRay = aimOrigin.GetComponent(raycastType) as Component ??
                         Undo.AddComponent(aimOrigin, raycastType) as Component;
            ConfigureRaycastCommon(
                aimRay,
                raycastDirection,
                raycastDistance,
                targetAnchor.transform,
                TechnickTargetPrefix,
                RaycastCollisionTarget.RemotePlayersOnly,
                false,
                false);
            EnsureRaycastAnimatorParameters(TechnickTargetPrefix);
            EnsureLocalRaycastTargetingExpressionParameter();

            var groundOrigin = CreateContactChild(targetAnchor, "[SoY Downward Ground Probe]", true);
            groundOrigin.transform.localPosition = Vector3.up * SpellGroundProbeHeight;
            groundOrigin.transform.localRotation = Quaternion.identity;
            groundOrigin.transform.localScale = Vector3.one;

            var groundResult = CreateContactChild(rig, "[SoY Technick Ground Result]", true);
            groundResult.transform.localPosition = Vector3.zero;
            groundResult.transform.localRotation = Quaternion.identity;
            groundResult.transform.localScale = Vector3.one;

            var groundRay = groundOrigin.GetComponent(raycastType) as Component ??
                            Undo.AddComponent(groundOrigin, raycastType) as Component;
            ConfigureRaycastCommon(
                groundRay,
                Vector3.down,
                SpellGroundProbeDistance,
                groundResult.transform,
                TechnickGroundPrefix,
                RaycastCollisionTarget.Worlds,
                true,
                true);
            EnsureRaycastAnimatorParameters(TechnickGroundPrefix);

            var targetIcon = CreateLocalTargetingIcon(
                targetAnchor,
                "[LOCAL ONLY] Technick Targeting Icon");

            var worldDropCarrier = CreateContactChild(
                rig,
                TechnickWorldDropPrefix + safePrefix,
                true);
            worldDropCarrier.transform.localPosition = Vector3.zero;
            worldDropCarrier.transform.localRotation = Quaternion.identity;
            worldDropCarrier.transform.localScale = Vector3.one;

            var worldDropConstraint = ConfigureWorldDropParentConstraint(
                worldDropCarrier,
                groundResult.transform);
            if (worldDropConstraint == null)
            {
                EditorUtility.DisplayDialog(
                    "Technick World / Ground Placement",
                    "TB10 could not configure the VRChat Parent Constraint required for World Drop. Check the Stories tool log, then run Create / Repair again.",
                    "OK");
                return;
            }

            var actionName = "[SoY Technick Placement] " + safePrefix;
            var actionHost = CreateContactChild(worldDropCarrier, actionName, false);
            RemoveMismatchedManagedRaycastActionChildren(
                actionHost,
                OutgoingContactKind.Technick,
                selectedTechnick.Id);

            var effectHolder = CreateContactChild(
                actionHost,
                "FX — Faces Down (Place Particle Here)",
                true);
            effectHolder.transform.localPosition = Vector3.zero;
            effectHolder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            effectHolder.transform.localScale = Vector3.one;

            var oldTarget = explicitTarget;
            explicitTarget = actionHost;
            var oldTechnickRadius = technickRadius;
            var oldTechnickEnabled = technickStartsEnabled;
            technickRadius = raycastImpactRadius;
            technickStartsEnabled = true;
            suppressContactAttachment = true;
            try
            {
                CreateTechnickSenders();
            }
            finally
            {
                technickRadius = oldTechnickRadius;
                technickStartsEnabled = oldTechnickEnabled;
                suppressContactAttachment = false;
                explicitTarget = oldTarget;
            }

            RebuildRaycastGateLayer(
                safePrefix,
                actionHost,
                TechnickGroundPrefix,
                actionGateParameter,
                technickId,
                true,
                TechnickTargetPrefix,
                worldDropConstraint);

            RebuildRaycastTargetingLayer(
                "TechnickGroundPlacement",
                targetIcon,
                TechnickTargetPrefix,
                "SoY_TechnickType",
                0,
                true,
                true,
                TechnickGroundPrefix);

            var castClip = EnsureRaycastSelectorAnimationBinding(
                OutgoingContactKind.Technick,
                selectedTechnick.Id,
                selectedTechnick.Name,
                actionHost);
            if (castClip != null)
                Log("Technick World / Ground Raycast animation is bound to the same Technick menu parameter. Edit: " + AssetDatabase.GetAssetPath(castClip));

            Selection.activeGameObject = aimOrigin;
            Log(
                "Created/repaired World / Ground Placement for Technick ID " +
                technickId +
                ". The Technick button arms placement, then World Drops the Technick at the valid floor hit and keeps it frozen until the button/toggle releases.");
        }

        private string RegexSafeParameter(string value)
        {
            var cleaned = new string((value ?? "SoY_Raycast").Where(x => char.IsLetterOrDigit(x) || x == '_').ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "SoY_Raycast" : cleaned;
        }

        private float GetRaycastRecoverySeconds(string actionParameter, int actionValue, bool integerGate)
        {
            if (integerGate && actionValue > 0)
            {
                if (actionParameter == "SoY_SpellType") return GetRecoverySeconds(ActionAnimationKind.Spell, actionValue);
                if (actionParameter == "SoY_TechnickType") return GetRecoverySeconds(ActionAnimationKind.Technick, actionValue);
                if (actionParameter == "SoY_ItemType") return GetRecoverySeconds(ActionAnimationKind.Item, actionValue);
            }
            if (actionParameter == RaycastFireParameter)
                return DefaultAttackRecoverySeconds;
            return DefaultRaycastRecoverySeconds;
        }

        private static string ApprovedParameterForSelector(string actionParameter)
        {
            if (actionParameter == "SoY_SpellType") return SpellApprovedParameter;
            if (actionParameter == "SoY_TechnickType") return TechnickApprovedParameter;
            if (actionParameter == "SoY_ItemType") return ItemApprovedParameter;
            return string.Empty;
        }

        private void RebuildRaycastGateLayer(
            string layerKey,
            GameObject actionHost,
            string hitPrefix,
            string actionParameter,
            int actionValue,
            bool integerGate,
            string additionalRequiredHitPrefix,
            Component worldDropConstraint = null)
        {
            if (fxController == null || actionHost == null)
                return;

            EnsureAnimatorParameter(fxController, RaycastApprovedParameter, AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);
            var categoryApprovedParameter = ApprovedParameterForSelector(actionParameter);
            if (!string.IsNullOrWhiteSpace(categoryApprovedParameter))
                EnsureAnimatorParameter(fxController, categoryApprovedParameter, AnimatorControllerParameterType.Bool);

            var recoverySeconds = Mathf.Max(0.1f, GetRaycastRecoverySeconds(actionParameter, actionValue, integerGate));
            var layerName = RaycastLayerPrefix + layerKey;
            RemoveLayerByName(fxController, layerName);
            var layer = CreateHookLayer(fxController, layerName);

            // TB17 retains the hardened TB14/TB16 two-stage native raycast flow, but adds a mandatory
            // recovery state after release so a held/retriggered request cannot immediately
            // reopen the Contact payload. Sam.py/Desktop can later replace this local timing
            // with an authoritative cooldown without changing the generated avatar contract.
            var ready = AddHookState(layer.stateMachine, "Ready / Hidden", new Vector3(120f, 120f));
            var armed = AddHookState(layer.stateMachine, "Armed / Waiting For Raycast", new Vector3(420f, 120f));
            var on = AddHookState(layer.stateMachine, "Cast / Approved Contact Window", new Vector3(720f, 120f));
            var waitForRelease = AddHookState(layer.stateMachine, "Wait For Menu Release", new Vector3(720f, 300f));
            var recovery = AddHookState(layer.stateMachine, "Recovery " + recoverySeconds.ToString("0.##") + "s", new Vector3(420f, 300f));
            layer.stateMachine.defaultState = ready;

            var animationIdentity = RaycastGateAnimationIdentity(layerKey, actionParameter, actionValue, integerGate);
            if (worldDropConstraint != null)
            {
                ready.motion = CreateOrReplaceWorldDropClip(RaycastAnimationClipPath(animationIdentity, "WorldDrop_Ready"), actionHost, worldDropConstraint, false, false, 1f / 60f);
                armed.motion = CreateOrReplaceWorldDropClip(RaycastAnimationClipPath(animationIdentity, "WorldDrop_Armed"), actionHost, worldDropConstraint, false, false, RaycastActionArmSeconds);
                on.motion = CreateOrReplaceWorldDropClip(RaycastAnimationClipPath(animationIdentity, "WorldDrop_Placed"), actionHost, worldDropConstraint, true, true, 1f / 60f);
                waitForRelease.motion = CreateOrReplaceWorldDropClip(RaycastAnimationClipPath(animationIdentity, "WorldDrop_Reset"), actionHost, worldDropConstraint, false, false, 1f / 60f);
                recovery.motion = CreateOrReplaceWorldDropClip(RaycastAnimationClipPath(animationIdentity, "WorldDrop_Recovery"), actionHost, worldDropConstraint, false, false, recoverySeconds);
            }
            else
            {
                ready.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Contact_Ready"), new[] { actionHost }, false, 1f / 60f);
                armed.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Contact_Armed"), new[] { actionHost }, false, RaycastActionArmSeconds);
                on.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Contact_Pulse"), new[] { actionHost }, true, RaycastActionPulseSeconds);
                waitForRelease.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Contact_WaitRelease"), new[] { actionHost }, false, 1f / 60f);
                recovery.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Contact_Recovery"), new[] { actionHost }, false, recoverySeconds);
            }

            foreach (var state in new[] { ready, armed, waitForRelease, recovery })
            {
                DriveBoolOnStateEnter(state, RaycastApprovedParameter, false);
                if (!string.IsNullOrWhiteSpace(categoryApprovedParameter))
                    DriveBoolOnStateEnter(state, categoryApprovedParameter, false);
            }
            DriveBoolOnStateEnter(on, RaycastApprovedParameter, true);
            if (!string.IsNullOrWhiteSpace(categoryApprovedParameter))
                DriveBoolOnStateEnter(on, categoryApprovedParameter, true);

            var arm = ready.AddTransition(armed);
            arm.hasExitTime = false;
            arm.duration = 0f;
            arm.AddCondition(integerGate ? AnimatorConditionMode.Equals : AnimatorConditionMode.If,
                integerGate ? actionValue : 0f, actionParameter);
            arm.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

            var confirm = armed.AddTransition(on);
            confirm.hasExitTime = false;
            confirm.duration = 0f;
            confirm.AddCondition(AnimatorConditionMode.If, 0f, hitPrefix + "_Hit");
            if (!string.IsNullOrWhiteSpace(additionalRequiredHitPrefix))
                confirm.AddCondition(AnimatorConditionMode.If, 0f, additionalRequiredHitPrefix + "_Hit");
            confirm.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

            var timeout = armed.AddTransition(waitForRelease);
            timeout.hasExitTime = true;
            timeout.exitTime = 1f;
            timeout.duration = 0f;

            if (worldDropConstraint == null)
            {
                var pulseDone = on.AddTransition(waitForRelease);
                pulseDone.hasExitTime = true;
                pulseDone.exitTime = 1f;
                pulseDone.duration = 0f;
            }
            else
            {
                var worldDropReleased = on.AddTransition(waitForRelease);
                worldDropReleased.hasExitTime = false;
                worldDropReleased.duration = 0f;
                worldDropReleased.AddCondition(integerGate ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.IfNot,
                    integerGate ? actionValue : 0f, actionParameter);
            }

            var koFromArmed = armed.AddTransition(waitForRelease);
            koFromArmed.hasExitTime = false;
            koFromArmed.duration = 0f;
            koFromArmed.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

            var koFromOn = on.AddTransition(waitForRelease);
            koFromOn.hasExitTime = false;
            koFromOn.duration = 0f;
            koFromOn.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

            var released = waitForRelease.AddTransition(recovery);
            released.hasExitTime = false;
            released.duration = 0f;
            released.AddCondition(integerGate ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.IfNot,
                integerGate ? actionValue : 0f, actionParameter);

            var recovered = recovery.AddTransition(ready);
            recovered.hasExitTime = true;
            recovered.exitTime = 1f;
            recovered.duration = 0f;

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
            Log(BuildNumber + " rebuilt raycast gate '" + layerKey + "' with " + recoverySeconds.ToString("0.##") + "s local recovery and approved-action pulse.");
        }

        private void RebuildRaycastTargetingLayer(
            string layerKey,
            GameObject targetIcon,
            string hitPrefix,
            string actionParameter,
            int actionValue,
            bool integerGate,
            bool showOnlyWhenActionIsZero,
            string additionalRequiredHitPrefix = null)
        {
            if (fxController == null || targetIcon == null)
                return;

            EnsureAnimatorParameter(fxController, "IsLocal", AnimatorControllerParameterType.Bool);
            var layerName = RaycastTargetLayerPrefix + layerKey;
            RemoveLayerByName(fxController, layerName);
            var layer = CreateHookLayer(fxController, layerName);
            var hidden = AddHookState(layer.stateMachine, "Hidden", new Vector3(180f, 160f));
            var visible = AddHookState(layer.stateMachine, "Local Targeting", new Vector3(500f, 160f));
            layer.stateMachine.defaultState = hidden;

            var animationIdentity = "Targeting_" + layerKey;
            hidden.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Crosshair_Hidden"), new[] { targetIcon }, false, 1f / 60f);
            visible.motion = CreateOrReplaceActiveClip(RaycastAnimationClipPath(animationIdentity, "Crosshair_Visible"), new[] { targetIcon }, true, 1f / 60f);

            var show = hidden.AddTransition(visible);
            show.hasExitTime = false;
            show.duration = 0f;
            show.AddCondition(AnimatorConditionMode.If, 0f, "IsLocal");
            show.AddCondition(AnimatorConditionMode.If, 0f, RaycastTargetingParameter);
            show.AddCondition(AnimatorConditionMode.If, 0f, hitPrefix + "_Hit");
            if (!string.IsNullOrWhiteSpace(additionalRequiredHitPrefix))
                show.AddCondition(AnimatorConditionMode.If, 0f, additionalRequiredHitPrefix + "_Hit");
            show.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");
            if (showOnlyWhenActionIsZero && integerGate)
                show.AddCondition(AnimatorConditionMode.Equals, 0f, actionParameter);
            else
                show.AddCondition(integerGate ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.IfNot, integerGate ? actionValue : 0f, actionParameter);

            var targetingDisabled = visible.AddTransition(hidden);
            targetingDisabled.hasExitTime = false;
            targetingDisabled.duration = 0f;
            targetingDisabled.AddCondition(AnimatorConditionMode.IfNot, 0f, RaycastTargetingParameter);

            var lost = visible.AddTransition(hidden);
            lost.hasExitTime = false;
            lost.duration = 0f;
            lost.AddCondition(AnimatorConditionMode.IfNot, 0f, hitPrefix + "_Hit");

            if (!string.IsNullOrWhiteSpace(additionalRequiredHitPrefix))
            {
                var floorLost = visible.AddTransition(hidden);
                floorLost.hasExitTime = false;
                floorLost.duration = 0f;
                floorLost.AddCondition(AnimatorConditionMode.IfNot, 0f, additionalRequiredHitPrefix + "_Hit");
            }

            var remote = visible.AddTransition(hidden);
            remote.hasExitTime = false;
            remote.duration = 0f;
            remote.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsLocal");

            var ko = visible.AddTransition(hidden);
            ko.hasExitTime = false;
            ko.duration = 0f;
            ko.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");

            var fired = visible.AddTransition(hidden);
            fired.hasExitTime = false;
            fired.duration = 0f;
            if (showOnlyWhenActionIsZero && integerGate)
                fired.AddCondition(AnimatorConditionMode.NotEqual, 0f, actionParameter);
            else
                fired.AddCondition(integerGate ? AnimatorConditionMode.Equals : AnimatorConditionMode.If, integerGate ? actionValue : 0f, actionParameter);

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            AssetDatabase.SaveAssets();
        }

        private void EnsureAnimatorParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            if (controller == null || controller.parameters.Any(x => x.name == name)) return;
            controller.AddParameter(name, type); EditorUtility.SetDirty(controller);
        }

        private void SetEnumMemberByKeywords(Component component, string[] keywords, params string[] names)
        {
            if (component == null || keywords == null || keywords.Length == 0)
                return;

            var member = FindMember(component.GetType(), names);
            var enumType = member is FieldInfo ? ((FieldInfo)member).FieldType : member is PropertyInfo ? ((PropertyInfo)member).PropertyType : null;
            if (enumType != null && enumType.IsEnum)
            {
                var values = Enum.GetValues(enumType).Cast<object>().ToArray();
                var matches = values
                    .Where(value => keywords.All(keyword => value.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderBy(value => value.ToString().Length)
                    .ToArray();
                var match = matches.FirstOrDefault();
                if (match == null)
                    match = values.OrderBy(value => value.ToString().Length)
                        .FirstOrDefault(value => value.ToString().IndexOf(keywords[0], StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null && WriteMember(component, match, names))
                    return;
            }

            SetSerializedEnumMemberByKeywords(component, keywords, names);
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
            SpellDefinition spell;
            ActionDefinition action;
            if (outgoingKind == OutgoingContactKind.Spell && TryGetSelectedSpell(out spell))
                return "Spell_" + spell.Id + "_" + MakeSafeAssetName(spell.Name);
            if (outgoingKind == OutgoingContactKind.Technick && TryGetSelectedTechnick(out action))
                return "Technick_" + action.Id + "_" + MakeSafeAssetName(action.Name);
            if (outgoingKind == OutgoingContactKind.Item && TryGetSelectedItem(out action))
                return "Item_" + action.Id + "_" + MakeSafeAssetName(action.Name);
            if (outgoingKind == OutgoingContactKind.Attack)
                return "Attack_" + attackTier;
            if (outgoingKind == OutgoingContactKind.Debuff)
                return "Debuff_" + string.Join("_", GetSelectedDebuffs().DefaultIfEmpty("Effect").ToArray());
            return "SoY_Raycast";
        }

        private void DrawCompactRaycastSettings()
        {
            DrawTagRow("SDK", RaycastTypeAvailable() ? "✓ Ready" : "✕ Missing", "Official VRCRaycast");
            var currentRaycastCount = CountRaycastComponents();
            DrawTagRow("Budget", currentRaycastCount + " / 80", currentRaycastCount >= 72 ? "Near shared Raycast/FinalIK limit" : "OK");
            raycastDistance = Mathf.Clamp(EditorGUILayout.FloatField("Range", raycastDistance), 0.1f, 1000f);
            raycastImpactRadius = Mathf.Clamp(EditorGUILayout.FloatField("Impact Radius", raycastImpactRadius), 0.01f, 1f);

            if (outgoingKind == OutgoingContactKind.Spell)
            {
                raycastDeliveryStyle = RaycastDeliveryStyle.WorldGroundPlacement;
                DrawTagRow("Mode", "World / Ground Placement", "Remote-player aim → world-floor probe → downward-facing FX holder");
                EditorGUILayout.HelpBox(
                    "After generating/repairing menus, turn Targeting ON. Aim at another player until the LOCAL crosshair appears, then press the installed Spell button. That same button arms placement, World Drops the Spell at the floor hit, and fires its Contact/animation state.",
                    MessageType.Info);
            }
            else if (outgoingKind == OutgoingContactKind.Technick)
            {
                var technickMode = technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement ? 1 : 0;
                technickMode = EditorGUILayout.Popup(
                    "Mode",
                    technickMode,
                    new[] { "Direct Impact", "World / Ground Placement" });
                technickRaycastDeliveryStyle = technickMode == 1
                    ? RaycastDeliveryStyle.WorldGroundPlacement
                    : RaycastDeliveryStyle.DirectImpact;
                raycastDeliveryStyle = technickRaycastDeliveryStyle;

                if (technickRaycastDeliveryStyle == RaycastDeliveryStyle.WorldGroundPlacement)
                {
                    DrawTagRow("Targets", "Remote player → floor beneath target", "Same two-stage World targeting used by Spells");
                    EditorGUILayout.HelpBox(
                        "World / Ground Placement is for Technicks that should be placed beneath a target: traps, fields, target-centered effects, ground sigils, or other placed Technicks. Turn Targeting ON, aim until the LOCAL crosshair appears, then press/hold the installed Technick button.",
                        MessageType.Info);
                }
                else
                {
                    raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Targets", raycastCollisionTarget);
                    EditorGUILayout.LabelField("Direct Impact is for projectile-like Technicks that resolve at the first hit.", wrappedLabel);
                }
            }
            else
            {
                raycastDeliveryStyle = RaycastDeliveryStyle.DirectImpact;
                raycastCollisionTarget = (RaycastCollisionTarget)EditorGUILayout.EnumPopup("Targets", raycastCollisionTarget);
                EditorGUILayout.LabelField("Direct Impact is intended for bullets, arrows, Items, and Debuffs.", wrappedLabel);
            }

            showRaycastAdvanced = EditorGUILayout.Foldout(showRaycastAdvanced, "Advanced Raycast", true);
            if (showRaycastAdvanced)
            {
                raycastDirection = EditorGUILayout.Vector3Field("Local Aim Direction", raycastDirection);
                if (!CurrentRaycastUsesWorldGroundPlacement())
                    raycastApplyRotation = EditorGUILayout.ToggleLeft("Align direct impact to hit surface", raycastApplyRotation);
                raycastCreateLineRenderer = EditorGUILayout.ToggleLeft("Create visual LineRenderer holder", raycastCreateLineRenderer);
                raycastUseCustomPrefix = EditorGUILayout.ToggleLeft("Use custom asset prefix", raycastUseCustomPrefix);
                if (raycastUseCustomPrefix)
                    raycastParameterPrefix = EditorGUILayout.TextField("Custom Prefix", raycastParameterPrefix);
            }

            var suggested = CurrentRaycastSuggestedPrefix();
            EditorGUILayout.LabelField("Creates / repairs: " + suggested, EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!RaycastTypeAvailable() || !HasUsableTargets() || avatarRoot == null))
            {
                if (GUILayout.Button("Create / Repair Raycast", GUILayout.Height(largeControls ? 42f : 32f)))
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
                attackStartsEnabled = EditorGUILayout.ToggleLeft("Debug: Start enabled (TB17 functional gate overrides during Play)", attackStartsEnabled);
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
            var spells = GetVisibleSpellDefinitions();
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
                spellStartsEnabled = EditorGUILayout.ToggleLeft("Debug: Start enabled (TB17 functional gate overrides during Play)", spellStartsEnabled);
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
            var rows = GetVisibleTechnickDefinitions();
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
                technickStartsEnabled = EditorGUILayout.ToggleLeft("Debug: Start enabled (TB17 functional gate overrides during Play)", technickStartsEnabled);
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
            var rows = GetVisibleItemDefinitions();
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
                itemStartsEnabled = EditorGUILayout.ToggleLeft("Debug: Start enabled (TB17 functional gate overrides during Play)", itemStartsEnabled);
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
                "Creates an active guard receiver that accepts the canonical '" + TagBlockable + "' tag plus outside-system aliases '" +
                TagHitBlocked + "' and '" + TagExternalParryDetect + "'. All three drive the existing Bool parameter '" +
                TagHitBlocked + "' and can mirror into SoY_HitBlocked for Desktop/Sam.py. " +
                "The tool can also emit the legacy '" + TagHitBlocked + "' sender tag for wider compatibility.",
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
            EditorGUILayout.LabelField("• Weak, Average, and Strong Stories attacks carry 'Blockable'.", wrappedLabel);
            EditorGUILayout.LabelField("• External block-compatible senders accepted here: 'Blockable', 'Hit Blocked', and 'Parry_Detect'.", wrappedLabel);
            EditorGUILayout.LabelField("• Critical Stories attacks never carry 'Blockable' and bypass this receiver.", wrappedLabel);
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

            incomingHits = EditorGUILayout.ToggleLeft("Create hit receivers: Stories tiers + outside Contact aliases", incomingHits);
            if (incomingHits)
            {
                EditorGUILayout.HelpBox(
                    "External compatibility is enabled on the same body receiver: 'Sword' and 'Weapon' register as Average; VRChat's default 'Hands' tag registers as Weak. " +
                    "Because outside systems do not provide SoY caster alignment, these aliases are treated as hostile/unknown and still pass through the normal Desktop → Sam.py DM Gate.",
                    MessageType.Info);
            }
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
            DrawTagRow(TagWeak + " / " + TagExternalHands, "SoY_HitWeak", "Canonical Weak plus default VRChat Hands compatibility");
            DrawTagRow(TagAverage + " / " + TagExternalSword + " / " + TagExternalWeapon, "SoY_HitAverage", "Canonical Average plus outside sword/weapon compatibility");
            DrawTagRow(TagStrong, "SoY_HitStrong", "Exact canonical Strong collision tag");
            DrawTagRow(TagCritical, "SoY_HitCritical", "Exact canonical Critical tag; Critical remains unblockable");
            DrawTagRow(string.Join(" / ", CompatibleBlockContactTags), TagHitBlocked + " + SoY_HitBlocked", "Accepted by active block surfaces as compatible block/parry sender tags");
            DrawTagRow("Burn / Silence", "SoY_DebuffBurn / Silence", "Exact debuff tags; Constant mode held for OSC rising-edge detection");
            DrawTagRow("Freeze / Bind / Bleed", "SoY_DebuffFreeze / Bind / Bleed", "Exact debuff tags; Constant mode held for OSC rising-edge detection");
            DrawTagRow("Spell Active", SpellActiveParameter, "True while any encoded spell sender is inside the receiver");
            DrawTagRow("Spell ID Bits", SpellBitParameterPrefix + "0-7", "Eight Bool values reconstruct the stable 1-255 spell ID in Desktop v0.8.1+");
            DrawTagRow("Resolved Spell", "SoY_SpellType (Int)", "Desktop reconstructs the bit bus and sends the normal registry ID to Sam.py");
            DrawTagRow("Canonical action alignment", "SoY_HealingSourceEnemy / SoY_DamageSourceEnemy", "Driven only by SoY Caster Enemy");
            DrawTagRow("External damage source", ExternalDamageSourceParameter, "Sword / Weapon / Hands compatibility is isolated from canonical Stories alignment");
            DrawTagRow("Technick Bus", "SoY_TechnickActive + Bit0-7", "Desktop reconstructs TECHNICK_ID_REGISTRY_v1");
            DrawTagRow("Item Bus", "SoY_ItemActive + Bit0-7", "Desktop reconstructs ITEM_ID_REGISTRY_v1; Sam.py verifies inventory");
            DrawTagRow("I-Frames", "1.0 second", "Incoming damage receiver child is disabled after each accepted hit");
            EndCard();
        }

        private void DrawAnimatorSetup()
        {
            switch (animationPage)
            {
                case AnimationPage.Resources:
                    DrawResourceFxBuilder();
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
                case AnimationPage.Evasion:
                    DrawEvasionAnimationBuilder();
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

            if (GUILayout.Button("OPEN AVATAR SETUP / REPAIR", GUILayout.Height(largeControls ? 44f : 34f)))
                tab = StudioTab.Setup;

            var installedActionCount = GetInstalledSpellDefinitions().Length + GetInstalledTechnickDefinitions().Length + GetInstalledItemDefinitions().Length;
            using (new EditorGUI.DisabledScope(fxController == null || avatarRoot == null || installedActionCount == 0))
            {
                if (GUILayout.Button("AUTOMATE / REPAIR ALL INSTALLED ACTIONS", GUILayout.Height(largeControls ? 48f : 36f)))
                    RebuildAllAutomatedActionLayers();
            }
            EditorGUILayout.LabelField(
                "TB17 automation discovers installed Spell/Technick/Item actions, repairs their profile bindings, generates functional gates and timer motions, and preserves optional shared/custom presentation clips.",
                wrappedLabel);

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
            BeginCard("Start Here — Recommended Workflow");
            EditorGUILayout.LabelField("1. Assign the avatar's VRC Avatar Descriptor and press Load From Avatar.", wrappedLabel);
            EditorGUILayout.LabelField("2. Open Setup and press Prepare / Repair. The tool creates and assigns a safe FX copy; the original FX controller is never edited.", wrappedLabel);
            EditorGUILayout.LabelField("3. Use Contacts for physical/contact senders and receivers. Use Raycast Studio for bullets, arrows, projectiles, and ground-placed spells.", wrappedLabel);
            EditorGUILayout.LabelField("4. Build or repair the generated Stories RP menu. The menu is grouped into Combat, Spells, Actions, Targeting, Status, and optional Quick Access.", wrappedLabel);
            EditorGUILayout.LabelField("5. Run Build & Test / Audit before uploading the avatar.", wrappedLabel);
            EndCard();

            BeginCard("Generated Avatar Workspace");
            EditorGUILayout.LabelField(
                "New generated assets stay together under a folder named for the avatar/model. Existing legacy generated assets are recognized but are not automatically moved, so old Unity references are not broken.",
                wrappedLabel);
            EditorGUILayout.SelectableLabel(
                GeneratedAssetRoot + "/<Avatar>/",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            DrawTagRow("FX", "<Avatar>/FX", "Safe FX controller copies");
            DrawTagRow("Menus", "<Avatar>/Menus", "Stories RP menus and menu pages");
            DrawTagRow("Animations", "<Avatar>/Animations", "Contact, Raycast, I-Frame, status, and helper clips");
            DrawTagRow("Profiles", "<Avatar>/Profiles", "Animation/profile metadata");
            DrawTagRow("Backups", "<Avatar>/Backups", "Manifests, migrations, and repair snapshots");
            EndCard();

            BeginCard("Raycast — Direct Impact");
            EditorGUILayout.LabelField(
                "Use Direct Impact for bullets, arrows, beams, thrown effects, and other actions that should resolve at the first surface/player hit. Select the muzzle, weapon, hand, or focus that supplies the local ray direction.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Remote Players Only is the recommended player mode. The tool prefers VRChat's custom Player layer (9) and excludes PlayerLocal (10). If the current SDK does not expose the custom layer-mask member, the builder falls back to Hit Players and writes a warning instead of creating a dead raycast.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Attack/Debuff raycasts use the generated Targeting → Projectile Fire control. Selector actions such as Technicks and Items use their action selection/trigger.",
                wrappedLabel);
            EndCard();

            BeginCard("Raycast — Spell Ground Placement");
            EditorGUILayout.LabelField(
                "Use Spell Ground Placement when the effect should appear beneath another player rather than directly on their body. Spell delivery uses a shared two-stage rig so every spell does not consume two more VRCRaycast components.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Stage 1 aims at a remote player. When a valid target is found, a local-only targeting icon appears at the hit point. Pressing the selected Spell hides that icon immediately and arms the cast.",
                wrappedLabel);
            EditorGUILayout.LabelField(
                "Stage 2 begins above that target point and casts straight down into world geometry. The final ground result enables Apply Rotation with +Y aligned to the floor normal. Put particles/VFX under the generated child named:",
                wrappedLabel);
            EditorGUILayout.SelectableLabel(
                "FX — Faces Down (Place Particle Here)",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField(
                "That child is rotated so conventional +Z-facing effects point downward while still following the contacted floor's orientation. Do not re-parent the VRCRaycast Result Transform under its own ray origin.",
                wrappedLabel);
            EditorGUILayout.HelpBox(
                "Persistent target following after you stop aiming is intentionally not generated here. VRCRaycast follows the current hit while aiming; a Contact-Tracker-style system is a separate feature for effects that must remain attached to a moving player after the cast.",
                MessageType.Info);
            EndCard();

            BeginCard("Local Targeting Icon");
            EditorGUILayout.LabelField(
                "The targeting crosshair is controlled by VRChat's built-in IsLocal parameter. It is visible only on the avatar owner, only while a valid target exists, and only while the corresponding action is not being fired/cast. Other players do not see it.",
                wrappedLabel);
            EndCard();

            BeginCard("Contact Preview");
            EditorGUILayout.LabelField(
                "Preview is an editor-only trigger Collider, not a VRChat Contact. Position, rotate, and resize it safely with Unity Transform/Collider tools. Finalize copies the preview transform/shape into the real Contact and removes the temporary preview object.",
                wrappedLabel);
            EndCard();

            BeginCard("Safe FX Copy Workflow");
            EditorGUILayout.LabelField(
                "The tool never edits the avatar's original FX controller. Before adding Animator parameters or layers it duplicates the controller and stores the managed copy under:",
                wrappedLabel);
            EditorGUILayout.SelectableLabel(
                GeneratedAssetRoot + "/<Avatar>/FX/<FX>_SoY_FX.controller",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EndCard();

            BeginCard("Stories RP Menu Map");
            DrawTagRow("Combat", "RP Combat / Enemy Mode", "Core on/off and alignment controls");
            DrawTagRow("Spells", "School pages", "Stable Spell IDs and momentary spell actions");
            DrawTagRow("Actions", "Technicks / Items", "Non-spell action selectors");
            DrawTagRow("Targeting", "Projectile Fire", "Direct-impact Raycast fire control");
            DrawTagRow("Status", "Mist / Curse / status gauges", "Status and resource visibility");
            DrawTagRow("Quick Access", "Optional shortcuts", "Frequently used controls without cluttering the root menu");
            EndCard();

            BeginCard("Contact Types");
            DrawTagRow("Attack", "Weak / Average / Strong / Critical", "Weapon contacts or Direct Impact Raycast contacts");
            DrawTagRow("Spell", "8-bit Contact Bus → SoY_SpellType", "Stable spell IDs with Ally/Enemy caster alignment");
            DrawTagRow("Blocking", string.Join(" / ", CompatibleBlockContactTags) + " → " + TagHitBlocked, "Shield face or guarded weapon volume");
            DrawTagRow("External Average", TagExternalSword + " / " + TagExternalWeapon, "Normalized to SoY_HitAverage");
            DrawTagRow("External Weak", TagExternalHands, "Default VRChat hand tag normalized to SoY_HitWeak");
            DrawTagRow("Debuff", string.Join(", ", DebuffTags), "Spell, raycast impact, aura, or effect volume");
            DrawTagRow("Incoming", "SoY_Hit* / SoY_Debuff*", "Avatar body receiver volume for Desktop/Sam.py synchronization");
            EndCard();

            BeginCard("External Contact Compatibility — TB12");
            EditorGUILayout.LabelField(
                "TB12 accepts a small compatibility vocabulary from outside avatar/contact systems without modifying those senders. Tags are exact and case-sensitive.",
                wrappedLabel);
            DrawTagRow(TagExternalSword, "Average hit", "Writes SoY_HitAverage and marks the outside source hostile/unknown for the normal Sam.py DM Gate");
            DrawTagRow(TagExternalWeapon, "Average hit", "Writes SoY_HitAverage and marks the outside source hostile/unknown for the normal Sam.py DM Gate");
            DrawTagRow(TagExternalHands, "Weak hit", "VRChat default hand tag; writes SoY_HitWeak");
            DrawTagRow(TagBlockable, "Block-compatible", "Canonical Stories blockable sender");
            DrawTagRow(TagHitBlocked, "Block-compatible", "Accepted as an outside block/parry alias; still emitted for legacy compatibility");
            DrawTagRow(TagExternalParryDetect, "Block-compatible", "Accepted by active Stories block surfaces");
            EditorGUILayout.HelpBox(
                "Important: 'Hands' is a broad/default VRChat contact tag. If the incoming body receiver is enabled, ordinary hand contact from another avatar can register as a Weak hit. Use receiver animation/activation gating when passive touching should not count as combat.",
                MessageType.Warning);
            EndCard();

            BeginCard("Accessibility");
            EditorGUILayout.LabelField(
                "Open Maintenance → Display for Normal, Large, and Extra Large text; larger controls; high-contrast and color-vision-friendly palettes; short VRChat labels; and purpose-first spell navigation.",
                wrappedLabel);
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

            EnsureAssetFolder(CurrentAvatarGeneratedFolder("Backups/Unity Tool"));
            var backupPath = AssetDatabase.GenerateUniqueAssetPath(
                CurrentAvatarGeneratedFolder("Backups/Unity Tool") + "/StoriesOfYggdrasilOSCContactSystem_v" + Version + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
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
            BeginCard("Unity / OSC Protocol Marker");
            DrawTagRow("Tool", "v" + Version + " " + BuildNumber, "Current authoring build");
            DrawTagRow("Protocol", OscProtocolVersion.ToString(), "Required by Desktop v0.8.21-prebuild.2");
            DrawTagRow("Marker", HasCurrentCompatibilityMarkerLayer() ? "✓ Installed" : "✕ Missing", UnityMarkerLayer);
            DrawTagRow("Schema", managedRepairAuditReady && CurrentSchemaIsValid() ? "✓ Valid" : "! Audit / migration required", "Run Avatar Setup → Migrate / Validate");
            EndCard();
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
            return path.StartsWith(LegacyFxCopyRoot + "/", StringComparison.OrdinalIgnoreCase) || IsNewAvatarFxPath(path);
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

            var avatarName = MakeSafeAssetName(avatarDescriptor.gameObject.name);
            var fxFolder = AvatarGeneratedFolder(avatarName, "FX");
            EnsureAssetFolder(fxFolder);
            var controllerName = MakeSafeAssetName(fxController.name);
            var destination = AssetDatabase.GenerateUniqueAssetPath(
                fxFolder + "/" + controllerName + "_SoY_FX.controller");

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
            SaveCurrentAvatarContext();
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

        private static string AvatarGeneratedFolder(string avatarName, string category)
        {
            var root = GeneratedAssetRoot + "/" + MakeSafeAssetName(avatarName);
            return string.IsNullOrWhiteSpace(category) ? root : root + "/" + category.Trim('/');
        }

        private string CurrentAvatarGeneratedFolder(string category)
        {
            var avatarName = avatarDescriptor != null
                ? avatarDescriptor.gameObject.name
                : avatarRoot != null ? avatarRoot.name : "Avatar";
            return AvatarGeneratedFolder(avatarName, category);
        }

        private static bool IsNewAvatarFxPath(string path)
        {
            var normalized = (path ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith(GeneratedAssetRoot + "/", StringComparison.OrdinalIgnoreCase))
                return false;
            return normalized.IndexOf("/FX/", StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static string PvpAttemptParameterForTier(AttackTier tier)
        {
            switch (tier)
            {
                case AttackTier.Weak: return PvpAttemptWeakParameter;
                case AttackTier.Strong: return PvpAttemptStrongParameter;
                case AttackTier.Critical: return PvpAttemptCriticalParameter;
                default: return PvpAttemptAverageParameter;
            }
        }

        private void ConfigurePvpAttemptReceiver(
            GameObject host,
            AttackTier tier,
            ContactShape shape,
            float radius,
            float height,
            Vector3 boxSize,
            Vector3 position,
            Vector3 rotation)
        {
            if (host == null)
                return;
            var parameter = PvpAttemptParameterForTier(tier);
            var receiver = EnsureReceiverForTagsAndParameter(
                host,
                FindType(ReceiverTypeName),
                PvpRemoteBodyTags,
                parameter);
            if (receiver == null)
                return;

            ConfigureContact(receiver, shape, radius, height, boxSize, position, rotation, PvpRemoteBodyTags);
            SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
            SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            SetStringMember(receiver, parameter, "parameter", "Parameter");
            SetEnumMember(receiver, "OnEnter", "receiverType", "ReceiverType");
            SetFloatMember(receiver, 1f, "value", "Value");
            SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
            FinishContact(receiver);
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

                // Protocol 21: local attacker-side proof that this exact attack volume
                // touched another humanoid avatar. Built-in body tags are generated by
                // VRChat for humanoid avatars, so this does not depend on the target
                // having the Stories tool installed. The receiver is local-only and
                // never identifies the target; Sam.py pairs it with the target's
                // authenticated hit receipt.
                ConfigurePvpAttemptReceiver(
                    host,
                    attackTier,
                    attackShape,
                    attackRadius,
                    attackHeight,
                    attackBoxSize,
                    attackPosition,
                    attackRotation);

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

                var receiverType = FindType(ReceiverTypeName);
                var receiver = EnsureReceiverForTagsAndParameter(host, receiverType, CompatibleBlockContactTags, TagHitBlocked);
                if (receiver != null)
                {
                    ConfigureContact(receiver, blockShape, blockRadius, blockHeight, blockBoxSize, blockPosition, blockRotation, CompatibleBlockContactTags);
                    SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
                    SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
                    SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                    SetStringMember(receiver, TagHitBlocked, "parameter", "Parameter");
                    SetEnumMember(receiver, "Constant", "receiverType", "ReceiverType");
                    SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
                    FinishContact(receiver);
                    Log("Block receiver ready on '" + host.name + "': " +
                        string.Join(" / ", CompatibleBlockContactTags) + " → " + TagHitBlocked);
                }

                if (bridgeBlockToOsc)
                {
                    var oscReceiver = EnsureReceiverForTagsAndParameter(host, receiverType, CompatibleBlockContactTags, "SoY_HitBlocked");
                    if (oscReceiver != null)
                    {
                        ConfigureContact(oscReceiver, blockShape, blockRadius, blockHeight, blockBoxSize, blockPosition, blockRotation, CompatibleBlockContactTags);
                        SetBoolMember(oscReceiver, false, "allowSelf", "AllowSelf");
                        SetBoolMember(oscReceiver, true, "allowOthers", "AllowOthers");
                        SetBoolMember(oscReceiver, true, "localOnly", "LocalOnly");
                        SetStringMember(oscReceiver, "SoY_HitBlocked", "parameter", "Parameter");
                        SetEnumMember(oscReceiver, "Constant", "receiverType", "ReceiverType");
                        SetFloatMember(oscReceiver, 0f, "minVelocity", "MinVelocity");
                        FinishContact(oscReceiver);
                        Log("OSC block mirror ready on '" + host.name + "': " +
                            string.Join(" / ", CompatibleBlockContactTags) + " → SoY_HitBlocked");
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
            SpellDefinition spell;
            if (!TryGetSelectedSpell(out spell))
                return;

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Spell - " + spell.Id + " " + spell.Name, spellStartsEnabled);
                CreateContactChild(host, "FX — Spell Visuals (Place Here)", true);
                var allyHost = CreateContactChild(host, "[SoY Spell Ally] " + spell.Id + " " + spell.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Spell Enemy] " + spell.Id + " " + spell.Name, false);

                ConfigureSpellSender(allyHost, spell, CasterAllyTag);
                ConfigureSpellSender(enemyHost, spell, CasterEnemyTag);

                Selection.activeGameObject = host;
                Log("Spell sender ready on '" + host.name + "': " + spell.Name + " (ID " + spell.Id + ", bits " + GetSpellBinary(spell.Id) + ").");
            }

            SyncInstalledActionAnimationProfile(ActionAnimationKind.Spell);
            RebuildSpellCastAnimationLayer();
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
            ActionDefinition technick;
            if (!TryGetSelectedTechnick(out technick))
                return;

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Technick - " + technick.Id + " " + technick.Name, technickStartsEnabled);
                CreateContactChild(host, "FX — Technick Visuals (Place Here)", true);
                var allyHost = CreateContactChild(host, "[SoY Technick Ally] " + technick.Id + " " + technick.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Technick Enemy] " + technick.Id + " " + technick.Name, false);
                ConfigureActionSender(allyHost, technick.Id, TechnickActiveTag, TechnickBitTagPrefix, "SoY Technick", CasterAllyTag,
                    technickShape, technickRadius, technickHeight, technickBoxSize, technickPosition, technickRotation);
                ConfigureActionSender(enemyHost, technick.Id, TechnickActiveTag, TechnickBitTagPrefix, "SoY Technick", CasterEnemyTag,
                    technickShape, technickRadius, technickHeight, technickBoxSize, technickPosition, technickRotation);
                Selection.activeGameObject = host;
                Log("Technick sender ready on '" + host.name + "': " + technick.Name + " (ID " + technick.Id + ", bits " + GetActionBinary(technick.Id) + ").");
            }
            SyncInstalledActionAnimationProfile(ActionAnimationKind.Technick);
            RebuildActionAnimationLayer(ActionAnimationKind.Technick, "SoY_TechnickType", TechnickCastLayer, animationProfile.technickAnimations);
            RebuildSpellAlignmentLayer();
        }

        private void ConfigureHelpfulItemReceiver(
            GameObject host,
            string parameter,
            bool allowSelf,
            bool allowOthers)
        {
            if (host == null)
                return;
            var receiver = EnsureReceiverForTagsAndParameter(
                host,
                FindType(ReceiverTypeName),
                new[] { "Head" },
                parameter);
            if (receiver == null)
                return;
            ConfigureContact(
                receiver,
                ContactShape.Sphere,
                0.11f,
                0.22f,
                Vector3.one * 0.22f,
                Vector3.zero,
                Vector3.zero,
                new[] { "Head" });
            SetBoolMember(receiver, allowSelf, "allowSelf", "AllowSelf");
            SetBoolMember(receiver, allowOthers, "allowOthers", "AllowOthers");
            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            SetStringMember(receiver, parameter, "parameter", "Parameter");
            SetEnumMember(receiver, "OnEnter", "receiverType", "ReceiverType");
            SetFloatMember(receiver, 1f, "value", "Value");
            SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
            FinishContact(receiver);
        }

        private void ConfigureHelpfulItemBusSender(GameObject host, int itemId)
        {
            if (host == null)
                return;
            var tags = GetActionBusTags(itemId, HelpfulItemActiveTag, HelpfulItemBitTagPrefix).ToArray();
            var sender = EnsureContact(host, FindType(SenderTypeName), tags, null);
            if (sender == null)
                return;
            ConfigureContact(
                sender,
                ContactShape.Sphere,
                0.11f,
                0.22f,
                Vector3.one * 0.22f,
                Vector3.zero,
                Vector3.zero,
                tags);
            SetBoolMember(sender, false, "localOnly", "LocalOnly");
            FinishContact(sender);
        }

        private bool EnsureHelpfulItemHeadReceiverBus()
        {
            if (avatarRoot == null)
                return false;
            var animator = avatarRoot.GetComponent<Animator>() ?? avatarRoot.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
            {
                EditorUtility.DisplayDialog(
                    "Helpful Item Head Receiver",
                    "TB18 requires a Humanoid Animator with a mapped Head bone for physical helpful-item targeting.",
                    "OK");
                return false;
            }

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                EditorUtility.DisplayDialog(
                    "Helpful Item Head Receiver",
                    "The Humanoid avatar has no mapped Head bone.",
                    "OK");
                return false;
            }

            var host = CreateContactChild(head.gameObject, HelpfulItemHeadReceiverHost, true);
            var mappings = new List<ReceiverMapping>
            {
                new ReceiverMapping(HelpfulItemActiveTag, HelpfulItemActiveParameter)
            };
            for (var bit = 0; bit < ActionBitCount; bit++)
                mappings.Add(new ReceiverMapping(
                    HelpfulItemBitTagPrefix + bit,
                    HelpfulItemBitParameterPrefix + bit));

            var oldSuppress = suppressContactAttachment;
            suppressContactAttachment = true;
            try
            {
                foreach (var mapping in mappings)
                {
                    var tags = mapping.CollisionTags.Distinct(StringComparer.Ordinal).Take(16).ToArray();
                    var receiver = EnsureReceiverForTagsAndParameter(
                        host,
                        FindType(ReceiverTypeName),
                        tags,
                        mapping.Parameter);
                    if (receiver == null)
                        continue;
                    ConfigureContact(
                        receiver,
                        ContactShape.Sphere,
                        0.16f,
                        0.32f,
                        Vector3.one * 0.32f,
                        Vector3.zero,
                        Vector3.zero,
                        tags);
                    SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
                    SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
                    SetBoolMember(receiver, true, "localOnly", "LocalOnly");
                    SetStringMember(receiver, mapping.Parameter, "parameter", "Parameter");
                    SetEnumMember(receiver, "Constant", "receiverType", "ReceiverType");
                    SetFloatMember(receiver, 1f, "value", "Value");
                    SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
                    FinishContact(receiver);
                }
            }
            finally
            {
                suppressContactAttachment = oldSuppress;
            }

            return true;
        }

        private static void AddHelpfulExitTransitions(
            AnimatorState from,
            AnimatorState hidden,
            int itemId)
        {
            if (from == null || hidden == null)
                return;

            var deselect = from.AddTransition(hidden);
            deselect.hasExitTime = false;
            deselect.duration = 0f;
            deselect.AddCondition(AnimatorConditionMode.NotEqual, itemId, "SoY_ItemType");

            var ko = from.AddTransition(hidden);
            ko.hasExitTime = false;
            ko.duration = 0f;
            ko.AddCondition(AnimatorConditionMode.If, 0f, "SoY_KO");
        }

        private static void AddHelpfulResultTransitions(
            AnimatorState from,
            AnimatorState success,
            AnimatorState failure)
        {
            if (from == null)
                return;
            var ok = from.AddTransition(success);
            ok.hasExitTime = false;
            ok.duration = 0f;
            ok.AddCondition(AnimatorConditionMode.Equals, 1f, HelpfulItemUseResultParameter);

            for (var result = 2; result <= 6; result++)
            {
                var fail = from.AddTransition(failure);
                fail.hasExitTime = false;
                fail.duration = 0f;
                fail.AddCondition(AnimatorConditionMode.Equals, result, HelpfulItemUseResultParameter);
            }
        }

        private void RebuildAllHelpfulItemInteractions()
        {
            if (animationProfile == null || animationProfile.itemAnimations == null)
                return;
            foreach (var binding in animationProfile.itemAnimations
                .Where(binding => binding != null && binding.physicalHelpful && IsHelpfulPhysicalItem(binding.id))
                .OrderBy(binding => binding.id)
                .ToArray())
            {
                RebuildHelpfulItemInteraction(binding, false);
            }
        }

        private void RebuildHelpfulItemInteraction(ActionAnimationBinding binding, bool showErrors = true)
        {
            if (binding == null || !binding.physicalHelpful || !IsHelpfulPhysicalItem(binding.id))
                return;
            if (avatarRoot == null || fxController == null || !EnsureSafeFxCopy(showErrors))
                return;

            var prop = FindAvatarObjectByRelativePath(binding.physicalPropPath);
            if (prop == null)
            {
                if (showErrors)
                    EditorUtility.DisplayDialog("Physical Helpful Item", "Assign an Item Object / Prop under the avatar first.", "OK");
                return;
            }
            if (binding.grabGesture == binding.useGesture)
            {
                if (showErrors)
                    EditorUtility.DisplayDialog(
                        "Physical Helpful Item",
                        "Grab / Toggle Gesture and Use Gesture must be different so the toggle latch and use window cannot fight each other.",
                        "OK");
                return;
            }
            if (!binding.helpfulAllowSelf && !binding.helpfulAllowOthers)
                return;
            if (!EnsureHelpfulItemHeadReceiverBus())
                return;

            EnsureAnimatorParameter(fxController, "SoY_ItemType", AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, "SoY_KO", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(fxController, HelpfulItemUseResultParameter, AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, HelpfulItemReceiveResultParameter, AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, "GestureLeft", AnimatorControllerParameterType.Int);
            EnsureAnimatorParameter(fxController, "GestureRight", AnimatorControllerParameterType.Int);

            var interactionRoot = CreateContactChild(
                prop,
                "[SoY Helpful Item Interaction] " + binding.id + " " + binding.name,
                false);
            var selfHost = CreateContactChild(interactionRoot, "[SoY Helpful Self Head Detect]", binding.helpfulAllowSelf);
            var otherHost = CreateContactChild(interactionRoot, "[SoY Helpful Other Head Detect]", binding.helpfulAllowOthers);
            var senderHost = CreateContactChild(interactionRoot, "[SoY Helpful Item Bus]", binding.helpfulAllowOthers);

            var oldSuppress = suppressContactAttachment;
            suppressContactAttachment = true;
            try
            {
                if (binding.helpfulAllowSelf)
                    ConfigureHelpfulItemReceiver(selfHost, HelpfulItemSelfTouchParameter, true, false);
                if (binding.helpfulAllowOthers)
                {
                    ConfigureHelpfulItemReceiver(otherHost, HelpfulItemOtherTouchParameter, false, true);
                    ConfigureHelpfulItemBusSender(senderHost, binding.id);
                }
            }
            finally
            {
                suppressContactAttachment = oldSuppress;
            }

            var layerName = HelpfulItemLayerPrefix + binding.id + " " + binding.name;
            RemoveLayerByName(fxController, layerName);
            var layer = CreateHookLayer(fxController, layerName);
            var folder = CurrentAvatarGeneratedFolder("Animations/Helpful Items/" + binding.id + "_" + MakeSafeAssetName(binding.name));
            EnsureAssetFolder(folder);

            var controlled = new[] { prop, interactionRoot };
            var hidden = AddHookState(layer.stateMachine, "Hidden", new Vector3(100f, 160f));
            var grabLatch = AddHookState(layer.stateMachine, "Grab Toggle On — Wait Release", new Vector3(400f, 80f));
            var held = AddHookState(layer.stateMachine, "Held / Ready", new Vector3(700f, 160f));
            var use = AddHookState(layer.stateMachine, "Use — Head Contacts Active", new Vector3(1000f, 80f));
            var hideLatch = AddHookState(layer.stateMachine, "Grab Toggle Off — Wait Release", new Vector3(700f, 360f));
            var success = AddHookState(layer.stateMachine, "Server Result — Success", new Vector3(1000f, 260f));
            var failure = AddHookState(layer.stateMachine, "Server Result — Failure / Empty", new Vector3(1000f, 430f));
            var resultWait = AddHookState(layer.stateMachine, "Wait For Result Reset", new Vector3(700f, 520f));

            hidden.motion = CreateOrReplaceSelectiveActiveClip(folder + "/Hidden.anim", controlled, Array.Empty<GameObject>(), 1f / 60f);
            grabLatch.motion = CreateOrReplaceSelectiveActiveClip(folder + "/GrabLatch.anim", controlled, new[] { prop }, 1f / 60f);
            held.motion = CreateOrReplaceSelectiveActiveClip(folder + "/Held.anim", controlled, new[] { prop }, 1f / 60f);
            use.motion = CreateOrReplaceSelectiveActiveClip(folder + "/Use.anim", controlled, new[] { prop, interactionRoot }, 1f / 60f);
            hideLatch.motion = CreateOrReplaceSelectiveActiveClip(folder + "/HideLatch.anim", controlled, new[] { prop }, 1f / 60f);
            success.motion = LoadClipFromPath(binding.helpfulSuccessClipPath) ??
                CreateOrReplaceTimerClip(folder + "/Success.anim", 0.45f);
            failure.motion = LoadClipFromPath(binding.helpfulFailureClipPath) ??
                CreateOrReplaceTimerClip(folder + "/Failure.anim", 0.65f);
            resultWait.motion = CreateOrReplaceSelectiveActiveClip(folder + "/ResultWait.anim", controlled, new[] { prop }, 1f / 60f);
            layer.stateMachine.defaultState = hidden;

            var gestureParameter = GestureParameterForHand(binding.helpfulHand);
            var grab = (int)binding.grabGesture;
            var useGesture = (int)binding.useGesture;

            var equip = hidden.AddTransition(grabLatch);
            equip.hasExitTime = false;
            equip.duration = 0f;
            equip.AddCondition(AnimatorConditionMode.Equals, binding.id, "SoY_ItemType");
            equip.AddCondition(AnimatorConditionMode.Equals, grab, gestureParameter);
            equip.AddCondition(AnimatorConditionMode.IfNot, 0f, "SoY_KO");

            var grabReleased = grabLatch.AddTransition(held);
            grabReleased.hasExitTime = false;
            grabReleased.duration = 0f;
            grabReleased.AddCondition(AnimatorConditionMode.NotEqual, grab, gestureParameter);

            var beginUse = held.AddTransition(use);
            beginUse.hasExitTime = false;
            beginUse.duration = 0f;
            beginUse.AddCondition(AnimatorConditionMode.Equals, useGesture, gestureParameter);

            var endUse = use.AddTransition(held);
            endUse.hasExitTime = false;
            endUse.duration = 0f;
            endUse.AddCondition(AnimatorConditionMode.NotEqual, useGesture, gestureParameter);

            var beginHide = held.AddTransition(hideLatch);
            beginHide.hasExitTime = false;
            beginHide.duration = 0f;
            beginHide.AddCondition(AnimatorConditionMode.Equals, grab, gestureParameter);

            var hideReleased = hideLatch.AddTransition(hidden);
            hideReleased.hasExitTime = false;
            hideReleased.duration = 0f;
            hideReleased.AddCondition(AnimatorConditionMode.NotEqual, grab, gestureParameter);

            AddHelpfulResultTransitions(held, success, failure);
            AddHelpfulResultTransitions(use, success, failure);

            var successDone = success.AddTransition(resultWait);
            successDone.hasExitTime = true;
            successDone.exitTime = 1f;
            successDone.duration = 0f;
            var failureDone = failure.AddTransition(resultWait);
            failureDone.hasExitTime = true;
            failureDone.exitTime = 1f;
            failureDone.duration = 0f;

            var resetDone = resultWait.AddTransition(held);
            resetDone.hasExitTime = false;
            resetDone.duration = 0f;
            resetDone.AddCondition(AnimatorConditionMode.Equals, 0f, HelpfulItemUseResultParameter);

            foreach (var activeState in new[] { grabLatch, held, use, hideLatch, success, failure, resultWait })
                AddHelpfulExitTransitions(activeState, hidden, binding.id);

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            EditorUtility.SetDirty(prop);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log(
                BuildNumber + " built physical helpful item " + binding.id + " — " + binding.name +
                " using " + binding.helpfulHand + " hand, grab " + binding.grabGesture +
                ", use " + binding.useGesture + ", Self=" + binding.helpfulAllowSelf +
                ", Others=" + binding.helpfulAllowOthers + ". FaceEmo assets were not modified.");
        }

        private void CreateItemSenders()
        {
            ActionDefinition item;
            if (!TryGetSelectedItem(out item))
                return;

            foreach (var target in GetTargets())
            {
                var host = CreateContactChild(target, "Stories Item - " + item.Id + " " + item.Name, itemStartsEnabled);
                CreateContactChild(host, "FX — Item Visuals (Place Here)", true);
                var allyHost = CreateContactChild(host, "[SoY Item Ally] " + item.Id + " " + item.Name, true);
                var enemyHost = CreateContactChild(host, "[SoY Item Enemy] " + item.Id + " " + item.Name, false);
                ConfigureActionSender(allyHost, item.Id, ItemActiveTag, ItemBitTagPrefix, "SoY Item", CasterAllyTag,
                    itemShape, itemRadius, itemHeight, itemBoxSize, itemPosition, itemRotation);
                ConfigureActionSender(enemyHost, item.Id, ItemActiveTag, ItemBitTagPrefix, "SoY Item", CasterEnemyTag,
                    itemShape, itemRadius, itemHeight, itemBoxSize, itemPosition, itemRotation);
                Selection.activeGameObject = host;
                Log("Item sender ready on '" + host.name + "': " + item.Name + " (ID " + item.Id + ", bits " + GetActionBinary(item.Id) + ").");
            }
            SyncInstalledActionAnimationProfile(ActionAnimationKind.Item);
            RebuildActionAnimationLayer(ActionAnimationKind.Item, "SoY_ItemType", ItemUseLayer, animationProfile.itemAnimations);
            var physicalBinding = animationProfile.itemAnimations.FirstOrDefault(binding => binding != null && binding.id == item.Id);
            if (physicalBinding != null && physicalBinding.physicalHelpful)
                RebuildHelpfulItemInteraction(physicalBinding);
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
                // One receiver per damage parameter, with compatibility aliases grouped into its
                // collision-tag OR list. This avoids multiple Contact Receivers fighting over the
                // same Bool when an outside sender carries more than one compatible tag.
                damageMappings.Add(new ReceiverMapping(IncomingWeakContactTags, "SoY_HitWeak"));
                damageMappings.Add(new ReceiverMapping(IncomingAverageContactTags, "SoY_HitAverage"));
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
            {
                // Protocol 20 keeps Stories alignment and generic compatibility separate.
                // Canonical SoY_DamageSourceEnemy is driven ONLY by SoY Caster Enemy.
                damageMappings.Add(new ReceiverMapping(CasterEnemyTag, DamageSourceEnemyParameter));
                if (incomingHits)
                    damageMappings.Add(new ReceiverMapping(ExternalDamageContactTags, ExternalDamageSourceParameter));
            }

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
            var tags = mapping.CollisionTags.Distinct(StringComparer.Ordinal).Take(16).ToArray();
            var receiver = EnsureReceiverForTagsAndParameter(host, FindType(ReceiverTypeName), tags, mapping.Parameter);
            if (receiver == null)
                return;
            ConfigureContact(receiver, incomingShape, incomingRadius, incomingHeight, incomingBoxSize, incomingPosition, incomingRotation, tags);
            SetBoolMember(receiver, false, "allowSelf", "AllowSelf");
            SetBoolMember(receiver, true, "allowOthers", "AllowOthers");
            SetBoolMember(receiver, true, "localOnly", "LocalOnly");
            SetStringMember(receiver, mapping.Parameter, "parameter", "Parameter");
            SetEnumMember(receiver, mapping.ReceiverType, "receiverType", "ReceiverType");
            SetFloatMember(receiver, mapping.Value, "value", "Value");
            SetFloatMember(receiver, 0f, "minVelocity", "MinVelocity");
            FinishContact(receiver);
            Log("Incoming receiver ready on '" + host.name + "': " + mapping.DisplayTags + " → " + mapping.Parameter + " = " + mapping.Value);
        }

        private Component EnsureReceiverForTagsAndParameter(
            GameObject host,
            Type receiverType,
            IEnumerable<string> tags,
            string receiverParameter)
        {
            if (host == null || receiverType == null)
                return null;

            var expectedTags = (tags ?? Enumerable.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.Ordinal)
                .Take(16)
                .ToArray();

            // TB12 migration behavior: older avatars may already have a one-tag receiver
            // for SoY_HitWeak / SoY_HitAverage / block handling. Reuse the existing
            // parameter writer and expand its collision-tag list instead of adding a
            // second receiver that could race the same Bool.
            var matchingParameterReceivers = host.GetComponents(receiverType)
                .Cast<Component>()
                .Where(existing => string.Equals(
                    ReadStringMember(existing, "parameter", "Parameter"),
                    receiverParameter,
                    StringComparison.Ordinal))
                .ToList();

            var receiver = matchingParameterReceivers.FirstOrDefault();
            if (receiver == null)
                receiver = EnsureContact(host, receiverType, expectedTags, receiverParameter);

            if (receiver == null)
                return null;

            // Remove only redundant Stories-managed receivers on the same managed host
            // that write the exact same parameter. Foreign/user-authored objects elsewhere
            // are not touched.
            if (IsStrictlyStoriesManagedHost(host))
            {
                foreach (var duplicate in matchingParameterReceivers.Skip(1).ToArray())
                {
                    if (duplicate == null || duplicate == receiver)
                        continue;
                    Undo.DestroyObjectImmediate(duplicate);
                    Log("TB12 removed a redundant managed receiver for '" + receiverParameter +
                        "' while consolidating external Contact aliases.");
                }
            }

            Undo.RecordObject(receiver, "Repair Stories External Contact Compatibility");
            return receiver;
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
            // TB6: VRChat SDK packages do not guarantee that every runtime assembly has
            // already been loaded into the AppDomain merely because the component appears
            // in Unity's Add Component menu. Prefer the documented public namespace first,
            // then Unity's TypeCache (which can discover compiled package component types),
            // then already-instantiated components and a resilient assembly scan.
            foreach (var name in ParentConstraintTypeNames)
            {
                var found = FindType(name);
                if (found != null && typeof(Component).IsAssignableFrom(found))
                    return found;
            }

            try
            {
                var cached = TypeCache.GetTypesDerivedFrom<Component>()
                    .FirstOrDefault(type => type != null &&
                                            type.Name == "VRCParentConstraint" &&
                                            (type.Namespace ?? string.Empty).StartsWith("VRC.", StringComparison.Ordinal));
                if (cached != null)
                    return cached;
            }
            catch (Exception ex)
            {
                Log("TB6 TypeCache lookup for VRCParentConstraint failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            try
            {
                var existing = Resources.FindObjectsOfTypeAll<Component>()
                    .FirstOrDefault(component => component != null &&
                                                 component.GetType().Name == "VRCParentConstraint");
                if (existing != null)
                    return existing.GetType();
            }
            catch (Exception ex)
            {
                Log("TB6 live-component lookup for VRCParentConstraint failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null).ToArray();
                }
                catch
                {
                    continue;
                }

                var found = types.FirstOrDefault(type => type != null &&
                                                        type.Name == "VRCParentConstraint" &&
                                                        typeof(Component).IsAssignableFrom(type));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static object CreateConstraintSourceEntry(Type sourceType, Transform source, float weight)
        {
            if (sourceType == null || source == null) return null;

            object entry = null;
            try
            {
                var ctor = sourceType.GetConstructor(new[] { typeof(Transform), typeof(float) });
                if (ctor != null)
                    entry = ctor.Invoke(new object[] { source, weight });
            }
            catch
            {
                entry = null;
            }

            if (entry == null)
            {
                try
                {
                    entry = Activator.CreateInstance(sourceType);
                }
                catch
                {
                    return null;
                }
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var name in new[] { "SourceTransform", "sourceTransform", "Source", "source" })
            {
                var field = sourceType.GetField(name, flags);
                if (field != null && typeof(Transform).IsAssignableFrom(field.FieldType))
                {
                    field.SetValue(entry, source);
                    break;
                }
                var property = sourceType.GetProperty(name, flags);
                if (property != null && property.CanWrite && typeof(Transform).IsAssignableFrom(property.PropertyType))
                {
                    property.SetValue(entry, source, null);
                    break;
                }
            }

            foreach (var name in new[] { "Weight", "weight" })
            {
                var field = sourceType.GetField(name, flags);
                if (field != null && field.FieldType == typeof(float))
                {
                    field.SetValue(entry, weight);
                    break;
                }
                var property = sourceType.GetProperty(name, flags);
                if (property != null && property.CanWrite && property.PropertyType == typeof(float))
                {
                    property.SetValue(entry, weight, null);
                    break;
                }
            }

            return entry;
        }

        private bool TryAssignParentConstraintSourceViaPublicApi(Component component, Transform source, float weight)
        {
            if (component == null || source == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = component.GetType();
            var property = type.GetProperty("Sources", flags) ?? type.GetProperty("sources", flags);
            if (property == null || !property.CanRead)
                return false;

            try
            {
                if (property.PropertyType.IsArray)
                {
                    var elementType = property.PropertyType.GetElementType();
                    var entry = CreateConstraintSourceEntry(elementType, source, weight);
                    if (entry == null || !property.CanWrite) return false;
                    var array = Array.CreateInstance(elementType, 1);
                    array.SetValue(entry, 0);
                    property.SetValue(component, array, null);
                    return true;
                }

                var sourcesObject = property.GetValue(component, null);
                if (sourcesObject == null && property.CanWrite)
                {
                    try
                    {
                        sourcesObject = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(component, sourcesObject, null);
                    }
                    catch
                    {
                        sourcesObject = null;
                    }
                }
                if (sourcesObject == null) return false;

                var listType = sourcesObject.GetType();
                var clear = listType.GetMethod("Clear", flags, null, Type.EmptyTypes, null);
                var add = listType.GetMethods(flags)
                    .FirstOrDefault(method => method.Name == "Add" && method.GetParameters().Length == 1);
                if (add == null) return false;

                var sourceType = add.GetParameters()[0].ParameterType;
                var sourceEntry = CreateConstraintSourceEntry(sourceType, source, weight);
                if (sourceEntry == null) return false;

                clear?.Invoke(sourcesObject, null);
                add.Invoke(sourcesObject, new[] { sourceEntry });

                // Some SDK revisions expose a mutable keyable-list object; others use a
                // settable wrapper. Reassign when possible so both forms are covered.
                if (property.CanWrite)
                    property.SetValue(component, sourcesObject, null);
                return true;
            }
            catch (Exception ex)
            {
                Log("TB6 public Sources API configuration failed on " + type.FullName + ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool TryAssignParentConstraintSourceSerialized(Component component, Transform source, float weight)
        {
            if (component == null || source == null) return false;
            try
            {
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

                if (sources == null) return false;
                sources.arraySize = 1;
                var element = sources.GetArrayElementAtIndex(0);
                var assigned = false;
                if (element.propertyType == SerializedPropertyType.ObjectReference)
                {
                    element.objectReferenceValue = source;
                    assigned = true;
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
                            assigned = true;
                        }
                        else if (cursor.propertyType == SerializedPropertyType.Float &&
                                 cursor.name.IndexOf("weight", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cursor.floatValue = weight;
                        }
                    }
                }

                serialized.ApplyModifiedProperties();
                return assigned;
            }
            catch (Exception ex)
            {
                Log("TB6 serialized Sources fallback failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool ConfigureParentConstraintCommon(Component component, Transform source, float weight, bool maintainOffset, bool worldDrop)
        {
            if (component == null || source == null) return false;
            Undo.RecordObject(component, worldDrop ? "Configure Stories Spell World Drop Constraint" : "Configure Stories VRC Parent Constraint");

            var sourceAssigned = TryAssignParentConstraintSourceViaPublicApi(component, source, weight);
            if (!sourceAssigned)
                sourceAssigned = TryAssignParentConstraintSourceSerialized(component, source, weight);
            if (!sourceAssigned)
                return false;

            SetFloatMember(component, weight, "GlobalWeight", "globalWeight", "Weight", "weight");
            SetBoolMember(component, true, "IsActive", "isActive", "Active", "active");
            SetBoolMember(component, maintainOffset, "Locked", "locked", "IsLocked", "isLocked");
            SetBoolMember(component, false, "SolveInLocalSpace", "solveInLocalSpace");

            // Freeze To World only affects axes the constraint itself is configured to
            // evaluate. Explicitly enable all parent position/rotation axes for a full
            // world drop rather than trusting SDK defaults.
            if (worldDrop)
            {
                foreach (var member in new[]
                {
                    "AffectsPositionX", "AffectsPositionY", "AffectsPositionZ",
                    "AffectsRotationX", "AffectsRotationY", "AffectsRotationZ"
                })
                    SetBoolMember(component, true, member, char.ToLowerInvariant(member[0]) + member.Substring(1));

                SetBoolMember(component, false, "FreezeToWorld", "freezeToWorld");
                SetBoolMember(component, false, "RebakeOffsetsWhenUnfrozen", "rebakeOffsetsWhenUnfrozen");
            }

            InvokeNoArg(component, "ApplyConfigurationChanges");
            if (maintainOffset)
                InvokeNoArg(component, "ActivateConstraint");
            else
                InvokeNoArg(component, "ZeroConstraint");
            InvokeNoArg(component, "ApplyConfigurationChanges");

            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            return true;
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

        private bool ConfigureParentConstraintViaDocumentedSdkApi(
            VRCParentConstraint component,
            Transform source,
            float weight,
            bool maintainOffset,
            bool worldDrop)
        {
            if (component == null || source == null) return false;
            Undo.RecordObject(component, worldDrop ? "Configure Stories Spell World Drop Constraint" : "Configure Stories VRC Parent Constraint");

            try
            {
                // TB10: use the exact public API documented by VRChat and preserve the original zero offset on unfreeze instead of rebaking the dropped world offset.
                // Sources is mutable even though the property itself is not settable.
                while (component.Sources.Count > 0)
                    component.Sources.RemoveAt(component.Sources.Count - 1);

                var sourceEntry = new VRCConstraintSource(source, Mathf.Clamp01(weight));
                component.Sources.Add(sourceEntry);

                component.GlobalWeight = Mathf.Clamp01(weight);
                component.IsActive = true;
                component.SolveInLocalSpace = false;

                if (worldDrop)
                {
                    component.FreezeToWorld = false;
                    component.RebakeOffsetsWhenUnfrozen = false;
                    // IMPORTANT: keep this false. VRChat rebakes the current dropped offset when unfreezing if true, which makes later following remain displaced. False restores the original zero offset and snaps the carrier back onto the live ground-result source.

                    // FreezeToWorld only locks the axes affected by the constraint.
                    // Keep reflection here only for the six axis toggles because SDK
                    // revisions have renamed those editor-facing members before; the
                    // critical component/source API above is now strongly typed.
                    foreach (var member in new[]
                    {
                        "AffectsPositionX", "AffectsPositionY", "AffectsPositionZ",
                        "AffectsRotationX", "AffectsRotationY", "AffectsRotationZ"
                    })
                        SetBoolMember(component, true, member, char.ToLowerInvariant(member[0]) + member.Substring(1));
                }

                component.ApplyConfigurationChanges();
                if (maintainOffset)
                    component.ActivateConstraint();
                else
                    component.ZeroConstraint();
                component.ApplyConfigurationChanges();

                if (component.Sources.Count != 1)
                {
                    Log("TB10 direct SDK Sources API returned " + component.Sources.Count + " source(s) after adding exactly one source.");
                    return false;
                }

                var configuredSource = component.Sources[0];
                if (configuredSource.SourceTransform != source)
                {
                    Log("TB10 direct SDK Sources API added a source entry, but SourceTransform did not match '" + source.name + "'.");
                    return false;
                }

                EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                Log("TB10 direct SDK Sources API configured " + component.GetType().FullName +
                    " with source '" + source.name + "' (weight " + configuredSource.Weight.ToString("0.###") + ").");
                return true;
            }
            catch (Exception ex)
            {
                Log("ERROR: TB10 direct SDK Parent Constraint configuration failed on " + component.GetType().FullName +
                    ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private Component ConfigureWorldDropParentConstraint(GameObject host, Transform source)
        {
            if (host == null || source == null) return null;

            VRCParentConstraint component = host.GetComponent<VRCParentConstraint>();
            if (component == null)
                component = Undo.AddComponent<VRCParentConstraint>(host);

            if (component == null)
            {
                Log("ERROR: TB10 Unity refused to add VRCParentConstraint to '" + host.name + "'.");
                return null;
            }

            Log("TB10 using documented VRCParentConstraint API: " + component.GetType().AssemblyQualifiedName);
            if (!ConfigureParentConstraintViaDocumentedSdkApi(component, source, 1f, false, true))
            {
                Log("ERROR: TB10 created VRCParentConstraint but could not configure Sources via the documented SDK API for '" + source.name + "'. The incomplete component was removed to avoid a source-less world lock.");
                Undo.DestroyObjectImmediate(component);
                return null;
            }

            Log("World Drop constraint ready on '" + host.name + "' with source '" + source.name + "'. Freeze To World is driven by the Spell selector while cast is active.");
            return component;
        }

        private bool ConfigureVRCParentConstraint(GameObject host, Transform source)
        {
            if (host == null || source == null) return false;

            VRCParentConstraint component = host.GetComponent<VRCParentConstraint>();
            if (component == null)
                component = Undo.AddComponent<VRCParentConstraint>(host);
            if (component == null) return false;

            var ok = ConfigureParentConstraintViaDocumentedSdkApi(
                component,
                source,
                Mathf.Clamp01(constraintWeight),
                constraintMaintainOffset,
                false);
            if (!ok)
            {
                Log("TB10 VRC Parent Constraint was added, but the documented Sources API could not configure source '" + source.name + "'.");
                Undo.DestroyObjectImmediate(component);
            }
            return ok;
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
                case SpellSchool.LightMagick: return "Light Magick";
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
                case SpellSchool.LightMagick: return incomingLightSpells;
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
            incomingLightSpells = value;
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

            var avatarName = MakeSafeAssetName(avatarRoot.name);
            var animationFolder = AvatarGeneratedFolder(avatarName, "Animations/System");
            EnsureAssetFolder(animationFolder);
            var readyClip = CreateOrReplaceActiveClip(
                animationFolder + "/SoY_IFrames_Ready.anim", hosts, true, 1f / 60f);
            var cooldownClip = CreateOrReplaceActiveClip(
                animationFolder + "/SoY_IFrames_1s.anim", hosts, false, HitIFrameSeconds);

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
                var receiverType = FindType(ReceiverTypeName);
                if (receiverType != null)
                {
                    foreach (var receiver in host.GetComponents(receiverType).Cast<Component>()
                        .Where(component => string.Equals(ReadStringMember(component, "parameter", "Parameter"), DamageSourceEnemyParameter, StringComparison.Ordinal))
                        .ToArray())
                    {
                        RepairIncomingReceiverMapping(receiver);
                    }
                }
                ConfigureIncomingReceiver(host.gameObject, new ReceiverMapping(CasterEnemyTag, DamageSourceEnemyParameter));
                ConfigureIncomingReceiver(host.gameObject, new ReceiverMapping(ExternalDamageContactTags, ExternalDamageSourceParameter));
            }
            // Existing v0.5.5 and earlier avatars may still start I-Frames from raw hit
            // receivers. Rebuild the layer so only Sam.py-accepted SoY_Damaged pulses
            // can begin the one-second protection window.
            RebuildIFrameLayer();
            RebuildManagedActionContactGate(ActionAnimationKind.Spell);
            RebuildManagedActionContactGate(ActionAnimationKind.Technick);
            RebuildManagedActionContactGate(ActionAnimationKind.Item);
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

            var avatarName = MakeSafeAssetName(avatarRoot.name);
            var animationFolder = AvatarGeneratedFolder(avatarName, "Animations/System");
            EnsureAssetFolder(animationFolder);
            var allyClip = CreateOrReplaceAlignmentClip(
                animationFolder + "/SoY_Action_Ally.anim", allyHosts, enemyHosts, false);
            var enemyClip = CreateOrReplaceAlignmentClip(
                animationFolder + "/SoY_Action_Enemy.anim", allyHosts, enemyHosts, true);
            var koClip = CreateOrReplaceActiveClip(
                animationFolder + "/SoY_Action_KO_Lock.anim", allyHosts.Concat(enemyHosts), false, 1f / 60f);

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

        private AnimationClip CreateOrReplaceWorldDropClip(
            string path,
            GameObject actionHost,
            Component worldDropConstraint,
            bool actionActive,
            bool freezeToWorld,
            float length)
        {
            var clip = LoadOrCreateClip(path);
            clip.ClearCurves();
            var endTime = Mathf.Max(1f / 60f, length);

            if (actionHost != null)
            {
                var actionPath = GetRelativePath(avatarRoot.transform, actionHost.transform);
                if (actionPath != null)
                {
                    var activeBinding = EditorCurveBinding.FloatCurve(actionPath, typeof(GameObject), "m_IsActive");
                    AnimationUtility.SetEditorCurve(clip, activeBinding, new AnimationCurve(
                        new Keyframe(0f, actionActive ? 1f : 0f),
                        new Keyframe(endTime, actionActive ? 1f : 0f)));
                }
            }

            if (worldDropConstraint != null)
            {
                var constraintPath = GetRelativePath(avatarRoot.transform, worldDropConstraint.transform);
                if (constraintPath != null)
                {
                    // VRChat exposes this exact animatable property name on its constraints.
                    // VRCFury also detects world-constrained animation bindings by FreezeToWorld.
                    var freezeBinding = EditorCurveBinding.FloatCurve(
                        constraintPath,
                        worldDropConstraint.GetType(),
                        "FreezeToWorld");
                    AnimationUtility.SetEditorCurve(clip, freezeBinding, new AnimationCurve(
                        new Keyframe(0f, freezeToWorld ? 1f : 0f),
                        new Keyframe(endTime, freezeToWorld ? 1f : 0f)));
                }
            }

            EditorUtility.SetDirty(clip);
            return clip;
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
            var expectedName = Path.GetFileNameWithoutExtension(path);
            if (clip != null)
            {
                if (clip.name != expectedName)
                {
                    clip.name = expectedName;
                    EditorUtility.SetDirty(clip);
                }
                return clip;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("Generated animation path is occupied by a non-AnimationClip asset: " + path);

            clip = new AnimationClip { frameRate = 60f, name = expectedName };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private sealed class AnimatorIntegrityReport
        {
            public int LayerCount;
            public int ReachableObjectCount;
            public int OrphanTransitionCount;
            public readonly List<string> Errors = new List<string>();

            public bool IsValid
            {
                get { return OrphanTransitionCount == 0 && Errors.Count == 0; }
            }

            public string Summary
            {
                get
                {
                    if (IsValid)
                        return "Healthy — " + LayerCount + " layer(s), " + ReachableObjectCount + " reachable graph object(s), no orphan transitions.";
                    var parts = new List<string>();
                    if (OrphanTransitionCount > 0)
                        parts.Add(OrphanTransitionCount + " unreachable transition subasset(s)");
                    if (Errors.Count > 0)
                        parts.Add(Errors.Count + " live graph error(s): " + string.Join(" | ", Errors.Take(4).ToArray()));
                    return string.Join("; ", parts.ToArray());
                }
            }
        }

        private static HashSet<UnityEngine.Object> CollectReachableAnimatorObjects(AnimatorController controller)
        {
            var reachable = new HashSet<UnityEngine.Object>();
            if (controller == null)
                return reachable;

            foreach (var layer in controller.layers)
                CollectReachableAnimatorObjects(layer.stateMachine, reachable);
            return reachable;
        }

        private static void CollectReachableAnimatorObjects(AnimatorStateMachine machine, HashSet<UnityEngine.Object> reachable)
        {
            if (machine == null || reachable == null || !reachable.Add(machine))
                return;

            foreach (var child in machine.states)
            {
                var state = child.state;
                if (state == null)
                    continue;
                reachable.Add(state);

                foreach (var behaviour in state.behaviours ?? Array.Empty<StateMachineBehaviour>())
                    if (behaviour != null)
                        reachable.Add(behaviour);

                foreach (var transition in state.transitions ?? Array.Empty<AnimatorStateTransition>())
                    if (transition != null)
                        reachable.Add(transition);
            }

            foreach (var transition in machine.anyStateTransitions ?? Array.Empty<AnimatorStateTransition>())
                if (transition != null)
                    reachable.Add(transition);

            foreach (var transition in machine.entryTransitions ?? Array.Empty<AnimatorTransition>())
                if (transition != null)
                    reachable.Add(transition);

            foreach (var childMachine in machine.stateMachines)
                if (childMachine.stateMachine != null)
                    CollectReachableAnimatorObjects(childMachine.stateMachine, reachable);
        }

        private static List<UnityEngine.Object> FindOrphanedAnimatorTransitions(AnimatorController controller)
        {
            var result = new List<UnityEngine.Object>();
            if (controller == null)
                return result;

            var path = AssetDatabase.GetAssetPath(controller);
            if (string.IsNullOrWhiteSpace(path))
                return result;

            var reachable = CollectReachableAnimatorObjects(controller);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset == null || reachable.Contains(asset))
                    continue;
                if (asset is AnimatorStateTransition || asset is AnimatorTransition)
                    result.Add(asset);
            }
            return result;
        }

        private static void ValidateStateMachineIntegrity(
            AnimatorStateMachine machine,
            string path,
            AnimatorIntegrityReport report,
            HashSet<AnimatorStateMachine> visited)
        {
            if (machine == null)
            {
                report.Errors.Add(path + " has a missing state machine.");
                return;
            }
            if (!visited.Add(machine))
                return;

            var directStates = machine.states
                .Select(child => child.state)
                .Where(state => state != null)
                .ToArray();

            if (machine.states.Any(child => child.state == null))
                report.Errors.Add(path + " contains a missing state reference.");

            if (directStates.Length > 0)
            {
                if (machine.defaultState == null)
                    report.Errors.Add(path + " has states but no default state.");
                else if (!directStates.Contains(machine.defaultState))
                    report.Errors.Add(path + " default state is not one of its direct child states.");
            }

            foreach (var state in directStates)
            {
                foreach (var transition in state.transitions ?? Array.Empty<AnimatorStateTransition>())
                {
                    if (transition == null)
                    {
                        report.Errors.Add(path + "/" + state.name + " contains a missing transition reference.");
                        continue;
                    }
                    if (!transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                        report.Errors.Add(path + "/" + state.name + " has a transition with no live destination.");
                }
            }

            foreach (var transition in machine.anyStateTransitions ?? Array.Empty<AnimatorStateTransition>())
            {
                if (transition == null)
                {
                    report.Errors.Add(path + " Any State contains a missing transition reference.");
                    continue;
                }
                if (!transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                    report.Errors.Add(path + " Any State has a transition with no live destination.");
            }

            foreach (var transition in machine.entryTransitions ?? Array.Empty<AnimatorTransition>())
            {
                if (transition == null)
                {
                    report.Errors.Add(path + " Entry contains a missing transition reference.");
                    continue;
                }
                if (transition.destinationState == null && transition.destinationStateMachine == null)
                    report.Errors.Add(path + " Entry has a transition with no live destination.");
            }

            foreach (var child in machine.stateMachines)
            {
                if (child.stateMachine == null)
                {
                    report.Errors.Add(path + " contains a missing child state-machine reference.");
                    continue;
                }
                ValidateStateMachineIntegrity(child.stateMachine, path + "/" + child.stateMachine.name, report, visited);
            }
        }

        private static AnimatorIntegrityReport InspectAnimatorControllerIntegrity(AnimatorController controller)
        {
            var report = new AnimatorIntegrityReport();
            if (controller == null)
            {
                report.Errors.Add("No FX AnimatorController is assigned.");
                return report;
            }

            report.LayerCount = controller.layers.Length;
            var visited = new HashSet<AnimatorStateMachine>();
            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine == null)
                {
                    report.Errors.Add("Layer '" + layer.name + "' has no state machine.");
                    continue;
                }
                ValidateStateMachineIntegrity(layer.stateMachine, layer.name, report, visited);
            }

            report.ReachableObjectCount = CollectReachableAnimatorObjects(controller).Count;
            report.OrphanTransitionCount = FindOrphanedAnimatorTransitions(controller).Count;
            return report;
        }

        private static int CleanupOrphanedAnimatorTransitions(AnimatorController controller)
        {
            if (controller == null)
                return 0;

            var orphaned = FindOrphanedAnimatorTransitions(controller);
            foreach (var transition in orphaned)
            {
                if (transition != null)
                    Undo.DestroyObjectImmediate(transition);
            }

            if (orphaned.Count > 0)
                EditorUtility.SetDirty(controller);
            return orphaned.Count;
        }

        private string CreateAnimatorIntegrityBackup(string reason)
        {
            if (fxController == null)
                return string.Empty;

            var sourcePath = AssetDatabase.GetAssetPath(fxController);
            if (string.IsNullOrWhiteSpace(sourcePath))
                return string.Empty;

            var folder = CurrentAvatarGeneratedFolder("Backups/Animator Integrity");
            EnsureAssetFolder(folder);
            var safeReason = MakeSafeAssetName(string.IsNullOrWhiteSpace(reason) ? "repair" : reason);
            var targetPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + MakeSafeAssetName(fxController.name) + "_pre_" + BuildNumber.Replace(".", "_") + "_" + safeReason + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".controller");

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
                throw new InvalidOperationException("Could not create the Animator integrity backup at '" + targetPath + "'.");

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            operationLog.Insert(0, "Created full FX controller safety backup: " + targetPath);
            return targetPath;
        }

        private void RepairAnimatorControllerIntegrity()
        {
            if (fxController == null)
            {
                EditorUtility.DisplayDialog("Animator Integrity", "Assign an FX AnimatorController first.", "OK");
                return;
            }

            var before = InspectAnimatorControllerIntegrity(fxController);
            var orphanCount = before.OrphanTransitionCount;
            if (orphanCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Animator Integrity",
                    before.Errors.Count == 0
                        ? "The controller graph is healthy. No unreachable transition subassets were found."
                        : "No safe orphan-transition cleanup is available. Live graph errors remain and were not modified:\n\n" + before.Summary,
                    "OK");
                return;
            }

            lastAnimatorIntegrityBackupPath = CreateAnimatorIntegrityBackup("integrity_repair");
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Stories OSC Animator Integrity Repair");

            try
            {
                Undo.RecordObject(fxController, "Preserve FX Controller Before Integrity Repair");
                var removed = CleanupOrphanedAnimatorTransitions(fxController);
                var after = InspectAnimatorControllerIntegrity(fxController);
                if (!after.IsValid)
                    throw new InvalidOperationException(after.Summary);

                EditorUtility.SetDirty(fxController);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(group);

                operationLog.Insert(0, "Animator integrity repair removed " + removed + " unreachable transition subasset(s). Validation passed before save.");
                managedRepairPreview = "Animator integrity repaired: removed " + removed + " unreachable transition subasset(s). Backup: " + lastAnimatorIntegrityBackupPath;
                EditorUtility.DisplayDialog(
                    "Animator Integrity Repaired",
                    "Removed " + removed + " unreachable transition subasset(s).\n\nThe live Animator graph passed validation before save.\n\nBackup:\n" + lastAnimatorIntegrityBackupPath,
                    "OK");
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                try
                {
                    EditorUtility.SetDirty(fxController);
                    AssetDatabase.SaveAssets();
                }
                catch (Exception saveException)
                {
                    Debug.LogException(saveException);
                }
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Animator Integrity Repair Rolled Back",
                    "The controller was not committed because validation still failed.\n\n" + exception.Message + "\n\nBackup:\n" + lastAnimatorIntegrityBackupPath,
                    "OK");
            }
        }

        private static int AnimatorSubAssetDestroyPriority(UnityEngine.Object obj)
        {
            if (obj is AnimatorStateTransition || obj is AnimatorTransition) return 0;
            if (obj is StateMachineBehaviour) return 1;
            if (obj is AnimatorState) return 2;
            if (obj is AnimatorStateMachine) return 3;
            return 4;
        }

        private static void RemoveLayerByName(AnimatorController controller, string layerName)
        {
            if (controller == null || string.IsNullOrWhiteSpace(layerName))
                return;

            for (var index = controller.layers.Length - 1; index >= 0; index--)
            {
                var layer = controller.layers[index];
                if (layer.name != layerName)
                    continue;

                var ownedGraph = new HashSet<UnityEngine.Object>();
                CollectReachableAnimatorObjects(layer.stateMachine, ownedGraph);
                var controllerPath = AssetDatabase.GetAssetPath(controller);

                Undo.RecordObject(controller, "Remove Stories Animator Layer");
                controller.RemoveLayer(index);

                // A controller layer is only an array entry; removing it does not guarantee
                // that its embedded states/transitions are removed from the .controller asset.
                // Destroy only graph objects that became unreachable after the layer was removed.
                var stillReachable = CollectReachableAnimatorObjects(controller);
                foreach (var obj in ownedGraph
                    .Where(obj => obj != null && !stillReachable.Contains(obj))
                    .OrderBy(AnimatorSubAssetDestroyPriority)
                    .ToArray())
                {
                    if (obj == controller)
                        continue;
                    var objectPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrWhiteSpace(objectPath) &&
                        string.Equals(objectPath, controllerPath, StringComparison.Ordinal))
                        Undo.DestroyObjectImmediate(obj);
                }

                EditorUtility.SetDirty(controller);
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

        private static int[] CurrentToolVersionParts()
        {
            var parts = ParseVersion(Version);
            return new[]
            {
                parts.Length > 0 ? parts[0] : 0,
                parts.Length > 1 ? parts[1] : 0,
                parts.Length > 2 ? parts[2] : 0
            };
        }

        private static int[] CurrentToolBuildParts()
        {
            var raw = (BuildNumber ?? string.Empty).Trim();
            if (raw.StartsWith("TB", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(2);
            var parts = raw.Split('.');
            int tb;
            int revision;
            if (!int.TryParse(parts.Length > 0 ? parts[0] : "0", out tb)) tb = 0;
            if (!int.TryParse(parts.Length > 1 ? parts[1] : "0", out revision)) revision = 0;
            return new[] { tb, revision };
        }

        private string ManagedSchemaCoreFailureSummary()
        {
            if (fxController == null)
                return "FX AnimatorController is not assigned.";
            if (expressionParameters == null)
                return "Expression Parameters asset is not assigned.";
            if (!managedRepairAuditReady)
                return "Managed-system audit has not completed.";

            var problems = new List<string>();
            var animatorMap = fxController.parameters
                .GroupBy(parameter => parameter.name)
                .ToDictionary(group => group.Key, group => group.First().type, StringComparer.Ordinal);
            foreach (var spec in BridgeParameters)
            {
                AnimatorControllerParameterType animatorType;
                if (!animatorMap.TryGetValue(spec.Name, out animatorType))
                    problems.Add("Animator missing " + spec.Name);
                else if (animatorType != spec.AnimatorType)
                    problems.Add("Animator " + spec.Name + " is " + animatorType + ", expected " + spec.AnimatorType);
            }

            var expressionMap = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>())
                .Where(parameter => parameter != null)
                .GroupBy(parameter => parameter.name)
                .ToDictionary(group => group.Key, group => group.First().valueType, StringComparer.Ordinal);
            foreach (var spec in BridgeParameters)
            {
                VRCExpressionParameters.ValueType expressionType;
                if (!expressionMap.TryGetValue(spec.Name, out expressionType))
                    problems.Add("Expression missing " + spec.Name);
                else if (expressionType != spec.ExpressionType)
                    problems.Add("Expression " + spec.Name + " is " + expressionType + ", expected " + spec.ExpressionType);
            }

            foreach (var finding in managedRepairFindings.Where(finding =>
                finding.Kind != ManagedRepairKind.UnityCompatibilityMarker &&
                (finding.State == ManagedRepairState.Outdated ||
                 finding.State == ManagedRepairState.Repairable ||
                 finding.State == ManagedRepairState.Broken ||
                 finding.State == ManagedRepairState.Defunct)).Take(8))
            {
                problems.Add((finding.Role ?? finding.Kind.ToString()) + " still requires repair");
            }

            return problems.Count == 0
                ? "No core-schema failure detected."
                : string.Join(" | ", problems.Take(16).ToArray());
        }

        private bool ManagedSchemaCoreIsValid()
        {
            if (fxController == null || expressionParameters == null || !managedRepairAuditReady)
                return false;

            var animatorParameters = fxController.parameters
                .GroupBy(x => x.name)
                .ToDictionary(group => group.Key, group => group.First().type, StringComparer.Ordinal);
            foreach (var spec in BridgeParameters)
            {
                AnimatorControllerParameterType type;
                if (!animatorParameters.TryGetValue(spec.Name, out type) || type != spec.AnimatorType)
                    return false;
            }

            var expressionMap = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>())
                .Where(x => x != null)
                .GroupBy(x => x.name)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var spec in BridgeParameters)
            {
                VRCExpressionParameters.Parameter parameter;
                if (!expressionMap.TryGetValue(spec.Name, out parameter) || parameter.valueType != spec.ExpressionType)
                    return false;
            }

            return !managedRepairFindings.Any(finding =>
                finding.Kind != ManagedRepairKind.UnityCompatibilityMarker &&
                (finding.State == ManagedRepairState.Outdated ||
                 finding.State == ManagedRepairState.Repairable ||
                 finding.State == ManagedRepairState.Broken ||
                 finding.State == ManagedRepairState.Defunct));
        }

        private bool CurrentSchemaIsValid()
        {
            return ManagedSchemaCoreIsValid() && HasCurrentCompatibilityMarkerLayer();
        }

        private static bool IsUnityMarkerParameterName(string name)
        {
            return name == UnityToolPresentParameter ||
                   name == UnityToolMajorParameter ||
                   name == UnityToolMinorParameter ||
                   name == UnityToolPatchParameter ||
                   name == UnityToolTbParameter ||
                   name == UnityToolTbRevisionParameter ||
                   name == ProtocolVersionParameter ||
                   name == UnitySchemaValidParameter ||
                   name == UnityMarkerBeaconParameter;
        }

        private int RepairUnityMarkerParameterContract()
        {
            var repaired = 0;
            var markerSpecs = BridgeParameters.Where(spec => IsUnityMarkerParameterName(spec.Name)).ToArray();

            if (fxController != null)
            {
                Undo.RecordObject(fxController, "Repair Stories Unity Marker Parameters");
                var parameters = fxController.parameters.ToList();
                foreach (var spec in markerSpecs)
                {
                    var matches = parameters.Select((parameter, index) => new { parameter, index })
                        .Where(entry => entry.parameter.name == spec.Name).ToList();
                    var keepIndex = matches.Where(entry => entry.parameter.type == spec.AnimatorType)
                        .Select(entry => entry.index).DefaultIfEmpty(-1).First();
                    for (var index = matches.Count - 1; index >= 0; index--)
                    {
                        var entry = matches[index];
                        if (entry.index == keepIndex) continue;
                        parameters.RemoveAt(entry.index);
                        repaired++;
                        if (entry.index < keepIndex) keepIndex--;
                    }
                    if (keepIndex < 0)
                    {
                        parameters.Add(new AnimatorControllerParameter
                        {
                            name = spec.Name,
                            type = spec.AnimatorType,
                            defaultBool = spec.DefaultValue > 0.5f,
                            defaultFloat = spec.DefaultValue,
                            defaultInt = Mathf.RoundToInt(spec.DefaultValue)
                        });
                        repaired++;
                    }
                }
                fxController.parameters = parameters.ToArray();
                EditorUtility.SetDirty(fxController);
            }

            if (expressionParameters != null)
            {
                Undo.RecordObject(expressionParameters, "Repair Stories Unity Marker Expression Parameters");
                var parameters = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>())
                    .Where(parameter => parameter != null).ToList();
                foreach (var spec in markerSpecs)
                {
                    var matches = parameters.Select((parameter, index) => new { parameter, index })
                        .Where(entry => entry.parameter.name == spec.Name).ToList();
                    var keepIndex = matches.Where(entry => entry.parameter.valueType == spec.ExpressionType)
                        .Select(entry => entry.index).DefaultIfEmpty(-1).First();
                    for (var index = matches.Count - 1; index >= 0; index--)
                    {
                        var entry = matches[index];
                        if (entry.index == keepIndex) continue;
                        parameters.RemoveAt(entry.index);
                        repaired++;
                        if (entry.index < keepIndex) keepIndex--;
                    }
                    if (keepIndex < 0)
                    {
                        parameters.Add(new VRCExpressionParameters.Parameter
                        {
                            name = spec.Name,
                            valueType = spec.ExpressionType,
                            defaultValue = spec.DefaultValue,
                            saved = spec.Saved,
                            networkSynced = false
                        });
                        repaired++;
                    }
                    else
                    {
                        var parameter = parameters[keepIndex];
                        if (!Mathf.Approximately(parameter.defaultValue, spec.DefaultValue))
                        { parameter.defaultValue = spec.DefaultValue; repaired++; }
                        if (parameter.saved != spec.Saved)
                        { parameter.saved = spec.Saved; repaired++; }
                        if (parameter.networkSynced)
                        { parameter.networkSynced = false; repaired++; }
                    }
                }
                expressionParameters.parameters = parameters.ToArray();
                EditorUtility.SetDirty(expressionParameters);
            }

            return repaired;
        }

        private static bool DriverPublishesValue(
            VRCAvatarParameterDriver driver,
            string parameterName,
            float expectedValue)
        {
            if (driver == null || driver.parameters == null)
                return false;
            return driver.parameters.Any(parameter =>
                parameter != null &&
                string.Equals(parameter.name, parameterName, StringComparison.Ordinal) &&
                parameter.type == VRC_AvatarParameterDriver.ChangeType.Set &&
                Mathf.Approximately(parameter.value, expectedValue));
        }

        private static bool MarkerStatePublishes(AnimatorState state, bool schemaValid, int beaconValue)
        {
            if (state == null)
                return false;

            var driver = state.behaviours
                .OfType<VRCAvatarParameterDriver>()
                .FirstOrDefault();
            if (driver == null || !driver.localOnly)
                return false;

            var version = CurrentToolVersionParts();
            var build = CurrentToolBuildParts();

            return DriverPublishesValue(driver, UnityToolPresentParameter, 1f) &&
                   DriverPublishesValue(driver, UnityToolMajorParameter, version[0]) &&
                   DriverPublishesValue(driver, UnityToolMinorParameter, version[1]) &&
                   DriverPublishesValue(driver, UnityToolPatchParameter, version[2]) &&
                   DriverPublishesValue(driver, UnityToolTbParameter, build[0]) &&
                   DriverPublishesValue(driver, UnityToolTbRevisionParameter, build[1]) &&
                   DriverPublishesValue(driver, ProtocolVersionParameter, OscProtocolVersion) &&
                   DriverPublishesValue(driver, UnitySchemaValidParameter, schemaValid ? 1f : 0f) &&
                   DriverPublishesValue(driver, UnityMarkerBeaconParameter, beaconValue);
        }

        private bool HasCurrentCompatibilityMarkerStructure()
        {
            if (fxController == null || expressionParameters == null)
                return false;

            var layers = fxController.layers.Where(layer => layer.name == UnityMarkerLayer).ToArray();
            if (layers.Length != 1 || layers[0].stateMachine == null)
                return false;

            var animatorBeacon = fxController.parameters.Any(parameter =>
                parameter.name == UnityMarkerBeaconParameter &&
                parameter.type == AnimatorControllerParameterType.Int);
            var expressionBeacon = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>())
                .Any(parameter => parameter != null &&
                    parameter.name == UnityMarkerBeaconParameter &&
                    parameter.valueType == VRCExpressionParameters.ValueType.Int);
            if (!animatorBeacon || !expressionBeacon)
                return false;

            var states = layers[0].stateMachine.states
                .Select(child => child.state)
                .Where(state => state != null)
                .ToArray();

            var validA = states.FirstOrDefault(state => state.name == UnityMarkerStateA);
            var validB = states.FirstOrDefault(state => state.name == UnityMarkerStateB);
            var invalid = states.FirstOrDefault(state => state.name == UnityMarkerStateInvalid);

            var validPair = MarkerStatePublishes(validA, true, 121) &&
                            MarkerStatePublishes(validB, true, 122);
            var invalidMarker = MarkerStatePublishes(invalid, false, 0);
            return validPair || invalidMarker;
        }

        private bool HasCurrentCompatibilityMarkerLayer()
        {
            if (fxController == null || expressionParameters == null)
                return false;

            var layers = fxController.layers.Where(layer => layer.name == UnityMarkerLayer).ToArray();
            if (layers.Length != 1 || layers[0].stateMachine == null)
                return false;

            var animatorBeacon = fxController.parameters.Any(parameter =>
                parameter.name == UnityMarkerBeaconParameter &&
                parameter.type == AnimatorControllerParameterType.Int);
            var expressionBeacon = (expressionParameters.parameters ?? Array.Empty<VRCExpressionParameters.Parameter>())
                .Any(parameter => parameter != null &&
                    parameter.name == UnityMarkerBeaconParameter &&
                    parameter.valueType == VRCExpressionParameters.ValueType.Int);
            if (!animatorBeacon || !expressionBeacon)
                return false;

            var states = layers[0].stateMachine.states
                .Select(child => child.state)
                .Where(state => state != null)
                .ToArray();
            var stateA = states.FirstOrDefault(state => state.name == UnityMarkerStateA);
            var stateB = states.FirstOrDefault(state => state.name == UnityMarkerStateB);
            if (!MarkerStatePublishes(stateA, true, 121) ||
                !MarkerStatePublishes(stateB, true, 122))
                return false;

            var machine = layers[0].stateMachine;
            if (machine.defaultState != stateA)
                return false;
            var aToB = stateA.transitions != null && stateA.transitions.Any(transition => transition != null && transition.destinationState == stateB);
            var bToA = stateB.transitions != null && stateB.transitions.Any(transition => transition != null && transition.destinationState == stateA);
            return aToB && bToA;
        }

        private static void ConfigureUnityMarkerDriver(AnimatorState state, bool schemaValid, int beaconValue)
        {
            if (state == null)
                return;
            var version = CurrentToolVersionParts();
            var build = CurrentToolBuildParts();
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.localOnly = true;
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
            {
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolPresentParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = 1f },
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolMajorParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = version[0] },
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolMinorParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = version[1] },
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolPatchParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = version[2] },
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolTbParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = build[0] },
                new VRC_AvatarParameterDriver.Parameter { name = UnityToolTbRevisionParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = build[1] },
                new VRC_AvatarParameterDriver.Parameter { name = ProtocolVersionParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = OscProtocolVersion },
                new VRC_AvatarParameterDriver.Parameter { name = UnitySchemaValidParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = schemaValid ? 1f : 0f },
                new VRC_AvatarParameterDriver.Parameter { name = UnityMarkerBeaconParameter, type = VRC_AvatarParameterDriver.ChangeType.Set, value = beaconValue },
            };
        }

        private void RebuildUnityToolMarkerLayer(bool schemaValid)
        {
            if (fxController == null)
                return;

            foreach (var spec in BridgeParameters.Where(spec =>
                spec.Name == UnityToolPresentParameter ||
                spec.Name == UnityToolMajorParameter ||
                spec.Name == UnityToolMinorParameter ||
                spec.Name == UnityToolPatchParameter ||
                spec.Name == UnityToolTbParameter ||
                spec.Name == UnityToolTbRevisionParameter ||
                spec.Name == ProtocolVersionParameter ||
                spec.Name == UnitySchemaValidParameter ||
                spec.Name == UnityMarkerBeaconParameter))
            {
                EnsureAnimatorParameter(fxController, spec.Name, spec.AnimatorType);
            }

            RemoveLayerByName(fxController, UnityMarkerLayer);
            var layer = CreateHookLayer(fxController, UnityMarkerLayer);
            var folder = CurrentAvatarGeneratedFolder("Animations/System");
            EnsureAssetFolder(folder);

            if (schemaValid)
            {
                var stateA = AddHookState(layer.stateMachine, UnityMarkerStateA, new Vector3(220f, 100f));
                var stateB = AddHookState(layer.stateMachine, UnityMarkerStateB, new Vector3(560f, 100f));
                stateA.motion = CreateOrReplaceTimerClip(folder + "/SOY_UnityMarker_Beacon_A.anim", 2f);
                stateB.motion = CreateOrReplaceTimerClip(folder + "/SOY_UnityMarker_Beacon_B.anim", 2f);
                ConfigureUnityMarkerDriver(stateA, true, 121);
                ConfigureUnityMarkerDriver(stateB, true, 122);
                layer.stateMachine.defaultState = stateA;

                var toB = stateA.AddTransition(stateB);
                toB.hasExitTime = true; toB.exitTime = 1f; toB.duration = 0f;
                var toA = stateB.AddTransition(stateA);
                toA.hasExitTime = true; toA.exitTime = 1f; toA.duration = 0f;
            }
            else
            {
                var state = AddHookState(layer.stateMachine, UnityMarkerStateInvalid, new Vector3(260f, 120f));
                layer.stateMachine.defaultState = state;
                ConfigureUnityMarkerDriver(state, false, 0);
            }

            fxController.AddLayer(layer);
            EditorUtility.SetDirty(fxController);
            operationLog.Insert(0,
                "Published Unity marker v" + Version + " " + BuildNumber + " / OSC protocol " + OscProtocolVersion +
                " / schema " + (schemaValid ? "VALID with periodic marker beacon" : "INVALID — migration/repair required") + ".");
        }

        private void DrawProtocolCompatibilityCard()
        {
            BeginCard("OSC Runtime Compatibility");
            var markerPresent = HasCurrentCompatibilityMarkerLayer();
            var markerStructurePresent = HasCurrentCompatibilityMarkerStructure();
            var markerLayerExists = fxController != null &&
                fxController.layers.Any(layer => layer.name == UnityMarkerLayer && layer.stateMachine != null);
            var schemaValid = managedRepairAuditReady && CurrentSchemaIsValid();
            DrawTagRow("Unity Tool", "v" + Version + " " + BuildNumber, "Written into the avatar marker layer");
            DrawTagRow("OSC Protocol", OscProtocolVersion.ToString(), "Desktop v0.8.22-prebuild.1 requires Protocol 21 for physical helpful-item interactions");
            DrawTagRow("Marker Layer",
                markerPresent ? "✓ Installed" :
                markerStructurePresent ? "! Installed / Schema Invalid" :
                markerLayerExists ? "! Present / Outdated" : "✕ Missing",
                UnityMarkerLayer);
            DrawTagRow("Schema", markerPresent && schemaValid ? "✓ Valid" : "✕ Update Required", "Legacy/broken Stories-managed Contacts remain blocked by the Desktop runtime");
            EditorGUILayout.HelpBox(
                "TB18 keeps the metadata-based compatibility marker and adds Protocol 21 physical helpful-item interactions. The 121/122 local beacon identifies the TB18 / Protocol 21 schema to Desktop v0.8.22-prebuild.1.",
                markerPresent && schemaValid ? MessageType.Info : MessageType.Warning);
            using (new EditorGUI.DisabledScope(avatarDescriptor == null))
            {
                if (GUILayout.Button("MIGRATE / VALIDATE AVATAR FOR PROTOCOL " + OscProtocolVersion, GUILayout.Height(largeControls ? 46f : 34f)))
                {
                    LoadFromAvatarDescriptor();
                    if (EnsureSafeFxCopy(true))
                    {
                        AuditManagedSystems();
                        if (managedRepairFindings.Any(finding => finding.CanAutoRepair))
                            RunManagedRepair(false);
                        InstallAllBridgeHooks();
                        if (GetInstalledSpellDefinitions().Length + GetInstalledTechnickDefinitions().Length + GetInstalledItemDefinitions().Length > 0)
                            RebuildAllAutomatedActionLayers();
                    }
                }
            }
            EndCard();
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
            var markerParameterRepairs = RepairUnityMarkerParameterContract();
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
                AuditManagedSystems();
                RebuildUnityToolMarkerLayer(ManagedSchemaCoreIsValid());
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshHealthAudit();

            var summary = "Repaired " + markerParameterRepairs + " Unity marker parameter setting(s), added " + parameterCount + " Animator parameter(s), " +
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
                var desiredNetworkSync = DesiredNetworkSync(spec.Name, spec.NetworkSynced);
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
                    if (existing.networkSynced != desiredNetworkSync)
                    {
                        if (desiredNetworkSync && !existing.networkSynced &&
                            SyncedExpressionCost(list) + ExpressionParameterCost(existing.valueType) > 256)
                        {
                            operationLog.Insert(0,
                                "Skipped synchronizing Expression parameter '" + spec.Name +
                                "': enabling it would exceed VRChat's 256-bit parameter budget.");
                        }
                        else
                        {
                            existing.networkSynced = desiredNetworkSync;
                            repaired = true;
                        }
                    }
                    if (repaired)
                        added++;
                    continue;
                }

                var canSync = !desiredNetworkSync ||
                    SyncedExpressionCost(list) + ExpressionParameterCost(spec.ExpressionType) <= 256;
                if (desiredNetworkSync && !canSync)
                {
                    // Never skip a required Stories parameter entirely. A local/unsynced selector
                    // still keeps local menus, OSC, and Raycast gating functional. Remote cosmetics
                    // may not mirror until the user frees parameter budget and reruns Repair.
                    operationLog.Insert(0,
                        "Added Expression parameter '" + spec.Name +
                        "' as LOCAL ONLY because synchronizing it would exceed VRChat's 256-bit parameter budget.");
                }

                list.Add(new VRCExpressionParameters.Parameter
                {
                    name = spec.Name,
                    valueType = spec.ExpressionType,
                    defaultValue = spec.DefaultValue,
                    saved = spec.Saved,
                    networkSynced = desiredNetworkSync && canSync
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

            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var menuFolder = AvatarGeneratedFolder(avatarName, "Menus");
            EnsureAssetFolder(menuFolder);
            var mainPath = menuFolder + "/Stories_RP_Menu.asset";
            var combatPath = menuFolder + "/Stories_Combat_Menu.asset";
            var targetingPath = menuFolder + "/Stories_Targeting_Menu.asset";
            var statusPath = menuFolder + "/Stories_Status_Menu.asset";
            var spellsPath = menuFolder + "/Stories_Spells_Menu.asset";
            var coreSchoolsPath = menuFolder + "/Stories_Core_Schools.asset";
            var specializedSchoolsPath = menuFolder + "/Stories_Specialized_Schools.asset";
            var forbiddenSchoolsPath = menuFolder + "/Stories_Forbidden_Schools.asset";
            var schoolBrowsePath = menuFolder + "/Stories_Browse_By_School.asset";
            var purposePath = menuFolder + "/Stories_By_Purpose.asset";
            var quickPath = menuFolder + "/Stories_Quick_Access.asset";

            var storiesMenu = LoadOrCreateMenu(mainPath);
            var combatMenu = LoadOrCreateMenu(combatPath);
            var targetingMenu = LoadOrCreateMenu(targetingPath);
            var statusMenu = LoadOrCreateMenu(statusPath);
            var spellsMenu = LoadOrCreateMenu(spellsPath);
            var installedSpells = GetInstalledSpellDefinitions();
            var installedTechnicks = GetInstalledTechnickDefinitions();
            var installedItems = GetInstalledItemDefinitions();
            var installedEvasions = GetInstalledEvasionDefinitions();
            var technicksMenu = BuildActionMenuPages(avatarName, "Technicks", "SoY_TechnickType", installedTechnicks);
            var itemsMenu = BuildActionMenuPages(avatarName, "Items", "SoY_ItemType", installedItems);
            var evasionsMenu = BuildActionMenuPages(avatarName, "Evasion", EvadeTypeParameter, installedEvasions);
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

            var actionsMenu = LoadOrCreateMenu(menuFolder + "/Stories_Actions.asset");
            Undo.RecordObject(actionsMenu, "Build Stories Action Menu");
            actionsMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (technicksMenu != null && technicksMenu.controls != null && technicksMenu.controls.Count > 0)
                actionsMenu.controls.Add(CreateSubMenuControl("Technicks", technicksMenu));
            if (itemsMenu != null && itemsMenu.controls != null && itemsMenu.controls.Count > 0)
                actionsMenu.controls.Add(CreateSubMenuControl("Items", itemsMenu));
            if (evasionsMenu != null && evasionsMenu.controls != null && evasionsMenu.controls.Count > 0)
                actionsMenu.controls.Add(CreateSubMenuControl("Evasion", evasionsMenu));
            EditorUtility.SetDirty(actionsMenu);

            Undo.RecordObject(combatMenu, "Build Stories Combat Menu");
            combatMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (HasExpressionParameter("SoY_CombatEnabled"))
                combatMenu.controls.Add(CreateToggleControl("RP Combat", "SoY_CombatEnabled"));
            if (HasExpressionParameter("SoY_IsEnemy"))
                combatMenu.controls.Add(CreateToggleControl("Enemy Mode", "SoY_IsEnemy"));
            EditorUtility.SetDirty(combatMenu);

            Undo.RecordObject(targetingMenu, "Build Stories Targeting Menu");
            targetingMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (HasInstalledManagedRaycast())
            {
                EnsureLocalRaycastTargetingExpressionParameter();
                targetingMenu.controls.Add(CreateToggleControl("Targeting Crosshair", RaycastTargetingParameter));
            }
            if (HasManualRaycastFireAction() && HasExpressionParameter(RaycastFireParameter))
                targetingMenu.controls.Add(CreateButtonControl("Projectile Fire", RaycastFireParameter, 1f));
            EditorUtility.SetDirty(targetingMenu);

            Undo.RecordObject(statusMenu, "Build Stories Status Menu");
            statusMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (HasExpressionParameter("SoY_MistPercent"))
                statusMenu.controls.Add(CreateRadialControl("Mist Charge", "SoY_MistPercent"));
            if (HasExpressionParameter("SoY_DiablosPercent"))
                statusMenu.controls.Add(CreateRadialControl("Curse Of Diablos", "SoY_DiablosPercent"));
            if (HasExpressionParameter("SoY_ArousalPercent"))
                statusMenu.controls.Add(CreateRadialControl("Arousal", "SoY_ArousalPercent"));
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
                    SpellSchool.LightMagick
                });

            Undo.RecordObject(schoolBrowseMenu, "Build Stories School Browser");
            schoolBrowseMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (coreSchoolsMenu.controls.Count > 0) schoolBrowseMenu.controls.Add(CreateSubMenuControl("Core Magick", coreSchoolsMenu));
            if (specializedSchoolsMenu.controls.Count > 0) schoolBrowseMenu.controls.Add(CreateSubMenuControl("Specialized", specializedSchoolsMenu));
            if (forbiddenSchoolsMenu.controls.Count > 0) schoolBrowseMenu.controls.Add(CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu));
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
                if (purposeMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("By Purpose", purposeMenu));
                if (schoolBrowseMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("By School", schoolBrowseMenu));
            }
            else if (menuNavigationMode == MenuNavigationMode.SchoolFirst)
            {
                if (coreSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Core Magick", coreSchoolsMenu));
                if (specializedSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Specialized", specializedSchoolsMenu));
                if (forbiddenSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu));
            }
            else
            {
                if (purposeMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("By Purpose", purposeMenu));
                if (coreSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Core Magick", coreSchoolsMenu));
                if (specializedSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Specialized", specializedSchoolsMenu));
                if (forbiddenSchoolsMenu.controls.Count > 0) spellsMenu.controls.Add(CreateSubMenuControl("Forbidden & Custom", forbiddenSchoolsMenu));
            }
            EditorUtility.SetDirty(spellsMenu);

            // Build the root last so installed-only child menus have their final contents.
            Undo.RecordObject(storiesMenu, "Build Stories RP Menu");
            storiesMenu.controls = new List<VRCExpressionsMenu.Control>();
            if (combatMenu.controls != null && combatMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Combat", combatMenu));
            if (spellsMenu.controls != null && spellsMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Spells", spellsMenu));
            if (actionsMenu.controls != null && actionsMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Actions", actionsMenu));
            if (targetingMenu.controls != null && targetingMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Targeting", targetingMenu));
            if (statusMenu.controls != null && statusMenu.controls.Count > 0)
                storiesMenu.controls.Add(CreateSubMenuControl("Status", statusMenu));
            if (quickMenu.controls.Count > 0 && storiesMenu.controls.Count < 8)
                storiesMenu.controls.Add(CreateSubMenuControl("Quick Access", quickMenu));
            EditorUtility.SetDirty(storiesMenu);

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
            operationLog.Insert(0, "Stories RP sub-menu created at " + mainPath + " using installed Contacts only: " +
                installedSpells.Length + " spell(s), " + installedTechnicks.Length + " technick(s), " + installedItems.Length + " item(s), " + installedEvasions.Length + " evade(s).");
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
            var installedIds = new HashSet<int>(GetInstalledSpellDefinitions().Select(spell => spell.Id));
            var spells = GetSpellsForSchool(school).Where(spell => installedIds.Contains(spell.Id)).ToArray();
            return BuildSpellDefinitionPages(avatarName, GetSpellSchoolAssetLabel(school), spells);
        }

        private VRCExpressionsMenu BuildSpellCategoryMenuPages(string avatarName, SpellCategory category)
        {
            var installedIds = new HashSet<int>(GetInstalledSpellDefinitions().Select(spell => spell.Id));
            var spells = SpellDefinitions
                .Where(spell => spell.Category == category && installedIds.Contains(spell.Id))
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
                var path = AvatarGeneratedFolder(avatarName, "Menus") + "/" + assetLabel + "_Page_" + (page + 1) + ".asset";
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
                var path = AvatarGeneratedFolder(avatarName, "Menus") + "/Stories_" + safeLabel + "_Page_" + (page + 1) + ".asset";
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
            EnsureResourceGaugeProfileDefaults();
            foreach (var kind in AllResourceGaugeKinds())
            {
                var config = GetResourceGaugeProfile(kind);
                var existingLayers = target.layers.Where(layer => layer.name == ResourceLayerName(kind)).ToArray();
                var shouldExist = config != null && config.enabled && config.visibility != ResourceVisibilityMode.Never && config.blendPoints.Any(point => point != null && point.enabled);
                var needsUpgrade = shouldExist && (existingLayers.Length != 1 || !ResourceLayerIsCurrent(existingLayers.FirstOrDefault(), kind));
                if (!shouldExist && existingLayers.Length > 0)
                {
                    RemoveManagedResourceLayer(target, kind);
                    added++;
                }
                else if (needsUpgrade)
                {
                    RemoveManagedResourceLayer(target, kind);
                    AddResourceGaugeLayer(target, kind);
                    added++;
                    operationLog.Insert(0, "Installed or upgraded " + ResourceDisplayName(kind) + " Resource FX.");
                }
            }
            if (!target.layers.Any(layer => layer.name == ReactionLayer))
            {
                AddReactionLayer(target);
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

        private static string SanitizeAnimatorStateName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Stories State";

            // Unity rejects '/' in Animator state names. TB17.1/TB17.2 used slash-delimited
            // compatibility-marker labels, which caused AddState warnings and prevented the
            // marker audit from converging. Sanitize every Stories-generated state at the
            // common creation point so future builders cannot reintroduce the same failure.
            return name.Replace("/", " - ").Replace("\\", " - ").Trim();
        }

        private static AnimatorState AddHookState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            var safeName = SanitizeAnimatorStateName(name);
            if (!string.Equals(name, safeName, StringComparison.Ordinal))
                Debug.LogWarning("[Stories OSC Unity Tool] Sanitized Animator state name '" + name + "' -> '" + safeName + "'.");
            if (safeName.IndexOf('/') >= 0 || safeName.IndexOf('\\') >= 0)
                throw new InvalidOperationException("Generated Animator state name still contains an illegal path separator: '" + safeName + "'.");
            var state = machine.AddState(safeName, position);
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

        private static bool ResourceLayerIsCurrent(AnimatorControllerLayer layer, ResourceGaugeKind kind)
        {
            if (layer == null || layer.stateMachine == null) return false;
            var blendState = layer.stateMachine.states.Select(child => child.state)
                .FirstOrDefault(state => state != null && state.name == ResourceDisplayName(kind) + " Blend");
            var tree = blendState != null ? blendState.motion as BlendTree : null;
            if (tree == null || tree.blendType != BlendTreeType.Simple1D || tree.blendParameter != ResourceParameter(kind)) return false;
            var special = ResourceSpecialStateName(kind);
            return string.IsNullOrWhiteSpace(special) || layer.stateMachine.states.Any(child => child.state != null && child.state.name == special);
        }

        private void AddVitalLayer(AnimatorController target)
        {
            AddResourceGaugeLayer(target, ResourceGaugeKind.Health);
        }

        private void AddResourceGaugeLayer(AnimatorController target, ResourceGaugeKind kind)
        {
            EnsureResourceGaugeProfileDefaults();
            var config = GetResourceGaugeProfile(kind);
            if (config == null || !config.enabled || config.visibility == ResourceVisibilityMode.Never) return;

            var layer = CreateHookLayer(target, ResourceLayerName(kind));
            var blendState = AddHookState(layer.stateMachine, ResourceDisplayName(kind) + " Blend", new Vector3(440f, 120f));
            var visibilityParameter = config.visibility == ResourceVisibilityMode.CombatOnly
                ? "SoY_CombatEnabled"
                : config.visibility == ResourceVisibilityMode.ApplicableOnly ? ResourceApplicabilityParameter(kind) : string.Empty;
            AnimatorState inactive = null;
            if (!string.IsNullOrWhiteSpace(visibilityParameter))
            {
                inactive = AddHookState(layer.stateMachine, "Hidden / Inactive", new Vector3(120f, 120f));
                layer.stateMachine.defaultState = inactive;
                var hide = layer.stateMachine.AddAnyStateTransition(inactive);
                hide.hasExitTime = false; hide.duration = 0f; hide.canTransitionToSelf = false;
                hide.AddCondition(AnimatorConditionMode.IfNot, 0f, visibilityParameter);
            }
            else
            {
                layer.stateMachine.defaultState = blendState;
            }

            var blendTree = new BlendTree
            {
                name = "SOY " + ResourceDisplayName(kind) + " Gauge Blend Tree",
                hideFlags = HideFlags.HideInHierarchy,
                blendType = BlendTreeType.Simple1D,
                blendParameter = ResourceParameter(kind),
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, target);
            var avatarName = MakeSafeAssetName(avatarDescriptor != null ? avatarDescriptor.gameObject.name : "Avatar");
            var folder = AvatarGeneratedFolder(avatarName, "Animations/Resources/" + MakeSafeAssetName(ResourceId(kind)));
            var points = config.blendPoints.Where(point => point != null && point.enabled).OrderBy(point => point.threshold).ToList();
            var usedThresholds = new List<float>();
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var threshold = Mathf.Clamp01(point.threshold);
                if (usedThresholds.Count > 0 && threshold <= usedThresholds[usedThresholds.Count - 1])
                {
                    var previous = usedThresholds[usedThresholds.Count - 1];
                    if (previous >= 0.9999f) { operationLog.Insert(0, "Skipped duplicate " + ResourceDisplayName(kind) + " threshold at 100% for '" + point.label + "'."); continue; }
                    threshold = Mathf.Min(1f, previous + 0.0001f);
                }
                var safeLabel = MakeSafeAssetName(string.IsNullOrWhiteSpace(point.label) ? "Point_" + (i + 1) : point.label);
                var motion = LoadClipFromPath(point.clipPath) ?? GetOrCreatePlaceholderClip(folder, "SOY_" + ResourceId(kind) + "_" + safeLabel);
                blendTree.AddChild(motion, threshold);
                usedThresholds.Add(threshold);
            }
            if (usedThresholds.Count == 0)
                blendTree.AddChild(GetOrCreatePlaceholderClip(folder, "SOY_" + ResourceId(kind) + "_Fallback"), 1f);
            blendState.motion = blendTree;

            var specialParameter = ResourceSpecialBoolParameter(kind);
            var specialName = ResourceSpecialStateName(kind);
            AnimatorState specialState = null;
            if (!string.IsNullOrWhiteSpace(specialParameter) && !string.IsNullOrWhiteSpace(specialName))
            {
                specialState = AddHookState(layer.stateMachine, specialName, new Vector3(720f, 260f));
                specialState.motion = LoadClipFromPath(config.specialClipPath) ?? GetOrCreatePlaceholderClip(folder, "SOY_" + ResourceId(kind) + "_" + MakeSafeAssetName(specialName));
                var enterSpecial = layer.stateMachine.AddAnyStateTransition(specialState);
                enterSpecial.hasExitTime = false; enterSpecial.duration = 0f; enterSpecial.canTransitionToSelf = false;
                if (!string.IsNullOrWhiteSpace(visibilityParameter))
                    enterSpecial.AddCondition(AnimatorConditionMode.If, 0f, visibilityParameter);
                enterSpecial.AddCondition(AnimatorConditionMode.If, 0f, specialParameter);
            }

            if (!string.IsNullOrWhiteSpace(visibilityParameter))
            {
                var show = layer.stateMachine.AddAnyStateTransition(blendState);
                show.hasExitTime = false; show.duration = 0.08f; show.canTransitionToSelf = false;
                show.AddCondition(AnimatorConditionMode.If, 0f, visibilityParameter);
                if (!string.IsNullOrWhiteSpace(specialParameter)) show.AddCondition(AnimatorConditionMode.IfNot, 0f, specialParameter);
            }
            else if (specialState != null)
            {
                var leaveSpecial = specialState.AddTransition(blendState);
                leaveSpecial.hasExitTime = false; leaveSpecial.duration = 0.08f;
                leaveSpecial.AddCondition(AnimatorConditionMode.IfNot, 0f, specialParameter);
            }

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
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Reflection write failed on " + component.GetType().Name + ": " + exception.Message + ". Trying Unity serialization fallback.");
            }

            // VRChat has changed some SDK component internals between releases. The Inspector
            // can still serialize those values even when a public/private C# member was renamed.
            // Falling back to SerializedObject keeps Raycast configuration resilient without
            // compile-time binding to one specific SDK layout.
            return WriteSerializedMember(component, value, names);
        }

        private static string NormalizeSerializedMemberName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static bool SerializedPropertyMatches(SerializedProperty property, IEnumerable<string> names)
        {
            if (property == null)
                return false;
            var propertyName = NormalizeSerializedMemberName(property.name);
            var displayName = NormalizeSerializedMemberName(property.displayName);
            foreach (var candidate in names ?? Enumerable.Empty<string>())
            {
                var normalized = NormalizeSerializedMemberName(candidate);
                if (normalized.Length == 0)
                    continue;
                if (propertyName == normalized ||
                    propertyName == "m" + normalized ||
                    displayName == normalized)
                    return true;
            }
            return false;
        }

        private static bool WriteSerializedMember(Component component, object value, params string[] names)
        {
            if (component == null || names == null || names.Length == 0)
                return false;

            try
            {
                var serialized = new SerializedObject(component);
                serialized.Update();
                var iterator = serialized.GetIterator();
                var enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (!SerializedPropertyMatches(iterator, names))
                        continue;

                    var applied = false;
                    switch (iterator.propertyType)
                    {
                        case SerializedPropertyType.Boolean:
                            iterator.boolValue = Convert.ToBoolean(value);
                            applied = true;
                            break;
                        case SerializedPropertyType.Integer:
                        case SerializedPropertyType.LayerMask:
                            iterator.intValue = Convert.ToInt32(value);
                            applied = true;
                            break;
                        case SerializedPropertyType.Float:
                            iterator.floatValue = Convert.ToSingle(value);
                            applied = true;
                            break;
                        case SerializedPropertyType.String:
                            iterator.stringValue = value != null ? value.ToString() : string.Empty;
                            applied = true;
                            break;
                        case SerializedPropertyType.Vector3:
                            if (value is Vector3 vector)
                            {
                                iterator.vector3Value = vector;
                                applied = true;
                            }
                            break;
                        case SerializedPropertyType.Quaternion:
                            if (value is Quaternion quaternion)
                            {
                                iterator.quaternionValue = quaternion;
                                applied = true;
                            }
                            break;
                        case SerializedPropertyType.ObjectReference:
                            if (value == null || value is UnityEngine.Object)
                            {
                                iterator.objectReferenceValue = value as UnityEngine.Object;
                                applied = true;
                            }
                            break;
                    }

                    if (!applied)
                        return false;

                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(component);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Unity serialization fallback failed on " + component.GetType().Name + ": " + exception.Message);
            }
            return false;
        }

        private static bool SetSerializedEnumMemberByKeywords(Component component, string[] keywords, params string[] names)
        {
            if (component == null || keywords == null || keywords.Length == 0)
                return false;
            try
            {
                var serialized = new SerializedObject(component);
                serialized.Update();
                var iterator = serialized.GetIterator();
                var enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (!SerializedPropertyMatches(iterator, names) || iterator.propertyType != SerializedPropertyType.Enum)
                        continue;

                    var options = iterator.enumNames ?? Array.Empty<string>();
                    var match = options
                        .Select((name, index) => new { name, index })
                        .Where(entry => keywords.All(keyword => entry.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                        .OrderBy(entry => entry.name.Length)
                        .FirstOrDefault();

                    if (match == null)
                    {
                        match = options
                            .Select((name, index) => new { name, index })
                            .Where(entry => entry.name.IndexOf(keywords[0], StringComparison.OrdinalIgnoreCase) >= 0)
                            .OrderBy(entry => entry.name.Length)
                            .FirstOrDefault();
                    }

                    if (match == null)
                        return false;

                    iterator.enumValueIndex = match.index;
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(component);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Could not set serialized enum on " + component.GetType().Name + ": " + exception.Message);
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

        private static bool SetLayerMaskMember(Component component, int mask, params string[] names)
        {
            if (component == null)
                return false;
            var member = FindMember(component.GetType(), names);
            try
            {
                if (member is FieldInfo field)
                {
                    if (field.FieldType == typeof(LayerMask))
                        field.SetValue(component, (LayerMask)mask);
                    else
                        field.SetValue(component, Convert.ChangeType(mask, field.FieldType));
                    return true;
                }
                if (member is PropertyInfo property && property.CanWrite)
                {
                    if (property.PropertyType == typeof(LayerMask))
                        property.SetValue(component, (LayerMask)mask, null);
                    else
                        property.SetValue(component, Convert.ChangeType(mask, property.PropertyType), null);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Stories Of Yggdrasil OSC Contact System] Reflection layer-mask write failed: " + exception.Message + ". Trying Unity serialization fallback.");
            }

            return WriteSerializedMember(component, mask, names);
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
